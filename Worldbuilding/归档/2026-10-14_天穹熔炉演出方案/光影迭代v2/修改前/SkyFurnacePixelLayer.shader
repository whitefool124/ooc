Shader "OCC/HD2D/SkyFurnacePixelLayer"
{
 Properties { [MainTexture] _MainTex("Native32 artwork",2D)="white"{} _UVRect("Lossless source region",Vector)=(0,0,1,1) _RepeatU("Repeat within region",Float)=0 _ShadeLevel("Lighting envelope",Float)=1 _Emission("Authored colour energy",Float)=0 _Opacity("Envelope",Range(0,1))=1 _Seed("Layer seed",Float)=0 _Age("Layer age",Float)=0 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+10" "RenderType"="Transparent" }
  Pass
  {
   Tags {"LightMode"="UniversalForward"}
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off Cull Off
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile _ _ADDITIONAL_LIGHTS
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   struct A{float4 p:POSITION;float2 uv:TEXCOORD0;};struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;};
   TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
   CBUFFER_START(UnityPerMaterial)
   float4 _MainTex_ST,_UVRect;float _RepeatU,_ShadeLevel,_Emission,_Opacity,_Seed,_Age;
   CBUFFER_END
   V Vert(A a){V v;v.world=TransformObjectToWorld(a.p.xyz);v.p=TransformWorldToHClip(v.world);v.uv=a.uv;return v;}
   half4 Frag(V v):SV_Target
   {
    float2 artworkUV=float2(_RepeatU>.5?frac(v.uv.x):v.uv.x,v.uv.y)*_UVRect.zw+_UVRect.xy;
    half4 source=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,artworkUV);clip(source.a-.5);
    float3 normal=GetWorldSpaceNormalizeViewDir(v.world);Light key=GetMainLight();
    half3 lighting=half3(.16,.18,.23)+key.color*(.25+.55*saturate(dot(normal,key.direction)));
    #if defined(_ADDITIONAL_LIGHTS)
    uint count=GetAdditionalLightsCount();
    for(uint i=0;i<count;i++){Light l=GetAdditionalLight(i,v.world);lighting+=l.color*l.distanceAttenuation*(.2+.5*saturate(dot(normal,l.direction)));}
    #endif
    // No geometric sprite growth, RGB tint, gradients or painted extra detail.
    return half4(source.rgb*(lighting+_Emission)*_ShadeLevel,source.a*_Opacity);
   }
   ENDHLSL
  }
 }
}
