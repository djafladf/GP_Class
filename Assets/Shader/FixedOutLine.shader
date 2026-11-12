
Shader "UI/FixedBorderImage"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _SourceBorderPx ("Source Border Pixels", Float) = 12
        _TargetBorderPx ("Screen Border Pixels", Float) = 12

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)]
        _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "CanUseSpriteAtlas" = "False"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            fixed4 _Color;
            float _SourceBorderPx;
            float _TargetBorderPx;
            float4 _ClipRect;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 localPos : TEXCOORD1;
                fixed4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.localPos = v.vertex;
                o.color = v.color * _Color;
                return o;
            }

            float SliceUV(float uv, float src, float dst)
            {
                if (uv < dst)
                    return uv * src / max(dst, 0.000001);

                if (uv > 1.0 - dst)
                    return 1.0 - (1.0 - uv) * src
                                     / max(dst, 0.000001);

                return src + (uv - dst) * (1.0 - 2.0 * src)
                                      / max(1.0 - 2.0 * dst, 0.000001);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 원본 텍스처의 테두리 영역
                float2 src = min(
                    _SourceBorderPx * _MainTex_TexelSize.xy,
                    0.499
                );

                // 화면의 1픽셀이 차지하는 UV 거리
                float2 pixelUV = float2(
                    length(float2(ddx(i.uv.x), ddy(i.uv.x))),
                    length(float2(ddx(i.uv.y), ddy(i.uv.y)))
                );

                // 실제 화면 픽셀 기준 테두리 영역
                float2 dst = min(
                    _TargetBorderPx * pixelUV,
                    0.499
                );

                float2 uv;
                uv.x = SliceUV(i.uv.x, src.x, dst.x);
                uv.y = SliceUV(i.uv.y, src.y, dst.y);

                fixed4 col = tex2D(_MainTex, uv) * i.color;

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(
                    i.localPos.xy, _ClipRect
                );
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif

                return col;
            }

            ENDCG
        }
    }
}
