Shader "Unlit/SelectionCircleShader"
{
    Properties
    {
        _Color ("Color", Color) = (0,1,0,0.8)
        _OuterRadius ("Outer Radius", Range(0.1, 0.5)) = 0.45
        _InnerRadius ("Inner Radius", Range(0.0, 0.45)) = 0.35
        _Softness ("Edge Softness", Range(0.001, 0.1)) = 0.01
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _OuterRadius;
                float _InnerRadius;
                float _Softness;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv - 0.5;
                float dist = length(uv);

                float outerAlpha = 1.0 - smoothstep(_OuterRadius - _Softness, _OuterRadius, dist);
                float innerAlpha = smoothstep(_InnerRadius - _Softness, _InnerRadius, dist);

                float alpha = outerAlpha * innerAlpha;

                return half4(_Color.rgb, _Color.a * alpha);
            }
            ENDHLSL
        }
    }
}