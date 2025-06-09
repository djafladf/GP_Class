Shader "Custom/CubeUVFade"
{
    Properties
    {
        _Color ("Color Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
        HLSLPROGRAM
        #pragma vertex Vert
        #pragma fragment Frag
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        struct Attributes
        {
            float3 positionOS : POSITION;
            float3 normalOS   : NORMAL;
            float2 uv         : TEXCOORD0;
        };

        struct Varyings
        {
            float4 posCS       : SV_POSITION;
            float2 uv          : TEXCOORD0;
            float3 worldNormal: TEXCOORD1;
            float2  localPos    : TEXCOORD2;
        };
        float4 _Color;

        Varyings Vert(Attributes IN)
        {
            Varyings OUT;
            OUT.posCS       = TransformObjectToHClip(IN.positionOS);
            OUT.worldNormal = normalize(TransformObjectToWorldNormal(IN.normalOS));
            OUT.uv          = IN.uv;
            OUT.localPos   = IN.positionOS.xy;
            return OUT;
        }

        float4 Frag(Varyings IN) : SV_Target
        {

           if (IN.worldNormal.y > 0.01) discard;
           float y = saturate(IN.localPos.y + 0.5f);
           return float4(_Color.rgb,0.2f + (abs(sin(_Time.w)  * 0.1f) - y));
        }
        ENDHLSL
        }
    }
    FallBack Off
}
