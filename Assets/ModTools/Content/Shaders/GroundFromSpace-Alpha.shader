Shader "Jundroo/GroundFromSpaceAlpha" 
{
    Properties
    {
        _Cube("Cubemap", CUBE) = "" {}
        _BumpCube("Bump Cubemap", CUBE) = "" {}
        _adjustedCameraPosition("Camera Position",Vector) = (0,0,0)
        _lightDir("Light Direction",Vector) = (0,0,0)
        _invWaveLength("Inverse WaveLength",Vector) = (0,0,0,0)
        _cameraHeight("Camera Height",Float) = 0
        _cameraHeight2("Camera Height2",Float) = 0
        _outerRadius("Outer Radius",Float) = 0
        _outerRadius2("Outer Radius 2",Float) = 0
        _innerRadius("Inner Radius",Float) = 0
        _innerRadius2("Inner Radius 2",Float) = 0
        _atmosSizeScale("Atmosphere Size Scale",Float) = 1
        _krESun("KrESun",Float) = 0
        _kmESun("KmESun",Float) = 0
        _kr4PI("Kr4PI",Float) = 0
        _km4PI("Km4PI",Float) = 0
        _scale("Scale",Float) = 0
        _atmosScale("Atmosphere Scale",Float) = 0
        _scaleDepth("Scale Depth",Float) = 0
        _scaleOverScaleDepth("Scale Over Scale Depth",Float) = 0
        _samples("Samples",Float) = 0
        _g("G",Float) = 0
        _g2("G2",Float) = 0
        _debugOutput("Output",Color) = (0, 0, 0, 0)
        _ambientLight("Apply Ambient Light",Float) = 1
        _MinASL("Min ASL", Float) = 0
        _MaxASL("Max ASL", Float) = 0
        _MaxColors("Max Colors", Vector) = (1,1,1)
        _VertexDisplacementLod("Vertex Displacement Lod", Float) = 0
        _Eclipse("Eclipse", float) = 0

        [Space(20)][Header(Light Options)]
        [KeywordEnum(LOW, MEDIUM, HIGH)] SR_LIGHTING("Lighting Quality", Float) = 0
    }

    SubShader
    { 
        Fog{ Mode Off }
        Tags{ "RenderType" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha

        Pass 
        {
            Tags { "LightMode" = "ForwardBase" }

            CGPROGRAM
            #define SRSTANDARD_SCALEDSPACE 1
            #define UNITY_PASS_FORWARDBASE 1

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile __ ATMOSPHERE
            #pragma multi_compile SR_LIGHTING_NONE SR_LIGHTING_LOW SR_LIGHTING_MEDIUM SR_LIGHTING_HIGH
            #pragma multi_compile_local __ NORMALMAP NORMALMAP_WITH_VERTEX_DISPLACEMENT
            //#pragma enable_d3d11_debug_symbols 

            //#pragma target 3.5 // 3.5 adds instancing which cause the shader to be excluded on Adreno 3xx GPUs
            #pragma require derivatives interpolators10 interpolators15 samplelod fragcoord mrt4 integers 2darray

            #include "SrStandardConstants.cginc"
            #include "SrStandardShaderData.cginc"
            #include "Sr2ShaderStructures.cginc"
            #include "SrStandardEffects.cginc"
            #include "UnityCG.cginc"

            samplerCUBE _Cube;
            half3 _MaxColors;
            float _Eclipse;

            #if NORMALMAP || NORMALMAP_WITH_VERTEX_DISPLACEMENT
                samplerCUBE _BumpCube;
                float _MinASL;
                float _MaxASL;
                float _VertexDisplacementLod;
            #endif

            v2f vert(float4 vertex : POSITION, float3 normal : NORMAL)
            {
                v2f OUT;
                UNITY_INITIALIZE_OUTPUT(v2f, OUT);

                #if NORMALMAP_WITH_VERTEX_DISPLACEMENT
                    half4 h = texCUBElod(_BumpCube, float4(normal, _VertexDisplacementLod)).a;
                    float3 vertPos = vertex * lerp(_MinASL, _MaxASL, h);
                #else
                    float3 vertPos = vertex;
                #endif

                OUT.position = UnityObjectToClipPos(vertPos);
                OUT.normal = normal;
                OUT.worldPosition = mul(unity_ObjectToWorld, vertPos);
                OUT.lightColor = GetLightColor(saturate(GetLightingAtten(vertPos, GetLightDirection(OUT.worldPosition), 0)));

                float3 camToVertex = vertPos - _adjustedCameraPosition;
                
                float distToVertex = length(camToVertex); 
                OUT.cameraToVertex = float4(camToVertex / distToVertex, distToVertex); 

                #if ATMOSPHERE
                OUT.atmosColor.rgb = GetAtmosphereData(vertPos, _lightDir, OUT.lightColor, _invWaveLength).color;
                #endif

                return OUT;
            }

            float4 frag(v2f INPUT) : SV_Target
            {
                #if NORMALMAP || NORMALMAP_WITH_VERTEX_DISPLACEMENT
                    // Bump map
                    INPUT.worldNormal = normalize((texCUBE(_BumpCube, INPUT.normal).rgb * 2) - 1);
                #else
                    INPUT.worldNormal = INPUT.normal;
                #endif

                // Base cube-map color.
                float4 color = texCUBE(_Cube, INPUT.normal);
                color *= float4(_MaxColors, 1);

                // Get the material properties
                half metallicness = TerrainMetallicness;
                half smoothness = clamp((color.a - (127.0 / 255.0)) / (128.0 / 255.0), 0, 1);
                half emissiveStrength = 1.0 - clamp(color.a / (20.0 / 255.0), 0, 1);

                // Compute emission and reduce base color based accordingly
                half3 emission = 0;
                if (emissiveStrength > 0)
                {
                    emission = color.rgb * emissiveStrength;
                    color.rgb *= 1 - saturate(emissiveStrength);
                }

                #if !SR_LIGHTING_NONE
                // Apply lighting and atmosphere
                color = ApplyStandardLightingAndAtmosphere(color, metallicness, smoothness, emission, INPUT.cameraToVertex.xyz, INPUT.cameraToVertex.w, 1, INPUT);
                #else
                    color.rgb += emission;
                #endif

                color = clamp(color, 0, _maxColorValue);
                color.a = _Eclipse;
                return color;
            }

            ENDCG
        }
    }
    FallBack "None"
}