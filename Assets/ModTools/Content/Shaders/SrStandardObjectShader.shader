Shader "Jundroo/SR Standard/SrStandardObjectShader" 
{
    Properties
    {
        [Header(PBR Options)]
        _metallicness("Metallicness", Range(0, 1)) = 0
        _smoothness("Smoothness", Range(0, 1)) = 0
        _texture("Texture", 2D) = "white" {}

        [Space(20)][Header(Normal Map Options)]
        _normalMap("Normal Map", 2D) = "bump" {}
        //[KeywordEnum(OFF, ON)] NORMAL_MAPS("Normal Maps", Float) = 0 // TODO-PERFORMANCE: Wire up environment normal maps on/off option.

        [HDR]
        _emissive("Emission", Color) = (0, 0, 0, 0)

        [Space(20)][Header(Color)]
        _colorMultiplier("Color Multiplier", Color) = (1, 1, 1, 1)

        [Space(20)][Header(Atmosphere Options)]
        [KeywordEnum(None, LOW, HIGH)] TERRAIN_ATMOSPHERE("Atmosphere Quality", Float) = 0
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
            #pragma multi_compile __ TERRAIN_STRUCTURE_NORMAL_MAPS_ON 
            #pragma multi_compile __ OBJECT_ATMOSPHERE
            #pragma multi_compile __ UNDERWATER
            #pragma multi_compile SR_LIGHTING_LOW SR_LIGHTING_MEDIUM SR_LIGHTING_HIGH
            //#pragma enable_d3d11_debug_symbols

            //#pragma target 3.5 // 3.5 adds instancing which cause the shader to be excluded on Adreno 3xx GPUs
            #pragma require derivatives interpolators10 interpolators15 samplelod fragcoord mrt4 integers 2darray
            
            #define UNITY_PASS_FORWARDBASE 1

            #include "SrStandardObjectShaderPass.cginc"


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
            #pragma multi_compile __ TERRAIN_STRUCTURE_NORMAL_MAPS_ON 
            #pragma multi_compile __ UNDERWATER
            #pragma multi_compile SR_LIGHTING_LOW SR_LIGHTING_MEDIUM SR_LIGHTING_HIGH
            #pragma multi_compile_fwdadd_fullshadows
            //#pragma enable_d3d11_debug_symbols

            //#pragma target 3.5 // 3.5 adds instancing which cause the shader to be excluded on Adreno 3xx GPUs
            #pragma require derivatives interpolators10 interpolators15 samplelod fragcoord mrt4 integers 2darray
            
            #define UNITY_PASS_FORWARDADD 1

            #include "SrStandardObjectShaderPass.cginc"


            ENDCG
        }
    }
    Fallback "VertexLit"
}