Shader "URP/Unlit/LineTwinkleXR"
{
    Properties
    {
        _BaseColor      ("Base Color", Color) = (0.08, 0.9, 1, 0.85)
        [HDR]_GlowColor ("Glow Color", Color) = (0.6, 1, 1, 1)
        _Glow           ("Glow Intensity", Range(0,8)) = 2

        _TwinkleDensity ("Twinkle Density", Range(1,32)) = 10
        _TwinkleSpeed   ("Twinkle Speed", Range(0,10)) = 2
        _TwinkleSharp   ("Twinkle Sharpness", Range(0.1,12)) = 5

        _GlobalPulse    ("Global Pulse", Range(0,2)) = 1
        _ScrollSpeed    ("Scroll Speed (UV units/s)", Range(-10,10)) = 1.5

        _UVScale        ("UV Scale (tiles along length)", Range(0.1,256)) = 8
        _SoftAlphaBoost ("Soft Alpha Boost", Range(0,2)) = 0.6
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            // Additive 계열(반짝임 강조). 투명 합성 원하면 OneMinusSrcAlpha로 교체
            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            // URP & XR
            #pragma multi_compile_instancing
            #pragma multi_compile _ _STEREO_INSTANCING_ON _STEREO_MULTIVIEW_ON _STEREO_DOUBLEWIDE_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;   // LineRenderer가 제공하는 UV(길이/두께)
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _GlowColor;
            float  _Glow;

            float  _TwinkleDensity;
            float  _TwinkleSpeed;
            float  _TwinkleSharp;

            float  _GlobalPulse;
            float  _ScrollSpeed;

            float  _UVScale;        // 라인 길이 방향 타일 수
            float  _SoftAlphaBoost; // 베이스 알파에 부가 가산
            CBUFFER_END

            // 간단 해시(스파클 위치 난수)
            float hash(float n) { return frac(sin(n) * 43758.5453); }

            // u(0~1 반복) 구간에 density 개의 스파클 군집 생성, 시간에 따라 흘림
            float sparkle(float u, float t, float density, float speed, float sharp)
            {
                float sum = 0;
                float N = max(1.0, density);
                [loop]
                for (int i = 0; i < 32; i++)
                {
                    if (i >= N) break;
                    float id    = (float)i;
                    float baseP = (id + 0.5) / N;               // 각 스파클 기준 위치[0,1]
                    float phase = hash(id) * 6.2831853;         // 랜덤 위상
                    // 시간에 따른 미세 이동(기준 위치 around)
                    float pos   = baseP + t * speed * 0.20 + sin(t * speed * 0.5 + phase) * 0.05;

                    // u와 스파크 중심 pos 사이의 주기적 거리(0~1)
                    float d = abs(frac(u - pos) - 0.5) * 2.0;

                    // 날카롭게 모이는 커브(값이 클수록 뾰족)
                    sum += pow(saturate(1.0 - d), sharp);
                }
                return sum / N;
            }

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.pos = TransformObjectToHClip(v.vertex.xyz);
                o.uv  = v.uv;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float t   = _Time.y;
                // 길이 방향 u: 타일 반복 + 스크롤
                float u   = frac(i.uv.x * _UVScale + t * _ScrollSpeed);

                // 반짝임 계산
                float tw  = sparkle(u, t, _TwinkleDensity, _TwinkleSpeed, _TwinkleSharp);

                // 전체 펄스(호흡감)
                float pulse = (sin(t * 3.14159 * 0.8) * 0.5 + 0.5) * _GlobalPulse + (1.0 - _GlobalPulse);

                // 발광 세기
                float glow = tw * _Glow * pulse;

                // 컬러 구성
                float3 col = _BaseColor.rgb + _GlowColor.rgb * glow;

                // 알파: 기본 알파 + 소프트 부스트 + 반짝임 기여
                float alpha = saturate(_BaseColor.a + _SoftAlphaBoost * 0.5 + glow * 0.5);

                return half4(col, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
