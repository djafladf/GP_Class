Shader "Custom/Blur"{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurRadius("BlurRadius", Float) = 1.0
        _AlphaWeight("AlphaWeight",Float) = 1.0
        _Power("Power",Int) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Transparent" }
        Pass
        {
            Name "BlurPass"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float _BlurRadius;
            float4 _MainTex_TexelSize;
            float _AlphaWeight;
            int _Power;

             struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.positionCS = TransformObjectToHClip(v.vertex.xyz);
                o.uv = v.uv;
                return o;
            }

            float Gaussian(float x, float deviation)
            {
                return exp(-(x * x) / (2.0 * deviation * deviation)) / (deviation * sqrt(2.0 * 3.14159));
            }

            half4 frag(v2f i) : SV_Target
            {
                half4 MainColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                MainColor.a *= _AlphaWeight;

                if (MainColor.a == 0) discard;
                if (_BlurRadius <= 0.001) return MainColor;

                float2 texelSize = _MainTex_TexelSize.xy;

                float4 color = float4(0, 0, 0, 0);
                float totalWeight = 0;

                for (int x = -_Power; x <= _Power; x++)
                {
                    float2 offset = float2(x, 0) * texelSize * _BlurRadius;
                    float2 sampleUV = clamp(i.uv + offset, 0.0, 1.0);
                    float4 sampleColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, sampleUV);
                    float weight = Gaussian(x, _BlurRadius);
                    color += sampleColor * weight;
                    totalWeight += weight;
                }

                for (int y = -_Power; y <= _Power; y++)
                {
                    float2 offset = float2(0, y) * texelSize * _BlurRadius;
                    float2 sampleUV = clamp(i.uv + offset, 0.0, 1.0);
                    float4 sampleColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, sampleUV);
                    float weight = Gaussian(y, _BlurRadius);
                    color += sampleColor * weight;
                    totalWeight += weight;
                }

                color /= totalWeight;
                color.a *= _AlphaWeight;

                return color;
            }
            ENDHLSL
        }
    }
}