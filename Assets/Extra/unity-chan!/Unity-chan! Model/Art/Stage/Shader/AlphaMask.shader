Shader "Custom/AlphaMask_URP"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _AlphaMask ("Mask (A)", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        Pass
        {
            Name "AlphaMaskPass"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

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

            sampler2D _MainTex;
            sampler2D _AlphaMask;
            float4 _MainTex_ST;
            float4 _AlphaMask_ST;

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                half4 color = tex2D(_MainTex, IN.uv);
                half alpha = tex2D(_AlphaMask, IN.uv).a;
                return half4(color.rgb, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
