Shader "LeftBehind/PS1Post"
{
    Properties
    {
        _ColorLevels ("Colour levels per channel", Float) = 32
        _DitherStrength ("Dither strength", Range(0,2)) = 1
        _GrainStrength ("Film grain strength", Range(0,0.2)) = 0.03
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off Blend Off
        Pass
        {
            Name "PS1Post"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _ColorLevels;
            float _DitherStrength;
            float _GrainStrength;

            static const float bayer4[16] = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv);

                float2 px = floor(uv * _ScreenParams.xy);
                int ix = (int)fmod(px.x, 4.0);
                int iy = (int)fmod(px.y, 4.0);
                float d = (bayer4[ix + iy * 4] / 16.0 - 0.5) * _DitherStrength / _ColorLevels;

                col.rgb = floor(saturate(col.rgb + d) * (_ColorLevels - 1.0) + 0.5) / (_ColorLevels - 1.0);

                float n = frac(sin(dot(px + floor(_Time.y * 24.0) * 17.0, float2(12.9898, 78.233))) * 43758.5453);
                col.rgb += (n - 0.5) * _GrainStrength;
                return col;
            }
            ENDHLSL
        }
    }
    Fallback Off
}