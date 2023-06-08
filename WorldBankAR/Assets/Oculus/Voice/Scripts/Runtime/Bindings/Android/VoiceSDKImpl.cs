﻿/*
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
using Meta.WitAi;
using Meta.WitAi.Configuration;
using Meta.WitAi.Events;
using Meta.WitAi.Interfaces;
using Meta.WitAi.Requests;
using Oculus.Voice.Core.Bindings.Android;
using Oculus.Voice.Interfaces;
using Debug = UnityEngine.Debug;

namespace Oculus.Voice.Bindings.Android
{
    // TODO: Fix VoiceSDKImpl to work with IVoiceRequest
    public class VoiceSDKImpl : BaseAndroidConnectionImpl<VoiceSDKBinding>,
        IPlatformVoiceService, IVCBindingEvents
    {
        private bool _isServiceAvailable = true;
        public Action OnServiceNotAvailableEvent;
        private IVoiceService _baseVoiceService;

        private bool _isActive;

        public VoiceSDKImpl(IVoiceService baseVoiceService) : base(
            "com.oculus.assistant.api.unity.immersivevoicecommands.UnityIVCServiceFragment")
        {
            _baseVoiceService = baseVoiceService;
        }

        public bool PlatformSupportsWit => service.PlatformSupportsWit && _isServiceAvailable;

        public bool Active => service.Active && _isActive;
        public bool IsRequestActive => service.IsRequestActive;
        public bool MicActive => service.MicActive;
        public void SetRuntimeConfiguration(WitRuntimeConfiguration configuration)
        {
            service.SetRuntimeConfiguration(configuration);
        }

        private VoiceSDKListenerBinding eventBinding;

        public ITranscriptionProvider TranscriptionProvider { get; set; }
        public bool CanActivateAudio()
        {
            return true;
        }

        public bool CanSend()
        {
            return true;
        }

        public override void Connect(string version)
        {
            base.Connect(version);
            eventBinding = new VoiceSDKListenerBinding(this, this);
            eventBinding.VoiceEvents.OnStoppedListening.AddListener(OnStoppedListening);
            service.SetListener(eventBinding);
            service.Connect();
            Debug.Log(
                $"Platform integration initialization complete. Platform integrations are {(PlatformSupportsWit ? "active" : "inactive")}");
        }

        public override void Disconnect()
        {
            base.Disconnect();
            if (null != eventBinding)
            {
                eventBinding.VoiceEvents.OnStoppedListening.RemoveListener(OnStoppedListening);
            }
        }

        private void OnStoppedListening()
        {
            _isActive = false;
        }

        public VoiceServiceRequest Activate(string text, WitRequestOptions requestOptions,
            VoiceServiceRequestEvents requestEvents)
        {
            eventBinding.VoiceEvents.OnRequestOptionSetup?.Invoke(requestOptions);
            service.Activate(text, requestOptions);
            return null;
        }

        public VoiceServiceRequest Activate(WitRequestOptions requestOptions,
            VoiceServiceRequestEvents requestEvents)
        {
            if (_isActive) return null;
            _isActive = true;
            eventBinding.VoiceEvents.OnRequestOptionSetup?.Invoke(requestOptions);
            service.Activate(requestOptions);
            return null;
        }

        public VoiceServiceRequest ActivateImmediately(WitRequestOptions requestOptions,
            VoiceServiceRequestEvents requestEvents)
        {
            if (_isActive) return null;
            _isActive = true;
            eventBinding.VoiceEvents.OnRequestOptionSetup?.Invoke(requestOptions);
            service.ActivateImmediately(requestOptions);
            return null;
        }

        public void Deactivate()
        {
            _isActive = false;
            service.Deactivate();
        }

        public void DeactivateAndAbortRequest()
        {
            _isActive = false;
            service.Deactivate();
        }

        public void DeactivateAndAbortRequest(VoiceServiceRequest request)
        {

        }

        public void OnServiceNotAvailable(string error, string message)
        {
            _isActive = false;
            _isServiceAvailable = false;
            OnServiceNotAvailableEvent?.Invoke();
        }

        public VoiceEvents VoiceEvents
        {
            get => _baseVoiceService.VoiceEvents;
            set => _baseVoiceService.VoiceEvents = value;
        }

        public TelemetryEvents TelemetryEvents
        {
            get => _baseVoiceService.TelemetryEvents;
            set => _baseVoiceService.TelemetryEvents = value;
        }
    }
}
