#ifndef UNIVERSAL_LITEXT_INPUT_INCLUDED
#define UNIVERSAL_LITEXT_INPUT_INCLUDED
#include "Packages/com.llealloo.audiolink/Runtime/Shaders/AudioLink.cginc"

#define AUDIOLINKEXT_VARS \
    int _AudioBand; \
    float _AudioIntensity; \
    float _AudioEmissionMinimum; \
    float _AudioEmissionInverse; \
    int _AudioTimingSource; \
    float _AudioTimingInverse; \
    Texture2D _AudioTimingTexture; \
    float4 _AudioTimingTexture_ST; \
    float4 _AudioTimingTexture_TexelSize;

struct AudioData
{
    float intensity;
    int band;
    bool inverse;
    float minimum;
    int timingSource;
    bool timingInverse;
    Texture2D timingTexture;
};

float gray(float4 color)
{
    return dot(color.rgb, float3(0.212655, 0.715158, 0.072187)) * color.a;
}

float UVDistanceFromCenter(float2 uv)
{
    // Normalize the distance (divide by maximum possible distance)
    // In UV space, the maximum distance from center to corner is sqrt(0.5²+0.5²) = sqrt(0.5) ≈ 0.7071
    return saturate(length(uv - 0.5) / 0.7071);
}

#define TIMING_SOURCE_CHANNELS float4(0, 1, 2, 3)
#define TIMING_SOURCE_GRAYSCALE 4
#define TIMING_SOURCE_UVX 5
#define TIMING_SOURCE_UVY 6
#define TIMING_SOURCE_CENTER 7

void ModifyEmissionForAudioLink(inout SurfaceData surfaceData, float2 uv, AudioData props)
{
    #ifdef AUDIOLINK_CGINC_INCLUDED
    if (AudioLinkIsAvailable() && props.intensity > 0)
    {
        uv = abs(uv) % float2(1,1); // correct for scale/bias from BaseMap
        float delay = 0;
        if (props.timingSource < TIMING_SOURCE_GRAYSCALE)
            delay = 1 - SAMPLE_TEXTURE2D(props.timingTexture, sampler_EmissionMap, uv)[props.timingSource];
        else if (props.timingSource == TIMING_SOURCE_GRAYSCALE)
            delay = 1 - gray(SAMPLE_TEXTURE2D(props.timingTexture, sampler_EmissionMap, uv));
        else if (props.timingSource == TIMING_SOURCE_UVX)
            delay = uv.x;
        else if (props.timingSource == TIMING_SOURCE_UVY)
            delay = 1 - uv.y;
        else if (props.timingSource == TIMING_SOURCE_CENTER)
            delay = UVDistanceFromCenter(uv);

        if (props.timingInverse) delay = 1 - delay;
        float audioLerp = AudioLinkLerp(float2(delay * (AUDIOLINK_WIDTH - 1), props.band));
        if (props.inverse) audioLerp = 1 - audioLerp;

        surfaceData.emission *= Remap(0, 1, props.minimum, props.intensity,
            saturate(audioLerp));
    }
    #endif
}

#endif
