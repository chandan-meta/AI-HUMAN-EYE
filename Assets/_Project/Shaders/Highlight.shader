Shader "VisionLab/Highlight" {
 Properties {
  _BaseMap("Texture",2D)="white"{}
  _BaseColor("Base colour",Color)=(1,1,1,1)
  _Tint("Rim colour",Color)=(.18,.77,1,1)
  _Alpha("Opacity",Range(0,1))=1
  _Highlight("Rim strength",Range(0,3))=.15
  _SweepY("Scan height",Float)=-100
  _LayerIndex("Layer",Float)=0
  _Whiten("White tissue",Range(0,1))=0
  _ZWrite("Depth write",Float)=1
  _Cull("Face culling",Float)=2
 }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
  Pass {
   Tags {"LightMode"="UniversalForward"}
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite [_ZWrite]
   Cull [_Cull]
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   struct Attributes {float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
   struct Varyings {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float2 uv:TEXCOORD2;};
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
   float4 _BaseMap_ST,_BaseColor,_Tint;float _Alpha,_Highlight,_SweepY,_LayerIndex,_Whiten;
   CBUFFER_END
   Varyings vert(Attributes v){Varyings o;o.world=TransformObjectToWorld(v.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);o.normal=TransformObjectToWorldNormal(v.normalOS);o.uv=TRANSFORM_TEX(v.uv,_BaseMap);return o;}
   half4 frag(Varyings i):SV_Target{
    float3 n=normalize(i.normal);float rim=pow(1-saturate(dot(n,normalize(GetWorldSpaceViewDir(i.world)))),3);
    Light light=GetMainLight();float lighting=.42+.58*saturate(dot(n,light.direction));
    float3 colour=lerp(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb,float3(1,1,1),_Whiten)*_BaseColor.rgb*lighting;
    float scan=exp(-abs(i.world.y-_SweepY)*65);
    return half4(colour+_Tint.rgb*(rim*_Highlight+scan*.8),_Alpha*_BaseColor.a);
   }
   ENDHLSL
  }
 }
}
