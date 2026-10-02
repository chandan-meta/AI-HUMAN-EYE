// Unlit additive glow for light rays, electrical pulses, beams, scan wave and the retinal image.
// Scrolling uses the visualization clock (_HE_VizTime) so FREEZE stops the flow.
Shader "HumanEye/Glow"
{
    Properties
    {
        _Color("Colour", Color) = (1,1,1,1)
        _MainTex("Texture", 2D) = "white" {}
        _Intensity("Intensity", Float) = 1
        _Scroll("Scroll (uv per second)", Vector) = (0,0,0,0)
        _Alpha("Alpha", Range(0,1)) = 1
        _EdgeFade("View Edge Fade", Float) = 0
        _LengthFade("Length Fade (uv.y)", Float) = 0
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
            CBUFFER_START(UnityPerMaterial)
                float4 _Color, _MainTex_ST, _Scroll;
                float _Intensity, _Alpha, _EdgeFade, _LengthFade;
            CBUFFER_END
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            float _HE_VizTime;
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; float facing : TEXCOORD1; float rawV : TEXCOORD2; };
            Varyings vert(Attributes i)
            {
                Varyings o;
                float3 wp = TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(wp);
                o.uv = i.uv * _MainTex_ST.xy + _MainTex_ST.zw + _Scroll.xy * _HE_VizTime;
                o.color = i.color;
                float3 n = TransformObjectToWorldNormal(i.normalOS);
                o.facing = abs(dot(normalize(n), GetWorldSpaceNormalizeViewDir(wp)));
                o.rawV = i.uv.y;
                return o;
            }
            half4 frag(Varyings i) : SV_Target
            {
                half4 t = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                float a = t.a * _Color.a * i.color.a * _Alpha;
                if (_EdgeFade > 0) a *= pow(saturate(i.facing), _EdgeFade);
                if (_LengthFade > 0) a *= saturate(sin(i.rawV * 3.14159)) ;
                return half4(t.rgb * _Color.rgb * i.color.rgb * _Intensity, a);
            }
            ENDHLSL
        }
    }
}
