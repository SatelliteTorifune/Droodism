Shader "Jundroo/SR Standard/SrStandardPartTMProShader"
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

        [Space(20)][Header(Atmosphere Options)]
        [KeywordEnum(None, LOW, HIGH)] OBJECT_ATMOSPHERE("Atmosphere Quality", Float) = 0

        // Text Mesh Pro
        [Space(20)][Header(Text Mesh Pro)]
        _FaceTex("Fill Texture", 2D) = "white" {}
        _FaceColor("Fill Color", Color) = (1,1,1,1)
        _FaceDilate("Face Dilate", Range(-1,1)) = 0

        _OutlineColor("Outline Color", Color) = (0,0,0,1)
        _OutlineTex("Outline Texture", 2D) = "white" {}
        _OutlineWidth("Outline Thickness", Range(0, 1)) = 0
        _OutlineSoftness("Outline Softness", Range(0,1)) = 0

        _WeightNormal("Weight Normal", float) = 0
        _WeightBold("Weight Bold", float) = 0.5

        _AlphaCutoff("Alpha Cutoff", float) = 0.1

            // Should not be directly exposed to the user
            _ScaleRatioA("Scale RatioA", float) = 1

            _MainTex("Font Atlas", 2D) = "white" {}
            _TextureWidth("Texture Width", float) = 512
            _TextureHeight("Texture Height", float) = 512
            _GradientScale("Gradient Scale", float) = 5.0
            _ScaleX("Scale X", float) = 1.0
            _ScaleY("Scale Y", float) = 1.0
            _PerspectiveFilter("Perspective Correction", Range(0, 1)) = 0.875
            _Sharpness("Sharpness", Range(-1,1)) = 0

            _VertexOffsetX("Vertex OffsetX", float) = 0
            _VertexOffsetY("Vertex OffsetY", float) = 0
    }

    SubShader
    {
        Stencil
        {
            Ref 0
            Comp Always
            Pass Replace
        }

        Pass
        {
            Tags 
            { 
                "LightMode" = "ForwardBase"
                "Queue" = "AlphaTest"
                "IgnoreProjector" = "True"
                "RenderType" = "TransparentCutout"
            }

            Blend[_BaseBlendModeSource][_BaseBlendModeDestination]
            ZWrite[_BaseZWrite]
            ZTest LEqual
            Cull[_CullMode]
            ColorMask RGB

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile __ OBJECT_ATMOSPHERE
            #pragma multi_compile __ UNDERWATER
            #pragma multi_compile SR_LIGHTING_LOW SR_LIGHTING_MEDIUM SR_LIGHTING_HIGH
            //#pragma enable_d3d11_debug_symbols

            //#pragma target 3.5 // 3.5 adds instancing which cause the shader to be excluded on Adreno 3xx GPUs
            #pragma require derivatives interpolators10 interpolators15 samplelod fragcoord mrt4 integers 2darray

            #define UNITY_PASS_FORWARDBASE 1

            #include "SrStandardPartTMProShaderPass.cginc"

            ENDCG
        }

        Pass
        {
            Tags
            {
                "LightMode" = "ForwardAdd"
                "Queue" = "AlphaTest"
                "IgnoreProjector" = "True"
                "RenderType" = "TransparentCutout"
            }

            Blend[_ForwardBlendModeSource][_ForwardBlendModeDestination]
            ZWrite[_ForwardZWrite]
            ZTest LEqual
            Cull[_CullMode]
            ColorMask RGB

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdadd_fullshadows
            #pragma multi_compile __ UNDERWATER
            #pragma multi_compile SR_LIGHTING_LOW SR_LIGHTING_MEDIUM SR_LIGHTING_HIGH
            //#pragma enable_d3d11_debug_symbols

            //#pragma target 3.5 // 3.5 adds instancing which cause the shader to be excluded on Adreno 3xx GPUs
            #pragma require derivatives interpolators10 interpolators15 samplelod fragcoord mrt4 integers 2darray

            #define UNITY_PASS_FORWARDADD 1

            #include "SrStandardPartTMProShaderPass.cginc"


            ENDCG
        }

        // Pass to render object as a shadow caster
        Pass
        {
            Name "Caster"
            Tags { "LightMode" = "ShadowCaster" }
            Offset 1, 1

            Fog {Mode Off}
            ZWrite On 
            ZTest LEqual 
            Cull Off

            CGPROGRAM
            #pragma vertex vert_shadow
            #pragma fragment frag_shadow
            #pragma multi_compile_shadowcaster

            #define SR_LIGHTING_LOW 1
            #define SRSTANDARD_PART_TMPRO_SHADOWCASTER 1

            #include "SrStandardPartTMProShaderPass.cginc"

            ENDCG
        }

    }
    Fallback "VertexLit"
}