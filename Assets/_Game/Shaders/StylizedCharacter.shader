Shader "BoozeBlocks/StylizedCharacter"
{
    Properties
    {
        _ShirtColor ("Shirt", Color) = (0.05, 0.48, 0.76, 1)
        _SkinColor ("Skin", Color) = (1.0, 0.67, 0.44, 1)
        _PantsColor ("Pants", Color) = (0.08, 0.14, 0.22, 1)
        _DarkColor ("Hair and shoes", Color) = (0.05, 0.035, 0.025, 1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 150

        CGPROGRAM
        #pragma surface surf Lambert fullforwardshadows
        #pragma target 3.0

        fixed4 _ShirtColor;
        fixed4 _SkinColor;
        fixed4 _PantsColor;
        fixed4 _DarkColor;

        struct Input
        {
            fixed4 color : COLOR;
        };

        void surf(Input input, inout SurfaceOutput output)
        {
            fixed3 mask = saturate(input.color.rgb);
            fixed darkMask = 1.0 - saturate(mask.r + mask.g + mask.b);
            output.Albedo = _ShirtColor.rgb * mask.r
                + _SkinColor.rgb * mask.g
                + _PantsColor.rgb * mask.b
                + _DarkColor.rgb * darkMask;
            output.Alpha = 1.0;
        }
        ENDCG
    }

    Fallback "Diffuse"
}
