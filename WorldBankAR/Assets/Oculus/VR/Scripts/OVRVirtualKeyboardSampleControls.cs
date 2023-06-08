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

using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

[RequireComponent(typeof(OVRVirtualKeyboardSampleInputHandler))]
public class OVRVirtualKeyboardSampleControls : MonoBehaviour
{
    private const float THUMBSTICK_DEADZONE = 0.2f;

    [SerializeField]
    private Button ShowButton;

    [SerializeField]
    private Button MoveButton;

    [SerializeField]
    private Button HideButton;

    [SerializeField]
    private Button ToggleInputModeButton;

    private Text ToggleInputModeText;

    [SerializeField]
    private OVRVirtualKeyboard keyboard;

    private OVRVirtualKeyboardSampleInputHandler inputHandler;

    private bool isMovingKeyboard_ = false;
    private float keyboardMoveDistance_ = 0.0f;
    private float keyboardScale_ = 1.0f;

    void Start()
    {
        inputHandler = GetComponent<OVRVirtualKeyboardSampleInputHandler>();

        ShowKeyboard();

        OVRVirtualKeyboard.Events.KeyboardHidden += OnHideKeyboard;

        ToggleInputModeButton.onClick.AddListener(ToggleInputMode);
        ToggleInputModeText = ToggleInputModeButton.gameObject.GetComponentInChildren<Text>();
    }

    private void OnDestroy()
    {
        OVRVirtualKeyboard.Events.KeyboardHidden -= OnHideKeyboard;
    }

    public void ShowKeyboard()
    {
        keyboard.gameObject.SetActive(true);
        UpdateButtonInteractable();
    }

    public void MoveKeyboard()
    {
        if (keyboard.gameObject.activeSelf)
        {
            isMovingKeyboard_ = true;
            var kbTransform = keyboard.transform;
            keyboardMoveDistance_ = (inputHandler.InputRayPosition - kbTransform.position).magnitude;
            keyboardScale_ = kbTransform.localScale.x;
            UpdateButtonInteractable();
        }
    }

    public void HideKeyboard()
    {
        keyboard.gameObject.SetActive(false);
        isMovingKeyboard_ = false;
        UpdateButtonInteractable();
    }

    private void ToggleInputMode()
    {
        keyboard.InputMode = (OVRVirtualKeyboard.KeyboardInputMode)(int)keyboard.InputMode + 1;
        if (keyboard.InputMode == OVRVirtualKeyboard.KeyboardInputMode.Max)
        {
            keyboard.InputMode = (OVRVirtualKeyboard.KeyboardInputMode)0;
        }

        keyboard.SuggestVirtualKeyboardLocationForInputMode(keyboard.InputMode);
        ToggleInputModeText.text =
            $"Input Mode: {Enum.GetName(typeof(OVRVirtualKeyboard.KeyboardInputMode), keyboard.InputMode)}";
    }

    private void OnHideKeyboard()
    {
        UpdateButtonInteractable();
    }

    private void UpdateButtonInteractable()
    {
        ShowButton.interactable = !keyboard.gameObject.activeSelf;
        MoveButton.interactable = keyboard.gameObject.activeSelf && !isMovingKeyboard_;
        HideButton.interactable = keyboard.gameObject.activeSelf && !isMovingKeyboard_;
        ToggleInputModeButton.interactable = keyboard.gameObject.activeSelf && !isMovingKeyboard_;
    }

    void Update()
    {
        if (isMovingKeyboard_)
        {
            keyboardMoveDistance_ *= 1.0f + inputHandler.AnalogStickY * 0.01f;
            keyboardMoveDistance_ = Mathf.Clamp(keyboardMoveDistance_, 0.1f, 100.0f);

            keyboardScale_ += inputHandler.AnalogStickX * 0.01f;
            keyboardScale_ = Mathf.Clamp(keyboardScale_, 0.25f, 2.0f);

            var rotation = inputHandler.InputRayRotation;
            var kbTransform = keyboard.transform;
            kbTransform.SetPositionAndRotation(
                inputHandler.InputRayPosition + keyboardMoveDistance_ * (rotation * Vector3.forward),
                rotation);
            kbTransform.localScale = Vector3.one * keyboardScale_;

            if (inputHandler.IsPressed)
            {
                isMovingKeyboard_ = false;
                UpdateButtonInteractable();
            }
        }
    }
}
