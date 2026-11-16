Shader "Custom/SwordTrail"
{
    Properties
    {
        _InkTex ("Ink Texture", 2D) = "white" {}
        _Tint ("Tint", Color) = (1,1,1,1)
        _Opacity ("Opacity", Range(0,1)) = 1

        // 전체를 더 검게
        _DarkGamma ("Dark Gamma", Range(1,3)) = 1.6

        // 검은 부분은 더 진하게, 밝은 부분은 더 약하게
        _AlphaContrast ("Alpha Contrast", Range(0.5,3)) = 1.25

        // 손잡이쪽은 짧게 남기고
        _BaseCut ("Base Cut", Range(0,1)) = 0.72

        // 경계 부드럽게
        _TailFeather ("Tail Feather", Range(0.001,0.1)) = 0.025

        // vertex color alpha가 거의 끝날 때만 빠르게 사라짐
        _LifeFadeStart ("Life Fade Start", Range(0,1)) = 0.15
        _LifeFadeEnd ("Life Fade End", Range(0,1)) = 0.02
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
            Name "Forward"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_InkTex);
            SAMPLER(sampler_InkTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                float _Opacity;
                float _DarkGamma;
                float _AlphaContrast;
                float _BaseCut;
                float _TailFeather;
                float _LifeFadeStart;
                float _LifeFadeEnd;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                half4 color       : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                half4 color       : COLOR;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }
            float smootherstep(float a, float b, float x)
            {
                float t = saturate((x - a) / (b - a));
                return t * t * t * (t * (t * 6.0 - 15.0) + 10.0);
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 uv = saturate(i.uv);
                float2 texUV = float2(1-uv.y,1 - uv.x);

                half4 tex = SAMPLE_TEXTURE2D(_InkTex, sampler_InkTex, texUV);

                // 1) 색: Tint 추가
                half3 rgb = tex.rgb * _Tint.rgb;
                //rgb = 1 - rgb;
                // 잔상
                float baseZone = smoothstep(0.0, 0.7, uv.x);
                // 기본 잔상은 거의 전체 유지
                float cutLine = lerp(0.02, _BaseCut, baseZone);
                // 오래된 궤적만 잘라냄
                float tailMask = smoothstep(
                    cutLine - _TailFeather,
                    cutLine + _TailFeather,
                    uv.y
                );

                // 수명
                float life = smoothstep(
                    _LifeFadeEnd,
                    max(_LifeFadeStart, _LifeFadeEnd + 0.0001),
                    i.color.a
                );
                // 위 -> 아래 연하게
                float height = smoothstep(0,0.9,smoothstep(0,1,texUV.y)) + 0.1;

                // texture alpha도 같이 사용
                float alpha = tex.a * height * tailMask * life * _Opacity;
    

                return half4(rgb, alpha);
            }

            ENDHLSL
        }
    }
}