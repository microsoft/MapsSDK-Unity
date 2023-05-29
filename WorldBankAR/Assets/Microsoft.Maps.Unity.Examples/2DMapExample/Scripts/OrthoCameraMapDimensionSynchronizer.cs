// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Maps.Unity;
using UnityEngine;

[RequireComponent(typeof(MapRenderer))]
public class OrthoCameraMapDimensionSynchronizer : MonoBehaviour
{
    private Camera _camera = null;
    private MapRenderer _mapRenderer = null;

    private void Awake()
    {
        _camera = Camera.main;
        _mapRenderer = GetComponent<MapRenderer>();
    }

    private void Update()
    {
        var cameraOrthoSize = _camera.orthographicSize;
        var cameraOrthoHeight = 2 * cameraOrthoSize;
        var cameraOrthoWidth = cameraOrthoHeight * _camera.aspect;

        _mapRenderer.LocalMapDimension = new Vector2(Mathf.Min(3.0f, cameraOrthoWidth), Mathf.Min(3.0f, cameraOrthoHeight));
    }
}
