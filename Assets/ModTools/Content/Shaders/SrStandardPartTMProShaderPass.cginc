#ifndef SRSTANDARDPARTTMPRO_INCLUDED
#define SRSTANDARDPARTTMPRO_INCLUDED


#define SRSTANDARD_PART_TMPRO 1

float4 _MaterialColors[50];
float4 _MaterialData[50];
float4 _PartData[25];
float  _EmissiveOverride;

// UI Editable properties
uniform float		_FaceDilate;				// v[ 0, 1]

// API Editable properties
uniform float		_WeightNormal;
uniform float		_WeightBold;

uniform float		_ScaleRatioA;

uniform float		_VertexOffsetX;
uniform float		_VertexOffsetY;


uniform float		_OutlineSoftness;			// v[ 0, 1]
uniform sampler2D	_OutlineTex;				// RGBA : Color + Opacity
uniform float		_OutlineUVSpeedX;
uniform float		_OutlineUVSpeedY;
uniform fixed4		_OutlineColor;				// RGBA : Color + Opacity
uniform float		_OutlineWidth;				// v[ 0, 1]

// Font Atlas properties
uniform sampler2D	_MainTex;
uniform float		_TextureWidth;
uniform float		_TextureHeight;
uniform float 		_GradientScale;
uniform float		_ScaleX;
uniform float		_ScaleY;
uniform float		_PerspectiveFilter;
uniform float		_Sharpness;

float _AlphaCutoff;


#include "SrStandardConstants.cginc"
#include "SrStandardShaderData.cginc"
#include "Sr2ShaderStructures.cginc"
#include "SrStandardEffects.cginc"
#include "Utils.cginc"

#if SRSTANDARD_PART_TMPRO_SHADOWCASTER
    // Unused but needed to prevent compile errors from the non-shadowcaster vert/frag functions below.
    // Would be unnecessary if they didn't share the same file with shadow caster vert/frag functions.
    #if defined(TRANSFER_SHADOW)
        #undef TRANSFER_SHADOW
    #endif
    #define TRANSFER_SHADOW(OUT) OUT;
#endif


v2f vert(vertInput v)
{
    v.vertex.x += _VertexOffsetX;
    v.vertex.y += _VertexOffsetY;

    // Generate normal for backface
    float3 view = ObjSpaceViewDir(v.vertex);
    v.normal *= sign(dot(v.normal, view));

    InitializeVertexOutput(OUT);
    GetAtmosphereDataForVertex(OUT);

    // Part material ids are encoded in the vertex color
    // The alpha channel is the outline thickness.
    // The green channel also encodes the gradient lerp value (0 or 1)
    float gradientLerp = v.color.g >= 0.5 ? 1 : 0;
    v.color.g -= v.color.g >= 0.5 ? (128.0 / 255.0) : 0.0;
    OUT.ids = float4(v.color.r * 255 + 0.25, v.color.g * 255 + 0.25, v.color.b * 255 + 0.25, gradientLerp);
    OUT.param.z = v.color.a;

    float bold = step(v.uv2.y, 0);

    #if USE_DERIVATIVE
        ////data.param.y = 1;
    #else
        float4 vert = v.vertex;
        float4 vPosition = UnityObjectToClipPos(vert);
        float2 pixelSize = vPosition.w;

        pixelSize /= float2(_ScaleX, _ScaleY) * mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy);
        float scale = rsqrt(dot(pixelSize, pixelSize));
        scale *= abs(v.uv2.y) * _GradientScale * (_Sharpness + 1);
        scale = lerp(scale * (1 - _PerspectiveFilter), scale, abs(dot(UnityObjectToWorldNormal(v.normal.xyz), normalize(WorldSpaceViewDir(vert)))));
        OUT.param.y = scale;
    #endif

    OUT.param.x = (lerp(_WeightNormal, _WeightBold, bold) / 4.0 + _FaceDilate) * _ScaleRatioA * 0.5;

    OUT.uv = v.uv1;

    return OUT;
}

half4 frag(v2f INPUT) : SV_Target
{
    // Lookup color and material data
    half4 colorPrimary = _MaterialColors[INPUT.ids.x];
    half4 colorTrim1 = _MaterialColors[INPUT.ids.y];
    half4 colorTrim2 = _MaterialColors[INPUT.ids.z];

    half4 dataPrimary = _MaterialData[INPUT.ids.x];
    half4 dataTrim1 = _MaterialData[INPUT.ids.y];
    half4 dataTrim2 = _MaterialData[INPUT.ids.z];

    half gradientLerp = INPUT.ids.w;
    half4 color = lerp(colorPrimary, colorTrim1, gradientLerp);
    half4 data = lerp(dataPrimary, dataTrim1, gradientLerp);

    half outlineWidth = INPUT.param.z;
        
#if USE_DERIVATIVE | BEVEL_ON
    ////float3 delta = float3(1.0 / _TextureWidth, 1.0 / _TextureHeight, 0.0);

    ////float4 smp4x = { tex2D(_MainTex, input.uv_MainTex - delta.xz).a,
    ////				tex2D(_MainTex, input.uv_MainTex + delta.xz).a,
    ////				tex2D(_MainTex, input.uv_MainTex - delta.zy).a,
    ////				tex2D(_MainTex, input.uv_MainTex + delta.zy).a };
#endif

#if USE_DERIVATIVE
    ////// Screen space scaling reciprocal with anisotropic correction
    ////float2 edgeNormal = Normalize(float2(smp4x.x - smp4x.y, smp4x.z - smp4x.w));
    ////float2 res = float2(_TextureWidth * input.param.y, _TextureHeight);
    ////float2 tdx = ddx(input.uv_MainTex)*res;
    ////float2 tdy = ddy(input.uv_MainTex)*res;
    ////float lx = length(tdx);
    ////float ly = length(tdy);
    ////float s = sqrt(min(lx, ly) / max(lx, ly));
    ////s = lerp(1, s, abs(dot(normalize(tdx + tdy), edgeNormal)));
    ////float scale = rsqrt(abs(tdx.x * tdy.y - tdx.y * tdy.x)) * (_GradientScale * 2) * s;
#else
    float scale = INPUT.param.y;
#endif

    // Signed distance
    float c = tex2D(_MainTex, INPUT.uv).a;
    float sd = (.5 - c - INPUT.param.x) * scale + .5;
    float outline = outlineWidth * _ScaleRatioA * scale;
    float softness = _OutlineSoftness * _ScaleRatioA * scale;

    // Color & Alpha
    color.a = 1; // No transparency support
    half faceAlpha = 1 - saturate((sd - outline * 0.5 + softness * 0.5) / (1.0 + softness));
    half outlineAlpha = saturate((sd + outline * 0.5)) * sqrt(min(1.0, outline));
    color = lerp(color, colorTrim2, outlineAlpha) * faceAlpha;
    data = lerp(data, dataTrim2, outlineAlpha) * faceAlpha;
    color.rgb /= max(color.a, 0.0001);

    clip(color.a - _AlphaCutoff);

    // Compute emission and reduce base color based accordingly
    half emissionStrength = (_EmissiveOverride < 0 ? data.w : _EmissiveOverride);
    half3 emission = color.rgb * emissionStrength;
    color.rgb *= 1 - saturate(emissionStrength);

    float3 pixelDir;
    float pixelDist;
    GetPixelDir(INPUT.worldPosition.xyz, pixelDir, pixelDist);

    // Apply standard lighting and atmospheric effects
    color = ApplyStandardLightingAndAtmosphere(color, data.x, data.y, emission, pixelDir, pixelDist, _atmosphereStrenghtAtCamera, INPUT);

    return color;
}



struct vertInput_shadow
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    half4 color : COLOR;
    float2 uv1 : TEXCOORD0;
    float2 uv2 : TEXCOORD1;
};

struct v2f_shadow {
    V2F_SHADOW_CASTER;
    float2	uv			: TEXCOORD1;
    float3 param : TEXCOORD6;
};

v2f_shadow vert_shadow(vertInput_shadow v)
{
    v2f_shadow OUT;
    TRANSFER_SHADOW_CASTER(OUT)

    v.vertex.x += _VertexOffsetX;
    v.vertex.y += _VertexOffsetY;

    // Generate normal for backface
    float3 view = ObjSpaceViewDir(v.vertex);
    v.normal *= sign(dot(v.normal, view));

    // Outline width
    OUT.param.z = v.color.a;

    float bold = step(v.uv2.y, 0);

    #if USE_DERIVATIVE
        ////data.param.y = 1;
    #else
        float4 vert = v.vertex;
        float4 vPosition = UnityObjectToClipPos(vert);
        float2 pixelSize = vPosition.w;

        pixelSize /= float2(_ScaleX, _ScaleY) * mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy);
        float scale = rsqrt(dot(pixelSize, pixelSize));
        scale *= abs(v.uv2.y) * _GradientScale * (_Sharpness + 1);
        scale = lerp(scale * (1 - _PerspectiveFilter), scale, abs(dot(UnityObjectToWorldNormal(v.normal.xyz), normalize(WorldSpaceViewDir(vert)))));
        OUT.param.y = scale;
    #endif

    OUT.param.x = (lerp(_WeightNormal, _WeightBold, bold) / 4.0 + _FaceDilate) * _ScaleRatioA * 0.5; // 

    OUT.uv = v.uv1;

    return OUT;
}

float4 frag_shadow(v2f_shadow INPUT) : COLOR
{
    half outlineWidth = INPUT.param.z;

#if USE_DERIVATIVE | BEVEL_ON
    ////float3 delta = float3(1.0 / _TextureWidth, 1.0 / _TextureHeight, 0.0);

    ////float4 smp4x = { tex2D(_MainTex, input.uv_MainTex - delta.xz).a,
    ////				tex2D(_MainTex, input.uv_MainTex + delta.xz).a,
    ////				tex2D(_MainTex, input.uv_MainTex - delta.zy).a,
    ////				tex2D(_MainTex, input.uv_MainTex + delta.zy).a };
#endif

#if USE_DERIVATIVE
    ////// Screen space scaling reciprocal with anisotropic correction
    ////float2 edgeNormal = Normalize(float2(smp4x.x - smp4x.y, smp4x.z - smp4x.w));
    ////float2 res = float2(_TextureWidth * input.param.y, _TextureHeight);
    ////float2 tdx = ddx(input.uv_MainTex)*res;
    ////float2 tdy = ddy(input.uv_MainTex)*res;
    ////float lx = length(tdx);
    ////float ly = length(tdy);
    ////float s = sqrt(min(lx, ly) / max(lx, ly));
    ////s = lerp(1, s, abs(dot(normalize(tdx + tdy), edgeNormal)));
    ////float scale = rsqrt(abs(tdx.x * tdy.y - tdx.y * tdy.x)) * (_GradientScale * 2) * s;
#else
    float scale = INPUT.param.y;
#endif

    // Signed distance
    float c = tex2D(_MainTex, INPUT.uv).a;
    float sd = (.5 - c - INPUT.param.x) * scale + .5;
    float outline = outlineWidth * _ScaleRatioA * scale;
    float softness = _OutlineSoftness * _ScaleRatioA * scale;

    // Color & Alpha
    half faceAlpha = 1 - saturate((sd - outline * 0.5 + softness * 0.5) / (1.0 + softness));
    float4 color = faceAlpha;
    color.rgb /= max(color.a, 0.0001);

    clip(color.a - _AlphaCutoff);

    SHADOW_CASTER_FRAGMENT(INPUT);
}


#endif