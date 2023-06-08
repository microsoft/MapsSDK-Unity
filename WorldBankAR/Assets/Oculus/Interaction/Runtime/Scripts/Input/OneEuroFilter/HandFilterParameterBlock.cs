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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Interaction.Input
{
    [CreateAssetMenu(menuName = "Oculus/Interaction/SDK/Input/Hand Filter Parameters")]
    [System.Serializable]
    public class HandFilterParameterBlock : ScriptableObject
    {
        [Header("One Euro Filter Parameters")]
        [SerializeField]
        [Tooltip("Smoothing for wrist position")]
        public OneEuroFilterPropertyBlock wristPositionParameters = new OneEuroFilterPropertyBlock { _beta = 5.0f, _minCutoff = 0.5f, _dCutoff = 1.0f };
        [SerializeField]
        [Tooltip("Smoothing for wrist rotation")]
        public OneEuroFilterPropertyBlock wristRotationParameters = new OneEuroFilterPropertyBlock { _beta = 5.0f, _minCutoff = 0.5f, _dCutoff = 1.0f };
        [SerializeField]
        [Tooltip("Smoothing for finger rotation")]
        public OneEuroFilterPropertyBlock fingerRotationParameters = new OneEuroFilterPropertyBlock { _beta = 1.0f, _minCutoff = 0.5f, _dCutoff = 1.0f };
        [SerializeField]
        [Tooltip("Frequency (in frames per sec)")]
        public float frequency = 72.0f;
    }

}
