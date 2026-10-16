Shader "OCC/HD2D/SkyFurnaceHeat"
{
 Properties { _Strength("Local refraction",Float)=.007 _Age("Cinematic clock",Float)=0 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+30" "RenderType"="Transparent" }
  Pass
  {
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off Cull Off
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
   struct A{float4 p:POSITION;float2 uv:TEXCOORD0;};struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;};
   CBUFFER_START(UnityPerMaterial)
   float _Strength,_Age;
   CBUFFER_END
   V Vert(A a){V v;v.p=TransformObjectToHClip(a.p.xyz);v.uv=a.uv;return v;}
   half4 Frag(V v):SV_Target
   {float2 p=v.uv*2-1;float mask=saturate(1-dot(p,p));float2 screen=v.p.xy/_ScaledScreenParams.xy;
    float2 offset=float2(sin(v.uv.y*45+_Age*5),cos(v.uv.x*33+_Age*4))*_Strength*mask;
    return half4(SampleSceneColor(saturate(screen+offset)),mask*.3*saturate(_Strength*150));}
   ENDHLSL
  }
 }
}
