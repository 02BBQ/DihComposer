Shader "VFXComposer/Gradient"
{
    Properties
    {
        _ColorA ("Color A", Color) = (0,0,0,1)
        _ColorB ("Color B", Color) = (1,1,1,1)
        _Angle ("Angle", Float) = 0
        _GradientType ("Type", Int) = 0
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
            
            float4 _ColorA;
            float4 _ColorB;
            float _Angle;
            int _GradientType;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float t = 0;
                
                if (_GradientType == 0)
                {
                    float2 dir = float2(cos(_Angle), sin(_Angle));
                    t = dot(uv - 0.5, dir) + 0.5;
                }
                else if (_GradientType == 1)
                {
                    t = length(uv - 0.5) * 2;
                }
                
                t = saturate(t);
                return lerp(_ColorA, _ColorB, t);
            }
            ENDCG
        }
    }
}