// Lit anatomy surface for URP: wrap diffuse + specular + environment reflection, fresnel rim,
// cyan highlight, optional projected vessel detail, interior darkening for the closed eye and a uv.y reveal clip.
//
// Lighting hierarchy (cinematic pass, 7 October 2026). The anatomy is lit by, in order of importance:
//   1 key      — "Eye Key" / "Face Key" spots from camera-left-above: form and the corneal catchlight (soft shadows);
//   2 fill     — "Eye Fill" point from camera-right: lifts the shadow side so no structure goes black (no shadows);
//   3 rim      — "Eye Rim" / "Brain Rim" cool spots from behind: separation from the hall (no shadows);
//   4 practical— the hall's own Key Light (directional, soft shadows) and Display Light: the room, not the subject;
//   5 accent   — "Interior Light" inside the eye for the reveal, the signal's head light, the selection rim (_Highlight).
// For that to hold the shader must see every light: additional lights are summed with URP's cluster loop (Forward+ on the
// PC renderer; the plain loop on the Mobile/Forward renderer), the main light and the additional lights carry their
// shadows (_ReceiveShadows = 0 opts a material out, e.g. the face if self-shadowing looks harsh), a ShadowCaster pass lets
// solid anatomy throw shadows (faded, revealing and clear structures stop casting) and a DepthNormals pass lets the
// renderer's SSAO see anatomy. The pupil remap (Dilate) runs in every pass so shadows and depth match the drawn geometry.
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
        _Cutoff("Alpha Clip (0 = off)", Range(0,1)) = 0
        _RevealUV("Reveal Clips By uv.y", Float) = 1
        _RevealFrom("Visible From uv.y (hidden inside a modelled stub)", Float) = 0
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Strength (0 = off)", Float) = 0
        _SpecMap("Specular Mask (r)", 2D) = "white" {}
        _Wet("Wet Film (sharp coat)", Range(0,1)) = 0
        [Toggle] _PhysicalEye("Physical Dielectric Surface", Float) = 0
        _IOR("Dielectric Index Of Refraction", Range(1,2)) = 1.376
        _CoatSmoothness("Wet Film Smoothness", Range(0,1)) = 0.96
        _SmoothnessMap("Smoothness Map (r)", 2D) = "white" {}
        _SmoothnessStrength("Smoothness Map Strength", Range(0,1)) = 0
        _OcclusionMap("Ambient Occlusion (r)", 2D) = "white" {}
        _OcclusionStrength("Ambient Occlusion Strength", Range(0,1)) = 0
        _IrisRoot("Iris Root Radius (0 = no pupil remap)", Float) = 0
        _PupilBase("Modelled Pupil Radius", Float) = 0.18
        _PupilNow("Current Pupil Radius", Float) = 0.18
        _Glass("Clear Medium (highlights ignore alpha)", Float) = 0
        _ReceiveShadows("Receive Shadows (0 = off)", Float) = 1
        _Wipe("Wipe (x front world y, y alpha above, z alpha below, w on)", Vector) = (0,1,1,0)
        _WipeEdge("Wipe Edge Width (m)", Float) = 0.02
        _WipeColor("Wipe Edge Colour", Color) = (1,0.93,0.8,1)
        _WipeFloor("Wipe Floor (world y; always cut below while wiping)", Float) = -1000
        _BodyFade("Body Fade (x shoulder world y, y width, z strength)", Vector) = (0,0,0,0)
        [Toggle] _AboveBodyFade("Surface entirely above body fade (keep opaque depth)", Float) = 0
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
            float _DetailSpan, _DetailStrength, _Alpha, _Highlight, _HighlightTint, _F0, _Gloss, _Specular, _Wrap, _Rim, _InteriorDark, _Reveal, _Flow, _Cutoff;
            float4 _BumpMap_ST;
            float _RevealUV, _RevealFrom, _BumpScale, _Wet, _IrisRoot, _PupilBase, _PupilNow, _Glass, _ReceiveShadows;
            float _PhysicalEye, _IOR, _CoatSmoothness, _SmoothnessStrength, _OcclusionStrength;
            float4 _Wipe, _WipeColor, _BodyFade; float _WipeEdge, _WipeFloor;
        CBUFFER_END
        #include "AnatomyDissolve.hlsl"
        // The iris annulus keeps its root and slides its inner edge: the pupil opening dilates as real geometry.
        float3 Dilate(float3 p)
        {
            float r = length(p.xy);
            if (_IrisRoot > 0 && r > 1e-4 && r < _IrisRoot)
                p.xy *= (_IrisRoot - (_IrisRoot - r) * (_IrisRoot - _PupilNow) / max(1e-4, _IrisRoot - _PupilBase)) / r;
            return p;
        }
        // The uv.y reveal (nerve growth, the part hidden inside a modelled stub): identical in colour, shadow and normals passes.
        void RevealClip(float2 uv)
        {
            clip(_Reveal - uv.y * _RevealUV);
            if (_RevealUV > 0) clip(uv.y - _RevealFrom);
        }
        // Layer wipe (the reference's skin -> muscle -> bone transitions): a front at world height _Wipe.x; the layer keeps
        // alpha _Wipe.y above it and _Wipe.z below it (0 = cut away), never below _WipeFloor. Same test in every pass.
        // With the breakup field on (_HE_Dissolve.x > 0, AnatomyDissolve.hlsl) the front is compared against the seeded
        // noisy height instead of the plain height: the layer erodes in scattered islands, co-registered across layers.
        float WipeAlpha(float3 positionWS)
        {
            float h = _HE_Dissolve.x > 0 ? HE_DissolveField(positionWS) : positionWS.y;
            float wipe = _Wipe.w > 0.5 ? (positionWS.y < _WipeFloor ? 0 : h > _Wipe.x ? _Wipe.y : _Wipe.z) : 1;
            float fade = smoothstep(_BodyFade.x - max(_BodyFade.y, .001), _BodyFade.x, positionWS.y);
            return wipe * lerp(1, fade, saturate(_BodyFade.z));
        }
        // Depth, shadow and SSAO only where the layer is solid.
        void WipeClip(float3 positionWS) { clip(WipeAlpha(positionWS) - 0.5); }
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
            // URP keywords (names from the installed package: Shaders/Lit.shader, ShaderLibrary/RealtimeLights.hlsl, Shadows.hlsl).
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            // Local reflection probes (box projection, blending, the Forward+ probe atlas): the lab's baked probes give the
            // cornea and the wet film real room reflections instead of only the sky cubemap.
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_DetailMap); SAMPLER(sampler_DetailMap);
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
            TEXTURE2D(_SpecMap); SAMPLER(sampler_SpecMap);
            TEXTURE2D(_SmoothnessMap); SAMPLER(sampler_SmoothnessMap);
            TEXTURE2D(_OcclusionMap); SAMPLER(sampler_OcclusionMap);
            float _HE_InteriorDark, _HE_VizTime;

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float4 tangentOS : TANGENT; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; float2 uv : TEXCOORD2; float3 positionOS : TEXCOORD3; float4 tangentWS : TEXCOORD4; };

            // One additional light: the same wrap diffuse and specular as the main light, attenuated by distance, cone,
            // its own shadow (optional per material) and screen-space AO on direct light.
            void Accumulate(Light fill, float3 n, float3 film, float3 v, float3 albedo, float specMask, float aoDirect,
                BRDFData tissue, BRDFData coat, inout float3 col, inout float3 surface)
            {
                float atten = fill.distanceAttenuation * lerp(1, fill.shadowAttenuation, _ReceiveShadows) * aoDirect;
                float d = saturate((dot(n, fill.direction) + _Wrap) / (1 + _Wrap));
                if (_PhysicalEye > 0.5)
                {
                    float ndl = saturate(dot(n, fill.direction));
                    float coatFresnel = .04 + .96 * pow(1 - saturate(dot(film, v)), 5);
                    float transmission = 1 - _Wet * coatFresnel;
                    col += tissue.diffuse * d * fill.color * atten * transmission;
                    surface += DirectBRDFSpecular(tissue, n, fill.direction, v) * tissue.specular * ndl * fill.color * atten * _Specular * specMask * transmission;
                    surface += DirectBRDFSpecular(coat, film, fill.direction, v) * coat.specular * saturate(dot(film, fill.direction)) * fill.color * atten * _Wet;
                    return;
                }
                float s = pow(saturate(dot(n, normalize(fill.direction + v))), lerp(8, 256, _Gloss)) * _Specular * specMask;
                col += albedo * d * fill.color * atten;
                surface += s * fill.color * atten;
            }

            Varyings vert(Attributes i)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(Dilate(i.positionOS.xyz));
                o.positionCS = p.positionCS; o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.tangentWS = float4(TransformObjectToWorldDir(i.tangentOS.xyz), i.tangentOS.w * GetOddNegativeScale());
                o.uv = i.uv; o.positionOS = i.positionOS.xyz;
                return o;
            }

            half4 frag(Varyings i, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
            {
                RevealClip(i.uv);
                float3 n = normalize(i.normalWS);
                float flip = IS_FRONT_VFACE(face, 1, -1);
                float3 film = n * flip;   // the tear film / capsule stays smooth over bumpy tissue
                float2 uvMain = i.uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
                if (_BumpScale > 0)
                {
                    float3 t = normalize(i.tangentWS.xyz), b = cross(n, t) * i.tangentWS.w;
                    float3 nt = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvMain), _BumpScale);
                    n = normalize(nt.x * t + nt.y * b + nt.z * n);
                }
                n *= flip;
                float3 v = GetWorldSpaceNormalizeViewDir(i.positionWS);
                float4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvMain);
                if (_Cutoff > 0) clip(tex.a - _Cutoff);
                float3 albedo = _BaseColor.rgb * tex.rgb;
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
                // URP's input block, by this exact name: the cluster light loop (LIGHT_LOOP_BEGIN under _CLUSTER_LIGHT_LOOP)
                // reads inputData.normalizedScreenSpaceUV and inputData.positionWS, SSAO reads the screen uv.
                InputData inputData = (InputData)0;
                inputData.positionWS = i.positionWS;
                inputData.normalWS = n;
                inputData.viewDirectionWS = v;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                inputData.shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                half4 shadowMask = half4(1, 1, 1, 1);   // no lightmaps or occlusion probes in the observatory
                // Screen-space AO only on solid pixels: a faded layer sits over some other surface's occlusion.
                float opaque = step(0.995, _BaseColor.a * _Alpha);
                float aoIndirect = 1, aoDirect = 1;
                #if defined(_SCREEN_SPACE_OCCLUSION)
                    AmbientOcclusionFactor ao = GetScreenSpaceAmbientOcclusion(inputData.normalizedScreenSpaceUV);
                    aoIndirect = lerp(1, ao.indirectAmbientOcclusion, opaque);
                    aoDirect = lerp(1, ao.directAmbientOcclusion, opaque);
                #endif
                BRDFData tissue = (BRDFData)0, coat = (BRDFData)0;
                float smoothness = _Gloss;
                if (_PhysicalEye > 0.5)
                {
                    smoothness = saturate(_Gloss * lerp(1, SAMPLE_TEXTURE2D(_SmoothnessMap, sampler_SmoothnessMap, uvMain).r, _SmoothnessStrength));
                    float f0 = pow((_IOR - 1) / max(_IOR + 1, 1e-3), 2);
                    half alpha = 1;
                    InitializeBRDFDataDirect(albedo, albedo * (1 - f0), f0.xxx, f0, 1 - f0, smoothness, alpha, tissue);
                    InitializeBRDFDataDirect(0, 0, .04, .04, .96, _CoatSmoothness, alpha, coat);
                    aoIndirect *= lerp(1, SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, uvMain).r, _OcclusionStrength);
                }
                Light light = GetMainLight(inputData.shadowCoord, i.positionWS, shadowMask);   // realtime shadow with the distance fade
                float shadow = lerp(1, light.shadowAttenuation, _ReceiveShadows) * aoDirect;
                float ndl = dot(n, light.direction);
                float diffuse = saturate((ndl + _Wrap) / (1 + _Wrap));
                float3 h = normalize(light.direction + v);
                float specMask = SAMPLE_TEXTURE2D(_SpecMap, sampler_SpecMap, uvMain).r;
                float spec = pow(saturate(dot(n, h)), lerp(8, 256, _Gloss)) * _Specular * specMask;
                float fres = pow(1 - saturate(dot(n, v)), 3);
                // Body (lit albedo) and surface (highlights, reflections, rim) are kept apart so a clear medium can show
                // its highlights at full strength while its body stays nearly invisible.
                float3 col, surface;
                if (_PhysicalEye > 0.5)
                {
                    float ff = pow(1 - saturate(dot(film, v)), 5);
                    float transmission = 1 - _Wet * (.04 + .96 * ff);
                    col = tissue.diffuse * SampleSH(n) * aoIndirect * transmission + _Emission.rgb;
                    surface = GlossyEnvironmentReflection(reflect(-v, n), i.positionWS, 1 - smoothness, 1, inputData.normalizedScreenSpaceUV)
                        * EnvironmentBRDFSpecular(tissue, pow(1 - saturate(dot(n, v)), 5)) * _Specular * specMask * aoIndirect * transmission;
                    surface += GlossyEnvironmentReflection(reflect(-v, film), i.positionWS, 1 - _CoatSmoothness, 1, inputData.normalizedScreenSpaceUV)
                        * EnvironmentBRDFSpecular(coat, ff) * _Wet * aoIndirect;
                    Accumulate(light, n, film, v, albedo, specMask, aoDirect, tissue, coat, col, surface);
                }
                else
                {
                    float3 env = GlossyEnvironmentReflection(reflect(-v, n), i.positionWS, 1 - _Gloss, 1, inputData.normalizedScreenSpaceUV) * _Specular * aoIndirect;
                    col = albedo * (light.color * diffuse * shadow + SampleSH(n) * aoIndirect) + _Emission.rgb;
                    surface = light.color * spec * shadow + env * (_F0 + (1 - _F0) * fres) + _RimColor.rgb * fres * _Rim;
                    if (_Wet > 0)
                    {
                        float ff = pow(1 - saturate(dot(film, v)), 5);
                        surface += light.color * pow(saturate(dot(film, h)), 900) * _Wet * 3 * shadow
                                 + GlossyEnvironmentReflection(reflect(-v, film), i.positionWS, 0.03, 1, inputData.normalizedScreenSpaceUV) * _Wet * (0.03 + 0.97 * ff);
                    }
                }
                #ifdef _ADDITIONAL_LIGHTS
                uint pixelLightCount = GetAdditionalLightsCount();   // 0 under the cluster loop, which walks its own list
                #if USE_CLUSTER_LIGHT_LOOP
                // Forward+ keeps additional directional lights outside the clusters (as in Lighting.hlsl).
                [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                {
                    Light fill = GetAdditionalLight(lightIndex, i.positionWS, shadowMask);
                    Accumulate(fill, n, film, v, albedo, specMask, aoDirect, tissue, coat, col, surface);
                }
                #endif
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light fill = GetAdditionalLight(lightIndex, i.positionWS, shadowMask);
                    Accumulate(fill, n, film, v, albedo, specMask, aoDirect, tissue, coat, col, surface);
                LIGHT_LOOP_END
                #endif
                // Clear cornea and lens transmit the anatomy behind them; only their surface reflects light.
                if (_PhysicalEye > .5 && _Glass > .5) col = _Emission.rgb;
                float dark = lerp(1, 0.05, _InteriorDark * _HE_InteriorDark);
                col *= dark; surface *= dark;
                // Hover and the idle invitation read as a restrained cyan rim. A selected part (_Highlight above one half;
                // 22 October 2026 brief: the picked part must read at a glance) adds a stronger rim, a soft body tint, a slow
                // pulse and thin scan lines sweeping up the structure.
                float sel = saturate(_Highlight * 2 - 1);
                float pulse = 0.85 + 0.15 * sin(_HE_VizTime * 4) + sel * 0.2 * sin(_HE_VizTime * 2.4);
                float glow = _Highlight * lerp(pow(fres, 0.7), 1, _HighlightTint + 0.22 * sel) * pulse;
                float scan = sel * smoothstep(0.82, 1, frac(i.positionWS.y * 40 - _HE_VizTime * 0.8)) * 0.4;
                col += _HighlightColor.rgb * (glow * (0.7 + 0.7 * sel) + scan);
                float wipe = WipeAlpha(i.positionWS);
                clip(wipe - 0.001);
                float edge = 0;
                if (_Wipe.w > 0.5 && _HE_Dissolve.x > 0)
                {
                    // Electric breakup: the breaking edge itself, the crack network (cell borders) and thin contour filaments
                    // scribbled around it, all flickering, strongest where geometry is about to vanish; a faint rim on the
                    // fragments. The same field on both layers makes the cyan lines continuous across the hand-over.
                    float2 cells; float d = HE_DissolveField(i.positionWS, cells) - _Wipe.x;
                    float reach = max(_HE_Dissolve.w, 1e-3), near = exp(-abs(d) / reach);
                    float flicker = .75 + .25 * sin(_HE_VizTime * 41 + HE_Hash3(floor(i.positionWS * 90)).x * 6.2832);
                    float boundary = saturate(1 - abs(d) / max(_WipeEdge, 1e-4));
                    float crack = 1 - smoothstep(0, .09, cells.y - cells.x);
                    float vein = smoothstep(.8, 1, 1 - abs(frac(d / (reach * .5)) - .5) * 2);
                    edge = boundary * boundary + (crack * .7 + vein * .15 + fres * .3) * near * flicker;
                    col += _HE_DissolveColor.rgb * _HE_DissolveColor.w * edge;
                }
                else if (_Wipe.w > 0.5)
                {
                    // the older head reveal: a thin warm line rides the flat wipe front on both layers
                    edge = saturate(1 - abs(i.positionWS.y - _Wipe.x) / max(_WipeEdge, 1e-4));
                    col += _WipeColor.rgb * edge * edge * 2.5;
                }
                float a = _BaseColor.a * _Alpha * wipe;
                if (a < 0.999) a = saturate(a + fres * 0.25 * a + _Highlight * fres * .06 + scan * .5 + edge * .5);
                float clearReflection = _PhysicalEye > .5 ? 1 / max(a, .01) : min(1 / max(a, .01), 4);
                col += surface * (_Glass > 0 && a < 0.999 ? clearReflection : 1);   // alpha blending scales it back to full strength
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
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            struct A { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct V { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float2 uv : TEXCOORD1; };
            V vert(A v) { V o; o.positionWS = TransformObjectToWorld(Dilate(v.positionOS.xyz)); o.positionCS = TransformWorldToHClip(o.positionWS); o.uv = v.uv; return o; }
            half frag(V i) : SV_Target { WipeClip(i.positionWS); RevealClip(i.uv); clip(_Alpha - .5); if (_Cutoff > 0) clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, TRANSFORM_TEX(i.uv, _BaseMap)).a - _Cutoff); return 0; }
            ENDHLSL
        }

        // Solid anatomy throws shadows (Key Light, Display Light, Face Key, Eye Key). Faded structures (alpha below .5: a layer
        // isolated away, the X-ray skin, the clear media whose base alpha is .09–.34) and the part of a structure still
        // hidden by its reveal stop casting, so a shadow never betrays something the learner cannot see.
        // Template: Shaders/ShadowCasterPass.hlsl (bias from _LightDirection / _LightPosition, ApplyShadowClamping).
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 positionWS : TEXCOORD1; };

            Varyings vert(Attributes i)
            {
                Varyings o;
                float3 positionWS = TransformObjectToWorld(Dilate(i.positionOS.xyz));
                o.positionWS = positionWS;
                float3 normalWS = TransformObjectToWorldNormal(i.normalOS);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif
                o.positionCS = ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS)));
                o.uv = i.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                RevealClip(i.uv);
                WipeClip(i.positionWS);
                clip(_BaseColor.a * _Alpha - 0.5);
                if (_Cutoff > 0) clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv * _BaseMap_ST.xy + _BaseMap_ST.zw).a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }

        // Depth + world normal for the renderer's SSAO (its source is DepthNormals). Only solid parts reach this prepass:
        // faded parts move to the transparent queue. Template: Shaders/DepthNormalsPass.hlsl.
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite On
            Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float2 uv : TEXCOORD1; float3 positionWS : TEXCOORD2; };

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(Dilate(i.positionOS.xyz));
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.uv = i.uv;
                return o;
            }

            half4 frag(Varyings i, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
            {
                RevealClip(i.uv);
                WipeClip(i.positionWS);
                if (_Cutoff > 0) clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv * _BaseMap_ST.xy + _BaseMap_ST.zw).a - _Cutoff);
                float3 n = normalize(i.normalWS) * IS_FRONT_VFACE(face, 1, -1);   // double-sided cut surfaces face the camera
                return half4(n, 0);
            }
            ENDHLSL
        }
    }
}
