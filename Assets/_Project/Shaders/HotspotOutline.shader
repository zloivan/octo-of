Shader "OnlyFarms/HotspotOutline"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineWidth ("Outline Width", Float) = 1.5
        _InnerWidth ("Inner Outline Width", Float) = 0.0
        _Brightness ("Brightness", Float) = 1.0
        _FillColor ("Fill Color", Color) = (1,1,1,0)
        _GradientFalloff ("Gradient Falloff", Range(0,1)) = 0.0
        _DashEnabled ("Dash Enabled", Range(0,1)) = 0.0
        _DashDensity ("Dash Density", Float) = 8.0
        _DashSpeed ("Dash Speed", Float) = 1.0
        _DashSharpness ("Dash Sharpness", Range(0,1)) = 0.5
        _PulseEnabled ("Pulse Enabled", Range(0,1)) = 0.0
        _PulseSpeed ("Pulse Speed", Float) = 1.0
        _PulseMin ("Pulse Min", Range(0,1)) = 0.8
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
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

            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _OutlineColor;
                half4 _FillColor;
                float _OutlineWidth;
                float _InnerWidth;
                float _Brightness;
                float _GradientFalloff;
                float _DashEnabled;
                float _DashDensity;
                float _DashSpeed;
                float _DashSharpness;
                float _PulseEnabled;
                float _PulseSpeed;
                float _PulseMin;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color;
                return OUT;
            }

            half SampleNeighborsMax(float2 uv, float2 offset)
            {
                half maxA = 0;
                maxA = max(maxA, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2( offset.x, 0)).a);
                maxA = max(maxA, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-offset.x, 0)).a);
                maxA = max(maxA, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2( 0, offset.y)).a);
                maxA = max(maxA, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2( 0, -offset.y)).a);
                maxA = max(maxA, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2( offset.x, offset.y)).a);
                maxA = max(maxA, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-offset.x, offset.y)).a);
                maxA = max(maxA, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2( offset.x, -offset.y)).a);
                maxA = max(maxA, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-offset.x, -offset.y)).a);
                return maxA;
            }

            half SampleNeighborsMin(float2 uv, float2 offset)
            {
                half minA = 1;
                minA = min(minA, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2( offset.x, 0)).a);
                minA = min(minA, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-offset.x, 0)).a);
                minA = min(minA, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2( 0, offset.y)).a);
                minA = min(minA, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2( 0, -offset.y)).a);
                minA = min(minA, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2( offset.x, offset.y)).a);
                minA = min(minA, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-offset.x, offset.y)).a);
                minA = min(minA, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2( offset.x, -offset.y)).a);
                minA = min(minA, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-offset.x, -offset.y)).a);
                return minA;
            }

            half ComputeDashMask(float2 uv)
            {
                // Градиент alpha = нормаль к краю спрайта
                float2 g;
                g.x = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2( _MainTex_TexelSize.x, 0)).a
                    - SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-_MainTex_TexelSize.x, 0)).a;
                g.y = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0, _MainTex_TexelSize.y)).a
                    - SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0, -_MainTex_TexelSize.y)).a;

                // Тангент = перпендикуляр к нормали
                float2 tangent = normalize(float2(-g.y, g.x) + 0.0001);

                // Проекция UV на тангент = приближение к arc length
                float arcParam = dot(uv, tangent);

                float wave = sin(arcParam * _DashDensity * 40.0 + _Time.y * _DashSpeed);
                return smoothstep(-_DashSharpness, _DashSharpness, wave);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half rawAlpha = texColor.a;
                texColor *= IN.color * _Color;

                // Финальный brightness — общий для всего outline
                half finalBrightness = _Brightness;
                if (_PulseEnabled > 0.5)
                {
                    half pulse = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;
                    finalBrightness *= lerp(_PulseMin, 1.0, pulse);
                }

                // Общий dash mask — вычисляем один раз
                half dashMask = (_DashEnabled > 0.5) ? ComputeDashMask(IN.uv) : 1.0;

                if (rawAlpha > 0.01)
                {
                    // Inner outline
                    if (_InnerWidth > 0.0)
                    {
                        float2 oi = _MainTex_TexelSize.xy * _InnerWidth;
                        half minA = SampleNeighborsMin(IN.uv, oi);

                        if (minA < 0.01)
                        {
                            float2 oiHalf = _MainTex_TexelSize.xy * (_InnerWidth * 0.5);
                            half halfMinA = SampleNeighborsMin(IN.uv, oiHalf);
                            half innerGradient = lerp(1.0, step(0.01, halfMinA), _GradientFalloff);

                            return half4(
                                _OutlineColor.rgb * finalBrightness,
                                _OutlineColor.a * texColor.a * innerGradient * dashMask
                            );
                        }
                    }

                    // Fill interior
                    return half4(_FillColor.rgb, _FillColor.a * texColor.a);
                }

                // Outer outline
                float2 oNear = _MainTex_TexelSize.xy * _OutlineWidth;
                half nearAlpha = SampleNeighborsMax(IN.uv, oNear);

                if (nearAlpha > 0.01)
                {
                    float2 oFar = _MainTex_TexelSize.xy * (_OutlineWidth * 0.5);
                    half farAlpha = SampleNeighborsMax(IN.uv, oFar);
                    half gradientMask = lerp(1.0, farAlpha, _GradientFalloff);

                    return half4(
                        _OutlineColor.rgb * finalBrightness,
                        _OutlineColor.a * gradientMask * dashMask
                    );
                }

                return half4(0, 0, 0, 0);
            }
            ENDHLSL
        }
    }
}