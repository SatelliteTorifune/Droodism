#ifndef SrStandardShaderData_INCLUDED
#define SrStandardShaderData_INCLUDED

    // Dynamic Data
    float4 _sunLightColor;
    float4 _adjustedCameraPosition;
    float4 _worldCameraPosition;
    float4 _cameraViewDir;
    float4 _lightDir;
    float4 _planetCenter;
    float4 _nightToDayLerpValue;
    float _cameraHeight;
    float _cameraHeight2;
    float _cameraHeightAtmosPercent;
    float _debugScaler;
    float _atmosphereStrenghtAtCamera;
    float _groundToSkyLightFade;
    float _atmosScale;
    float _scaleDepth;
    float _scaleOverScaleDepth;
    sampler2D _ScaledSpaceTerrainTexture;

    #if UNDERWATER
        float _underwaterLightFadeDepth;
        float _underwaterLightFadeDistance;
        half  _underwaterColorIntensity;
        half3 _underwaterColor;
        half3 _underwaterDarkColor;
    #endif
    
    // 1 means full quad strength, 0 means full scaled-space
    float _quadToScaledTransition;

    // Static Data
    float _samples;
    float _g;
    float _g2;
    float3 _noonColor;
    float3 _duskColor;
    float4 _invWaveLength;
    float _outerRadius;
    float _outerRadius2;
    float _innerRadius;
    float _innerRadius2;
    float _atmosSizeScale;
    float _seaLevelWorldRadius;
    float _worldPositionScale;
    float _krESun;
    float _kmESun;
    float _kr4PI;
    float _km4PI;
    float _scale;
    half _minimumReflectivity;
    half _ambientLightMinFalloff;
    half _lightingFresnelBias;
    float _maxColorValue;
    float3 _directionalLightAdditive_Direction;

    // Setup common defines
    #if !UNITY_PASS_FORWARDADD
        #if TERRAIN_ATMOSPHERE || OBJECT_ATMOSPHERE
            #if !defined(ATMOSPHERE)
                #define ATMOSPHERE 1
            #endif
        #endif
    #endif

    #if defined(UNITY_PBS_USE_BRDF3) || defined(UNITY_PBS_USE_BRDF2) || defined(UNITY_PBS_USE_BRDF1)
        #undef UNITY_PBS_USE_BRDF3
        #undef UNITY_PBS_USE_BRDF2
        #undef UNITY_PBS_USE_BRDF1
    #endif

    #if defined(UNITY_BRDF_PBS)
        #error SR Standard Lighting Method Already Defined
    #elif defined(UNITY_PBS_USE_BRDF3) || defined(UNITY_PBS_USE_BRDF2) || defined(UNITY_PBS_USE_BRDF1)
        #error SR Standard Lighting Method Already Configured
    #elif SR_LIGHTING_NONE
        #define UNITY_PBS_USE_BRDF3 1
        //#define _SPECULARHIGHLIGHTS_OFF
    #elif SR_LIGHTING_LOW
        #define UNITY_PBS_USE_BRDF3 1
        //#define _SPECULARHIGHLIGHTS_OFF
    #elif SR_LIGHTING_MEDIUM
        #define UNITY_PBS_USE_BRDF2 1
    #elif SR_LIGHTING_HIGH
        #define UNITY_PBS_USE_BRDF1 1
    #else
        #error SR Standard Lighting Method Not Specified
    #endif

    #if DISTANCE_BLENDED_TEXTURES
        // Data used configure different levels of texture tiling on a planet cube face.
        // Index 1 through 15 contain settings for increasingly further distances between the camera and the vertex.
        // Distances increase index by powers of 2, adjusted by the distance scalar and adjustment value.
        // Index 0 Data:
        //   x: Distance scalar (default is 1). At 0.5, distances at which tiling scales switch is doubled.
        //   y: Distance adjustment (default is 10). Added to the distances at which tiling begins (after the scalar is applied).
        //   z: The tiling level at which scaled UV coordinates start being used.
        //   w: Unused
        // Index 1 through 15 Data:
        //   x: Tiling scale for textures at this tiling level.
        //   y: The strength of the texture at this tiling level.
        //   z: Water Only: The speed of the waves at this tiling level.
        //   w: Water Only: The specular strength at this tiling level.
        float4 _distanceBlendLookup[21];
    #endif
    
    // Disable reflection probe blending and box projection because we don't use them
    #undef UNITY_SPECCUBE_BOX_PROJECTION
    #undef UNITY_SPECCUBE_BLENDING

    // Always disable reflection probes except for the part shader
    #define _GLOSSYREFLECTIONS_OFF 1
    #if SRSTANDARD_PART || SRSTANDARD_PART_TMPRO
        #undef _GLOSSYREFLECTIONS_OFF
    #endif

#endif