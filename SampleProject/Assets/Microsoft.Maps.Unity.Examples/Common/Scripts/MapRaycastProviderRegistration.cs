// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Maps.Unity;
using Microsoft.MixedReality.Toolkit;
using UnityEngine;

/// <summary>
/// Registers the MapRenderer component with the MapRaycastProvider.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(MapRenderer))]
public class MapRaycastProviderRegistration : MonoBehaviour
{
    private MapRenderer _mapRenderer = null;
    private MapRaycastProvider _mapRaycastProvider = null;

    private void Start()
    {
        _mapRenderer = GetComponent<MapRenderer>();
        if (
            _mapRenderer != null &&
            CoreServices.InputSystem.RaycastProvider is MapRaycastProvider _mapRaycastProvider)
        {
            _mapRaycastProvider.RegisterMapRenderer(_mapRenderer);
        }
    }

    private void OnDestroy()
    {
        if (_mapRaycastProvider != null)
        {
            _mapRaycastProvider.UnregisterMapRenderer(_mapRenderer);
        }
    }
}
