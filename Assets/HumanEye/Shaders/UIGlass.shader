Shader "HumanEye/UI Glass"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _RimColor ("Rim colour", Color) = (.48,.89,1,1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="False" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha   // alpha = coverage (premultiplied overlay captures; an opaque back buffer stays opaque)
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 p:TEXCOORD0; float2 size:TEXCOORD1; float2 shape:TEXCOORD2; float2 halo:TEXCOORD3; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex:SV_POSITION; float4 color:COLOR; float2 p:TEXCOORD0; float2 size:TEXCOORD1; float2 shape:TEXCOORD2; float2 halo:TEXCOORD3; float4 world:TEXCOORD4; UNITY_VERTEX_OUTPUT_STEREO };
            float4 _ClipRect, _RimColor;
            v2f vert(appdata v)
            {
                v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.world=v.vertex; o.vertex=UnityObjectToClipPos(v.vertex); o.color=v.color;
                o.p=v.p; o.size=v.size; o.shape=v.shape; o.halo=v.halo; return o;
            }
            float4 frag(v2f i):SV_Target
            {
                float2 q=abs(i.p-i.size*.5)-(i.size*.5-i.shape.x);
                float d=length(max(q,0))+min(max(q.x,q.y),0)-i.shape.x;
                float body=1-smoothstep(-.7,.7,d);
                float rim=1-smoothstep(i.shape.y*.45,i.shape.y*.45+1,abs(d+i.shape.y*.65));
                // Calm by default: a hairline border at rest; the cyan rim and halo appear only for hover/focus
                // (glow ~.8) and the selected / required action (glow 1.4). No sheen, a barely-there vertical gradient.
                float g=i.halo.y, active=saturate((g-.3)*1.6);
                float3 rimCol=lerp(float3(.141,.22,.31),_RimColor.rgb,active);
                float h=max(i.halo.x*.38,1);
                float halo=exp(-max(d,0)*max(d,0)/(h*h)) * saturate(d+1) * g*.5*active;
                float y=saturate(i.p.y/max(i.size.y,1));
                float3 fill=i.color.rgb*lerp(.95,1.05,y);
                float edgeGlow=exp(-abs(d)/5)*g*.16*active;
                float3 rgb=lerp(fill+rimCol*edgeGlow,lerp(rimCol,float3(.75,1,1),.2*active),rim);
                rgb=lerp(rimCol,rgb,body);
                float alpha=saturate(body*i.color.a+rim*lerp(.7,.9,active)+halo);
                #ifdef UNITY_UI_CLIP_RECT
                alpha*=UnityGet2DClipping(i.world.xy,_ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(alpha-.001);
                #endif
                return float4(rgb,alpha);
            }
            ENDCG
        }
    }
}
