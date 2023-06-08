/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Meta.WitAi.TTS.Utilities;

namespace Meta.WitAi.TTS.Interfaces
{
    public interface ISpeakerTextPreprocessor
    {

        /// <summary>
        /// Called before prefix/postfix modifications are applied to the input string
        /// </summary>
        /// <param name="speaker">The speaker that will be used to speak the resulting text</param>
        /// <param name="text">The current text that will be used for speech</param>
        /// <returns>If false is returned, the calling speak operation will be cancelled</returns>
        bool OnPreprocessTTS(TTSSpeaker speaker, ref string text);
    }
    
    public interface ISpeakerTextPostprocessor
    {
        /// <summary>
        /// Called after prefix/postfix modifications are applied to the input string
        /// </summary>
        /// <param name="speaker"></param>
        /// <param name="text"></param>
        /// <returns>If false is returned, the calling speak operation will be cancelled</returns>
        bool OnPostprocessTTS(TTSSpeaker speaker, ref string text);
    }
}
