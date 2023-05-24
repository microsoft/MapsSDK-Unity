using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

public class JsonSerializerBC
{

    // save to local file persistent data path
    public async static UniTask SaveToFileAsync(List<Poco> data, string savePath)
    {
        if (File.Exists(savePath))
        {
            string jsonStringToConvert = JsonConvert.SerializeObject(data);
            Debug.Log(data.Count + " obj to save. jsonString = " + jsonStringToConvert);

            using (StreamWriter stream = new StreamWriter(savePath))
            {
                await stream.WriteAsync(jsonStringToConvert);
                stream.Close();
            }
        }
        else
            Debug.LogError("### Json File does not exist! at path: " + savePath);
    }


    // load and parse Json from local path
    public static async Task<List<Poco>> LoadAndParseJson(string path)
    {
        List<Poco> dataList = new List<Poco>();

        //string path = string.Concat(Application.streamingAssetsPath, "/", fileName);
        //todo check if file is there
        if (File.Exists(path))
        {
            using (StreamReader stream = new StreamReader(path))
            {
                await stream.ReadToEndAsync().AsUniTask().ContinueWith(fetched =>
                {
                    if (!string.IsNullOrEmpty(fetched))
                    {
                        dataList = ParseJsonToList(fetched);
                    }
                });
                stream.Close();
            }
        }
        else
            Debug.LogError("### File does not exist! at path: " + path);

        return dataList;
    }

    public static List<Poco> ParseJsonToList(string loadedJsonString)
    {
        Debug.Log("### loadedJsonString: " + loadedJsonString);

        List<Poco> dataList = new List<Poco>();

        try
        {
            dataList = JsonConvert.DeserializeObject<List<Poco>>(loadedJsonString);

            Debug.Log($"### Finished parsing. There are {dataList.Count} entries");
            //GlobalEventBroker.CallJsonParsed(true);
        }
        catch (Exception ex)
        {
            Debug.Log(ex.Message);
        }
        return dataList;
    }

    public static GbifRoot ParseJsonToArray(string loadedJsonString)
    {
        Debug.Log("### loadedJsonString: " + loadedJsonString);

        try
        {
            var root = JsonConvert.DeserializeObject<GbifRoot>(loadedJsonString);

            Debug.Log($"### Finished parsing.");
            return root;
        }
        catch (Exception ex)
        {
            Debug.Log(ex.Message);
        }
        return null;
    }
}
