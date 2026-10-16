Shader "OCC/HD2D/WetCourtyard"
{
 Properties
 {
  [MainTexture] _BaseMap("Native pixel colour",2D)="white"{}
  [MainColor] _BaseColor("Colour",Color)=(1,1,1,1)
  _Smoothness("Dry stone smoothness",Range(0,1))=0.08
  _Metallic("Metallic",Float)=0
  _ClearCoatMask("Authored wet coverage",Range(0,1))=0
  _ClearCoatSmoothness("Water film smoothness",Range(0,1))=0.88
  _Cutoff("Cutoff",Float)=0.5
  _AlphaClip("Alpha clip",Float)=0
  _BumpScale("Normal scale",Float)=1
  [Normal] _BumpMap("Authored stone normal",2D)="bump"{}
  _RoughnessMap("Authored roughness (linear R)",2D)="white"{}
  _GroundSurfaceData("Authored ground R roughness/G metal/B coat",2D)="black"{}
  _OcclusionStrength("Occlusion",Float)=1
  _EmissionColor("Emission",Color)=(0,0,0,0)
  _SpecColor("Specular",Color)=(0.2,0.2,0.2,1)
 }
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
  Pass
  {
   Name "ForwardLit" Tags {"LightMode"="UniversalForward"}
   HLSLPROGRAM
   #pragma target 3.0
   #pragma vertex WetVertex
   #pragma fragment WetFragment
   #pragma shader_feature_local _ALPHATEST_ON
   #pragma shader_feature_local_fragment _OCC_CONTACT_SOFT_SHADOWS
   #pragma target 4.5 _OCC_CONTACT_SOFT_SHADOWS
   #pragma shader_feature_local_fragment _OCC_PIXEL_EMISSION
            #pragma target 4.5 _OCC_PIXEL_EMISSION
   #pragma shader_feature_local _NORMALMAP
   #pragma shader_feature_local _OCC_ROUGHNESS_MAP
   #pragma shader_feature_local _OCC_GROUND_DATA
   #pragma shader_feature_local _ENVIRONMENTREFLECTIONS_OFF
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
   #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
   #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
   #pragma shader_feature_local_fragment _OCC_LOCAL_LIGHT_SHADOWS
   #pragma multi_compile_fragment _ _LIGHT_LAYERS
   #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
   #pragma multi_compile_fragment _ _LIGHT_COOKIES
   #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
   #pragma multi_compile _ _REFLECTION_PROBE_BLENDING
   #pragma multi_compile _ _REFLECTION_PROBE_BOX_PROJECTION
   #pragma multi_compile _ _REFLECTION_PROBE_ATLAS
   #define _CLEARCOAT 1
   #if !defined(_OCC_LOCAL_LIGHT_SHADOWS)
   #undef _ADDITIONAL_LIGHT_SHADOWS
   #endif
   #include "Hd2DContactSoftShadows.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"
            #include "Hd2DNativePixelEmission.hlsl"
   TEXTURE2D(_RoughnessMap); SAMPLER(sampler_RoughnessMap);
   TEXTURE2D(_GroundSurfaceData); SAMPLER(sampler_GroundSurfaceData);
   struct WetVaryings { Varyings surface; half visibility:TEXCOORD11; float2 ground:TEXCOORD12; };
   WetVaryings WetVertex(Attributes input)
   {
    WetVaryings output;output.surface=LitPassVertex(input);
    output.visibility=input.staticLightmapUV.x;
    output.ground=TransformObjectToWorld(input.positionOS.xyz).xz;
    return output;
   }
   half4 WetFragment(WetVaryings input):SV_Target
   {
    SurfaceData surface;InitializeStandardLitSurfaceData(input.surface.uv,surface);
    #if defined(_OCC_PIXEL_EMISSION)
    surface.emission+=NativePixelEmission(input.surface.uv,surface.albedo);
    #endif
    // Coverage comes from the tile/material role. Large analytic sine patches
    // produced detached oval dark spots that looked like missing-object shadows.
    // Keep the water film without inventing another large-scale ground pattern.
    half wet=saturate(_ClearCoatMask);
    surface.albedo*=1.h-.12h*wet;
    half drySmoothness=_Smoothness;
    #if defined(_OCC_ROUGHNESS_MAP)
    drySmoothness=1.h-saturate(SAMPLE_TEXTURE2D(_RoughnessMap,sampler_RoughnessMap,input.surface.uv).r);
    #endif
    half authoredCoat=0.h;
    #if defined(_OCC_GROUND_DATA)
    half3 field=SAMPLE_TEXTURE2D(_GroundSurfaceData,sampler_GroundSurfaceData,input.surface.uv).rgb;
    drySmoothness=1.h-field.r;surface.metallic=field.g;authoredCoat=field.b;
    #endif
    surface.smoothness=lerp(drySmoothness,.64h,wet);
    surface.clearCoatMask=max(wet,authoredCoat);surface.clearCoatSmoothness=_ClearCoatSmoothness;
    surface.occlusion=lerp(.62h,1.h,saturate(input.visibility));
    InputData data;InitializeInputData(input.surface,surface.normalTS,data);
    InitializeBakedGIData(input.surface,data);
    half4 colour=UniversalFragmentPBR(data,surface);
    colour.rgb=MixFog(colour.rgb,data.fogCoord);
    colour.a=OutputAlpha(colour.a,IsSurfaceTypeTransparent());
    return colour;
   }
   ENDHLSL
  }
  UsePass "Universal Render Pipeline/Lit/ShadowCaster"
  UsePass "Universal Render Pipeline/Lit/DepthOnly"
 }
}
