Shader "VisionLab/Holo" {
 Properties {_BaseColor("Colour",Color)=(.05,.7,1,1) _Alpha("Reveal",Range(0,1))=.75}
 SubShader {Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent"}
  Pass {Blend SrcAlpha One ZWrite Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct A{float4 p:POSITION;float3 n:NORMAL;};struct V{float4 p:SV_POSITION;float3 w:TEXCOORD0;float3 n:TEXCOORD1;};
   CBUFFER_START(UnityPerMaterial)
   float4 _BaseColor;float _Alpha;
   CBUFFER_END
   V vert(A a){V v;v.w=TransformObjectToWorld(a.p.xyz);v.p=TransformWorldToHClip(v.w);v.n=TransformObjectToWorldNormal(a.n);return v;}
   half4 frag(V v):SV_Target{float rim=pow(1-saturate(dot(normalize(v.n),normalize(GetWorldSpaceViewDir(v.w)))),2);float lines=.8+.2*sin(v.w.y*180-_Time.y*3);return half4(_BaseColor.rgb*(.45+rim)*lines,_Alpha*(.6+rim*.4));}
   ENDHLSL
  }
 }
}
