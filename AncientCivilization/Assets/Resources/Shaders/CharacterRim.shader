Shader "PLUGIN/CharacterRim"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _RimColor ("Rim", Color) = (0.15,0.65,0.8,0.5)
        _ArmCharge ("Arm charge", Range(0,1)) = 0
        _ArmPulse ("Arm activity", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off Lighting Off Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            sampler2D _MainTex;float4 _MainTex_TexelSize;fixed4 _RimColor;float _ArmCharge,_ArmPulse;
            v2f vert(appdata v) { v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o; }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 c=tex2D(_MainTex,i.uv)*i.color;
                // React only to cyan reactor/channels; ivory armor, gold trims and clothing retain their palette.
                float reactor=saturate((min(c.g,c.b)-c.r-.12)*4)*step(.25,c.g)*step(.5,c.a);
                float activity=_ArmCharge*.35+_ArmPulse*(.25+.25*sin(_Time.y*18));
                c.rgb+=reactor*activity*fixed3(.22,.55,.6);
                float2 d=_MainTex_TexelSize.xy*2.0;
                float neighbour=max(max(tex2D(_MainTex,i.uv+float2(d.x,0)).a,tex2D(_MainTex,i.uv-float2(d.x,0)).a),
                                    max(tex2D(_MainTex,i.uv+float2(0,d.y)).a,tex2D(_MainTex,i.uv-float2(0,d.y)).a));
                float rim=saturate(neighbour-c.a)*_RimColor.a*i.color.a*(1-c.a);
                return fixed4(c.rgb*c.a+_RimColor.rgb*rim,c.a+rim);
            }
            ENDCG
        }
    }
    Fallback "Sprites/Default"
}
