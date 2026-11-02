// Full-screen VHS "rewind" overlay for the loop transition, drawn by a
// RawImage on a Screen Space - Overlay canvas.
// _UseTex 1: distorts a captured frame (the moment the player leaves).
// _UseTex 0: draws only tracking bars and static over the live scene
//            (the moment the player arrives), with _Fade to black.
Shader "LeftBehind/RewindOverlay"
{
    Properties
    {
        [PerRendererData] _MainTex ("Captured frame", 2D) = "black" {}
        _Intensity ("Distortion", Range(0, 1)) = 0
        _Fade ("Fade to black", Range(0, 1)) = 0
        _UseTex ("Use captured frame", Float) = 1
        _Roll ("Vertical roll", Float) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Overlay" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Intensity;
            float _Fade;
            float _UseTex;
            float _Roll;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float t = _Time.y;
                float2 uv = i.uv;
                uv.y = frac(uv.y + _Roll);

                // Horizontal tearing in bands, re-rolled 20 times a second.
                float band = floor(uv.y * 60.0);
                float jitter = (hash(float2(band, floor(t * 20.0))) - 0.5) * 0.06 * _Intensity;

                // Two bright tracking bars crawling up the screen.
                float barY = frac(t * 0.6);
                float bar = saturate(1.0 - abs(uv.y - barY) * 14.0) * _Intensity;
                float bar2 = saturate(1.0 - abs(uv.y - frac(barY + 0.37)) * 30.0) * _Intensity;

                float shift = jitter + (bar + bar2) * 0.04;
                float ca = 0.008 * _Intensity;
                float3 col = 0;
                if (_UseTex > 0.5)
                {
                    col.r = tex2D(_MainTex, uv + float2(shift + ca, 0)).r;
                    col.g = tex2D(_MainTex, uv + float2(shift, 0)).g;
                    col.b = tex2D(_MainTex, uv + float2(shift - ca, 0)).b;
                }

                float n = hash(i.uv * float2(640.0, 360.0) + floor(t * 30.0));
                float lum = dot(col, float3(0.299, 0.587, 0.114));
                col = lerp(col, lum * float3(1.0, 0.93, 0.85), 0.6 * _Intensity);   // washed-out tape colour
                col = lerp(col, n.xxx, 0.22 * _Intensity);                         // static
                col += (bar * 0.55 + bar2 * 0.35) * float3(0.9, 0.95, 1.0);
                col *= 0.86 + 0.14 * sin(i.uv.y * 900.0);                          // scanlines
                col *= 1.0 - _Fade;

                float a = _UseTex > 0.5
                    ? 1.0
                    : saturate(max(_Fade, (bar * 0.8 + bar2 * 0.6 + step(0.82, n) * 0.5) * _Intensity));
                return fixed4(col, a * i.color.a);
            }
            ENDCG
        }
    }
}
