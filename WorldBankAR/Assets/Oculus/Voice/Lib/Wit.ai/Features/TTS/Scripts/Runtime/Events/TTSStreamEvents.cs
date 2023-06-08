﻿/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Meta.WitAi.TTS.Data;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.WitAi.TTS.Events
{
    [Serializable]
    public class TTSClipEvent : UnityEvent<TTSClipData>
    {
    }
    [Serializable]
    public class TTSClipErrorEvent : UnityEvent<TTSClipData, string>
    {
    }

    [Serializable]
    public class TTSStreamEvents
    {
        [Tooltip("Called when a audio clip stream begins")]
        public TTSClipEvent OnStreamBegin = new TTSClipEvent();

        [Tooltip("Called when a audio clip is ready for playback")]
        public TTSClipEvent OnStreamReady = new TTSClipEvent();

        [Tooltip("Called if/when an audio clip is adjusted")]
        public TTSClipEvent OnStreamClipUpdate = new TTSClipEvent();

        [Tooltip("Called when a audio clip is completely loaded")]
        public TTSClipEvent OnStreamComplete = new TTSClipEvent();

        [Tooltip("Called when a audio clip stream has been cancelled")]
        public TTSClipEvent OnStreamCancel = new TTSClipEvent();

        [Tooltip("Called when a audio clip stream has failed")]
        public TTSClipErrorEvent OnStreamError = new TTSClipErrorEvent();
    }
}
