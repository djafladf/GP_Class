Shader "Custom/Ghost"
{
    Properties
    {
        [HDR] _RimColor ("Rim Color", Color) = (1,0.05,0.02,1)
        _RimPower ("Rim Power", Range(0.5,8)) = 3
        _RimThreshold ("Rim Threshold", Range(0,1)) = 0.3
        _RimSoftness ("Rim Softness", Range(0.001,0.5)) = 0.1

        _FillAlpha ("Fill Alpha", Range(0,1)) = 0
        _Fade ("Fade", Range(0,1)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Back
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _RimColor;
                float _RimPower;
                float _RimThreshold;
                float _RimSoftness;
                float _FillAlpha;
                float _Fade;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;

                o.positionCS =
                    TransformObjectToHClip(v.positionOS.xyz);

                o.positionWS =
                    TransformObjectToWorld(v.positionOS.xyz);

                o.normalWS =
                    TransformObjectToWorldNormal(v.normalOS);

                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 normal = normalize(i.normalWS);
                float3 viewDir = normalize(
                    _WorldSpaceCameraPos.xyz - i.positionWS
                );

                // 카메라를 향하는 면 = 0
                // 실루엣 가장자리 = 1
                float fresnel = 1.0 - saturate(
                    dot(normal, viewDir)
                );

                fresnel = pow(fresnel, _RimPower);

                float rim = smoothstep(
                    _RimThreshold,
                    _RimThreshold + _RimSoftness,
                    fresnel
                );

                float alpha = max(rim, _FillAlpha)
                            * _RimColor.a * _Fade;

                return half4(
                    _RimColor.rgb,
                    saturate(alpha)
                );
            }

            ENDHLSL
        }
    }
}
