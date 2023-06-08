/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Meta.WitAi.Events;
using Meta.WitAi.Interfaces;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Meta.WitAi.Dictation.Events
{
    [Serializable]
    public class DictationEvents : EventRegistry, ITranscriptionEvent, IAudioInputEvents
    {
        private const string EVENT_CATEGORY_TRANSCRIPTION_EVENTS = "Transcription Events";
        private const string EVENT_CATEGORY_MIC_EVENTS = "Mic Events";
        private const string EVENT_CATEGORY_DICTATION_EVENTS = "Dictation Events";
        private const string EVENT_CATEGORY_ACTIVATION_RESULT_EVENTS = "Activation Result Events";

        /// <summary>
        /// Triggered when a partial transcription is received. This can be received many times per activation.
        /// </summary>
        [EventCategory(EVENT_CATEGORY_TRANSCRIPTION_EVENTS)]
        [FormerlySerializedAs("OnPartialTranscription")]
        [Tooltip("Message fired when a partial transcription has been received.")]
        public WitTranscriptionEvent onPartialTranscription = new WitTranscriptionEvent();

        /// <summary>
        /// Triggered when an utterance has completed.
        /// </summary>
        [EventCategory(EVENT_CATEGORY_TRANSCRIPTION_EVENTS)]
        [FormerlySerializedAs("OnFullTranscription")]
        [Tooltip("Message received when a complete transcription is received.")]
        public WitTranscriptionEvent onFullTranscription = new WitTranscriptionEvent();

        /// <summary>
        /// Triggered when the service sends a response. This will fire for all OnPartialTranscription,
        /// OnFullTranscription, etc responses.
        /// </summary>
        [EventCategory(EVENT_CATEGORY_ACTIVATION_RESULT_EVENTS)]
        [Tooltip("Called when a response from Wit.ai has been received")]
        public WitResponseEvent onResponse = new WitResponseEvent();

        [EventCategory(EVENT_CATEGORY_ACTIVATION_RESULT_EVENTS)]
        [Tooltip(
            "Called when the activation is about to be aborted by a direct user interaction.")]
        public UnityEvent OnAborting = new UnityEvent();

        [EventCategory(EVENT_CATEGORY_ACTIVATION_RESULT_EVENTS)]
        [Tooltip(
            "Called when the activation stopped because the network request was aborted. This can be via a timeout or call to AbortActivation.")]
        public UnityEvent OnAborted = new UnityEvent();

        [EventCategory(EVENT_CATEGORY_ACTIVATION_RESULT_EVENTS)]
        [Tooltip(
            "Called when a a request has completed and all response and error callbacks have fired.")]
        public UnityEvent OnRequestCompleted = new UnityEvent();

        [EventCategory(EVENT_CATEGORY_ACTIVATION_RESULT_EVENTS)]
        [Tooltip(
            "Called when a a request has been created.")]
        public WitRequestCreatedEvent OnRequestCreated = new WitRequestCreatedEvent();

        /// <summary>
        /// Called when the microphone begins listening to capture transcriptions
        /// </summary>
        [Tooltip("Called when the microphone begins listening to capture transcriptions")]
        [EventCategory(EVENT_CATEGORY_MIC_EVENTS)]
        public UnityEvent onStart = new UnityEvent();

        /// <summary>
        /// Called when the microphone ends listening for transcriptions.
        /// </summary>
        [Tooltip("Called when the microphone ends listening for transcriptions.")]
        [EventCategory(EVENT_CATEGORY_MIC_EVENTS)]
        public UnityEvent onStopped = new UnityEvent();

        /// <summary>
        /// Called when the server returns an error or the request has timed out
        /// </summary>
        [Tooltip("Called when the server returns an error or the request has timed out")]
        [EventCategory(EVENT_CATEGORY_ACTIVATION_RESULT_EVENTS)]
        public WitErrorEvent onError = new WitErrorEvent();

        /// <summary>
        /// Called when an individual dictation session has started. This can include multiple server activations if
        /// dictation is set up to automatically reactivate when the server endpoints an utterance.
        /// </summary>
        [Tooltip("Called when an individual dictation session has started. This can include multiple server activations if dictation is set up to automatically reactivate when the server endpoints an utterance.")]
        [EventCategory(EVENT_CATEGORY_DICTATION_EVENTS)]
        public DictationSessionEvent onDictationSessionStarted = new DictationSessionEvent();

        /// <summary>
        /// Called when a dictation is completed after Deactivate has been called or auto-reactivate is disabled.
        /// </summary>
        [Tooltip("Called when a dictation is completed after Deactivate has been called or auto-reactivate is disabled.")]
        [EventCategory(EVENT_CATEGORY_DICTATION_EVENTS)]
        public DictationSessionEvent onDictationSessionStopped = new DictationSessionEvent();

        /// <summary>
        /// Called when mic audio level changes
        /// </summary>
        [Tooltip("Called when mic audio level changes")]
        [EventCategory(EVENT_CATEGORY_MIC_EVENTS)]
        public WitMicLevelChangedEvent onMicAudioLevel = new WitMicLevelChangedEvent();

        #region Shared Event API - Transcription

        /// <summary>
        /// Message fired when a partial transcription has been received.
        /// </summary>
        public WitTranscriptionEvent OnPartialTranscription => onPartialTranscription;
        /// <summary>
        /// Triggered when an utterance has completed.
        /// </summary>
        public WitTranscriptionEvent OnFullTranscription => onFullTranscription;

        #endregion

        #region Shared Event API - Microphone

        /// <summary>
        /// Called when mic audio level changes
        /// </summary>
        public WitMicLevelChangedEvent OnMicAudioLevelChanged => onMicAudioLevel;
        /// <summary>
        /// Called when the microphone begins listening to capture transcriptions
        /// </summary>
        public UnityEvent OnMicStartedListening => onStart;
        /// <summary>
        /// Called when the microphone ends listening for transcriptions.
        /// </summary>
        public UnityEvent OnMicStoppedListening => onStopped;

        #endregion
    }
}
