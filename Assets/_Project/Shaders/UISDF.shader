Shader "VisionLab/UI Distance Field" {
 Properties {
  _MainTex("Font atlas",2D)="white"{}
  _FaceColor("Face",Color)=(1,1,1,1)
  _GradientScale("Gradient",Float)=10
  _FaceDilate("Weight",Float)=0
  _ScaleRatioA("Ratio",Float)=1
  _StencilComp("Stencil comparison",Float)=8
  _Stencil("Stencil",Float)=0
  _StencilOp("Stencil operation",Float)=0
  _StencilWriteMask("Stencil write mask",Float)=255
  _StencilReadMask("Stencil read mask",Float)=255
  _ColorMask("Colour mask",Float)=15
 }
 SubShader {
  Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"}
  Stencil {Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask]}
  Cull Off ZWrite Off ZTest [unity_GUIZTestMode] Blend SrcAlpha OneMinusSrcAlpha ColorMask [_ColorMask]
  Pass {
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
   #include "UnityCG.cginc"
   #include "UnityUI.cginc"
   sampler2D _MainTex;float4 _FaceColor,_ClipRect;float _FaceDilate;
   struct A{float4 position:POSITION;float4 color:COLOR;float4 uv:TEXCOORD0;};
   struct V{float4 position:SV_POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;float2 local:TEXCOORD1;};
   V vert(A i){V o;o.position=UnityObjectToClipPos(i.position);o.color=i.color*_FaceColor;o.uv=i.uv.xy;o.local=i.position.xy;return o;}
   half4 frag(V i):SV_Target{
    float distance=tex2D(_MainTex,i.uv).a;
    float width=max(fwidth(distance),.001);
    i.color.a*=smoothstep(.5-_FaceDilate-width,.5-_FaceDilate+width,distance);
    #ifdef UNITY_UI_CLIP_RECT
    i.color.a*=UnityGet2DClipping(i.local,_ClipRect);
    #endif
    return i.color;
   }
   ENDHLSL
  }
 }
}
