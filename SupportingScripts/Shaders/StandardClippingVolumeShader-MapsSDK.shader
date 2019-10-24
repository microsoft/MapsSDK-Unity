// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

Shader "MapsSDK/StandardClippingVolumeShader"
{
    Properties
    {
        _Color("Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        LOD 200

        // Render the clipping volume's walls.
        Pass
        {
            Tags{ "RenderType" = "Opaque" "LightMode" = "ForwardBase" }

            CGPROGRAM

            #pragma require geometry

            #pragma vertex vert
            #pragma fragment frag
            #pragma geometry geom

            #pragma multi_compile __ ENABLE_ELEVATION_TEXTURE
            #pragma multi_compile __ ENABLE_MRTK_INTEGRATION

            #pragma multi_compile_fog
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight

            // Use shader model 3.0 target to get nicer looking lighting.
            #pragma target 3.0

            //#pragma enable_d3d11_debug_symbols

            #include "AutoLight.cginc"
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "ClippingVolume-MapsSDK.cginc"
            #include "ElevationOffset-MapsSDK.cginc"
            #include "MRTKIntegration-MapsSDK.cginc"

            struct appdata
            {
                float3 vertex : POSITION;
#if ENABLE_ELEVATION_TEXTURE
                float2 texcoord : TEXCOORD;
#endif

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2g
            {
                float4 pos : SV_POSITION;

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            struct g2f
            {
                float4 pos : SV_POSITION;
                float3 worldPosition : POSITION1;
                SHADOW_COORDS(1)
                UNITY_FOG_COORDS(2)

                UNITY_VERTEX_OUTPUT_STEREO
            };

            struct Triangle
            {
                g2f vertices[3];
            };

            // In some configurations, Unity expects TRANSFER_SHADOW to have a struct called v with a field named vertex.
            struct TransferShadowHelper
            {
                float4 vertex;
            };

            #define GET_VERT(tri, vertindex) tri.vertices[vertindex]

            v2g vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);

#if ENABLE_ELEVATION_TEXTURE
                float elevationOffset =
                    CalculateElevationOffset(
                        _ElevationTex,
                        v.texcoord,
                        _ElevationTexScaleAndOffset.x,
                        _ElevationTexScaleAndOffset.yz,
                        _ElevationTexScaleAndOffset.w);
                v.vertex.y += elevationOffset;
#endif

                v2g o = (v2g)0;
                UNITY_INITIALIZE_OUTPUT(v2g, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.pos = float4(v.vertex, 1);

                return o;
            }

            // If the tri needs to be clipped, introduces two triangles for the edge segment, i.e. 4 verts in a triangle strip. This is done
            // per visible clipping plane (2 planes), so 8 is maximum number of vertices. (Will likely only need 4 so an additional
            // optimization here could be to split the draw calls for each clipping plane, guaranteeing max vertex count of 4.)
            [maxvertexcount(8)]
            void geom(triangle v2g input[3], inout TriangleStream<g2f> OutputStream)
            {
                DEFAULT_UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input[0]);

                float3 worldPositions[3] =
                {
                    mul(unity_ObjectToWorld, input[0].pos).xyz,
                    mul(unity_ObjectToWorld, input[1].pos).xyz,
                    mul(unity_ObjectToWorld, input[2].pos).xyz
                };

                // Check if the tri is degenerate. If so, early out. Degenrate triangles will introduce weird rendering artifacts.
                if (distance(worldPositions[0], worldPositions[1]) == 0 ||
                    distance(worldPositions[0], worldPositions[2]) == 0 ||
                    distance(worldPositions[1], worldPositions[2]) == 0)
                {
                    return;
                }

                float3 planePosition = _ClippingVolumePosition.xyz;

                for (uint clippingPlaneId = 0; clippingPlaneId < 2; clippingPlaneId++)
                {
                    float3 planeNormal = _ClippingVolumeNormals[clippingPlaneId].xyz;

                    // The shader misbehaves when indexing the float4 so just use an if-else construct instead.
                    float clippingVolumeSize = 0.999 * (clippingPlaneId == 0 ? _ClippingVolumeSize.x : _ClippingVolumeSize.y);

                    float3 intersectingPositions[2] = { float3(0, 0, 0), float3(0, 0, 0) };
                    uint intersectionCount = 0;
                    for (uint vertexId = 0; vertexId < 3 && intersectionCount < 2; vertexId++)
                    {
                        // Solve for distance along the edge, from the inside vertex to the outside vertex, that intersects the plane.
                        float3 outsideVertexWorldPosition = worldPositions[vertexId];
                        float outsideVertexDistanceToPlane = dot(planeNormal, outsideVertexWorldPosition - planePosition) - clippingVolumeSize;
                        float3 insideVertexWorldPosition = worldPositions[(vertexId + 1) % 3];
                        float insideVertexDistanceToPlane = dot(planeNormal, insideVertexWorldPosition - planePosition) - clippingVolumeSize;
                        bool isIntersectingPlane = sign(insideVertexDistanceToPlane) != sign(outsideVertexDistanceToPlane);
                        if (isIntersectingPlane)
                        {
                            // Compute intersection.
                            float3 insideToOutsideVector = outsideVertexWorldPosition - insideVertexWorldPosition;
                            float3 directionToOutsideVertex = normalize(insideToOutsideVector);
                            float distanceToOutsideVertex = length(insideToOutsideVector);
                            float distanceToIntersection = distanceToOutsideVertex - (outsideVertexDistanceToPlane / dot(directionToOutsideVertex, planeNormal));
                            intersectingPositions[intersectionCount] = insideVertexWorldPosition + distanceToIntersection * directionToOutsideVertex;
                            intersectionCount++;
                        }
                    }

                    if (intersectionCount != 2)
                    {
                        // If only 1 edge is intersecting the plane, that's weird... Quit while we're ahead.
                        continue;
                    }

                    // Determine the order of the vertices we need to emit.
                    float3 intersection0To1 = normalize(intersectingPositions[0] - intersectingPositions[1]);
                    float3 frontFacingNormal = cross(intersection0To1, _ClippingVolumeUp);
                    bool isFrontFacing = dot(frontFacingNormal, _ClippingVolumeNormals[clippingPlaneId]) < 0;

                    // Upper edge 0.
                    g2f upperEdgeVertex0 = (g2f)0;
                    UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(input[0], upperEdgeVertex0);
                    if (isFrontFacing)
                    {
                        upperEdgeVertex0.worldPosition = intersectingPositions[0];
                    }
                    else
                    {
                        upperEdgeVertex0.worldPosition = intersectingPositions[1];
                    }
                    upperEdgeVertex0.pos = UnityWorldToClipPos(upperEdgeVertex0.worldPosition);
                    TransferShadowHelper v;
                    v.vertex = mul(unity_WorldToObject, float4(upperEdgeVertex0.worldPosition, 1.0));
                    TRANSFER_SHADOW(upperEdgeVertex0);
                    UNITY_TRANSFER_FOG(upperEdgeVertex0, upperEdgeVertex0.pos);

                    // Upper edge 1.
                    g2f upperEdgeVertex1 = (g2f)0;
                    UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(input[0], upperEdgeVertex1);
                    if (isFrontFacing)
                    {
                        upperEdgeVertex1.worldPosition = intersectingPositions[1];
                    }
                    else
                    {
                        upperEdgeVertex1.worldPosition = intersectingPositions[0];
                    }
                    upperEdgeVertex1.pos = UnityWorldToClipPos(upperEdgeVertex1.worldPosition);
                    v.vertex = mul(unity_WorldToObject, float4(upperEdgeVertex1.worldPosition, 1.0));
                    TRANSFER_SHADOW(upperEdgeVertex1);
                    UNITY_TRANSFER_FOG(upperEdgeVertex1, upperEdgeVertex1.pos);

                    // Lower edge 0.
                    g2f lowerEdgeVertex0 = (g2f)0;
                    {
                        UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(input[0], lowerEdgeVertex0);
                        float lowerEdgeVertex0DistanceToBase = max(dot(_ClippingVolumeUp, upperEdgeVertex0.worldPosition - planePosition), 0);
                        float3 lowerEdgeVertex0WorldPosition = upperEdgeVertex0.worldPosition - (lowerEdgeVertex0DistanceToBase * _ClippingVolumeUp);
                        lowerEdgeVertex0.worldPosition = lowerEdgeVertex0WorldPosition;
                        lowerEdgeVertex0.pos = UnityWorldToClipPos(lowerEdgeVertex0WorldPosition);
                        TransferShadowHelper v;
                        v.vertex = mul(unity_WorldToObject, float4(lowerEdgeVertex0.worldPosition, 1.0));
                        TRANSFER_SHADOW(lowerEdgeVertex0);
                        UNITY_TRANSFER_FOG(lowerEdgeVertex0, lowerEdgeVertex0.pos);
                    }

                    // Lower edge 1.
                    g2f lowerEdgeVertex1 = (g2f)0;
                    {
                        UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(input[0], lowerEdgeVertex1);
                        float lowerEdgeVertex1DistanceToBase = max(dot(_ClippingVolumeUp, upperEdgeVertex1.worldPosition - planePosition), 0);
                        float3 lowerEdgeVertex1WorldPosition = upperEdgeVertex1.worldPosition - (lowerEdgeVertex1DistanceToBase * _ClippingVolumeUp);
                        lowerEdgeVertex1.worldPosition = lowerEdgeVertex1WorldPosition;
                        lowerEdgeVertex1.pos = UnityWorldToClipPos(lowerEdgeVertex1WorldPosition);
                        TransferShadowHelper v;
                        v.vertex = mul(unity_WorldToObject, float4(lowerEdgeVertex1.worldPosition, 1.0));
                        TRANSFER_SHADOW(lowerEdgeVertex1);
                        UNITY_TRANSFER_FOG(lowerEdgeVertex1, lowerEdgeVertex1.pos);
                    }

                    OutputStream.Append(upperEdgeVertex0);
                    OutputStream.Append(lowerEdgeVertex0);
                    OutputStream.Append(upperEdgeVertex1);
                    OutputStream.Append(lowerEdgeVertex1);
                    OutputStream.RestartStrip();
                }
            }

            fixed4 frag(g2f i) : SV_Target
            {
                // Because we sample from a fullscreen texture (the shadow map), don't forget to setup the eye index.
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                // Discard pixels outside the clipping volume.
                float3 planePosition = _ClippingVolumePosition;
                float distanceToPlane1 = abs(dot(_ClippingVolumeNormals[0].xyz, i.worldPosition - planePosition)) - _ClippingVolumeSize.x;
                float distanceToPlane2 = abs(dot(_ClippingVolumeNormals[1].xyz, i.worldPosition - planePosition)) - _ClippingVolumeSize.y;

                if (distanceToPlane1 > 0.00001 || distanceToPlane2 > 0.00001)
                {
                    discard;
                }

                // Get the normal of this fin, based on closest clipping plane normal.
                float3 normal = _ClippingVolumeNormals[0].xyz;
                if (distanceToPlane1 < distanceToPlane2)
                {
                    normal = _ClippingVolumeNormals[1].xyz;
                }

                // Add a small amount of shading based on the camera direction.
                float shadeFactor = min(0.5 * saturate(dot(normalize(_WorldSpaceLightPos0.xyz), normal)) + 0.5, max(SHADOW_ATTENUATION(i), 0.5));

                fixed4 finalColor = fixed4(_ClippingVolumeColor.rgb * shadeFactor, _ClippingVolumeColor.a);

                // MRTK hover light.
                finalColor = ApplyHoverLight(i.worldPosition.xyz, finalColor);

                // Apply fog.
                UNITY_APPLY_FOG(i.fogCoord, finalColor);

                return finalColor;
            }

            ENDCG
        }

        // Custom shadow caster rendering pass to handle clipping volume.
        Pass
        {
            Tags { "RenderType" = "Opaque" "LightMode" = "ShadowCaster" }


            CGPROGRAM

            #pragma require geometry

            #pragma vertex vert
            #pragma fragment frag
            #pragma geometry geom

            // These are the map specific keywords...
            #pragma multi_compile __ ENABLE_ELEVATION_TEXTURE

            #pragma multi_compile_shadowcaster

            #include "UnityCG.cginc"
            #include "ClippingVolume-MapsSDK.cginc"
            #include "ElevationOffset-MapsSDK.cginc"

            struct appdata
            {
                float3 vertex : POSITION;
#if ENABLE_ELEVATION_TEXTURE
                float2 texcoord: TEXCOORD;
#endif

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2g
            {
                float4 vertex : SV_POSITION;

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            struct g2f
            {
                V2F_SHADOW_CASTER;

                float3 worldPosition : POSITION2;

                UNITY_VERTEX_OUTPUT_STEREO
            };

            struct Triangle
            {
                g2f vertices[3];
            };

            struct Transfer
            {
                float4 vertex;
                float3 normal;
            };

            #define GET_VERT(tri, vertindex) tri.vertices[vertindex]

            v2g vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);

#if ENABLE_ELEVATION_TEXTURE
                float elevationOffset =
                    CalculateElevationOffset(
                        _ElevationTex,
                        v.texcoord,
                        _ElevationTexScaleAndOffset.x,
                        _ElevationTexScaleAndOffset.yz,
                        _ElevationTexScaleAndOffset.w);
                v.vertex.y += elevationOffset;
#endif

                v2g o = (v2g)0;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.vertex = float4(v.vertex, 1);

                return o;
            }

#define TRANSFER_SHADOW_CASTER_FROM_WORLD_SPACE_VERT(opos, vertex) \
            opos = mul(UNITY_MATRIX_VP, float4(vertex, 1.0)); \
            opos = UnityApplyLinearShadowBias(opos);

            // If the tri needs to be clipped, introduces two triangles for the edge segment, i.e. 4 verts in a triangle strip. This is done
            // per visible clipping plane (2 planes), so 8 is maximum number of vertices. (Will likely only need 4 so an additional
            // optimization here could be to split the draw calls for each clipping plane, guaranteeing max vertex count of 4.)
            [maxvertexcount(8)]
            void geom(triangle v2g input[3], inout TriangleStream<g2f> OutputStream)
            {
                DEFAULT_UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input[0]);

                float3 worldPositions[3] =
                {
                    mul(unity_ObjectToWorld, input[0].vertex).xyz,
                    mul(unity_ObjectToWorld, input[1].vertex).xyz,
                    mul(unity_ObjectToWorld, input[2].vertex).xyz
                };

                // Check if the tri is degenerate. If so, early out. Degenrate triangles will introduce weird rendering artifacts.
                if (distance(worldPositions[0], worldPositions[1]) == 0 ||
                    distance(worldPositions[0], worldPositions[2]) == 0 ||
                    distance(worldPositions[1], worldPositions[2]) == 0)
                {
                    return;
                }

                float3 planePosition = _ClippingVolumePosition.xyz;

                for (uint clippingPlaneId = 0; clippingPlaneId < 2; clippingPlaneId++)
                {
                    float3 planeNormal = _ClippingVolumeNormals[clippingPlaneId].xyz;

                    // The shader misbehaves when indexing the float4 so just use an if-else construct instead.
                    float clippingVolumeSize = 0.999 * (clippingPlaneId == 0 ? _ClippingVolumeSize.x : _ClippingVolumeSize.y);

                    float3 intersectingPositions[2] = { float3(0, 0, 0), float3(0, 0, 0) };
                    uint intersectionCount = 0;
                    for (uint vertexId = 0; vertexId < 3 && intersectionCount < 2; vertexId++)
                    {
                        // Solve for distance along the edge, from the inside vertex to the outside vertex, that intersects the plane.
                        float3 outsideVertexWorldPosition = worldPositions[vertexId];
                        float outsideVertexDistanceToPlane = dot(planeNormal, outsideVertexWorldPosition - planePosition) - clippingVolumeSize;
                        float3 insideVertexWorldPosition = worldPositions[(vertexId + 1) % 3];
                        float insideVertexDistanceToPlane = dot(planeNormal, insideVertexWorldPosition - planePosition) - clippingVolumeSize;
                        bool isIntersectingPlane = sign(insideVertexDistanceToPlane) != sign(outsideVertexDistanceToPlane);
                        if (isIntersectingPlane)
                        {
                            // Compute intersection.
                            float3 insideToOutsideVector = outsideVertexWorldPosition - insideVertexWorldPosition;
                            float3 directionToOutsideVertex = normalize(insideToOutsideVector);
                            float distanceToOutsideVertex = length(insideToOutsideVector);
                            float distanceToIntersection = distanceToOutsideVertex - (outsideVertexDistanceToPlane / dot(directionToOutsideVertex, planeNormal));
                            intersectingPositions[intersectionCount] = insideVertexWorldPosition + distanceToIntersection * directionToOutsideVertex;
                            intersectionCount++;
                        }
                    }

                    if (intersectionCount != 2)
                    {
                        // If only 1 edge is intersecting the plane, that's weird... Quit while we're ahead.
                        continue;
                    }

                    // Determine the order of the vertices we need to emit.
                    float3 intersection0To1 = normalize(intersectingPositions[0] - intersectingPositions[1]);
                    float3 frontFacingNormal = cross(intersection0To1, _ClippingVolumeUp);
                    bool isFrontFacing = dot(frontFacingNormal, _ClippingVolumeNormals[clippingPlaneId]) < 0;

                    // Upper edge 0.
                    g2f upperEdgeVertex0 = (g2f)0;
                    UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(input[0], upperEdgeVertex0);
                    if (isFrontFacing)
                    {
                        upperEdgeVertex0.worldPosition = intersectingPositions[0];
                    }
                    else
                    {
                        upperEdgeVertex0.worldPosition = intersectingPositions[1];
                    }
                    TRANSFER_SHADOW_CASTER_FROM_WORLD_SPACE_VERT(upperEdgeVertex0.pos, upperEdgeVertex0.worldPosition);

                    // Upper edge 1.
                    g2f upperEdgeVertex1 = (g2f)0;
                    UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(input[0], upperEdgeVertex1);
                    if (isFrontFacing)
                    {
                        upperEdgeVertex1.worldPosition = intersectingPositions[1];
                    }
                    else
                    {
                        upperEdgeVertex1.worldPosition = intersectingPositions[0];
                    }
                    TRANSFER_SHADOW_CASTER_FROM_WORLD_SPACE_VERT(upperEdgeVertex1.pos, upperEdgeVertex1.worldPosition);

                    // Lower edge 0.
                    g2f lowerEdgeVertex0 = (g2f)0;
                    {
                        UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(input[0], lowerEdgeVertex0);
                        float lowerEdgeVertex0DistanceToBase = max(dot(_ClippingVolumeUp, upperEdgeVertex0.worldPosition - planePosition), 0);
                        float3 lowerEdgeVertex0WorldPosition = upperEdgeVertex0.worldPosition - (lowerEdgeVertex0DistanceToBase * _ClippingVolumeUp);
                        lowerEdgeVertex0.worldPosition = lowerEdgeVertex0WorldPosition;
                        TRANSFER_SHADOW_CASTER_FROM_WORLD_SPACE_VERT(lowerEdgeVertex0.pos, lowerEdgeVertex0.worldPosition);
                    }

                    // Lower edge 1.
                    g2f lowerEdgeVertex1 = (g2f)0;
                    {
                        UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(input[0], lowerEdgeVertex1);
                        float lowerEdgeVertex1DistanceToBase = max(dot(_ClippingVolumeUp, upperEdgeVertex1.worldPosition - planePosition), 0);
                        float3 lowerEdgeVertex1WorldPosition = upperEdgeVertex1.worldPosition - (lowerEdgeVertex1DistanceToBase * _ClippingVolumeUp);
                        lowerEdgeVertex1.worldPosition = lowerEdgeVertex1WorldPosition;
                        TRANSFER_SHADOW_CASTER_FROM_WORLD_SPACE_VERT(lowerEdgeVertex1.pos, lowerEdgeVertex1.worldPosition);
                    }

                    OutputStream.Append(upperEdgeVertex0);
                    OutputStream.Append(lowerEdgeVertex0);
                    OutputStream.Append(upperEdgeVertex1);
                    OutputStream.Append(lowerEdgeVertex1);
                    OutputStream.RestartStrip();
                }
            }

            float4 frag(g2f i) : SV_Target
            {
                // Discard pixels outside the clipping volume.
                float3 planePosition = _ClippingVolumePosition;
                float distanceToPlane1 = abs(dot(_ClippingVolumeNormals[0].xyz, i.worldPosition - planePosition)) - _ClippingVolumeSize.x;
                float distanceToPlane2 = abs(dot(_ClippingVolumeNormals[1].xyz, i.worldPosition - planePosition)) - _ClippingVolumeSize.y;

                if (distanceToPlane1 > 0.00001 || distanceToPlane2 > 0.00001)
                {
                    discard;
                }

                SHADOW_CASTER_FRAGMENT(i)
            }

            ENDCG
        }
    }
}
