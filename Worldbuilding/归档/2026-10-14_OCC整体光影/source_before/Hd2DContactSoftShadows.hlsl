#ifndef OCC_CONTACT_SOFT_SHADOWS_INCLUDED
#define OCC_CONTACT_SOFT_SHADOWS_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
#if defined(_OCC_CONTACT_SOFT_SHADOWS)
// Fixed disk samples avoid frame noise. The existing directional atlas supplies
// both blockers and comparison depth; no new shadow camera or target is used.
static const float2 occShadowDisk[16]={
 float2(-.613,.354),float2(.170,-.040),float2(-.299,-.791),float2(.645,.493),
 float2(-.652,-.520),float2(.422,-.621),float2(.023,.692),float2(.782,-.271),
 float2(-.182,.240),float2(.214,.291),float2(-.893,.020),float2(.079,-.920),
 float2(-.375,.814),float2(.914,.113),float2(.517,.824),float2(-.053,-.393)};
half OCCContactSoftMainLightShadow(float4 incoming,float3 positionWS,half4 shadowMask,half4 channels)
{
 #if !defined(MAIN_LIGHT_CALCULATE_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_SCREEN)
 return MainLightShadow(incoming,positionWS,shadowMask,channels);
 #else
 uint cascade=0;
 #if defined(_MAIN_LIGHT_SHADOWS_CASCADE)
 cascade=(uint)ComputeCascadeIndex(positionWS);
 if(cascade>=4)return MainLightShadow(incoming,positionWS,shadowMask,channels);
 #endif
 float4x4 projection=_MainLightWorldToShadow[cascade];
 float3 sc=mul(projection,float4(positionWS,1)).xyz;
 if(BEYOND_SHADOW_FAR(sc))return 1.h;
 ShadowSamplingData sampling=GetMainLightShadowSamplingData();
 float2 texel=sampling.shadowmapSize.xy;
 float2 tileSpan=float2(1,1);
 #if defined(_MAIN_LIGHT_SHADOWS_CASCADE)
 tileSpan=float2(.5,min(1.,sampling.shadowmapSize.z/(2.*sampling.shadowmapSize.w)));
 #endif
 float2 tileMin=floor(sc.xy/tileSpan)*tileSpan+texel;
 float2 tileMax=tileMin+tileSpan-2*texel;
 float2 uvPerMetre=float2(length(projection[0].xyz),length(projection[1].xyz));
 float depthPerMetre=max(length(projection[2].xyz),1e-6);
 float sum=0;uint blockers=0;
 [unroll]for(uint i=0;i<12;i++)
 {
   float2 uv=clamp(sc.xy+occShadowDisk[i]*uvPerMetre*.18,tileMin,tileMax);
   float depth=SAMPLE_TEXTURE2D_LOD(_MainLightShadowmapTexture,sampler_PointClamp,uv,0).r;
   #if UNITY_REVERSED_Z
   float distance=(depth-sc.z)/depthPerMetre;
   #else
   float distance=(sc.z-depth)/depthPerMetre;
   #endif
   if(distance>.003&&distance<12){sum+=distance;blockers++;}
 }
 if(blockers==0)return MainLightShadow(incoming,positionWS,shadowMask,channels);
 // A candidate 1.26-degree source radius: contact starts at atlas precision,
 // penumbra grows with actual blocker separation, capped at 14cm in world space.
 float radius=min(.14,(sum/blockers)*.022);
 // A dense tent kernel spans at most four atlas texels. The sparse wide disk
 // left visible holes inside foliage shadows; this bound keeps sampling covered.
 float2 uvRadius=clamp(uvPerMetre*radius,texel*.65,texel*4);
 half visibility=0;
 [unroll]for(int y=-2;y<=2;y++)[unroll]for(int x=-2;x<=2;x++)
 {
   float2 uv=clamp(sc.xy+float2(x,y)*uvRadius*.5,tileMin,tileMax);
   half weight=(3-abs(x))*(3-abs(y));
   visibility+=weight*SAMPLE_TEXTURE2D_SHADOW(_MainLightShadowmapTexture,sampler_LinearClampCompare,float3(uv,sc.z));
 }
 half4 shadowParams=GetMainLightShadowParams();
 half realtime=LerpWhiteTo(visibility/81.h,shadowParams.x);
 half baked=1.h;
 #if defined(CALCULATE_BAKED_SHADOWS)
 baked=BakedShadow(shadowMask,channels,shadowParams);
 #endif
 return MixRealtimeAndBakedShadows(realtime,baked,GetMainLightShadowFade(positionWS));
 #endif
}
// Shadows.hlsl is already included above. Only subsequent lighting call sites
// are redirected; the original implementation remains available for fallback.
#define MainLightShadow OCCContactSoftMainLightShadow
#endif
#endif
