Shader "Custom/WaterDrop"
{
    
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _Distortion ("Distortion Amount", Range(0, 1)) = 0.1
        _FresnelPower ("Fresnel Power", Range(1, 10)) = 5
        _ReflectionStrength ("Reflection Strength", Range(0, 1)) = 0.5
        _Opacity ("Opacity", Range(0, 1)) = 0.8
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // TEXTURES
            sampler2D _MainTex;
            sampler2D _NormalMap;
            float4 _MainTex_ST;

            float _Distortion;
            float _FresnelPower;
            float _ReflectionStrength;
            float _Opacity;

            TEXTURE2D(_CameraOpaqueTexture); SAMPLER(sampler_CameraOpaqueTexture);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
                float4 screenPos : TEXCOORD3;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(positionWS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS = GetWorldSpaceViewDir(positionWS);
                OUT.screenPos = ComputeScreenPos(OUT.positionHCS);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 normalWS = normalize(IN.normalWS);
                float3 viewDir = normalize(IN.viewDirWS);

                float3 normalTS = UnpackNormal(tex2D(_NormalMap, IN.uv));
                float2 distortionUV = normalTS.xy * _Distortion;

                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                screenUV = screenUV * 0.5 + 0.5; 
                screenUV += distortionUV;

                float4 background = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV);

                float3 reflectDir = reflect(-viewDir, normalWS); reflectDir.y = saturate(reflectDir.y);
                float4 reflection = SAMPLE_TEXTURECUBE(unity_SpecCube0, samplerunity_SpecCube0, reflectDir);

                float fresnel = pow(1.0 - saturate(dot(viewDir, normalWS)), _FresnelPower);

                float3 finalColor = lerp(background.rgb, reflection.rgb, _ReflectionStrength * fresnel);

                return float4(finalColor, fresnel * _Opacity);
            }
            ENDHLSL
        }
    }
}
