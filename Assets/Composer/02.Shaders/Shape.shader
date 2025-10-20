Shader "VFXComposer/Shape"
{
    Properties
    {
        _ShapeType ("Shape Type", Int) = 0
        _Size ("Size", Float) = 0.5
        _Smoothness ("Smoothness", Float) = 0.01
        _FillColor ("Fill Color", Color) = (1,1,1,1)
        _BackgroundColor ("Background Color", Color) = (0,0,0,0)
    }
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        
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
            
            int _ShapeType;
            float _Size;
            float _Smoothness;
            float4 _FillColor;
            float4 _BackgroundColor;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv - 0.5;
                float d = 1;
                
                if (_ShapeType == 0)
                {
                    d = length(uv);
                }
                else if (_ShapeType == 1)
                {
                    d = max(abs(uv.x), abs(uv.y));
                }
                else if (_ShapeType == 2)
                {
                    float a = atan2(uv.y, uv.x);
                    float r = length(uv);
                    d = r - _Size * (0.5 + 0.5 * cos(5 * a));
                }
                
                float mask = 1 - smoothstep(_Size - _Smoothness, _Size + _Smoothness, d);
                return lerp(_BackgroundColor, _FillColor, mask);
            }
            ENDCG
        }
    }
}