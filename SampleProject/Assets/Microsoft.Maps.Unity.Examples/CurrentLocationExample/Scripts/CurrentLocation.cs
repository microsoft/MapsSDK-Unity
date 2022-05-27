#pragma warning disable 0414

using System.Threading.Tasks;
using UnityEngine;
#if UNITY_WSA && !UNITY_EDITOR
using System;
using Windows.Devices.Geolocation;
using Microsoft.Geospatial;
#endif
using TMPro;
using Microsoft.Maps.Unity;

/// <summary>
/// This class retrieves the current location of the user via the connected network and returns the latitude and longitude to the MapRenderer.
/// It only works on the actual device.
/// </summary>
public class CurrentLocation : MonoBehaviour
{
    [SerializeField]
    private MapRenderer _mapRenderer = null;

    [SerializeField] 
    private TextMeshPro _debugText = null;

    private uint _desireAccuracyInMetersValue = 0;

    private async void Start()
    {
        _debugText.text = "Initialization.";

#if UNITY_WSA && !UNITY_EDITOR
        var accessStatus = await Geolocator.RequestAccessAsync();
        switch (accessStatus)
        {
            case GeolocationAccessStatus.Allowed:
                _debugText.text = "Waiting for update...";
                Geolocator geolocator = new Geolocator { DesiredAccuracyInMeters = _desireAccuracyInMetersValue };
                Geoposition position = await geolocator.GetGeopositionAsync();
                UpdateLocationData(position);
                break;

            case GeolocationAccessStatus.Denied:
                _debugText.text = "Access to location is denied.";
                break;

            case GeolocationAccessStatus.Unspecified:
                _debugText.text = "Unspecified error.";
                UpdateLocationData(null);
                break;
        }
#else
        await Task.Delay(0);
#endif
    }

#if UNITY_WSA && !UNITY_EDITOR
    private void UpdateLocationData(Geoposition geoposition)
    {
        if (geoposition == null)
        {
            _debugText.text = "No data";
        }
        else
        {
            var pointPosition = geoposition.Coordinate.Point.Position;
            _debugText.text = pointPosition.Latitude.ToString() + "\n" + pointPosition.Longitude.ToString();
            _mapRenderer.SetMapScene(new MapSceneOfLocationAndZoomLevel(new LatLon(pointPosition.Latitude, pointPosition.Longitude), 17f));
        }
    }
#endif
}
