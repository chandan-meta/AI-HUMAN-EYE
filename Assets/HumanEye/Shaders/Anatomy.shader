// Lit anatomy surface for URP: wrap diffuse + specular + environment reflection, fresnel rim,
// cyan highlight, optional projected vessel detail, interior darkening for the closed eye and a uv.y reveal clip.
Shader "HumanEye/Anatomy"
{
    Properties
    {
        _BaseColor("Base Colour", Color) = (1,1,1,1)
        _BaseMap("Base Map", 2D) = "white" {}
        _DetailMap("Projected Detail (vessels)", 2D) = "black" {}
        _DetailColor("Detail Colour", Color) = (0.6,0.05,0.05,1)
        _DetailAxis("Detail Axis (object space)", Vector) = (0,0,-1,0)
        _DetailSpan("Detail Span (radians)", Float) = 1.6
        _DetailStrength("Detail Strength", Range(0,1)) = 0
        _Alpha("Alpha", Range(0,1)) = 1
        _Highlight("Highlight", Range(0,1)) = 0
        _HighlightTint("Highlight Body Tint", Range(0,1)) = 0.1
        _F0("Reflectance At Normal Incidence", Range(0,1)) = 0.3
        _HighlightColor("Highlight Colour", Color) = (0.15,0.85,1,1)
        _Gloss("Gloss", Range(0,1)) = 0.4
        _Specular("Specular", Range(0,1)) = 0.3
        _Wrap("Wrap Lighting", Range(0,1)) = 0.4
        _Rim("Rim", Range(0,2)) = 0.15
        _RimColor("Rim Colour", Color) = (0.55,0.8,1,1)
        _Emission("Emission", Color) = (0,0,0,0)
        _InteriorDark("Darken When Eye Closed", Float) = 0
        _Reveal("Reveal (uv.y)", Range(0,1)) = 1
        _Flow("Flow Stripes", Float) = 0
        [HideInInspector] _SrcBlend("Src Blend", Float) = 1
        [HideInInspector] _DstBlend("Dst Blend", Float) = 0
        [HideInInspector] _ZWrite("Z Write", Float) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor, _BaseMap_ST, _DetailColor, _DetailAxis, _HighlightColor, _RimColor, _Emission;
            float _DetailSpan, _DetailStrength, _Alpha, _Highlight, _HighlightTint, _F0, _Gloss, _Specular, _Wrap, _Rim, _InteriorDark, _Reveal, _Flow;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_DetailMap); SAMPLER(sampler_DetailMap);
            float _HE_InteriorDark, _HE_VizTime;

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; float2 uv : TEXCOORD2; float3 positionOS : TEXCOORD3; };

            Varyings vert(Attributes i)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(i.positionOS.xyz);
                o.positionCS = p.positionCS; o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.uv = i.uv; o.positionOS = i.positionOS.xyz;
                return o;
            }

            half4 frag(Varyings i, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
            {
                clip(_Reveal - i.uv.y);
                float3 n = normalize(i.normalWS);
                n = IS_FRONT_VFACE(face, n, -n);
                float3 v = GetWorldSpaceNormalizeViewDir(i.positionWS);
                float3 albedo = _BaseColor.rgb * SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv * _BaseMap_ST.xy + _BaseMap_ST.zw).rgb;
                if (_DetailStrength > 0)
                {
                    float3 d = normalize(i.positionOS + 1e-5);
                    float3 ax = normalize(_DetailAxis.xyz);
                    float ang = acos(clamp(dot(d, ax), -1, 1)) / _DetailSpan;
                    float3 t1 = normalize(abs(ax.y) < 0.9 ? cross(ax, float3(0,1,0)) : cross(ax, float3(1,0,0)));
                    float3 t2 = cross(ax, t1);
                    float2 dir = normalize(float2(dot(d, t1), dot(d, t2)) + 1e-5);
                    float m = SAMPLE_TEXTURE2D(_DetailMap, sampler_DetailMap, 0.5 + dir * saturate(ang) * 0.5).r * step(ang, 1);
                    albedo = lerp(albedo, _DetailColor.rgb, m * _DetailStrength);
                }
                if (_Flow > 0)
                    albedo *= 0.8 + 0.35 * smoothstep(0.6, 1.0, sin(i.uv.x * 6.2831 * 24) * 0.5 + 0.5);
                Light light = GetMainLight();
                float ndl = dot(n, light.direction);
                float diffuse = saturate((ndl + _Wrap) / (1 + _Wrap));
                float3 h = normalize(light.direction + v);
                float spec = pow(saturate(dot(n, h)), lerp(8, 256, _Gloss)) * _Specular;
                float fres = pow(1 - saturate(dot(n, v)), 3);
                float3 env = GlossyEnvironmentReflection(reflect(-v, n), 1 - _Gloss, 1) * _Specular;
                float3 col = albedo * (light.color * diffuse + SampleSH(n)) + light.color * spec + env * (_F0 + (1 - _F0) * fres)
                           + _RimColor.rgb * fres * _Rim + _Emission.rgb;
                #ifdef _ADDITIONAL_LIGHTS
                for (uint j = 0; j < GetAdditionalLightsCount(); j++)
                {
                    Light fill = GetAdditionalLight(j, i.positionWS);
                    float d = saturate((dot(n, fill.direction) + _Wrap) / (1 + _Wrap));
                    float s = pow(saturate(dot(n, normalize(fill.direction + v))), lerp(8, 256, _Gloss)) * _Specular;
                    col += (albedo * d + s) * fill.color * fill.distanceAttenuation * fill.shadowAttenuation;
                }
                #endif
                col *= lerp(1, 0.05, _InteriorDark * _HE_InteriorDark);
                // Selection reads as a restrained cyan rim, not a tint over the structure.
                float glow = _Highlight * lerp(pow(fres, 0.7), 1, _HighlightTint) * (0.85 + 0.15 * sin(_HE_VizTime * 4));
                col += _HighlightColor.rgb * glow * .7;
                float a = _BaseColor.a * _Alpha;
                if (a < 0.999) a = saturate(a + fres * 0.25 * a + _Highlight * fres * .06);
                return half4(col, a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask R
            Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            float4 vert(float4 positionOS : POSITION) : SV_POSITION { return TransformObjectToHClip(positionOS.xyz); }
            half frag() : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
