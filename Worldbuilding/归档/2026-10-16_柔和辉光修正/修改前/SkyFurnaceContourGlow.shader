Shader "OCC/HD2D/SkyFurnaceContourGlow"
{
 Properties { _MainTex("Original seal/core",2D)="black"{} _ChargeMap("Activation data",2D)="black"{} _SealMode("Seal",Float)=0 _SealReveal("Charged native texels",Float)=1 _BurstMode("Core",Float)=0 _BurstRise("Core rise",Float)=1 _GlowEnergy("Contour radiance",Float)=1 [HDR] _GlowColour("Optical tint",Color)=(1,.38,.045,1) }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+5" "RenderType"="Transparent" }
  Pass
  {
   Tags {"LightMode"="UniversalForward"}
   Blend One One ZWrite Off Cull Back
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct A {float4 p:POSITION;float2 uv:TEXCOORD0;};struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;};
   TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);TEXTURE2D(_ChargeMap);SAMPLER(sampler_ChargeMap);
   CBUFFER_START(UnityPerMaterial)
   float4 _MainTex_TexelSize,_GlowColour;float _SealMode,_SealReveal,_BurstMode,_BurstRise,_GlowEnergy;
   CBUFFER_END
   V Vert(A a){V v;v.p=TransformObjectToHClip(a.p.xyz);v.uv=a.uv;return v;}
   float Ink(float2 uv)
   {
    float inside=step(0,uv.x)*step(uv.x,1)*step(0,uv.y)*step(uv.y,1);
    float2 cell=(floor(uv*_MainTex_TexelSize.zw)+.5)*_MainTex_TexelSize.xy;
    float ink=step(.5,SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,cell).a)*inside;
    if(_SealMode>.5)ink*=step(SAMPLE_TEXTURE2D(_ChargeMap,sampler_ChargeMap,cell).r,_SealReveal);
    if(_BurstMode>.5)ink*=step(cell.y,_BurstRise);
    return ink;
   }
   half4 Frag(V v):SV_Target
   {
    // Empty pixels on either side of every ink boundary: outer silhouette AND inner holes.
    clip(.5-Ink(v.uv));float nearInk=0,farInk=0;
    [unroll]for(int y=-1;y<=1;y++)[unroll]for(int x=-1;x<=1;x++)
    {
     if(x==0&&y==0)continue;
     float2 d=float2(x,y)*_MainTex_TexelSize.xy;
     nearInk=max(nearInk,Ink(v.uv+d));farInk=max(farInk,Ink(v.uv+d*3));
    }
    float glow=nearInk*.80+farInk*.20;clip(glow-.01);
    return half4(_GlowColour.rgb*glow*_GlowEnergy,0);
   }
   ENDHLSL
  }
 }
}
