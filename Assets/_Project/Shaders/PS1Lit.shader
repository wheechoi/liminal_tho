// PS1 룩 기본 셰이더 (URP):
//  - 정점 스냅: 클립 좌표를 160x120 격자에 반올림 → 폴리곤 흔들림
//  - Lambert 라이팅 (포인트 라이트 + 전역 앰비언트 _LiminalAmbient), 그림자 없음
//  - 포인트 필터 텍스처, 선형 안개
//  - _Unlit = 1 이면 조명 무시 (전등갓·랜턴처럼 스스로 빛나는 것)
Shader "Liminal/PS1Lit"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1,1,1,1)
        [Toggle] _Unlit ("Unlit (glow)", Float) = 0
        _SnapRes ("Vertex Snap Grid", Vector) = (160, 120, 0, 0)
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Cull [_Cull]

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Unlit;
                float4 _SnapRes;
                float _Cull;
            CBUFFER_END
            half4 _LiminalAmbient;

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                half fog : TEXCOORD3;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                float4 cs = p.positionCS;
                // PS1 정점 스냅 (목업 snap())
                float2 grid = _SnapRes.xy;
                cs.xy = floor(cs.xy / cs.w * grid + 0.5) / grid * cs.w;
                o.positionCS = cs;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.fog = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                half3 color = albedo.rgb;

                if (_Unlit < 0.5)
                {
                    half3 n = normalize(i.normalWS);
                    half3 light = _LiminalAmbient.rgb;

                    InputData inputData = (InputData)0;
                    inputData.positionWS = i.positionWS;
                    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);

                #if defined(_ADDITIONAL_LIGHTS)
                    uint count = GetAdditionalLightsCount();
                    #if USE_CLUSTER_LIGHT_LOOP
                    [loop] for (uint li = 0; li < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); li++)
                    {
                        Light l = GetAdditionalLight(li, i.positionWS);
                        light += l.color * l.distanceAttenuation * saturate(dot(n, l.direction));
                    }
                    #endif
                    LIGHT_LOOP_BEGIN(count)
                        Light l = GetAdditionalLight(lightIndex, i.positionWS);
                        light += l.color * l.distanceAttenuation * saturate(dot(n, l.direction));
                    LIGHT_LOOP_END
                #endif
                    // 가까운 조명에서 하얗게 타지 않도록 부드럽게 눌러줌 (목업의 선형 감쇠 느낌)
                    light = light * 1.4 / (1.0 + light * 0.4);
                    color *= light;
                }

                color = MixFog(color, i.fog);
                return half4(color, 1);
            }
            ENDHLSL
        }

        // 깊이 (URP 깊이 텍스처용)
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Unlit;
                float4 _SnapRes;
                float _Cull;
            CBUFFER_END
            float4 vert(float4 positionOS : POSITION) : SV_POSITION
            {
                float4 cs = TransformObjectToHClip(positionOS.xyz);
                cs.xy = floor(cs.xy / cs.w * _SnapRes.xy + 0.5) / _SnapRes.xy * cs.w;
                return cs;
            }
            half frag() : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
