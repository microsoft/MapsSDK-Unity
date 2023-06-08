/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

/// <summary>
/// OVR Component to drive blend shapes on a <c>SkinnedMeshRenderer</c> based on Face Tracking provided by <c>OVRFaceExpressions</c>.
/// </summary>
/// <remarks>
/// See <see cref="OVRFace"/> for more information.
/// This specialization of <see cref="OVRFace"/> provides mapping based on an array, configurable from the editor
/// This component comes with a custom editor that supports attempting to auto populate the mapping array based on string matching
/// See <see cref="OVRCustomFaceEditor"/> for more information.
/// </remarks>
[RequireComponent(typeof(SkinnedMeshRenderer))]
public class OVRCustomFace : OVRFace
{
    public OVRFaceExpressions.FaceExpression[] Mappings
    {
        get => _mappings;
        set => _mappings = value;
    }

    [SerializeField]
    [Tooltip("The mapping between Face Expressions to the blendshapes available " +
             "on the shared mesh of the skinned mesh renderer")]
    internal OVRFaceExpressions.FaceExpression[] _mappings;

    [SerializeField, HideInInspector]
    internal RetargetingType retargetingType;

    [SerializeField]
    [Tooltip("Allow duplicates when mapping blendshapes to Face Expressions")]
    internal bool _allowDuplicateMapping = true;

    protected bool AllowDuplicateMapping => _allowDuplicateMapping;

    /// <inheritdoc/>
    protected override void Start()
    {
        base.Start();

        Assert.IsNotNull(_mappings);
        Assert.AreEqual(_mappings.Length, GetComponent<SkinnedMeshRenderer>().sharedMesh.blendShapeCount,
            "Mapping out of sync with shared mesh.");
    }

    /// <inheritdoc/>
    internal protected override OVRFaceExpressions.FaceExpression GetFaceExpression(int blendShapeIndex)
    {
        Assert.IsTrue(blendShapeIndex < _mappings.Length && blendShapeIndex >= 0);
        return _mappings[blendShapeIndex];
    }

    /// <summary>
    /// Allows the user to define their own blend shape name and face expression pair mappings.
    /// By default it will just return the Oculus version.
    /// </summary>
    /// <returns>Two arrays, each relating a blend shape name with a face expression pair.</returns>
    internal protected virtual (string[], OVRFaceExpressions.FaceExpression[]) GetCustomBlendShapeNameAndExpressionPairs()
    {
        string[] oculusBlendShapeNames = Enum.GetNames(typeof(OVRFaceExpressions.FaceExpression));
        OVRFaceExpressions.FaceExpression[] oculusFaceExpressions =
            (OVRFaceExpressions.FaceExpression[])Enum.GetValues(typeof(OVRFaceExpressions.FaceExpression));
        return (oculusBlendShapeNames, oculusFaceExpressions);
    }

    public enum RetargetingType
    {
        OculusFace,
        Custom,
    }
}
