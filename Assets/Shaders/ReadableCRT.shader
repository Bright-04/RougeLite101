Shader "Hidden/RougeLite101/ReadableCRT"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _WarmTint ("Warm Tint", Color) = (1.01, 1.0, 0.99, 1.0)
        _WarmStrength ("Warm Strength", Range(0, 1)) = 0.06
        _Curvature ("Curvature XY", Vector) = (8.0, 8.0, 0, 0)
        _ShadowMaskStrength ("Shadow Mask Strength", Range(0, 1)) = 0.1
        _MaskScale ("Shadow Mask Scale", Range(0.5, 3.0)) = 1.0
        _ScanlineStrength ("Scanline Strength", Range(0, 0.2)) = 0.11
        _BlurBlend ("Blur Blend", Range(0, 0.2)) = 0.09
        _GlowStrength ("Bloom Intensity", Range(0, 1)) = 0.3
        _BloomThreshold ("Bloom Threshold", Range(0.5, 1.0)) = 0.9
        _BloomScatter ("Bloom Scatter", Range(0, 1)) = 0.45
        _ColorBleedStrength ("Color Bleed", Range(0, 0.2)) = 0.07
        _PhosphorMaskStrength ("Phosphor RGB Mask", Range(0, 1)) = 0.12
        _ChromaticStrength ("Chromatic Aberration", Range(0, 0.03)) = 0.008
        _NoiseStrength ("Noise Strength", Range(0, 0.08)) = 0.012
        _FadeAmount ("Fade Amount", Range(0, 0.25)) = 0.06
        _Saturation ("Saturation", Range(0.8, 1.3)) = 1.05
        _Contrast ("Contrast", Range(0.8, 1.5)) = 1.03
        _VignetteStrength ("Vignette", Range(0, 0.3)) = 0.06
    }

    SubShader
    {
        Tags
        {
            "Queue"="Overlay"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        Cull Off
        ZWrite Off
        ZTest Always
        Blend Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _WarmTint;
            float _WarmStrength;
            float4 _Curvature;
            float _ShadowMaskStrength;
            float _MaskScale;
            float _ScanlineStrength;
            float _BlurBlend;
            float _GlowStrength;
            float _BloomThreshold;
            float _BloomScatter;
            float _ColorBleedStrength;
            float _PhosphorMaskStrength;
            float _ChromaticStrength;
            float _NoiseStrength;
            float _FadeAmount;
            float _Saturation;
            float _Contrast;
            float _VignetteStrength;

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
                fixed4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            float Hash12(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float3 SampleRgb(float2 uv)
            {
                return tex2D(_MainTex, saturate(uv)).rgb;
            }

            float2 Curve(float2 uv)
            {
                uv = uv * 2.0 - 1.0;
                float2 offset = abs(uv.yx) / max(_Curvature.xy, float2(0.0001, 0.0001));
                uv = uv + uv * offset * offset;
                uv = uv * 0.5 + 0.5;
                return uv;
            }

            float3 ComputeShadowMask(float2 uv)
            {
                float2 pixel = uv * _MainTex_TexelSize.zw * _MaskScale;
                float triad = frac(pixel.x / 3.0);

                float3 mask;
                mask.r = exp(-72.0 * (triad - 0.1667) * (triad - 0.1667));
                mask.g = exp(-72.0 * (triad - 0.5000) * (triad - 0.5000));
                mask.b = exp(-72.0 * (triad - 0.8333) * (triad - 0.8333));

                float slot = 0.88 + 0.12 * sin(pixel.y * 3.14159265);
                return (0.76 + mask * 0.32) * slot;
            }

            float3 SampleBloom(float2 uv, float2 texel, float scatter)
            {
                float2 radius = texel * lerp(1.0, 2.8, scatter);

                float3 c = 0.0;
                c += SampleRgb(uv) * 0.227027;
                c += SampleRgb(uv + float2(radius.x, 0.0)) * 0.1945946;
                c += SampleRgb(uv - float2(radius.x, 0.0)) * 0.1945946;
                c += SampleRgb(uv + float2(0.0, radius.y)) * 0.1216216;
                c += SampleRgb(uv - float2(0.0, radius.y)) * 0.1216216;
                c += SampleRgb(uv + radius) * 0.0702703;
                c += SampleRgb(uv - radius) * 0.0702703;
                return c;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = Curve(i.uv);
                float2 texel = _MainTex_TexelSize.xy;
                float2 centered = uv - 0.5;
                float edgeDistance = saturate(length(centered) * 1.35);
                float2 edgeDirection = normalize(centered + 0.0001);

                float2 caOffset = edgeDirection * texel * _ChromaticStrength * edgeDistance;
                float3 baseColor;
                baseColor.r = SampleRgb(uv + caOffset).r;
                baseColor.g = SampleRgb(uv).g;
                baseColor.b = SampleRgb(uv - caOffset).b;

                float bleedOffset = texel.x * _ColorBleedStrength * 3.0;
                float3 bleedColor;
                bleedColor.r = SampleRgb(uv + float2(bleedOffset, 0.0)).r;
                bleedColor.g = baseColor.g;
                bleedColor.b = SampleRgb(uv - float2(bleedOffset, 0.0)).b;
                baseColor = lerp(baseColor, bleedColor, saturate(_ColorBleedStrength * 0.85));

                float3 blur =
                    SampleRgb(uv + float2(texel.x, 0.0)) +
                    SampleRgb(uv - float2(texel.x, 0.0)) +
                    SampleRgb(uv + float2(0.0, texel.y)) +
                    SampleRgb(uv - float2(0.0, texel.y));
                blur *= 0.25;

                float3 color = lerp(baseColor, blur, _BlurBlend);

                float luminance = dot(color, float3(0.299, 0.587, 0.114));
                float bloomMask = smoothstep(_BloomThreshold, 0.999, luminance);
                float3 bloomBlur = SampleBloom(uv, texel, _BloomScatter);
                float3 glow = bloomBlur * bloomMask;
                color += glow * _GlowStrength;

                float scanline = sin(uv.y * _MainTex_TexelSize.w * 3.14159265);
                scanline = 0.5 + 0.5 * scanline;
                color *= 1.0 - ((1.0 - scanline) * _ScanlineStrength);

                float3 phosphorMask = ComputeShadowMask(uv);
                color *= lerp(1.0.xxx, phosphorMask, _PhosphorMaskStrength);

                float luma = dot(color, float3(0.299, 0.587, 0.114));
                color = lerp(luma.xxx, color, _Saturation);
                color = (color - 0.5) * _Contrast + 0.5;
                color = lerp(color, color * _WarmTint.rgb, _WarmStrength);
                color = lerp(color, color * 0.94 + 0.03, _FadeAmount);

                float noise = Hash12(floor(uv * _MainTex_TexelSize.zw) + floor(_Time.y * 24.0));
                color += (noise - 0.5) * _NoiseStrength;

                return float4(saturate(color), 1.0);
            }
            ENDCG
        }
    }
}
