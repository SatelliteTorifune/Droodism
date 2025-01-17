Shader "Jundroo/SkyFromGround"
{
    Properties
    {
        _adjustedCameraPosition("Camera Position",Vector) = (0,0,0)
        _lightDir("Light Direction",Vector) = (0,0,0,0)
        _invWaveLength("Inverse WaveLength",Color) = (0,0,0,0)
        _cameraHeight("Camera Height",Float) = 0
        _cameraHeight2("Camera Height2",Float) = 0
        _cameraHeightAtmosPercent("Camera Height Atmosphere Percent",Float) = 0
        _cameraViewDir("Camera View Direction",Vector) = (0,0,0)
        _debugOutput("Output",Color) = (0, 0, 0, 0)
        _outerRadius("Outer Radius",Float) = 0
        _outerRadius2("Outer Radius 2",Float) = 0
        _innerRadius("Inner Radius",Float) = 0
        _innerRadius2("Inner Radius 2",Float) = 0
        _atmosSizeScale("Atmosphere Size Scale",Float) = 1
        _km4PI("Km4PI",Float) = 0
        _kmESun("KmESun",Float) = 0
        _kr4PI("Kr4PI",Float) = 0
        _krESun("KrESun",Float) = 0
        _scale("Scale",Float) = 0
        _scaleDepth("Scale Depth",Float) = 0
        _maxColorValue("The value at which bloom begins",Float) = 2
        _scaleOverScaleDepth("Scale Over Scale Depth",Float) = 0
        _samples("Samples",Float) = 0
        _g("G",Float) = 0
        _g2("G2",Float) = 0
        _worldCameraPosition("Camera Position",Vector) = (0,0,0)

        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("BlendSource", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("BlendDestination", Float) = 10
        [Enum(UnityEngine.Rendering.BlendOp)] _BlendOpColor("BlendOp Color", Float) = 0
        [Enum(UnityEngine.Rendering.BlendOp)] _BlendOpAlpha("BlendOp Alpha", Float) = 0
        [Toggle] _ZWrite("ZWrite", Float) = 0
        [Toggle] _legacySkyShader("Legacy Shader", Float) = 0
    }

    SubShader
    {
        Tags{ "Queue" = "Transparent" }
        Pass
        {
            Cull Front
            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]
            BlendOp[_BlendOpColor],[_BlendOpAlpha]

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            //#pragma enable_d3d11_debug_symbols
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #include "Lighting.cginc"
            #include "SkyUtils.cginc"
            #include "Utils.cginc"

            float3 _adjustedCameraPosition;
            float _cameraHeightAtmosPercent;
            float _cameraHeight;
            float _cameraHeight2;
            float _g;
            float _g2;
            float _innerRadius;
            float _innerRadius2;
            float _atmosSizeScale;
            float3 _invWaveLength;
            float _km4PI;
            float _kmESun;
            float _kr4PI;
            float _krESun;
            float3 _lightDir;
            float _samples;
            float _scale;
            float _scaleDepth;
            float _scaleOverScaleDepth;
            float3 _worldCameraPosition;
            bool _legacySkyShader;
            float _maxColorValue;

            struct v2f
            {
                float4 position : POSITION;
                float3 rayleigh : COLOR0;
                float4 mie : COLOR1;
                float3 cameraToVertex : TEXCOORD0;
                float3 normalWorld : NORMAL;
                float3 cameraToVertexWorld : TEXCOORD2;
                float transparency : TEXCOORD3;
            };

            v2f vert(float4 vertex : POSITION, float3 normal : NORMAL)
            {
                 // TODO: Need to look at this...it seems like it may be more accurate
                // https://forum.unity3d.com/threads/atmospheric-scattering-help.12296/page-2#post-678024

                // Get the ray from the camera to the vertex and its length (which is the far point of the ray passing through the atmosphere)
                float3 vertexPos = vertex.xyz;
                float3 cameraToVertex = vertexPos - _adjustedCameraPosition.xyz;
                float3 cameraToVertexDist = length(cameraToVertex);

                // Normalize the camera vector to get direction.
                float3 cameraToVertexDir = cameraToVertex / cameraToVertexDist;

                // Calculate the ray's starting position, then calculate its scattering offset
                float3 start = _adjustedCameraPosition.xyz;
                float startAngle = dot(cameraToVertexDir, start) / length(start);
                float depth = exp(_scaleOverScaleDepth * (_innerRadius - _cameraHeight));
                float startOffset = depth*ExpScale(startAngle, _scaleDepth, _atmosSizeScale);

                // Initialize the scattering loop variables
                float sampleLength = cameraToVertexDist / _samples;
                float scaledLength = sampleLength * _scale;
                float3 sampleRay = cameraToVertexDir * sampleLength;
                float3 samplePoint = start + sampleRay * .5;
                float3 frontColor = float3(0.0, 0.0, 0.0);
                float3 attenuate;

                // Now loop through the sample rays
                for (int i = 0; i < _samples; i++)
                {
                    float height = length(samplePoint);
                    float depth = exp(_scaleOverScaleDepth * (_innerRadius - height));
                    float lightAngle = dot(_lightDir.xyz, samplePoint) / height;
                    float cameraAngle = clamp(dot(cameraToVertexDir, samplePoint) / height, 0, 1);
                    float scatter = (startOffset + depth * (ExpScale(lightAngle, _scaleDepth, _atmosSizeScale) - ExpScale(cameraAngle, _scaleDepth, _atmosSizeScale)));

                    attenuate = exp(-scatter * (_invWaveLength.xyz * _kr4PI + _km4PI));

                    bool clouds = false;
                    if (clouds)
                    {
                        frontColor += attenuate * (((depth + abs(snoise(samplePoint * 10)) * .15)) * scaledLength);
                    }
                    else
                    { 
                        frontColor += attenuate * (depth * scaledLength);
                    }

                    samplePoint += sampleRay;
                }

                v2f OUT;
                OUT.position = UnityObjectToClipPos(vertex);
                OUT.cameraToVertex = cameraToVertex;
                OUT.rayleigh = frontColor * (_invWaveLength.xyz * _krESun);
                OUT.mie = float4(frontColor * _kmESun, 1);
                
                if (_legacySkyShader)
                {
                    OUT.transparency = smoothstep(-1, GroundTransparency, dot(_lightDir.xyz, normalize(vertexPos)));
                }

                OUT.cameraToVertexWorld = normalize(mul(unity_ObjectToWorld, vertex) - _worldCameraPosition.xyz);
                OUT.normalWorld = normalize(UnityObjectToWorldNormal(normal));

                return OUT;
            }

            /// <remarks>
            /// NOTE: There is a complex dance going on between the ground/space sky shaders. Do not change one shader w/o complementary changes to the other.
            /// The main issues at play are:
            /// 1. Eliminate/reduce a visible line as the camera passes through the top of the atmosphere
            /// 2. Preventing a "pop" of different shading between shaders.  
            /// 3. Gray color to show in the absence of other atmosphere, which fades out as the camera nears the outer atmos.
            /// 4. Space shader dims as the camera moves away, but needs to match ground version before it passes back into atmos.
            /// </remarks>
            float4 frag(v2f INPUT) : COLOR
            {
                // Calculate atmosphere final base color.
                float lightAngle = dot(_lightDir.xyz, normalize(-INPUT.cameraToVertex.xyz));
                float sunSize = lightAngle * lightAngle;
                
                float miePhaseAtten = 1.5 * ((1.0 - _g2) / (2.0 + _g2)) * (1.0 + sunSize) / pow(1.0 + _g2 - 2.0*_g*lightAngle, 1.5);
                float rayleighPhaseAtten = 0.75 * (1.0 + sunSize);
                float4 fragColor;

                fragColor.xyz = (rayleighPhaseAtten * INPUT.rayleigh) + (miePhaseAtten * INPUT.mie);

                // Calculate atmosphere transparency.  It is determined either by the pixel color (dark=transparent), or the transparency calculated
                // in the vertex shader.
                float skyFromSpaceTrans = GetSkyFromSpaceTransparency(1, length(fragColor.xyz));
                fragColor.w = lerp(1, skyFromSpaceTrans, _cameraHeightAtmosPercent);

                // Clamp color/trans to show a light gray dome when atmos isn't present.  Fade it out as the camera raises through the atmos.
                float scaler = 1 - (_cameraHeightAtmosPercent);
                
                // Use these to adjust how the skybox shows through the atmos at night. MinSkyboxIntensity will also impact day-time colors,
                // but to a much lesser extent. It should impact dusk/night much more.
                const float MinSkyboxIntensity = .85;   // Higher means lower intensity.

                if(_legacySkyShader)
                {
                    float colorMagTrans = length(fragColor.xyz);
                    float skyFromSpaceTrans = GetSkyFromSpaceTransparency(INPUT.transparency, colorMagTrans);
                    fragColor.w = lerp(INPUT.transparency, skyFromSpaceTrans, _cameraHeightAtmosPercent);
                }
                else
                {
                    fragColor.w = max(fragColor.w, MinSkyboxIntensity * scaler);
                }
                
                // Clamp HDR values so bloom doesn't go crazy.
                return clamp(float4(fragColor.rgb, fragColor.w), 0, _maxColorValue);
            }

        ENDCG
    }
    }
        FallBack "None"
}
