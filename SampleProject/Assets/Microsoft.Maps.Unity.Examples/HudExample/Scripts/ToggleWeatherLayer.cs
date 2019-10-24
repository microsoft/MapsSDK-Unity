// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using UnityEngine;

public class ToggleWeatherLayer : MonoBehaviour
{
    public void Toggle()
    {
        var existingLayer = GetComponent<WeatherTextureTileLayer>();
        if (existingLayer == null)
        {
            gameObject.AddComponent<WeatherTextureTileLayer>();
        }
        else
        {
            Destroy(existingLayer);
        }
    }
}
