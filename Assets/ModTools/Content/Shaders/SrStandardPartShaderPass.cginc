#ifndef SRSTANDARDPART_INCLUDED
#define SRSTANDARDPART_INCLUDED


#define SRSTANDARD_PART 1

float4 _MaterialColors[50];
float4 _MaterialData[50];
float4 _PartData[25];
float  _EmissiveOverride;
float  _AlphaOverride = -1;
float  _IsFlightScene;
UNITY_DECLARE_TEX2DARRAY(_DetailTextures);
UNITY_DECLARE_TEX2DARRAY(_NormalMapTextures);

#if RIMSHADE_ON
    half3 _Color;
    half _MinPower;
    half _MaxPower;
#endif

#if DETAIL_TEXTURES_ON
    sampler2D _DecalTexture;
    float4 _DecalTexture_ST;
    float4 _DecalTextureMaterialIds;
    float _UseDecalTexture;
#endif

#if CRAFT_MASK_RENDER_ON
    float _ReentryMaskWrapAmount;
    float _VaporMaskWrapAmount;
    float _ReentryMaskBaseStrength;
    float _VaporMaskBaseStrength;
    float3 _playerCraftVelocityNormalized;
#endif


#include "SrStandardConstants.cginc"
#include "SrStandardShaderData.cginc"
#include "Sr2ShaderStructures.cginc"
#include "SrStandardEffects.cginc"
#include "Utils.cginc"


inline fixed3 GetNormal(half4 tex) 
{
    fixed3 normal;
    normal.xy = tex.ag * 2 - 1;
    normal.z = sqrt(1 - saturate(dot(normal.xy, normal.xy)));
    return normal;
}


v2f vert(vertInput v)
{
    UNITY_SETUP_INSTANCE_ID(v);
    InitializeVertexOutput(OUT);

    OUT.uv = float3((v.uv1.x * v.uv2.x) + frac(v.uv2.z), (v.uv1.y * v.uv2.y) + frac(v.uv2.w), v.uv1.z + 1);
    OUT.ids = float4(frac(v.uv1.w) * 100, floor(v.uv2.z), floor(v.uv2.w), v.uv1.w);

    #if NORMAL_MAPS_ON
        OUT.tangentDir.xyz = UnityObjectToWorldDir(v.tangent);
        OUT.bitangentDir.xyz = cross(OUT.worldNormal.xyz, OUT.tangentDir.xyz) * (v.tangent.w * unity_WorldTransformParams.w);
    #endif

    GetAtmosphereDataForVertex(OUT);

    return OUT;
}

struct FragmentOutput
{
    half4 color : SV_Target0;
    #if CRAFT_MASK_RENDER_ON
        half4 mask : SV_Target1;
    #endif
};

FragmentOutput frag(v2f INPUT)
{
    FragmentOutput outColors;

    // Lookup color and material data
    half4 color = _MaterialColors[INPUT.ids.x];
    color.a = (_AlphaOverride < 0 ? color.a : _AlphaOverride);

    half4 data = _MaterialData[INPUT.ids.x];
    float4 partData = _PartData[INPUT.ids.w];

    #if DETAIL_TEXTURES_ON || NORMAL_MAPS_ON
        // Calculate our texture UVs
        float2 uv = INPUT.uv.xy / INPUT.uv.z;
    #endif

    #if DETAIL_TEXTURES_ON
        half decalStrength = 0;
        half4 decal = 0;

        // Branch if we are using a decal
        UNITY_BRANCH
        if (_UseDecalTexture != 0)
        {
            float4 _dm = _DecalTextureMaterialIds;

            // Calculate the decal UVs
            float2 decalUV = (uv - _DecalTexture_ST.zw) * _DecalTexture_ST.xy;

            // Sample the decal texture
            decal = tex2D(_DecalTexture, decalUV);

            // The decal strength is used to lerp between original and decal colors/materials
            decalStrength = decal.a;

            // Calculate the material data values for the decal, lerping between original data and decal based on decal alpha
            half4 decalData = _dm.w >= 0 ? _MaterialData[_dm.w] : ((_MaterialData[_dm.x] * decal.r) + (_MaterialData[_dm.y] * decal.g) + (_MaterialData[_dm.z] * decal.b));
            data = lerp(data, decalData, decalStrength);

            // Calculate the color values for the decal
            decal = _dm.w >= 0 ? decal : ((_MaterialColors[_dm.x] * decal.r) + (_MaterialColors[_dm.y] * decal.g) + (_MaterialColors[_dm.z] * decal.b));
        }

        // Get the detail color and adjust our color accordingly
        half2 texDetail = UNITY_SAMPLE_TEX2DARRAY(_DetailTextures, float3(uv, INPUT.ids.y)).rg;
        color.rgb += (texDetail.r - 0.5019608) * data.z;
        color.rgb = saturate(color.rgb);

        // Lerp between regular color and decal color based on decal alpha
        color = lerp(color, decal, decalStrength);
        
        // Keep things in 0 to 1 range
        color = clamp(color, 0, 1);
    #endif

    // Compute emission and reduce base color based accordingly
    half emissionStrength = (_EmissiveOverride < 0 ? data.w : _EmissiveOverride);
    half3 emission = color.rgb * emissionStrength;
    color.rgb *= 1 - saturate(emissionStrength);

    // Update our normal based on the normal map
    #if NORMAL_MAPS_ON
        half4 texNormal = UNITY_SAMPLE_TEX2DARRAY(_NormalMapTextures, float3(uv, INPUT.ids.z));
        fixed3 localNormal = UnpackNormal(texNormal);
        localNormal.xy *= data.z;
        localNormal.z += 0.0001;
        float3x3 tangentTransform = float3x3(INPUT.tangentDir.xyz, INPUT.bitangentDir.xyz, INPUT.worldNormal);
        INPUT.worldNormal = normalize(mul(localNormal, tangentTransform));
    #endif

    float3 pixelDir;
    float pixelDist;
    GetPixelDir(INPUT.worldPosition.xyz, pixelDir, pixelDist);

    // Apply standard lighting and atmospheric effects
    color = ApplyStandardLightingAndAtmosphere(color, data.x, data.y, emission, pixelDir, pixelDist, _atmosphereStrenghtAtCamera, INPUT);

    // Apply rim shading if needed
    #if RIMSHADE_ON
        half rimShadeMultiplier = _IsFlightScene > 0 ? saturate(partData.x) : 1.0;
        half rimShadeDot = max(0, _MaxPower - dot(INPUT.worldNormal, -pixelDir));
        half rimShadeStrength = max(_MinPower, rimShadeDot * rimShadeDot) * rimShadeMultiplier;
        color.rgb += rimShadeStrength * _Color;
    #endif
    
    outColors.color = color;

    #if CRAFT_MASK_RENDER_ON
        float reEntryDotProd = (dot(INPUT.worldNormal, _playerCraftVelocityNormalized) - 1) * length(_playerCraftVelocityNormalized) + 1;
        float vaporDotProd = dot(INPUT.worldNormal, -_playerCraftVelocityNormalized);

        // Smoothstep between a base value and 1 to allow the effect to reach around the sides a bit.
        float baseReEntryDot = smoothstep(_ReentryMaskWrapAmount, 1, reEntryDotProd);
        float baseVaporDot = smoothstep(_VaporMaskWrapAmount, 1, vaporDotProd);

        // Clamp between 0, 1
        //float mask = saturate(dotProd);

        // The large scalar allows for a "white-hot" look to appear during extreme drag conditions.
        float reEntryMask = baseReEntryDot * _ReentryMaskBaseStrength * partData.y * 10;
        float vaporTrailMask = baseVaporDot * _VaporMaskBaseStrength * partData.z;

        outColors.mask = half4(reEntryMask, vaporTrailMask, 0, 1);
    #endif
    return outColors;
}


#endif