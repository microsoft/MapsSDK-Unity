// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Geospatial;
using Microsoft.Maps.Unity;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using System.Linq;
using UnityEngine;

/// <summary>
/// Handles panning and dragging the <see cref="MapRenderer"/> via pointer rays, and zooming in and out of a selected location.
/// </summary>
[RequireComponent(typeof(MapInteractionController))]
public class MixedRealityMapInteractionHandler : MapInteractionHandler, IMixedRealityPointerHandler, IMixedRealityInputHandler<Vector2>, IMixedRealityFocusHandler
{
    private const float DoubleTapThresholdInSeconds = 1.0f;
    private const double JoystickDeadZone = 0.3;

    private IMixedRealityPointer _pointer = null;
    private Vector3 _targetPointInLocalSpace;
    private MercatorCoordinate _targetPointInMercator;
    private double _targetAltitudeInMeters;
    private Vector3 _currentPointInLocalSpace;
    private Vector3 _smoothedPointInLocalSpace;
    private Vector2 _currentZoomValue;
    private bool _isFocused = false;
    private IMixedRealityPointer _zoomPointer;
    private float _lastPointerDownTime = float.MaxValue;
    private float _lastClickTime = float.MinValue;
    private bool _isInteracting = false;

    [SerializeField]
    [Range(0, 1)]
    private float _zoomSpeed = 0.5f;

    [SerializeField]
    [Range(0, 1)]
    private float _panSmoothness = 0.5f;

    private void OnEnable()
    {
        if (CoreServices.InputSystem != null)
        {
            CoreServices.InputSystem.RegisterHandler<IMixedRealityInputHandler<Vector2>>(this);
            CoreServices.InputSystem.RegisterHandler<IMixedRealityPointerHandler>(this);
        }
    }

    private void Update()
    {
        // First case handles when the map is selected and being dragged and/or zoomed.
        if (_isInteracting &&
            _pointer != null &&
            CoreServices.InputSystem.FocusProvider.TryGetFocusDetails(_pointer, out var focusDetails) &&
            focusDetails.Object == gameObject)
        {
            // The current point the ray is targeting has been calculated in OnPointerDragged. Smooth it here.
            var panSmoothness = Mathf.Lerp(0.0f, 0.5f, _panSmoothness);
            _smoothedPointInLocalSpace = DynamicExpDecay(_smoothedPointInLocalSpace, _currentPointInLocalSpace, panSmoothness);

            // Reconstruct ray from pointer position to focus details.
            var rayTargetPoint = MapRenderer.transform.TransformPoint(_smoothedPointInLocalSpace);
            var ray = new Ray(_pointer.Position, (rayTargetPoint - _pointer.Position).normalized);
            MapInteractionController.PanAndZoom(ray, _targetPointInMercator, _targetAltitudeInMeters, ComputeZoomToApply());

            // Update starting point so that the focus point tracks with this point.
            _targetPointInLocalSpace =
                MapRenderer.TransformMercatorWithAltitudeToLocalPoint(_targetPointInMercator, _targetAltitudeInMeters);

            // Also override the FocusDetails so that the pointer ray tracks the target coordinate.
            focusDetails.Point = MapRenderer.transform.TransformPoint(_targetPointInLocalSpace);
            focusDetails.PointLocalSpace = _targetPointInLocalSpace;
            CoreServices.InputSystem.FocusProvider.TryOverrideFocusDetails(_pointer, focusDetails);

            // Reset timings used for tap-and-hold and double tap.
            _lastPointerDownTime = float.MaxValue;
            _lastClickTime = float.MinValue;
        }
        else if (_zoomPointer != null && _isFocused) // This case handles when the map is just focused, not selected, and being zoomed.
        {
            var zoomToApply = ComputeZoomToApply();
            if (zoomToApply != 0)
            {
                if (!_isInteracting)
                {
                    _isInteracting = true;
                    MapInteractionController.OnInteractionStarted?.Invoke();
                }
                var pointerRayPosition = _zoomPointer.Position;
                var pointerRayDirection = (_zoomPointer.Rotation * Vector3.forward).normalized;
                MapInteractionController.Zoom(zoomToApply, new Ray(pointerRayPosition, pointerRayDirection));

                // Reset timings used for tap-and-hold and double tap.
                _lastPointerDownTime = float.MaxValue;
                _lastClickTime = float.MinValue;
            }
            else
            {
                if (_zoomPointer !=  null && _isInteracting)
                {
                    // We were zooming last frame. End the interaction.
                    MapInteractionController.OnInteractionEnded?.Invoke();
                    _isInteracting = false;
                    _zoomPointer = null;
                }
            }
        }
        else
        {
            if (_isInteracting)
            {
                // Last frame there was interaction happening. This is the first frame where the interaction has ended.
                MapInteractionController.OnInteractionEnded?.Invoke();
                _isInteracting = false;
            }
        }

        // Check for a tap and hold.
        if (_pointer != null && (Time.time - _lastPointerDownTime) > TapAndHoldThresholdInSeconds)
        {
            MapInteractionController.OnTapAndHold?.Invoke(new LatLonAlt(_targetPointInMercator.ToLatLon(), _targetAltitudeInMeters));

            // Reset timings used for tap-and-hold and double tap.
            _lastPointerDownTime = float.MaxValue;
            _lastClickTime = float.MinValue;
        }
    }

    private void OnDisable()
    {
        if (CoreServices.InputSystem != null)
        {
            CoreServices.InputSystem.UnregisterHandler<IMixedRealityInputHandler<Vector2>>(this);
            CoreServices.InputSystem.UnregisterHandler<IMixedRealityPointerHandler>(this);
        }

        // Clear pointers.
        _pointer = null;
        _zoomPointer = null;

        // Reset timings used for tap-and-hold and double tap.
        _lastPointerDownTime = float.MaxValue;
        _lastClickTime = float.MinValue;

        if (_isInteracting)
        {
            _isInteracting = false;
            MapInteractionController.OnInteractionEnded?.Invoke();
        }
    }

    public void OnPointerClicked(MixedRealityPointerEventData eventData)
    {
        if (_pointer == eventData.Pointer &&
            CoreServices.InputSystem.FocusProvider.TryGetFocusDetails(_pointer, out var focusDetails) &&
            focusDetails.Object == gameObject)
        {
            if (!_isInteracting)
            {
                // Check for a double tap.
                if ((Time.time - _lastClickTime) < DoubleTapThresholdInSeconds)
                {
                    var targetLatLon = _targetPointInMercator.ToLatLon();

                    var newZoomLevel = MapRenderer.ZoomLevel + 1.0f;
                    newZoomLevel = Mathf.Max(MapRenderer.MinimumZoomLevel, Mathf.Min(MapRenderer.MaximumZoomLevel, newZoomLevel));
                    MapRenderer.SetMapScene(new MapSceneOfLocationAndZoomLevel(targetLatLon, newZoomLevel), MapSceneAnimationKind.Linear, 150.0f);

                    MapInteractionController.OnDoubleTap?.Invoke(new LatLonAlt(targetLatLon, _targetAltitudeInMeters));
                    _lastClickTime = float.MinValue;
                }
                else
                {
                    _lastClickTime = Time.time;
                }
            }
            else
            {
                _lastClickTime = float.MinValue;
            }

            eventData.Use();
        }
    }

    public void OnPointerDown(MixedRealityPointerEventData eventData)
    {
        if (_isFocused &&
            CoreServices.InputSystem.FocusProvider.TryGetFocusDetails(eventData.Pointer, out var focusDetails) &&
            focusDetails.Object == gameObject)
        {
            _pointer = eventData.Pointer;
            _lastPointerDownTime = Time.time;

            _targetPointInLocalSpace = focusDetails.PointLocalSpace;
            _targetPointInMercator =
                MapRenderer.TransformLocalPointToMercatorWithAltitude(
                    _targetPointInLocalSpace,
                    out _targetAltitudeInMeters,
                    out _);
            _currentPointInLocalSpace = _targetPointInLocalSpace;
            _smoothedPointInLocalSpace = _targetPointInLocalSpace;

            eventData.Use();
        }
    }

    public void OnPointerDragged(MixedRealityPointerEventData eventData)
    {
        if (_pointer == eventData.Pointer)
        {
            // Raycast an imaginary plane orignating from the updated _targetPointInLocalSpace.
            var rayPositionInMapLocalSpace = MapRenderer.transform.InverseTransformPoint(_pointer.Position);
            var rayDirectionInMapLocalSpace = MapRenderer.transform.InverseTransformDirection(_pointer.Rotation * Vector3.forward).normalized;
            var rayInMapLocalSpace = new Ray(rayPositionInMapLocalSpace, rayDirectionInMapLocalSpace.normalized);
            var hitPlaneInMapLocalSpace = new Plane(Vector3.up, _targetPointInLocalSpace);
            if (hitPlaneInMapLocalSpace.Raycast(rayInMapLocalSpace, out float enter))
            {
                _currentPointInLocalSpace = rayInMapLocalSpace.GetPoint(enter);

                // Check if an interaction has started.
                if (!_isInteracting && HasInitialPointMovedPastThreshold(_targetPointInLocalSpace, _currentPointInLocalSpace))
                {
                    _isInteracting = true;
                    MapInteractionController.OnInteractionStarted?.Invoke();
                }
            }

            eventData.Use();
        }
    }

    public void OnPointerUp(MixedRealityPointerEventData eventData)
    {
        if (_pointer == eventData.Pointer)
        {
            eventData.Use();
        }

        _pointer = null;
    }

    public void OnInputChanged(InputEventData<Vector2> eventData)
    {
        if (_isFocused && eventData.MixedRealityInputAction.Description == "Zoom Map")
        {
            _currentZoomValue = eventData.InputData;
            _zoomPointer = eventData.InputSource.Pointers.First(x => x.IsActive);
            eventData.Use();
        }
    }

    public void OnFocusEnter(FocusEventData eventData)
    {
        _isFocused = eventData.NewFocusedObject == gameObject;
    }

    public void OnFocusExit(FocusEventData eventData)
    {
        _isFocused = false;
        _zoomPointer = null;
    }

    /// <summary>
    /// Determines the amount of zoom to apply based on the current zoom input's magnitude.
    /// </summary>
    private float ComputeZoomToApply()
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
        var zoomSpeed = Mathf.Lerp(0.4f, 2.0f, _zoomSpeed);
        return zoomSpeed * directionalZoomAmount;
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

    private bool HasInitialPointMovedPastThreshold(Vector3 targetPointInLocalSpace, Vector3 currentPointInLocalSpace)
    {
        return (targetPointInLocalSpace - currentPointInLocalSpace).sqrMagnitude > 0.01f;
    }
}
