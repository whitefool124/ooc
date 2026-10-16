Shader "OCC/HD2D/SkyFurnaceEnergy"
{
 Properties { _Tint("Energy",Color)=(.2,3,5,1) _Reveal("Conductor reveal",Range(0,1))=1 _Intensity("Envelope",Float)=1 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
  Pass
  {
   Blend SrcAlpha One
   ZWrite Off Cull Back
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct A { float4 p:POSITION; half4 color:COLOR; };
   struct V { float4 p:SV_POSITION; float3 local:TEXCOORD0; half4 color:COLOR; };
   CBUFFER_START(UnityPerMaterial)
   half4 _Tint;float _Reveal,_Intensity;
   CBUFFER_END
   V Vert(A a){V v;float3 p=round(a.p.xyz*32)/32;v.p=TransformObjectToHClip(p);v.local=p;v.color=a.color;return v;}
   half4 Frag(V v):SV_Target
   {
    float angle=atan2(v.local.z,v.local.x)/6.283185+.5;
    clip(_Reveal-angle);
    return half4(_Tint.rgb*_Intensity,_Tint.a);
   }
   ENDHLSL
  }
 }
}
