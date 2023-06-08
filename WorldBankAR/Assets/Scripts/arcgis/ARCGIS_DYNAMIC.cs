using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ARCGIS_DYNAMIC : MonoBehaviour
{
    private string _url = "https://tiles.arcgis.com/tiles/P8Cok4qAP1sTVE59/arcgis/rest/services?f=pjson";
    private string _networkJsonResponse;
    private JsonARCGISLayers _jsonLayers;
    private bool _isQuerySent = false;
    public bool _isJsonSerialized = false;
    //private List<string> _layersURLs;

    void Start()
    {
        StartCoroutine(GetARCGISLayersList(_url));
    }

    void Update()
    {
        if(_isQuerySent)
        {
            SerializeJsonToLayer(_networkJsonResponse);
            _isQuerySent = false;
            /*foreach(Server server in _jsonLayers.services)
            {
                _layersURLs.Add(server.url);
            }*/
        }
    }

    IEnumerator GetARCGISLayersList(string url)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.Send();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log(www.error);
            }
            else
            {
                _networkJsonResponse = www.downloadHandler.text;
                _isQuerySent = true;
            }
        }
    }

    private void SerializeJsonToLayer(string jsonString)
    {
        _jsonLayers = JsonUtility.FromJson<JsonARCGISLayers>(jsonString);
        _isJsonSerialized = true;
    }

    public bool GetIsJsonSerialized()
    {
        if (_isJsonSerialized)
        {
            _isJsonSerialized = !_isJsonSerialized;
            return !_isJsonSerialized;
        }
        else return _isJsonSerialized;
    }
    public IEnumerator<Server> GetLayers()
    {
        return _jsonLayers.services.GetEnumerator();
    }

    [Serializable]
    public class JsonARCGISLayers
    {
        public List<Server> services;
    }

    [Serializable]
    public class Server
    {
        public string name;
        public string url;
    }
}
