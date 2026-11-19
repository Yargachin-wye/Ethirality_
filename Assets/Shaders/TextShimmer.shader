// TMP SDF text for URP with iridescent FX.
// Same math as TextMeshPro/Mobile/Distance Field (face / outline / underlay,
// stencil for UI masks, clip rect), ported to URP HLSL, plus animated effects:
//   0 - none (plain SDF)
//   1 - shine sweep: bright band running across the text
//   2 - hue flow: colors flowing along the text
//   3 - aurora: hue flow + running highlight
//   4 - hue shift: whole text slowly changes color
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
        _EffectMode ("Effect Mode", Range(0,4)) = 3
        _Intensity ("FX Intensity", Range(0,2)) = 1
        _Speed ("FX Speed", Range(0,10)) = 0.5
        _Tiling ("FX Tiling", Range(0.1,4)) = 1
        _FxAngle ("FX Angle", Range(-180,180)) = 0
        _HueShift ("Hue Shift", Range(0,1)) = 0
        _Saturation ("Saturation", Range(0,1)) = 0.85
        _Value ("Value", Range(0,2)) = 1.1
        _SweepWidth ("Sweep Width", Range(0.02,1)) = 0.25
        _SweepSoftness ("Sweep Softness", Range(0.1,10)) = 2
        _SweepBands ("Sweep Bands", Range(1,4)) = 2
        [HDR] _SweepColor ("Sweep Color", Color) = (1.6,1.6,1.6,1)
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
                float _HueShift, _Saturation, _Value;
                float _SweepWidth, _SweepSoftness, _SweepBands;
                float4 _SweepColor;
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

            float3 HsvToRgb(float3 c)
            {
                float3 p = abs(frac(c.xxx + float3(0.0, 2.0 / 3.0, 1.0 / 3.0)) * 6.0 - 3.0);
                return c.z * lerp(float3(1, 1, 1), saturate(p - 1.0), c.y);
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
                outlineColor = lerp(faceColor, outlineColor, sqrt(min(1.0, (outline * 2))));

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
                float4 c = input.faceColor * saturate(d - input.param.w);

                #ifdef OUTLINE_ON
                c = lerp(input.outlineColor, input.faceColor, saturate(d - input.param.z));
                c *= saturate(d - input.param.y);
                #endif

                #if UNDERLAY_ON
                d = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.texcoord1.xy).a * input.underlayParam.x;
                c += float4(_UnderlayColor.rgb * _UnderlayColor.a, _UnderlayColor.a) * saturate(d - input.underlayParam.y) * (1 - c.a);
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
                else if (mode == 2 || mode == 3) // hue flow / aurora
                {
                    float hue = frac(p.x * _Tiling + _HueShift - t);
                    float3 hueColor = HsvToRgb(float3(hue, _Saturation, _Value));
                    tint = lerp(float3(1, 1, 1), hueColor, _Intensity);
                    if (mode == 3) // aurora: moving highlight over the hue flow
                    {
                        float pos = frac(p.x * _SweepBands - t * 1.4);
                        float band = saturate((1.0 - abs(pos - 0.5) * 2.0) / _SweepWidth);
                        band = pow(band, _SweepSoftness);
                        tint *= 1.0 + band * _SweepColor.rgb * _Intensity;
                    }
                }
                else if (mode == 4) // hue shift
                {
                    float hue = frac(_HueShift + t);
                    tint = lerp(float3(1, 1, 1), HsvToRgb(float3(hue, _Saturation, _Value)), _Intensity);
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