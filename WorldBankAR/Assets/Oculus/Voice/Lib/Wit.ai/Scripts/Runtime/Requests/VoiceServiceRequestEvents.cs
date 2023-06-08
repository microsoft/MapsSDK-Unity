/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Meta.Voice;
using UnityEngine.Events;

namespace Meta.WitAi.Requests
{
    [Serializable]
    public class VoiceServiceRequestEvent : UnityEvent<VoiceServiceRequest> {}

    [Serializable]
    public class VoiceServiceRequestEvents
        : NLPRequestEvents<VoiceServiceRequestEvent>
    {
    }
}
