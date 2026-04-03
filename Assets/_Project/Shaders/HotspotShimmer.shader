// Assets/_Project/Shaders/HotspotShimmer.shader
Shader "OnlyFarms/HotspotShimmer"
{
    Properties
    {
        _MainTex      ("Sprite Texture", 2D)    = "white" {}
        _Color        ("Tint",           Color) = (1,1,1,1)
        _GlowColor    ("Glow Color",     Color) = (1,0.85,0.3,1)
        _GlowWidth    ("Glow Width",     Float) = 3.0
        _GlowIntensity("Glow Intensity", Float) = 1.2
        _PulseSpeed   ("Pulse Speed",    Float) = 1.5
        _PulseMin     ("Pulse Min",   Range(0,1)) = 0.4
    }

    SubShader
    {
        Tags
        {
            "Queue"          = "Transparent"
            "RenderType"     = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector"= "True"
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

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings   { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _GlowColor;
                float _GlowWidth;
                float _GlowIntensity;
                float _PulseSpeed;
                float _PulseMin;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color       = IN.color;
                return OUT;
            }

            half SampleNeighborsMax(float2 uv, float2 offset)
            {
                half m = 0;
                m = max(m, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2( offset.x,  0      )).a);
                m = max(m, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-offset.x,  0      )).a);
                m = max(m, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2( 0,          offset.y)).a);
                m = max(m, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2( 0,         -offset.y)).a);
                m = max(m, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2( offset.x,  offset.y)).a);
                m = max(m, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-offset.x,  offset.y)).a);
                m = max(m, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2( offset.x, -offset.y)).a);
                m = max(m, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-offset.x, -offset.y)).a);
                return m;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex      = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half  ownAlpha = tex.a;

                float2 offset  = _MainTex_TexelSize.xy * _GlowWidth;
                half   glowRing = saturate(SampleNeighborsMax(IN.uv, offset) - ownAlpha);

                half pulse = lerp(_PulseMin, 1.0, sin(_Time.y * _PulseSpeed) * 0.5 + 0.5);

                half4 col  = tex * IN.color * _Color;
                col.rgb   *= pulse;
                col.rgb   += _GlowColor.rgb * _GlowIntensity * glowRing * pulse;
                col.a      = saturate(ownAlpha + glowRing * _GlowColor.a * pulse);

                return col;
            }
            ENDHLSL
        }
    }
}