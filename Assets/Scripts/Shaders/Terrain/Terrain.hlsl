#ifndef TERRAIN
#define TERRAIN

#include "../Includes/Noise3D.hlsl"

void WarpPosition_float(
    float3 WorldPostion,
    float3 PositionWarpFrequencies,
    float3 PositionWarpAmplitudes,
    out float3 WarpedPosition)
{
    float3 w = 0;
    w += snoise(WorldPostion * PositionWarpFrequencies.x) * PositionWarpAmplitudes.x;
    w += snoise(WorldPostion * PositionWarpFrequencies.y) * PositionWarpAmplitudes.y;
    w += snoise(WorldPostion * PositionWarpFrequencies.z) * PositionWarpAmplitudes.z;
    WarpedPosition = w + WorldPostion;
}

void BaseColor_float(
    float3 WarpedPosition,
    float NoiseFrequency,
    float NoiseAmplitude,
    float BoundYMin,
    float BoundYMax,
    UnityTexture2D TerrainTexture,
    out half3 BaseColor)
{
    float2 uv = 0;
    uv.x = smoothstep(BoundYMin, BoundYMax, WarpedPosition.y);
    float n = (snoise(WarpedPosition * NoiseFrequency) + 1) / 2 * NoiseAmplitude;
    uv.y = saturate(n);
    half4 c = SAMPLE_TEXTURE2D(TerrainTexture, TerrainTexture.samplerstate, uv);
    BaseColor = c.rgb;
}

void Normal_float(
    float3 WarpedPosition,
    float3 WorldNormal,
    float3 NormalWarpFrequencies,
    float3 NormalWarpAmplitudes,
    bool WarpNormals,
    out float3 Normal)
{
    float3 w = 0;
    if (WarpNormals)
    {
        w += snoise(WarpedPosition * NormalWarpFrequencies.x) * NormalWarpAmplitudes.x;
        w += snoise(WarpedPosition * NormalWarpFrequencies.y) * NormalWarpAmplitudes.y;
        w += snoise(WarpedPosition * NormalWarpFrequencies.z) * NormalWarpAmplitudes.z;
    }
    Normal = normalize(w + WorldNormal);
}

#endif //TERRAIN