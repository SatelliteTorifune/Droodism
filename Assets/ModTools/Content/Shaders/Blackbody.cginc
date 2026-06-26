#ifndef BLACKBODY_INCLUDED
#define BLACKBODY_INCLUDED

// Tanner Helland's RGB approximation of the Planckian locus.
// T in Kelvin (useful 800-10000). Returns unit-scaled chromaticity.
float3 BlackbodyEmission(float T)
{
    float t      = T * 0.01;
    float tHigh  = max(t - 60.0, 1e-3);   // pow() guard for the t<=66 branch
    float tBlue  = max(t - 10.0, 1e-3);

    float3 c;
    c.r = t <= 66.0 ? 1.0
                    : saturate(1.29293618606 * pow(tHigh, -0.1332047592));
    c.g = t <= 66.0 ? saturate(0.39008157877 * log(max(t, 1e-3)) - 0.63184144379)
                    : saturate(1.12989086090 * pow(tHigh, -0.0755148492));
    c.b = t >= 66.0 ? 1.0
        : t <= 19.0 ? 0.0
                    : saturate(0.54320678911 * log(tBlue) - 1.19625408914);

    return c;
}

float BlackbodyIntensity(float heat)
{
    return heat <= 1.0 ? heat * heat : 1.0 + 2.0 * log(heat);
}

#endif
