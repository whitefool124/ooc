Shader "OCC/HD2D/SkyFurnaceCloud"
{
 Properties { _Tint("Material colour",Color)=(.4,.35,.3,1) _Seed("Seed",Float)=0 _Age("Age",Float)=0 _Fire("Fire",Float)=0 _Opacity("Opacity",Float)=1 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+10" "RenderType"="Transparent" }
  Pass
  {
   Tags { "LightMode"="UniversalForward" }
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off Cull Off
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile _ _ADDITIONAL_LIGHTS
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   struct A { float4 p:POSITION;float2 uv:TEXCOORD0; };
   struct V { float4 p:SV_POSITION;float3 world:TEXCOORD1;float2 uv:TEXCOORD0; };
   CBUFFER_START(UnityPerMaterial)
   half4 _Tint;float _Seed,_Age,_Fire,_Opacity;
   CBUFFER_END
   float hash(float3 p){return frac(sin(dot(p,float3(12.9898,78.233,39.425)))*43758.5453);}
   float noise(float3 p)
   {
    float3 i=floor(p),f=frac(p);f=f*f*(3-2*f);
    return lerp(lerp(lerp(hash(i),hash(i+float3(1,0,0)),f.x),lerp(hash(i+float3(0,1,0)),hash(i+float3(1,1,0)),f.x),f.y),
     lerp(lerp(hash(i+float3(0,0,1)),hash(i+float3(1,0,1)),f.x),lerp(hash(i+float3(0,1,1)),hash(i+1),f.x),f.y),f.z);
   }
   V Vert(A a){V v;v.world=TransformObjectToWorld(a.p.xyz);v.p=TransformWorldToHClip(v.world);v.uv=a.uv;return v;}
   half4 Frag(V v):SV_Target
   {
    // Technical air layers use the same 1/32 metre spatial quantization as stage assets.
    float3 q=floor(v.world*32)/32;
    float n=noise(q*.75+float3(_Seed,_Age*.23,0))*.65+noise(q*1.8+_Seed)*.35;
    float2 p=v.uv*2-1;float radius=length(p);
    float density=saturate((1-radius+n*.3-.13)*3.2);
    clip(density-.025);
    float3 normal=normalize(GetWorldSpaceNormalizeViewDir(v.world)+float3(p.x,p.y,0)*.5);
    Light key=GetMainLight();half3 lit=half3(.12,.15,.21)+key.color*(.2+.45*saturate(dot(normal,key.direction)));
    #if defined(_ADDITIONAL_LIGHTS)
    uint count=GetAdditionalLightsCount();
    for(uint i=0;i<count;i++){Light l=GetAdditionalLight(i,v.world);lit+=l.color*l.distanceAttenuation*(.25+.55*saturate(dot(normal,l.direction)));}
    #endif
    half3 ash=_Tint.rgb*lit*(.6+floor(n*5)/5*.7);
    half3 fire=lerp(half3(.5,.045,.01),_Tint.rgb,saturate((1-radius)*1.5+n*.5));
    return half4(lerp(ash,fire,_Fire),density*_Opacity);
   }
   ENDHLSL
  }
 }
}
