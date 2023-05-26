using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class GbifApiManager : MonoBehaviour
{
    public static GbifApiManager Instance { get; private set; }
    public bool debug;
    public string baseUrlSpecies = "https://api.gbif.org/v1/species?";
    public string paramString = "name=Aquila%20pomarina&limit=1";
    //https://www.gbif.org/occurrence/map?taxon_key=3113414

    private string _json;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public async Task<int> GetNubKey(string query)
    {
        var result = await FetchGbifRoot(query);

        if (result != null)
        {
            List<GbifSpeciesObj> list = result.results;
            if (list.Count > 0)
            {
                var speciesObj = list[0];
                //Debug.Log(string.Format("### species {0}; nub key = {1}", speciesObj.species, speciesObj.nubKey));
                return speciesObj.nubKey;
            }
            else return -1;
        }
        else return -1;
    }

    public async Task<GbifRoot> FetchGbifRoot(string query)
    {
        await MakeRequest(query);

        if (!string.IsNullOrEmpty(_json))
        {
            return JsonSerializerBC.ParseJsonToArray(_json);
        }
        else
        {
            return null;
        }
    }

    private IEnumerator MakeRequest(string query)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(query))
        {
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.LogError("### " + www.error);
            }
            else
            {
                // results as text
                _json = www.downloadHandler.text;
            }
        }
    }

}
