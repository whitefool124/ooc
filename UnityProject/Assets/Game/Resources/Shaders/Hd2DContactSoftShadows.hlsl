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
 float sum=0,blockers=0;
 [unroll]for(uint i=0;i<12;i++)
 {
   float2 uv=clamp(sc.xy+occShadowDisk[i]*uvPerMetre*.18,tileMin,tileMax);
   // Interpolate valid blocker statistics, not depth itself: averaging a clear
   // sky texel with a leaf would invent a blocker. Gather keeps the search count
   // unchanged while avoiding the old nearest-texel radius jumps.
   // D3D gather order: (-,+),(+,+),(+,-),(-,-).
   float4 depths=GATHER_RED_TEXTURE2D(_MainLightShadowmapTexture,sampler_PointClamp,uv);
   float2 fraction=frac(uv*sampling.shadowmapSize.zw-.5);
   float4 weights=float4((1-fraction.x)*fraction.y,fraction.x*fraction.y,
       fraction.x*(1-fraction.y),(1-fraction.x)*(1-fraction.y));
   #if UNITY_REVERSED_Z
   float4 distance=(depths-sc.z)/depthPerMetre;
   #else
   float4 distance=(sc.z-depths)/depthPerMetre;
   #endif
   float4 validWeights=weights*step(.003,distance)*(1-step(12,distance));
   sum+=dot(distance,validWeights);blockers+=dot(validWeights,float4(1,1,1,1));
 }
 if(blockers<.0001)return MainLightShadow(incoming,positionWS,shadowMask,channels);
 // A candidate 1.26-degree source radius: contact starts at atlas precision,
 // penumbra grows with actual blocker separation, capped at 14cm in world space.
 float radius=min(.14,(sum/blockers)*.022);
 // Scaling a fixed 5x5 grid skipped native atlas texels at wide radii and
 // produced stripes. The pipeline's phase-correct 7x7 tent has full coverage.
 // Mix its stable penumbra with bilinear contact using measured separation.
 float2 radiusTexels=uvPerMetre*radius/texel;
 float softness=smoothstep(.65,3.5,max(radiusTexels.x,radiusTexels.y));
 half contact=SAMPLE_TEXTURE2D_SHADOW(_MainLightShadowmapTexture,sampler_LinearClampCompare,sc);
 half broad=SampleShadowmapFilteredHighQuality(TEXTURE2D_ARGS(_MainLightShadowmapTexture,sampler_LinearClampCompare),float4(sc,0),sampling);
 half visibility=lerp(contact,broad,softness);
 half4 shadowParams=GetMainLightShadowParams();
 half realtime=LerpWhiteTo(visibility,shadowParams.x);
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
