// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Geospatial;
using Microsoft.Geospatial.VectorMath;
using Microsoft.Maps.Unity;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Physics;
using System;
using UnityEngine;

/// <summary>
/// Handles panning and dragging the map via controller rays. Also handles zooming in and out of selected location.
/// </summary>
[RequireComponent(typeof(MapRenderer))]
public class MapInteractionHandler : MonoBehaviour, IMixedRealityPointerHandler, IMixedRealityInputHandler<Vector2>
{
    private const double JoystickDeadZone = 0.3;

    private MapRenderer _mapRenderer;
    private IMixedRealityPointer _panningPointer = null;
    private Vector3 _startingPointInLocalSpace;
    private Vector2D _startingPointInMercatorSpace;
    private double _startingMercatorScale;
    private double _startingAltitudeInMeters;
    private Vector3 _currentPointInLocalSpace;
    private Vector2D _startingMapCenterInMercator;
    private Vector2 _currentZoomValue;

    [SerializeField]
    [Range(0, 1)]
    private float _zoomSpeed = 0.5f;

    [SerializeField]
    [Range(0, 1)]
    private float _panSmoothness = 0.5f;

    private void Awake()
    {
        _mapRenderer = GetComponent<MapRenderer>();
    }

    public void OnPointerClicked(MixedRealityPointerEventData eventData)
    {
    }

    public void OnPointerDown(MixedRealityPointerEventData eventData)
    {
        if (CoreServices.InputSystem.FocusProvider.TryGetFocusDetails(eventData.Pointer, out FocusDetails focusDetails) &&
            focusDetails.Object == gameObject)
        {
            _panningPointer = eventData.Pointer;
            _startingPointInLocalSpace = focusDetails.PointLocalSpace;
            _startingPointInMercatorSpace =
                _mapRenderer.TransformLocalPointToMercatorWithAltitude(
                    _startingPointInLocalSpace,
                    out _startingAltitudeInMeters,
                    out _startingMercatorScale);
            _currentPointInLocalSpace = _startingPointInLocalSpace;
            _startingMapCenterInMercator = _mapRenderer.Center.ToMercatorPosition();

            eventData.Use();
        }
    }

    public void OnPointerDragged(MixedRealityPointerEventData eventData)
    {
        if (_panningPointer == eventData.Pointer)
        {
            eventData.Use();
        }
    }

    public void OnPointerUp(MixedRealityPointerEventData eventData)
    {
        _panningPointer = null;
    }

    public void OnInputChanged(InputEventData<Vector2> eventData)
    {
        if (eventData.MixedRealityInputAction.Description == "Zoom Map")
        {
            _currentZoomValue = eventData.InputData;
            eventData.Use();
        }
    }

    void Update()
    {
        var isInteractingWithMap = _panningPointer != null;
        if (isInteractingWithMap &&
            CoreServices.InputSystem.FocusProvider.TryGetFocusDetails(_panningPointer, out FocusDetails focusDetails))
        {
            // Determine amount to zoom in or out.
            var directionalZoomAmount = 0.0f;
            var zoomMagnitude = _currentZoomValue.magnitude;
            if (zoomMagnitude > JoystickDeadZone)
            {
                var angle = Mathf.Rad2Deg * Mathf.Abs(Mathf.Atan2(_currentZoomValue.y, _currentZoomValue.x));
                if (Mathf.Abs(90.0f - angle) < 75)
                {
                    directionalZoomAmount = _currentZoomValue.y;
                }
            }

            // Compute more terms if there is any zoom to apply.
            var zoomRatio = 1.0;
            var zoomLevelToUseForInteraction = _mapRenderer.ZoomLevel;
            if (directionalZoomAmount != 0)
            {
                var zoomSpeed = Mathf.Lerp(0.01f, 0.045f, _zoomSpeed);
                var zoomToApply = zoomSpeed * directionalZoomAmount;
                var oldZoomLevel = _mapRenderer.ZoomLevel;
                var newZoomLevel =
                    Math.Min(
                        _mapRenderer.MaximumZoomLevel,
                        Math.Max(_mapRenderer.MinimumZoomLevel, oldZoomLevel + zoomToApply));
                zoomRatio = Math.Pow(2, oldZoomLevel - 1) / Math.Pow(2, newZoomLevel - 1);
                zoomLevelToUseForInteraction = newZoomLevel;
            }

            // The _startingPointInLocalSpace can be updated now as zoom changed so it's altitude may change as well.
            // A future improvement to make here is to actually requery the altitude of the _startingPointInMercatorSpace,
            // as this altitude can also change based on the level of detail being shown.
            var offsetAltitudeInMeters = _startingAltitudeInMeters - _mapRenderer.ElevationBaseline;
            var equatorialCircumferenceInLocalSpace = Math.Pow(2, zoomLevelToUseForInteraction - 1);
            var altitudeInLocalSpace =
                offsetAltitudeInMeters *
                _startingMercatorScale *
                (equatorialCircumferenceInLocalSpace / MapRendererTransformExtensions.EquatorialCircumferenceInWgs84Meters);
            _startingPointInLocalSpace.y = (float)(_mapRenderer.LocalMapHeight + altitudeInLocalSpace);

            // Now we can raycast an imaginary plane orignating from the updated _startingPointInLocalSpace.
            var rayPositionInMapLocalSpace = _mapRenderer.transform.InverseTransformPoint(_panningPointer.Position);
            var rayDirectionInMapLocalSpace =_mapRenderer.transform.InverseTransformDirection(_panningPointer.Rotation * Vector3.forward).normalized;
            var rayInMapLocalSpace = new Ray(rayPositionInMapLocalSpace, rayDirectionInMapLocalSpace.normalized);
            var hitPlaneInMapLocalSpace = new Plane(Vector3.up, _startingPointInLocalSpace);
            if (hitPlaneInMapLocalSpace.Raycast(rayInMapLocalSpace, out float enter))
            {
                // This point will be used to determine how much to translate the map.
                // Decaying the resulting position applies some smoothing to the input.
                var panSmoothness = Mathf.Lerp(0.0f, 0.5f, _panSmoothness);
                _currentPointInLocalSpace =
                    DynamicExpDecay(_currentPointInLocalSpace, rayInMapLocalSpace.GetPoint(enter), panSmoothness);

                // Also override the FocusDetails so that the pointer ray tracks with the map.
                // Otherwise, it would remain fixed in world space.
                focusDetails.Point = _mapRenderer.transform.TransformPoint(_currentPointInLocalSpace);
                focusDetails.PointLocalSpace = _currentPointInLocalSpace;
                CoreServices.InputSystem.FocusProvider.TryOverrideFocusDetails(_panningPointer, focusDetails);
            }

            // Apply zoom now, if needed.
            if (directionalZoomAmount != 0)
            {
                _mapRenderer.ZoomLevel = zoomLevelToUseForInteraction;
                var deltaToCenterInMercatorSpace = _startingPointInMercatorSpace - _startingMapCenterInMercator;
                var adjustedDeltaToCenterInMercatorSpace = zoomRatio * deltaToCenterInMercatorSpace;
                _startingMapCenterInMercator = _startingPointInMercatorSpace - adjustedDeltaToCenterInMercatorSpace;
            }

            // Apply pan translation.
            var deltaInLocalSpace = _currentPointInLocalSpace - _startingPointInLocalSpace;
            var deltaInMercatorSpace = MapRendererTransformExtensions.TransformLocalDirectionToMercator(deltaInLocalSpace, zoomLevelToUseForInteraction);
            var newCenterInMercator = _startingMapCenterInMercator - deltaInMercatorSpace;
            newCenterInMercator.Y = Math.Max(Math.Min(0.5, newCenterInMercator.Y), -0.5);

            _mapRenderer.Center = new LatLon(newCenterInMercator);
        }
    }

    private static Vector3 DynamicExpDecay(Vector3 from, Vector3 to, float halfLife)
    {
        return Vector3.Lerp(from, to, DynamicExpCoefficient(halfLife, Vector3.Distance(to, from)));
    }

    private static float DynamicExpCoefficient(float halfLife, float delta)
    {
        if (halfLife == 0)
        {
            return 1;
        }

        return 1.0f - Mathf.Pow(0.5f, delta / halfLife);
    }
}
