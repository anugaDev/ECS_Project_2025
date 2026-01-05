Shader "Unlit/AnimatedIndicatorShader"
{        Properties
    {
        _Color("Ring Color", Color) = (0,1,0,1)
        _Thickness("Ring Thickness", Range(0.01,5)) = 1
        _Speed("Pulse Speed", Float) = 2.0
        _LocalTime("Local Time", Float) = 0.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

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

            float4 _Color;
            float _Thickness;
            float _Speed;
            float _LocalTime;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv * 2 - 1;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float r = length(uv);

                float pulse = 0.5 + 0.5 * sin(_LocalTime * _Speed * 6.2831);
                pulse = pulse * pulse;

                float alpha = smoothstep(_Thickness + 0.02, _Thickness, r) * pulse;
                return float4(_Color.rgb, alpha * _Color.a);
            }
            ENDCG
        }
    }
}