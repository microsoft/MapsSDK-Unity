using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Networking;
using OVRSimpleJSON;

public class GoogleSheetsParser : MonoBehaviour
{
    private string layersURL = "https://sheets.googleapis.com/v4/spreadsheets/1ijj1FTueMFmvGrNC8_nbct-VqIAVqpIWsntkeb65U-0/values/Sheet1?key=AIzaSyCK4AWmFJOnieXIhDr3IDwjyvpvLqCiYSo";
    public event Action<List<Http>> OnLayersLoaded;

    void Start()
    {
        StartCoroutine(SendRequestLayers(layersURL));
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private IEnumerator SendRequestLayers(string url)
    {

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError)
            {
                Debug.LogError("Request Error: " + request.error);
            }
            else
            {
                List<Http> httpLayers = new List<Http>();

                //var downloadHandler = request.downloadHandler;
                
                JSONNode root = JSONNode.Parse(request.downloadHandler.text);
                foreach (JSONNode n in root)
                {
                    foreach (var item in n.Values)
                    {
                        httpLayers.Add(new Http { name = item[0], url = item[1] });
                    }
                }
                Debug.Log(httpLayers.Count);
                OnLayersLoaded?.Invoke(httpLayers);
            }
            
        }
    }
}



[System.Serializable]
public class Http
{
    public string name;
    public string url;
}
