#ifndef SRSTANDARDWATER_INCLUDED
#define SRSTANDARDWATER_INCLUDED


#define SRSTANDARD_OBJECT 1


half _metallicness;
half _smoothness;
half4 _colorMultiplier;
sampler2D _texture;
float4 _texture_ST;
sampler2D _normalMap;
float4 _normalMap_ST;
half3 _emissive;

#include "SrStandardConstants.cginc"
#include "SrStandardShaderData.cginc"
#include "Sr2ShaderStructures.cginc"
#include "SrStandardEffects.cginc"
#include "Utils.cginc"


v2f vert(vertInput v)
{
    InitializeVertexOutput(OUT);

    OUT.uv = v.uv;

    #if TERRAIN_STRUCTURE_NORMAL_MAPS_ON
        OUT.tangentDir.xyz = UnityObjectToWorldDir(v.tangent);
        OUT.bitangentDir.xyz = cross(OUT.worldNormal.xyz, OUT.tangentDir.xyz) * (v.tangent.w * unity_WorldTransformParams.w);
    #endif

    GetAtmosphereDataForVertex(OUT);

    return OUT;
}
            
float4 frag(v2f INPUT) : SV_Target
{
    // Initialize frag color to the texture color
    float2 texUV = (INPUT.uv - _texture_ST.zw) * _texture_ST.xy;
    half4 fragColor = tex2D(_texture, texUV);
    fragColor *= _colorMultiplier;

    // Apply the normal map
    #if TERRAIN_STRUCTURE_NORMAL_MAPS_ON
        float2 normalUV = (INPUT.uv - _normalMap_ST.zw) * _normalMap_ST.xy;
        half4 texNormal = tex2D(_normalMap, normalUV);
        fixed3 localNormal = UnpackNormal(texNormal);
        float3x3 tangentTransform = float3x3(INPUT.tangentDir.xyz, INPUT.bitangentDir.xyz, INPUT.worldNormal);
        INPUT.worldNormal = normalize(mul(localNormal, tangentTransform));
    #endif

    float3 pixelDir;
    float pixelDist;
    GetPixelDir(INPUT.worldPosition.xyz, pixelDir, pixelDist);

    // Apply standard lighting and atmospheric effects
    fragColor = ApplyStandardLightingAndAtmosphere(fragColor, _metallicness, _smoothness, _emissive, pixelDir, pixelDist, 1, INPUT);

    return fragColor;
}


#endif