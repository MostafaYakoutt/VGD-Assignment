// Stencil outline for URP, drawn by a mesh copy that follows the object.
// Pair it with LeftBehind/OutlineMask on the original mesh (lower queue):
// the mask marks the object's own pixels, then this pass draws the mesh
// pushed out along its normals by a fixed number of screen pixels,
// only where the mask is not set. The result is an unbroken silhouette
// line that the object's own geometry can't hide, while walls in front
// still occlude it through the depth test.
Shader "LeftBehind/Outline"
{
    Properties
    {
        _OutlineColor ("Color", Color) = (1, 1, 1, 1)
        // _ScreenParams is the output size, not the scaled render target,
        // so with render scale 0.4 about 4 px is the thinnest unbroken line.
        _OutlineWidth ("Width (screen pixels)", Range(0, 12)) = 4
        [IntRange] _StencilRef ("Stencil ref", Range(1, 255)) = 64
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry+10" }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Off
            ZWrite Off
            ZTest LEqual

            Stencil
            {
                Ref [_StencilRef]
                Comp NotEqual
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                float _OutlineWidth;
                float _StencilRef;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float4 positionCS = TransformObjectToHClip(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float2 normalCS = mul((float3x3)UNITY_MATRIX_VP, normalWS).xy;
                float len = length(normalCS);
                normalCS = len > 1e-5 ? normalCS / len : float2(0, 0);

                // Pixels -> NDC, then scale by w so the width is constant on screen.
                float2 offset = normalCS * _OutlineWidth * 2.0 / _ScreenParams.xy;
                positionCS.xy += offset * positionCS.w;
                output.positionCS = positionCS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
}
