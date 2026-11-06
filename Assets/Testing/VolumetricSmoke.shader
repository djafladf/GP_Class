
Shader "Custom/VolumetricSmoke"
{
    Properties
    {
        _SmokeColor ("Smoke Color", Color) = (0.55, 0.55, 0.55, 1)
        _Density ("Density", Range(0, 10)) = 1.5
        _NoiseScale ("Noise Scale", Range(0.1, 10)) = 2.5
        _Speed ("Animation Speed", Range(0, 3)) = 0.2
        _DensityTex ("Density",3D) = "white"{}
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
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _SmokeColor;
                float _Density;
                float _NoiseScale;
                float _Speed;
            CBUFFER_END

            TEXTURE3D(_DensityTex);
            SAMPLER(sampler_DensityTex);

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionOS = input.positionOS.xyz;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            // 간단한 3차원 노이즈
            float Noise3D(float3 p)
            {
                float n = sin(p.x * 2.1 + sin(p.z * 1.7));
                n += sin(p.y * 2.7 + p.x * 1.3);
                n += sin(p.z * 3.1 + p.y * 1.9);

                return saturate(n / 6.0 + 0.5);
            }

            // 현재 위치의 연기 밀도
            float SampleDensity(float3 p)
            {
                float3 uvw = p + 0.5;

                if (any(uvw < 0.0) || any(uvw > 1.0))
                    return 0.0;


                float gridDensity = SAMPLE_TEXTURE3D_LOD(
                    _DensityTex,
                    sampler_DensityTex,
                    uvw,
                    0
                ).r;

                return SAMPLE_TEXTURE3D_LOD(
                    _DensityTex,
                    sampler_DensityTex,
                    uvw,
                    0
                ).r;
                // float3 q = p * 2.0;

                // 상자 모서리에서 연기가 부드럽게 사라지도록
                float3 edge = saturate((0.5 - abs(p)) * 5.0);
                float boundary = edge.x * edge.y * edge.z;

                // 시간에 따라 움직이는 노이즈
                float t = _Time.y * _Speed;

                float noise = Noise3D(
                    p * _NoiseScale * 2.0 +
                    float3(t, t * 0.4, 0)
                );

                return gridDensity * boundary * noise;
            }

            half4 frag(
                Varyings input,
                bool isFrontFace : SV_IsFrontFace
            ) : SV_Target
            {
                
                float3 cameraOS = TransformWorldToObject(GetCameraPositionWS());

                float3 ray = input.positionOS - cameraOS;
                float3 direction = normalize(ray);

                // Ray와 로컬 AABB(-0.5 ~ 0.5)의 교차 계산
                float3 safeDir = direction;
                safeDir.x = abs(safeDir.x) < 0.00001 ? 0.00001 : safeDir.x;
                safeDir.y = abs(safeDir.y) < 0.00001 ? 0.00001 : safeDir.y;
                safeDir.z = abs(safeDir.z) < 0.00001 ? 0.00001 : safeDir.z;

                float3 a = (-0.5 - cameraOS) / safeDir;
                float3 b = ( 0.5 - cameraOS) / safeDir;

                float3 nearT = min(a, b);
                float3 farT = max(a, b);

                float entry = max(
                    max(nearT.x, nearT.y), nearT.z
                );
                float exitT = min(
                    min(farT.x, farT.y), farT.z
                );

                entry = max(entry, 0.0);

                if (exitT <= entry)
                    return half4(0, 0, 0, 0);
                    float2 screenUV = input.positionCS.xy / _ScaledScreenParams.xy;

                float rawDepth = SampleSceneDepth(screenUV);

                float3 sceneWS = ComputeWorldSpacePosition(
                    screenUV,
                    rawDepth,
                    UNITY_MATRIX_I_VP
                );

                float3 sceneOS = TransformWorldToObject(sceneWS);

                float sceneT = dot(sceneOS - cameraOS, direction);

                exitT = min(exitT, sceneT);

                if (exitT <= entry)
                    return half4(0, 0, 0, 0);


                // Raymarching
                const int STEPS = 128;

                float stepLength = (exitT - entry) / STEPS;
                float accumulatedAlpha = 0;
                [loop]
                for (int i = 0; i < STEPS; i++)
                {
                    float distance = entry + (i + 0.5) * stepLength;
                    float3 samplePos = cameraOS + direction * distance;

                    float density = SampleDensity(samplePos);

                    float alpha = 1.0 - exp(
                        -density * stepLength * _Density
                    );

                    accumulatedAlpha +=
                        (1.0 - accumulatedAlpha) * alpha;
                }

                return half4(
                    _SmokeColor.rgb,
                    saturate(accumulatedAlpha)
                );
            }
            ENDHLSL
        }
    }
}
