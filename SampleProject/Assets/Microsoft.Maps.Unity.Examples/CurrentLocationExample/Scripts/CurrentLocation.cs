using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.Maps.Unity;
using TMPro;

/// <summary>
/// This class retrieves the current location of the user via the connected network and returns the latitude and longitude to the MapRenderer.
/// It only works on the actual device.
/// </summary>
public class CurrentLocation : MonoBehaviour
{
    public MapRenderer mapRenderer;
    
    public TextMeshPro debugText;
    
    private uint _desireAccuracyInMetersValue = 0;
    async void Start()
    {
        debugText.text = "Initialization.";
       
#if UNITY_WSA && !UNITY_EDITOR
        var accessStatus = await Geolocator.RequestAccessAsync();
        switch (accessStatus)
        {
            case GeolocationAccessStatus.Allowed:
                debugText.text = "Waiting for update...";
                Geolocator geolocator = new Geolocator { DesiredAccuracyInMeters = _desireAccuracyInMetersValue };
                Geoposition pos = await geolocator.GetGeopositionAsync();
                UpdateLocationData(pos);
                break;

            case GeolocationAccessStatus.Denied:
                debugText.text = "Access to location is denied.";
                break;

            case GeolocationAccessStatus.Unspecified:
                debugText.text = "Unspecified error.";
                UpdateLocationData(null);
                break;
        }
#endif
    }

#if UNITY_WSA && !UNITY_EDITOR
    private void UpdateLocationData(Geoposition position)
    {
        if (position == null)
        {
            debugText.text = "No data";
        }
        else
        {
            debugText.text = position.Coordinate.Point.Position.Latitude.ToString() + "\n" + position.Coordinate.Point.Position.Longitude.ToString();
            mapRenderer.SetMapScene(new MapSceneOfLocationAndZoomLevel(new LatLon(position.Coordinate.Point.Position.Latitude, position.Coordinate.Point.Position.Longitude), 17f));
        }
    }
#endif
}
