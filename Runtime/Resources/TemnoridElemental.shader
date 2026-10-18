Shader "Temnorid/Elemental Energy"
{
    Properties { _Tint ("Tint", Color) = (0.5,0.8,1,1) _Membrane ("Membrane", Float) = 0 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _Tint;
            float _Membrane;
            CBUFFER_END
            struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; half3 normalWS:TEXCOORD1; float2 uv:TEXCOORD2; UNITY_VERTEX_OUTPUT_STEREO };
            V Vert(A i)
            {
                V o;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionWS = TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.uv = i.uv;
                return o;
            }
            half4 Frag(V i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float facing = saturate(abs(dot(normalize(i.normalWS), GetWorldSpaceNormalizeViewDir(i.positionWS))));
                float rim = pow(1 - facing, 2.5);
                float flow = sin(i.positionWS.y * 18 + sin(i.positionWS.x * 11 + _Time.y * 2) * 2 - _Time.y * 4) * 0.5 + 0.5;
                float ribbonEdge = pow(saturate(sin(i.uv.y * 3.14159)), 0.6);
                float alpha = lerp(ribbonEdge * (0.55 + flow * 0.35), 0.025 + rim * 0.6 + pow(flow, 16) * 0.07, _Membrane);
                return half4(_Tint.rgb * (1.1 + rim * 1.5 + flow * 0.3), _Tint.a * alpha);
            }
            ENDHLSL
        }
    }
}
