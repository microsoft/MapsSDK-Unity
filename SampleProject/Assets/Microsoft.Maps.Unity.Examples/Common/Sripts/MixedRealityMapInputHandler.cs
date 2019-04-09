using Microsoft.Maps.Unity;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;

public class MixedRealityMapInputHandler : MonoBehaviour, IMixedRealityInputHandler, IMixedRealityInputHandler<Vector2>
{
    private const double JoystickDeadZone = 0.3;

    [SerializeField]
    private MapNavigation _mapNavigation = null;

    private Vector2 _currentPanValue;
    private Vector2 _currentRotateZoomValue;
    private bool _isRotateZoomButtonDown;
    private bool _isRotateZoomButtonDownThisFrame;

    public void Start()
    {
        if (MixedRealityToolkit.IsInitialized && MixedRealityToolkit.InputSystem != null)
        {
            MixedRealityToolkit.InputSystem.Register(gameObject);
        }
    }

    private void Update()
    {
        // Pan.
        if (_currentPanValue.magnitude > JoystickDeadZone)
        {
            _mapNavigation.Pan(_currentPanValue, true);
        }

        // Zoom and Rotate - share the same touchpad button.
        var rotateZoomMagnitude = _currentRotateZoomValue.magnitude;
        if (rotateZoomMagnitude > JoystickDeadZone)
        {
            var angle = Mathf.Rad2Deg * Mathf.Abs(Mathf.Atan2(_currentRotateZoomValue.y, _currentRotateZoomValue.x));

            // Zoom.
            if (_isRotateZoomButtonDown)
            {
                if ((angle > 45 && angle < 135))
                {
                    _mapNavigation.Zoom(2.0f * Mathf.Sign(_currentRotateZoomValue.y)); // Need to hookup Gaze.
                }
            }

            // Rotate.
            if (_isRotateZoomButtonDownThisFrame && rotateZoomMagnitude > 0.5f)
            {
                if (angle > 145 && angle < 215)
                {
                    _mapNavigation.RotateMap(true);
                }
                else if (angle < 35)
                {
                    _mapNavigation.RotateMap(false);
                }
            }
        }

        // This bool should only be true for the first frame which the rotate/zoom button was pressed.
        _isRotateZoomButtonDownThisFrame = false;
    }

    public void OnInputChanged(InputEventData<Vector2> eventData)
    {
        if (eventData.MixedRealityInputAction.Description == "Pan Map")
        {
            _currentPanValue = eventData.InputData;
            eventData.Use();
        }
        else if (eventData.MixedRealityInputAction.Description == "Rotate Zoom Map")
        {
            _currentRotateZoomValue = eventData.InputData;
            eventData.Use();
        }
    }

    public void OnInputUp(InputEventData eventData)
    {
        if (eventData.MixedRealityInputAction.Description == "Rotate Zoom Pressed")
        {
            _isRotateZoomButtonDownThisFrame = false;
            _isRotateZoomButtonDown = false;
            eventData.Use();
        }
    }

    public void OnInputDown(InputEventData eventData)
    {
        if (eventData.MixedRealityInputAction.Description == "Rotate Zoom Pressed")
        {
            _isRotateZoomButtonDownThisFrame = true;
            _isRotateZoomButtonDown = true;
            eventData.Use();
        }
    }

    // Unused interface implementations...

    public void OnInputPressed(InputEventData<float> eventData) { }

    public void OnPositionInputChanged(InputEventData<Vector2> eventData) { }
}
