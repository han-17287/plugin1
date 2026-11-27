Shader "PLUGIN/Blue Plug/Current"
{
    Properties
    {
        [HDR] _Signal ("Movement signal", Color) = (.360784,.796078,1,1)
        _Energy ("Emission intensity", Range(0,6)) = 1
        _Flow ("Pulse speed and direction", Float) = 1
        _CableLength ("Cable length", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Cull Off ZWrite Off Lighting Off Blend SrcAlpha One, Zero One
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct Input { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct Output { float4 position:SV_POSITION; float2 uv:TEXCOORD0; };
            float4 _Signal;
            float _Energy, _Flow, _CableLength;
            Output vert(Input v)
            {
                Output o;
                o.position = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            half4 frag(Output i):SV_Target
            {
                // 피복 양끝은 고정하고 전류 중심만 미세하게 떤다.
                float tremor = sin(i.uv.x * 81 + _Time.y * 23) * sin(i.uv.x * 37 - _Time.y * 13);
                float edge = abs(i.uv.y * 2 - 1 + .065 * tremor);
                float coverage = 1 - smoothstep(.54, 1, edge);
                float x = i.uv.x * max(1, _CableLength) * .60 - _Time.y * 2.5 * _Flow;
                float pulse = 1 - smoothstep(.03, .18, abs(frac(x) - .5));
                float3 radiance = _Signal.rgb * max(.35, _Energy) * (.52 + .72 * pulse);
                return half4(radiance, coverage * .9);
            }
            ENDCG
        }
    }
    Fallback "Sprites/Default"
}
