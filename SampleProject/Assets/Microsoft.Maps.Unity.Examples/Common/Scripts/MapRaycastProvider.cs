// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Maps.Unity;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Physics;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MapRaycastProvider : BaseCoreSystem, IMixedRealityRaycastProvider
{
    private List<MapRenderer> _mapRenderers = new List<MapRenderer>();

    public MapRaycastProvider(
        IMixedRealityServiceRegistrar registrar,
        MixedRealityInputSystemProfile profile) : base(registrar, profile)
    {
    }

    public void RegisterMapRenderer(MapRenderer mapRenderer)
    {
        if (!_mapRenderers.Contains(mapRenderer))
        {
            _mapRenderers.Add(mapRenderer);
        }
    }

    public void UnregisterMapRenderer(MapRenderer mapRenderer)
    {
        _mapRenderers.Remove(mapRenderer);
    }

    public bool Raycast(
        RayStep step,
        LayerMask[] prioritizedLayerMasks,
        bool focusIndividualCompoundCollider,
        out MixedRealityRaycastHit hitInfo)
    {
        var hasPhysicsHit =
            MixedRealityRaycaster.RaycastSimplePhysicsStep(step, prioritizedLayerMasks, focusIndividualCompoundCollider, out RaycastHit physicsHit);

        MapRendererRaycastHit? closerMapHitInfo = null;
        MapRenderer hitMapRenderer = null;
        foreach (var mapRenderer in _mapRenderers)
        {
            if (
                mapRenderer.Raycast(
                    step,
                    out var mapHitInfo,
                    hasPhysicsHit ? physicsHit.distance : step.Length))
            {
                if (hasPhysicsHit)
                {
                    if (physicsHit.distance > mapHitInfo.Distance)
                    {
                        if (!closerMapHitInfo.HasValue || closerMapHitInfo.Value.Distance > mapHitInfo.Distance)
                        {
                            hitMapRenderer = mapRenderer;
                            closerMapHitInfo = mapHitInfo;
                        }
                    }
                }
                else
                {
                    if (!closerMapHitInfo.HasValue || closerMapHitInfo.Value.Distance > mapHitInfo.Distance)
                    {
                        hitMapRenderer = mapRenderer;
                        closerMapHitInfo = mapHitInfo;
                    }
                }
            }
        }

        if (closerMapHitInfo != null)
        {
            hitInfo = new MixedRealityRaycastHit();
            var mapRendererHitInfo = closerMapHitInfo.Value;
            hitInfo.distance = mapRendererHitInfo.Distance;
            hitInfo.point = mapRendererHitInfo.Point;
            hitInfo.normal = mapRendererHitInfo.Normal;
            hitInfo.transform = hitMapRenderer.transform;
            return true;
        }
        else
        {
            hitInfo = new MixedRealityRaycastHit(hasPhysicsHit, physicsHit);
            return hasPhysicsHit;
        }
    }

    public bool SphereCast(RayStep step, float radius, LayerMask[] prioritizedLayerMasks, bool focusIndividualCompoundCollider, out MixedRealityRaycastHit hitInfo)
    {
        var result = MixedRealityRaycaster.RaycastSpherePhysicsStep(step, radius, step.Length, prioritizedLayerMasks, focusIndividualCompoundCollider, out RaycastHit physicsHit);
        hitInfo = new MixedRealityRaycastHit(result, physicsHit);
        return result;
    }

    public RaycastResult GraphicsRaycast(EventSystem eventSystem, PointerEventData pointerEventData, LayerMask[] layerMasks)
    {
        return eventSystem.Raycast(pointerEventData, layerMasks);
    }
}
