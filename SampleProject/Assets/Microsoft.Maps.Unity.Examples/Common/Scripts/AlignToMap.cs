// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using UnityEngine;

public class AlignToMap : MonoBehaviour
{
    private Transform _transformToAlignTo;

    void Start()
    {
        _transformToAlignTo = GameObject.Find("Map").transform;

        if (_transformToAlignTo != null)
        {
            transform.forward = _transformToAlignTo.forward;
        }
    }

    void Update()
    {
        if (_transformToAlignTo != null)
        {
            transform.forward = _transformToAlignTo.forward;
        }
    }
}
