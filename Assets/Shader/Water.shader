Shader "Custom/Waater"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _NormalMap("NormalMap",2D) = "bump" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Power ("Blend Power", Range(0,1)) = 0.5
        _Alpha ("Alpha",Range(0,1)) = 0.8
    }
    SubShader
    {
        Tags { "Queue" = "Overlay"  "RenderType" = "Transparent" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
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
                float4 tangentOS : TANGENT;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 screenUV : TEXCOORD1;
            };

            float4 _MainTex_ST;
            float4 _NormalMap_ST;
            sampler2D _MainTex;
            sampler2D _NormalMap;
            
            float4 _Color;
            float _Power;
            float _Alpha;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.screenUV = ComputeScreenPos(OUT.positionHCS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                IN.uv.xy = abs(1 - IN.uv.xy);
                float2 uv = IN.uv;
                uv.x += sin(_Time.x) * 0.05;
                uv.y += sin(_Time.x) * 0.05;
                float3 normal = UnpackNormal(tex2D(_NormalMap, uv)).rgb;

                float4 reflectColor = tex2D(_MainTex, IN.uv + normal.xy);

                float4 finalColor = lerp(_Color, reflectColor, _Power);
                finalColor.a = _Alpha;
                return finalColor;

            }
            ENDHLSL
        }
    }

    FallBack Off
}