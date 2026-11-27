Shader "PLUGIN/Traversal/BluePlugSilhouette"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Alpha", 2D) = "white" {}
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
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment SilhouetteFrag
            #pragma multi_compile_instancing
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            fixed4 SilhouetteFrag(v2f input) : SV_Target
            {
                // 원화의 붉은 스카프·흰 제복 색은 버리고 알파와 이동 신호색만 사용한다.
                fixed alpha = SampleSpriteTexture(input.texcoord).a;
                return fixed4(input.color.rgb, alpha * input.color.a);
            }
            ENDCG
        }
    }
    Fallback "Sprites/Default"
}
