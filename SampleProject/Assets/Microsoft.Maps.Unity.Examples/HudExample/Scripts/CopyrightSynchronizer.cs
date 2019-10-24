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
    private TextMeshPro _textMeshPro = null;

    void Update()
    {
        var copyrightString = _mapRenderer.Copyright;
        _textMeshPro.text = copyrightString;
    }
}
