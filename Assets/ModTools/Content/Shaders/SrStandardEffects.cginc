#ifndef SrStandardEffects_INCLUDED
#define SrStandardEffects_INCLUDED
    
    #include "Sr2ShaderStructures.cginc"
    #include "Utils.cginc"
    #include "SrStandardUnityLighting.cginc"

    // Uncomment for Dev / Syntax Highlighting only
    //#define ATMOSPHERE 1
    
    #define InitializeVertexOutput(OUT) \
        v2f OUT; \
        UNITY_INITIALIZE_OUTPUT(v2f, OUT); \
        OUT.pos = UnityObjectToClipPos(v.vertex); \
        OUT.worldPosition.xyz = mul(unity_ObjectToWorld, v.vertex); \
        float3 relativeVertPos = OUT.worldPosition.xyz - _planetCenter; \
        float3 relativeVertDir = normalize(relativeVertPos); \
        float atten = GetLightingAtten(relativeVertDir, GetLightDirection(OUT.worldPosition.xyz), 0); \
        OUT.lightColor = GetLightColor(atten); \
        OUT.worldNormal = NormalizePerVertexNormal(UnityObjectToWorldNormal(v.normal)); \
        float3 camToVertex = OUT.worldPosition.xyz - _worldCameraPosition; \
        float distToVertex = length(camToVertex); \
        half ambientScale = 1 - saturate(distToVertex * AmbientLightFadeRange); \
        OUT.ambient = float4(VertexGI(OUT.worldPosition.xyz, OUT.worldNormal), max(_ambientLightMinFalloff, ambientScale * ambientScale)); \
        TRANSFER_SHADOW(OUT);

    struct AtmosData
    {
        float3 color;
        float dist;
    };

    void GetPixelDir(float3 worldPixelPos, out float3 pixelDir, out float pixelDist)
    {
        pixelDir = worldPixelPos - _WorldSpaceCameraPos;
        pixelDist = length(pixelDir);
        pixelDir = pixelDir / pixelDist;
    }

    float3 GetAtmosColor(float3 startPos, float3 direction, float3 lightDir, float atmosphereDist, half3 lightColor, float3 invWavelength)
    {
        float3 atmosColor = 0;
        float attenuate = 0;

        float cloudMask = 0;
        
        // Atmos
        float3 posNorm = normalize(startPos);

        float cameraAngle;
        float lightAngle;
        float cameraScale;
        float lightScale;
        
        bool precompute = false;
        float precomputedScatter;
        float precomputedCameraOffset;
        
        if(precompute)
        {
            cameraAngle = saturate(dot(direction, posNorm));
            lightAngle = dot(lightDir.xyz, posNorm);
            cameraScale = ExpScale(cameraAngle, _scaleDepth, _atmosSizeScale);
            lightScale = ExpScale(lightAngle, _scaleDepth, _atmosSizeScale);
            
            float startDepth = exp((_innerRadius - _outerRadius) / _scaleDepth);
            precomputedCameraOffset = startDepth * cameraScale;
            precomputedScatter = (lightScale + cameraScale);
        }

        float sampleLength = atmosphereDist / _samples;
        float scaledLength = sampleLength * _scale * _atmosScale;

        float3 step = direction * sampleLength;
        float stepLen = length(step);
        
        float3 current = startPos;
        for (int index = 0; index < _samples; ++index)
        {
            // Atmos
            float height = length(current);
            float depth = exp(_scaleOverScaleDepth * min(0, _innerRadius - height));
            float scatter;

            if(!precompute)
            {
                float3 currentNormalized = current / height;
                cameraAngle = saturate(dot(direction, currentNormalized));
                lightAngle = dot(lightDir.xyz, currentNormalized);
                cameraScale = ExpScale(cameraAngle, _scaleDepth, _atmosSizeScale);
                lightScale = ExpScale(lightAngle, _scaleDepth, _atmosSizeScale);
                scatter =  exp(-1.0 / _scaleDepth) + depth * (lightScale - cameraScale);
            }
            else
            {
                scatter = depth * precomputedScatter - precomputedCameraOffset;
            }

            float attenuate = exp(-scatter * (invWavelength.xyz * _kr4PI + _km4PI));
            atmosColor += attenuate * depth * scaledLength;

            current += step;
        }

        return atmosColor;
    }

    float GetNearIntersection(float3 cameraToPlanetDir, float3 cameraToVertexDir, float3 planetToVertex, float distance2, float radius2)
    {
        // Calculate the closest intersection of the ray with the outer atmosphere 
        //(which is the near point of the ray passing through the atmosphere)
        float B = 2.0 * dot(cameraToPlanetDir, cameraToVertexDir);
        float C = distance2 - radius2;
        float det = max(0.0, B * B - 4.0 * C);
        return 0.5 * (-B - sqrt(det)); // TODO: Need to verify if this is supposed to be -B - sqrt(), or -B + sqrt()
    }

    // Note: positionAtten = 1 at noon, -1 at midnight.
    float3 GetShadows(v2f INPUT, float3 positionAtten)
    {
        // Get basic unity shadow atten...will be either 0 or 1...all or none.
        //float shadowAtten = LIGHT_ATTENUATION(INPUT);
        UNITY_LIGHT_ATTENUATION(shadowAtten, INPUT, INPUT.worldPosition.xyz);
        
        const float noonShadowStrength = .5;
        const float midnightShadowStrength = 1;

        float shadowStrength = lerp(noonShadowStrength, midnightShadowStrength, 1 - positionAtten);
        float shadowAmmount = (1 - shadowAtten) * shadowStrength;
    
        return 1 - shadowAmmount;
    }

    float3 GetTpmTexture(float3 worldNormal, float3 worldPosition, sampler2D tex, float4 inspectorScale, float planetRadius, int small, int med, int large)
    {
            float3 blending = abs( worldNormal );
            blending = normalize(max(blending, 0.00001)); // Force weights to sum to 1.0
            float b = (blending.x + blending.y + blending.z);
            blending /= float3(b, b, b);
           
            float3 xaxis = GetTexture(worldPosition.yz / planetRadius, tex, inspectorScale, small, med, large);
            float3 yaxis = GetTexture(worldPosition.xz / planetRadius, tex, inspectorScale, small, med, large);
            float3 zaxis = GetTexture(worldPosition.xy / planetRadius, tex, inspectorScale, small, med, large);
            return xaxis * blending.x + yaxis * blending.y + zaxis * blending.z;
    }
    
    AtmosData GetAtmosphereData(float3 vertPosObjectSpace, float3 lightDir, half3 lightColor, float3 invWavelength)
    {
        float3 vertexToCameraDir = _adjustedCameraPosition.xyz - vertPosObjectSpace;
        float cameraToVertexDist = length(vertexToCameraDir);

        // Normalize ray.
        vertexToCameraDir /= cameraToVertexDist;
        float atmosphereDist = cameraToVertexDist;

        // If the camera is outside the atmosphere, use the sphere intersection function to determine how much there is.
        if (_cameraHeight2 > _outerRadius2)
        {
            atmosphereDist = GetIntersectionDist(vertPosObjectSpace, vertexToCameraDir, _outerRadius);
        }

        // Calculate the atmosphere color.
        float3 atmosColor = GetAtmosColor(vertPosObjectSpace, vertexToCameraDir, lightDir, atmosphereDist, lightColor, invWavelength);

        // NOTE: Color seemed to be getting some NANs or Infinities in Solar SOI. The clamp fixes it.
        AtmosData atmosData;
        atmosData.color = clamp(atmosColor * (invWavelength.xyz * _krESun + _kmESun), 0, 10000);
        atmosData.dist = atmosphereDist;
        return atmosData;
    }

    #if DISTANCE_BLENDED_TEXTURES
    void CalculateDistanceBlendedTextureData(float distanceToVertex, float4 inputUVs, out float4 outputUVs, out float4 outputStrengths, out float4 outputData) 
    {
        #if DISTANCE_BLENDED_TEXTURES_FAST
            // Add some distance to all vertex distances.
            // This helps band-aide the issue of textures looking like they disappear when running in the vertex shader rather than fragment.
            // I believe this is because a single quad in low quality settings can straddle an entire tiling level, 
            // which ultimately messes up UVs and mipmap selection.
            const float distanceOffset = 64;
        #else
            const float distanceOffset = 0;
        #endif

        float distance = (distanceToVertex * _distanceBlendLookup[0].x) + _distanceBlendLookup[0].y + distanceOffset;
        float lg = min(log2(max(4, distance)) - 1, 19.999);
        float lgf = floor(lg);

        float mod = fmod(lgf, 2);
        float modI = 1 - mod;
        float modN = sign(mod - 0.5);
        float modIN = sign(modI - 0.5);

        float lgc = lgf + modI;
        lgf += mod;

        float s = frac(lg);
        float s1 = (s * modIN) + mod;
        float s2 = (s * modN) + modI;

        float4 lgcData = _distanceBlendLookup[lgc];
        float4 lgfData = _distanceBlendLookup[lgf];

        float lgcUVType = max(0, sign(lgc - _distanceBlendLookup[0].z));
        float lgfUVType = max(0, sign(lgf - _distanceBlendLookup[0].z));

        float2 lgcUVs = (inputUVs.xy * lgcUVType) + (inputUVs.zw * (1 - lgcUVType));
        float2 lgfUVs = (inputUVs.xy * lgfUVType) + (inputUVs.zw * (1 - lgfUVType));
        
        outputUVs = float4(lgcUVs * lgcData.x, lgfUVs * lgfData.x);
        outputStrengths = float4(s1 * lgcData.y, s2 * lgfData.y, s1, s2);
        outputData = float4(lgcData.z, lgfData.z, lgcData.w, lgfData.w);
    }
    #endif

    #if ATMOSPHERE
        #define GetAtmosphereDataForVertex(OUT) \
            /* Note: GetAtmosphereData() takes sphereObjectSpaceVertPos, which is an object space vertex position.  For shaders such as GroundFromSpace, the \
               regular vertex's object space position is used as-is.  We can't do that with quad-sphere rendering b/c object space positions are anchored around \
               a quad, not a sphere.  So we're using world-space positions and getting their center-relative positions and then scaling them to construct \
               an object-space position. */ \
            float3 vertexPosWorldRelative = OUT.worldPosition.xyz - _planetCenter; \
            float3 vertexObjectPos = vertexPosWorldRelative / _worldPositionScale; \
            AtmosData atmosData = GetAtmosphereData(vertexObjectPos, GetLightDirection(OUT.worldPosition.xyz), OUT.lightColor, _invWaveLength); \
            OUT.atmosColor.rgb = atmosData.color;
    #else
        #define GetAtmosphereDataForVertex(OUT) // Do Nothing
    #endif

    float3 GetLightDirection(float3 worldPosition)
    {
        #if UNITY_PASS_FORWARDBASE
            return _lightDir;
        #else
            #if defined(POINT) || defined(POINT_COOKIE) || defined(SPOT)
                return normalize(_WorldSpaceLightPos0.xyz - worldPosition);
            #else
                // This should return _WorldSpaceLightPos0.xyz but Unity doesn't seem to be reliably setting this variable during the additive pass.
                //return _WorldSpaceLightPos0.xyz;
                return _directionalLightAdditive_Direction;
            #endif
        #endif
    }

    half3 GetLightColor(float positionAttenuation)
    {
        #if UNITY_PASS_FORWARDADD
            return _LightColor0;
        #else
            float intensityAtGround = smoothstep(-0.1, 0.0, positionAttenuation);
            float duskLerp = saturate(positionAttenuation * 2.5);
            #if SRSTANDARD_TERRAIN || SRSTANDARD_WATER || SRSTANDARD_OBJECT || SRSTANDARD_SCALEDSPACE
                return lerp(_duskColor, _noonColor, duskLerp) * intensityAtGround;
            #else
                half4 ground = half4(lerp(_duskColor, _noonColor, duskLerp), intensityAtGround);
                half4 light = lerp(ground, _sunLightColor, _groundToSkyLightFade);
                return light.xyz * light.a;
            #endif
        #endif
    }

    half4 ApplyUnityPBRLightingMetallic(half3 albedo, half metallic, half smoothness, half3 camToPixel, UnityLight light, v2f INPUT, half3 indirectSpecular, out fixed lightAttenuationOut)
    {
        UNITY_LIGHT_ATTENUATION(lightAttenuation, INPUT, INPUT.worldPosition.xyz);
        lightAttenuationOut = lightAttenuation;
        FragmentCommonData fragData = MetallicSetup(albedo, metallic, smoothness, INPUT.worldPosition.xyz, camToPixel, INPUT.worldNormal);
        #if UNITY_PASS_FORWARDADD
            return ApplyUnityLightingAdd(fragData, light, lightAttenuation);
        #elif SRSTANDARD_PART || SRSTANDARD_PART_TMPRO
            return ApplyUnityLightingBase_ScaledIndirectSpecular(fragData, light, INPUT.ambient, lightAttenuation, _minimumReflectivity, indirectSpecular);
        #else
            return ApplyUnityLightingBase(fragData, light, INPUT.ambient, lightAttenuation, indirectSpecular);
        #endif
    }

    half4 ApplyUnityPBRLightingSpecular(half3 albedo, half3 specColor, half smoothness, half3 camToPixel, UnityLight light, v2f INPUT, out fixed lightAttenuationOut)
    {
        UNITY_LIGHT_ATTENUATION(lightAttenuation, INPUT, INPUT.worldPosition.xyz);
        lightAttenuationOut = lightAttenuation;
        FragmentCommonData fragData = SpecularSetup (albedo, specColor, smoothness, INPUT.worldPosition.xyz, camToPixel, INPUT.worldNormal);
        #if UNITY_PASS_FORWARDADD
            return ApplyUnityLightingAdd(fragData, light, lightAttenuation);
        #elif SRSTANDARD_PART || SRSTANDARD_PART_TMPRO
            return ApplyUnityLightingBase_ScaledIndirectSpecular(fragData, light, INPUT.ambient, lightAttenuation, _minimumReflectivity, 0);
        #else
            return ApplyUnityLightingBase(fragData, light, INPUT.ambient, lightAttenuation, 0);
        #endif
    }

    // TODO: Is atmosphere strength needed here or will atmosphere/fog already fade out on the craft as it leaves the atmosphere
    #if SRSTANDARD_WATER
    half4 ApplyStandardLightingAndAtmosphere(half4 color, half metallic, half smoothness, half3 emission, float3 pixelDir, float3 pixelDist, half atmosphereStrength, v2f INPUT, half3 indirectSpecular)
    #elif SRSTANDARD_SPECULAR_LIGHTING
    half4 ApplyStandardLightingAndAtmosphere(half4 color, half3 specularColor, half smoothness, half emission, float3 pixelDir, float3 pixelDist, half atmosphereStrength, v2f INPUT)
    #else
    half4 ApplyStandardLightingAndAtmosphere(half4 color, half metallic, half smoothness, half3 emission, float3 pixelDir, float3 pixelDist, half atmosphereStrength, v2f INPUT)
    #endif
    {
        // Lighting
        UnityLight light;
        light.dir = GetLightDirection(INPUT.worldPosition.xyz);
        light.color = INPUT.lightColor;

        half lightAttenuation = 0;
        half emissionStrength = 0;
        
        #if UNDERWATER
            // Underwater depth and camera distance affect light color and strength
            half distance = saturate(pixelDist / (_underwaterLightFadeDistance));
            half oneMinusDistance = 1.0 - distance;
            #if UNITY_PASS_FORWARDADD
                half depth = 0;
                half depthPlusDistance = distance;
            #else
                half depth = _underwaterLightFadeDepth;
                half depthPlusDistance = saturate(depth + distance);
            #endif

            half3 underwaterLightColor = lerp(light.color, _underwaterDarkColor, depthPlusDistance);
            light.color = underwaterLightColor;
            INPUT.ambient.xyz = underwaterLightColor;
            INPUT.ambient.w = 1.0 - depthPlusDistance;

            // Don't render atmosphere under water
            atmosphereStrength *= ((length(INPUT.worldPosition.xyz - _planetCenter) - _seaLevelWorldRadius) < 10) ? 0 : 1;
        #endif

        #if SRSTANDARD_WATER
            color.rgb = ApplyUnityPBRLightingMetallic(color, metallic, smoothness, pixelDir, light, INPUT, indirectSpecular, lightAttenuation);
        #elif SRSTANDARD_SPECULAR_LIGHTING
            color.rgb = ApplyUnityPBRLightingSpecular(color, specularColor, smoothness, pixelDir, light, INPUT, lightAttenuation);
        #else
            color.rgb = ApplyUnityPBRLightingMetallic(color, metallic, smoothness, pixelDir, light, INPUT, 0, lightAttenuation);
        #endif

        #if SRSTANDARD_PART || SRSTANDARD_PART_TMPRO
            // clamp the color to prevent really high specular values for parts
            color = min(color, 3);
        #endif

        // Apply emission
        #if UNITY_PASS_FORWARDBASE
            #if UNDERWATER
                // Under water, emission fades with distance
                emission *= oneMinusDistance;
                emissionStrength = length(emission);
            #endif
            color.rgb += emission;
        #endif

        #if ATMOSPHERE  
            float3 atmos = INPUT.atmosColor.rgb * atmosphereStrength;
            // Add in atmosphere, reducing the "saturation" of the base color the more powerful the atmosphere is.
            color.xyz = atmos + color.xyz * (1 - saturate(length(atmos)));
        #endif

        #if UNDERWATER
            #if UNITY_PASS_FORWARDADD
                // Reduce underwater lighting based on the fragment's distance from the light
                half fragDistanceFromLight = length(_WorldSpaceLightPos0.xyz - INPUT.worldPosition.xyz);
                lightAttenuation *= 1.0 - saturate(fragDistanceFromLight / _underwaterLightFadeDistance);
                lightAttenuation *= saturate(dot(INPUT.worldNormal, light.dir));

                // Reduce underwater lighting color based on the strength of the light
                half lightStrength = saturate(length(lightAttenuation * _LightColor0.rgb));
                
                // Apply the underwater fog for the light
                color.rgb = lerp(color.xyz, _underwaterColor * lightStrength * oneMinusDistance, _underwaterColorIntensity);
            #else
                // Apply underwater fog, fading to the dark color based on depth (or night), unless emissive.
                half underwaterDarkFade = saturate(max(_nightToDayLerpValue.y, depth) - emissionStrength);
                color.rgb = lerp(lerp(color.xyz, _underwaterColor, _underwaterColorIntensity), _underwaterDarkColor, underwaterDarkFade);
            #endif
        #endif

        return color;
    }

#endif