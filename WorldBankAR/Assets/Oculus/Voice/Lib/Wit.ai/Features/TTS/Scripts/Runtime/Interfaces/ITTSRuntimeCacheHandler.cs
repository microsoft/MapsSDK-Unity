/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Meta.WitAi.TTS.Data;
using Meta.WitAi.TTS.Events;

namespace Meta.WitAi.TTS.Interfaces
{
    public interface ITTSRuntimeCacheHandler
    {
        /// <summary>
        /// Callback for clips being added to the runtime cache
        /// </summary>
        TTSClipEvent OnClipAdded { get; set; }
        /// <summary>
        /// Callback for clips being removed from the runtime cache
        /// </summary>
        TTSClipEvent OnClipRemoved { get; set; }

        /// <summary>
        /// Method for obtaining all cached clips
        /// </summary>
        TTSClipData[] GetClips();
        /// <summary>
        /// Method for obtaining a specific cached clip
        /// </summary>
        TTSClipData GetClip(string clipID);

        /// <summary>
        /// Method for adding a clip to the cache
        /// </summary>
        /// <param name="clipData"></param>
        /// <returns></returns>
        bool AddClip(TTSClipData clipData);
        /// <summary>
        /// Method for removing a clip from the cache
        /// </summary>
        void RemoveClip(string clipID);
    }
}
