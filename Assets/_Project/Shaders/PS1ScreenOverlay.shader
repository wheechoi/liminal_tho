// 화면 전체 덮개 (UI 최상단 RawImage에 사용):
//  - 스캔라인: 화면 3픽셀마다 1픽셀 28% 어둡게
//  - 비네트: 중앙 55%부터 가장자리 75% 검정
//  - 6초 주기 미세 플리커 (목업 CSS @keyframes flick)
Shader "Liminal/PS1ScreenOverlay"
{
    Properties
    {
        [PerRendererData] _MainTex ("Unused", 2D) = "white" {}
        _ScanDarkness ("Scanline Darkness", Range(0,1)) = 0.28
        _ScanPeriod ("Scanline Period (px)", Float) = 3
        _VignetteStart ("Vignette Start", Range(0,1)) = 0.55
        _VignetteDarkness ("Vignette Darkness", Range(0,1)) = 0.75
        _FlickerColor ("Flicker Color", Color) = (0.8, 0.93, 0.93, 1)
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off ZTest Always
        Blend One OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _ScanDarkness, _ScanPeriod, _VignetteStart, _VignetteDarkness;
            float4 _FlickerColor;

            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord.xy;
                return o;
            }

            float flicker(float t)
            {
                float p = frac(t / 6.0);
                if (p >= 0.47 && p < 0.48) return 0.06;
                if (p >= 0.83 && p < 0.84) return 0.04;
                return 0;
            }

            float4 frag(v2f i) : SV_Target
            {
                float scan = fmod(floor(i.pos.y), _ScanPeriod) < 1.0 ? _ScanDarkness : 0.0;
                // 타원형 비네트 (CSS radial-gradient ellipse)
                float r = length((i.uv - 0.5) * 2.0) / 1.41421356;
                float vig = smoothstep(_VignetteStart, 1.0, r) * _VignetteDarkness;
                float dark = 1.0 - (1.0 - scan) * (1.0 - vig);
                float f = flicker(_Time.y);
                // 결과 = 플리커색*f + 배경*(1 - dark)
                return float4(_FlickerColor.rgb * f, dark);
            }
            ENDHLSL
        }
    }
}
