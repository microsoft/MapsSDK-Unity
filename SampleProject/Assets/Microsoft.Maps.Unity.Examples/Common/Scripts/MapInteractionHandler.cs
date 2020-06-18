// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Geospatial;
using Microsoft.Maps.Unity;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Physics;
using System.Linq;
using UnityEngine;

/// <summary>
/// Handles panning and dragging the <see cref="MapRenderer"/> via pointer rays, and zooming in and out of a selected location.
/// </summary>
[RequireComponent(typeof(MapInteractionController))]
public class MapInteractionHandler : MonoBehaviour, IMixedRealityPointerHandler, IMixedRealityInputHandler<Vector2>, IMixedRealityFocusHandler
{
    private const double JoystickDeadZone = 0.3;

    private MapRenderer _mapRenderer;
    private MapInteractionController _mapInteractionController;
    private IMixedRealityPointer _pointer = null;
    private Vector3 _targetPointInLocalSpace;
    private MercatorCoordinate _targetPointInMercator;
    private double _startingAltitudeInMeters;
    private Vector3 _currentPointInLocalSpace;
    private Vector2 _currentZoomValue;
    private bool _isFocused = false;
    private IMixedRealityPointer _zoomPointer;

    [SerializeField]
    [Range(0, 1)]
    private float _zoomSpeed = 0.5f;

    [SerializeField]
    [Range(0, 1)]
    private float _panSmoothness = 0.5f;

    private void Awake()
    {
        _mapRenderer = GetComponent<MapRenderer>();
        _mapInteractionController = GetComponent<MapInteractionController>();
    }

    private void OnEnable()
    {
        if (CoreServices.InputSystem != null)
        {
            CoreServices.InputSystem.RegisterHandler<IMixedRealityInputHandler<Vector2>>(this);
            CoreServices.InputSystem.RegisterHandler<IMixedRealityPointerHandler>(this);
        }

        _mapRenderer = GetComponent<MapRenderer>();
        _mapInteractionController = GetComponent<MapInteractionController>();
    }

    private void Update()
    {
        // First case handles when the map is selected and being dragged and/or zoomed.
        var isPanning = _pointer != null;
        if (isPanning &&
            CoreServices.InputSystem.FocusProvider.TryGetFocusDetails(_pointer, out FocusDetails focusDetails))
        {
            // Now we can raycast an imaginary plane orignating from the updated _targetPointInLocalSpace.
            var rayPositionInMapLocalSpace = _mapRenderer.transform.InverseTransformPoint(_pointer.Position);
            var rayDirectionInMapLocalSpace = _mapRenderer.transform.InverseTransformDirection(_pointer.Rotation * Vector3.forward).normalized;
            var rayInMapLocalSpace = new Ray(rayPositionInMapLocalSpace, rayDirectionInMapLocalSpace.normalized);
            var hitPlaneInMapLocalSpace = new Plane(Vector3.up, _targetPointInLocalSpace);
            if (hitPlaneInMapLocalSpace.Raycast(rayInMapLocalSpace, out float enter))
            {
                // This point will be used to determine how much to translate the map.
                // Decaying the resulting position applies some smoothing to the input.
                var panSmoothness = Mathf.Lerp(0.0f, 0.5f, _panSmoothness);
                _currentPointInLocalSpace =
                    DynamicExpDecay(_currentPointInLocalSpace, rayInMapLocalSpace.GetPoint(enter), panSmoothness);
            }

            // Reconstruct ray from pointer position to focus details.
            var rayTargetPoint = _mapRenderer.transform.TransformPoint(_currentPointInLocalSpace);
            var ray = new Ray(_pointer.Position, (rayTargetPoint - _pointer.Position).normalized);
            var zoomToApply = ComputeZoomToApply();
            _mapInteractionController.PanAndZoom(ray, _targetPointInMercator, _startingAltitudeInMeters, zoomToApply);

            // Update starting point so that the focus point tracks with this point.
            _targetPointInLocalSpace =
                _mapRenderer.TransformMercatorWithAltitudeToLocalPoint(_targetPointInMercator, _startingAltitudeInMeters);

            // Also override the FocusDetails so that the pointer ray tracks the target coordinate.
            focusDetails.Point = _mapRenderer.transform.TransformPoint(_targetPointInLocalSpace);
            focusDetails.PointLocalSpace = _targetPointInLocalSpace;
            CoreServices.InputSystem.FocusProvider.TryOverrideFocusDetails(_pointer, focusDetails);
        }
        else if (_zoomPointer != null && _isFocused) // This case handles when the map is just focused, not selected, and being zoomed.
        {
            var zoomToApply = ComputeZoomToApply();
            if (zoomToApply != 0)
            {
                var pointerRayPosition = _zoomPointer.Position;
                var pointerRayDirection = (_zoomPointer.Rotation * Vector3.forward).normalized;
                _mapInteractionController.Zoom(zoomToApply, new Ray(pointerRayPosition, pointerRayDirection));
            }
        }
    }

    private void OnDisable()
    {
        if (CoreServices.InputSystem != null)
        {
            CoreServices.InputSystem.UnregisterHandler<IMixedRealityInputHandler<Vector2>>(this);
            CoreServices.InputSystem.UnregisterHandler<IMixedRealityPointerHandler>(this);
        }
    }

    public void OnPointerClicked(MixedRealityPointerEventData eventData)
    {
    }

    public void OnPointerDown(MixedRealityPointerEventData eventData)
    {
        if (_isFocused &&
            CoreServices.InputSystem.FocusProvider.TryGetFocusDetails(eventData.Pointer, out FocusDetails focusDetails))
        {
            _pointer = eventData.Pointer;
            _targetPointInLocalSpace = focusDetails.PointLocalSpace;
            _targetPointInMercator =
                _mapRenderer.TransformLocalPointToMercatorWithAltitude(
                    _targetPointInLocalSpace,
                    out _startingAltitudeInMeters,
                    out _);
            _currentPointInLocalSpace = _targetPointInLocalSpace;

            eventData.Use();
        }
    }

    public void OnPointerDragged(MixedRealityPointerEventData eventData)
    {
        if (_pointer == eventData.Pointer)
        {
            eventData.Use();
        }
    }

    public void OnPointerUp(MixedRealityPointerEventData eventData)
    {
        _pointer = null;
    }

    public void OnInputChanged(InputEventData<Vector2> eventData)
    {
        if (eventData.MixedRealityInputAction.Description == "Zoom Map")
        {
            _currentZoomValue = eventData.InputData;
            _zoomPointer = eventData.InputSource.Pointers.First(x => x.IsActive);
            eventData.Use();
        }
    }

    public void OnFocusEnter(FocusEventData eventData)
    {
        _isFocused = true;
    }

    public void OnFocusExit(FocusEventData eventData)
    {
        _isFocused = false;
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
}
