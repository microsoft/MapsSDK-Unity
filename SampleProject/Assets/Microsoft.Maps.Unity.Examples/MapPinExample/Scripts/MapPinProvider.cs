// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Geospatial;
using Microsoft.Maps.Unity;
using System;
using System.Globalization;
using TMPro;
using UnityEngine;

/// <summary>
/// Adds <see cref="MapPin"/>s to the <see cref="MapPinLayer"/> based on the CSV file. Expected CSV file format is "Lat,Lon,Name,Type".
/// </summary>
public class MapPinProvider : MonoBehaviour
{
    [SerializeField]
    private MapPinLayer _mapPinLayer = null;

    [SerializeField]
    private MapPin _mapPinPrefab = null;

    [SerializeField]
    private TextAsset _mapPinLocationsCsv = null;

    private void Awake()
    {
        Debug.Assert(_mapPinLayer != null);
        Debug.Assert(_mapPinPrefab != null);
        Debug.Assert(_mapPinLocationsCsv != null);

        var lines = _mapPinLocationsCsv.text.Split(new [] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

        _mapPinPrefab.gameObject.SetActive(false);
        
        // Generate a MapPin for each of the locations and add it to the layer.
        foreach (var csvLine in lines)
        {
            var csvEntries = csvLine.Split(',');

            var mapPin = Instantiate(_mapPinPrefab);
            mapPin.Location =
                new LatLon(
                    double.Parse(csvEntries[0], NumberStyles.Number, CultureInfo.InvariantCulture),
                    double.Parse(csvEntries[1], NumberStyles.Number, CultureInfo.InvariantCulture));
            _mapPinLayer.MapPins.Add(mapPin);

            mapPin.GetComponentInChildren<TextMeshPro>().text = csvEntries[2].ToLower() == "null" ? "" : csvEntries[2];
        }
    }
}
