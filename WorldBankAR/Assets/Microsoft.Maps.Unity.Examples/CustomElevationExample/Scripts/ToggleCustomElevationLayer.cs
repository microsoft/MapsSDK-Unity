// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using UnityEngine;

public class ToggleCustomElevationLayer : MonoBehaviour
{
    public void Toggle()
    {
        var existingLayer = GetComponent<CustomElevationTileLayer>();
        existingLayer.enabled = !existingLayer.enabled;
    }
}
