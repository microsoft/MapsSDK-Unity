using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Implements a zoom logic that will zoom the map (map tiles) based on the 
/// ratio of the distance between screen touches.
/// object_scale = start_object_scale * curr_hand_dist / start_hand_dist
/// 
/// Usage:
/// When a manipulation starts, call Setup.
/// </summary>
public class MapZoomPinchLogic
{
    private float _startMapZoomFactor;
    private float _startTouchDelta = 1;

    /// <summary>
    /// Initialize system with source info from controllers/hands
    /// </summary>
    /// <param name="handsPressedArray">Array with positions of down pointers</param>
    /// <param name="manipulationRoot">Transform of gameObject to be manipulated</param>
    public virtual void Setup(Vector3[] handsPressedArray, float mapZoom)
    {
        _startTouchDelta = GetDistanceBetweenTouches(handsPressedArray);
        _startMapZoomFactor = mapZoom;
    }

    /// <summary>
    /// update map with new Scale 
    /// </summary>
    /// <param name="touchesArray">Array with positions of down pointers, order should be the same as handsPressedArray provided in Setup</param>
    /// <returns>a float zoom factor</returns>
    public virtual float GetZoomFactor(Vector3[] touchesArray)
    {
        var delta = GetDistanceBetweenTouches(touchesArray);
        var multiplier = delta / _startTouchDelta;
        multiplier = Mathf.Clamp(_startMapZoomFactor * multiplier, 1f, 8f);

        if (delta < _startTouchDelta)
            multiplier *= -1;
        _startTouchDelta = delta;
        return multiplier;
    }


    private float GetDistanceBetweenTouches(Vector3[] touchesArray)
    {
        return Vector3.Distance(touchesArray[0], touchesArray[1]);
    }
}
