Shader "Hidden/PLUGIN/Presentation"
{
    Properties
    {
        _MainTex ("Source",2D)="white" {}
        _BloomStrength ("Glow",Range(0,1))=0.32
        _BloomThreshold ("Glow threshold",Range(0,2))=0.82
        _BloomRadius ("Glow radius in pixels",Range(1,6))=2.8
        _DockUVPulse ("Socket glow position",Vector)=(0,0,0,1)
        _DamageAmount ("Damage",Float)=0
        _LowHealth ("Low health",Float)=0
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize, _DockUVPulse;
            float _BloomStrength, _BloomThreshold, _BloomRadius, _DamageAmount, _LowHealth;

            half3 glow(float2 uv)
            {
                half3 rgb=tex2D(_MainTex, uv).rgb;
                // 채널마다 빼면 주황색이 빨강으로 변한다. 가장 밝은 채널로 한 번만 문턱을 계산한다.
                half peak=max(rgb.r,max(rgb.g,rgb.b));
                half knee=smoothstep(_BloomThreshold-.08, _BloomThreshold+.12, peak);
                return rgb*knee;
            }
            half4 frag(v2f_img i):SV_Target
            {
                half4 source=tex2D(_MainTex,i.uv);
                half3 light=0;
                if(_BloomStrength>.001)
                {
                    float2 d=_MainTex_TexelSize.xy*_BloomRadius;
                    light=(glow(i.uv+float2(d.x,0))+glow(i.uv-float2(d.x,0))
                          +glow(i.uv+float2(0,d.y))+glow(i.uv-float2(0,d.y)))*.17;
                    d*=1.65;
                    light+=(glow(i.uv+d)+glow(i.uv-d)
                           +glow(i.uv+float2(d.x,-d.y))+glow(i.uv+float2(-d.x,d.y)))*.08;
                }
                float2 p=i.uv*2-1;
                float edgeDist=dot(p,p);
                half3 graded=((source.rgb-.5)*1.035+.5)*half3(.985,1.01,1.025);
                float2 viewUV=i.uv;
                #if UNITY_UV_STARTS_AT_TOP
                    if(_MainTex_TexelSize.y<0) viewUV.y=1-viewUV.y;
                #endif
                float2 offset=(viewUV-_DockUVPulse.xy)*float2(_DockUVPulse.w,1);
                half localGlow=saturate(1-dot(offset,offset)*100)*_DockUVPulse.z;
                half3 outputColor=saturate((graded+light*(_BloomStrength+localGlow))*(1-edgeDist*.045));
                outputColor=lerp(outputColor,half3(.70,.15,.04),saturate(edgeDist*.6)*_DamageAmount*.32);
                half low=smoothstep(.4,1.6,edgeDist)*_LowHealth;
                outputColor=lerp(outputColor,dot(outputColor,half3(.21,.72,.07)).xxx,low*.24)*(1-low*.18);
                return half4(outputColor,source.a);
            }
            ENDCG
        }
    }
    Fallback Off
}
