Shader "VFXComposer/Blend"
{
    Properties
    {
        _BaseTexture ("Base Texture", 2D) = "white" {}
        _BlendTexture ("Blend Texture", 2D) = "white" {}
        _BlendMode ("Blend Mode", Int) = 0
        _Opacity ("Opacity", Range(0, 1)) = 1
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

            sampler2D _BaseTexture;
            sampler2D _BlendTexture;
            int _BlendMode;
            float _Opacity;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // Blend mode functions
            float3 BlendNormal(float3 base, float3 blend)
            {
                return blend;
            }

            float3 BlendMultiply(float3 base, float3 blend)
            {
                return base * blend;
            }

            float3 BlendScreen(float3 base, float3 blend)
            {
                return 1.0 - (1.0 - base) * (1.0 - blend);
            }

            float3 BlendOverlay(float3 base, float3 blend)
            {
                float3 result;
                result.r = base.r < 0.5 ? (2.0 * base.r * blend.r) : (1.0 - 2.0 * (1.0 - base.r) * (1.0 - blend.r));
                result.g = base.g < 0.5 ? (2.0 * base.g * blend.g) : (1.0 - 2.0 * (1.0 - base.g) * (1.0 - blend.g));
                result.b = base.b < 0.5 ? (2.0 * base.b * blend.b) : (1.0 - 2.0 * (1.0 - base.b) * (1.0 - blend.b));
                return result;
            }

            float3 BlendAdd(float3 base, float3 blend)
            {
                return min(base + blend, 1.0);
            }

            float3 BlendSubtract(float3 base, float3 blend)
            {
                return max(base - blend, 0.0);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 baseColor = tex2D(_BaseTexture, i.uv);
                fixed4 blendColor = tex2D(_BlendTexture, i.uv);

                float3 result;

                // Select blend mode
                if (_BlendMode == 0) // Normal
                    result = BlendNormal(baseColor.rgb, blendColor.rgb);
                else if (_BlendMode == 1) // Multiply
                    result = BlendMultiply(baseColor.rgb, blendColor.rgb);
                else if (_BlendMode == 2) // Screen
                    result = BlendScreen(baseColor.rgb, blendColor.rgb);
                else if (_BlendMode == 3) // Overlay
                    result = BlendOverlay(baseColor.rgb, blendColor.rgb);
                else if (_BlendMode == 4) // Add
                    result = BlendAdd(baseColor.rgb, blendColor.rgb);
                else if (_BlendMode == 5) // Subtract
                    result = BlendSubtract(baseColor.rgb, blendColor.rgb);
                else
                    result = baseColor.rgb;

                // Apply opacity
                result = lerp(baseColor.rgb, result, _Opacity);

                return fixed4(result, 1.0);
            }
            ENDCG
        }
    }
}
