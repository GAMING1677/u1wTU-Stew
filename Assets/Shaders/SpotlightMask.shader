Shader "UI/SpotlightMask"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (0, 0, 0, 0.7)
        _SpotlightCenter ("Spotlight Center", Vector) = (0.5, 0.5, 0, 0)
        _SpotlightRadius ("Spotlight Radius", Float) = 0.2
        _EdgeSoftness ("Edge Softness", Range(0, 0.5)) = 0.05
        _EllipseRatio ("Ellipse Ratio (X/Y)", Float) = 1.0
        
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
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
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float2 screenPos : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float2 _SpotlightCenter;
            float _SpotlightRadius;
            float _EdgeSoftness;
            float _EllipseRatio;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                
                // スクリーン座標（0-1）を計算
                float4 clipPos = OUT.vertex;
                OUT.screenPos = (clipPos.xy / clipPos.w) * 0.5 + 0.5;
                
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // アスペクト比で正円に補正後、EllipseRatioで楕円化
                // EllipseRatio = 1.0 で正円、> 1.0 で横長、< 1.0 で縦長
                float aspectRatio = _ScreenParams.x / _ScreenParams.y;
                float combinedRatio = aspectRatio * _EllipseRatio;
                
                float2 correctedPos = float2(IN.screenPos.x * combinedRatio, IN.screenPos.y);
                float2 correctedCenter = float2(_SpotlightCenter.x * combinedRatio, _SpotlightCenter.y);
                
                // 補正後の座標で距離を計算
                float dist = distance(correctedPos, correctedCenter);
                
                // 距離に基づいてアルファを計算
                // スポットライト内部はアルファ0（透明）、外部はアルファ1（暗い）
                float innerRadius = _SpotlightRadius - _EdgeSoftness;
                float outerRadius = _SpotlightRadius;
                
                float alpha = smoothstep(innerRadius, outerRadius, dist);
                
                // スポットライトが無効（半径0以下）の場合は全体を暗くする
                if (_SpotlightRadius <= 0)
                {
                    alpha = 1.0;
                }
                
                fixed4 color = IN.color;
                color.a *= alpha;
                
                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif
                
                return color;
            }
            ENDCG
        }
    }
}
