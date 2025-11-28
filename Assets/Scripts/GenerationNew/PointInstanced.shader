Shader "Custom/PointInstanced"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _PointSize ("Point Size", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            StructuredBuffer<float2> _Positions;
            StructuredBuffer<float4> _Colors;
            StructuredBuffer<float> _Sizes;

            float _PointSize;

            v2f vert (appdata v, uint instanceID : SV_InstanceID)
            {
                UNITY_SETUP_INSTANCE_ID(v);

                v2f o;
                float2 worldPos = _Positions[instanceID];
                float size = _Sizes[instanceID] * _PointSize;

                float4 vertexWorld = float4(worldPos.x + v.vertex.x * size, worldPos.y + v.vertex.y * size, 0, 1);
                o.pos = UnityObjectToClipPos(vertexWorld);
                o.uv = v.uv;
                o.color = _Colors[instanceID];

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Круглая точка
                float dist = length(i.uv - float2(0.5, 0.5));
                clip(0.5 - dist);

                return i.color;
            }
            ENDCG
        }
    }
}