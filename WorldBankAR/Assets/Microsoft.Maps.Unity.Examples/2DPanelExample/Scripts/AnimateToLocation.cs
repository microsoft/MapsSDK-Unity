// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Geospatial;
using Microsoft.Maps.Unity;
using UnityEngine;

[RequireComponent(typeof(MapRenderer))]
public class AnimateToLocation : MonoBehaviour
{
    public void Animate(string location)
    {
        if (location == "North America")
        {
            Animate(new MapSceneOfLocationAndZoomLevel(new LatLon(49.8457301182239, -110.734009177481), 1.9f));
        }
        else if (location == "Alaska")
        {
            Animate(new MapSceneOfLocationAndZoomLevel(new LatLon(61.5830450739248, -154.151974287195), 2.7f));
        }
        else if (location == "Caribbean")
        {
            Animate(new MapSceneOfLocationAndZoomLevel(new LatLon(19.76917618445, -73.3561529043696), 4.3f));
        }
        else if (location == "Great Lakes")
        {
            Animate(new MapSceneOfLocationAndZoomLevel(new LatLon(44.9620029011226, -85.0208010786911), 4.6f));
        }
        else if (location == "Hawaii")
        {
            Animate(new MapSceneOfLocationAndZoomLevel(new LatLon(19.8609249894142, -157.858532034001), 5.6f));
        }
    }

    public void Animate(MapScene mapScene)
    {
        var mapRenderer = GetComponent<MapRenderer>();
        mapRenderer.SetMapScene(mapScene);
    }
}
