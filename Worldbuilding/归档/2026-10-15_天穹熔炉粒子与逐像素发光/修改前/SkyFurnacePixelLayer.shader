Shader "OCC/HD2D/SkyFurnacePixelLayer"
{
 Properties { [MainTexture] _MainTex("Native32 artwork",2D)="white"{} _UVRect("Source region",Vector)=(0,0,1,1) _RepeatU("Repeat within region",Float)=0 _ShadeLevel("Lighting envelope",Float)=1 _Emission("Authored colour energy",Float)=0 _Opacity("Envelope",Range(0,1))=1 _Seed("Layer seed",Float)=0 _Age("Layer age",Float)=0 _SoftDepth("World intersection fade",Float)=0 _ThinScatter("Thin atmosphere scatter",Float)=0 _SealMode("Seal choreography only",Float)=0 _SealReveal("Whole texel coverage",Range(0,1))=1 _SealClock("Energy travel",Float)=0 _SealFlow("Travel energy",Float)=0 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+10" "RenderType"="Transparent" }
  Pass
  {
   Tags {"LightMode"="UniversalForward"}
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off Cull Back
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile _ _ADDITIONAL_LIGHTS
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
   #pragma multi_compile_fragment _ _SHADOWS_SOFT
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
   struct A{float4 p:POSITION;float2 uv:TEXCOORD0;float3 n:NORMAL;};struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;float3 n:TEXCOORD2;};
   TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
   CBUFFER_START(UnityPerMaterial)
   float4 _MainTex_ST,_MainTex_TexelSize,_UVRect;float _RepeatU,_ShadeLevel,_Emission,_Opacity,_Seed,_Age,_SoftDepth,_ThinScatter,_SealMode,_SealReveal,_SealClock,_SealFlow;
   CBUFFER_END
   V Vert(A a){V v;v.world=TransformObjectToWorld(a.p.xyz);v.p=TransformWorldToHClip(v.world);v.uv=a.uv;v.n=TransformObjectToWorldNormal(a.n);return v;}
   half4 Frag(V v):SV_Target
   {
    float2 artworkUV=float2(_RepeatU>.5?frac(v.uv.x):v.uv.x,v.uv.y)*_UVRect.zw+_UVRect.xy;
    half4 source=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,artworkUV);clip(source.a-.5);
    float travel=0;
    if(_SealMode>.5)
    {
     // Coverage and travelling light are constant within each authored texel.
     // No new lines, colours, noise, or fractional edge fragments are painted.
     float2 cell=(floor(artworkUV*_MainTex_TexelSize.zw)+.5)*_MainTex_TexelSize.xy-.5;
     float angle=frac(atan2(cell.y,cell.x)/6.2831853+.25);
     // The complete design stays visible; the charging front lights it up.
     float charged=step(angle,_SealReveal+.0001);
     source.rgb*=lerp(.28,1,charged);
     float band=frac(angle*2-_SealClock);
     travel=step(.78,band)*_SealFlow*charged;
    }
    float3 normal=normalize(v.n);if(dot(normal,GetWorldSpaceNormalizeViewDir(v.world))<0)normal=-normal;
    Light key=GetMainLight(TransformWorldToShadowCoord(v.world));
    half3 lighting=half3(.16,.18,.23)+key.color*(.25+.55*saturate(dot(normal,key.direction)))*key.shadowAttenuation;
    #if defined(_ADDITIONAL_LIGHTS)
    uint count=GetAdditionalLightsCount();
    for(uint i=0;i<count;i++){Light l=GetAdditionalLight(i,v.world,half4(1,1,1,1));lighting+=l.color*l.distanceAttenuation*l.shadowAttenuation*(.2+.5*saturate(dot(normal,l.direction))+.25*_ThinScatter);}
    #endif
    // No geometric sprite growth, RGB tint, gradients or painted extra detail.
    float intersection=1;
    if(_SoftDepth>0){float scene=LinearEyeDepth(SampleSceneDepth(GetNormalizedScreenSpaceUV(v.p)),_ZBufferParams);float own=-TransformWorldToView(v.world).z;intersection=saturate((scene-own)/_SoftDepth);}
    return half4(source.rgb*(lighting+_Emission+travel)*_ShadeLevel,source.a*_Opacity*intersection);
   }
   ENDHLSL
  }
 }
}
