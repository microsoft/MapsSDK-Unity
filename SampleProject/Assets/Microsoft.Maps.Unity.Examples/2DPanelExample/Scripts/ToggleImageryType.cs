// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Maps.Unity;
using UnityEngine;

public class ToggleImageryType : MonoBehaviour
{
    public void Toggle()
    {
        var defaultTextureTileLayer = GetComponent<DefaultTextureTileLayer>();
        if (defaultTextureTileLayer != null)
        {
            if (defaultTextureTileLayer.ImageryType == MapImageryType.Aerial)
            {
                defaultTextureTileLayer.ImageryType = MapImageryType.Symbolic;
            }
            else
            {
                defaultTextureTileLayer.ImageryType = MapImageryType.Aerial;
            }
        }
    }
}
