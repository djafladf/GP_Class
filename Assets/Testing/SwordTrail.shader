
Shader "Custom/SwordTrail"
{
    Properties
    {
        _BrushTex ("Brush Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (0.02,0.02,0.02,0.8)
        _TipColor ("Tip Color", Color) = (0.12,0.12,0.12,1)

        _Cutoff ("Ink Cutoff", Range(0,1)) = 0.25
        _Softness ("Edge Softness", Range(0.001,0.5)) = 0.08
        _EdgeFade ("Edge Fade", Range(0.001,0.5)) = 0.08
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

            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BrushTex);
            SAMPLER(sampler_BrushTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _TipColor;
                float _Cutoff;
                float _Softness;
                float _EdgeFade;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS =
                    TransformObjectToHClip(v.positionOS.xyz);

                o.uv = v.uv;
                o.color = v.color;

                return o;
            }
            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float Noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                f = f * f * (3.0 - 2.0 * f);

                float a = Hash(i);
                float b = Hash(i + float2(1, 0));
                float c = Hash(i + float2(0, 1));
                float d = Hash(i + float2(1, 1));

                return lerp(
                    lerp(a, b, f.x),
                    lerp(c, d, f.x),
                    f.y
                );
            }
            
        
        half4 frag(Varyings i) : SV_Target
        {
            float2 uv = i.uv;

            half4 col = lerp(
                _BaseColor, _TipColor, saturate(uv.x)
            );

            float2 brushUV = float2(
                uv.y,
                lerp(0.4, 0.6, uv.x)
            );

            half4 brush = SAMPLE_TEXTURE2D(
                _BrushTex, sampler_BrushTex, brushUV
            );

            float ink = smoothstep(0.04, 0.3, brush.a);

            float sideBias = smoothstep(0.0, 1.0, uv.x);
            float coarse = Noise(float2(uv.x * 6.0, 7.3));
            float fine   = Noise(float2(uv.x * 18.0, 13.1));

            float baseDepth = lerp(0.62, 0.05, sideBias);

            // 불규칙성은 작게 유지
            float variation = (coarse - 0.5) * 0.05+ (fine - 0.5) * 0.02;

            float depth = saturate(baseDepth + variation);

            // 완전히 딱 잘리지 않도록 경계만 살짝 부드럽게
            float fray = smoothstep(
    depth - 0.02,
    depth + 0.02,
    uv.y
);

            // 기존 붓자국 마스크 유지
            col.a *= ink * fray * i.color.a;
            
            col.a = saturate(col.a * 1.3);

                return col;
            }


            ENDHLSL
        }
    }
}
