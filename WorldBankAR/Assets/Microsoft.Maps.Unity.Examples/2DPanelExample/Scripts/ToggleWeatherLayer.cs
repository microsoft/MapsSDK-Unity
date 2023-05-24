// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Maps.Unity;
using UnityEngine;

public class ToggleWeatherLayer : MonoBehaviour
{
    public void Toggle()
    {
        var existingLayer = GetComponent<HttpTextureTileLayer>();
        existingLayer.enabled = !existingLayer.enabled;
    }
}
