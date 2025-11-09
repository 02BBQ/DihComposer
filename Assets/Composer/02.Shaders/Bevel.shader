Shader "VFXComposer/Bevel"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Size ("Size", Range(0, 0.5)) = 0.1
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
        _Inner ("Inner", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Size;
            float _Smoothness;
            float _Inner;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                float gray = dot(col.rgb, float3(0.299, 0.587, 0.114));

                float offset = _Size * 0.01;
                float samples = 8.0;
                float angleStep = 6.28318 / samples;

                float minDist = 1.0;
                float maxDist = 0.0;

                for (float angle = 0.0; angle < 6.28318; angle += angleStep)
                {
                    float2 dir = float2(cos(angle), sin(angle)) * offset;
                    float sampleGray = dot(tex2D(_MainTex, i.uv + dir).rgb, float3(0.299, 0.587, 0.114));
                    minDist = min(minDist, sampleGray);
                    maxDist = max(maxDist, sampleGray);
                }

                float edge = maxDist - minDist;
                float bevel = lerp(gray, edge, _Size * 2.0);

                if (_Inner > 0.5)
                {
                    bevel = lerp(gray, 1.0 - edge, _Size * 2.0);
                }

                float smoothed = lerp(gray, bevel, _Smoothness);

                return fixed4(smoothed, smoothed, smoothed, col.a);
            }
            ENDCG
        }
    }
}
