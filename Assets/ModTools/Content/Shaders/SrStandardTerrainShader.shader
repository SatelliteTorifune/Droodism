Shader "Jundroo/SR Standard/SrStandardTerrainShader" 
{
    Properties
    {
        [Header(Detail Texture Splatmap Options)]
        [KeywordEnum(None, 4_TEXTURES, 8_TEXTURES)] DETAIL_SPLATMAP("Detail Splatmap Type", Float) = 0
        [KeywordEnum(DEFAULT, FAST)] DISTANCE_BLENDED_TEXTURES("Texture Blend Quality", Float) = 0
        [NoScaleOffset] _detailSplatTexture1("Detail Splat Texture 1", 2D) = "gray" {}
        [NoScaleOffset] _detailSplatTexture2("Detail Splat Texture 2", 2D) = "gray" {}

        [Space(20)][Header(Ground Detail Texture Splatmap Options)]
        [KeywordEnum(None, 4_TEXTURES, 8_TEXTURES)] GROUND_DETAIL_SPLATMAP("Ground Detail Splatmap Type", Float) = 0
        [NoScaleOffset] _groundDetailSplatTexture1("Ground Detail Splat Texture 1", 2D) = "gray" {}
        [NoScaleOffset] _groundDetailSplatTexture2("Ground Detail Splat Texture 2", 2D) = "gray" {}
        _groundDetailSplatTilingScale("Ground Detail Splat Tiling Scale", Float) = 1

        [Space(20)][Header(Atmosphere Options)]
        [KeywordEnum(None, LOW, HIGH)] TERRAIN_ATMOSPHERE("Atmosphere Quality", Float) = 0

        [Space(20)][Header(Light Options)]
        [KeywordEnum(PBR_SIMPLE, PBR_FULL)] LIGHT("Lighting Model", Float) = 0
        [KeywordEnum(LOW, MEDIUM, HIGH)] SR_LIGHTING("Lighting Quality", Float) = 0
    }

    SubShader
    { 
        // Shadows in non-surf shaders: http://joeyfladderak.com/let-there-be-shadow/
        Pass 
        {
            Tags { "LightMode" = "ForwardBase" }

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile __ DETAIL_SPLATMAP_4_TEXTURES DETAIL_SPLATMAP_8_TEXTURES
            #pragma multi_compile __ GROUND_DETAIL_SPLATMAP_4_TEXTURES GROUND_DETAIL_SPLATMAP_8_TEXTURES
            #pragma multi_compile __ TERRAIN_ATMOSPHERE
            #pragma multi_compile __ DISTANCE_BLENDED_TEXTURES_FAST
            #pragma multi_compile __ BLEND_SCALED_SPACE
            #pragma multi_compile __ UNDERWATER
            #pragma multi_compile SR_LIGHTING_LOW SR_LIGHTING_MEDIUM SR_LIGHTING_HIGH
            //#pragma enable_d3d11_debug_symbols

            //#pragma target 3.5 // 3.5 adds instancing which cause the shader to be excluded on Adreno 3xx GPUs
            #pragma require derivatives interpolators10 interpolators15 samplelod fragcoord mrt4 integers 2darray

            #define UNITY_PASS_FORWARDBASE 1

            #include "SrStandardTerrainShaderPass.cginc"

            ENDCG
        }

        Pass 
        {
            Tags { "LightMode" = "ForwardAdd" }

            Blend One One
            ZWrite Off

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdadd_fullshadows
            #pragma multi_compile __ DETAIL_SPLATMAP_4_TEXTURES DETAIL_SPLATMAP_8_TEXTURES
            #pragma multi_compile __ GROUND_DETAIL_SPLATMAP_4_TEXTURES GROUND_DETAIL_SPLATMAP_8_TEXTURES
            #pragma multi_compile __ UNDERWATER
            #pragma multi_compile SR_LIGHTING_LOW SR_LIGHTING_MEDIUM SR_LIGHTING_HIGH
            //#pragma enable_d3d11_debug_symbols

            //#pragma target 3.5 // 3.5 adds instancing which cause the shader to be excluded on Adreno 3xx GPUs
            #pragma require derivatives interpolators10 interpolators15 samplelod fragcoord mrt4 integers 2darray

            #define UNITY_PASS_FORWARDADD 1

            #include "SrStandardTerrainShaderPass.cginc"

            ENDCG
        }
    }
    Fallback "VertexLit"
}