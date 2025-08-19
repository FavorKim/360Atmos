Shader "UI/ChromaKey"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _ChromaColor ("Chroma Key Color", Color) = (0,1,0,1) // 기본 녹색
        _Threshold ("Threshold", Range(0,1)) = 0.3
        _Smoothing ("Smoothing", Range(0,1)) = 0.1
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Lighting Off ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                half2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _ChromaColor;
            float _Threshold;
            float _Smoothing;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.texcoord);

                // RGB 차이 계산
                float diff = distance(col.rgb, _ChromaColor.rgb);

                // 알파 계산 (Threshold, Smoothing 기반)
                float alpha = smoothstep(_Threshold, _Threshold + _Smoothing, diff);

                col.a *= alpha;
                return col;
            }
            ENDCG
        }
    }
}
