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

using System;
using System.Diagnostics;

internal static class OVRTelemetry
{
    internal static bool IsActive { get; set; } = OVRRuntimeSettings.Instance.TelemetryEnabled;
    private static readonly TelemetryClient InactiveClient = new NullTelemetryClient();
    private static readonly TelemetryClient ActiveClient = new QPLTelemetryClient();
    public static TelemetryClient Client => IsActive ? ActiveClient : InactiveClient;

    public readonly struct MarkerPoint : IDisposable
    {
        public int NameHandle { get; }

        public MarkerPoint(string name)
        {
            Client.CreateMarkerHandle(name, out var nameHandle);
            NameHandle = nameHandle;
        }

        public void Dispose()
        {
            Client.DestroyMarkerHandle(NameHandle);
        }
    }

    public abstract class TelemetryClient
    {
        public abstract void MarkerStart(int markerId, int instanceKey = OVRPlugin.Qpl.DefaultInstanceKey,
            long timestampMs = OVRPlugin.Qpl.AutoSetTimestampMs);

        public abstract void MarkerPointCached(int markerId, int nameHandle,
            int instanceKey = OVRPlugin.Qpl.DefaultInstanceKey, long timestampMs = OVRPlugin.Qpl.AutoSetTimestampMs);

        public abstract void MarkerAnnotation(int markerId, string annotationKey,
            string annotationValue, int instanceKey = OVRPlugin.Qpl.DefaultInstanceKey);

        public abstract void MarkerEnd(int markerId,
            OVRPlugin.Qpl.ResultType resultTypeId = OVRPlugin.Qpl.ResultType.Success,
            int instanceKey = OVRPlugin.Qpl.DefaultInstanceKey, long timestampMs = OVRPlugin.Qpl.AutoSetTimestampMs);

        public abstract bool CreateMarkerHandle(string name, out int nameHandle);
        public abstract bool DestroyMarkerHandle(int nameHandle);
    }

    private class NullTelemetryClient : TelemetryClient
    {
        public override void MarkerStart(int markerId, int instanceKey = OVRPlugin.Qpl.DefaultInstanceKey,
            long timestampMs = OVRPlugin.Qpl.AutoSetTimestampMs)
        {
        }

        public override void MarkerPointCached(int markerId, int nameHandle,
            int instanceKey = OVRPlugin.Qpl.DefaultInstanceKey, long timestampMs = OVRPlugin.Qpl.AutoSetTimestampMs)
        {
        }

        public override void MarkerAnnotation(int markerId, string annotationKey,
            string annotationValue, int instanceKey = OVRPlugin.Qpl.DefaultInstanceKey)
        {
        }

        public override void MarkerEnd(int markerId,
            OVRPlugin.Qpl.ResultType resultTypeId = OVRPlugin.Qpl.ResultType.Success,
            int instanceKey = OVRPlugin.Qpl.DefaultInstanceKey, long timestampMs = OVRPlugin.Qpl.AutoSetTimestampMs)
        {
        }

        public override bool CreateMarkerHandle(string name, out int nameHandle)
        {
            nameHandle = default;
            return false;
        }

        public override bool DestroyMarkerHandle(int nameHandle) => false;
    }

    private class QPLTelemetryClient : TelemetryClient
    {
        public override void MarkerStart(int markerId, int instanceKey = OVRPlugin.Qpl.DefaultInstanceKey,
            long timestampMs = OVRPlugin.Qpl.AutoSetTimestampMs)
        {
            OVRPlugin.Qpl.MarkerStart(markerId, instanceKey, timestampMs);
        }

        public override void MarkerPointCached(int markerId, int nameHandle,
            int instanceKey = OVRPlugin.Qpl.DefaultInstanceKey, long timestampMs = OVRPlugin.Qpl.AutoSetTimestampMs)
        {
            OVRPlugin.Qpl.MarkerPointCached(markerId, nameHandle, instanceKey, timestampMs);
        }

        public override void MarkerAnnotation(int markerId, string annotationKey,
            string annotationValue, int instanceKey = OVRPlugin.Qpl.DefaultInstanceKey)
        {
            OVRPlugin.Qpl.MarkerAnnotation(markerId, annotationKey, annotationValue, instanceKey);
        }

        public override void MarkerEnd(int markerId,
            OVRPlugin.Qpl.ResultType resultTypeId = OVRPlugin.Qpl.ResultType.Success,
            int instanceKey = OVRPlugin.Qpl.DefaultInstanceKey, long timestampMs = OVRPlugin.Qpl.AutoSetTimestampMs)
        {
            OVRPlugin.Qpl.MarkerEnd(markerId, resultTypeId, instanceKey, timestampMs);
        }

        public override bool CreateMarkerHandle(string name, out int nameHandle)
        {
            return OVRPlugin.Qpl.CreateMarkerHandle(name, out nameHandle);
        }

        public override bool DestroyMarkerHandle(int nameHandle)
        {
            return OVRPlugin.Qpl.DestroyMarkerHandle(nameHandle);
        }
    }

    public static void SendEvent(int markerId, OVRPlugin.Qpl.ResultType result = OVRPlugin.Qpl.ResultType.Success)
    {
        Client.MarkerStart(markerId);
        Client.MarkerEnd(markerId, result);
    }

    public struct MarkerScope : IDisposable
    {
        private readonly int _markerId;
        private readonly int _instanceKey;
        private OVRPlugin.Qpl.ResultType _result;
        private static string _sdkVersionString;

        public MarkerScope(int markerId, int instanceKey = OVRPlugin.Qpl.DefaultInstanceKey,
            long timestampMs = OVRPlugin.Qpl.AutoSetTimestampMs)
        {
            _markerId = markerId;
            _instanceKey = instanceKey;
            _result = OVRPlugin.Qpl.ResultType.Success;
            Client.MarkerStart(markerId, instanceKey, timestampMs);
        }

        public void SetResult(OVRPlugin.Qpl.ResultType result)
        {
            _result = result;
        }

        public void AddPoint(MarkerPoint point)
        {
            Client.MarkerPointCached(_markerId, point.NameHandle, _instanceKey);
        }

        public void AddAnnotation(string annotationKey, string annotationValue)
        {
            Client.MarkerAnnotation(_markerId, annotationKey, annotationValue, _instanceKey);
        }

        public void AddSDKVersionAnnotation()
        {
            if (string.IsNullOrEmpty(_sdkVersionString))
            {
                _sdkVersionString = OVRPlugin.version.ToString();
            }

            AddAnnotation("sdk_version", _sdkVersionString);
        }

        public void Dispose()
        {
            Client.MarkerEnd(_markerId, _result, _instanceKey);
        }
    }
}
