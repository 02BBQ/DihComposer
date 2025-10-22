Shader "VFXComposer/Transform"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _OffsetX ("Offset X", Float) = 0.0
        _OffsetY ("Offset Y", Float) = 0.0
        _Rotation ("Rotation", Float) = 0.0
        _ScaleX ("Scale X", Float) = 1.0
        _ScaleY ("Scale Y", Float) = 1.0
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
            float _OffsetX;
            float _OffsetY;
            float _Rotation;
            float _ScaleX;
            float _ScaleY;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // Center around 0.5
                uv -= 0.5;

                // Apply scale
                uv.x /= _ScaleX;
                uv.y /= _ScaleY;

                // Apply rotation
                float angle = _Rotation * 3.14159265 / 180.0;
                float cosA = cos(angle);
                float sinA = sin(angle);
                float2 rotatedUV = float2(
                    uv.x * cosA - uv.y * sinA,
                    uv.x * sinA + uv.y * cosA
                );

                // Apply offset
                rotatedUV.x -= _OffsetX;
                rotatedUV.y -= _OffsetY;

                // Back to 0-1 range
                rotatedUV += 0.5;

                // Sample texture
                fixed4 col = tex2D(_MainTex, rotatedUV);

                // Return black for out-of-bounds
                if (rotatedUV.x < 0 || rotatedUV.x > 1 || rotatedUV.y < 0 || rotatedUV.y > 1)
                {
                    col = fixed4(0, 0, 0, 0);
                }

                return col;
            }
            ENDCG
        }
    }
}
