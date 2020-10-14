// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Maps.Unity;
using UnityEngine;

/// <summary>
/// Enables appropriate interaction handler based on presence of touch input support.
/// </summary>
[RequireComponent(typeof(MapMouseInteractionHandler))]
[RequireComponent(typeof(MapTouchInteractionHandler))]
public class MapInteractionHandlerSelector : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<MapMouseInteractionHandler>().enabled = !Input.touchSupported;
        GetComponent<MapTouchInteractionHandler>().enabled = Input.touchSupported;
    }
}
