#ifndef SRSTANDARDPART_INCLUDED
#define SRSTANDARDPART_INCLUDED


#define SRSTANDARD_PART 1

float4 _MaterialColors[50];
float4 _MaterialData[50];
float4 _PartData[25];
// Per-part nozzle glow. x: nozzle wall temperature in Kelvin (0 = no glow). y: unused. z: inner-wall temperature in Kelvin.
float4 _PartNozzleData[25];
// Per-renderer nozzle data. xy: axial bounds; z: 1 on nozzle renderers.
float4 _NozzleAxis;
float  _EmissiveOverride = -1;
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
#include "Blackbody.cginc"


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
    // ids.w indexes per-part arrays; +0.25 guards against interpolation jitter and driver rounding.
    OUT.ids = float4(frac(v.uv1.w) * 100, floor(v.uv2.z), floor(v.uv2.w), floor(v.uv1.w) + 0.25);

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

    // Explicit floor: ids.x's fraction is the Trim4 glow marker and some GLES drivers round float array indices.
    int materialIndex = (int)floor(INPUT.ids.x);
    half4 color = _MaterialColors[materialIndex];
    color.a = (_AlphaOverride < 0 ? color.a : _AlphaOverride);

    half4 data = _MaterialData[materialIndex];
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

    // Thermal emission: partData.y is the part's glow temperature in Kelvin. BlackbodyEmission
    // gives the hue directly; a heat ramp scales it to HDR so only hot parts glow and bloom.
    float heat = max(0, partData.y - 700.0) / 1500.0;
    emission += BlackbodyEmission(partData.y) * BlackbodyIntensity(heat);

    // Glow only on nozzle renderers (_NozzleAxis.z) and only on authored nozzle-interior (Trim4) geometry.
    // That geometry is tagged paint-independently in uv1.w, surfacing here as frac(ids.x) ~0.7 (vs ~0.3 otherwise).
    float4 partNozzle = _PartNozzleData[INPUT.ids.w];
    UNITY_BRANCH
    if ((partNozzle.x > 670.0 || partNozzle.z > 670.0) && _NozzleAxis.z > 0.5 && frac(INPUT.ids.x) > 0.5)
    {
        float3 nozzleObjPos = mul(unity_WorldToObject, float4(INPUT.worldPosition.xyz, 1.0)).xyz;
        float along = saturate((nozzleObjPos.y - _NozzleAxis.x) / max(1e-4, _NozzleAxis.y - _NozzleAxis.x));

        float3 worldNormal = normalize(INPUT.worldNormal);
        float3 radialDir = mul((float3x3)unity_ObjectToWorld, float3(nozzleObjPos.x, 0.0, nozzleObjPos.z));
        float inner = saturate(dot(worldNormal, radialDir / max(1e-4, length(radialDir))) * -4.0);

        // Throat: down-facing disc at the top; the "along" gate excludes the down-facing exit lips.
        float3 axisDown = normalize(mul((float3x3)unity_ObjectToWorld, float3(0.0, -1.0, 0.0)));
        float throat = saturate(dot(worldNormal, axisDown)) * smoothstep(0.6, 0.9, along);

        float gradientTemp = lerp(670.0, partNozzle.x, along);
        float nozzleTemp = lerp(gradientTemp, max(gradientTemp, partNozzle.z), max(inner, throat));

        float nozzleHeat = max(0, nozzleTemp - 700.0) / 1500.0;
        emission += BlackbodyEmission(nozzleTemp) * BlackbodyIntensity(nozzleHeat) * (1.0 + throat * 2.0);
    }

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

        // partData.y carries Kelvin now, so rebuild the legacy 0..1 reentry strength from it.
        // The large scalar allows for a "white-hot" look to appear during extreme drag conditions.
        float reEntryStrength = saturate((partData.y - 670.0) / 1070.0);
        float reEntryMask = baseReEntryDot * _ReentryMaskBaseStrength * reEntryStrength * 10;
        float vaporTrailMask = baseVaporDot * _VaporMaskBaseStrength * partData.z;

        outColors.mask = half4(reEntryMask, vaporTrailMask, 0, 1);
    #endif
    return outColors;
}


#endif