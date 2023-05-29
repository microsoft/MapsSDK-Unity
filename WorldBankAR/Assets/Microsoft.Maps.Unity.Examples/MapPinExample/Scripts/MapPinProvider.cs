// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Geospatial;
using Microsoft.Maps.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
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

        StartCoroutine(LoadMapPinsFromCsv());
    }

    IEnumerator LoadMapPinsFromCsv()
    {
        var startTime = Time.realtimeSinceStartup;
        var frameStartTime = startTime;

        var lines = _mapPinLocationsCsv.text.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

        _mapPinPrefab.gameObject.SetActive(false);

        Debug.Log($"Creating MapPins ({lines.Length}) from {_mapPinLocationsCsv.name}...");

        // Generate a MapPin for each of the locations and add it to the layer.
        var numCreated = 0;
        var mapPinsCreatedThisFrame = new List<MapPin>(lines.Length);
        foreach (var csvLine in lines)
        {
            var csvEntries = csvLine.Split(',');

            var mapPin = Instantiate(_mapPinPrefab);
            mapPin.Location =
                new LatLon(
                    double.Parse(csvEntries[0], NumberStyles.Number, CultureInfo.InvariantCulture),
                    double.Parse(csvEntries[1], NumberStyles.Number, CultureInfo.InvariantCulture));

            mapPin.GetComponentInChildren<TextMeshPro>().text = csvEntries[2].ToLower() == "null" ? "" : csvEntries[2];
            mapPinsCreatedThisFrame.Add(mapPin);

            // yield occasionally to not block rendering.
            if (Time.realtimeSinceStartup - frameStartTime > 0.015f)
            {
                numCreated += mapPinsCreatedThisFrame.Count;

                _mapPinLayer.MapPins.AddRange(mapPinsCreatedThisFrame);
                mapPinsCreatedThisFrame.Clear();

                Debug.Log($"{numCreated}/{lines.Length} MapPins created.");

                yield return null;

                frameStartTime = Time.realtimeSinceStartup;
            }
        }

        _mapPinLayer.MapPins.AddRange(mapPinsCreatedThisFrame);
        mapPinsCreatedThisFrame.Clear();

        Debug.Log($"MapPin creation complete. ({Time.realtimeSinceStartup - startTime:F2}s)");
    }
}
