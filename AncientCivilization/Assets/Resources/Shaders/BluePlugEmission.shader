Shader "PLUGIN/Traversal/BluePlugEmission"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite / Line Texture", 2D) = "white" {}
        [HDR] _EmissionTint ("Emission Tint", Color) = (1,1,1,1)
        _Intensity ("Emission Intensity", Range(0,5)) = 1.65
        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Cull Off
        ZWrite Off
        Lighting Off
        Blend SrcAlpha One, Zero One
        // 태그를 생략한 Unlit 패스는 기본 렌더러와 URP 2D의 SRPDefaultUnlit 경로에서 사용한다.
        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment EmissionFrag
            #pragma multi_compile_instancing
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            half4 _EmissionTint;
            half _Intensity;

            half4 EmissionFrag(v2f input) : SV_Target
            {
                half4 texel = SampleSpriteTexture(input.texcoord);
                half3 light = texel.rgb * input.color.rgb * _EmissionTint.rgb * _Intensity;
                return half4(light, texel.a * input.color.a * _EmissionTint.a);
            }
            ENDCG
        }
    }
    Fallback "Sprites/Default"
}
