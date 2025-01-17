#ifndef SkyUtils_INCLUDED
#define SkyUtils_INCLUDED

    // This controls sky transparency when the camera is near the ground. It determines at what time the sky-box is allowed to show through. 
    // The default value is zero meaning the sky-box will lerp between fully visible at midnight to fully opaque at dawn.
    // The range is -1 to 1 (midnight to noon).  0 = dawn.
    // NOTE: It is located here b/c both the ground and space sky shaders need this value so they can
    // coordinate transitions.
    const float GroundTransparency = 0;


    /// <summary>
    /// Gets the pixel transparency used by the sky from space shader.
    /// </summary>
    float GetSkyFromSpaceTransparency(float vertexTrans, float colorMagTrans)
    {
        // This little bit of code is in its own method because the sky from ground shader also needs to know what that space shaders
        // transparency will be at the transition point so the transition will be smooth.
        return min(vertexTrans, colorMagTrans);
    }

#endif
