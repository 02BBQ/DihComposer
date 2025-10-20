Shader "VFXComposer/Noise"
{
    Properties
    {
        _NoiseType ("Noise Type", Int) = 0
        _Scale ("Scale", Float) = 5
        _Octaves ("Octaves", Int) = 4
        _Persistence ("Persistence", Float) = 0.5
        _Offset ("Offset", Vector) = (0,0,0,0)
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        
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
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            
            int _NoiseType;
            int _Octaves;
            float _Scale;
            float _Persistence;
            float2 _Offset;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }
            
            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                
                float a = hash(i);
                float b = hash(i + float2(1, 0));
                float c = hash(i + float2(0, 1));
                float d = hash(i + float2(1, 1));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            
            float fbm(float2 p)
            {
                float value = 0;
                float amplitude = 1;
                float frequency = 1;
                
                for (int i = 0; i < _Octaves; i++)
                {
                    value += amplitude * noise(p * frequency);
                    frequency *= 2;
                    amplitude *= _Persistence;
                }
                
                return value;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv * _Scale + _Offset;
                float n = fbm(uv);
                return fixed4(n, n, n, 1);
            }
            ENDCG
        }
    }
}