Shader "PLUGIN/AncientEnergyClone"
{
 Properties { [PerRendererData] _MainTex ("Sprite",2D)="white" {} [HideInInspector] _Flip ("Flip",Vector)=(1,1,1,1) }
 SubShader
 {
  Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
  Cull Off Lighting Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   sampler2D _MainTex;float4 _Flip;
   struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;};
   struct v2f {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;float2 local:TEXCOORD1;};
   v2f vert(appdata v){v2f o;v.vertex.xy*=_Flip.xy;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.local=v.vertex.xy;return o;}
   fixed4 frag(v2f i):SV_Target
   {
    fixed4 src=tex2D(_MainTex,i.uv);clip(src.a-.4);
    float scan=.5+.5*sin(i.local.y*52-_Time.y*8);
    float luma=dot(src.rgb,float3(.2,.6,.2));
    return fixed4(lerp(float3(.015,.16,.65),float3(.18,.88,1),luma*.65+scan*.35),.82);
   }
   ENDCG
  }
 }
}
