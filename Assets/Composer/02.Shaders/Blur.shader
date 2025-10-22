Shader "VFXComposer/Blur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurSize ("Blur Size", Float) = 1.0
        _Iterations ("Iterations", Int) = 2
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
            float _BlurSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // Gaussian kernel weights (9-tap)
            static const float weights[9] = {
                0.05, 0.09, 0.12, 0.15, 0.18, 0.15, 0.12, 0.09, 0.05
            };

            fixed4 frag (v2f i) : SV_Target
            {
                float2 texelSize = _MainTex_TexelSize.xy * _BlurSize;
                fixed4 col = fixed4(0, 0, 0, 0);

                // Horizontal + Vertical blur (box approximation)
                for (int x = -4; x <= 4; x++)
                {
                    for (int y = -4; y <= 4; y++)
                    {
                        float2 offset = float2(x, y) * texelSize;
                        float weight = weights[x + 4] * weights[y + 4];
                        col += tex2D(_MainTex, i.uv + offset) * weight;
                    }
                }

                return col;
            }
            ENDCG
        }
    }
}
