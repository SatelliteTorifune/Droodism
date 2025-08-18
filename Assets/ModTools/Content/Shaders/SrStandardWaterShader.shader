Shader "Jundroo/SR Standard/SrStandardWaterShader" 
{
    Properties
    {
        [Header(Atmosphere Options)]
        [KeywordEnum(None, LOW, HIGH)] TERRAIN_ATMOSPHERE("Atmosphere Quality", Float) = 0

        [Header(Textures)]
        [NoScaleOffset] _WaveNormalMap("Wave Normal Map", 2D) = "bump" {}
        [NoScaleOffset] _WaveNormalMap2("Wave Normal Map 2", 2D) = "bump" {}

        [Header(Water Settings)]
        [KeywordEnum(BASIC, BLENDED_FAST, BLENDED)] WATER_NORMAL_MAPS("Normal Maps Type", Float) = 0
        [KeywordEnum(BI_DIRECTIONAL, OPPOSING_BI_DIRECTIONAL, OMNI_DIRECTIONAL)] WATER_MOVEMENT("Movement Type", Float) = 0
        _MaxTransparencyDepth("Max Transparency Depth", Float) = 15
        _RefractionDistortionStrength("Refraction Distortion Strength", Range(0.0, 4.0)) = 2
        _ReflectionDistortionStrength("Reflection Distortion Strength", Range(0.0, 100.0)) = 20
        _FresnelBias("Fresnel Bias", Range(0.0, 1.0)) = 0.2
        _FoamColor("Foam Color", Color) = (1,1,1,1)
        _FoamDepthInverse("Foam Depth Inverse", Float) = 0.5

        [Header(Wave Settings)]
        _TessellationEdgeLength("Tessellation Edge Length", Range(5, 100)) = 50
        _WaveAmplitude("Wave Amplitude", Range(0, 100)) = .25
        _WaveLength("Wave Length", Range(1, 1000)) = 25
        _WaveSpeed("Wave Speed", Range(0, 100)) = 1
        _MaxDisplacementDist("MaxDisplacementDist", Range(0, 100000)) = 1
        _WaveTime("WaveTime", Range(-50, 50)) = 0
        _WaveOffset("_WaveOffset", Vector) = (0, 0, 0)

        [Space(20)][Header(Light Options)]
        [KeywordEnum(LOW, MEDIUM, HIGH)] SR_LIGHTING("Lighting Quality", Float) = 0
    }

    SubShader
    {
        // Tessellation with Refraction and Reflection
        Lod 540

        Tags
        {
            "IgnoreProjector" = "True"
            "Queue" = "Transparent-50"
            "RenderType" = "Transparent"
        }
        
        // The GrabPass is handled manually with a command buffer.
        // This was done because this GrabPass was causing the SetPass calls to skyrocket.
        ////GrabPass{ "_WaterRefractionTexture" }

        Pass 
        {
            Tags 
            { 
                "LightMode" = "ForwardBase"
            }

            Blend Off
            ZWrite On
            ZTest LEqual
            Cull Off

            CGPROGRAM

            #pragma vertex vertTess
            #pragma fragment frag
            #pragma hull hull
            #pragma domain domain
            #pragma multi_compile_fwdbase
            #pragma multi_compile __ TERRAIN_ATMOSPHERE
            #pragma multi_compile __ WATER_NORMAL_MAPS_BLENDED_FAST WATER_NORMAL_MAPS_BLENDED
            #pragma multi_compile __ BLEND_SCALED_SPACE
            #pragma multi_compile __ UNDERWATER
            #pragma shader_feature WATER_MOVEMENT_BI_DIRECTIONAL WATER_MOVEMENT_OPPOSING_BI_DIRECTIONAL WATER_MOVEMENT_OMNI_DIRECTIONAL
            #pragma multi_compile SR_LIGHTING_LOW SR_LIGHTING_MEDIUM SR_LIGHTING_HIGH
            //#pragma enable_d3d11_debug_symbols

            #pragma target 4.6
            
            #define UNITY_PASS_FORWARDBASE 1
            #define REFRACTION 1
            #define REFLECTION 1
            #define WAVES 1
            #if UNDERWATER
                #undef REFLECTION
            #endif

            #include "SrStandardWaterShaderPass.cginc"

            ENDCG
        }

        Pass 
        {
            Tags { "LightMode" = "ForwardAdd" }

            Blend One One
            ZWrite Off
            ZTest LEqual

            CGPROGRAM

            #pragma vertex vertTess
            #pragma fragment frag
            #pragma hull hull
            #pragma domain domain
            #pragma multi_compile_fwdadd
            #pragma multi_compile __ WATER_NORMAL_MAPS_BLENDED_FAST WATER_NORMAL_MAPS_BLENDED
            #pragma multi_compile __ UNDERWATER
            #pragma shader_feature WATER_MOVEMENT_BI_DIRECTIONAL WATER_MOVEMENT_OPPOSING_BI_DIRECTIONAL WATER_MOVEMENT_OMNI_DIRECTIONAL
            #pragma multi_compile SR_LIGHTING_LOW SR_LIGHTING_MEDIUM SR_LIGHTING_HIGH
            //#pragma enable_d3d11_debug_symbols

            #pragma target 4.6
            
            #define UNITY_PASS_FORWARDADD 1
            #define WAVES 1

            #include "SrStandardWaterShaderPass.cginc"

            ENDCG
        }
    }

    SubShader
    {
        // Tessellation with Refraction but NO Reflection
        Lod 530

        Tags
        {
            "IgnoreProjector" = "True"
            "Queue" = "Transparent-50"
            "RenderType" = "Transparent"
        }

        // The GrabPass is handled manually with a command buffer.
        // This was done because this GrabPass was causing the SetPass calls to skyrocket.
        ////GrabPass{ "_WaterRefractionTexture" }

        Pass 
        {
            Tags 
            { 
                "LightMode" = "ForwardBase"
            }

            Blend Off
            ZWrite On
            ZTest LEqual
            Cull Off

            CGPROGRAM

            #pragma vertex vertTess
            #pragma fragment frag
            #pragma hull hull
            #pragma domain domain
            #pragma multi_compile_fwdbase
            #pragma multi_compile __ TERRAIN_ATMOSPHERE
            #pragma multi_compile __ WATER_NORMAL_MAPS_BLENDED_FAST WATER_NORMAL_MAPS_BLENDED
            #pragma multi_compile __ BLEND_SCALED_SPACE
            #pragma multi_compile __ UNDERWATER
            #pragma shader_feature WATER_MOVEMENT_BI_DIRECTIONAL WATER_MOVEMENT_OPPOSING_BI_DIRECTIONAL WATER_MOVEMENT_OMNI_DIRECTIONAL
            #pragma multi_compile SR_LIGHTING_LOW SR_LIGHTING_MEDIUM SR_LIGHTING_HIGH
            //#pragma enable_d3d11_debug_symbols

            #pragma target 4.6
            
            #define UNITY_PASS_FORWARDBASE 1
            #define REFRACTION 1
            #define WAVES 1

            #include "SrStandardWaterShaderPass.cginc"

            ENDCG
        }

        Pass 
        {
            Tags { "LightMode" = "ForwardAdd" }

            Blend One One
            ZWrite Off
            ZTest LEqual

            CGPROGRAM

            #pragma vertex vertTess
            #pragma fragment frag
            #pragma hull hull
            #pragma domain domain
            #pragma multi_compile_fwdadd
            #pragma multi_compile __ WATER_NORMAL_MAPS_BLENDED_FAST WATER_NORMAL_MAPS_BLENDED
            #pragma multi_compile __ UNDERWATER
            #pragma shader_feature WATER_MOVEMENT_BI_DIRECTIONAL WATER_MOVEMENT_OPPOSING_BI_DIRECTIONAL WATER_MOVEMENT_OMNI_DIRECTIONAL
            #pragma multi_compile SR_LIGHTING_LOW SR_LIGHTING_MEDIUM SR_LIGHTING_HIGH
            //#pragma enable_d3d11_debug_symbols

            #pragma target 4.6
            
            #define UNITY_PASS_FORWARDADD 1
            #define WAVES 1

            #include "SrStandardWaterShaderPass.cginc"

            ENDCG
        }
    }

    SubShader
    {
        // Tessellation with Reflection but NO Refraction
        Lod 520

        Tags
        {
            "Queue" = "Geometry+100"
            "RenderType" = "Opaque"
        }
            
        Pass 
        {
            Tags 
            { 
                "LightMode" = "ForwardBase"
            }

            Blend Off
            ZWrite On
            ZTest LEqual
            Cull Off

            CGPROGRAM

            #pragma vertex vertTess
            #pragma fragment frag
            #pragma hull hull
            #pragma domain domain
            #pragma multi_compile_fwdbase
            #pragma multi_compile __ TERRAIN_ATMOSPHERE
            #pragma multi_compile __ WATER_NORMAL_MAPS_BLENDED_FAST WATER_NORMAL_MAPS_BLENDED
            #pragma multi_compile __ BLEND_SCALED_SPACE
            #pragma multi_compile __ UNDERWATER
            #pragma shader_feature WATER_MOVEMENT_BI_DIRECTIONAL WATER_MOVEMENT_OPPOSING_BI_DIRECTIONAL WATER_MOVEMENT_OMNI_DIRECTIONAL
            #pragma multi_compile SR_LIGHTING_LOW SR_LIGHTING_MEDIUM SR_LIGHTING_HIGH
            //#pragma enable_d3d11_debug_symbols

            #pragma target 4.6
            
            #define UNITY_PASS_FORWARDBASE 1
            #define REFLECTION 1
            #define WAVES 1
            #if UNDERWATER
                #undef REFLECTION
            #endif

            #include "SrStandardWaterShaderPass.cginc"

            ENDCG
        }

        Pass 
        {
            Tags { "LightMode" = "ForwardAdd" }

            Blend One One
            ZWrite Off
            ZTest LEqual

            CGPROGRAM

            #pragma vertex vertTess
            #pragma fragment frag
            #pragma hull hull
            #pragma domain domain
            #pragma multi_compile_fwdadd
            #pragma multi_compile __ WATER_NORMAL_MAPS_BLENDED_FAST WATER_NORMAL_MAPS_BLENDED
            #pragma multi_compile __ UNDERWATER
            #pragma shader_feature WATER_MOVEMENT_BI_DIRECTIONAL WATER_MOVEMENT_OPPOSING_BI_DIRECTIONAL WATER_MOVEMENT_OMNI_DIRECTIONAL
            #pragma multi_compile SR_LIGHTING_LOW SR_LIGHTING_MEDIUM SR_LIGHTING_HIGH
            //#pragma enable_d3d11_debug_symbols

            #pragma target 4.6
            
            #define UNITY_PASS_FORWARDADD 1
            #define WAVES 1

            #include "SrStandardWaterShaderPass.cginc"

            ENDCG
        }
    }
        
    SubShader
    {
        // Tessellation with NO Refraction OR Reflection
        Lod 510

        Tags
        {
            "Queue" = "Geometry+100"
            "RenderType" = "Opaque"
        }

        Pass 
        {
            Tags 
            { 
                "LightMode" = "ForwardBase"
            }

            Blend Off
            ZWrite On
            ZTest LEqual
            Cull Off

            CGPROGRAM

            #pragma vertex vertTess
            #pragma fragment frag
            #pragma hull hull
            #pragma domain domain
            #pragma multi_compile_fwdbase
            #pragma multi_compile __ TERRAIN_ATMOSPHERE
            #pragma multi_compile __ WATER_NORMAL_MAPS_BLENDED_FAST WATER_NORMAL_MAPS_BLENDED
            #pragma multi_compile __ BLEND_SCALED_SPACE
            #pragma multi_compile __ UNDERWATER
            #pragma shader_feature WATER_MOVEMENT_BI_DIRECTIONAL WATER_MOVEMENT_OPPOSING_BI_DIRECTIONAL WATER_MOVEMENT_OMNI_DIRECTIONAL
            #pragma multi_compile SR_LIGHTING_LOW SR_LIGHTING_MEDIUM SR_LIGHTING_HIGH
            //#pragma enable_d3d11_debug_symbols

            #pragma target 4.6
            
            #define UNITY_PASS_FORWARDBASE 1
            #define WAVES 1

            #include "SrStandardWaterShaderPass.cginc"

            ENDCG
        }

        Pass 
        {
            Tags { "LightMode" = "ForwardAdd" }

            Blend One One
            ZWrite Off
            ZTest LEqual

            CGPROGRAM

            #pragma vertex vertTess
            #pragma fragment frag
            #pragma hull hull
            #pragma domain domain
            #pragma multi_compile_fwdadd
            #pragma multi_compile __ WATER_NORMAL_MAPS_BLENDED_FAST WATER_NORMAL_MAPS_BLENDED
            #pragma multi_compile __ UNDERWATER
            #pragma shader_feature WATER_MOVEMENT_BI_DIRECTIONAL WATER_MOVEMENT_OPPOSING_BI_DIRECTIONAL WATER_MOVEMENT_OMNI_DIRECTIONAL
            #pragma multi_compile SR_LIGHTING_LOW SR_LIGHTING_MEDIUM SR_LIGHTING_HIGH
            //#pragma enable_d3d11_debug_symbols

            #pragma target 4.6
            
            #define UNITY_PASS_FORWARDADD 1
            #define WAVES 1

            #include "SrStandardWaterShaderPass.cginc"

            ENDCG
        }
    }

    SubShader
    {
        // Refraction and Reflection
        Lod 400

        Tags
        {
            "IgnoreProjector" = "True"
            "Queue" = "Transparent-50"
            "RenderType" = "Transparent"
        }
        
        // The GrabPass is handled manually with a command buffer.
        // This was done because this GrabPass was causing the SetPass calls to skyrocket.
        ////GrabPass{ "_WaterRefractionTexture" }

        Pass 
        {
            Tags 
            { 
                "LightMode" = "ForwardBase"
            }

            Blend Off
            ZWrite On
            ZTest LEqual
            Cull Off

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile __ TERRAIN_ATMOSPHERE
            #pragma multi_compile __ WATER_NORMAL_MAPS_BLENDED_FAST WATER_NORMAL_MAPS_BLENDED
            #pragma multi_compile __ BLEND_SCALED_SPACE
            #pragma multi_compile __ UNDERWATER
            #pragma shader_feature WATER_MOVEMENT_BI_DIRECTIONAL WATER_MOVEMENT_OPPOSING_BI_DIRECTIONAL WATER_MOVEMENT_OMNI_DIRECTIONAL
            #pragma multi_compile SR_LIGHTING_LOW SR_LIGHTING_MEDIUM SR_LIGHTING_HIGH
            //#pragma enable_d3d11_debug_symbols

            //#pragma target 3.5 // 3.5 adds instancing which cause the shader to be excluded on Adreno 3xx GPUs
            #pragma require derivatives interpolators10 interpolators15 samplelod fragcoord mrt4 integers 2darray
            
            #define UNITY_PASS_FORWARDBASE 1
            #define REFRACTION 1
            #define REFLECTION 1
            #if UNDERWATER
                #undef REFLECTION
            #endif

            #include "SrStandardWaterShaderPass.cginc"

            ENDCG
        }

        Pass 
        {
            Tags { "LightMode" = "ForwardAdd" }

            Blend One One
            ZWrite Off
            ZTest LEqual

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdadd
            #pragma multi_compile __ WATER_NORMAL_MAPS_BLENDED_FAST WATER_NORMAL_MAPS_BLENDED
            #pragma multi_compile __ UNDERWATER
            #pragma shader_feature WATER_MOVEMENT_BI_DIRECTIONAL WATER_MOVEMENT_OPPOSING_BI_DIRECTIONAL WATER_MOVEMENT_OMNI_DIRECTIONAL
            #pragma multi_compile SR_LIGHTING_LOW SR_LIGHTING_MEDIUM SR_LIGHTING_HIGH
            //#pragma enable_d3d11_debug_symbols

            //#pragma target 3.5 // 3.5 adds instancing which cause the shader to be excluded on Adreno 3xx GPUs
            #pragma require derivatives interpolators10 interpolators15 samplelod fragcoord mrt4 integers 2darray
            
            #define UNITY_PASS_FORWARDADD 1

            #include "SrStandardWaterShaderPass.cginc"

            ENDCG
        }
    }

    SubShader
    {
        // Refraction but NO Reflection
        Lod 300

        Tags
        {
            "IgnoreProjector" = "True"
            "Queue" = "Transparent-50"
            "RenderType" = "Transparent"
        }

        // The GrabPass is handled manually with a command buffer.
        // This was done because this GrabPass was causing the SetPass calls to skyrocket.
        ////GrabPass{ "_WaterRefractionTexture" }

        Pass 
        {
            Tags 
            { 
                "LightMode" = "ForwardBase"
            }

            Blend Off
            ZWrite On
            ZTest LEqual
            Cull Off

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile __ TERRAIN_ATMOSPHERE
            #pragma multi_compile __ WATER_NORMAL_MAPS_BLENDED_FAST WATER_NORMAL_MAPS_BLENDED
            #pragma multi_compile __ BLEND_SCALED_SPACE
            #pragma multi_compile __ UNDERWATER
            #pragma shader_feature WATER_MOVEMENT_BI_DIRECTIONAL WATER_MOVEMENT_OPPOSING_BI_DIRECTIONAL WATER_MOVEMENT_OMNI_DIRECTIONAL
            #pragma multi_compile SR_LIGHTING_LOW SR_LIGHTING_MEDIUM SR_LIGHTING_HIGH
            //#pragma enable_d3d11_debug_symbols

            //#pragma target 3.5 // 3.5 adds instancing which cause the shader to be excluded on Adreno 3xx GPUs
            #pragma require derivatives interpolators10 interpolators15 samplelod fragcoord mrt4 integers 2darray
            
            #define UNITY_PASS_FORWARDBASE 1
            #define REFRACTION 1

            #include "SrStandardWaterShaderPass.cginc"

            ENDCG
        }

        Pass 
        {
            Tags { "LightMode" = "ForwardAdd" }

            Blend One One
            ZWrite Off
            ZTest LEqual

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdadd
            #pragma multi_compile __ WATER_NORMAL_MAPS_BLENDED_FAST WATER_NORMAL_MAPS_BLENDED
            #pragma multi_compile __ UNDERWATER
            #pragma shader_feature WATER_MOVEMENT_BI_DIRECTIONAL WATER_MOVEMENT_OPPOSING_BI_DIRECTIONAL WATER_MOVEMENT_OMNI_DIRECTIONAL
            #pragma multi_compile SR_LIGHTING_LOW SR_LIGHTING_MEDIUM SR_LIGHTING_HIGH
            //#pragma enable_d3d11_debug_symbols

            //#pragma target 3.5 // 3.5 adds instancing which cause the shader to be excluded on Adreno 3xx GPUs
            #pragma require derivatives interpolators10 interpolators15 samplelod fragcoord mrt4 integers 2darray
            
            #define UNITY_PASS_FORWARDADD 1

            #include "SrStandardWaterShaderPass.cginc"

            ENDCG
        }
    }

    SubShader
    {
        // Reflection but NO Refraction
        Lod 200

        Tags
        {
            "Queue" = "Geometry+100"
            "RenderType" = "Opaque"
        }
            
        Pass 
        {
            Tags 
            { 
                "LightMode" = "ForwardBase"
            }

            Blend Off
            ZWrite On
            ZTest LEqual
            Cull Off

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile __ TERRAIN_ATMOSPHERE
            #pragma multi_compile __ WATER_NORMAL_MAPS_BLENDED_FAST WATER_NORMAL_MAPS_BLENDED
            #pragma multi_compile __ BLEND_SCALED_SPACE
            #pragma multi_compile __ UNDERWATER
            #pragma shader_feature WATER_MOVEMENT_BI_DIRECTIONAL WATER_MOVEMENT_OPPOSING_BI_DIRECTIONAL WATER_MOVEMENT_OMNI_DIRECTIONAL
            #pragma multi_compile SR_LIGHTING_LOW SR_LIGHTING_MEDIUM SR_LIGHTING_HIGH
            //#pragma enable_d3d11_debug_symbols

            //#pragma target 3.5 // 3.5 adds instancing which cause the shader to be excluded on Adreno 3xx GPUs
            #pragma require derivatives interpolators10 interpolators15 samplelod fragcoord mrt4 integers 2darray
            
            #define UNITY_PASS_FORWARDBASE 1
            #define REFLECTION 1
            #if UNDERWATER
                #undef REFLECTION
            #endif

            #include "SrStandardWaterShaderPass.cginc"

            ENDCG
        }

        Pass 
        {
            Tags { "LightMode" = "ForwardAdd" }

            Blend One One
            ZWrite Off
            ZTest LEqual

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdadd_fullshadows
            #pragma multi_compile __ WATER_NORMAL_MAPS_BLENDED_FAST WATER_NORMAL_MAPS_BLENDED
            #pragma multi_compile __ UNDERWATER
            #pragma shader_feature WATER_MOVEMENT_BI_DIRECTIONAL WATER_MOVEMENT_OPPOSING_BI_DIRECTIONAL WATER_MOVEMENT_OMNI_DIRECTIONAL
            #pragma multi_compile SR_LIGHTING_LOW SR_LIGHTING_MEDIUM SR_LIGHTING_HIGH
            //#pragma enable_d3d11_debug_symbols

            //#pragma target 3.5 // 3.5 adds instancing which cause the shader to be excluded on Adreno 3xx GPUs
            #pragma require derivatives interpolators10 interpolators15 samplelod fragcoord mrt4 integers 2darray
            
            #define UNITY_PASS_FORWARDADD 1

            #include "SrStandardWaterShaderPass.cginc"

            ENDCG
        }
    }
        
    SubShader
    {
        // NO Refraction OR Reflection
        Lod 100

        Tags
        {
            "Queue" = "Geometry+100"
            "RenderType" = "Opaque"
        }

        Pass 
        {
            Tags 
            { 
                "LightMode" = "ForwardBase"
            }

            Blend Off
            ZWrite On
            ZTest LEqual
            Cull Off

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile __ TERRAIN_ATMOSPHERE
            #pragma multi_compile __ WATER_NORMAL_MAPS_BLENDED_FAST WATER_NORMAL_MAPS_BLENDED
            #pragma multi_compile __ BLEND_SCALED_SPACE
            #pragma multi_compile __ UNDERWATER
            #pragma shader_feature WATER_MOVEMENT_BI_DIRECTIONAL WATER_MOVEMENT_OPPOSING_BI_DIRECTIONAL WATER_MOVEMENT_OMNI_DIRECTIONAL
            #pragma multi_compile SR_LIGHTING_LOW SR_LIGHTING_MEDIUM SR_LIGHTING_HIGH
            //#pragma enable_d3d11_debug_symbols

            //#pragma target 3.5 // 3.5 adds instancing which cause the shader to be excluded on Adreno 3xx GPUs
            #pragma require derivatives interpolators10 interpolators15 samplelod fragcoord mrt4 integers 2darray
            
            #define UNITY_PASS_FORWARDBASE 1

            #include "SrStandardWaterShaderPass.cginc"

            ENDCG
        }

        Pass 
        {
            Tags { "LightMode" = "ForwardAdd" }

            Blend One One
            ZWrite Off
            ZTest LEqual

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdadd_fullshadows
            #pragma multi_compile __ WATER_NORMAL_MAPS_BLENDED_FAST WATER_NORMAL_MAPS_BLENDED
            #pragma multi_compile __ UNDERWATER
            #pragma shader_feature WATER_MOVEMENT_BI_DIRECTIONAL WATER_MOVEMENT_OPPOSING_BI_DIRECTIONAL WATER_MOVEMENT_OMNI_DIRECTIONAL
            #pragma multi_compile SR_LIGHTING_LOW SR_LIGHTING_MEDIUM SR_LIGHTING_HIGH
            //#pragma enable_d3d11_debug_symbols

            //#pragma target 3.5 // 3.5 adds instancing which cause the shader to be excluded on Adreno 3xx GPUs
            #pragma require derivatives interpolators10 interpolators15 samplelod fragcoord mrt4 integers 2darray
            
            #define UNITY_PASS_FORWARDADD 1

            #include "SrStandardWaterShaderPass.cginc"

            ENDCG
        }
    }

    Fallback "VertexLit"
}
