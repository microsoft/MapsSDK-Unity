﻿/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Meta.WitAi.Json;
using UnityEngine.Events;

namespace Meta.WitAi.Events
{
    [Serializable]
    public class WitResponseEvent : UnityEvent<WitResponseNode>
    {
    }
}
