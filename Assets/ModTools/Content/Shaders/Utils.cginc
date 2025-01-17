#ifndef Utils_INCLUDED
#define Utils_INCLUDED

    #include "UnityCG.cginc"
    #include "UnityPBSLighting.cginc"
    #include "AutoLight.cginc"
    #include "Lighting.cginc"
    #include "noiseSimplex.cginc"

    // Forward declarations (only using when necessary).
    float3 GetRawNormalFromMap(float3x3 transform, sampler2D map, float2 uv, float2 mapTiling, float2 mapOffset);
    float Rand(float4 seed);

    struct UnityLightCustomInputs
    {
        half3 ambientLight;
        half3 ambientSpecular;
        float3 lightColor;
    };

    float Rand(float4 seed)
    {
        return frac(sin(dot(seed, float4(12.9898, 78.233, 45.5432, 25.8976))) * 43758.5453);
    }

    #define GET_TEXTURE_ARRAY_BLENDED(texArray, texIndex, uv, lowDetailScale, distToPixel) \
    ((\
        UNITY_SAMPLE_TEX2DARRAY(texArray, float3(uv, texIndex)) + \
        UNITY_SAMPLE_TEX2DARRAY(texArray, float3(uv * lowDetailScale, texIndex + 1))\
    ) / 2)

   //#define GET_TEXTURE_ARRAY_BLENDED(texArray, texIndex, uv, lowDetailScale, distToPixel) \
   //     ((\
   //         UNITY_SAMPLE_TEX2DARRAY(texArray, float3(uv, texIndex)) + \
   //         UNITY_SAMPLE_TEX2DARRAY(texArray, float3(uv * lowDetailScale, texIndex)) + \
   //         UNITY_SAMPLE_TEX2DARRAY(texArray, float3(uv * lowDetailScale / 100, texIndex + 1))\
   //     ) / 3)

    #define GET_TEXTURE_ARRAY_BLENDED_NOISE(texArray, texIndex, uv, small, med, large, noise) \
        ((\
            UNITY_SAMPLE_TEX2DARRAY(texArray, float3(uv / small, texIndex)) + \
            UNITY_SAMPLE_TEX2DARRAY(texArray, float3((uv * 20 * noise) / med, texIndex)) + \
            (UNITY_SAMPLE_TEX2DARRAY(texArray, float3((uv * noise * 1.4) / large, texIndex)))\
        ) / 3)

    // Define the PBR method we're using.
    #define UNITY_BRDF_PBS_CUSTOM BRDF1_Unity_PBS_CUSTOM

    //-------------------------------------------------------------------------------------
    // Philip's NOTE: Pulled from UnityStandardBRDF.cginc

    // Note: BRDF entry points use smoothness and oneMinusReflectivity for optimization
    // purposes, mostly for DX9 SM2.0 level. Most of the math is being done on these (1-x) values, and that saves
    // a few precious ALU slots.


    // Main Physically Based BRDF
    // Derived from Disney work and based on Torrance-Sparrow micro-facet model
    //
    //   BRDF = kD / pi + kS * (D * V * F) / 4
    //   I = BRDF * NdotL
    //
    // * NDF (depending on UNITY_BRDF_GGX):
    //  a) Normalized BlinnPhong
    //  b) GGX
    // * Smith for Visiblity term
    // * Schlick approximation for Fresnel

    half4 BRDF1_Unity_PBS_CUSTOM (half3 diffColor, half3 specColor, half oneMinusReflectivity, half smoothness, half3 normal, half3 viewDir, UnityLight light, UnityIndirect gi, UnityLightCustomInputs customInputs)
    {
        half perceptualRoughness = SmoothnessToPerceptualRoughness (smoothness);
        half3 halfDir = Unity_SafeNormalize (light.dir + viewDir);

    // NdotV should not be negative for visible pixels, but it can happen due to perspective projection and normal mapping
    // In this case normal should be modified to become valid (i.e facing camera) and not cause weird artifacts.
    // but this operation adds few ALU and users may not want it. Alternative is to simply take the abs of NdotV (less correct but works too).
    // Following define allow to control this. Set it to 0 if ALU is critical on your platform.
    // This correction is interesting for GGX with SmithJoint visibility function because artifacts are more visible in this case due to highlight edge of rough surface
    // Edit: Disable this code by default for now as it is not compatible with two sided lighting used in SpeedTree.
    #define UNITY_HANDLE_CORRECTLY_NEGATIVE_NDOTV 0

    #if UNITY_HANDLE_CORRECTLY_NEGATIVE_NDOTV
        // The amount we shift the normal toward the view vector is defined by the dot product.
        half shiftAmount = dot(normal, viewDir);
        normal = shiftAmount < 0.0f ? normal + viewDir * (-shiftAmount + 1e-5f) : normal;
        // A re-normalization should be applied here but as the shift is small we don't do it to save ALU.
        //normal = normalize(normal);

        half nv = saturate(dot(normal, viewDir)); // TODO: this saturate should no be necessary here
    #else
        half nv = abs(dot(normal, viewDir));    // This abs allow to limit artifact
    #endif

        half nl = saturate(dot(normal, light.dir));
        half nh = saturate(dot(normal, halfDir));

        half lv = saturate(dot(light.dir, viewDir));
        half lh = saturate(dot(light.dir, halfDir));

        // Diffuse term
        half diffuseTerm = DisneyDiffuse(nv, nl, lh, perceptualRoughness) * nl;

        // Specular term
        // HACK: theoretically we should divide diffuseTerm by Pi and not multiply specularTerm!
        // BUT 1) that will make shader look significantly darker than Legacy ones
        // and 2) on engine side "Non-important" lights have to be divided by Pi too in cases when they are injected into ambient SH
        half roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
    #if UNITY_BRDF_GGX
        half V = SmithJointGGXVisibilityTerm (nl, nv, roughness);
        half D = GGXTerm (nh, roughness);
    #else
        // Legacy
        half V = SmithBeckmannVisibilityTerm (nl, nv, roughness);
        half D = NDFBlinnPhongNormalizedTerm (nh, PerceptualRoughnessToSpecPower(perceptualRoughness));
    #endif

        half specularTerm = V*D * UNITY_PI; // Torrance-Sparrow model, Fresnel is applied later

    #   ifdef UNITY_COLORSPACE_GAMMA
            specularTerm = sqrt(max(1e-4h, specularTerm));
    #   endif

        // specularTerm * nl can be NaN on Metal in some cases, use max() to make sure it's a sane value
        specularTerm = max(0, specularTerm * nl);
    #if defined(_SPECULARHIGHLIGHTS_OFF)
        specularTerm = 0.0;
    #endif

        // surfaceReduction = Int D(NdotH) * NdotH * Id(NdotL>0) dH = 1/(roughness^2+1)
        half surfaceReduction;
    #   ifdef UNITY_COLORSPACE_GAMMA
            surfaceReduction = 1.0-0.28*roughness*perceptualRoughness;      // 1-0.28*x^3 as approximation for (1/(x^4+1))^(1/2.2) on the domain [0;1]
    #   else
            surfaceReduction = 1.0 / (roughness*roughness + 1.0);           // fade \in [0.5;1]
    #   endif

        // To provide true Lambert lighting, we need to be able to kill specular completely.
        specularTerm *= any(specColor) ? 1.0 : 0.0;

        half grazingTerm = saturate(smoothness + (1-oneMinusReflectivity));

        //half3 color =   diffColor * clamp((gi.diffuse + light.color * diffuseTerm), customInputs.ambientLight, 1)
        //                + specularTerm * light.color * FresnelTerm (specColor, lh)
        //                + surfaceReduction * gi.specular * FresnelLerp (specColor, grazingTerm, nv);

        half3 color = diffColor * (gi.diffuse + light.color * diffuseTerm)
                      + specularTerm * light.color * FresnelTerm(specColor, lh)
                      + surfaceReduction * gi.specular * FresnelLerp(specColor, grazingTerm, nv);

        return half4(color, 1);
    }

    // Philip's NOTE: Pulled from UnityPBSLighting.cginc
    // Philip's NOTE: Different versions are in UnityPBSLighting.cginc that support GI and other stuff.
    inline half4 LightingStandardCustom (SurfaceOutputStandard s, half3 viewDir, UnityGI gi, UnityLightCustomInputs customInputs)
    {
        s.Normal = normalize(s.Normal);

        half oneMinusReflectivity;
        half3 specColor;
        s.Albedo = DiffuseAndSpecularFromMetallic (s.Albedo, s.Metallic, /*out*/ specColor, /*out*/ oneMinusReflectivity);

        // shader relies on pre-multiply alpha-blend (_SrcBlend = One, _DstBlend = OneMinusSrcAlpha)
        // this is necessary to handle transparency in physically correct way - only diffuse component gets affected by alpha
        half outputAlpha;
        s.Albedo = PreMultiplyAlpha (s.Albedo, s.Alpha, oneMinusReflectivity, /*out*/ outputAlpha);

        half4 c = half4(s.Albedo, 0);
        c = UNITY_BRDF_PBS_CUSTOM (s.Albedo, specColor, oneMinusReflectivity, s.Smoothness, s.Normal, viewDir, gi.light, gi.indirect, customInputs);
        c.a = outputAlpha;
        return c;
    }

    float4 ApplyUnityLighting (float3 worldNormal, float3 fogCoord, float3 worldPos, float4 albedo, float3 lightDir, float4 atten, float metallic, float smoothness, UnityLightCustomInputs customInputs)
    {
        // TODO: Clean this stuff up and hook all the variables up properly and add support for the PBR stuff.
        fixed3 worldViewDir = normalize(UnityWorldSpaceViewDir(worldPos));
        SurfaceOutputStandard o = (SurfaceOutputStandard)0;
        o.Albedo = albedo;
        o.Emission = 0.0;
        o.Alpha = 0.0;
        o.Occlusion = 1.0;
        o.Normal = worldNormal;
        o.Metallic = metallic;
        o.Smoothness = smoothness;

        float4 c = 0;

        // Setup lighting environment
        UnityGI gi;
        UNITY_INITIALIZE_OUTPUT(UnityGI, gi);
        gi.indirect.diffuse = customInputs.ambientLight;
        gi.indirect.specular = customInputs.ambientSpecular;
        gi.light.color = customInputs.lightColor;
        gi.light.dir = lightDir;
        gi.light.color *= atten;
    
        c += LightingStandardCustom (o, worldViewDir, gi, customInputs);
        c.a = 0.0;
        //UNITY_APPLY_FOG(fogCoord, c); // apply fog
        UNITY_OPAQUE_ALPHA(c.a);
        return c;
    }

    // Definitions
    float4 Dither(float4 input, float4 randSeed, float power)
    {
        return input * (1 + Rand(randSeed) * power);
    }

    float3 Dither(float3 input, float3 randSeed, float power)
    {
        return Dither(float4(input, 0), float4(randSeed, 0), power);
    }

    float ExpScale(float cos, float scaleDepth, float atmosSizeScale)
    {
        float x = 1 - cos;
        return pow(scaleDepth * exp(-0.00287 + x * (0.459 + x * (3.83 + x * (-6.80 + x * 5.25)))), 1.0 / atmosSizeScale);
    }

    float4 GetSample(sampler2D map, float2 uv, float2 mapTiling, float2 mapOffset)
    {
        return tex2D(map, uv.xy * mapTiling.xy + mapOffset.xy);
    }

    float GetIntersectionDist(float3 insidePos, float3 directionToOutside, float radius)
    {
        const float PI = 3.1415926535897932384626433832795;

        //Using SSA (side-side-angle) and law of sines
        //                                        a (atmosDistance)
        //	             (position on circle) C<------B (insidePos/vertex)
        //                                     ^      ^
        //                                      \     |
        //                                       \    |
        //                              (radius) b\   |c (len(insidePos))
        //                                         \  |
        //                                          \ |
        //                                           \|
        //                                            A
        //                                     (planetCenter (0,0))
        //
        // Note: B is not constrained to a right angle...it was just easier to plot in ASCII

        // Known values
        float b = radius;
        float c = length(insidePos);

        // If the point is outside the radius, return 0
        if(c > radius)
        {
            return 0;
        }

        float3 insidePosDir = insidePos / c;

        // Calculate angle B
        float B = acos(dot(directionToOutside, -insidePosDir));

        // Make sure this angle doesn't approach too close to 180deg or things mess up.
        const float maxDeg = PI * .95;
        B = clamp(B, 0, maxDeg);

        // Use law of sines to find angle C
        // sin(C)/c = sin(B)/b
        // sin(C) = c * (sin(B)/b)
        // C = asin(c * (sin(B)/b))
        float bRatio = sin(B) / b;
        float C = asin(c * bRatio);

        // Use 180deg triangle rule to find A
        float A = PI - (B + C);

        // Use law of sines again to find a (the atmospheric distance)
        // sin(B)/b = sin(A)/a
        // bRatio = sin(A)/a
        // a * bRatio = sin(A)
        // a = sin(A)/bRatio
        float a = sin(A) / bRatio;
        return a;
    }

    float3 GetLightingAtten(float3 normalDir, float3 lightDir, float ambientLightAmount)
    {
        float atten = dot(normalDir, lightDir);

        //if(atten < ambientLightAmount)
        //{
        //    // We're below ambient light amounts, stretch out the ambient light amount to zero over the back side of the planet...witch will end up pitch-black
        //    // on the opposite side of the light.
        //    atten = smoothstep(-1, ambientLightAmount, atten) * ambientLightAmount; 
        //}

        return atten;
    }

    float4 GetNegative(float4 color)
    {
        return abs(saturate(color) - 1);
    }

    float3 GetNormalFromMap(sampler2D bumpMap, uniform float4 bumpMap_ST, float2 uv, float3 tangentDir, float3 bitangentDir, float3 worldNormal, bool mix)
    {
        // Calculate normal map
        float3x3 tangentTransform = float3x3(tangentDir, bitangentDir, worldNormal);

        float3 norm;
        if (mix)
        {
            // TODO: This should probably be done in vert shader and interpolated.
            float3 standardNorm = GetRawNormalFromMap(tangentTransform, bumpMap, uv, bumpMap_ST.xy, bumpMap_ST.zw);
            float3 farNorm = GetRawNormalFromMap(tangentTransform, bumpMap, uv, bumpMap_ST.xy / 300, bumpMap_ST.zw);
            float3 superFarNorm = GetRawNormalFromMap(tangentTransform, bumpMap, uv, bumpMap_ST.xy / 3000, bumpMap_ST.zw);

            // Apply normal map.
            norm = normalize(standardNorm + farNorm + superFarNorm);
        }
        else
        {
            norm = GetRawNormalFromMap(tangentTransform, bumpMap, uv, bumpMap_ST.xy, bumpMap_ST.zw);
        }
                
        return norm;
    }

    float3 GetNormalFromMap(sampler2D bumpMap, uniform float4 bumpMap_ST, float2 uv, float3 tangentDir, float3 bitangentDir, float3 worldNormal, float baseSpeed, float medSpeed, float largeSpeed)
    {
        // Calculate normal map
        float3x3 tangentTransform = float3x3(tangentDir, bitangentDir, worldNormal);

        float2 offsetAnim = _SinTime.x / baseSpeed * float2(1, 1);
        float2 offset = bumpMap_ST.zw + offsetAnim;

        // TODO: This should probably be done in vert shader and interpolated.
        float3 standardNorm = GetRawNormalFromMap(tangentTransform, bumpMap, uv, bumpMap_ST.xy, offset);
        float3 farNorm = GetRawNormalFromMap(tangentTransform, bumpMap, uv, bumpMap_ST.xy / 200, offset / medSpeed);
        float3 superFarNorm = GetRawNormalFromMap(tangentTransform, bumpMap, uv, bumpMap_ST.xy / 3000, offset / largeSpeed);

        // Apply normal map.
        return normalize(standardNorm + farNorm + superFarNorm);
    }

    float3 GetRawNormalFromMap(float3x3 transform, sampler2D map, float2 uv, float2 mapTiling, float2 mapOffset)
    {
        float4 texN = GetSample(map, uv, mapTiling, mapOffset);
        float3 localCoords = float3(2.0 * texN.ag - float2(1.0, 1.0), 0.0);
        localCoords.z = 1.0 - 0.5 * dot(localCoords, localCoords);

        // Apply normal map.
        return normalize(mul(localCoords, transform));
    }

    float3 GetSpecularity(float3 worldNormal, float3 cameraDirNorm, float3 lightDir, float shinyness, float matte)
    {
        // Note: cameraDirNorm passed in as lightDir is unstable as hell in the designer for some reason...UNITY_MATRIX_IT_MV[2].xyz works way better, 
        // but do we want to give up being able to set an arbitrary render vantage?  Maybe just use the unity UNITY_MATRIX_IT_MV[2].xyz for designer rendering?
        // Update: I'm not sure the above is an issue anymore...might have been fixed somewhere along the line?
        float3 specularReflection = float3(0.0, 0.0, 0.0);
        float spec = pow(max(0.0, dot(reflect(lightDir, worldNormal), cameraDirNorm)), shinyness);
        specularReflection = spec * (1 - matte);

        return specularReflection;
    }

    float4 GetTexture(float2 uv, sampler2D tex, uniform float4 tex_st)
    {
        return GetSample(tex, uv, tex_st.xy, tex_st.zw);
    }

    float3 GetTexture(float2 uv, sampler2D tex, uniform float4 tex_st, int small, int med, int large)
    {
        float3 texStandard = GetSample(tex, uv, tex_st.xy / small, tex_st.zw).xyz;
        float3 texFar = GetSample(tex, uv, tex_st.xy / med, tex_st.zw).xyz;
        float3 texSuperFar = GetSample(tex, uv, tex_st.xy / large, tex_st.zw).xyz;

        float3 texSample = (texStandard + texFar + texSuperFar) / 3;
                
        // Debug to only apply to half the screen
        //if(INPUT.pos.x > _ScreenParams.x / 2)
        //{
        //    texSample.xyz = texStandard;
        //}
        //else
        //{
        //    texSample.xyz = texSample;
        //}

        return texSample;
    }

    float4 CalculateDistanceBlendedTextureValues(float distanceToVertex, float tilingFactor, float tilingScale) 
    {
        float lg = log10(distanceToVertex + 10);
        float lgf = floor(lg);

        float mod = fmod(lgf, 2);
        float modI = 1 - mod;
        float modN = sign(mod - 0.5);
        float modIN = sign(modI - 0.5);

        float lgc = lgf + modI;
        lgf += mod;

        float scale1 = tilingScale * pow(10, tilingFactor - lgc);
        float scale2 = tilingScale * pow(10, tilingFactor - lgf);

        float s = frac(lg);
        float s1 = (s * modIN) + mod;
        float s2 = (s * modN) + modI;

        return float4(scale1, scale2, s1, s2);
    }

#endif
