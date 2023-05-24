// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Maps.Unity;
using TMPro;
using UnityEngine;

/// <summary>
/// Copies the <see cref="MapRenderer.Copyright"/> value into the associated <see cref="TextMeshPro"/> component.
/// </summary>
[ExecuteInEditMode]
public class CopyrightSynchronizer : MonoBehaviour
{
    [SerializeField]
    private MapRenderer _mapRenderer = null;

    [SerializeField]
    private TMP_Text _textMeshPro = null;

    private DefaultTextureTileLayer _defaultTextureTileLayer;

    private void Awake()
    {
        _defaultTextureTileLayer = _mapRenderer.GetComponent<DefaultTextureTileLayer>();
    }

    private void Update()
    {
        var copyrightString = _mapRenderer.Copyright;
        _textMeshPro.text = copyrightString;

        if (_defaultTextureTileLayer != null)
        {
            var imageryType = _defaultTextureTileLayer.ImageryType;
            if (imageryType == MapImageryType.Aerial)
            {
                _textMeshPro.fontSharedMaterial.SetColor(ShaderUtilities.ID_FaceColor, Color.white);
                _textMeshPro.fontSharedMaterial.SetColor(ShaderUtilities.ID_OutlineColor, Color.black);
            }
            else
            {
                _textMeshPro.fontSharedMaterial.SetColor(ShaderUtilities.ID_FaceColor, Color.black);
                _textMeshPro.fontSharedMaterial.SetColor(ShaderUtilities.ID_OutlineColor, Color.white);
            }
        }
    }
}
