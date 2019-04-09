using Microsoft.Maps.Unity;
using UnityEngine;

/// <summary>
/// Zooms in towards the MapPin when clicked.
/// </summary>
[RequireComponent(typeof(MapPin))]
public class ZoomToMapPin : MonoBehaviour
{
    private MapRenderer _map;
    private MapPin _mapPin;

    void Start()
    {
        _map = GameObject.Find("Map").GetComponent<MapRenderer>();
        _mapPin = GetComponent<MapPin>();
    }

    public void Zoom()
    {
        var mapScene = new MapSceneOfLocationAndZoomLevel(_mapPin.Location, _map.ZoomLevel + 1.01f);
        _map.SetMapScene(mapScene);
    }
}
