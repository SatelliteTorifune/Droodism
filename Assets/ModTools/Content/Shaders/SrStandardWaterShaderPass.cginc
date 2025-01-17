#ifndef SRSTANDARDWATER_INCLUDED
#define SRSTANDARDWATER_INCLUDED


#define SRSTANDARD_WATER 1

#if WATER_NORMAL_MAPS_BLENDED
    #define DISTANCE_BLENDED_TEXTURES 1
    #undef DISTANCE_BLENDED_TEXTURES_FAST
#elif WATER_NORMAL_MAPS_BLENDED_FAST
    #define DISTANCE_BLENDED_TEXTURES 1
    #define DISTANCE_BLENDED_TEXTURES_FAST 1
#else
    #undef DISTANCE_BLENDED_TEXTURES
    #undef DISTANCE_BLENDED_TEXTURES_FAST
#endif

#if !defined(WATER_MOVEMENT_BI_DIRECTIONAL) && \
    !defined(WATER_MOVEMENT_OPPOSING_BI_DIRECTIONAL) && \
    !defined(WATER_MOVEMENT_OMNI_DIRECTIONAL)
    #define WATER_MOVEMENT_BI_DIRECTIONAL 1
#endif


static const float TangentEpsilon = 0.0001;

sampler2D _WaveNormalMap;
sampler2D _WaveNormalMap2;

#if REFLECTION || REFRACTION

    half _FresnelBias;

    #if REFLECTION
        sampler2D _WaterReflectionTexture;
        float4 _WaterReflectionTexture_TexelSize;
        half _ReflectionDistortionStrength;
        half _IsUnderWater;
    #endif

    #if REFRACTION
        sampler2D _LastCameraDepthTexture;
        sampler2D _WaterRefractionTexture;
        half _RefractionDistortionStrength;
        half _MaxTransparencyDepth;
        half3 _FoamColor;
        half _FoamDepthInverse;
    #endif

#endif

#if WAVES
    half _TessellationEdgeLength;
    half _WaveAmplitude;
    half _WaveLength;
    half _WaveSpeed;
    half _MaxDisplacementDist;
    float _WaveTime;
    float3 _WaveOffset;
#endif


#include "SrStandardConstants.cginc"
#include "SrStandardShaderData.cginc"
#include "Sr2ShaderStructures.cginc"
#include "SrStandardEffects.cginc"
#include "Utils.cginc"


// This combines ComputeNonStereoScreenPos and ComputeGrabScreenPos,
// storing the screenpos.y in the z-component (the original z is lost).
inline float4 ComputeCombinedScreenPosAndGrapPassPos(float4 pos) 
{
    #if UNITY_UV_STARTS_AT_TOP
    float scale = -1.0;
    #else
    float scale = 1.0;
    #endif

    float4 o = pos * 0.5f;
    o.xyz = float3(o.x, o.y * scale, o.y * _ProjectionParams.x) + o.w;
    o.w = pos.w;

    return o;
}

inline float2 RotateUV(float2 value)
{
    return float2(value.y, 1 - value.x);
}

inline fixed2 GetWaveNormal(sampler2D tex, float2 uv)
{
    // Note: This is intentionally not properly unpacking the normal.
    // We ignore the z component and skip the 2x - 1 modification
    // to save some work as this is handled elsewhere and is more optimized.
    #if defined(UNITY_NO_DXT5nm)
        return tex2D(tex, uv).rg;
    #else
        fixed4 n = tex2D(tex, uv);
        n.x *= n.w;
        return n.xy;
    #endif
}

inline half Fresnel(half3 viewVector, half3 worldNormal, half bias)
{
    half facing = saturate(1.0 - dot(viewVector, worldNormal));
    return saturate(bias + ((1.0 - bias) * facing * facing * facing));
}

#if REFRACTION
inline float GetDepth(float4 uvs, float fragDepth, half fade, half maxDepth)
{
    float depth = tex2Dproj(_LastCameraDepthTexture, UNITY_PROJ_COORD(uvs)).r;
    return lerp(LinearEyeDepth(depth) - fragDepth, maxDepth, fade);
}
inline void GetDepths(float4 uv, float4 uvDistorted, float fragDepth, half fade, half maxDepth, out float minDepth, out float undistortedDepth)
{
    float depth1 = tex2Dproj(_LastCameraDepthTexture, UNITY_PROJ_COORD(uv)).r;
    float depth2 = tex2Dproj(_LastCameraDepthTexture, UNITY_PROJ_COORD(uvDistorted)).r;

    #if UNITY_REVERSED_Z
        float depth = max(depth1, depth2);
    #else
        float depth = min(depth1, depth2);
    #endif

    minDepth = lerp(LinearEyeDepth(depth) - fragDepth, maxDepth, fade);
    undistortedDepth = LinearEyeDepth(depth1) - fragDepth;
}
#endif

inline half4 ApplyWaterEffects(v2f INPUT, fixed3 normal, half4 fragColor, float3 pixelDir, float pixelDist, out half3 indirectSpecular)
{
    indirectSpecular = 0;

    #if !REFRACTION && !REFLECTION

        return float4(fragColor.rgb, 1);

    #else 
    
        // Extract values packed in other places... see comments in vertex shader
        float fragDepth = INPUT.worldPosition.w;
        float4 screenPos = float4(INPUT.screenGrabPos.xz, 0, INPUT.screenGrabPos.w);

        #if REFRACTION

            half maxTransparencyDepth = _MaxTransparencyDepth * INPUT.uv3.y;
            half maxTransparencyDepthInverse = 1.0 / maxTransparencyDepth;

            // Calculate the UVs by distorting the screen position based on the fragment normal
            half2 refractionDistortion = normal.xy * _RefractionDistortionStrength;
            float4 refractionDistortionUVs = float4(screenPos.xy + refractionDistortion, screenPos.zw);

            // Calculate the depth fade so we can fade the depth to its maximum value before reaching the near camera far clip plane
            float depthFade = fragDepth * _ProjectionParams.w;

            // Calculate the depth for both the distorted and non-distorted fragment
            float minDepth;
            float undistortedDepth;
            GetDepths(screenPos, refractionDistortionUVs, fragDepth, depthFade, maxTransparencyDepth, minDepth, undistortedDepth);
    
            // Fade the refraction strength as we get closer to the surface
            half2 distortionWithFade = refractionDistortion * saturate(minDepth * maxTransparencyDepthInverse);

            // Calculate the UVs for the refraction and depth textures using the faded distortion
            float4 distortionFadeGrabpassUVs = float4(INPUT.screenGrabPos.xy + distortionWithFade, INPUT.screenGrabPos.zw);
            float4 distortionFadeUVs = float4(screenPos.xy + distortionWithFade, screenPos.zw);
        
            // Sample the grabpass texture to get the refraction color
            half3 refraction = tex2Dproj(_WaterRefractionTexture, UNITY_PROJ_COORD(distortionFadeGrabpassUVs)).rgb;
    
            // Calculate the water depth at this distorted fragment
            float depth = GetDepth(distortionFadeUVs, fragDepth, depthFade, maxTransparencyDepth);

            // Small hack to partially fix depth for water behind craft parts.
            // If craft is above water, distorted UV could sample craft position and get a negative depth.
            // This acts as if the water was at zero depth, making distorted water around above water craft parts look strange.
            // We hack around this by flipping the sign of the depth. 
            // The problem still occurs near the surface but begins to disappear the further above surface the craft parts are. 
            // Increasing the transparency depth will counteract some of this hacky fix.
            // To properly fix the issue, we might need to use 2 depth buffers, one before and one after craft rendering.
            depth *= sign(depth);
    
            // Calculate transparency strength, fading it out based on depth
            half transparencyDepthFade = saturate(depth * maxTransparencyDepthInverse);
            half transparencyStrength = INPUT.uv3.z - (INPUT.uv3.z * transparencyDepthFade * transparencyDepthFade);

            // Calculate the final refraction color
            half3 finalRefraction = lerp(fragColor.rgb, refraction, transparencyStrength).xyz;

        #else

            half3 finalRefraction = fragColor;

        #endif
            
        #if REFLECTION

            // Calculate the UVs by distorting the screen position based on the fragment normal
            half reflectionDistortionStrength = _WaterReflectionTexture_TexelSize.xy * (max(10, fragDepth) * _ReflectionDistortionStrength);
            half2 reflectionDistortion = normal.xy * reflectionDistortionStrength;
            float4 reflectionDistortionUVs = float4(screenPos.xy + reflectionDistortion, screenPos.zw);

            // Sample the reflection texture
            half3 reflection = tex2Dproj(_WaterReflectionTexture, UNITY_PROJ_COORD(reflectionDistortionUVs)).rgb;

            // Hang on to the reflection texture sample. 
            // We will use it as the indirect specular lighting later, rather than using a reflection probe.
            indirectSpecular = reflection;
            #if ATMOSPHERE
                half atmosphereReflectionFade = 1 - saturate((_cameraHeightAtmosPercent - 0.1) * 5);
            #else
                half atmosphereReflectionFade = smoothstep(1.05, 1.02, length(_adjustedCameraPosition));
            #endif

            // Fade the reflection out at a distance
            half reflectionStrength = INPUT.uv3.x * (1 - _IsUnderWater) * (1 - saturate((fragDepth - 50000) * 0.00002)) * atmosphereReflectionFade;
    
            // Fade the reflection out at an angle based on texture strength to emulate waves making the water rough
            reflectionStrength *= pow(saturate(1.2 - Fresnel(-pixelDir, INPUT.worldNormal, 0)), 0.35 * _ReflectionDistortionStrength);
    
            indirectSpecular *= reflectionStrength;

            // Blend the reflection texture based on reflection strength
            half3 finalReflection = lerp(fragColor.rgb, reflection, reflectionStrength).xyz;

        #else

            half3 finalReflection = fragColor;

        #endif

        // Blend between refraction and reflection based on the fresnel (unless we are underwater)
        #if UNDERWATER
            half4 finalColor = half4(finalRefraction, 1);
        #else
            half fresnel = Fresnel(-pixelDir, INPUT.worldNormal, _FresnelBias);
            half4 finalColor = half4(lerp(finalRefraction, finalReflection, fresnel), 1);
        #endif
            
        // Apply foam to the final color.
        // We can only do this if refraction is enabled as it gives us the depth information necessary to apply foam.
        #if REFRACTION
            undistortedDepth = max(0, undistortedDepth);
            half foamStrength = undistortedDepth < 0 ? 0 : (INPUT.uv3.w * (1 - saturate(undistortedDepth * _FoamDepthInverse)));
            half3 foam = _FoamColor * ((foamStrength * foamStrength * (1.0 - depthFade)));
            finalColor.rgb += foam;
        #endif

        return finalColor;

    #endif
}

#if WAVES

    float TessellationEdgeFactor(float3 p0, float3 p1) {

        float edgeLength = distance(p0, p1);

        float3 edgeCenter = (p0 + p1) * 0.5;
        float viewDistance = distance(edgeCenter, _WorldSpaceCameraPos);

        return edgeLength * _ScreenParams.y / (_TessellationEdgeLength * viewDistance);
    }

    TessellationFactors MyPatchConstantFunction(InputPatch<TessellationControlPoint, 3> patch) {

        float3 p0 = mul(unity_ObjectToWorld, patch[0].vertex).xyz;
        float3 p1 = mul(unity_ObjectToWorld, patch[1].vertex).xyz;
        float3 p2 = mul(unity_ObjectToWorld, patch[2].vertex).xyz;

        TessellationFactors f;
        f.edge[0] = TessellationEdgeFactor(p1, p2);
        f.edge[1] = TessellationEdgeFactor(p2, p0);
        f.edge[2] = TessellationEdgeFactor(p0, p1);
        f.inside = (TessellationEdgeFactor(p1, p2) + TessellationEdgeFactor(p2, p0) + TessellationEdgeFactor(p0, p1)) * (1 / 3.0);

        return f;
    }

    TessellationControlPoint vertTess(vertInput v) {
        return CreateTessellationControlPoint(v);
    }

    float GetWaveOffset(float3 samplePos, float waveReduction)
    {
            const float TwoPI = UNITY_PI * 2;
            float k = TwoPI / _WaveLength; 
            float speedTimesTime = _WaveSpeed * _WaveTime;

            float xOffset = sin(k * (samplePos.x - speedTimesTime));
            float yOffset = sin(k * (samplePos.y - speedTimesTime));
            float zOffset = sin(k * (samplePos.z - speedTimesTime));
            float waveOffset = _WaveAmplitude * (xOffset + yOffset + zOffset);

            waveOffset *= waveReduction;

            return waveOffset;
    }

    float3 GetSamplePos(float3 framePos)
    {
        return framePos - _WaveOffset;
    }

    void AdjustWaveNormals(float waveOffset, float waveReduction, float3 originalWorldVertPos, float3 samplePos, inout float3 worldNormal, inout float4 biTangent, inout float4 tangent)
    {
        // Current issues with this method:
        // 1. For small waves, the normals don't make enough visual difference, and we can't make large waves due to the water reflection plane, and quad seams between differing LOD levels.
        // 2. The lerping between original/modified bi/tangent vectors is visually apparent.

        const float normalOffset = .1;
        
        // Calculate where the new vertex will be in world coordinates.
        float3 worldVert = originalWorldVertPos + worldNormal * waveOffset;

        // Using the original tangent/bitangent, sample two points' wave offsets along those vectors to create approximate vertex neighbors to allow 
        // estimating the correct normal.
        float3 bitangentNeighbor = samplePos + biTangent.xyz * normalOffset;
        waveOffset = GetWaveOffset(bitangentNeighbor, waveReduction);
        bitangentNeighbor += worldNormal * waveOffset;

        float3 tangetNeighbor = samplePos + tangent.xyz * normalOffset;
        waveOffset = GetWaveOffset(tangetNeighbor, waveReduction);
        tangetNeighbor += worldNormal * waveOffset;

        // Calculate the vector from our vertex to the neighbors, which will be the new bi/tangent directions.
        biTangent = lerp(biTangent, float4(normalize(bitangentNeighbor - worldVert), 0), waveReduction);
        tangent = lerp(tangent, float4(normalize(tangetNeighbor - worldVert), 0), waveReduction);

        // Using the bi/tangent vectors, calculate the new normal vec.
        worldNormal = normalize(cross(biTangent, tangent));
    }

#endif

v2f vert(vertInput v)
{
        v2f OUT;
        UNITY_INITIALIZE_OUTPUT(v2f, OUT);
        OUT.worldNormal = NormalizePerVertexNormal(UnityObjectToWorldNormal(v.normal));
        OUT.worldPosition.xyz = mul(unity_ObjectToWorld, v.vertex);
        float3 camToVertex;
        float distToVertex; 

        // Calculate tangent and bitangent for normal mapping.
        // The vector crossed with the world normal to get the bitangent is slightly tweaked if the cross product would be zero.
        float3 bitangentGenerationVector = float3(1, 0, TangentEpsilon * (sign(1 - abs(OUT.worldNormal.x) - TangentEpsilon) - 1));
        OUT.bitangentDir = float4(normalize(cross(OUT.worldNormal, bitangentGenerationVector)), 0);
        OUT.tangentDir = float4(normalize(cross(OUT.worldNormal, OUT.bitangentDir)), 0);

        float waveAmplitudeScale = v.uv2.w * 2.55;
        
        // Unpack uv3 w channel
        half unpackUv3w = round(v.uv3.w * 255);
        v.uv2.w = floor(unpackUv3w * (1.0 / 11.0)) * (1.0 / 10.0);
        v.uv3.w = fmod(unpackUv3w, 11) * (10.0 / 255.0);

        OUT.uv2 = v.uv2;
        OUT.uv3 = v.uv3 * 2.55;

#if WAVES
        camToVertex = OUT.worldPosition.xyz - _worldCameraPosition;
        distToVertex = length(camToVertex);

        float waterDepth = v.vertColor.a;
        float distReduction = saturate(1 - (distToVertex / (min(_MaxDisplacementDist, _ProjectionParams.z))));
        float waterDepthReduction = saturate(v.vertColor.a / 20.0);
        float waveReduction = distReduction * waterDepthReduction;

        // Calculate the vertex offset due to waves, and apply.
        float3 samplePos = GetSamplePos(OUT.worldPosition.xyz);
        float waveOffset = GetWaveOffset(samplePos, waveReduction) * waveAmplitudeScale;
        v.vertex += float4(normalize(v.normal) * waveOffset, 0);

        // AdjustWaveNormals(waveOffset, waveReduction, OUT.worldPosition.xyz, samplePos, OUT.worldNormal, OUT.bitangentDir, OUT.tangentDir);

        // Update the water depth value, which is stored in the alpha channel, and recalculate the world position of the vertex.
        OUT.worldPosition.xyz = mul(unity_ObjectToWorld, v.vertex);
        v.vertColor.a += waveOffset;
#endif

        OUT.pos = UnityObjectToClipPos(v.vertex);

        float3 relativeVertPos = OUT.worldPosition.xyz - _planetCenter;
        float3 relativeVertDir = normalize(relativeVertPos);

        float atten = GetLightingAtten(relativeVertDir, GetLightDirection(OUT.worldPosition.xyz), 0);
        OUT.lightColor = GetLightColor(atten);
        camToVertex = OUT.worldPosition.xyz - _worldCameraPosition;
        distToVertex = length(camToVertex);
        half ambientScale = 1 - saturate(distToVertex * AmbientLightFadeRange);
        OUT.ambient = float4(VertexGI(OUT.worldPosition.xyz, OUT.worldNormal), max(_ambientLightMinFalloff, ambientScale * ambientScale));
        TRANSFER_SHADOW(OUT);
    
        OUT.vertColor = v.vertColor;

    GetAtmosphereDataForVertex(OUT);

    #if DISTANCE_BLENDED_TEXTURES

        #if DISTANCE_BLENDED_TEXTURES_FAST

            // Calculate the tiling levels and strengths
            float4 blendUVs;
            float4 blendStrengths;
            float4 blendData;
            float distToVert = length(OUT.worldPosition.xyz - _WorldSpaceCameraPos);
            CalculateDistanceBlendedTextureData(distToVert, v.uv, blendUVs, blendStrengths, blendData);

            // Pack the blend strength into the tangent and bitangent to avoid another interpolator
            OUT.tangentDir.w = blendStrengths.x;
            OUT.bitangentDir.w = blendStrengths.y;

            // Calculate the specularity and pack it into the ambient color to avoid another interpolator
            OUT.ambient.w = (blendData.z * blendStrengths.z) + (blendData.w * blendStrengths.w);

            // Calculate the wave movement
            float waveMovement1 = blendData.x * fmod(_Time.y, 1.0 / blendData.x);
            float waveMovement2 = blendData.y * fmod(_Time.y, 1.0 / blendData.y);

            // Calculate all 4 sets of UVs
            #if WATER_MOVEMENT_BI_DIRECTIONAL || WATER_MOVEMENT_OMNI_DIRECTIONAL
                OUT.distanceBlendedUVs.xy = waveMovement1 + blendUVs.xy;
                OUT.distanceBlendedRotatedUVs.xy = waveMovement1 + RotateUV(blendUVs.xy);
                OUT.distanceBlendedUVs.zw = waveMovement2 + blendUVs.zw;
                OUT.distanceBlendedRotatedUVs.zw = waveMovement2 + RotateUV(blendUVs.zw);
            #elif WATER_MOVEMENT_OPPOSING_BI_DIRECTIONAL
                OUT.distanceBlendedUVs.xy = waveMovement1 + blendUVs.xy;
                OUT.distanceBlendedRotatedUVs.xy = RotateUV(-waveMovement1 + blendUVs.xy);
                OUT.distanceBlendedUVs.zw = waveMovement2 + blendUVs.zw;
                OUT.distanceBlendedRotatedUVs.zw = RotateUV(-waveMovement2 + blendUVs.zw);
            #endif

        #else
            // All the work is done in the fragment shader to avoid artifacts caused by interpolation of UVs.
            OUT.uv = v.uv;
        #endif

    #else

        // Calculate the UVs here for a single lightweight texture lookup.
        float waveMovement = 0.01 * fmod(_Time.y, 100);
        OUT.uv.xy = waveMovement + v.uv.zw * 100;
        
        // Do some very basic fading of the normal map and specularity based on distance.
        float bumpStrength = 1 - saturate(distToVertex * 0.0002); 
        float specularStrength = 1 - saturate(distToVertex * 0.00005);
        OUT.uv.zw = float2(bumpStrength * bumpStrength, specularStrength * specularStrength);

    #endif

    #if REFRACTION || REFLECTION || BLEND_SCALED_SPACE
        // Ok, this part is ugly. We need the screen position and grab pass position but we are maxed out on interpolators.
        // Sometimes I think they are the same thing, but it appears not to be the case on some iOS devices that we've seen so far.
        // What we do is drop the z-component (don't think its used) and store grabpass xyw in xyw and screen position xyw in xzw.
        // Grabpass should be usable in texture projections lookups since its not using z. Screenpos will need to be extracted into
        // its own variable in the fragment shader, using x, z, and w.
        OUT.screenGrabPos = ComputeCombinedScreenPosAndGrapPassPos(OUT.pos);
    #endif

    #if REFRACTION || REFLECTION
        // Since grabpass/screenpos is using the z-component of screenGrabPos, we store the eye depth here instead.
        COMPUTE_EYEDEPTH(OUT.worldPosition.w);
    #endif

    return OUT;
}

half4 frag(v2f INPUT) : SV_Target
{
    // Initialize frag color to the vertex color
    half4 fragColor = INPUT.vertColor;

    float specularity = 0.6;

    float textureStrength = INPUT.uv2.w;

    float3 pixelDir;
    float pixelDist;
    GetPixelDir(INPUT.worldPosition.xyz, pixelDir, pixelDist);

    fixed3 tangentSpaceWaveNormal = fixed3(0, 0, 1);
    #if DISTANCE_BLENDED_TEXTURES

        #if DISTANCE_BLENDED_TEXTURES_FAST

            // Unpack the blend strengths from the tangent and bitanget.
            float2 blendStrengths = float2(INPUT.tangentDir.w, INPUT.bitangentDir.w);

            // Upack the specularity from the ambient light.
            specularity = INPUT.ambient.w;

            // Sample our normal maps twice for each tiling level. Once normally and once with rotated UVs.
            fixed2 waveTex1 = GetWaveNormal(_WaveNormalMap, INPUT.distanceBlendedUVs.xy);
            fixed2 waveTex1R = GetWaveNormal(_WaveNormalMap2, INPUT.distanceBlendedRotatedUVs.xy);
            fixed2 waveTex2 = GetWaveNormal(_WaveNormalMap, INPUT.distanceBlendedUVs.zw);
            fixed2 waveTex2R = GetWaveNormal(_WaveNormalMap2, INPUT.distanceBlendedRotatedUVs.zw);

            #if WATER_MOVEMENT_OMNI_DIRECTIONAL
                // Mode not supported, but this is here to prevent the shader from breaking.
                fixed2 waveTex1B = waveTex1;
                fixed2 waveTex1RB = waveTex1R;
                fixed2 waveTex2B = waveTex2;
                fixed2 waveTex2RB = waveTex2R;
            #endif

        #else

            float4 blendUVs;
            float4 blendStrengths;
            float4 blendData;
            CalculateDistanceBlendedTextureData(pixelDist, INPUT.uv, blendUVs, blendStrengths, blendData);

            // Calculate the specularity 
            specularity = (blendData.z * blendStrengths.z) + (blendData.w * blendStrengths.w);

            // Calculate the wave movement
            float waveMovement1 = blendData.x * fmod(_Time.y, 1.0 / blendData.x);
            float waveMovement2 = blendData.y * fmod(_Time.y, 1.0 / blendData.y);

            // Sample our normal maps twice for each tiling level. Once normally and once with rotated UVs.
            #if WATER_MOVEMENT_BI_DIRECTIONAL
                fixed2 waveTex1 = GetWaveNormal(_WaveNormalMap, waveMovement1 + blendUVs.xy);
                fixed2 waveTex1R = GetWaveNormal(_WaveNormalMap2, waveMovement1 + RotateUV(blendUVs.xy));
                fixed2 waveTex2 = GetWaveNormal(_WaveNormalMap, waveMovement2 + blendUVs.zw);
                fixed2 waveTex2R = GetWaveNormal(_WaveNormalMap2, waveMovement2 + RotateUV(blendUVs.zw));
            #elif WATER_MOVEMENT_OPPOSING_BI_DIRECTIONAL
                fixed2 waveTex1 = GetWaveNormal(_WaveNormalMap, waveMovement1 + blendUVs.xy);
                fixed2 waveTex1R = GetWaveNormal(_WaveNormalMap2, RotateUV(-waveMovement1 + blendUVs.xy));
                fixed2 waveTex2 = GetWaveNormal(_WaveNormalMap, waveMovement2 + blendUVs.zw);
                fixed2 waveTex2R = GetWaveNormal(_WaveNormalMap2, RotateUV(-waveMovement2 + blendUVs.zw));
            #elif WATER_MOVEMENT_OMNI_DIRECTIONAL
                fixed2 waveTex1 = GetWaveNormal(_WaveNormalMap, waveMovement1 + blendUVs.xy);
                fixed2 waveTex1R = GetWaveNormal(_WaveNormalMap2, waveMovement1 + RotateUV(blendUVs.xy));
                fixed2 waveTex1B = GetWaveNormal(_WaveNormalMap, -waveMovement1 + blendUVs.xy + float2(0, 0.5));
                fixed2 waveTex1RB = GetWaveNormal(_WaveNormalMap2, -waveMovement1 + RotateUV(blendUVs.xy + float2(0, 0.5)));
                fixed2 waveTex2 = GetWaveNormal(_WaveNormalMap, waveMovement2 + blendUVs.zw);
                fixed2 waveTex2R = GetWaveNormal(_WaveNormalMap2, waveMovement2 + RotateUV(blendUVs.zw));
                fixed2 waveTex2B = GetWaveNormal(_WaveNormalMap, -waveMovement2 + blendUVs.zw + float2(0, 0.5));
                fixed2 waveTex2RB = GetWaveNormal(_WaveNormalMap2, -waveMovement2 + RotateUV(blendUVs.zw + float2(0, 0.5)));
            #endif

        #endif
    
        // Combine our normal maps into a single tangent space normal.
        #if WATER_MOVEMENT_BI_DIRECTIONAL || WATER_MOVEMENT_OPPOSING_BI_DIRECTIONAL
            half2 wave1 = (waveTex1 + waveTex1R - fixed2(1.0, 1.0)) * blendStrengths.x;
            half2 wave2 = (waveTex2 + waveTex2R - fixed2(1.0, 1.0)) * blendStrengths.y;
            tangentSpaceWaveNormal = normalize(float3((wave1 + wave2) * textureStrength, 0.5));
        #elif WATER_MOVEMENT_OMNI_DIRECTIONAL
            half2 wave1 = (waveTex1 + waveTex1R + waveTex1B + waveTex1RB - fixed2(2.0, 2.0)) * blendStrengths.x;
            half2 wave2 = (waveTex2 + waveTex2R + waveTex2B + waveTex2RB - fixed2(2.0, 2.0)) * blendStrengths.y;
            tangentSpaceWaveNormal = normalize(float3((wave1 + wave2) * textureStrength, 1));
        #endif

        // Transform the tangent space normal into world space
        float3x3 tangentTransform = float3x3(INPUT.tangentDir.xyz, INPUT.bitangentDir.xyz, INPUT.worldNormal);
        INPUT.worldNormal = normalize(mul(tangentSpaceWaveNormal, tangentTransform));

    #else
    
        // Do a single normal map lookup and apply it to the world normal.
        float2 bump = (GetWaveNormal(_WaveNormalMap, INPUT.uv.xy) - 0.5) * INPUT.uv.z;
        tangentSpaceWaveNormal = normalize(float3(bump * textureStrength, 0.5));
        float3x3 tangentTransform = float3x3(INPUT.tangentDir.xyz, INPUT.bitangentDir.xyz, INPUT.worldNormal);
        INPUT.worldNormal = normalize(mul(tangentSpaceWaveNormal, tangentTransform));
        
        // Fade the specularity based on distance.
        specularity = 0.5 + (0.5 * INPUT.uv.w);

    #endif

    // Apply reflection and reflection if applicable
    half3 indirectSpecular = 0;
    fragColor = ApplyWaterEffects(INPUT, tangentSpaceWaveNormal, fragColor, pixelDir, pixelDist, indirectSpecular);

    // Get the material properties
    half smoothness = INPUT.uv2.x * specularity;
    half metallic = INPUT.uv2.y;
    half emissiveStrength = INPUT.uv2.z;

    // Compute emission and reduce base color based accordingly
    half3 emission = 0;
    if (emissiveStrength > 0)
    {
        emission = fragColor.rgb * emissiveStrength;
        fragColor.rgb *= 1 - saturate(emissiveStrength);
    }

    // Apply standard lighting and atmospheric effects
    fragColor = ApplyStandardLightingAndAtmosphere(fragColor, metallic, smoothness, emission, pixelDir, pixelDist, 1, INPUT, indirectSpecular);

    // Dither
    //fragColor.xyz = Dither(fragColor, float4(INPUT.worldPosition.xyz, 0), .05);

    #if BLEND_SCALED_SPACE
        half4 scaledSpace = tex2Dproj(_ScaledSpaceTerrainTexture, UNITY_PROJ_COORD(INPUT.screenGrabPos));
        return lerp(scaledSpace, fragColor, _quadToScaledTransition);
    #else
    return clamp(fragColor, 0, _maxColorValue);
    #endif
}

#if WAVES

    [UNITY_domain("tri")]
    [UNITY_outputcontrolpoints(3)]
    [UNITY_outputtopology("triangle_cw")]
    [UNITY_partitioning("fractional_odd")]
    [UNITY_patchconstantfunc("MyPatchConstantFunction")]
    TessellationControlPoint hull(InputPatch<TessellationControlPoint, 3> patch, uint id : SV_OutputControlPointID) {
        return patch[id];
    }

    [UNITY_domain("tri")]
    v2f domain(TessellationFactors factors, OutputPatch<TessellationControlPoint, 3> patch, float3 barycentricCoordinates : SV_DomainLocation) {
        vertInput data;

        #define DOMAIN_INTERPOLATE(fieldName) data.fieldName = \
		    patch[0].fieldName * barycentricCoordinates.x + \
		    patch[1].fieldName * barycentricCoordinates.y + \
		    patch[2].fieldName * barycentricCoordinates.z;

        DOMAIN_INTERPOLATE(vertex)
        DOMAIN_INTERPOLATE(normal)
        DOMAIN_INTERPOLATE(vertColor)
        DOMAIN_INTERPOLATE(uv)
        DOMAIN_INTERPOLATE(uv2)
        DOMAIN_INTERPOLATE(uv3)

        return vert(data);
    }

#endif

#endif