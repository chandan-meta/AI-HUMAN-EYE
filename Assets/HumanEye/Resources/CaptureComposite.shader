// QA recordings keep the rendered Canvas sharp and outside the scene's animated volume effects.
Shader "Hidden/HumanEye/CaptureComposite"
{
    Properties
    {
        _MainTex("Graded scene", 2D) = "black" {}
        _UI("Premultiplied UI", 2D) = "black" {}
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex, _UI;
            half4 frag(v2f_img i) : SV_Target
            {
                half4 scene = tex2D(_MainTex, i.uv);
                half4 ui = tex2D(_UI, i.uv);
                return half4(ui.rgb + scene.rgb * (1 - ui.a), 1);
            }
            ENDCG
        }
    }
}
