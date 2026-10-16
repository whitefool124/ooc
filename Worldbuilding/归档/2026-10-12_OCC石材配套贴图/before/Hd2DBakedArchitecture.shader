Shader "OCC/HD2D/BakedArchitecture"
{
 Properties
 {
  [MainTexture] _BaseMap("Base",2D)="white"{}
  [MainColor] _BaseColor("Color",Color)=(1,1,1,1)
  _Smoothness("Smoothness",Float)=0
  _Metallic("Metallic",Float)=0
  _Cutoff("Cutoff",Float)=0.5
  _AlphaClip("Alpha clip",Float)=0
  _BumpScale("Normal scale",Float)=1
  [Normal] _BumpMap("Authored normal map",2D)="bump"{}
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
   #pragma vertex BakedVertex
   #pragma fragment BakedFragment
   #pragma shader_feature_local _ALPHATEST_ON
   #pragma shader_feature_local _NORMALMAP
   #pragma shader_feature_local _ENVIRONMENTREFLECTIONS_OFF
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
   #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
   #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
   #pragma multi_compile _ _REFLECTION_PROBE_BLENDING
   #pragma multi_compile _ _REFLECTION_PROBE_BOX_PROJECTION
   #pragma multi_compile _ _REFLECTION_PROBE_ATLAS
   #include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"
   struct BakedVaryings { Varyings surface;half visibility:TEXCOORD11; };
   // The second UV channel stores visibility, leaving native texture coordinates intact.
   BakedVaryings BakedVertex(Attributes input) { BakedVaryings output;output.surface=LitPassVertex(input);output.visibility=input.staticLightmapUV.x;return output; }
   half4 BakedFragment(BakedVaryings input):SV_Target
   {
    SurfaceData surface;InitializeStandardLitSurfaceData(input.surface.uv,surface);
    // Architectural visibility attenuates indirect illumination through the BRDF,
    // preserving direct light, emission and the material's specular response.
    surface.occlusion*=lerp(.62h,1.h,saturate(input.visibility));
    InputData data;InitializeInputData(input.surface,surface.normalTS,data);
    InitializeBakedGIData(input.surface,data);
    half4 color=UniversalFragmentPBR(data,surface);
    color.rgb=MixFog(color.rgb,data.fogCoord);
    color.a=OutputAlpha(color.a,IsSurfaceTypeTransparent());
    return color;
   }
   ENDHLSL
  }
  UsePass "Universal Render Pipeline/Lit/ShadowCaster"
  UsePass "Universal Render Pipeline/Lit/DepthOnly"
 }
}
