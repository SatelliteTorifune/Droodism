#ifndef Sr2ShaderStructures_INCLUDED
#define Sr2ShaderStructures_INCLUDED

#include "UnityPBSLighting.cginc"
#include "AutoLight.cginc"

// Uncomment for Dev / Syntax Highlighting only
//#define SRSTANDARD_TERRAIN 1
//#define SRSTANDARD_WATER 1
//#define SRSTANDARD_PART 1
//#define SRSTANDARD_SCALEDSPACE 1
//#define ATMOSPHERE 1

#if SRSTANDARD_SCALEDSPACE
    struct v2f
    {
        float4 position : SV_POSITION;
        float3 normal : NORMAL;
        float3 worldNormal : TEXCOORD1;
        float4 cameraToVertex : TEXCOORD2;
        half4 ambient : TEXCOORD3;
        float3 lightColor : TEXCOORD4;
        float3 worldPosition : TEXCOORD0;

        #if ATMOSPHERE
            float3 atmosColor : COLOR0;
        #endif
    };
#else

#if SRSTANDARD_TERRAIN

    // NOTE: TRANSFER_VERTEX_TO_FRAGMENT (more specifically TRANSFER_SHADOW within it) must have a struct as input, with a vert pos named "v.vertex"
    // This only affects Android builds.
    struct vertInput
    {
        float4 vertex : POSITION;
        float3 normal : NORMAL;
        half4 vertColor : COLOR;
        float4 uv : TEXCOORD0;

        #if SPLATMAP1
            float4 uv2 : TEXCOORD1;
            #if SPLATMAP2
                float4 uv3 : TEXCOORD2;
            #endif
        #endif

        half3 uv4 : TEXCOORD3;
    };

    struct v2f
    {
        float4 pos : SV_POSITION;
        float3 worldPosition : TEXCOORD0;
        half4 vertColor : COLOR0;
        float3 worldNormal : NORMAL;
        
        half4 ambient : TEXCOORD2;
        UNITY_SHADOW_COORDS(3)
        half3 lightColor: TEXCOORD5;

        #if ATMOSPHERE
            float3 atmosColor : COLOR1;
        #endif

        #if SPLATMAP1
            float4 splatmap1 : TEXCOORD6;
            #if SPLATMAP2
                float4 splatmap2 : TEXCOORD7;
            #endif
        #endif

        #if DISTANCE_BLENDED_TEXTURES
            float4 distanceBlendedUVs : TEXCOORD8;
            #if DISTANCE_BLENDED_TEXTURES_FAST
                float4 distanceBlendedStrengths : TEXCOORD9;
            #endif
        #endif
        
        #if GROUND_DETAIL_SPLATMAP
            float2 groundDetailUVs : TEXCOORD10;
        #endif

        #if BLEND_SCALED_SPACE
            float4 screenGrabPos : TEXCOORD11;
        #endif

        half3 pbrData : TEXCOORD12;
    };
    
#elif SRSTANDARD_WATER

    // NOTE: TRANSFER_VERTEX_TO_FRAGMENT (more specifically TRANSFER_SHADOW within it) must have a struct as input, with a vert pos named "v.vertex"
    // This only affects Android builds.
    struct vertInput
    {
        float4 vertex : POSITION;
        float3 normal : NORMAL;
        half4 vertColor : COLOR;
        float4 uv : TEXCOORD0;
        half4 uv2 : TEXCOORD1;
        half4 uv3 : TEXCOORD2;
    };

    #if WAVES
        struct TessellationControlPoint
        {
            float4 vertex : INTERNALTESSPOS;
            float3 normal : NORMAL;
            half4 vertColor : COLOR;
            float4 uv : TEXCOORD0;
            half4 uv2 : TEXCOORD1;
            half4 uv3 : TEXCOORD2;
        };

        TessellationControlPoint CreateTessellationControlPoint(vertInput i) {
            TessellationControlPoint p;
            p.vertex = i.vertex;
            p.normal = i.normal;
            p.vertColor = i.vertColor;
            p.uv = i.uv;
            p.uv2 = i.uv2;
            p.uv3 = i.uv3;
            return p;
        }

        struct TessellationFactors {
            float edge[3] : SV_TessFactor;
            float inside : SV_InsideTessFactor;
        };
    #endif

    struct v2f
    {
        float4 pos : SV_POSITION;
        float4 worldPosition : TEXCOORD0;
        half4 vertColor : COLOR0;
        float3 worldNormal : NORMAL;
        
        half4 ambient : TEXCOORD2;
        UNITY_SHADOW_COORDS(3)
        half3 lightColor: TEXCOORD5;

        #if ATMOSPHERE
            float3 atmosColor : COLOR1;
        #endif

        float4 tangentDir : TEXCOORD6;
        float4 bitangentDir : TEXCOORD7;

        #if DISTANCE_BLENDED_TEXTURES
            #if DISTANCE_BLENDED_TEXTURES_FAST
                float4 distanceBlendedUVs : TEXCOORD8;
                float4 distanceBlendedRotatedUVs : TEXCOORD9;
            #else
                float4 uv : TEXCOORD8;
            #endif
        #else
            float4 uv : TEXCOORD8;
        #endif

        #if REFLECTION || REFRACTION || BLEND_SCALED_SPACE
            float4 screenGrabPos : TEXCOORD10;
        #endif

        half4 uv2 : TEXCOORD11;
        half4 uv3 : TEXCOORD12;
    };

    
#elif SRSTANDARD_OBJECT

    // NOTE: TRANSFER_VERTEX_TO_FRAGMENT (more specifically TRANSFER_SHADOW within it) must have a struct as input, with a vert pos named "v.vertex"
    // This only affects Android builds.
    struct vertInput
    {
        float4 vertex : POSITION;
        float3 normal : NORMAL;
        float2 uv : TEXCOORD0;

        #if TERRAIN_STRUCTURE_NORMAL_MAPS_ON
            float4 tangent : TANGENT;
        #endif
    };

    struct v2f
    {
        float4 pos : SV_POSITION;
        float3 worldPosition : TEXCOORD0;
        float3 worldNormal : NORMAL;
        
        float2 uv : TEXCOORD2;
        half4 ambient : TEXCOORD3;
        UNITY_SHADOW_COORDS(4)
        half3 lightColor: TEXCOORD8;

        #if ATMOSPHERE
            float3 atmosColor : COLOR0;
        #endif

        #if TERRAIN_STRUCTURE_NORMAL_MAPS_ON
            float4 tangentDir : TEXCOORD6;
            float4 bitangentDir : TEXCOORD7;
        #endif
    };

#elif SRSTANDARD_PART

    // NOTE: TRANSFER_VERTEX_TO_FRAGMENT (more specifically TRANSFER_SHADOW within it) must have a struct as input, with a vert pos named "v.vertex"
    // This only affects Android builds.
    struct vertInput
    {
        float4 vertex : POSITION;
        float3 normal : NORMAL;
        float4 uv1 : TEXCOORD0;
        float4 uv2 : TEXCOORD1;

        #if NORMAL_MAPS_ON
            float4 tangent : TANGENT;
        #endif

        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct v2f
    {
        float4 pos : SV_POSITION;
        float3 worldPosition : TEXCOORD0;
        float3 worldNormal : NORMAL;
        
        float3 uv : TEXCOORD2;
        float4 ids : TEXCOORD3;
        half4 ambient : TEXCOORD4;
        UNITY_SHADOW_COORDS(5)
        half3 lightColor: TEXCOORD9;

        #if NORMAL_MAPS_ON
            float4 tangentDir : TEXCOORD6;
            float4 bitangentDir : TEXCOORD7;
        #endif

        #if ATMOSPHERE
            float3 atmosColor : COLOR0;
        #endif

        UNITY_VERTEX_INPUT_INSTANCE_ID
    };
#elif SRSTANDARD_PART_TMPRO
    // NOTE: TRANSFER_VERTEX_TO_FRAGMENT (more specifically TRANSFER_SHADOW within it) must have a struct as input, with a vert pos named "v.vertex"
    // This only affects Android builds.
    struct vertInput
    {
        float4 vertex : POSITION;
        float3 normal : NORMAL;
        half4 color : COLOR;
        float2 uv1 : TEXCOORD0;
        float2 uv2 : TEXCOORD1;
    };

    struct v2f
    {
        float4 pos : SV_POSITION;
        float3 worldPosition : TEXCOORD0;
        float3 worldNormal : NORMAL;
        
        float2 uv : TEXCOORD2;
        float4 ids : TEXCOORD3;
        half4 ambient : TEXCOORD4;
        UNITY_SHADOW_COORDS(5)
        half3 lightColor: TEXCOORD9;

        #if ATMOSPHERE
            float3 atmosColor : COLOR0;
        #endif

        // TMPro
        float3 param : TEXCOORD6;
    };
#endif
#endif
#endif