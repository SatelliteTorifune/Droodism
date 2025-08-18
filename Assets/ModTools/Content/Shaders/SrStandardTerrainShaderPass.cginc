#ifndef SRSTANDARDTERRAIN_INCLUDED
#define SRSTANDARDTERRAIN_INCLUDED


#define SRSTANDARD_TERRAIN 1

#if DETAIL_SPLATMAP_4_TEXTURES || DETAIL_SPLATMAP_8_TEXTURES
    #define DETAIL_SPLATMAP 1
#endif

#if GROUND_DETAIL_SPLATMAP_4_TEXTURES || GROUND_DETAIL_SPLATMAP_8_TEXTURES
    #define GROUND_DETAIL_SPLATMAP 1
#endif

#if DETAIL_SPLATMAP || GROUND_DETAIL_SPLATMAP
    #define SPLATMAP1 1
#endif

#if DETAIL_SPLATMAP_8_TEXTURES || GROUND_DETAIL_SPLATMAP_8_TEXTURES
    #define SPLATMAP2 1
#endif

#if DETAIL_SPLATMAP
    #define DISTANCE_BLENDED_TEXTURES 1
#else
    #undef DISTANCE_BLENDED_TEXTURES_FAST
#endif


#if DETAIL_SPLATMAP
    sampler2D _detailSplatTexture1;
    #if DETAIL_SPLATMAP_8_TEXTURES
        sampler2D _detailSplatTexture2;
    #endif
#endif

#if GROUND_DETAIL_SPLATMAP
    float _groundDetailSplatTilingScale;
    sampler2D _groundDetailSplatTexture1;
    #if GROUND_DETAIL_SPLATMAP_8_TEXTURES
        sampler2D _groundDetailSplatTexture2;
    #endif
#endif

#include "SrStandardConstants.cginc"
#include "SrStandardShaderData.cginc"
#include "Sr2ShaderStructures.cginc"
#include "SrStandardEffects.cginc"
#include "Utils.cginc"


half4 GetDetailSplatmapsColor(v2f INPUT, float distToPixel)
{
    half4 result = half4(0, 0, 0, 0);

    #if DETAIL_SPLATMAP || GROUND_DETAIL_SPLATMAP
        half4 detailColor;
        half4 oneHalf = half4(0.5, 0.5, 0.5, 0.5);
        half detailColorValue = 0;
    #endif

    #if DETAIL_SPLATMAP

        #if DISTANCE_BLENDED_TEXTURES_FAST
            float4 blendUVs = INPUT.distanceBlendedUVs;
            float4 blendStrengths = INPUT.distanceBlendedStrengths;
        #else
            float4 blendUVs;
            float4 blendStrengths;
            float4 blendData; // Unused for terrain
            CalculateDistanceBlendedTextureData(distToPixel, INPUT.distanceBlendedUVs, blendUVs, blendStrengths, blendData);
        #endif

        detailColor  = (tex2D(_detailSplatTexture1, blendUVs.xy) - oneHalf) * (INPUT.splatmap1 * blendStrengths.x);
        detailColor += (tex2D(_detailSplatTexture1, blendUVs.zw) - oneHalf) * (INPUT.splatmap1 * blendStrengths.y);

        #if DETAIL_SPLATMAP_8_TEXTURES
            detailColor += (tex2D(_detailSplatTexture2, blendUVs.xy) - oneHalf) * (INPUT.splatmap2 * blendStrengths.x);
            detailColor += (tex2D(_detailSplatTexture2, blendUVs.zw) - oneHalf) * (INPUT.splatmap2 * blendStrengths.y);
        #endif
            
        detailColorValue = detailColor.r + detailColor.g + detailColor.b + detailColor.a;
        result += half4(detailColorValue, detailColorValue, detailColorValue, detailColorValue);

    #endif

    #if GROUND_DETAIL_SPLATMAP
            
        detailColor = (tex2D(_groundDetailSplatTexture1, INPUT.groundDetailUVs) - oneHalf) * INPUT.splatmap1;

        #if GROUND_DETAIL_SPLATMAP_8_TEXTURES
            detailColor += (tex2D(_groundDetailSplatTexture2, INPUT.groundDetailUVs) - oneHalf) * INPUT.splatmap2;
        #endif
            
        detailColorValue = detailColor.r + detailColor.g + detailColor.b + detailColor.a;
        result += half4(detailColorValue, detailColorValue, detailColorValue, detailColorValue);

    #endif

    return result;
}

v2f vert(vertInput v)
{
    InitializeVertexOutput(OUT);

    OUT.vertColor = v.vertColor;

    GetAtmosphereDataForVertex(OUT);

    #if SPLATMAP1
        OUT.splatmap1 = v.uv2;
        #if SPLATMAP2
            OUT.splatmap2 = v.uv3;
        #endif
    #endif

    #if DISTANCE_BLENDED_TEXTURES
        #if DISTANCE_BLENDED_TEXTURES_FAST
            float4 blendData; // Unused for terrain
            float distToVert = length(OUT.worldPosition.xyz - _WorldSpaceCameraPos);
            CalculateDistanceBlendedTextureData(distToVert, v.uv, OUT.distanceBlendedUVs, OUT.distanceBlendedStrengths, blendData);
        #else
            OUT.distanceBlendedUVs = v.uv;
        #endif
    #endif

    #if GROUND_DETAIL_SPLATMAP
        OUT.groundDetailUVs = v.uv.zw * _groundDetailSplatTilingScale;
    #endif

    #if BLEND_SCALED_SPACE
        OUT.screenGrabPos = ComputeGrabScreenPos(OUT.pos);
    #endif

    OUT.pbrData = v.uv4.xyz;

    return OUT;
}
            
float4 frag(v2f INPUT) : SV_Target
{
    float3 pixelDir;
    float pixelDist;
    GetPixelDir(INPUT.worldPosition.xyz, pixelDir, pixelDist);

    // Initialize frag color to the vertex color plus the splatmaps contribution
    half4 fragColor = INPUT.vertColor + GetDetailSplatmapsColor(INPUT, pixelDist);
    fragColor = max(fragColor, 0);

    // Get Metallic / Smoothness / Emission values
    half m = INPUT.pbrData.x;
    half s = INPUT.pbrData.y;
    half e = INPUT.pbrData.z;

    // Compute emission and reduce base color based accordingly
    half3 emission = 0;
    if (e > 0) 
    {
        emission = fragColor.rgb * e;
        fragColor.rgb *= 1 - saturate(e);
    }

    // Apply standard lighting and atmospheric effects
    fragColor = ApplyStandardLightingAndAtmosphere(fragColor, m, s, emission, pixelDir, pixelDist, 1, INPUT);

    // Dither
    //fragColor.xyz = Dither(fragColor, float4(INPUT.worldPosition.xyz, 0), .05);   

    // Show UVs
    //fragColor = half4((1 + INPUT.uv.x) / 2.0, (1 + INPUT.uv.y) / 2.0, 0, 1);

    #if BLEND_SCALED_SPACE
        half4 scaledSpace = tex2Dproj(_ScaledSpaceTerrainTexture, UNITY_PROJ_COORD(INPUT.screenGrabPos));
        fragColor = lerp(scaledSpace, fragColor, _quadToScaledTransition);
    #else
        fragColor = clamp(fragColor, 0, _maxColorValue);
    #endif

    // Draw terrain with alpha of 0.
    // This is used as a mask to prevent ambient occlusion on the terrain.
    fragColor.a = 0;

    return fragColor;
}

#endif