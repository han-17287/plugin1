Shader "Hidden/PLUGIN/Presentation"
{
    Properties { _MainTex ("Source",2D)="white" {} _BloomStrength ("Glow",Float)=0.32 }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;float4 _MainTex_TexelSize;float _BloomStrength;
            fixed4 frag(v2f_img i):SV_Target
            {
                fixed4 c=tex2D(_MainTex,i.uv);float2 d=_MainTex_TexelSize.xy*2;
                float3 light=max(tex2D(_MainTex,i.uv+float2(d.x,d.y)).rgb-.65,0)
                           +max(tex2D(_MainTex,i.uv+float2(-d.x,d.y)).rgb-.65,0)
                           +max(tex2D(_MainTex,i.uv+float2(d.x,-d.y)).rgb-.65,0)
                           +max(tex2D(_MainTex,i.uv-float2(d.x,d.y)).rgb-.65,0);
                float2 p=i.uv*2-1;float vignette=saturate(1-dot(p,p)*.055);
                float3 grade=(c.rgb-.5)*1.035+.5;
                grade*=float3(.985,1.01,1.025);
                return fixed4(saturate((grade+light*_BloomStrength*.25)*vignette),c.a);
            }
            ENDCG
        }
    }
    Fallback Off
}
