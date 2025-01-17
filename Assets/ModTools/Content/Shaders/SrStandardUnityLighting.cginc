#ifndef SrStandardUnityLighting_INCLUDED
#define SrStandardUnityLighting_INCLUDED


// WARNING: UNITY_STANDARD_SIMPLE does not appear to work right (no specular highlight). See note in implementation.
//#define UNITY_STANDARD_SIMPLE 1

#if defined(UNITY_NO_FULL_STANDARD_SHADER) && !defined(UNITY_STANDARD_SIMPLE)
#   define UNITY_STANDARD_SIMPLE 1
#endif


#include "UnityStandardConfig.cginc"
#include "UnityPBSLighting.cginc"
#include "SrStandardUnityBRDFWithTweaks.cginc"


struct FragmentCommonData
{
    half3 diffColor, specColor;
    // Note: smoothness & oneMinusReflectivity for optimization purposes, mostly for DX9 SM2.0 level.
    // Most of the math is being done on these (1-x) values, and that saves a few precious ALU slots.
    half oneMinusReflectivity, smoothness;
    half3 normalWorld, eyeVec;
    half alpha;
    float3 posWorld;

    #if UNITY_STANDARD_SIMPLE
    half3 reflUVW;
    #endif

    #if UNITY_STANDARD_SIMPLE
    half3 tangentSpaceNormal;
    #endif
};

half3 NormalizePerVertexNormal (float3 n) // takes float to avoid overflow
{
    #if (SHADER_TARGET < 30) || UNITY_STANDARD_SIMPLE
        return normalize(n);
    #else
        return n; // will normalize per-pixel instead
    #endif
}

half3 NormalizePerPixelNormal (half3 n)
{
    #if (SHADER_TARGET < 30) || UNITY_STANDARD_SIMPLE
        return n;
    #else
        return normalize(n);
    #endif
}

inline FragmentCommonData SpecularSetup(half3 albedo, half3 specColor, half smoothness, float3 posWorld, half3 eyeVec, half3 normalWorld)
{
    half oneMinusReflectivity;
    half3 diffColor = EnergyConservationBetweenDiffuseAndSpecular(albedo, specColor, /*out*/ oneMinusReflectivity);

    FragmentCommonData o = (FragmentCommonData)0;
    o.diffColor = diffColor;
    o.specColor = specColor;
    o.oneMinusReflectivity = oneMinusReflectivity;
    o.smoothness = smoothness;
    o.normalWorld = NormalizePerPixelNormal(normalWorld);
    o.eyeVec = eyeVec;
    o.posWorld = posWorld;

    return o;
}

inline FragmentCommonData MetallicSetup(half3 albedo, half metallic, half smoothness, float3 posWorld, half3 eyeVec, half3 normalWorld)
{
    half oneMinusReflectivity;
    half3 specColor;
    half3 diffColor = DiffuseAndSpecularFromMetallic(albedo, metallic, /*out*/ specColor, /*out*/ oneMinusReflectivity);

    FragmentCommonData o = (FragmentCommonData)0;
    o.diffColor = diffColor;
    o.specColor = specColor;
    o.oneMinusReflectivity = oneMinusReflectivity;
    o.smoothness = smoothness;
    o.normalWorld = NormalizePerPixelNormal(normalWorld);
    o.eyeVec = eyeVec;
    o.posWorld = posWorld;

    return o;
}

inline half3 VertexGI(float3 posWorld, half3 normalWorld)
{
    half3 ambient = 0;
    #if UNITY_SHOULD_SAMPLE_SH
        #ifdef VERTEXLIGHT_ON
            // Approximated illumination from non-important point lights
            ambient = Shade4PointLights (
                unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
                unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
                unity_4LightAtten0, posWorld, normalWorld);
        #endif

        ambient = ShadeSHPerVertex (normalWorld, ambient);
    #endif

    return ambient;
}

inline UnityGI FragmentGI(FragmentCommonData s, half3 ambient, half atten, UnityLight light, half3 indirectSpecular)
{
    UnityGIInput d;
    d.light = light;
    d.worldPos = s.posWorld;
    d.worldViewDir = -s.eyeVec;
    d.atten = atten;
    d.ambient = ambient;
    d.lightmapUV = 0;

    d.probeHDR[0] = unity_SpecCube0_HDR;
    d.probeHDR[1] = unity_SpecCube1_HDR;
    #if defined(UNITY_SPECCUBE_BLENDING) || defined(UNITY_SPECCUBE_BOX_PROJECTION)
      d.boxMin[0] = unity_SpecCube0_BoxMin; // .w holds lerp value for blending
    #endif
    #ifdef UNITY_SPECCUBE_BOX_PROJECTION
      d.boxMax[0] = unity_SpecCube0_BoxMax;
      d.probePosition[0] = unity_SpecCube0_ProbePosition;
      d.boxMax[1] = unity_SpecCube1_BoxMax;
      d.boxMin[1] = unity_SpecCube1_BoxMin;
      d.probePosition[1] = unity_SpecCube1_ProbePosition;
    #endif

    Unity_GlossyEnvironmentData g = UnityGlossyEnvironmentSetup(s.smoothness, -s.eyeVec, s.normalWorld, s.specColor);
    // Replace the reflUVW if it has been compute in Vertex shader. Note: the compiler will optimize the calcul in UnityGlossyEnvironmentSetup itself
    #if UNITY_STANDARD_SIMPLE
        g.reflUVW = s.reflUVW;
    #endif

    #if SRSTANDARD_WATER
        UnityGI gi = UnityGlobalIllumination(d, 1, s.normalWorld);
        gi.indirect.specular = indirectSpecular;
        return gi;
    #else
        return UnityGlobalIllumination(d, 1, s.normalWorld, g);
    #endif
}

#if UNITY_STANDARD_SIMPLE

#ifndef SPECULAR_HIGHLIGHTS
    #define SPECULAR_HIGHLIGHTS (!defined(_SPECULAR_HIGHLIGHTS_OFF))
#endif

// BUG: This appears to drop the specular highlight. After many attempts, there was no success in trying to fix it.
//      The following TODO was discovered in Unity's BRDF3 lighting code though, might be the culprit...
//      TODO: specular is too weak in Linear rendering mode
half4 ApplyUnityLightingBase(FragmentCommonData s, UnityLight light, half3 ambient, fixed lightAttenuation, half3 indirectSpecular)
{
    #if !SPECULAR_HIGHLIGHTS
        half3 reflectVector = half3(0, 0, 0);
    #else
        //half3 reflectVector = s.reflUVW;
        half3 reflectVector = reflect(s.eyeVec, s.normalWorld); // TODO: Should be in vertex shader
    #endif

    half ndotl = saturate(dot(s.normalWorld, light.dir));
    half rl = dot(reflectVector, light.dir);

    UnityGI gi = FragmentGI(s, ambient, lightAttenuation, light, indirectSpecular);
    half3 attenuatedLightColor = gi.light.color * ndotl;
    
    half grazingTerm = saturate(s.smoothness + (1 - s.oneMinusReflectivity));
    half fresnelTerm = Pow4(1 - saturate(dot(s.normalWorld, -s.eyeVec))); // TODO: Unity stuffed this in the world normal 'w' in the vert shader
    
    half3 c = BRDF3_Indirect(s.diffColor, s.specColor, gi.indirect, grazingTerm, fresnelTerm);

    #if SPECULAR_HIGHLIGHTS
        c += BRDF3_Direct(s.diffColor, s.specColor, Pow4(rl), s.smoothness) * attenuatedLightColor;
    #else
        c += s.diffColor * attenuatedLightColor;
    #endif

    return half4(c, 1);
}

half4 ApplyUnityLightingAdd(FragmentCommonData s, UnityLight light, fixed lightAttenuation)
{
    #if !SPECULAR_HIGHLIGHTS
        half3 reflectVector = half3(0, 0, 0);
    #else
        //half3 reflectVector = s.reflUVW;
        half3 reflectVector = reflect(s.eyeVec, s.normalWorld); // TODO: Should be in vertex shader
    #endif
    
    half ndotl = saturate(dot(s.normalWorld, light.dir));
    half rl = dot(reflectVector, light.dir);

    half3 c = BRDF3_Direct(s.diffColor, s.specColor, Pow4(rl), s.smoothness);

    #if SPECULAR_HIGHLIGHTS // else diffColor has premultiplied light color
        c *= light.color;
    #endif
    
    c *= lightAttenuation * ndotl;

    return half4(c, 1);
}

#else

half4 ApplyUnityLightingBase(FragmentCommonData s, UnityLight light, half4 ambient, fixed lightAttenuation, half3 indirectSpecular)
{
    UnityGI gi = FragmentGI(s, ambient.xyz, lightAttenuation, light, indirectSpecular);

    #if SRSTANDARD_TERRAIN || SRSTANDARD_WATER || SRSTANDARD_PART || SRSTANDARD_PART_TMPRO || SRSTANDARD_OBJECT
        gi.indirect.diffuse *= ambient.w;
    #endif
    #if SRSTANDARD_PART || SRSTANDARD_PART_TMPRO
        gi.indirect.specular *= ambient.w;
    #endif

    return UNITY_BRDF_PBS(s.diffColor, s.specColor, s.oneMinusReflectivity, s.smoothness, s.normalWorld, -s.eyeVec, gi.light, gi.indirect);
}

half4 ApplyUnityLightingBase_ScaledIndirectSpecular(FragmentCommonData s, UnityLight light, half4 ambient, fixed lightAttenuation, half minimumReflectivity, half3 indirectSpecular)
{
    UnityGI gi = FragmentGI(s, ambient.xyz, lightAttenuation, light, indirectSpecular);

    // Dim the reflection probes by the current indirect (ambient) lighting or our allowed minimum
    gi.indirect.specular *= max(half3(minimumReflectivity, minimumReflectivity, minimumReflectivity), gi.indirect.diffuse);

    #if SRSTANDARD_TERRAIN || SRSTANDARD_WATER || SRSTANDARD_PART || SRSTANDARD_PART_TMPRO || SRSTANDARD_OBJECT
        gi.indirect.diffuse *= ambient.w;
    #endif
    #if SRSTANDARD_PART || SRSTANDARD_PART_TMPRO
        gi.indirect.specular *= ambient.w;
    #endif

    return UNITY_BRDF_PBS(s.diffColor, s.specColor, s.oneMinusReflectivity, s.smoothness, s.normalWorld, -s.eyeVec, gi.light, gi.indirect);
}

half4 ApplyUnityLightingAdd(FragmentCommonData s, UnityLight light, fixed lightAttenuation)
{
    light.color *= lightAttenuation;

    UnityIndirect indirect;
    indirect.diffuse = 0;
    indirect.specular = 0;

    return UNITY_BRDF_PBS(s.diffColor, s.specColor, s.oneMinusReflectivity, s.smoothness, s.normalWorld, -s.eyeVec, light, indirect);
}

#endif

#endif