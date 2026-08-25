Shader "BoozeBlocks/BlobShadow"
{
    Properties
    {
        _Color ("Color", Color) = (0, 0, 0, 0.55)
        _InnerRadius ("Inner radius", Range(0, 0.5)) = 0.18
        _OuterRadius ("Outer radius", Range(0, 0.5)) = 0.48
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            Lighting Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _InnerRadius;
            float _OuterRadius;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float d = distance(i.uv, float2(0.5, 0.5));
                float mask = 1.0 - smoothstep(_InnerRadius, _OuterRadius, d);
                fixed4 col = _Color;
                col.a *= mask;
                return col;
            }
            ENDCG
        }
    }

    Fallback Off
}
