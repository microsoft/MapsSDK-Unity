// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Geospatial;
using Microsoft.Maps.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// When enabled, begins an animation through different MapScenes of Seattle.
/// </summary>
public class SeattleTour : MonoBehaviour
{
    private static readonly List<MapScene> MapScenes =
        new List<MapScene>
        {
            // Space Needle -> zoom out to Seattle Center
            new MapSceneOfLocationAndZoomLevel(new LatLon(47.620365, -122.349305), 17.0f),
            // MOHAI/wooden boats
            new MapSceneOfLocationAndZoomLevel(new LatLon(47.626872, -122.336026), 17.0f),
            new MapSceneOfLocationAndZoomLevel(new LatLon(47.627584, -122.336609), 18.0f),
            new MapSceneOfLocationAndZoomLevel(new LatLon(47.628131, -122.336676), 19.0f),
            // Gas Works
            new MapSceneOfLocationAndZoomLevel(new LatLon(47.644556, -122.335012), 16.0f),
            new MapSceneOfLocationAndZoomLevel(new LatLon(47.645065, -122.334899), 18.0f),
            // St Marks
            new MapSceneOfLocationAndZoomLevel(new LatLon(47.631862, -122.321263), 16.0f),
            new MapSceneOfLocationAndZoomLevel(new LatLon(47.631862, -122.321263), 18.0f),
            // Volunteer Park
            new MapSceneOfLocationAndZoomLevel(new LatLon(47.630118, -122.314719), 16.75f),
            new MapSceneOfLocationAndZoomLevel(new LatLon(47.628994, -122.31457), 18.0f),
            // Cal Anderson Park
            new MapSceneOfLocationAndZoomLevel(new LatLon(47.618389, -122.319168), 17.0f),
            new MapSceneOfLocationAndZoomLevel(new LatLon(47.615401, -122.318979), 18.0f),
            // Columbia Center -> Downtown
            new MapSceneOfLocationAndZoomLevel(new LatLon(47.603035, -122.331509), 16.0f),
            // Stadiums
            new MapSceneOfLocationAndZoomLevel(new LatLon(47.595005, -122.331778), 17.0f),
            new MapSceneOfLocationAndZoomLevel(new LatLon(47.591358, -122.332373), 17.0f),
            // Waterfront
            new MapSceneOfLocationAndZoomLevel(new LatLon(47.606085, -122.342533), 18.5f),
            new MapSceneOfLocationAndZoomLevel(new LatLon(47.607588, -122.343334), 18.5f),
            // Pike Place Market
            new MapSceneOfLocationAndZoomLevel(new LatLon(47.61043, -122.343521), 19.5f),
            new MapSceneOfLocationAndZoomLevel(new LatLon(47.608699, -122.340571), 19.5f),
            // Zoom out
            new MapSceneOfLocationAndZoomLevel(new LatLon(47.608699, -122.340571), 15f),
        };

    [SerializeField]
    private MapRenderer _map = null;

    private void Awake()
    {
        Debug.Assert(_map != null);
    }

    void Start()
    {
        StartCoroutine(RunTour());
    }

    private IEnumerator RunTour()
    {
        yield return _map.WaitForLoad();

        while (isActiveAndEnabled) // loop the tour as long as we are running.
        {
            foreach (var scene in MapScenes)
            {
                yield return _map.SetMapScene(scene);
                yield return _map.WaitForLoad();
                yield return new WaitForSeconds(3.0f);
            }
        }
    }
}
