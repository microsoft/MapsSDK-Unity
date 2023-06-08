using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.Maps.Unity;

public class HTTPLayersController : MonoBehaviour
{
    [SerializeField] GoogleSheetsParser _googleSheetsParser;
    public List<Http> _availableLayers;
    public Dictionary<int, Http> _selectedLayers;
    [SerializeField] private MapRenderer _mapRenderer;
    private bool _isLayersCopied = false;
    
    void Start()
    {
        _googleSheetsParser.OnLayersLoaded += CopyLayers;
    }

    public void Update()
    {
        if(_isLayersCopied)
        {
            SelectAndActivateLayer(0, _availableLayers[0].name, _availableLayers[0].url);
            _isLayersCopied = false;
        }
    }

    private void CopyLayers(List<Http> layerList)
    {
        _availableLayers = layerList;
        _isLayersCopied = true;
    }
    private void SelectLayer(int index, Http layer)
    {
        if (_selectedLayers.ContainsKey(index))
        {
            if (_selectedLayers[index].name != "NONE")
            {
                _availableLayers.Add(_selectedLayers[index]);
            }
            _selectedLayers[index] = layer;
        }
        else
        {
            _selectedLayers.Add(index, layer);
        }
        _availableLayers.Remove(layer);
    }


    public void SelectAndActivateLayer(int n, string name, string url)
    {
        var layer = new Http { name = name, url = url };
        //SelectLayer(n, layer);
        ActivateMapLayer(n + 1);
    }

    private void ActivateMapLayer(int index)
    {
        
        var tileLayers = _mapRenderer.TextureTileLayers;
        Debug.Log("Number of current layers" + _mapRenderer.TextureTileLayers.Count);
        HttpTextureTileLayer httpTextureLayer = (HttpTextureTileLayer)tileLayers[2];
        httpTextureLayer.UrlFormatString = _availableLayers[index].url;
        httpTextureLayer.enabled = true;
        /*if (_selectedLayers[index - 1] == null)
        {
            httpTextureLayer.UrlFormatString = null;
            httpTextureLayer.enabled = false;
        }
        else
        {
            httpTextureLayer.UrlFormatString = _selectedLayers[index - 1].url;
            httpTextureLayer.enabled = true;
        }*/
    }

}
