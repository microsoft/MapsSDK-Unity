// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.MixedReality.Toolkit;
using UnityEngine;

public class DeactivateOnTransparentDisplay : MonoBehaviour
{
    private void Awake()
    {
        CheckAndDeactivate();
    }

    private void Start()
    {
        CheckAndDeactivate();
    }

    private void Update()
    {
        CheckAndDeactivate();
    }

    private void CheckAndDeactivate()
    {
        var cameraSystem = CoreServices.CameraSystem;
        if (cameraSystem != null && !cameraSystem.IsOpaque)
        {
            gameObject.SetActive(false);
        }
    }
}
