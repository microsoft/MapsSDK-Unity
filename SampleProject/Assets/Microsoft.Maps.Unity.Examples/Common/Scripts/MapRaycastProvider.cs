// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Maps.Unity;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Physics;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Provides an implementation of MRTK's RaycastProvider which allows for MapRenderer surface geometry
/// to be utilized during ray casts. Only MapRenderers which have been regsitered with this instance
/// are considered for raycasting, see <see cref="MapRaycastProviderRegistration"/>.
/// </summary>
public class MapRaycastProvider : BaseCoreSystem, IMixedRealityRaycastProvider
{
    private readonly List<MapRenderer> _mapRenderers = new List<MapRenderer>();

    public MapRaycastProvider(MixedRealityInputSystemProfile profile) : base(profile)
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
            MixedRealityRaycaster.RaycastSimplePhysicsStep(
                step,
                prioritizedLayerMasks,
                focusIndividualCompoundCollider,
                out RaycastHit physicsHit);

        MapRendererRaycastHit? closestMapHitInfo = null;
        MapRenderer closestMapRenderer = null;
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
                        if (!closestMapHitInfo.HasValue || closestMapHitInfo.Value.Distance > mapHitInfo.Distance)
                        {
                            closestMapRenderer = mapRenderer;
                            closestMapHitInfo = mapHitInfo;
                        }
                    }
                }
                else
                {
                    if (!closestMapHitInfo.HasValue || closestMapHitInfo.Value.Distance > mapHitInfo.Distance)
                    {
                        closestMapRenderer = mapRenderer;
                        closestMapHitInfo = mapHitInfo;
                    }
                }
            }
        }

        if (closestMapHitInfo != null)
        {
            hitInfo = new MixedRealityRaycastHit();
            var mapRendererHitInfo = closestMapHitInfo.Value;
            hitInfo.distance = mapRendererHitInfo.Distance;
            hitInfo.point = mapRendererHitInfo.Point;
            hitInfo.normal = mapRendererHitInfo.Normal;
            hitInfo.transform = closestMapRenderer.transform;
            return true;
        }
        else
        {
            hitInfo = new MixedRealityRaycastHit(hasPhysicsHit, physicsHit);
            return hasPhysicsHit;
        }
    }

    public bool SphereCast(
        RayStep step,
        float radius,
        LayerMask[] prioritizedLayerMasks,
        bool focusIndividualCompoundCollider,
        out MixedRealityRaycastHit hitInfo)
    {
        // For now, this is just using the default behavior for sphere cast.
        // Leaving MapRenderer integration for a future change.

        var result =
            MixedRealityRaycaster.RaycastSpherePhysicsStep(
                step,
                radius,
                step.Length,
                prioritizedLayerMasks,
                focusIndividualCompoundCollider,
                out RaycastHit physicsHit);
        hitInfo = new MixedRealityRaycastHit(result, physicsHit);
        return result;
    }

    public RaycastResult GraphicsRaycast(EventSystem eventSystem, PointerEventData pointerEventData, LayerMask[] layerMasks)
    {
        return eventSystem.Raycast(pointerEventData, layerMasks);
    }
}
