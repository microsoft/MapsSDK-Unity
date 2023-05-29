// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Maps.Unity;
using Microsoft.Maps.Unity.Search;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(MapRenderer))]
public class SearchHandler : MonoBehaviour
{
    [SerializeField]
    TMP_InputField _inputField = null;

    public async void OnSearch()
    {
        if (MapSession.Current == null || string.IsNullOrWhiteSpace(MapSession.Current.DeveloperKey))
        {
            return;
        }

        var searchText = _inputField.text;
        var result = await MapLocationFinder.FindLocations(searchText);
        if (result.Locations.Count > 0)
        {
            var location = result.Locations[0];
            var mapRenderer = GetComponent<MapRenderer>();
            mapRenderer.SetMapScene(new MapSceneOfLocationAndZoomLevel(location.Point, 13));
        }
    }
}
