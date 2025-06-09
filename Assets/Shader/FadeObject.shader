Shader "Custom/FadeObject"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        _Ratio("Ratio", Float) = 1
        _Min("MinAlpha",Range(0,1)) = 0.1
        _Color("Color",Color) = (1,1,1,1)
        _UnscaledTime("UnScaledTime",Float) = 0
    }
        SubShader
        {
            Tags
            {
            "Queue" = "Transparent"
            //"IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            // "PreviewType" = "Plane"
            // "CanUseSpriteAtlas" = "True"
            }
            //  Stencil
            // {
            //     Ref 1
            //     Comp Equal
            //     Pass Keep
            // }

            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            //ZTest[unity_GUIZTestMode]

            Pass{
                Name "Default"
                HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
                //#include "UnityCG.cginc"

                struct appdata
                {
                    float4 vertex : POSITION;
                    float2 uv : TEXCOORD0;
                };

                struct v2f
                {
                    float4 pos : SV_POSITION;
                    float2 uv : TEXCOORD0;
                };

                sampler2D _MainTex;
                float4 _MainTex_ST;
                float4 _Color;
                float _Ratio;
                float  _Min;
                float _UnscaledTime;

                v2f vert (appdata v)
                {
                    v2f o;
                    o.pos = TransformObjectToHClip(v.vertex.xyz);
                    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                    return o;
                }

                half4 frag (v2f i) : SV_Target
                {
                    half4 col = tex2D(_MainTex,i.uv) * _Color;
                    return half4(col.rgb,( abs( cos(_UnscaledTime * 0.05f * _Ratio) ) * (1 - _Min) + _Min)*col.a);
                }
                ENDHLSL
            }
        }
}
