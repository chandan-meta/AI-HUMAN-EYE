Shader "VisionLab/Beam" {
 Properties {_BaseColor("Colour",Color)=(.08,.45,.65,1) _Alpha("Opacity",Range(0,1))=.08}
 SubShader {Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent"}
  Pass {Blend SrcAlpha One ZWrite Off Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct A{float4 p:POSITION;float3 n:NORMAL;};struct V{float4 p:SV_POSITION;float3 w:TEXCOORD0;float3 n:TEXCOORD1;};
   CBUFFER_START(UnityPerMaterial)
   float4 _BaseColor;float _Alpha;
   CBUFFER_END
   V vert(A a){V v;v.w=TransformObjectToWorld(a.p.xyz);v.p=TransformWorldToHClip(v.w);v.n=TransformObjectToWorldNormal(a.n);return v;}
   half4 frag(V v):SV_Target{float edge=pow(abs(dot(normalize(v.n),normalize(GetWorldSpaceViewDir(v.w)))),1.5);float noise=.9+.1*sin(v.w.x*8+_Time.y*.5);return half4(_BaseColor.rgb,_Alpha*edge*noise);}
   ENDHLSL
  }
 }
}
