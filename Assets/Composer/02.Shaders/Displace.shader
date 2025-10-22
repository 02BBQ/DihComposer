Shader "VFXComposer/Displace"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _DispTex ("Displacement Map", 2D) = "gray" {}
        _Intensity ("Intensity", Float) = 0.1
        _AngleOffset ("Angle Offset", Float) = 0.0
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
            sampler2D _DispTex;
            float _Intensity;
            float _AngleOffset;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample displacement map
                float4 dispColor = tex2D(_DispTex, i.uv);

                // Convert to displacement vector
                // R channel = X displacement, G channel = Y displacement
                float2 displacement = (dispColor.rg - 0.5) * 2.0;

                // Apply angle offset
                float angle = _AngleOffset * 3.14159265 / 180.0;
                float cosA = cos(angle);
                float sinA = sin(angle);
                float2 rotatedDisp = float2(
                    displacement.x * cosA - displacement.y * sinA,
                    displacement.x * sinA + displacement.y * cosA
                );

                // Apply intensity
                rotatedDisp *= _Intensity;

                // Sample base texture with displaced UV
                float2 displacedUV = i.uv + rotatedDisp;
                fixed4 col = tex2D(_MainTex, displacedUV);

                return col;
            }
            ENDCG
        }
    }
}
