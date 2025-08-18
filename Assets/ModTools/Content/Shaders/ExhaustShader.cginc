#pragma vertex vert
#pragma fragment frag

#include "UnityCG.cginc"

struct appdata
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
    float2 uv2 : TEXCOORD1;
    float3 normal : NORMAL;
};

struct v2f
{
    float4 vertex : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 vertexLocal: TEXCOORD1;
    float alpha : TEXCOORD2;
    float3 cameraLocal : TEXCOORD3;
    float3 normalLocal : TEXCOORD4;
};

// COLORS
fixed4 _Color;
fixed4 _ExpandedColor;
fixed4 _TipColor;
fixed4 _ShockColor;
fixed4 _FlameColor;
fixed4 _SootColor;

// INTENSITY
float _Alpha;
float _Emission;
float _EmissionShock;
sampler2D _MainTex;
float4 _MainTex_ST;

// PARAMS
float _Expansion;
float _ExpansionInv;
float _RimShade;
float _ShockDiamonds;
float _ShockDirection;
float _SootLength;
float _SootStrength;
float _SpikeLength;
float _SpikeCurve;
float _Stretch;
float _TextShift;
float _TextStrength;
float _Throttle;

inline float QuickAbsCos(float x)
{
    float a = frac(x);
    return abs(a * a * (6 - 4 * a) - 1);
}

v2f vert(appdata v)
{
    v2f o;

    // The distance from the nozzle
    float dist = -v.vertex.z;

    // Calculate scale for this vertex
    float expansion = 1 + dist * (dist - 2) * _Stretch;
    float expansionRate = 2 * _Stretch * (dist - 1); // differentiation of above wrt. dist
    
    // Grab the baked in shock effect from the second uv set's y-coordinate
    // v.uv2.y contains a cosine looking like: (1 - abs(cos(pi * v.vertex.z * 32 / 6)))
    float shockEffect = v.uv2.y * (1 - dist) * saturate(5 * _ShockDiamonds - 5 * dist);

    // Scale the geometry down based on the shock diamonds setting
    expansion *= 1 - 0.3 * shockEffect;

    // Calculate alpha for this vertex
    o.alpha = pow(saturate(1 - dist), max(1, 0.5 * _Expansion));

    // Calculate texture coordinate and animate with time
    o.uv = TRANSFORM_TEX(v.uv, _MainTex);

    // Animate the texture with time
    o.uv.y += _TextShift;

    // Spin it around slowly so it is harder to notice repetition
    o.uv.x += 0.1 * _TextShift;

    // Calculate how much of a spike the shape of the exhaust should follow
    float spikeRange = 10 * dist - 0.6;
    expansion *= 1 - saturate(0.5 - spikeRange * spikeRange) * _ExpansionInv * _SpikeCurve * _Throttle;

    // Calculate vertex offset based on the shocks and aerospike and add some noise to it
    float offset = 1 + tex2Dlod(_MainTex, float4(o.uv, 0, 0)) * saturate(dist) * (2 - _Throttle) * (1 - 0.5 * _Throttle) * _ExpansionInv * _TextStrength;
    o.vertexLocal = float3(offset * expansion * v.vertex.xy, v.vertex.z); // -saturate(dist * 10) * _ExpansionInv * _SpikeLength);

    // Adjust local normal for gradient induced by expansion (doesn't account for other effects)
    o.normalLocal = normalize(v.normal + float3(0, 0, expansionRate));
    
    o.vertex = UnityObjectToClipPos(o.vertexLocal);

    // Save camera position
    o.cameraLocal = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0));

    return o;
}

fixed4 frag(v2f i) : SV_Target
{
    // The distance from the nozzle
    float dist = -i.vertexLocal.z - _SpikeLength;

    // Sample the detail texture, at a higher frequency when the scale is low and near the nozzle, blending the transition
    float texSampleLow = tex2D(_MainTex, i.uv);
    float texSample = lerp(texSampleLow, tex2D(_MainTex, 5 * i.uv), 0.9 * _Throttle - _Expansion * 0.15 - dist);

    // Fade out the texture at high throttles, more near the nozzle
    texSample = lerp(1, texSample, (1 - 0.3 * saturate(_Throttle - dist)) * _TextStrength);
    texSample = lerp(texSample, 0.5, saturate(1 - 4 * _ExpansionInv));

    // Calculate the distance of the camera ray to the z axis and the outer wall
    float3 camDir = normalize(i.cameraLocal - i.vertexLocal);
    float radius = dot(i.vertexLocal.xy, camDir.xy);
    float rimShade = abs(dot(normalize(i.normalLocal), camDir));

    // Fade when at low throttle and apply the gradient towards the tip and expansion
    fixed4 color = lerp(_ExpandedColor, _Color, _ExpansionInv);
    color = lerp(_TipColor, color, saturate(1 - dist * dist * _ExpansionInv));
    color = lerp(_FlameColor, color, _Throttle);

    // Make the outer edges brighter than the core with an adaptive strength
    color.a *= 1 - _RimShade * rimShade * saturate(0.5 + _Throttle);
    color.a *= saturate(2 * pow(rimShade + 0.5 * (1 - _ExpansionInv), 2 * dist + 2));
    color.a *= texSample;

    // Apply the global alpha before adding the soot
    color.a = saturate(color.a * _Alpha * i.alpha);

    // Imitate flow separation messing with the texture near the nozzle
    float separation = saturate(1 - 1.5 * _SootStrength * _Throttle * texSample * saturate(_SootLength * _ExpansionInv - 10 * dist));
    separation *= separation * (3 - 2 * separation);
    separation *= separation;
    separation *= separation;
    color.a = lerp(_SootColor.a, color.a, separation);
    separation *= separation;
    color.rgb = lerp(_SootColor.rgb, color.rgb, separation);

    // Calculate the target color, only drawing shock diamonds on the back face
    fixed4 col;
    if (FACE < 0)
    {
        // Calculate at which z the camera ray is closest to the exhaust z axis
        float z = i.vertexLocal.z - camDir.z * radius;
        radius = saturate(length(i.vertexLocal.xy - camDir.xy * radius) - 0.2 * texSampleLow);

        // Force the diamonds to be larger towards the top vs the bottom
        float shockDirection = _ShockDirection * _ShockDiamonds * saturate(1 - 2 * dist);
        float shockDRemap = 0.5 * shockDirection + 0.5;

        // Offset their position based on how warped they are
        z -= 0.03 * abs(shockDirection) + 0.05 * dist - _SpikeLength;

        // Create a saw tooth with the period of the shocks, and remap it to offset more the top vs the bottom
        float shockSawTooth = frac(0.5 - z * 5.3);
        float offset = 1 - abs(2 * shockSawTooth * (z < 0) - 1);

        // (a-b)^4
        shockDRemap = shockSawTooth - shockDRemap;
        shockDRemap *= shockDRemap;
        shockDRemap *= shockDRemap;

        // Apply the offset
        offset *= 8 * abs(shockDirection) * pow(shockDRemap, abs(shockDirection));

        // Evaluate what radius the shock should have at that z
        // 32 / 6 = 5.333... (32 is the number of vertices, 6 the periods of the cos)
        float shockStrength = 1 - QuickAbsCos(z * 5.333);

        // Prevent shocks inside the nozzle and decade them the further up the stream they are
        shockStrength *= saturate(2 * _ShockDiamonds - dist) * (z < 0);
        shockStrength *= 1 - dist;

        // Shape and fade the shocks into their final form
        shockStrength *= 0.8 * shockStrength * (1 - abs(0.5 * shockDirection));
        shockStrength = saturate(shockStrength - radius - offset);
        shockStrength *= saturate(_ShockDiamonds - dist);

        // Mix the shock diamonds with the exhaust color
        col = lerp(color, _ShockColor * _EmissionShock, saturate(10 * shockStrength)) * _Emission;
        col.a = lerp(color.a, _ShockColor.a, saturate(shockStrength * shockStrength));
    }
    else
    {
        col.rgb = color.rgb * _Emission;
        col.a = color.a;
    }
    
    col.rgb *= col.a;
    return col;
}