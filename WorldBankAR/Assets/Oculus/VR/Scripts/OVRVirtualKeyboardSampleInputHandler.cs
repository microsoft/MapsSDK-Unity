/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using UnityEngine;
using UnityEngine.EventSystems;

public class OVRVirtualKeyboardSampleInputHandler : MonoBehaviour
{
    private const float RAY_HIDE_FRONT_DISTANCE = 0.2f;
    private const float RAY_HIDE_BEHIND_DISTANCE = 0.15f;
    private const float THUMBSTICK_DEADZONE = 0.2f;

    public bool IsPressed =>
        OVRInput.Get(
            OVRInput.Button.One | // right hand pinch
            OVRInput.Button.Three | // left hand pinch
            OVRInput.Button.PrimaryIndexTrigger |
            OVRInput.Button.SecondaryIndexTrigger,
            OVRInput.Controller.All);

    private static float ApplyDeadzone(float value)
    {
        if (value > THUMBSTICK_DEADZONE)
            return (value - THUMBSTICK_DEADZONE) / (1.0f - THUMBSTICK_DEADZONE);
        else if (value < -THUMBSTICK_DEADZONE)
            return (value + THUMBSTICK_DEADZONE) / (1.0f - THUMBSTICK_DEADZONE);
        return 0.0f;
    }

    public float AnalogStickX => ApplyDeadzone(OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).x +
                                               OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).x);

    public float AnalogStickY => ApplyDeadzone(OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).y +
                                               OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).y);

    public Vector3 InputRayPosition => inputModule.rayTransform.position;

    public Quaternion InputRayRotation =>
        interactionDevice_ == OVRInput.Controller.LHand
            ? inputModule.rayTransform.rotation * Quaternion.Euler(Vector3.forward * 180)
            : // Flip input rotation if left hand input
            inputModule.rayTransform.rotation;

    private Transform InputTransform;

    public OVRVirtualKeyboard OVRVirtualKeyboard;

    [SerializeField]
    private OVRHand leftHand;

    [SerializeField]
    private OVRHand rightHand;

    [SerializeField]
    private OVRRaycaster raycaster;

    [SerializeField]
    private OVRInputModule inputModule;

    [SerializeField]
    private LineRenderer leftLinePointer;

    [SerializeField]
    private LineRenderer rightLinePointer;

    private OVRInput.Controller? interactionDevice_;

    private void Update()
    {
        UpdateInteractionAnchor();
        UpdateLineRenderer();
    }

    private void UpdateLineRenderer()
    {
        leftLinePointer.enabled = false;
        rightLinePointer.enabled = false;

        foreach (var inputHandler in OVRVirtualKeyboard.InputHandlers)
        {
            if (!inputHandler.PositionValid)
            {
                continue;
            }

            LineRenderer linePointer =
                inputHandler.InteractionDevice == OVRInput.Controller.LHand ||
                inputHandler.InteractionDevice == OVRInput.Controller.LTouch
                    ? leftLinePointer
                    : rightLinePointer;

            linePointer.enabled = true;
            // Pull back ray origin a bit so when finger touches keys
            // it won't show the laser again
            Vector3 rayOrigin = inputHandler.InputPosePosition +
                                RAY_HIDE_BEHIND_DISTANCE * (inputHandler.InputPoseRotation * Vector3.back);
            var ray = new Ray(rayOrigin, inputHandler.InputPoseRotation * Vector3.forward);
            linePointer.SetPosition(0, inputHandler.InputPosePosition);

            if (Physics.Raycast(ray, out var hit, 100.0f))
            {
                // If the ray hits the keyboard Collider and you are close enough for
                // direct input, don't show the ray
                if (OVRVirtualKeyboard.InputMode == OVRVirtualKeyboard.KeyboardInputMode.Direct
                    && hit.collider == OVRVirtualKeyboard.Collider)
                {
                    linePointer.enabled = false;
                }

                linePointer.SetPosition(1, hit.point);
            }
            else
            {
                linePointer.SetPosition(1,
                    inputHandler.InputPosePosition + ray.direction * 2.5f);
            }
        }
    }

    private void UpdateInteractionAnchor()
    {
        // Determine currently active device

        foreach (var inputHandler in OVRVirtualKeyboard.InputHandlers)
        {
            if (inputHandler.PositionValid)
            {
                if (interactionDevice_ == null || inputHandler.IsPressed)
                {
                    interactionDevice_ = inputHandler.InteractionDevice;
                }
            }
            else if (interactionDevice_ == inputHandler.InteractionDevice)
            {
                interactionDevice_ = null;
            }
        }

        // Set transforms for Unity UI interaction

        raycaster.pointer = (interactionDevice_ == OVRInput.Controller.LHand)
            ? leftHand.gameObject
            : rightHand.gameObject;

        switch (interactionDevice_)
        {
            case OVRInput.Controller.LHand:
                inputModule.rayTransform = leftHand.PointerPose;
                break;
            case OVRInput.Controller.LTouch:
                inputModule.rayTransform = leftHand.transform;
                break;
            case OVRInput.Controller.RHand:
                inputModule.rayTransform = rightHand.PointerPose;
                break;
            case OVRInput.Controller.RTouch:
            default:
                inputModule.rayTransform = rightHand.transform;
                break;
        }
    }
}
