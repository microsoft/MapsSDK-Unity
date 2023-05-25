using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class ButtonCtrl : MonoBehaviour
{
    public InputField queryInputField;
    public Button queryBtn;
    [SerializeField] GameObject _mapGObj;
    MyDataLayer _myDataLayer;

    private void OnEnable()
    {
        _myDataLayer = _mapGObj.GetComponent<MyDataLayer>();
        queryBtn.onClick.RemoveAllListeners();
        queryBtn.onClick.AddListener(QueryButtonClick);
    }

    public async void QueryButtonClick()
    {
        string queryText = queryInputField.text;
        if (string.IsNullOrEmpty(queryText) ) return;
        //queryText.Replace(' ', "%20");

        string baseUrl = "https://api.gbif.org/v1/species?name=";
        StringBuilder sb = new StringBuilder(baseUrl);
        sb.Append(queryText);
        sb.Append("&limit=1");

        int nubKey = await GbifApiManager.Instance.GetNubKey(sb.ToString());
        Debug.Log(string.Format("### input field text= {0}; nubKey= {1}",
            queryText, nubKey));

        _myDataLayer.SetTaxonKey(nubKey);
    }

    private void OnDisable()
    {
        queryBtn.onClick.RemoveAllListeners();
    }
}
