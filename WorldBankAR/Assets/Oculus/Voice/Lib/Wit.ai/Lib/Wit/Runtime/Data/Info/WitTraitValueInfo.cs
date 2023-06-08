/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using UnityEngine;

namespace Meta.WitAi.Data.Info
{
    [Serializable]
    public struct WitTraitValueInfo
    {
        /// <summary>
        /// Trait value contents
        /// </summary>
        [SerializeField] public string id;
        /// <summary>
        /// Trait value unique identifier
        /// </summary>
        [SerializeField] public string value;
    }
}
