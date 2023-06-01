using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class HierarchyDeserializerList : MonoBehaviour
{
    private string[] LocaljsonString;
    private string NetworkjsonString;
    //private List<Rank> list;
    private bool _isListReceived = false;
    public static bool _isAccessible = false;
    private jsonDataClass jsonData;
    void Start()
    {
        LocaljsonString = System.IO.File.ReadAllLines(@"Assets/Resources/Species.json");
        StartCoroutine(GetText());
    }

    private void Update()
    {
        if(_isListReceived)
        {
            processJsonData(NetworkjsonString);
            _isAccessible = true;
            _isListReceived = !_isListReceived;
            /*foreach(Rank rank in jsonData.results)
            {
                Debug.Log(rank.scientificName);
            }*/
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
                NetworkjsonString = www.downloadHandler.text;

                // Or retrieve results as binary data
                byte[] results = www.downloadHandler.data;
                _isListReceived = true;
               
            }
        }
    }

    private void processJsonData(string _url)
    {
        jsonData = JsonUtility.FromJson<jsonDataClass>(_url);
       
    }

    public List<Rank> getRankList()
    {
        //Debug.Log(jsonData.results);
        return jsonData.results;
    }
}
