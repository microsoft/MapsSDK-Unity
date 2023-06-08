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

using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.Assertions;

public abstract class OVRProjectSetupTelemetryEvent
{
    public enum EventTypes
    {
        // Attention : Need to be kept in sync with QPL Event Ids
        Fix = 163058027,
        Option = 163058846,
        GoToSource = 163056520,
        Summary = 163063879,
        Open = 163056010,
        Close = 163056958,
        InteractionFlow = 163069594,
    }

    public enum AnnotationTypes
    {
        // Aligned with QPL Events Attributes
        Uid,
        Level,
        Type,
        Value,
        BuildTargetGroup,
        Group,
        Blocking,
        Count,
        Origin,
        TimeSpent,
        Interaction,
        ValueAfter,
        BuildTargetGroupAfter
    }

    public enum MarkerPoints
    {
        Process,
        Open,
        Interact,
        Close,
    }

    public enum ResultTypes
    {
        Success,
        Fail,
        Cancel
    }

    private EventTypes _type;
    private Dictionary<AnnotationTypes, string> _annotations;
    private ResultTypes _result = ResultTypes.Success;
    private bool _sent;

    protected OVRProjectSetupTelemetryEvent()
    {
    }

    protected virtual void Setup(EventTypes type)
    {
        _type = type;
        _annotations = new Dictionary<AnnotationTypes, string>();
    }

    public virtual OVRProjectSetupTelemetryEvent AddAnnotation(AnnotationTypes annotation, string value)
    {
        _annotations[annotation] = value;
        return this;
    }

    public virtual OVRProjectSetupTelemetryEvent AddPoint(MarkerPoints point)
    {
        return this;
    }

    public virtual OVRProjectSetupTelemetryEvent SetResult(ResultTypes result)
    {
        _result = result;
        return this;
    }

    public virtual void Send()
    {
        Log();
        _sent = true;
    }

    [Conditional("OVR_TELEMETRY_LOG")]
    private void Log()
    {
        UnityEngine.Debug.Log($"Telemetry Event Sent : {_type}");
    }

    public static OVRProjectSetupTelemetryEvent Start(EventTypes type)
    {
        return Create<OVRProjectSetupTelemetryEventReal>(type);
    }

    private static OVRProjectSetupTelemetryEvent Create<T>(EventTypes type)
        where T : OVRProjectSetupTelemetryEvent, new()
    {
        var telemetryEvent = new T();
        telemetryEvent.Setup(type);
        return telemetryEvent;
    }

#if OVRPLUGIN_TESTING
    protected static Queue<OVRProjectSetupTelemetryEventReal> _historyQueue = null;
    protected static Queue<OVRProjectSetupTelemetryEventExpected> _expectedQueue = null;

    public static OVRProjectSetupTelemetryEvent Expect(EventTypes type)
    {
        return Create<OVRProjectSetupTelemetryEventExpected>(type);
    }

    public static void Mock()
    {
        _historyQueue = new Queue<OVRProjectSetupTelemetryEventReal>();
        _expectedQueue = new Queue<OVRProjectSetupTelemetryEventExpected>();
    }

    public static void Unmock()
    {
        _historyQueue = null;
        _expectedQueue = null;
    }

    public static void TestExpectations()
    {
        Assert.AreEqual(_historyQueue.Count, _expectedQueue.Count);
        while ((_historyQueue?.Count ?? 0) > 0)
        {
            var actualMarker = _historyQueue.Dequeue();
            var expectedMarker = _expectedQueue.Dequeue();
            TestExpectation(expectedMarker, actualMarker);
        }
    }

    private static void TestExpectation(OVRProjectSetupTelemetryEventExpected expected,
        OVRProjectSetupTelemetryEvent actual)
    {
        Assert.AreEqual(true, actual._sent);
        Assert.AreEqual(expected._type, actual._type);
        Assert.AreEqual(expected._result, actual._result);
        foreach (var annotation in expected._annotations)
        {
            Assert.AreEqual(true, actual._annotations.TryGetValue(annotation.Key, out var value));
            Assert.AreEqual(annotation.Value, value);
        }
    }
#endif
}

public class OVRProjectSetupTelemetryEventReal : OVRProjectSetupTelemetryEvent
{
    private OVRTelemetry.MarkerScope _marker;

    private static readonly Dictionary<MarkerPoints, OVRTelemetry.MarkerPoint> _markerPoints =
        new Dictionary<MarkerPoints, OVRTelemetry.MarkerPoint>();

    private static OVRTelemetry.MarkerPoint GetMarkerPoint(MarkerPoints markerPointEnum)
    {
        if (!_markerPoints.TryGetValue(markerPointEnum, out var markerPoint))
        {
            markerPoint = new OVRTelemetry.MarkerPoint(markerPointEnum.ToString());
            _markerPoints.Add(markerPointEnum, markerPoint);
        }

        return markerPoint;
    }

    protected override void Setup(EventTypes type)
    {
        base.Setup(type);
        _marker = new OVRTelemetry.MarkerScope((int)type);
    }

    public override OVRProjectSetupTelemetryEvent AddAnnotation(AnnotationTypes annotation, string value)
    {
        _marker.AddAnnotation(annotation.ToString(), value);
        return base.AddAnnotation(annotation, value);
    }

    public override OVRProjectSetupTelemetryEvent AddPoint(MarkerPoints point)
    {
        var markerPoint = GetMarkerPoint(point);
        _marker.AddPoint(markerPoint);
        return base.AddPoint(point);
    }

    public override OVRProjectSetupTelemetryEvent SetResult(ResultTypes result)
    {
        var resultQpl = result switch
        {
            ResultTypes.Fail => OVRPlugin.Qpl.ResultType.Fail,
            ResultTypes.Cancel => OVRPlugin.Qpl.ResultType.Cancel,
            _ => OVRPlugin.Qpl.ResultType.Success
        };
        _marker.SetResult(resultQpl);
        return base.SetResult(result);
    }

    public override void Send()
    {
#if OVRPLUGIN_TESTING
        _historyQueue?.Enqueue(this);
#endif
        _marker.Dispose();
        base.Send();
    }
}

#if OVRPLUGIN_TESTING
public class OVRProjectSetupTelemetryEventExpected : OVRProjectSetupTelemetryEvent
{
    protected override void Setup(EventTypes type)
    {
        base.Setup(type);
        _expectedQueue?.Enqueue(this);
    }
}
#endif
