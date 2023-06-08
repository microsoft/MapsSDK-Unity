
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class PostSender : MonoBehaviour
{
    private jsonDataLayerClass _jsonData;

    private string mapId1;


    void Start()
    {
        StartCoroutine(Upload());
    }

    IEnumerator Upload()
    {
        string _url = "https://maps.worldbank.org/gspext-api/rest/earthengine-node";
        byte[] jsonToSend = System.IO.File.ReadAllBytes(@"Assets/Resources/Species.json");
        using (UnityWebRequest www = new UnityWebRequest(_url, "POST"))
        {
            UploadHandlerRaw uH = new UploadHandlerRaw(jsonToSend);
            DownloadHandlerBuffer dH = new DownloadHandlerBuffer();
            uH.contentType = "application/json";
            www.SetRequestHeader("content-type", "application/json");
            www.uploadHandler = uH;
            www.downloadHandler = dH;
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                Debug.Log("Form upload complete!");
                //Debug.Log(www.downloadHandler.text);
                string ServerResponse = www.downloadHandler.text;

                _jsonData = JsonUtility.FromJson<jsonDataLayerClass>(ServerResponse);
                Debug.Log("Parameter mapID1 can be found here:"+ _jsonData.treeCover.mapId1);
            }
        }
        StartCoroutine(RequestMapFromID());
    }

    IEnumerator RequestMapFromID()
    {
        using (UnityWebRequest www = UnityWebRequest.Get("https://earthengine.googleapis.com/v1alpha/projects/earthengine-legacy/maps/" + mapId1 + " tiles/8/138/84"))
        {
            yield return www.Send();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log(www.error);
            }
            else
            {
                // Show results as text
                //_networkJsonString = www.downloadHandler.text;

                // Or retrieve results as binary data
                byte[] results = www.downloadHandler.data;
                //_isListReceived = true;
            }
        }
    }

}
