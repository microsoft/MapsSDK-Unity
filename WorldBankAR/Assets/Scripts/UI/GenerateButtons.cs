using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static ButtonCtrl;

public class GenerateButtons : MonoBehaviour
{
    public Button prefab;
    [SerializeField]
    private HierarchyDeserializerList _myHierarchyDeserializerList;
    private bool isInitialized = false;
    private int iterationCounts = 0;
    [SerializeField]
    private int ButtonHeightInCell = 40;
    [SerializeField]
    private int Spacing = 5;
    [SerializeField]
    private Button backButton;
    [SerializeField]
    private ButtonCtrl _buttonCtrl;

    void Update()
    {
        if (HierarchyDeserializerList._isAccessible)
        {
            if(!isInitialized)
            {
                GenButtons("KINGDOM", null);
                isInitialized = true;
            }
        }
    }

    public void GenButtons(string Rank, string Kingdom)
    {
        DeleteButtons();
        if (Kingdom != null)
        {
            backButton.gameObject.SetActive(true);
            iterationCounts++;
        }
        foreach (Rank rank in _myHierarchyDeserializerList.getRankList())
        {
            if (rank.rank == Rank && (rank.kingdom == Kingdom || Kingdom == null))
            {
                iterationCounts++;
                GenButton(rank.scientificName,  rank.nubKey, (Kingdom == null ? true : false));//if rank has children, then add listener with GenButton()
            }
        }
        float x = this.GetComponent<RectTransform>().sizeDelta.x;
        this.GetComponent<RectTransform>().sizeDelta = new Vector2(x, iterationCounts * (ButtonHeightInCell + Spacing));
        iterationCounts = 0;
    }




    private void GenButton(string ButtonText, int nubKey, bool hasChildren)
    {
        Button button = Instantiate(prefab, Vector3.zero, Quaternion.identity, this.transform);
        button.GetComponentInChildren<Text>().text = ButtonText;
        if (hasChildren) button.onClick.AddListener(() => { ButtonPress(button); callButtonCtrlTaxon(nubKey); });
        else button.onClick.AddListener(() => { ButtonPress(button); callButtonCtrlTaxon(nubKey); });
    }

    private void callButtonCtrlTaxon(int taxon)
    {
        _buttonCtrl.StartCoroutine(_buttonCtrl.AdjustDataLayer(taxon));
    }
    private void DeleteButtons()
    {
        foreach(Button B in this.GetComponentsInChildren<Button>())
        {
            B.gameObject.SetActive(false);
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
