using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static ButtonCtrl;

public class GenerateButtons : MonoBehaviour
{
    public Button _prefab;
    [SerializeField]
    private HierarchyDeserializerList _myHierarchyDeserializerList;
    private bool _isInitialized = false;
    private int _iterationCounts = 0;
    [SerializeField]
    private int _buttonHeightInCell = 40;
    [SerializeField]
    private int _spacing = 5;
    [SerializeField]
    private Button _backButton;
    [SerializeField]
    private ButtonCtrl _buttonCtrl;

    void Update()
    {
        if (HierarchyDeserializerList._isAccessible)
        {
            if(!_isInitialized)
            {
                GenButtons("KINGDOM", null);
                _isInitialized = true;
            }
        }
    }

    public void GenButtons(string rank, string kingdom)
    {
        DeleteButtons();
        if (!string.IsNullOrEmpty(kingdom)) 
        {
            _backButton.gameObject.SetActive(true);
            _iterationCounts++;
        }
        IEnumerator<Rank> _buttonList = _myHierarchyDeserializerList.GetRankList();
        while(_buttonList.MoveNext())
        {
            if (_buttonList.Current.rank == rank && (string.Equals(_buttonList.Current.kingdom, kingdom) || string.IsNullOrEmpty(kingdom)))
            {
                _iterationCounts++;
                GenButton(_buttonList.Current.scientificName, _buttonList.Current.nubKey, (string.IsNullOrEmpty(kingdom)));//if rank has children, then add listener with GenButton()
            }
        }
        float x = this.GetComponent<RectTransform>().sizeDelta.x;
        this.GetComponent<RectTransform>().sizeDelta = new Vector2(x, _iterationCounts * (_buttonHeightInCell + _spacing));
        _iterationCounts = 0;
    }

    private void GenButton(string buttonText, int nubKey, bool hasChildren)
    {
        Button button = Instantiate(_prefab, Vector3.zero, Quaternion.identity, this.transform);
        button.GetComponentInChildren<Text>().text = buttonText;
        if (hasChildren) button.onClick.AddListener(() => { ButtonPress(button); CallButtonCtrlTaxon(nubKey); });
        else button.onClick.AddListener(() => { ButtonPress(button); CallButtonCtrlTaxon(nubKey); });
    }

    private void CallButtonCtrlTaxon(int taxon)
    {
        _buttonCtrl.StartCoroutine(_buttonCtrl.AdjustDataLayer(taxon));
    }
    private void DeleteButtons()
    {
        foreach(Button B in this.GetComponentsInChildren<Button>())
        {
            if (!string.Equals(B.tag, "Back"))  Destroy(B.gameObject); // onDestroy() function is attached to B prefab
        }
    }
    public void ButtonPress(Button button)
    {
        string input = button.GetComponentInChildren<Text>().text;
        GenButtons("PHYLUM", input);
    }

    public void BackPress()
    {
        GenButtons("KINGDOM", null);
    }
}
