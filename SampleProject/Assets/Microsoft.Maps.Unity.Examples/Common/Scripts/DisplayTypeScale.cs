// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.CameraSystem;
using UnityEngine;

/// <summary>
/// Applies the specified local scale based on the value of <see cref="IMixedRealityCameraSystem.IsOpaque"/>.
/// </summary>
public class DisplayTypeScale : MonoBehaviour
{
    [SerializeField]
    private Vector3 _opaqueScale = Vector3.one;

    [SerializeField]
    private Vector3 _transparentScale = Vector3.one;

    void Start()
    {
        var cameraSystem = CoreServices.CameraSystem;
        if (cameraSystem != null && !cameraSystem.IsOpaque)
        {
            transform.localScale = _transparentScale;
        }
        else
        {
            transform.localScale = _opaqueScale;
        }
    }
}
