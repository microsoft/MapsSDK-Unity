// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity
{
    using Microsoft.Geospatial;
    using System;
    using UnityEngine;

    /// <summary>
    /// Handles touch-screen based interactions like pan, pinch to zoom, double tap zoom, and tap-and-hold.
    /// </summary>
    [DisallowMultipleComponent]
    public class MapTouchInteractionHandler : MapInteractionHandler
    {
        private bool _isInteracting;
        private MercatorCoordinate _interactionTargetCoordinate;
        private double _interactionTargetAltitude;

        private int _lastTouchCount;
        private float _initialTouchPointDelta;
        private Vector2 _initialTouchPoint;
        private double _initialMapDimensionInMercator;
        private float _tapAndHoldBeginTime = float.MaxValue;

        private void Update()
        {
            var touchCount = Input.touchCount;
            if (touchCount == 0)
            {
                if (_isInteracting)
                {
                    _isInteracting = false;
                    MapInteractionController.OnInteractionEnded?.Invoke();
                }

                _initialTouchPointDelta = 0.0f;
                _lastTouchCount = touchCount;

                return;
            }

            var touch0 = Input.GetTouch(0);
            var touchPoint = touch0.position;
            var touchPointDelta = 0.0f; // Used when there are two touch points (for pinch/zoom).

            // A single touch point is a pan, a double-tap, or a tap-and-hold.
            if (touchCount == 1)
            {
                // Disable zoom with two touch points.
                _initialTouchPointDelta = 0.0f;

                // Check for a double tap.
                if (touch0.phase == TouchPhase.Ended && touch0.tapCount > 1)
                {
                    // Do a double tap and early out.
                    var ray = Camera.ScreenPointToRay(touchPoint);
                    if (MapRenderer.Raycast(ray, out var hitInfo))
                    {
                        MapInteractionController.DoubleTapZoom(hitInfo.Location, 1.0f);
                    }

                    _tapAndHoldBeginTime = float.MaxValue; // Reset tap and hold.
                    _lastTouchCount = 0; // Reset interactions.

                    return;
                }

                // Check for tap and hold.
                if (_isInteracting)
                {
                    // If we're in the middle of an interaction (in this case, a pan), tap and hold doesn't apply.
                    if (_tapAndHoldBeginTime != float.MaxValue)
                    {
                        _tapAndHoldBeginTime = float.MaxValue;
                    }
                }
                else
                {
                    // Track tap and hold.
                    if (touch0.phase == TouchPhase.Began)
                    {
                        _tapAndHoldBeginTime = Time.time;
                    }
                    else if (!HasInitialPointMovedPastThreshold(touchPoint))
                    {
                        // The touch point has not moved enough to start an interaction, so we are in a stationary tap.
                        // Fire off the TapAndHold event once we exceed the hold threshold.
                        if ((Time.time - _tapAndHoldBeginTime) >= TapAndHoldThresholdInSeconds)
                        {
                            var ray = Camera.ScreenPointToRay(touchPoint);
                            if (MapRenderer.Raycast(ray, out var hitInfo))
                            {
                                MapInteractionController.OnTapAndHold?.Invoke(hitInfo.Location);

                                // Reset so we don't continually fire off TapAndHold events on subsequent frames where touch point continues
                                // to be stationary.
                                _tapAndHoldBeginTime = float.MaxValue;
                            }
                        }
                    }

                    // Reset tap and hold state if this touch has been ended or cancelled.
                    if (touch0.phase == TouchPhase.Canceled || touch0.phase == TouchPhase.Ended)
                    {
                        _tapAndHoldBeginTime = float.MaxValue;
                    }
                }
            }
            else // Touch count > 1, start tracking pinch.
            {
                // Touch delta can be calculated from first and second points.
                touchPointDelta = (Input.GetTouch(1).position - touch0.position).magnitude;

                // Touch point will be average between first and second.
                touchPoint += Input.GetTouch(1).position;
                touchPoint *= 0.5f;

                _tapAndHoldBeginTime = float.MinValue; // Reset tap and hold.
            }

            // In case a second touch point is added or removed, or more generally, if this is the first frame of any touch interaction,
            // we'll need to reset the target coordiante... This involves raycasting the map to see where the touch initally hits.
            var resetInteractionTarget = _lastTouchCount != touchCount;
            if (resetInteractionTarget)
            {
                var ray = Camera.ScreenPointToRay(touchPoint);
                if (MapRenderer.Raycast(ray, out var hitInfo))
                {
                    // We have a hit, so set up some initial variables.
                    _interactionTargetCoordinate = hitInfo.Location.LatLon.ToMercatorCoordinate();
                    _interactionTargetAltitude = hitInfo.Location.AltitudeInMeters;
                    _initialTouchPointDelta = 0.0f;
                    _initialTouchPoint = touchPoint;
                    _lastTouchCount = touchCount;
                }
                else
                {
                    // The touch point didn't hit the map, so end any active interactions.
                    if (_isInteracting)
                    {
                        _lastTouchCount = 0;
                        _isInteracting = false;
                        MapInteractionController.OnInteractionEnded?.Invoke();
                    }
                }
            }
            else
            {
                // If we've made it to this case, the touch point has hit the map and we're either waiting for a movement
                // to occur which exceeds the touch interaction thresholds, or a movement is actively happening and we
                // need to update the map's state (i.e., the center and zoom properties).

                var touchPointDeltaToInitialDeltaRatio = touchPointDelta / _initialTouchPointDelta;

                if (!_isInteracting)
                {
                    // Check if the initial interaction touch points have moved past a threshold to consider this a pan or zoom.
                    if (touchPointDeltaToInitialDeltaRatio > 1.05 || HasInitialPointMovedPastThreshold(touchPoint))
                    {
                        _isInteracting = true;
                        MapInteractionController.OnInteractionStarted?.Invoke();
                    }
                }

                if (_isInteracting)
                {
                    // We're inside an intersection, handle pinch/zoom first.
                    if (touchPointDelta > 0.0f)
                    {
                        var isInitialZoomFrame = _initialTouchPointDelta == 0.0;
                        if (isInitialZoomFrame)
                        {
                            _initialTouchPointDelta = touchPointDelta;
                            _initialMapDimensionInMercator = Mathf.Pow(2, MapRenderer.ZoomLevel - 1);
                        }
                        else
                        {
                            var newMapDimensionInMercator = touchPointDeltaToInitialDeltaRatio * _initialMapDimensionInMercator;
                            var newZoomLevel = Math.Log(newMapDimensionInMercator) / Math.Log(2) + 1;
                            MapRenderer.ZoomLevel = Mathf.Clamp((float)newZoomLevel, MapRenderer.MinimumZoomLevel, MapRenderer.MaximumZoomLevel);
                        }
                    }

                    // Handle panning last. Zoom is handled above in the pinch case, and it should be updated prior to panning.
                    {
                        var ray = Camera.ScreenPointToRay(touchPoint);
                        MapInteractionController.PanAndZoom(ray, _interactionTargetCoordinate, _interactionTargetAltitude, 0.0f);
                    }
                }
            }
        }

        private void OnDisable()
        {
            if (_isInteracting)
            {
                _isInteracting = false;
                MapInteractionController.OnInteractionEnded?.Invoke();
            }

            _tapAndHoldBeginTime = float.MaxValue; // Reset tap and hold.
            _lastTouchCount = 0; // Reset interactions.
        }

        private bool HasInitialPointMovedPastThreshold(Vector2 touchPoint)
        {
            return (_initialTouchPoint - touchPoint).magnitude > 5 /* logic px */ * DpiScale;
        }
    }
}
