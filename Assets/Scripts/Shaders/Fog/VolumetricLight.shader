// credits:
// Technically Harry's Your first Volumetric Fog Shader: https://www.youtube.com/watch?v=8P338C9vYEE

Shader "Unlit/VolumetricFog"
{
    Properties
    {
        [Toggle] _TurnOn("Turn on", Float) = 1
        _DepthThreshold("Depth threshold", float) = 1000

        //_Color("Color", Color) = (1, 1, 1, 1)
        _MinDistance("Min distance", float) = 1
        _MaxDistance("Max distance", float) = 100
        _Density("Density", Range(0, 2)) = 1 
        [Toggle] _Squared("Squared", Float) = 0

        _StepSize("Step size", Range(0.1, 10)) = 100
        _NoiseOffset("Noise offset", float) = 0

        [HDR]_MainLightContribution("Main light contribution", Color) = (1, 1, 1, 1)
        [Toggle] _AddMainLight("Add main light", Float) = 0
        _Intensity("Intensity", Range(0, 1)) = 1

        // does not work or gives undiserable effect
        [HideInInspector] _LightScattering("Light scattering", Range(0, 1)) = 0.2
        [HideInInspector] _Scatter("Scatter", Range(0, 0.99)) = 0
    }
    SubShader
    {
        Tags 
        { 
            "RenderType"="Opaque" 
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            float _TurnOn;
            float _DepthThreshold;

            //float4  _Color;
            float _MinDistance;
            float _MaxDistance;
            float _Density;
            float _Squared;

            float _StepSize;
            float _NoiseOffset;
            
            float4 _MainLightContribution;
            float _AddMainLight;
            float _Intensity;

            float _LightScattering;
            float _Scatter;

            half4 _BlitTexture_TexelSize;

            float henyey_greenstein(float angle, float scattering)
            {
                return (1.0 - angle * angle) / (4.0 * PI * pow(max(1.0 + scattering * (scattering - 2.0 * angle), 0), 1.5));
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float4 col = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, IN.texcoord);
                if (!_TurnOn)
                {
                    return col;
                }
                float depth = SampleSceneDepth(IN.texcoord);
                float3 worldPos = ComputeWorldSpacePosition(IN.texcoord, depth, UNITY_MATRIX_I_VP);

                // Ray Marching
                float3 entryPoint = _WorldSpaceCameraPos;
                float3 viewDir = worldPos - _WorldSpaceCameraPos;
                float viewLength = length(viewDir);
                if (viewLength > _DepthThreshold){
                    return col;
                }
                float3 rayDir = normalize(viewDir);

                float2 pixelCoords = IN.texcoord * _BlitTexture_TexelSize.zw;
                float distLimit = min(viewLength, _MaxDistance);
                float distTravelled = _MinDistance + InterleavedGradientNoise(
                    pixelCoords, (int)(_Time.y / max(HALF_EPS, unity_DeltaTime.x))) * _NoiseOffset;
                float transmittance = 1;
                float4 shadowCol = col;

                while (distTravelled < distLimit)
                {
                    float3 rayPos = entryPoint + rayDir * distTravelled;
                    if (_Density > 0)
                    {
                        // Beer-Lambert's Law
                        float index = _Density * _StepSize;
                        if (_Squared)
                        {
                            index *= index;
                        }
                        transmittance *= exp(-index);

                        // Include Main Light Shadows
                        if (_AddMainLight)
                        {   
                            Light light = GetMainLight(TransformWorldToShadowCoord(rayPos));
                            float3 add = light.color.rgb * _MainLightContribution.rgb * light.shadowAttenuation * index * _Intensity;
                            if (_Scatter)
                            {
                                add *= henyey_greenstein(saturate(dot(rayDir, light.direction)), _LightScattering);
                            }
                            shadowCol.rgb += add;
                        }
                    }
                    distTravelled += _StepSize;
                }

                return lerp(col, shadowCol, 1.0 - saturate(transmittance));
            }
            ENDHLSL
        }
    }
}
