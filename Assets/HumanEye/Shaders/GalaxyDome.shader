// The lab's planetarium ceiling (visual-match pass, 8 October 2026): a galaxy projected on a shell just outside the new
// lab's roof, seen through the roof glass and the oculus while the side windows keep the daylight sky. The art boards show
// both at once: a daytime lab under a starfield ceiling, so this is an interior display, never the real sky. The shell is
// cut below _ClipY (the ceiling ring), so no sight line through a side window can reach it. Unlit, opaque, inside-out.
Shader "HumanEye/GalaxyDome"
{
    Properties
    {
        _MainTex("Night sky (equirectangular)", 2D) = "black" {}
        _Base("Base glow (deep blue)", Color) = (0.03, 0.09, 0.3, 1)
        _Tint("Star tint", Color) = (0.7, 0.85, 1, 1)
        _Intensity("Star intensity (high-pass of the sky)", Float) = 3.6
        _Nebula("Nebula glow (blurred sky)", Float) = 0.55
        _NebulaTint("Nebula tint", Color) = (0.25, 0.5, 1, 1)
        _Tilt("Tilt (degrees: keeps the texture's pole off the zenith, brings the Milky Way overhead)", Float) = 70
        _Rotation("Rotation (degrees)", Float) = 0
        _Spin("Spin (degrees per second)", Float) = 0.35
        _ClipY("Hidden below world height", Float) = 10.5
        _Fade("Fade-in height above the cut (m)", Float) = 1.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+50" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Pass
        {
            Name "Galaxy"
            Tags { "LightMode"="UniversalForward" }
            Cull Front
            ZWrite On
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Base, _Tint, _NebulaTint;
                float _Intensity, _Nebula, _Tilt, _Rotation, _Spin, _ClipY, _Fade;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; float3 positionWS : TEXCOORD1; };

            Varyings vert(Attributes a)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(a.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.dir = a.positionOS.xyz;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                clip(i.positionWS.y - _ClipY);
                float3 d = normalize(i.dir);
                float r = radians(_Rotation + _Spin * _Time.y), s = sin(r), c = cos(r);
                d.xz = float2(c * d.x - s * d.z, s * d.x + c * d.z);
                float t = radians(_Tilt), st = sin(t), ct = cos(t);
                d.yz = float2(ct * d.y - st * d.z, st * d.y + ct * d.z);
                // Unity's panoramic layout (same as Skybox/Panoramic, so the owner's texture reads as authored)
                float2 uv = float2(0.5 - atan2(d.z, d.x) / (2 * PI), 1 - acos(clamp(d.y, -1, 1)) / PI);
                half3 sky = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, uv, 0).rgb;        // lod 0: no mip spike at the seam
                half3 glow = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, uv, 4).rgb;       // the Milky Way as a soft glow
                half3 stars = max(sky - glow * 1.15, 0);                                       // point stars only: the sky minus its blur
                half fade = saturate((i.positionWS.y - _ClipY) / max(_Fade, 1e-3));
                half3 col = _Base.rgb + glow * _NebulaTint.rgb * _Nebula + stars * _Tint.rgb * _Intensity;
                return half4(col * fade, 1);
            }
            ENDHLSL
        }
        // Depth and depth-normals passes: the PC renderer primes depth (SSAO), and an opaque surface missing from the prepass
        // would fail the equal-depth test and never draw.
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            Cull Front ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Base, _Tint, _NebulaTint;
                float _Intensity, _Nebula, _Tilt, _Rotation, _Spin, _ClipY, _Fade;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; };
            Varyings vert(Attributes a) { Varyings o; o.positionWS = TransformObjectToWorld(a.positionOS.xyz); o.positionCS = TransformWorldToHClip(o.positionWS); return o; }
            half frag(Varyings i) : SV_Target { clip(i.positionWS.y - _ClipY); return 0; }
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            Cull Front ZWrite On
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Base, _Tint, _NebulaTint;
                float _Intensity, _Nebula, _Tilt, _Rotation, _Spin, _ClipY, _Fade;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; };
            Varyings vert(Attributes a)
            {
                Varyings o; o.positionWS = TransformObjectToWorld(a.positionOS.xyz); o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = -TransformObjectToWorldNormal(a.normalOS);   // seen from inside
                return o;
            }
            half4 frag(Varyings i) : SV_Target
            {
                clip(i.positionWS.y - _ClipY);
                float3 n = normalize(i.normalWS);
                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 oct = saturate(PackNormalOctQuadEncode(n) * 0.5 + 0.5);
                    return half4(PackFloat2To888(oct), 0);
                #else
                    return half4(n, 0);
                #endif
            }
            ENDHLSL
        }
    }
}
