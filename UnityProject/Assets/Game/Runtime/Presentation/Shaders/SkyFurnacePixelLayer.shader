Shader "OCC/HD2D/SkyFurnacePixelLayer"
{
 Properties { _EdgeEnergy("Inner and outer native rim radiance",Float)=0  _BurstMode("Core burst only",Float)=0 _BurstRise("Native flame rise",Range(0,1))=1 _BurstBurnout("Native flame burnout",Range(0,1))=0  [MainTexture] _MainTex("Native32 artwork",2D)="white"{} _UVRect("Source region",Vector)=(0,0,1,1) _RepeatU("Repeat within region",Float)=0 _ShadeLevel("Lighting envelope",Float)=1 _Emission("Authored colour energy",Float)=0 _Opacity("Envelope",Range(0,1))=1 _Seed("Layer seed",Float)=0 _Age("Layer age",Float)=0 _SoftDepth("World intersection fade",Float)=0 _ThinScatter("Thin atmosphere scatter",Float)=0 _ChargeMap("Native texel activation data",2D)="black"{} _SealMode("Seal choreography only",Float)=0 _SealReveal("Whole texel coverage",Range(0,1))=1 _SealClock("Energy travel",Float)=0 _SealFlow("Travel energy",Float)=0 }
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
   struct A{float4 p:POSITION;float2 uv:TEXCOORD0;float3 n:NORMAL;half4 colour:COLOR;};struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;float3 n:TEXCOORD2;half4 colour:COLOR;};
   TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);TEXTURE2D(_ChargeMap);SAMPLER(sampler_ChargeMap);
   CBUFFER_START(UnityPerMaterial)
   float4 _MainTex_ST,_MainTex_TexelSize,_UVRect;float _EdgeEnergy,_RepeatU,_ShadeLevel,_Emission,_Opacity,_Seed,_Age,_SoftDepth,_ThinScatter,_SealMode,_SealReveal,_SealClock,_SealFlow,_BurstMode,_BurstRise,_BurstBurnout;
   CBUFFER_END
   float LitInk(float2 uv)
   {
    float inside=step(0,uv.x)*step(uv.x,1)*step(0,uv.y)*step(uv.y,1);
    float2 cell=(floor(uv*_MainTex_TexelSize.zw)+.5)*_MainTex_TexelSize.xy;
    float ink=step(.5,SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,cell).a)*inside;
    if(_SealMode>.5)ink*=step(SAMPLE_TEXTURE2D(_ChargeMap,sampler_ChargeMap,cell).r,_SealReveal);
    if(_BurstMode>.5)ink*=step(cell.y,_BurstRise);
    return ink;
   }
   V Vert(A a){V v;v.world=TransformObjectToWorld(a.p.xyz);v.p=TransformWorldToHClip(v.world);v.uv=a.uv;v.n=TransformObjectToWorldNormal(a.n);v.colour=a.colour;return v;}
   half4 Frag(V v):SV_Target
   {
    float2 artworkUV=float2(_RepeatU>.5?frac(v.uv.x):v.uv.x,v.uv.y)*_UVRect.zw+_UVRect.xy;
    half4 source=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,artworkUV);clip(source.a-.5);
    if(_BurstMode>.5){float row=(floor(artworkUV.y*_MainTex_TexelSize.w)+.5)*_MainTex_TexelSize.y;clip(_BurstRise-row);clip(row-_BurstBurnout);}
    float energy=_Emission,coverage=1;
    if(_SealMode>.5)
    {
     // Reveal whole authored texels from transparency; uncharged ink has no dark backing.
     float2 cellUV=(floor(artworkUV*_MainTex_TexelSize.zw)+.5)*_MainTex_TexelSize.xy;
     float activation=SAMPLE_TEXTURE2D(_ChargeMap,sampler_ChargeMap,cellUV).r;
     float charged=step(activation,_SealReveal);
     coverage=charged;
     clip(coverage-.5);
     float head=step(0,_SealReveal-activation)*step(_SealReveal-activation,.045)*step(_SealReveal,.995);
     float pulse=step(.92,frac(_SealClock-activation));
     energy=_Emission+_SealFlow*(head*1.8+pulse*.35);
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
    float2 pixel=_MainTex_TexelSize.xy;
    float surrounded=min(min(LitInk(artworkUV+float2(pixel.x,0)),LitInk(artworkUV-float2(pixel.x,0))),min(LitInk(artworkUV+float2(0,pixel.y)),LitInk(artworkUV-float2(0,pixel.y))));
    half3 rim=half3(1,.36,.025)*(1-surrounded)*_EdgeEnergy;
    return half4(source.rgb*v.colour.rgb*(lighting+energy)*_ShadeLevel+rim,source.a*v.colour.a*_Opacity*intersection*coverage);
   }
   ENDHLSL
  }
 }
}
