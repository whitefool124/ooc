Shader "OCC/HD2D/CourtyardWater"
{
 Properties
 {
  [MainTexture] _BaseMap("Native basin floor",2D)="white"{}
  _WaterPattern("Native water colour clusters",2D)="gray"{}
  _WaterBounds("World XZ minimum and size",Vector)=(0,0,1,1)
  _Obstacle0("Stone XZ minimum and size",Vector)=(0,0,0,0)
  _Obstacle1("Stone XZ minimum and size",Vector)=(0,0,0,0)
  _Obstacle2("Stone XZ minimum and size",Vector)=(0,0,0,0)
  _DeepColor("Absorbed water colour",Color)=(.07,.23,.27,1)
  _WaterDepth("Authored optical depth",Float)=.5
  _WaveStrength("Normal ripple strength",Float)=.055
  _WaveSpeed("Ripple speed",Float)=.24
  _Smoothness("Water smoothness",Range(0,1))=.9
 }
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
  Pass
  {
   Name "WaterForward" Tags {"LightMode"="UniversalForward"}
   HLSLPROGRAM
   #pragma target 3.0
   #pragma vertex WaterVertex
   #pragma fragment WaterFragment
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
   #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
   #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
   #pragma multi_compile _ _REFLECTION_PROBE_BLENDING
   #pragma multi_compile _ _REFLECTION_PROBE_BOX_PROJECTION
   #pragma multi_compile _ _REFLECTION_PROBE_ATLAS
   #define _SPECULAR_SETUP 1
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
   TEXTURE2D(_WaterPattern);SAMPLER(sampler_WaterPattern);
   CBUFFER_START(UnityPerMaterial)
    float4 _WaterBounds,_Obstacle0,_Obstacle1,_Obstacle2;
    half4 _DeepColor;
    float _WaterDepth,_WaveStrength,_WaveSpeed,_Smoothness;
   CBUFFER_END
   struct Attributes { float4 positionOS:POSITION; };
   struct Varyings { float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; half fog:TEXCOORD1; };
   Varyings WaterVertex(Attributes input)
   {
    Varyings output;output.world=TransformObjectToWorld(input.positionOS.xyz);
    output.positionCS=TransformWorldToHClip(output.world);output.fog=ComputeFogFactor(output.positionCS.z);return output;
   }
   float StoneDistance(float2 p,float4 stone)
   {
    if(stone.z<=0||stone.w<=0)return 1000;
    return length(max(abs(p-(stone.xy+stone.zw*.5))-stone.zw*.5,0));
   }
   half4 WaterFragment(Varyings input):SV_Target
   {
    float2 p=input.world.xz;
    float2 bank=min(p-_WaterBounds.xy,_WaterBounds.xy+_WaterBounds.zw-p);
    float edge=max(0,min(bank.x,bank.y));
    edge=min(edge,min(StoneDistance(p,_Obstacle0),min(StoneDistance(p,_Obstacle1),StoneDistance(p,_Obstacle2))));
    float depth=max(0,_WaterDepth)*smoothstep(0,.45,edge);
    // Basin and water colour retain the same 32-texel/world-unit lattice.
    float2 nativeUV=frac((floor(p*32)+.5)/32);
    half3 bed=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,nativeUV).rgb*half3(.72,.88,.9);
    float2 drift=float2(floor(_Time.y*.3),floor(_Time.y*.15))/32;
    half3 pattern=SAMPLE_TEXTURE2D(_WaterPattern,sampler_WaterPattern,frac(nativeUV+drift)).rgb;
    half3 body=lerp(bed,_DeepColor.rgb,1-exp(-depth*2.8));
    body=lerp(body,pattern,.07);
    body*=lerp(.82,1,smoothstep(0,.0625,edge));
    float t=_Time.y*_WaveSpeed;
    float phase0=dot(p,float2(4.1,2.7))+t;
    float phase1=dot(p,float2(-2.3,5.2))-t*.73;
    float2 slope=(float2(4.1,2.7)*cos(phase0)+float2(-2.3,5.2)*cos(phase1)*.6)*_WaveStrength;
    slope*=smoothstep(0,.18,edge);
    InputData data=(InputData)0;
    data.positionWS=input.world;data.normalWS=normalize(float3(-slope.x,1,-slope.y));
    data.viewDirectionWS=GetWorldSpaceNormalizeViewDir(input.world);
    data.shadowCoord=TransformWorldToShadowCoord(input.world);
    data.bakedGI=SampleSH(data.normalWS);data.shadowMask=half4(1,1,1,1);
    data.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(input.positionCS);
    SurfaceData surface=(SurfaceData)0;
    surface.albedo=body;surface.specular=half3(.02,.02,.02);
    surface.smoothness=_Smoothness;surface.occlusion=lerp(.7,1,smoothstep(0,.3,edge));surface.alpha=1;
    half4 colour=UniversalFragmentPBR(data,surface);
    colour.rgb=MixFog(colour.rgb,input.fog);return half4(colour.rgb,1);
   }
   ENDHLSL
  }
  Pass
  {
   Name "DepthOnly" Tags {"LightMode"="DepthOnly"}
   ZWrite On ColorMask 0
   HLSLPROGRAM
   #pragma vertex DepthVertex
   #pragma fragment DepthFragment
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   float4 DepthVertex(float4 p:POSITION):SV_POSITION{return TransformObjectToHClip(p.xyz);}
   half4 DepthFragment():SV_Target{return 0;}
   ENDHLSL
  }
 }
}
