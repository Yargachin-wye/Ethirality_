Shader "Custom/SmoothWarp"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _NoiseTex ("Warp Noise (optional)", 2D) = "gray" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Amplitude ("Amplitude", Range(0, 0.2)) = 0.02
        _Frequency ("Frequency", Range(0, 20)) = 4
        _Speed ("Speed", Range(0, 10)) = 1
        _NoiseAmount ("Noise Amount", Range(0, 1)) = 0.5
        _ScrollSpeed ("Scroll Speed", Range(-2, 2)) = 0
        _ScrollDir ("Scroll Direction", Vector) = (1,0,0,0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _NoiseTex_ST;
                float _Amplitude, _Frequency, _Speed, _NoiseAmount, _ScrollSpeed;
                float4 _ScrollDir;
                float4 _Color;
            CBUFFER_END

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = TransformObjectToHClip(v.vertex.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                float t = _Time.y * _Speed;

                // Smooth sine warp
                float2 warp;
                warp.x = sin(i.uv.y * _Frequency * 6.2831 + t) * _Amplitude;
                warp.y = cos(i.uv.x * _Frequency * 6.2831 + t * 1.3) * _Amplitude;

                // Optional noise warp for a more organic look
                float2 n = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, i.uv * _NoiseTex_ST.xy + float2(t * 0.05, -t * 0.03)).rg * 2 - 1;
                warp += n * _Amplitude * _NoiseAmount * 2;

                float2 uv = i.uv + warp + _ScrollDir.xy * (_Time.y * _ScrollSpeed);
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * i.color;
            }
            ENDHLSL
        }
    }
}
