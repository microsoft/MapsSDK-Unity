using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class HierarchyDeserializerList : MonoBehaviour
{
    //private string[] _localJsonString; // kept for future local use
    private string _networkJsonString;
    private bool _isListReceived = false;
    public static bool _isAccessible = false;
    private jsonDataClass _jsonData;
    void Start()
    {
        //LocaljsonString = System.IO.File.ReadAllLines(@"Assets/Resources/Species.json"); //kept for future local use
        StartCoroutine(GetText());
    }

    private void Update()
    {
        if(_isListReceived)
        {
            ProcessJsonData(_networkJsonString);
            _isAccessible = true;
            _isListReceived = !_isListReceived;

        }
    }
    IEnumerator GetText()
    {
        using (UnityWebRequest www = UnityWebRequest.Get("https://api.gbif.org/v1/species/search?rank=KINGDOM&rank=PHYLUM&advanced=1&limit=350")) //https://api.gbif.org/v1/species/search?limit=86
        {
            yield return www.Send();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log(www.error);
            }
            else
            {
                // Show results as text
                _networkJsonString = www.downloadHandler.text;

                // Or retrieve results as binary data
                byte[] results = www.downloadHandler.data;
                _isListReceived = true;
               
            }
        }
    }

    private void ProcessJsonData(string url)
    {
        _jsonData = JsonUtility.FromJson<jsonDataClass>(url);
       
    }

    public List<Rank>.Enumerator GetRankList()
    {
        return _jsonData.results.GetEnumerator();
    }
}
