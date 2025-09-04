Shader "URP/Unlit/InfiniteGrid_VR"
{
    Properties
    {
        _FloorColor     ("Floor Base Color", Color) = (0,0,0,0)
        _MinorColor     ("Minor Line Color", Color) = (1,1,1,0.35)
        _MajorColor     ("Major Line Color", Color) = (1,1,1,1)
        _Emission       ("Emission Multiplier", Range(0,10)) = 2

        _CellSize       ("Cell Size (meters)", Float) = 1.0
        _MajorEvery     ("Major Every N cells", Float) = 10.0
        _MinorThickness ("Minor Thickness (cell frac)", Range(0.0005, 0.05)) = 0.01
        _MajorThickness ("Major Thickness (cell frac)", Range(0.0005, 0.05)) = 0.02

        _FadeStart      ("Fade Start Distance", Float) = 50.0
        _FadeEnd        ("Fade End Distance",   Float) = 200.0
        _GridHeight     ("Grid Height (Y)",     Float) = 0.0
        _FillFloor      ("Fill Floor (0~1)",    Range(0,1)) = 0.0
    }

    SubShader
    {
        Tags{
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "RenderPipeline"="UniversalPipeline"
        }

        // 투명 그리드가 앞 오브젝트에 정상적으로 가려지도록
        ZWrite On
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Back

        Pass
        {
            Name "InfiniteGrid_VR"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            // ========= XR / Instancing 지원 =========
            // 싱글패스 인스턴싱/멀티뷰 대응
            #pragma multi_compile _ _STEREO_INSTANCING_ON _STEREO_MULTIVIEW_ON
            // GPU 인스턴싱
            #pragma multi_compile_instancing
            // (필요 시) 포그
            #pragma multi_compile_fog

            // URP 코어
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _FloorColor;
                float4 _MinorColor;
                float4 _MajorColor;
                float  _Emission;

                float  _CellSize;
                float  _MajorEvery;
                float  _MinorThickness;
                float  _MajorThickness;

                float  _FadeStart;
                float  _FadeEnd;
                float  _GridHeight;
                float  _FillFloor;
            CBUFFER_END

            struct Attributes {
                float4 positionOS : POSITION;

                // XR/Instancing 매크로
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings {
                float4 positionCS : SV_Position;
                float3 positionWS : TEXCOORD0;

                // XR/Instancing 매크로
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert (Attributes v)
            {
                Varyings o;

                // XR/Instancing 셋업
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float3 ws = TransformObjectToWorld(v.positionOS.xyz);
                o.positionWS = ws;

                // TransformWorldToHClip 은 스테레오(멀티뷰/인스턴싱) 대응
                o.positionCS = TransformWorldToHClip(ws);
                return o;
            }

            // 화면공간 AA 라인
            float aaLine(float d, float halfWidth)
            {
                float w = fwidth(d);
                return smoothstep(halfWidth + w, halfWidth - w, abs(d));
            }

            float4 frag (Varyings i) : SV_Target
            {
                // 스테레오 셋업
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                // 바닥(y = _GridHeight) 기준
                float3 pos = i.positionWS;
                pos.y = _GridHeight;

                // 격자 좌표 (셀 단위)
                float2 g  = pos.xz / max(_CellSize, 1e-5);

                // Minor 라인
                float2 minorCell = frac(g) - 0.5;
                float  dMinor    = min(abs(minorCell.x), abs(minorCell.y));
                float  minor     = aaLine(dMinor, _MinorThickness);

                // Major 라인 (N셀마다)
                float2 gMaj      = g / max(_MajorEvery, 1.0);
                float2 majorCell = frac(gMaj) - 0.5;
                float  dMajor    = min(abs(majorCell.x), abs(majorCell.y));
                float  major     = aaLine(dMajor, _MajorThickness);

                // 라인 합성
                float minorA = minor * _MinorColor.a;
                float majorA = major; // 메이저를 1로 가정
                float lineA  = saturate(max(minorA, majorA));
                float3 lineC = lerp(_MinorColor.rgb, _MajorColor.rgb, step(0.5, major));

                // 카메라-거리 페이드(지평선)
                float3 cam = GetCameraPositionWS(); // URP XR에서 스테레오 대응
                float  dist = length((pos - cam).xz);
                float  fade = smoothstep(_FadeEnd, _FadeStart, dist);

                // 바닥 채우기(옵션)
                float  floorA = _FillFloor * fade;
                float3 floorC = _FloorColor.rgb;

                float a = saturate(lineA * fade + floorA);
                float3 c = (lineC * lineA + floorC * floorA) / max(a, 1e-6);

                // 네온 느낌
                c *= _Emission;

                return float4(c, a);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
