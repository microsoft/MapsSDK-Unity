// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity
{
    using Microsoft.Geospatial;
    using UnityEngine;
    using UnityEngine.EventSystems;

    /// <summary>
    /// Handles mouse and scroll wheel based interactions for pan, zoom, double tap zoom, and tap-and-hold.
    /// </summary>
    [DisallowMultipleComponent]
    public class MapMouseInteractionHandler : MapInteractionHandler
    {
        private bool _isMouseDown;
        private bool _isInteracting;
        private MercatorCoordinate _interactionTargetCoordinate;
        private double _interactionTargetAltitude;
        private Vector2 _initialMousePosition;
        private float _lastMouseDownTime = float.MaxValue;
        private float _lastMouseUpTime = float.MinValue;

        [SerializeField]
        private float _zoomSpeed = 5.0f;

        private void Update()
        {
            var mousePosition = Input.mousePosition;

            var zoomSpeed = 0.0f;
            if (IsPixelOverCamera(mousePosition) && !IsPointerOverGameObject())
            {
                zoomSpeed = 10 * _zoomSpeed * Input.GetAxis("Mouse ScrollWheel");
            }

            if (_isMouseDown && (_isInteracting || zoomSpeed > 0 || HasInitialPointMovedPastThreshold(mousePosition)))
            {
                // If this is the first frame of the interaction, invoke the interaction started event.
                if (!_isInteracting)
                {
                    _isInteracting = true;
                    MapInteractionController.OnInteractionStarted?.Invoke();
                }

                var ray = Camera.ScreenPointToRay(mousePosition);
                MapInteractionController.PanAndZoom(ray, _interactionTargetCoordinate, _interactionTargetAltitude, zoomSpeed);
            }
            else
            {
                // Zoom may still occur via the scroll wheel even if the mouse isn't down.
                if (zoomSpeed != 0)
                {
                    // If this is the first frame of the interaction, invoke the interaction started event.
                    if (!_isInteracting)
                    {
                        _isInteracting = true;
                        MapInteractionController.OnInteractionStarted?.Invoke();
                    }

                    var ray = Camera.ScreenPointToRay(mousePosition);
                    MapInteractionController.Zoom(zoomSpeed, ray, true);
                }
                else
                {
                    // There is no zoom interaction, so if this is the first frame of no interaction happening, invoke the interaction ended event.
                    if (_isInteracting)
                    {
                        _isInteracting = false;
                        MapInteractionController.OnInteractionEnded?.Invoke();
                    }
                }

                // If an interaction (i.e. a scroll wheel zoom) isn't occurring, check for tap and hold.
                if (!_isInteracting)
                {
                    if ((Time.time - _lastMouseDownTime) > TapAndHoldThresholdInSeconds)
                    {
                        var ray = Camera.ScreenPointToRay(mousePosition);
                        if (MapRenderer.Raycast(ray, out var hitInfo))
                        {
                            MapInteractionController.OnTapAndHold.Invoke(hitInfo.Location);
                            _lastMouseDownTime = float.MaxValue;
                        }
                    }
                }
            }

            if (_isInteracting)
            {
                // If we were panning or zooming the map, reset the last mouse up/down times used to track double tap and tap and hold.
                _lastMouseDownTime = float.MaxValue;
                _lastMouseUpTime = float.MinValue;
            }
        }

        private void OnMouseDown()
        {
            if (!isActiveAndEnabled || IsPointerOverGameObject())
            {
                return;
            }

            var mousePosition = Input.mousePosition;
            if (IsPixelOverCamera(mousePosition))
            {
                var ray = Camera.ScreenPointToRay(mousePosition);
                if (MapRenderer.Raycast(ray, out var hitInfo))
                {
                    _isMouseDown = true;
                    _interactionTargetCoordinate = hitInfo.Location.LatLon.ToMercatorCoordinate();
                    _interactionTargetAltitude = hitInfo.Location.AltitudeInMeters;
                    _initialMousePosition = mousePosition;
                    _lastMouseDownTime = Time.time;
                }
            }
        }

        private void OnMouseUp()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            var hasTapAndHoldOccured = _lastMouseDownTime == float.MaxValue;
            if (hasTapAndHoldOccured || _isInteracting)
            {
                // If we were panning or zooming the map prior to this or had already reached the tap and hold threshold,
                // reset the last mouse up time.
                _lastMouseUpTime = float.MinValue;
            }
            else
            {
                if ((Time.time - _lastMouseUpTime) < 0.5f)
                {
                    // Double click.
                    var mousePosition = Input.mousePosition;
                    var ray = Camera.ScreenPointToRay(mousePosition);
                    if (MapRenderer.Raycast(ray, out var hitInfo))
                    {
                        MapInteractionController.DoubleTapZoom(hitInfo.Location, 1.0f);
                    }
                }

                _lastMouseUpTime = Time.time;
            }

            _lastMouseDownTime = float.MaxValue;
            _isMouseDown = false;
        }

        private void OnDisable()
        {
            if (_isInteracting)
            {
                _isInteracting = false;
                MapInteractionController.OnInteractionEnded?.Invoke();
            }

            // Reset interactions.
            _isMouseDown = false;
            _lastMouseDownTime = float.MaxValue;
            _lastMouseUpTime = float.MinValue;
        }

        private bool IsPixelOverCamera(Vector2 position)
        {
            return Camera.pixelRect.Contains(position);
        }

        private static bool IsPointerOverGameObject()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private bool HasInitialPointMovedPastThreshold(Vector2 currentInteractionPoint)
        {
            return (_initialMousePosition - currentInteractionPoint).magnitude > 5 /* logical px */ * DpiScale;
        }
    }
}
