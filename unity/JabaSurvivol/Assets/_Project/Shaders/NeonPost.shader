Shader "JabaSurvivol/NeonPost"
{
    Properties
    {
        _BloomIntensity ("Bloom Intensity", Range(0, 1)) = 0.22
        _BloomThreshold ("Bloom Threshold", Range(0, 1)) = 0.6
        _BloomRadius ("Bloom Radius", Range(0.5, 8)) = 3.0
        _VignetteIntensity ("Vignette Intensity", Range(0, 1)) = 0.22
        _VignetteSmoothness ("Vignette Smoothness", Range(0.01, 1)) = 0.6
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always

        Pass
        {
            Name "JabaNeonPost"

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0

            float _BloomIntensity;
            float _BloomThreshold;
            float _BloomRadius;
            float _VignetteIntensity;
            float _VignetteSmoothness;

            half3 SampleBloom(float2 uv, float2 texel)
            {
                half3 sum = 0.0h;
                sum += saturate(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(texel.x, 0.0h)).rgb - _BloomThreshold);
                sum += saturate(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - float2(texel.x, 0.0h)).rgb - _BloomThreshold);
                sum += saturate(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(0.0h, texel.y)).rgb - _BloomThreshold);
                sum += saturate(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - float2(0.0h, texel.y)).rgb - _BloomThreshold);
                sum += saturate(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + texel).rgb - _BloomThreshold);
                sum += saturate(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - texel).rgb - _BloomThreshold);
                sum += saturate(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(texel.x, -texel.y)).rgb - _BloomThreshold);
                sum += saturate(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(-texel.x, texel.y)).rgb - _BloomThreshold);
                return sum * 0.125h;
            }

            half4 Frag(Varyings input) : SV_Target0
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord.xy;
                half4 baseColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                float2 texel = _BlitTexture_TexelSize.xy * _BloomRadius;
                half3 bloom = SampleBloom(uv, texel);
                half3 color = baseColor.rgb + bloom * _BloomIntensity;

                float2 centered = uv - 0.5h;
                float distanceSquared = dot(centered, centered) * 2.0h;
                float vignette = saturate((1.0h - distanceSquared) / max(_VignetteSmoothness, 0.01h));
                vignette = smoothstep(0.0h, 1.0h, vignette);
                color *= lerp(1.0h, vignette, _VignetteIntensity);

                return half4(color, baseColor.a);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
