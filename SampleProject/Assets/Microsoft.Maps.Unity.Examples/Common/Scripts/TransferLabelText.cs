using Microsoft.Maps.Unity;
using TMPro;
using UnityEngine;

public class TransferLabelText : MonoBehaviour
{
    [SerializeField]
    TextMeshPro _textMeshPro = null;

    [SerializeField]
    MapLabel _mapLabel = null;

    void Start()
    {
        Debug.Assert(_textMeshPro != null);
        Debug.Assert(_mapLabel != null);
        _textMeshPro.text = _mapLabel.Text;
    }
}
