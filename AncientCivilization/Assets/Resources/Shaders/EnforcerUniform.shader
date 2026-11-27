Shader "PLUGIN/EnforcerUniform"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "DisableBatching"="True" }
        Cull Off Lighting Off ZWrite Off Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex WardrobeVertex
            #pragma fragment WardrobeFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            float4 _EnforcerBend, _EnforcerHead;
            float4x4 _EnforcerToActor, _EnforcerToWorld;
            struct varyings
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 face : TEXCOORD1;
                fixed4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            varyings WardrobeVertex(appdata_t v)
            {
                varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float4 world = mul(unity_ObjectToWorld, UnityFlipSprite(v.vertex.xyz, _Flip));
                float4 p = mul(_EnforcerToActor, world);
                float shoulder = smoothstep(.52,1.65,p.y);
                float skirt = smoothstep(.10,.44,p.y) * (1-smoothstep(.76,1.35,p.y)) * smoothstep(.14,.43,abs(p.x));
                float hand = shoulder * smoothstep(.29,.87,abs(p.x));
                o.face = (p.xy - _EnforcerHead.xy) * float2(_EnforcerHead.z,1);
                p.x += _EnforcerHead.z * (_EnforcerBend.x * shoulder + _EnforcerBend.z * skirt);
                p.y += _EnforcerBend.y * shoulder + _EnforcerBend.w * hand;
                o.vertex = mul(UNITY_MATRIX_VP, mul(_EnforcerToWorld,p));
                o.uv = v.texcoord;
                o.color = v.color * _Color * _RendererColor;
                return o;
            }

            fixed4 WardrobeFragment(varyings i) : SV_Target
            {
                fixed4 ink = SampleSpriteTexture(i.uv);
                #ifndef UNITY_COLORSPACE_GAMMA
                    ink.rgb = LinearToGammaSpace(ink.rgb);
                #endif
                // 렌즈 위로 빗어 넘긴 머리는 잉크색, 관자놀이의 좁은 금속 결만 밝게 살린다.
                float2 h = i.face;
                float hair = smoothstep(-.075,.048,h.y - .52*h.x) * (1-smoothstep(.17,.23,h.y))
                           * smoothstep(-.37,-.30,h.x) * (1-smoothstep(-.012,.064,h.x));
                float neutral = 1-smoothstep(.065,.22,max(ink.r,max(ink.g,ink.b))-min(ink.r,min(ink.g,ink.b)));
                float luma = dot(ink.rgb, float3(.2126,.7152,.0722));
                float3 blueBlack = lerp(float3(.055,.095,.135),float3(.31,.43,.49),luma*luma);
                float seam = abs(h.y - .52*h.x - .07);
                float silver = 1-smoothstep(.012,.026,seam);
                ink.rgb = lerp(ink.rgb, lerp(blueBlack,ink.rgb,silver),hair*neutral*.95);
                // 청록 제복 위의 기록 인증 모듈. 가로 홈 둘과 작은 호박색 봉인으로 가문 장식을 읽힌다.
                // 가슴을 가린 흰 장갑과 투영 서류에는 배지가 나타나지 않는다.
                float cloth = smoothstep(.015,.08,ink.g-ink.r) * (1-smoothstep(.31,.58,luma));
                float2 badge = h-float2(-.078,-.376);
                badge.x += .12*badge.y;
                float edge = max(max(abs(badge.x)-.076,abs(badge.y)-.055),
                                 abs(badge.x)+abs(badge.y)-.109);
                float cover = (1-smoothstep(-.001,.004,edge))*cloth;
                float3 plate = lerp(float3(.08,.13,.16),float3(.54,.65,.65),
                                    smoothstep(-.011,-.004,edge));
                float notch = abs(badge.y+.008+.25*badge.x);
                float chevron = (1-smoothstep(.005,.008,min(notch,abs(badge.y-.016+.25*badge.x)))) *
                                (1-smoothstep(.039,.045,abs(badge.x)));
                plate = lerp(plate,float3(.77,.85,.83),chevron);
                float seal = 1-smoothstep(.008,.014,length(badge-float2(.049,-.029)));
                plate = lerp(plate,float3(1,.67,.26),seal);
                ink.rgb = lerp(ink.rgb,plate,cover);
                #ifndef UNITY_COLORSPACE_GAMMA
                    ink.rgb = GammaToLinearSpace(ink.rgb);
                #endif
                ink *= i.color;
                ink.rgb *= ink.a;
                return ink;
            }
            ENDCG
        }
    }
    Fallback Off
}
