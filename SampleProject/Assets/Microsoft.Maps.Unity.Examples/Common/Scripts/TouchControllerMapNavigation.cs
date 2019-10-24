// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using UnityEngine;
#if UNITY_WSA
using UnityEngine.XR.WSA.Input;
    using Microsoft.Maps.Unity;
#endif

public class TouchControllerMapNavigation : MonoBehaviour
{
#if UNITY_WSA
    private bool _rotated = false;
    private const double TouchpadDeadzone = 0.3;
    private const double RotationThreshold = 0.7;

    private MapNavigation _mapNavigation;

    private void Start()
    {
        _mapNavigation = GetComponent<MapNavigation>();
        Debug.Assert(_mapNavigation != null);

        InteractionManager.InteractionSourceUpdated += InteractionManager_InteractionSourceUpdated;
    }

    private void InteractionManager_InteractionSourceUpdated(InteractionSourceUpdatedEventArgs obj)
    {
        if (obj.state.source.handedness.Equals(InteractionSourceHandedness.Right))
        {
            // Handle panning from touchpad.
            if (obj.state.touchpadPosition.magnitude > TouchpadDeadzone && obj.state.touchpadPressed)
            {
                _mapNavigation.Pan(obj.state.touchpadPosition, true);
            }
        }

        if (obj.state.source.handedness.Equals(InteractionSourceHandedness.Left))
        {
            var magnitude = obj.state.touchpadPosition.magnitude;
            if (magnitude > TouchpadDeadzone && obj.state.touchpadPressed)
            {
                const float halfValidPressRangeInDegrees = 0.5f * 60; // Should be less than 45 degrees to not overlap.

                var normalizedTouchpadPosition = obj.state.touchpadPosition.normalized;
                var angle = Mathf.Rad2Deg * Mathf.Atan2(normalizedTouchpadPosition.y, normalizedTouchpadPosition.x);
                //Debug.Log(angle + ": " + normalizedTouchpadPosition.x + ", " + normalizedTouchpadPosition.y);

                // Handle continuous zooming from touchpad up/down.
                if (Mathf.Abs(90 - angle) < halfValidPressRangeInDegrees)
                {
                    _mapNavigation.Zoom(1);
                }
                else if (Mathf.Abs(-90 - angle) < halfValidPressRangeInDegrees)
                {
                    _mapNavigation.Zoom(-1);
                }

                // Handle rotation from touchpad left/right.
                else if (Mathf.Abs(angle) < halfValidPressRangeInDegrees && magnitude > RotationThreshold)
                {
                    if (!_rotated)
                    {
                        _mapNavigation.RotateMap(true);
                        _rotated = true;
                    }
                }
                else if (
                    (Mathf.Abs(180 - angle) < halfValidPressRangeInDegrees || Mathf.Abs(-180 - angle) < halfValidPressRangeInDegrees) &&
                    magnitude > RotationThreshold)
                {
                    if (!_rotated)
                    {
                        _mapNavigation.RotateMap(false);
                        _rotated = true;
                    }
                }
                else
                {
                    _rotated = false;
                }
            }
            else
            {
                _rotated = false;
            }
        }
    }
#endif
}
