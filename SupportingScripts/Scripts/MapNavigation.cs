// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity
{
    using Microsoft.Geospatial;
    using Microsoft.Geospatial.VectorMath;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    /// <summary>
    /// This component provides a wrapper on top of a MapRenderer to help with common interactions like panning, zooming, and rotating.
    /// </summary>
    public class MapNavigation : MonoBehaviour
    {
        private const double DegToRad = Math.PI / 180.0;

        /// <summary>
        /// If the gaze is pointed straight down, then the gaze angle in this case is 0.  As the gaze moves upward, it hits
        /// this threshold which it won't be able to go above for the purposes of zoom center point adjustment.
        /// </summary>
        private const double MaximumGazeAngleInDegrees = 80.0;

        private const double MaximumGazeAngleInRadians = MaximumGazeAngleInDegrees * DegToRad;
        private static readonly float MinimumGazeY = (float)Math.Cos(MaximumGazeAngleInRadians);

        private const float ZoomLevelsInPerSecond = 1.0f;
        private const float ZoomLevelsOutPerSecond = 1.1f;

        private readonly Stack<int> _rotationDirectionStack = new Stack<int>();
        private int _activeRotationDirection = 0;
        private float _targetRotation = 0.0f;

        [SerializeField]
        private MapRenderer _map = null;

        [SerializeField]
        private float _rotationSpeed = 3.5f;

        /// <summary>
        /// A speed of 1.0 means that map will pan a full view in 1 second.
        /// </summary>
        [SerializeField]
        private float _panSpeed = 0.5f;

        private void Awake()
        {
            Debug.Assert(_map != null);
        }

        private void Update()
        {
            // TODO: Move to coroutine.

            const float rotationAngle = 45.0f;

            // Update rotation, if needed.
            var currentMapRotation = _map.transform.localRotation.eulerAngles.y;
            if (_activeRotationDirection == 0 && _rotationDirectionStack.Any())
            {
                _activeRotationDirection = _rotationDirectionStack.Pop();
                _targetRotation = rotationAngle * Mathf.Round((currentMapRotation / rotationAngle) + _activeRotationDirection);
                //Debug.Log(currentMapRotation + ", " + _activeRotationDirection + ", " + _targetRotation);
                _targetRotation = _targetRotation < 0.0f ? (float)FMod(_targetRotation, 360.0f) : _targetRotation;
            }

            if (_activeRotationDirection != 0 && _targetRotation != currentMapRotation)
            {
                currentMapRotation = _activeRotationDirection < 0 && currentMapRotation == 0.0f ? 360.0f : currentMapRotation;
                var newRotation = currentMapRotation + rotationAngle * _activeRotationDirection * Time.deltaTime * _rotationSpeed;

                // Check if we are done. If so, reset active and target rotation to 0.
                var isComplete = false;
                if (_activeRotationDirection < 0)
                {
                    isComplete = newRotation <= _targetRotation;
                }
                else if (_activeRotationDirection > 0)
                {
                    isComplete = newRotation >= _targetRotation;
                }

                if (isComplete)
                {
                    newRotation = _targetRotation == 360.0f ? 0.0f : _targetRotation;
                    _activeRotationDirection = 0;
                    _targetRotation = 0;
                }

                _map.transform.localRotation =
                    Quaternion.Euler(_map.transform.localRotation.eulerAngles.x, newRotation, _map.transform.localRotation.eulerAngles.z);
            }
        }

        /// <summary>
        /// Begins a rotation that animates the map by 45 degrees.
        /// </summary>
        public void RotateMap(bool isClockwise)
        {
            _rotationDirectionStack.Push(isClockwise ? 1 : -1);
        }

        /// <summary>
        /// Pans the map by the specified direction. 
        /// </summary>
        /// <param name="direction">
        /// The direction is relative to the map's surface. The magnitude of the direction affects the amount that is panned.
        /// </param>
        /// <param name="orientWithMap">
        /// If true, the direction is relative to the map transform's forward vector.
        /// Otherwise, transforms the map with X representing west to east, and Y representing north to south.
        /// </param>
        public void Pan(Vector2 direction, bool orientWithMap)
        {
            var mercatorExtent = 2.0 / Math.Pow(2, _map.ZoomLevel - 1);
            if (orientWithMap)
            {
                // The following assumes the map is oriented with y-up. It should be made to be more generic by using the _map transform basis.

                var currentRotation = _map.transform.localRotation.eulerAngles.y;
                Translate(
                    mercatorExtent * _panSpeed * Time.deltaTime *
                    new Vector2D(
                        (direction.x * Math.Cos(currentRotation * DegToRad)) - (direction.y * Math.Sin(currentRotation * DegToRad)),
                        (direction.x * Math.Sin(currentRotation * DegToRad)) + (direction.y * Math.Cos(currentRotation * DegToRad))));
            }
            else
            {
                Translate(mercatorExtent * _panSpeed * Time.deltaTime * new Vector2D(direction.x, direction.y));
            }
        }

        /// <summary>
        /// Pans the map north.
        /// </summary>
        public void PanNorth()
        {
            Pan(new Vector2(0, 3), false);
        }

        /// <summary>
        /// Pans the map south.
        /// </summary>
        public void PanSouth()
        {
            Pan(new Vector2(0, -3), false);
        }

        /// <summary>
        /// Pans the map east.
        /// </summary>
        public void PanEast()
        {
            Pan(new Vector2(3, 0), false);
        }

        /// <summary>
        /// Pans the map west.
        /// </summary>
        public void PanWest()
        {
            Pan(new Vector2(-3, 0), false);
        }

        /// <summary>
        /// Converts a surface coordinate to a geo position (lat/lon).  The surface coordinate's x value is the same as the local x of the map
        /// surface and the y is the same as the local z axis of the map.
        /// </summary>
        private LatLon MapSurfaceToGeoposition(Vector2 surfaceCoordinate)
        {
            // x = -0.5...0.5 (left-to-right/west-to-east), y = -0.5...0.5 (bottom-to-top/south-to-north)
            Vector2 limitedSurfaceCoordinate = new Vector2(
                Math.Max(-0.5F, Math.Min(0.5F, surfaceCoordinate.x / _map.LocalMapDimension.x)),
                Math.Max(-0.5F, Math.Min(0.5F, surfaceCoordinate.y / _map.LocalMapDimension.y)));

            var mercatorBox = _map.Bounds.ToMercatorBoundingBox();
            var mercatorX = mercatorBox.BottomLeft.X + (0.5F + limitedSurfaceCoordinate.x) * (mercatorBox.TopRight.X - mercatorBox.BottomLeft.X);
            var mercatorY = mercatorBox.BottomLeft.Y + (0.5F + limitedSurfaceCoordinate.y) * (mercatorBox.TopRight.Y - mercatorBox.BottomLeft.Y);
            return LatLon.FromMercatorPosition(new Vector2D(mercatorX, mercatorY));
        }

        /// <summary>
        /// Adjusts the map's zoom level by the specified magnitude per second. Zooms the map towards the specified gaze ray.
        /// </summary>
        /// <param name="zoomLevelSpeed">1 for normal maximum zoom-in speed, and -1 for normal maximum zoom-out speed.</param>
        /// <param name="gazeOrigin">The origin of the gaze ray.</param>
        /// <param name="gazeDirection">The direction of the gaze ray.</param>
        public void Zoom(float zoomLevelSpeed, Vector3 gazeOrigin, Vector3 gazeDirection)
        {
            ZoomInternal(zoomLevelSpeed, new Ray(gazeOrigin, gazeDirection));
        }

        /// <summary>
        /// Adjusts the map's zoom level by the specified magnitude per second. Zooms the map towards the specified gaze ray.
        /// </summary>
        public void Zoom(float zoomLevelSpeed, Ray gazeRay)
        {
            ZoomInternal(zoomLevelSpeed, gazeRay);
        }

        /// <summary>
        /// Adjusts the map's zoom level by the specified magnitude per second.
        /// </summary>
        /// <param name="zoomLevelSpeed">1 for normal maximum zoom-in speed, and -1 for normal maximum zoom-out speed.</param>
        public void Zoom(float zoomLevelSpeed)
        {
            ZoomInternal(zoomLevelSpeed, null);
        }

        private void ZoomInternal(float zoomLevelSpeed, Ray? gazeRay = null)
        {
            if (zoomLevelSpeed == 0)
            {
                return;
            }

            float newZoomLevel = _map.ZoomLevel;

            // When zooming in, adjust the zoom center based on the direction of the user's gaze.

            if (zoomLevelSpeed > 0)
            {
                newZoomLevel += ZoomLevelsInPerSecond * Time.deltaTime * zoomLevelSpeed;

                // Don't do gaze zooming on the HoloLens since you have to be looking at the button.
                if (gazeRay.HasValue)
                {
                    var gazeRayValue = gazeRay.Value;

                    // Get the gaze ray in map space.
                    Vector3 localGazeOrigin = _map.transform.InverseTransformPoint(gazeRayValue.origin);
                    localGazeOrigin.y -= _map.LocalMapHeight;

                    // Collide the gaze ray with the plane that lies on the surface of the map only if the gaze origin is above the map plane.
                    if (localGazeOrigin.y > 0)
                    {
                        Vector3 localGazeDirection = gameObject.transform.InverseTransformDirection(gazeRayValue.direction);

                        // Force the ray to be downward so that it collides with the plane.
                        if (localGazeDirection.y > -MinimumGazeY)
                        {
                            localGazeDirection.y = -MinimumGazeY;
                            localGazeDirection.Normalize();
                        }

                        // Calculate coordinates where the ray intersects the plane (may be outside the limits of the map).
                        float gazeElevationRatio = localGazeOrigin.y / -localGazeDirection.y;
                        Vector2 surfaceCoordinate = new Vector2(
                            localGazeOrigin.x + localGazeDirection.x * gazeElevationRatio,
                            localGazeOrigin.z + localGazeDirection.z * gazeElevationRatio);

                        // Get the constrained lat/lon of the target
                        LatLon target = MapSurfaceToGeoposition(surfaceCoordinate);

                        // Adjust the center
                        double altitudeRatio = ZoomLevelToMercatorAltitude(newZoomLevel) / ZoomLevelToMercatorAltitude(_map.ZoomLevel);
                        double latitudeDelta = target.LatitudeInDegrees - _map.Center.LatitudeInDegrees;
                        double longitudeDelta = target.LongitudeInDegrees - _map.Center.LongitudeInDegrees;
                        _map.Center = new LatLon(
                            _map.Center.LatitudeInDegrees + latitudeDelta * (1.0 - altitudeRatio),
                            _map.Center.LongitudeInDegrees + longitudeDelta * (1.0 - altitudeRatio));
                    }
                }
            }
            else
            {
                newZoomLevel += ZoomLevelsOutPerSecond * Time.deltaTime * zoomLevelSpeed;
            }

            _map.ZoomLevel = newZoomLevel;
        }

        /// <summary>
        /// Translates the map by the specified amount in Mercator space.
        /// </summary>
        private void Translate(Vector2D amount)
        {
            _map.Center = LatLon.FromMercatorPosition(_map.Center.ToMercatorPosition() + amount);
        }

        private static float MercatorAltitudeToZoomLevel(double mercatorAltitude)
        {
            return (float)Math.Log(1.0 / mercatorAltitude, 2.0) - 1.0F;
        }

        private static double ZoomLevelToMercatorAltitude(float zoomLevel)
        {
            return Math.Pow(2, 1.0 - zoomLevel);
        }

        private static double FMod(double a, double b)
        {
            return a - b * Math.Floor(a / b);
        }
    }

}
