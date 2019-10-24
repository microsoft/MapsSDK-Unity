// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Maps.Unity;
using UnityEngine;

/// <summary>
/// Zooms in towards the <see cref="ClusterMapPin"/> when clicked.
/// </summary>
[RequireComponent(typeof(ClusterMapPin))]
public class ZoomToClusterMapPin : MonoBehaviour
{
    private MapRenderer _map;
    private ClusterMapPin _clusterMapPin;

    void Start()
    {
        _map = GameObject.Find("Map").GetComponent<MapRenderer>();
        Debug.Assert(_map != null);
        _clusterMapPin = GetComponent<ClusterMapPin>();
        Debug.Assert(_clusterMapPin != null);
    }

    public void Zoom()
    {
        var mapScene = new MapSceneOfLocationAndZoomLevel(_clusterMapPin.Location, _map.ZoomLevel + 1.01f);
        _map.SetMapScene(mapScene);
    }
}
