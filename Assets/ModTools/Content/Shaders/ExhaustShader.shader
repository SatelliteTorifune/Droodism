Shader "Jundroo/ExhaustShader"
{
    Properties
    {
        // COLORS
        _Color("Main Color", Color) = (1, 1, 1, 1)
        _ExpandedColor("Expanded Color", Color) = (1, 1, 1, 1)
        _TipColor("Tip Color", Color) = (1, 0.4, 0.1, 0.2)
        _ShockColor("Shock Diamond Color", Color) = (0.5, 0.5, 1, 0.8)
        _FlameColor("Flame Out Color", Color) = (1, 0.4, 0, 0.5)
        _SootColor("Soot Color", Color) = (0, 0, 0, 1)

        // INTENSITY
        _Alpha("Alpha", float) = 1
        _Emission("Emission", float) = 1
        _EmissionShock("Shock Diamond Emission", float) = 1
        _MainTex("Texture", 2D) = "white" {}

        // PARAMS
        _Expansion("Underexpansion", float) = 1
        _ExpansionInv("Inversed Underexpansion", float) = 1
        _RimShade("Rim Shading", float) = 0
        _ShockDiamonds("Shock Diamonds", float) = 1
        _ShockDirection("Shock Direction", float) = -1
        _SootLength("Soot Separation", float) = 0
        _SootStrength("Soot Strength", float) = 0
        _SpikeLength("Aerospike Length", float) = 0
        _SpikeCurve("Aerospike Curvature", float) = 0
        _Stretch("Stretch", float) = 1
        _TextShift("Texture Shift Speed", float) = 0
        _TextStrength("Texture Strength", float) = 1
        _Throttle("Throttle", float) = 1
    }

    SubShader
    {
        Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
        LOD 100

        ZWrite Off
        Lighting Off
        Blend One OneMinusSrcAlpha

        // Rendering the back face first, then the front face, to avoid some graphical glitches
        // The repeated pass with different culling modes ensures the GPU sorts faces the right way
        Cull Front
        Pass
        {
            CGPROGRAM
            #define FACE -1
            #include "ExhaustShader.cginc"
            ENDCG
        }
        Cull Back
        Pass
        {
            CGPROGRAM
            #define FACE 1
            #include "ExhaustShader.cginc"
            ENDCG
        }
    }
}
