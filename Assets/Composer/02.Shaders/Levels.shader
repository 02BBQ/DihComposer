Shader "VFXComposer/Levels"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _InputBlack ("Input Black", Range(0, 1)) = 0.0
        _InputWhite ("Input White", Range(0, 1)) = 1.0
        _Gamma ("Gamma", Range(0.1, 10)) = 1.0
        _OutputBlack ("Output Black", Range(0, 1)) = 0.0
        _OutputWhite ("Output White", Range(0, 1)) = 1.0
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
            float _InputBlack;
            float _InputWhite;
            float _Gamma;
            float _OutputBlack;
            float _OutputWhite;

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

                // Apply levels adjustment to RGB channels
                float3 rgb = col.rgb;

                // Input levels
                rgb = saturate((rgb - _InputBlack) / (_InputWhite - _InputBlack));

                // Gamma correction
                rgb = pow(rgb, 1.0 / _Gamma);

                // Output levels
                rgb = rgb * (_OutputWhite - _OutputBlack) + _OutputBlack;

                return fixed4(rgb, col.a);
            }
            ENDCG
        }
    }
}
