// TMP SDF text for URP with iridescent FX.
// Same math as TextMeshPro/Mobile/Distance Field (face / outline / underlay,
// stencil for UI masks, clip rect), ported to URP HLSL, plus animated effects:
// The outline is drawn by atlas dilation (sampling around the glyph), so it
// works with bitmap atlases and small text/canvas scales where the SDF ring
// thresholds go negative and flood the quad.
//   0 - none (plain SDF)
//   1 - shine sweep: bright band running across the text
//   2 - palette flow: colors flowing along the text
//   3 - aurora: palette flow + running highlight
//   4 - palette shift: whole text slowly cycles the palette
//   5 - noise sparkle: animated shimmer patches from fbm noise
//   6 - iridescent noise: palette colors driven by fbm noise
//   7 - plasma: classic flowing waves through the palette
// The flow modes (2,3,4,6,7) cycle through the FX color palette
// (_FxColor1..4, _FxColorCount stops); _HueShift offsets along the palette.
// Animation uses _Time (enable "Animated Materials" in the Scene view to preview).
// Editor tools: Tools/Text FX (UI.Editor.TextFxTools).

Shader "Custom/Text/Shimmer"
{
    Properties
    {
        [HDR] _FaceColor     ("Face Color", Color) = (1,1,1,1)
        _FaceDilate          ("Face Dilate", Range(-1,1)) = 0

        [HDR] _OutlineColor  ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth        ("Outline Thickness", Range(0,1)) = 0
        _OutlineSoftness     ("Outline Softness", Range(0,1)) = 0

        [HDR] _UnderlayColor ("Border Color", Color) = (0,0,0,.5)
        _UnderlayOffsetX     ("Border OffsetX", Range(-1,1)) = 0
        _UnderlayOffsetY     ("Border OffsetY", Range(-1,1)) = 0
        _UnderlayDilate      ("Border Dilate", Range(-1,1)) = 0
        _UnderlaySoftness    ("Border Softness", Range(0,1)) = 0

        [HideInInspector] _WeightNormal ("Weight Normal", float) = 0
        [HideInInspector] _WeightBold ("Weight Bold", float) = .5
        [HideInInspector] _ShaderFlags ("Flags", float) = 0
        [HideInInspector] _ScaleRatioA ("Scale RatioA", float) = 1
        [HideInInspector] _ScaleRatioB ("Scale RatioB", float) = 1
        [HideInInspector] _ScaleRatioC ("Scale RatioC", float) = 1

        _MainTex ("Font Atlas", 2D) = "white" {}
        [HideInInspector] _TextureWidth ("Texture Width", float) = 512
        [HideInInspector] _TextureHeight ("Texture Height", float) = 512
        [HideInInspector] _GradientScale ("Gradient Scale", float) = 5
        [HideInInspector] _ScaleX ("Scale X", float) = 1
        [HideInInspector] _ScaleY ("Scale Y", float) = 1
        [HideInInspector] _PerspectiveFilter ("Perspective Correction", Range(0, 1)) = 0.875
        [HideInInspector] _Sharpness ("Sharpness", Range(-1,1)) = 0

        [HideInInspector] _VertexOffsetX ("Vertex OffsetX", float) = 0
        [HideInInspector] _VertexOffsetY ("Vertex OffsetY", float) = 0

        [HideInInspector] _ClipRect ("Clip Rect", vector) = (-32767, -32767, 32767, 32767)
        [HideInInspector] _MaskSoftnessX ("Mask SoftnessX", float) = 0
        [HideInInspector] _MaskSoftnessY ("Mask SoftnessY", float) = 0

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _CullMode ("Cull Mode", Float) = 0
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15

        // ---- Text FX ----
        _EffectMode ("Effect Mode", Range(0,7)) = 3
        _Intensity ("FX Intensity", Range(0,2)) = 1
        _Speed ("FX Speed", Range(0,10)) = 0.5
        _Tiling ("FX Tiling", Range(0.1,4)) = 1
        _FxAngle ("FX Angle", Range(-180,180)) = 0
        _HueShift ("Hue Shift", Range(0,1)) = 0
        _FxColorCount ("FX Color Count", Range(1,4)) = 4
        [HDR] _FxColor1 ("FX Color 1", Color) = (1,0.25,0.25,1)
        [HDR] _FxColor2 ("FX Color 2", Color) = (1,0.85,0.2,1)
        [HDR] _FxColor3 ("FX Color 3", Color) = (0.3,1,0.4,1)
        [HDR] _FxColor4 ("FX Color 4", Color) = (0.35,0.5,1,1)
        _SweepWidth ("Sweep Width", Range(0.02,1)) = 0.25
        _SweepSoftness ("Sweep Softness", Range(0.1,10)) = 2
        _SweepBands ("Sweep Bands", Range(1,4)) = 2
        [HDR] _SweepColor ("Sweep Color", Color) = (1.6,1.6,1.6,1)
        _NoiseScale ("Noise Scale", Range(0.5,30)) = 4
        _NoiseOctaves ("Noise Octaves", Range(1,5)) = 3
        _NoiseContrast ("Noise Contrast", Range(0.1,6)) = 2
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull [_CullMode]
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local __ OUTLINE_ON
            #pragma shader_feature_local __ UNDERLAY_ON UNDERLAY_INNER
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _FaceColor;
                float _FaceDilate;
                float4 _OutlineColor;
                float _OutlineWidth;
                float _OutlineSoftness;
                float4 _UnderlayColor;
                float _UnderlayOffsetX, _UnderlayOffsetY, _UnderlayDilate, _UnderlaySoftness;
                float _WeightNormal, _WeightBold;
                float _ShaderFlags;
                float _ScaleRatioA, _ScaleRatioB, _ScaleRatioC;
                float _TextureWidth, _TextureHeight, _GradientScale, _ScaleX, _ScaleY, _PerspectiveFilter, _Sharpness;
                float _VertexOffsetX, _VertexOffsetY;
                float4 _ClipRect;
                float _MaskSoftnessX, _MaskSoftnessY;
                float _StencilComp, _Stencil, _StencilOp, _StencilWriteMask, _StencilReadMask;
                float _CullMode, _ColorMask;
                // Text FX
                float _EffectMode, _Intensity, _Speed, _Tiling, _FxAngle;
                float _HueShift;
                float _FxColorCount;
                float4 _FxColor1, _FxColor2, _FxColor3, _FxColor4;
                float _SweepWidth, _SweepSoftness, _SweepBands;
                float4 _SweepColor;
                float _NoiseScale, _NoiseOctaves, _NoiseContrast;
            CBUFFER_END

            struct vertex_t
            {
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 color : COLOR;
                float2 texcoord0 : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
            };

            struct pixel_t
            {
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
                float4 vertex : SV_POSITION;
                float4 faceColor : COLOR;
                float4 outlineColor : COLOR1;
                float4 texcoord0 : TEXCOORD0;       // atlas uv, mask uv
                float4 param : TEXCOORD1;           // scale(x), biasIn(y), biasOut(z), bias(w)
                float4 mask : TEXCOORD2;            // clip-space position(xy), softness(zw)
                float4 fxPos : TEXCOORD5;           // clip-space position for the FX coordinate
                #if (UNDERLAY_ON | UNDERLAY_INNER)
                float4 texcoord1 : TEXCOORD3;       // atlas uv + offset, alpha, reserved
                float2 underlayParam : TEXCOORD4;   // scale(x), bias(y)
                #endif
            };

            // Color ramp through _FxColor1..4; t in 0..1 maps across _FxColorCount stops.
            float3 FxPalette(float t)
            {
                int count = clamp((int)(_FxColorCount + 0.5), 1, 4);
                t = saturate(t);
                if (count == 1) return _FxColor1.rgb;
                float seg = t * (count - 1);
                int i = clamp((int)seg, 0, count - 2);
                float f = frac(seg);
                float3 c0 = _FxColor1.rgb;
                float3 c1 = _FxColor2.rgb;
                if (i == 1) { c0 = _FxColor2.rgb; c1 = _FxColor3.rgb; }
                else if (i == 2) { c0 = _FxColor3.rgb; c1 = _FxColor4.rgb; }
                return lerp(c0, c1, f);
            }

            // Procedural value noise + fbm for the noise FX modes.
            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i);
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float Fbm(float2 p, int octaves)
            {
                float v = 0.0;
                float amp = 0.5;
                float freq = 1.0;
                [unroll]
                for (int o = 0; o < 5; o++)
                {
                    if (o >= octaves) break;
                    v += amp * ValueNoise(p * freq);
                    freq *= 2.0;
                    amp *= 0.5;
                }
                return v;
            }

            pixel_t vert(vertex_t input)
            {
                pixel_t output;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float bold = step(input.texcoord1.y, 0);

                float4 vert = input.vertex;
                vert.x += _VertexOffsetX;
                vert.y += _VertexOffsetY;
                float4 vPosition = TransformObjectToHClip(vert.xyz);

                float2 pixelSize = vPosition.w;
                pixelSize /= float2(_ScaleX, _ScaleY) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));

                float scale = rsqrt(dot(pixelSize, pixelSize));
                scale *= abs(input.texcoord1.y) * _GradientScale * (_Sharpness + 1);
                float3 worldNormal = TransformObjectToWorldNormal(input.normal.xyz);
                float3 worldPos = TransformObjectToWorld(vert.xyz);
                float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - worldPos);
                if (UNITY_MATRIX_P[3][3] == 0)
                    scale = lerp(abs(scale) * (1 - _PerspectiveFilter), scale, abs(dot(worldNormal, viewDir)));

                float weight = lerp(_WeightNormal, _WeightBold, bold) / 4.0;
                weight = (weight + _FaceDilate) * _ScaleRatioA * 0.5;

                float layerScale = scale;

                scale /= 1 + (_OutlineSoftness * _ScaleRatioA * scale);
                float bias = (0.5 - weight) * scale - 0.5;
                float outline = _OutlineWidth * _ScaleRatioA * 0.5 * scale;

                float opacity = input.color.a;
                #if (UNDERLAY_ON | UNDERLAY_INNER)
                opacity = 1.0;
                #endif

                float4 faceColor = float4(input.color.rgb, opacity) * _FaceColor;
                faceColor.rgb *= faceColor.a;

                float4 outlineColor = _OutlineColor;
                outlineColor.a *= opacity;
                outlineColor.rgb *= outlineColor.a;

                #if (UNDERLAY_ON | UNDERLAY_INNER)
                layerScale /= 1 + ((_UnderlaySoftness * _ScaleRatioC) * layerScale);
                float layerBias = (.5 - weight) * layerScale - .5 - ((_UnderlayDilate * _ScaleRatioC) * .5 * layerScale);

                float x = -(_UnderlayOffsetX * _ScaleRatioC) * _GradientScale / _TextureWidth;
                float y = -(_UnderlayOffsetY * _ScaleRatioC) * _GradientScale / _TextureHeight;
                float2 layerOffset = float2(x, y);
                #endif

                // UV for the masking texture
                float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
                float2 maskUV = (vert.xy - clampedRect.xy) / (clampedRect.zw - clampedRect.xy);

                output.vertex = vPosition;
                output.faceColor = faceColor;
                output.outlineColor = outlineColor;
                output.texcoord0 = float4(input.texcoord0.x, input.texcoord0.y, maskUV.x, maskUV.y);
                output.param = float4(scale, bias - outline, bias + outline, bias);
                output.mask = float4(vert.xy * 2 - clampedRect.xy - clampedRect.zw,
                    0.25 / (0.25 * float2(_MaskSoftnessX, _MaskSoftnessY) + pixelSize.xy));
                output.fxPos = vPosition;
                #if (UNDERLAY_ON || UNDERLAY_INNER)
                output.texcoord1 = float4(input.texcoord0 + layerOffset, input.color.a, 0);
                output.underlayParam = float2(layerScale, layerBias);
                #endif

                return output;
            }

            float4 frag(pixel_t input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float d = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.texcoord0.xy).a * input.param.x;
                // The TMP bias carries a fixed half-texel SDF fudge (-0.5) that
                // dominates when the shader scale is small (bitmap fonts, scaled
                // canvases, small text) and pushes the thresholds negative,
                // flooding the whole quad with the outline color. Thresholds are
                // clamped to 0, and the outline is drawn by dilation (sampling
                // the atlas a few screen pixels around the glyph), which works
                // with any atlas type (bitmap or SDF) at any scale.
                #ifdef OUTLINE_ON
                float2 dilate = float2(ddx(input.texcoord0.x), ddy(input.texcoord0.y)) * (_OutlineWidth * 8.0);
                float outAlpha = d;
                outAlpha = max(outAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.texcoord0.xy + dilate).a * input.param.x);
                outAlpha = max(outAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.texcoord0.xy - dilate).a * input.param.x);
                outAlpha = max(outAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.texcoord0.xy + float2(dilate.y, dilate.x)).a * input.param.x);
                outAlpha = max(outAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.texcoord0.xy - float2(dilate.y, dilate.x)).a * input.param.x);
                float faceMask = saturate(d - max(input.param.w, 0.0));
                float4 c = lerp(input.outlineColor, input.faceColor, faceMask) * saturate(outAlpha - max(input.param.w, 0.0));
                #else
                float4 c = input.faceColor * saturate(d - max(input.param.w, 0.0));
                #endif

                #if UNDERLAY_ON
                d = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.texcoord1.xy).a * input.underlayParam.x;
                c += float4(_UnderlayColor.rgb * _UnderlayColor.a, _UnderlayColor.a) * saturate(d - max(input.underlayParam.y, 0.0)) * (1 - c.a);
                #endif

                #if UNDERLAY_INNER
                float sd = saturate(d - input.param.z);
                d = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.texcoord1.xy).a * input.underlayParam.x;
                c += float4(_UnderlayColor.rgb * _UnderlayColor.a, _UnderlayColor.a) * (1 - saturate(d - input.underlayParam.y)) * sd * (1 - c.a);
                #endif

                // ---- Text FX ----
                // fxUv covers the whole text quad in 0..1, so effects do not
                // depend on font size or canvas scale.
                float2 fxUv = input.fxPos.xy / input.fxPos.w * 0.5 + 0.5;
                float rad = _FxAngle * 0.0174532925;
                float ca = cos(rad), sa = sin(rad);
                float2 p = float2(dot(fxUv, float2(ca, -sa)), dot(fxUv, float2(sa, ca)));
                float t = _Time.y * _Speed;
                float3 tint = float3(1, 1, 1);
                int mode = (int)(_EffectMode + 0.5);

                if (mode == 1) // shine sweep
                {
                    float pos = frac(p.x * _SweepBands - t);
                    float band = saturate((1.0 - abs(pos - 0.5) * 2.0) / _SweepWidth);
                    band = pow(band, _SweepSoftness);
                    tint = lerp(float3(1, 1, 1), _SweepColor.rgb, band * _Intensity);
                }
                else if (mode == 2 || mode == 3) // palette flow / aurora
                {
                    float pos = frac(p.x * _Tiling + _HueShift - t);
                    float3 palColor = FxPalette(pos);
                    tint = lerp(float3(1, 1, 1), palColor, _Intensity);
                    if (mode == 3) // aurora: moving highlight over the palette flow
                    {
                        float bandPos = frac(p.x * _SweepBands - t * 1.4);
                        float band = saturate((1.0 - abs(bandPos - 0.5) * 2.0) / _SweepWidth);
                        band = pow(band, _SweepSoftness);
                        tint *= 1.0 + band * _SweepColor.rgb * _Intensity;
                    }
                }
                else if (mode == 4) // palette shift: whole text cycles the palette
                {
                    tint = lerp(float3(1, 1, 1), FxPalette(frac(_HueShift + t)), _Intensity);
                }
                else if (mode == 5) // noise sparkle: animated shimmer patches
                {
                    int octaves = clamp((int)(_NoiseOctaves + 0.5), 1, 5);
                    float n = Fbm(p * _NoiseScale + float2(t * 0.7, t * 0.35), octaves);
                    n = pow(saturate(n), _NoiseContrast);
                    tint = lerp(float3(1, 1, 1), _SweepColor.rgb, n * _Intensity);
                }
                else if (mode == 6) // iridescent noise: palette colors driven by fbm
                {
                    int octaves = clamp((int)(_NoiseOctaves + 0.5), 1, 5);
                    float n = Fbm(p * _NoiseScale + float2(t * 0.5, t * 0.3), octaves);
                    float pos = frac(_HueShift + n * _Tiling - t * 0.2);
                    tint = lerp(float3(1, 1, 1), FxPalette(pos), _Intensity);
                }
                else if (mode == 7) // plasma: flowing waves through the palette
                {
                    int octaves = clamp((int)(_NoiseOctaves + 0.5), 1, 5);
                    float2 q = p * _NoiseScale;
                    float v = sin(q.x + t * 1.3) + sin(q.y + t * 0.9)
                            + sin((q.x + q.y) * 0.5 + t * 0.5)
                            + Fbm(q + float2(t * 0.4, -t * 0.3), octaves) * 1.5;
                    float pos = frac(_HueShift + v * 0.15 * _Tiling + t * 0.1);
                    tint = lerp(float3(1, 1, 1), FxPalette(pos), _Intensity);
                }

                c.rgb *= tint;

                #if UNITY_UI_CLIP_RECT
                float2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(input.mask.xy)) * input.mask.zw);
                c *= m.x * m.y;
                #endif

                #if (UNDERLAY_ON | UNDERLAY_INNER)
                c *= input.texcoord1.z;
                #endif

                #if UNITY_UI_ALPHACLIP
                clip(c.a - 0.001);
                #endif

                return c;
            }
            ENDHLSL
        }
    }

    Fallback "TextMeshPro/Mobile/Distance Field"
    CustomEditor "UI.Editor.TextFxShaderGUI"
}