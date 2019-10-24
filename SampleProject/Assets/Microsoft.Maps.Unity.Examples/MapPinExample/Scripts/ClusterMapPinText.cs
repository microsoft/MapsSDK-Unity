// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Maps.Unity;
using TMPro;
using UnityEngine;

/// <summary>
/// Updates the text component to reflect the size of the cluster.
/// </summary>
public class ClusterMapPinText : MonoBehaviour
{
    private TextMeshPro _textMeshPro;
    private ClusterMapPin _clusterMapPin;

    void Start()
    {
        _textMeshPro = GetComponentInChildren<TextMeshPro>();
        _clusterMapPin = GetComponent<ClusterMapPin>();
    }

    void Update ()
    {
        _textMeshPro.text = string.Format("{0:n0}", _clusterMapPin.Size);
    }
}
