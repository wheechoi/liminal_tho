// Surplus Person 화면 셰이더 (저해상도 게임 화면을 띄우는 RawImage에 사용).
// 레퍼런스(단면 저택 일러스트, 픽셀 어드벤처, 1비트 만화, 붉은 포스터 톤)의 공통점을 흉내낸다:
//  - 잉크 선: 색이 크게 바뀌는 경계를 어둡게 (화면 픽셀 기준 Sobel)
//  - 색 수 제한 + 4x4 Bayer 디더링
//  - 종이 질감 그레인, 약한 비네트
//  - _GradientAmount: 0이면 원래 색, 1이면 3색 그라데이션 맵 (뒤쪽 공포 장면용 손잡이)
Shader "Liminal/IllustratedScreen"
{
    Properties
    {
        [PerRendererData] _MainTex ("Game View", 2D) = "black" {}
        _Levels ("Color Levels", Range(2, 32)) = 8
        _Dither ("Dither Strength", Range(0, 1.5)) = 0.55
        _Saturation ("Saturation", Range(0, 1.5)) = 0.9
        _Ink ("Ink Strength", Range(0, 1)) = 0.85
        _InkThreshold ("Ink Threshold", Range(0.01, 1)) = 0.16
        _InkColor ("Ink Color", Color) = (0.16, 0.11, 0.11, 1)
        _Grain ("Paper Grain", Range(0, 0.3)) = 0.02
        _PaperTint ("Paper Tint", Color) = (1, 0.97, 0.9, 1)
        _Vignette ("Vignette", Range(0, 1)) = 0.3
        _GradientAmount ("Gradient Map Amount", Range(0, 1)) = 0
        _GradShadow ("Gradient Shadow", Color) = (0.12, 0.06, 0.05, 1)
        _GradMid ("Gradient Mid", Color) = (0.62, 0.32, 0.26, 1)
        _GradLight ("Gradient Light", Color) = (0.95, 0.86, 0.72, 1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Levels, _Dither, _Saturation, _Ink, _InkThreshold, _Grain, _Vignette, _GradientAmount;
            float4 _InkColor, _PaperTint, _GradShadow, _GradMid, _GradLight;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            float Luma(float3 c) { return dot(c, float3(0.299, 0.587, 0.114)); }

            float Bayer4(float2 p)
            {
                int x = (int)fmod(p.x, 4), y = (int)fmod(p.y, 4);
                static const float m[16] = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };
                return (m[y * 4 + x] + 0.5) / 16.0;
            }

            float Hash(float2 p) { return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453); }

            float4 frag(v2f i) : SV_Target
            {
                float2 texel = _MainTex_TexelSize.xy;
                // 게임 화면 픽셀 중심에 맞춰 샘플 (포인트 필터 느낌 유지)
                float2 px = floor(i.uv * _MainTex_TexelSize.zw);
                float2 uv = (px + 0.5) * texel;
                float3 c = tex2D(_MainTex, uv).rgb;

                // 잉크 선 (Sobel)
                float l00 = Luma(tex2D(_MainTex, uv + texel * float2(-1, -1)).rgb);
                float l10 = Luma(tex2D(_MainTex, uv + texel * float2( 0, -1)).rgb);
                float l20 = Luma(tex2D(_MainTex, uv + texel * float2( 1, -1)).rgb);
                float l01 = Luma(tex2D(_MainTex, uv + texel * float2(-1,  0)).rgb);
                float l21 = Luma(tex2D(_MainTex, uv + texel * float2( 1,  0)).rgb);
                float l02 = Luma(tex2D(_MainTex, uv + texel * float2(-1,  1)).rgb);
                float l12 = Luma(tex2D(_MainTex, uv + texel * float2( 0,  1)).rgb);
                float l22 = Luma(tex2D(_MainTex, uv + texel * float2( 1,  1)).rgb);
                float gx = (l20 + 2 * l21 + l22) - (l00 + 2 * l01 + l02);
                float gy = (l02 + 2 * l12 + l22) - (l00 + 2 * l10 + l20);
                float edge = smoothstep(_InkThreshold, _InkThreshold * 1.6, sqrt(gx * gx + gy * gy));

                // 채도
                float y = Luma(c);
                c = lerp(y.xxx, c, _Saturation);

                // 그라데이션 맵 (공포 장면용)
                float3 grad = y < 0.5 ? lerp(_GradShadow.rgb, _GradMid.rgb, y * 2) : lerp(_GradMid.rgb, _GradLight.rgb, y * 2 - 1);
                c = lerp(c, grad, _GradientAmount);

                // 종이 질감
                c *= lerp(1, _PaperTint.rgb, 0.6);
                c += (Hash(px) - 0.5) * _Grain;

                // 색 수 제한 + 디더링
                float L = max(_Levels - 1, 1);
                c = floor(saturate(c) * L + 0.5 + (Bayer4(px) - 0.5) * _Dither) / L;

                // 잉크
                c = lerp(c, _InkColor.rgb, edge * _Ink);

                // 비네트
                float2 d = i.uv - 0.5;
                c *= 1 - _Vignette * smoothstep(0.35, 0.75, length(d * float2(1.1, 1)));

                return float4(saturate(c), i.color.a);
            }
            ENDHLSL
        }
    }
}
