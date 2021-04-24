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
    using UnityEngine.Events;

    /// <summary>
    /// This component provides a wrapper on top of a <see cref="MapRenderer"/> to help with interactions like pan, zoom, and rotate.
    /// Provides callbacks for events to track when interactions have began and ended or when specific events occur, like double tap,
    /// tap, or tap and hold. A <see cref="MapInteractionHandler"/> implementaiton can invoke these events.
    /// </summary>
    [RequireComponent(typeof(MapRenderer))]
    [DisallowMultipleComponent]
    public class MapInteractionController : MonoBehaviour
    {
        private const float ZoomLevelsPerSecond = 1.0f;

        private readonly Stack<int> _rotationDirectionStack = new Stack<int>();

        private MapRenderer _mapRenderer;
        private int _activeRotationDirection;
        private float _targetRotation;

        [SerializeField]
        private float _rotationSpeed = 3.5f;

        /// <summary>
        /// A speed of 1.0 means that map will pan a full view in 1 second.
        /// </summary>
        [SerializeField]
        private float _panSpeed = 0.5f;

        [SerializeField]
        private LatLonAltUnityEvent _onTapAndHold = new LatLonAltUnityEvent();

        /// <summary>
        /// An event fired when a tap and hold occurs. Provides the location where the interaction takes place.
        /// </summary>
        public LatLonAltUnityEvent OnTapAndHold => _onTapAndHold;

        [SerializeField]
        private LatLonAltUnityEvent _onDoubleTap = new LatLonAltUnityEvent();

        /// <summary>
        /// An event fired when a double tap occurs. Provides the location where the interaction takes place.
        /// </summary>
        public LatLonAltUnityEvent OnDoubleTap => _onDoubleTap;

        [SerializeField]
        private UnityEvent _onInteractionStarted = new UnityEvent();

        /// <summary>
        /// An event fired when an interaction begins that manipulates the map, e.g. panning the map.
        /// </summary>
        public UnityEvent OnInteractionStarted => _onInteractionStarted;

        [SerializeField]
        private UnityEvent _onInteractionEnded = new UnityEvent();

        /// <summary>
        /// An event fired when an interaction ends that manipulated the map, e.g. panning the map.
        /// </summary>
        public UnityEvent OnInteractionEnded => _onInteractionEnded;

        private void Awake()
        {
            _mapRenderer = GetComponent<MapRenderer>();
            Debug.Assert(_mapRenderer != null);
        }

        private void OnEnable()
        {
            _mapRenderer = GetComponent<MapRenderer>();
            Debug.Assert(_mapRenderer != null);
        }

        private void Update()
        {
            const float rotationAngle = 45.0f;

            // Update rotation, if needed.
            var currentMapRotation = _mapRenderer.transform.localRotation.eulerAngles.y;
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

                _mapRenderer.transform.localRotation =
                    Quaternion.Euler(_mapRenderer.transform.localRotation.eulerAngles.x, newRotation, _mapRenderer.transform.localRotation.eulerAngles.z);
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
        /// Pans the map in the specified direction.
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
            var mercatorExtent = 2.0 / Math.Pow(2, _mapRenderer.ZoomLevel - 1);
            if (orientWithMap)
            {
                // The following assumes the map is oriented with y-up. It should be made to be more generic by using the _map transform basis.

                var currentRotationInDegrees = _mapRenderer.transform.localRotation.eulerAngles.y;
                var currentRotationInRadians = currentRotationInDegrees * Constants.DegreesToRadians;
                Pan(
                    mercatorExtent * _panSpeed * Time.deltaTime *
                    new MercatorCoordinate(
                        (direction.x * Math.Cos(currentRotationInRadians)) - (direction.y * Math.Sin(currentRotationInRadians)),
                        (direction.x * Math.Sin(currentRotationInRadians)) + (direction.y * Math.Cos(currentRotationInRadians))));
            }
            else
            {
                Pan(mercatorExtent * _panSpeed * Time.deltaTime * new MercatorCoordinate(direction.x, direction.y));
            }
        }

        /// <summary>
        /// Given a ray, target coordinate and target altitude, adjust the map's center so the target coordinate aligns with the ray.
        /// </summary>
        public void Pan(
            Ray ray,
            in MercatorCoordinate targetMercatorCoordinate,
            double targetAltitudeInMeters,
            bool requireRayHitForZoom = false)
        {
            PanAndZoom(ray, (targetMercatorCoordinate, targetAltitudeInMeters), 0.0f, requireRayHitForZoom);
        }

        /// <summary>
        /// Given a ray, target coordinate, and zoom level delta, adjust the map's center and zoom level so that at the new zoom level, the
        /// target coordinate aligns with the ray.
        /// </summary>
        public void PanAndZoom(
            Ray ray,
            in MercatorCoordinate targetMercatorCoordinate,
            double targetAltitudeInMeters,
            float zoomLevelSpeed,
            bool requireRayHitForZoom = false)
        {
            PanAndZoom(ray, (targetMercatorCoordinate, targetAltitudeInMeters), zoomLevelSpeed, requireRayHitForZoom);
        }

        /// <summary>
        /// Adjusts the map's zoom level by the specified magnitude per second.
        /// </summary>
        /// <param name="zoomLevelSpeed">1 for normal maximum zoom-in speed, and -1 for normal maximum zoom-out speed.</param>
        public void Zoom(float zoomLevelSpeed)
        {
            _mapRenderer.ZoomLevel += ZoomLevelsPerSecond * Time.deltaTime * zoomLevelSpeed;
        }

        /// <summary>
        /// Adjusts the map's zoom level by the specified magnitude per second. Zooms the map towards the specified ray.
        /// </summary>
        public void Zoom(float zoomLevelSpeed, Ray ray, bool requireRayHitForZoom = false)
        {
            PanAndZoom(ray, null, zoomLevelSpeed, requireRayHitForZoom);
        }

        /// <summary>
        /// Performs a double tap zoom and invokes the <see cref="OnDoubleTap"/> event.
        /// </summary>
        public void DoubleTapZoom(LatLonAlt targetLatLonAlt, float zoomLevelOffset)
        {
            if (zoomLevelOffset == 0.0f)
            {
                return;
            }

            // Handle case where we've zoomed in to the max level already. If so, early out.
            var newZoomLevel = _mapRenderer.ZoomLevel + zoomLevelOffset;
            newZoomLevel = Mathf.Max(_mapRenderer.MinimumZoomLevel, Mathf.Min(_mapRenderer.MaximumZoomLevel, newZoomLevel));
            if (newZoomLevel == _mapRenderer.ZoomLevel)
            {
                return;
            }

            // Find center point of zoomed view and zoom to that.
            var targetOffsetInLocalSpace = _mapRenderer.TransformLatLonAltToLocalPoint(targetLatLonAlt);
            var zoomScale = 1.0 - 1.0 / Math.Pow(2, newZoomLevel - _mapRenderer.ZoomLevel);
            var newCenterInLocalSpace = Vector3.Lerp(Vector3.zero, targetOffsetInLocalSpace, (float)zoomScale);
            var newCenter = _mapRenderer.TransformLocalPointToMercator(newCenterInLocalSpace).ToLatLon();

            _mapRenderer.SetMapScene(new MapSceneOfLocationAndZoomLevel(newCenter, newZoomLevel), MapSceneAnimationKind.Linear, 150.0f);

            OnDoubleTap?.Invoke(targetLatLonAlt);
        }

        /// <summary>
        /// Translates the map by the specified amount in Mercator space.
        /// </summary>
        private void Pan(in MercatorCoordinate amount)
        {
            _mapRenderer.Center = (_mapRenderer.Center.ToMercatorCoordinate() + amount).ToLatLon();
        }

        /// <summary>
        /// Given a ray, target coordinate, and zoom level delta, adjust the map's center and zoom level so that at the new zoom level, the
        /// target coordinate aligns with the ray. If no target coordinate is specified, a target coordinate is determined from the ray so
        /// zooming will be relative to that automatically derived coordinate. If the ray does not intersect the map, and the target
        /// coordinate is not specified, the zoom is relative to the map center.
        /// </summary>
        private void PanAndZoom(
            Ray ray,
            ValueTuple<MercatorCoordinate, double>? targetCoordinateAndAltitude,
            float zoomLevelSpeed,
            bool requireRayHitForZoom)
        {
            var zoomLevelDelta = ZoomLevelsPerSecond * Time.deltaTime * zoomLevelSpeed;
            if (zoomLevelDelta != 0 &&
                !(_mapRenderer.ZoomLevel >= _mapRenderer.MaximumZoomLevel && zoomLevelDelta > 0) &&
                !(_mapRenderer.ZoomLevel <= _mapRenderer.MinimumZoomLevel && zoomLevelDelta < 0))
            {
                // If we're not panning, detemrine where the ray is intersecting the map. If it's not intersecting the map,
                // no zoom will occur.
                if (!targetCoordinateAndAltitude.HasValue)
                {
                    if (_mapRenderer.Raycast(ray, out var hitInfo))
                    {
                        targetCoordinateAndAltitude = (hitInfo.Location.LatLon.ToMercatorCoordinate(), hitInfo.Location.AltitudeInMeters);
                    }
                }
            }

            if (targetCoordinateAndAltitude.HasValue)
            {
                _mapRenderer.ZoomLevel += zoomLevelDelta;

                var targetCoordinate = targetCoordinateAndAltitude.Value.Item1;
                var targetAltitude = targetCoordinateAndAltitude.Value.Item2;

                // Compute a plane that is parallel to the map's surface and passes through the target coordinate and altitude.
                var newTargetLocationInWorldSpace =
                    _mapRenderer.TransformMercatorWithAltitudeToWorldPoint(targetCoordinate, targetAltitude);
                var targetPlaneInWorldSpace = new Plane(_mapRenderer.transform.up, newTargetLocationInWorldSpace);

                // Raycast this "target plane" to determine where the ray is hitting.
                if (targetPlaneInWorldSpace.Raycast(ray, out var enter))
                {
                    var pointInWorldSpace = ray.GetPoint(enter);
                    var planeHitPointInMercatorSpace = _mapRenderer.TransformWorldPointToMercator(pointInWorldSpace);
                    var deltaInMercatorSpace = planeHitPointInMercatorSpace - targetCoordinate;
                    var recoveredCenter = _mapRenderer.Center.ToMercatorCoordinate() - deltaInMercatorSpace;
                    var newCenter = recoveredCenter.ToLatLon();
                    _mapRenderer.Center = newCenter;
                }
            }
            else if (!requireRayHitForZoom)
            {
                _mapRenderer.ZoomLevel += zoomLevelDelta;
            }
        }

        private static double FMod(double a, double b)
        {
            return a - b * Math.Floor(a / b);
        }
    }
}
