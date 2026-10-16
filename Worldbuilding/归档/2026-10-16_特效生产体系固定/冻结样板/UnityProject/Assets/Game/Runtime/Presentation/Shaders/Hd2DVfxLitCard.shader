Shader "OCC/HD2D/VfxLitCard"
{
 Properties
 {
  [MainTexture] _BaseMap("Native pixel artwork",2D)="white"{}
  [MainColor] _BaseColor("Surface colour",Color)=(1,1,1,1)
  _Cutoff("Silhouette cutoff",Range(0,1))=.5
  _Wrap("Thin card diffuse wrap",Range(0,1))=.35
  _BackScatter("Thin card transmission",Range(0,1))=.22
  _ReceiveShadows("Receive shadows on visible card",Float)=1
 }
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest"}
  HLSLINCLUDE
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
  CBUFFER_START(UnityPerMaterial)
  float4 _BaseMap_ST,_BaseColor;float _Cutoff,_Wrap,_BackScatter,_ReceiveShadows;
  CBUFFER_END
  struct A {float4 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;};
  struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;float3 n:TEXCOORD2;};
  V Vert(A a){V v;v.world=TransformObjectToWorld(a.p.xyz);v.p=TransformWorldToHClip(v.world);v.uv=TRANSFORM_TEX(a.uv,_BaseMap);v.n=TransformObjectToWorldNormal(a.n);return v;}
  V GroundedVertex(A a)
  {
   V v=Vert(a);float3 origin=TransformObjectToWorld(float3(0,0,0));
   float3 horizontal=mul((float3x3)unity_ObjectToWorld,float3(a.p.x,0,0));
   float heightScale=length(mul((float3x3)unity_ObjectToWorld,float3(0,1,0)));
   float4 upright=TransformWorldToHClip(origin+horizontal+float3(0,a.p.y*heightScale,0));
   v.p.z=(upright.z/upright.w)*v.p.w;return v;
  }
  half4 Surface(V v){half4 c=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,v.uv)*_BaseColor;clip(c.a-_Cutoff);return c;}
  ENDHLSL
  Pass
  {
   Name "ForwardLit" Tags {"LightMode"="UniversalForward"} Cull Off ZWrite On
   HLSLPROGRAM
   #pragma target 3.0
   #pragma vertex GroundedVertex
   #pragma fragment CardFragment
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile _ _ADDITIONAL_LIGHTS
   #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
   #pragma multi_compile_fragment _ _SHADOWS_SOFT
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   half3 Irradiance(Light l,float3 n)
   {
    half ndl=dot(n,l.direction);
    half diffuse=saturate((ndl+_Wrap)/(1+_Wrap));
    half transmission=_BackScatter*saturate(-ndl);
    return l.color*(diffuse+transmission)*l.distanceAttenuation*lerp(1,l.shadowAttenuation,_ReceiveShadows);
   }
   half4 CardFragment(V v):SV_Target
   {
    half4 c=Surface(v);float3 n=normalize(v.n);if(dot(n,GetWorldSpaceNormalizeViewDir(v.world))<0)n=-n;
    half3 light=SampleSH(n)*.65h;
    light+=Irradiance(GetMainLight(TransformWorldToShadowCoord(v.world)),n);
    #if defined(_ADDITIONAL_LIGHTS)
    uint count=GetAdditionalLightsCount();for(uint i=0;i<count;i++)light+=Irradiance(GetAdditionalLight(i,v.world,half4(1,1,1,1)),n);
    #endif
    return half4(c.rgb*light,c.a);
   }
   ENDHLSL
  }
  Pass
  {
   Name "ShadowCaster" Tags {"LightMode"="ShadowCaster"} Cull Off ZWrite On ColorMask 0
   HLSLPROGRAM
   #pragma vertex ShadowVertex
   #pragma fragment ShadowFragment
   #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
   float3 _LightDirection,_LightPosition;
   V ShadowVertex(A a)
   {
    V v=Vert(a);
    #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
    float3 direction=normalize(_LightPosition-v.world);
    #else
    float3 direction=_LightDirection;
    #endif
    v.p=ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(v.world,v.n,direction)));return v;
   }
   half4 ShadowFragment(V v):SV_Target {Surface(v);return 0;}
   ENDHLSL
  }
  Pass
  {
   Name "DepthOnly" Tags {"LightMode"="DepthOnly"} Cull Off ZWrite On ColorMask R
   HLSLPROGRAM
   #pragma vertex GroundedVertex
   #pragma fragment DepthFragment
   half4 DepthFragment(V v):SV_Target {Surface(v);return v.p.z;}
   ENDHLSL
  }
 }
}
