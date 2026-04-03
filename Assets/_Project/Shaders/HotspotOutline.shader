Shader "OnlyFarms/HotspotOutline"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineWidth ("Outline Width", Float) = 1.5
        _Brightness ("Brightness", Float) = 1.0
        _InnerWidth ("Inner Outline Width", Float) = 0.0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MainTex_TexelSize;
                half4 _OutlineColor;
                half4  _Color; 
                float _OutlineWidth;
                float _InnerWidth;
                float _Brightness;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half rawAlpha = texColor.a;
                texColor *= IN.color * _Color;

                if (rawAlpha > 0.01)
                {
                    // Пиксель внутри спрайта — проверяем inner outline
                    if (_InnerWidth > 0.0)
                    {
                        float2 oi = _MainTex_TexelSize.xy * _InnerWidth;

                        half minAlpha = 1;
                        minAlpha = min(
                            minAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2( oi.x, 0)).a);
                        minAlpha = min(
                            minAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2(-oi.x, 0)).a);
                        minAlpha = min(
                            minAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2( 0, oi.y)).a);
                        minAlpha = min(
                            minAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2( 0, -oi.y)).a);
                        minAlpha = min(
                            minAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2( oi.x, oi.y)).a);
                        minAlpha = min(
                            minAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2(-oi.x, oi.y)).a);
                        minAlpha = min(
                            minAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2( oi.x, -oi.y)).a);
                        minAlpha = min(
                            minAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2(-oi.x, -oi.y)).a);

                        if (minAlpha < 0.01)
                            return half4(_OutlineColor.rgb * _Brightness, _OutlineColor.a * texColor.a);
                    }

                    return half4(0, 0, 0, 0);
                }

                // Пиксель прозрачный — проверяем outer outline
                float2 o = _MainTex_TexelSize.xy * _OutlineWidth;

                half maxAlpha = 0;
                maxAlpha = max(maxAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2( o.x, 0)).a);
                maxAlpha = max(maxAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2(-o.x, 0)).a);
                maxAlpha = max(maxAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2( 0, o.y)).a);
                maxAlpha = max(maxAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2( 0, -o.y)).a);
                maxAlpha = max(maxAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2( o.x, o.y)).a);
                maxAlpha = max(maxAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2(-o.x, o.y)).a);
                maxAlpha = max(maxAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2( o.x, -o.y)).a);
                maxAlpha = max(maxAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2(-o.x, -o.y)).a);

                if (maxAlpha > 0.01)
                    return half4(_OutlineColor.rgb * _Brightness, _OutlineColor.a);

                return half4(0, 0, 0, 0);
            }
            ENDHLSL
        }
    }
}