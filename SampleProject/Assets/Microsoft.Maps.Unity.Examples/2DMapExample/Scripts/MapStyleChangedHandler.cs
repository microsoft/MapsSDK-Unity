// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Maps.Unity;
using UnityEngine;

[RequireComponent(typeof(MapRenderer))]
public class MapStyleChangedHandler : MonoBehaviour
{
    public void OnMapImageryTypeChanged(int dropdownValue)
    {
        var defaultTextureTileLayer = GetComponent<DefaultTextureTileLayer>();
        defaultTextureTileLayer.ImageryType = dropdownValue == 0 ? MapImageryType.Symbolic : MapImageryType.Aerial;
    }

    public void OnToggleTraffic()
    {
        var trafficLayer = GetComponent<DefaultTrafficTextureTileLayer>();
        if (trafficLayer == null)
        {
            trafficLayer = gameObject.AddComponent<DefaultTrafficTextureTileLayer>();
        }
        else
        {
            trafficLayer.enabled = !trafficLayer.enabled;
        }
    }
}
