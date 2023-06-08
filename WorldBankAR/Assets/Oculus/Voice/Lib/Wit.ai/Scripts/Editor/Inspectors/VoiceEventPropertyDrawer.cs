/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using UnityEditor;

namespace Meta.WitAi.Events.Editor
{
    [CustomPropertyDrawer(typeof(VoiceEvents))]
    public class VoiceEventPropertyDrawer : EventPropertyDrawer<VoiceEvents>
    {
    }
}
