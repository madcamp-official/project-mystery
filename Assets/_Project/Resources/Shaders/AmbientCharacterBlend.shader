Shader "Wake/UI/Ambient Character Blend"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Saturation ("Saturation", Range(0, 1.2)) = 0.72
        _Exposure ("Exposure", Range(0.4, 1.2)) = 0.82
        _Contrast ("Contrast", Range(0.5, 1.2)) = 0.88
        _Softness ("Painterly Softness", Range(0, 1)) = 0.3
        _LightDirection ("Light Direction", Vector) = (0.5,0.5,0,0)
        _UvRect ("Atlas UV Rect", Vector) = (0,0,1,1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip (
            "Use Alpha Clip",
            Float
        ) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
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
            Name "Default"

            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct AppData
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float _Saturation;
            float _Exposure;
            float _Contrast;
            float _Softness;
            float4 _LightDirection;
            float4 _UvRect;

            Varyings Vert(AppData input)
            {
                Varyings output;
                output.worldPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.color = input.color * _Color;
                return output;
            }

            fixed4 Frag(Varyings input) : SV_Target
            {
                float2 texel = _MainTex_TexelSize.xy;
                fixed4 center = tex2D(_MainTex, input.uv);
                fixed4 softened =
                    center * 4.0 +
                    tex2D(_MainTex, input.uv + float2(texel.x, 0.0)) +
                    tex2D(_MainTex, input.uv - float2(texel.x, 0.0)) +
                    tex2D(_MainTex, input.uv + float2(0.0, texel.y)) +
                    tex2D(_MainTex, input.uv - float2(0.0, texel.y));
                softened *= 0.125;

                fixed4 sampled = lerp(center, softened, _Softness);
                sampled += _TextureSampleAdd;

                float3 color = sampled.rgb;
                float luminance = dot(
                    color,
                    float3(0.299, 0.587, 0.114));
                color = lerp(luminance.xxx, color, _Saturation);
                color = (color - 0.5) * _Contrast + 0.5;

                float2 localUv =
                    (input.uv - _UvRect.xy) /
                    max(_UvRect.zw, float2(0.0001, 0.0001));
                float2 lightDirection = normalize(
                    _LightDirection.xy + float2(0.0001, 0.0001));
                float lightRamp = dot(localUv - 0.5, lightDirection);
                color *= 1.0 + lightRamp * 0.18;
                color *= input.color.rgb * _Exposure;

                float3 stepped = floor(saturate(color) * 32.0 + 0.5) / 32.0;
                color = lerp(color, stepped, 0.12);

                fixed4 result;
                result.rgb = saturate(color);
                result.a = sampled.a * input.color.a;

                #ifdef UNITY_UI_CLIP_RECT
                result.a *= UnityGet2DClipping(
                    input.worldPosition.xy,
                    _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(result.a - 0.001);
                #endif

                return result;
            }
            ENDCG
        }
    }
}
