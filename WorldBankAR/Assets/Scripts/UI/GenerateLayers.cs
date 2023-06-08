using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static ARCGIS_DYNAMIC;

public class GenerateLayers : MonoBehaviour
{
    [SerializeField]
    private Button _prefab;
    [SerializeField]
    private ARCGIS_DYNAMIC _myLayersListGenerator;
    private IEnumerator<Server> _myLayersList;
    private int _iterationCounts = 0;
    [SerializeField]
    private int _buttonHeightInCell = 40;
    [SerializeField]
    private int _spacing = 5;

    void Start()
    {
        
    }

    void Update()
    {
        if(_myLayersListGenerator.GetIsJsonSerialized())
        {
            _myLayersList = _myLayersListGenerator.GetLayers();
            _myLayersListGenerator._isJsonSerialized = false;
            while (_myLayersList.MoveNext())
            {
                GenButton(_myLayersList.Current.name, _myLayersList.Current.url);//if rank has children, then add listener with GenButton() 
                _iterationCounts++;
            }
            float x = this.GetComponent<RectTransform>().sizeDelta.x;
            this.GetComponent<RectTransform>().sizeDelta = new Vector2(x, _iterationCounts * (_buttonHeightInCell + _spacing));
        }
    }
    private void GenButton(string buttonText, string buttonURL)
    {
        Button button = Instantiate(_prefab, Vector3.zero, Quaternion.identity, this.transform);
        button.GetComponentInChildren<Text>().text = buttonText;
        button.GetComponent<URLHolder>()._LayerUrl = buttonURL;
        //if (hasChildren) button.onClick.AddListener(() => { ButtonPress(button); CallButtonCtrlTaxon(nubKey); });
        //else button.onClick.AddListener(() => { ButtonPress(button); CallButtonCtrlTaxon(nubKey); });
    }

}
