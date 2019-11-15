// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Maps.Unity;
using Microsoft.Maps.Unity.Search;
using Microsoft.Maps.Unity.Services;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using TMPro;
using UnityEngine;

/// <summary>
/// Instantiates a <see cref="MapPin"/> for each location that is reverse geocoded.
/// The <see cref="MapPin"/> will display the address of the reverse geocoded location.
/// </summary>
[RequireComponent(typeof(MapRenderer))]
public class ReverseGeocodeOnClick : MonoBehaviour
{
    private MapRenderer _mapRenderer = null;

    /// <summary>
    /// The layer to place MapPins.
    /// </summary>
    [SerializeField]
    private MapPinLayer _mapPinLayer = null;

    /// <summary>
    /// The MapPin prefab to instantiate for each location that is reverse geocoded.
    /// If it uses a TextMeshPro component, the address text will be written to it.
    /// </summary>
    [SerializeField]
    private MapPin _mapPinPrefab = null;

    public void Awake()
    {
        _mapRenderer = GetComponent<MapRenderer>();
        Debug.Assert(_mapRenderer != null);
        Debug.Assert(_mapPinLayer != null);
    }

    public async void OnMapClick(MixedRealityPointerEventData mixedRealityPointerEventData)
    {
        if (string.IsNullOrEmpty(MapServices.BingMapsKey))
        {
            Debug.LogError(
                "Provide a Bing Maps key to use the map services. " +
                "This key can either be set on the MapRenderer or directly in code through MapServices.BingMapsKey property.");
            return;
        }

        var focusProvider = CoreServices.InputSystem.FocusProvider;
        if (focusProvider.TryGetFocusDetails(mixedRealityPointerEventData.Pointer, out var focusDetails))
        {
            var location = _mapRenderer.TransformWorldPointToLatLon(focusDetails.Point);
            var finderResult = await MapLocationFinder.FindLocationsAt(location);

            string formattedAddressString = null;
            if (finderResult.Locations.Count > 0)
            {
                formattedAddressString = finderResult.Locations[0].Address.FormattedAddress;
            }

            if (_mapPinPrefab != null)
            {
                // Create a new MapPin instance, using the location of the focus details.
                var newMapPin = Instantiate(_mapPinPrefab);
                newMapPin.Location = location;
                var textMesh = newMapPin.GetComponentInChildren<TextMeshPro>();
                textMesh.text = formattedAddressString ?? "No address found.";

                _mapPinLayer.MapPins.Add(newMapPin);
            }
        }
        else
        {
            // Unexpected.
            Debug.LogWarning("Unable to get FocusDetails from Pointer.");
        }
    }
}
