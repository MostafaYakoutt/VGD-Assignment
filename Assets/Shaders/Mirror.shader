Shader "LeftBehind/Mirror"
{
    // The glass of a mirror. EmptyMirror renders the reflection into _ReflectionTex from the
    // mirrored viewpoint; the glass shows it at each pixel's place on screen, flipped left to right
    // (the reflection camera is a proper rotation, so its picture is the mirror image of the real one).
    Properties
    {
        _ReflectionTex ("Reflection", 2D) = "black" {}
        _Tint ("Tint", Color) = (0.78, 0.8, 0.78, 1)
        _Edge ("Darkening towards the frame", Range(0, 1)) = 0.45
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Name "Mirror"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_ReflectionTex);
            SAMPLER(sampler_ReflectionTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _ReflectionTex_ST;
                half4 _Tint;
                half _Edge;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos : TEXCOORD0;
                float2 uv : TEXCOORD1;
                half fog : TEXCOORD2;
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.screenPos = ComputeScreenPos(o.positionCS);
                o.uv = v.uv;
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float2 uv = i.screenPos.xy / i.screenPos.w;
                uv.x = 1.0 - uv.x;
                half3 col = SAMPLE_TEXTURE2D(_ReflectionTex, sampler_ReflectionTex, uv).rgb * _Tint.rgb;
                // Old silvering: darker towards the frame.
                float2 e = abs(i.uv - 0.5) * 2.0;
                col *= 1.0 - _Edge * pow(saturate(max(e.x, e.y)), 3.0);
                col = MixFog(col, i.fog);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
