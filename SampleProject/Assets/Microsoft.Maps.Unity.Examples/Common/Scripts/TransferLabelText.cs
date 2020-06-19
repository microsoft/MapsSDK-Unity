// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Maps.Unity;
using TMPro;
using UnityEngine;

public class TransferLabelText : MonoBehaviour
{
    [SerializeField]
    TextMeshPro _textMeshPro = null;

    [SerializeField]
    MapLabel _mapLabel = null;

    private void Start()
    {
        Debug.Assert(_textMeshPro != null);
        Debug.Assert(_mapLabel != null);

        _textMeshPro.text = _mapLabel.Text;
        _textMeshPro.color = _mapLabel.Style.Color;
        _textMeshPro.fontStyle = ConvertFontStyle(_mapLabel.Style);
    }

    private FontStyles ConvertFontStyle(Style mapLabelStyle)
    {
        FontStyles result = 0;

        switch (mapLabelStyle.FontStyle)
        {
            case Microsoft.Maps.Unity.FontStyle.Italic:
                result |= FontStyles.Italic;
                break;
        }

        switch (mapLabelStyle.FontWeight)
        {
            case Microsoft.Maps.Unity.FontWeight.Bold:
                result |= FontStyles.Bold;
                break;
        }

        return result;
    }
}
