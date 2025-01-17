Shader "Jundroo/SR Standard/SrStandardPartShader" 
{
    Properties
    {
        [Space(20)] [Header(Base Pass Config)]
        [Enum(UnityEngine.Rendering.BlendMode)] _BaseBlendModeSource("_BaseBlendModeSource", float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)]_BaseBlendModeDestination("_BaseBlendModeDestination", float) = 0
        [Enum(Off,0,On,1)]_BaseZWrite("ZWrite", Float) = 1.0

        [Space(20)][Header(Forward Pass Config)]
        [Enum(UnityEngine.Rendering.BlendMode)]_ForwardBlendModeSource("_ForwardBlendModeSource", float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)]_ForwardBlendModeDestination("_ForwardBlendModeDestination", float) = 1
        [Enum(Off,0,On,1)]_ForwardZWrite("ZWrite", Float) = 0.0

        [Space(20)][Header(Textures)]
        _DetailTextures("Detail Textures", 2DArray) = "" {}
        _NormalMapTextures("Normal Maps Textures", 2DArray) = "" {}
        _DecalTexture("Decal Texture", 2D) = "black" {}
        _DecalTextureMaterialIds("Decal Materials", Vector) = (0,0,1,1)
        _UseDecalTexture("Use Decal Texture", float) = 0
        _AlphaOverride("Alpha Override", float) = -1

        [Space(20)][Header(Texture Options)]
        [KeywordEnum(OFF, ON)] DETAIL_TEXTURES("Detail Textures", Float) = 0
        [KeywordEnum(OFF, ON)] NORMAL_MAPS("Normal Maps", Float) = 0

        [Space(20)][Header(Scene Variant)]
        [KeywordEnum(OTHER, FLIGHT)] SCENE("Scene", Float) = 0

        [Space(20)][Header(Rim Shading Options)]
        [KeywordEnum(OFF, ON)] RIMSHADE("Rim Shading", Float) = 0
        _Color("Color", Color) = (1,1,1,1)
        _MinPower("Min Power", Range(0.0, 1.0)) = 0.1
        _MaxPower("Max Power", Range(0.0, 1.0)) = 1.0

        [Space(20)][Header(Atmosphere Options)]
        [KeywordEnum(None, LOW, HIGH)] OBJECT_ATMOSPHERE("Atmosphere Quality", Float) = 0

        [Space(20)][Header(Mask Render)]
        _ReentryMaskBaseStrength("Reentry Mask Base Strength", Range(0.0, 10.0)) = 2.0
        _ReentryMaskWrapAmount("Reentry Mask WrapAmount", Range(-1.0, 0.0)) = -1.0
        _VaporMaskBaseStrength("Vapor Mask Base Strength", Range(0.0, 10.0)) = 1.0
        _VaporMaskWrapAmount("Vapor Mask WrapAmount", Range(-1.0, 0.0)) = -0.2

        // Unused _MainTex that is here to prevent errors being logged from TMPro for the label part
        _MainTex("Main Texture (Unused)", 2D) = "black" {}
    }

    SubShader
    { 
        Stencil
        {
            Ref 0
            Comp Always
            Pass Replace
        }

        // Shadows in non-surf shaders: http://joeyfladderak.com/let-there-be-shadow/
        Pass 
        {
            Tags { "LightMode" = "ForwardBase" }

            Blend[_BaseBlendModeSource][_BaseBlendModeDestination]
            ZWrite [_BaseZWrite]
            ZTest LEqual

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile __ RIMSHADE_ON
            #pragma multi_compile __ DETAIL_TEXTURES_ON
            #pragma multi_compile __ NORMAL_MAPS_ON
            #pragma multi_compile __ CRAFT_MASK_RENDER_ON
            #pragma multi_compile __ OBJECT_ATMOSPHERE
            #pragma multi_compile __ UNDERWATER
            #pragma multi_compile SR_LIGHTING_LOW SR_LIGHTING_MEDIUM SR_LIGHTING_HIGH
            #pragma multi_compile_instancing
            //#pragma enable_d3d11_debug_symbols

            //#pragma target 3.5 // 3.5 adds instancing which cause the shader to be excluded on Adreno 3xx GPUs
            #pragma require derivatives interpolators10 interpolators15 samplelod fragcoord mrt4 integers 2darray

            #define UNITY_PASS_FORWARDBASE 1

            #include "SrStandardPartShaderPass.cginc"

            ENDCG
        }
            
        Pass 
        {
            Tags { "LightMode" = "ForwardAdd" }
            
            Blend [_ForwardBlendModeSource] [_ForwardBlendModeDestination]
            ZWrite[_ForwardZWrite]
            ZTest LEqual

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdadd_fullshadows
            #pragma multi_compile __ NORMAL_MAPS_ON
            #pragma multi_compile __ DETAIL_TEXTURES_ON
            #pragma multi_compile __ UNDERWATER
            #pragma multi_compile SR_LIGHTING_LOW SR_LIGHTING_MEDIUM SR_LIGHTING_HIGH
            #pragma multi_compile_instancing
            //#pragma enable_d3d11_debug_symbols

            //#pragma target 3.5 // 3.5 adds instancing which cause the shader to be excluded on Adreno 3xx GPUs
            #pragma require derivatives interpolators10 interpolators15 samplelod fragcoord mrt4 integers 2darray
            
            #define UNITY_PASS_FORWARDADD 1

            #include "SrStandardPartShaderPass.cginc"


            ENDCG
        }

    }
    Fallback "VertexLit"
}