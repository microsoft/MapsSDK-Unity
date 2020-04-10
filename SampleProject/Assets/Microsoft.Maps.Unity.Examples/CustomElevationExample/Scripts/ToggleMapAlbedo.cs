// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using UnityEngine;

public class ToggleMapAlbedo : MonoBehaviour
{
    [SerializeField]
    private Material _customTerrainMaterial = null;

    private void Awake()
    {
        _customTerrainMaterial.SetFloat("_UseSolidColor", 1);
    }

    private void OnDestroy()
    {
        _customTerrainMaterial.SetFloat("_UseSolidColor", 1);
    }

    public void Toggle()
    {
        var currentUseSolidColor = _customTerrainMaterial.GetFloat("_UseSolidColor");
        _customTerrainMaterial.SetFloat("_UseSolidColor", -currentUseSolidColor);
    }
}
