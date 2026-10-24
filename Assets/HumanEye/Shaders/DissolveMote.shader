// Motes of the electric breakup (14 October 2026): the particle system emits from the whole departing surface; this shader
// keeps only motes born where the shared breakup field was crossing the front at their birth, so sparks and shards leave
// the actual breaking patches. Custom1 = (birth position xyz, front at birth), stamped by AnatomyRevealController.
Shader "HumanEye/DissolveMote"
{
    Properties
    {
        _Color("Colour", Color) = (1,1,1,1)
        _MainTex("Texture", 2D) = "white" {}
        _Intensity("Intensity", Float) = 1
        _Band("Visible band (m, field distance from the front at birth)", Float) = 0.05
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "AnatomyDissolve.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _Color, _MainTex_ST;
                float _Intensity, _Band;
            CBUFFER_END
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            float _HE_VizTime;
            // Vertex streams: Position, Color, UV (xy) + Custom1.xy (uv.zw), Custom1.zw (TEXCOORD1).
            struct Attributes { float4 positionOS : POSITION; float4 color : COLOR; float4 uv : TEXCOORD0; float2 stamp : TEXCOORD1; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            Varyings vert(Attributes i)
            {
                Varyings o;
                float3 birth = float3(i.uv.zw, i.stamp.x); float front = i.stamp.y;
                float d = abs(HE_DissolveField(birth) - front);
                bool keep = front != 0 && d < _Band;   // unstamped or born off the breaking band: collapsed behind the near plane
                float3 wp = TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS = keep ? TransformWorldToHClip(wp) : float4(0, 0, -2, 1);
                o.uv = i.uv.xy * _MainTex_ST.xy + _MainTex_ST.zw;
                o.color = i.color;
                o.color.a *= .8 + .2 * sin(_HE_VizTime * 23 + birth.x * 97 + birth.y * 61);   // scintillation
                return o;
            }
            half4 frag(Varyings i) : SV_Target
            {
                half4 t = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                return half4(t.rgb * _Color.rgb * i.color.rgb * _Intensity, t.a * _Color.a * i.color.a);
            }
            ENDHLSL
        }
    }
}
