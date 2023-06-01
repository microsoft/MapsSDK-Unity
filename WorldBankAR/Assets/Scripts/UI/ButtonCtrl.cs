using System.Collections;
using System.Collections.Generic;
using System.Text;
using Microsoft.Geospatial;
using Microsoft.Maps.Unity;
using Microsoft.MixedReality.Toolkit.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
public class ButtonCtrl : MonoBehaviour
{
    public InputField queryInputField;
    public Button queryBtn;
    [SerializeField] GameObject _mapGObj;
    [SerializeField] private bool _canDragMapGameObject = true;

    private MyDataLayer _myDataLayer;
    private MapRaycastProviderRegistration _mapRaycastProvider;
    private MapInteractionController _mapInteractionCtrl;
    private MapInteractionHandler _mapInteractionHandler;
    private ObjectManipulator _objectManipulator;
    private MapZoomManipulator _mapZoomManipulator;

    private void Awake()
    {
        _mapRaycastProvider = _mapGObj.GetComponent<MapRaycastProviderRegistration>();
        _mapInteractionCtrl = _mapGObj.GetComponent<MapInteractionController>();
        _mapInteractionHandler = _mapGObj.GetComponent<MapInteractionHandler>();
        _objectManipulator = _mapGObj.GetComponent<ObjectManipulator>();
        _mapZoomManipulator = _mapGObj.GetComponent<MapZoomManipulator>();

        _mapRaycastProvider.enabled = false;
        _mapInteractionCtrl.enabled = false;
        _mapInteractionHandler.enabled = false;
        _mapZoomManipulator.enabled = false;
    }

    private void OnEnable()
    {
        _myDataLayer = _mapGObj.GetComponent<MyDataLayer>();
        queryBtn.onClick.RemoveAllListeners();
        queryBtn.onClick.AddListener(QueryButtonClick);
    }

    public void LogTapHold()
    {
        Debug.Log("+++ LoTapHold on lla= " );
    }

    public void LogDoubleTap()
    {
        Debug.Log("+++ double tap on lla= " );
    }

    // drag, pinch, rotate gesture takes effect either on the map GameObject or the map content
    public void ToggleMovePanMap(TextMeshProUGUI text)
    {
        _canDragMapGameObject = !_canDragMapGameObject;
        _mapRaycastProvider.enabled = !_canDragMapGameObject;
        _mapInteractionCtrl.enabled = !_canDragMapGameObject; //<--
        _mapInteractionHandler.enabled = !_canDragMapGameObject;
        _mapZoomManipulator.enabled = !_canDragMapGameObject;

        _objectManipulator.enabled = _canDragMapGameObject;


        text.text = _canDragMapGameObject ? "Pan & Zoom" : "Place Map";
    }

    public async void QueryButtonClick()
    {
        string queryText = queryInputField.text;
        Debug.Log("inside QueryButtonClick : " + queryText);
        if (string.IsNullOrEmpty(queryText) ) return;
        //queryText.Replace(' ', "%20");

        string baseUrl = "https://api.gbif.org/v1/species?name=";
        StringBuilder sb = new StringBuilder(baseUrl);
        sb.Append(queryText);
        sb.Append("&limit=1");
        int nubKey = await GbifApiManager.Instance.GetNubKey(sb.ToString());
        StartCoroutine(AdjustDataLayer(nubKey));
    }

    public IEnumerator AdjustDataLayer(int nubKey)
    {
        _myDataLayer.SetTaxonKey(nubKey);
        _myDataLayer.enabled = false;
        yield return new WaitForSeconds(.25f);
        _myDataLayer.enabled = true;
    }

    private void OnDisable()
    {
        queryBtn.onClick.RemoveAllListeners();
    }
}
