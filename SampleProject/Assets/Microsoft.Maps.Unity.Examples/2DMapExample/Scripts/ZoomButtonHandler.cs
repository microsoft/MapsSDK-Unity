// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Maps.Unity;
using UnityEngine;

[RequireComponent(typeof(MapRenderer))]
public class ZoomButtonHandler : MonoBehaviour
{
    private const float AnimationTimeScale = 100.0f; // Faster than default.

    public void OnZoomIn()
    {
        var mapRenderer = GetComponent<MapRenderer>();
        mapRenderer.SetMapScene(new MapSceneOfLocationAndZoomLevel(mapRenderer.Center, mapRenderer.ZoomLevel + 1), MapSceneAnimationKind.Linear, AnimationTimeScale);
    }

    public void OnZoomOut()
    {
        var mapRenderer = GetComponent<MapRenderer>();
        mapRenderer.SetMapScene(new MapSceneOfLocationAndZoomLevel(mapRenderer.Center, mapRenderer.ZoomLevel - 1), MapSceneAnimationKind.Linear, AnimationTimeScale);
    }
}
