Shader "OCC/HD2D/VfxVoxelSurface"
{
 Properties
 {
  _MainTex("Registered pixel material",2D)="white"{}
  _SourceRect("Native source rectangle",Vector)=(0,0,32,32)
  _Energy("Emission envelope",Float)=0
  _Reveal("Voxel reveal coverage",Range(0,1))=1
  _Phase("Material flow in native texels",Float)=0
  _ClipHeight("Local vertical reveal",Float)=999
  _RevealMode("1 circumference,2 height",Float)=2
  _RevealBounds("Native local height bounds",Vector)=(0,1,0,0)
  _LightResponse("External scene-light response",Float)=1
 }
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
  HLSLINCLUDE
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
  CBUFFER_START(UnityPerMaterial)
  float4 _MainTex_TexelSize,_SourceRect,_RevealBounds;float _Energy,_Reveal,_Phase,_ClipHeight,_RevealMode,_LightResponse;
  CBUFFER_END
  struct A{float4 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;};
  struct V{float4 p:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float2 uv:TEXCOORD2;float3 local:TEXCOORD3;};
  V Vert(A a){V v;v.local=a.p.xyz;v.world=TransformObjectToWorld(a.p.xyz);v.p=TransformWorldToHClip(v.world);v.normal=TransformObjectToWorldNormal(a.n);v.uv=a.uv;return v;}
  void Coverage(V v)
  {
   // The mask removes whole native cells. It never scales the source or paints extra detail.
   float3 cell=floor(v.local*32+.001)/32;
   float h=_RevealMode<1.5?frac(atan2(cell.z,cell.x)/6.2831853+.5):saturate((cell.y-_RevealBounds.x)/max(.03125,_RevealBounds.y));
   clip(_Reveal-h+.00001);
   clip(_ClipHeight-v.local.y);
  }
  ENDHLSL
  Pass
  {
   Name "ForwardLit" Tags {"LightMode"="UniversalForward"} Cull Back ZWrite On
   HLSLPROGRAM
   #pragma target 3.0
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile _ _ADDITIONAL_LIGHTS
   #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
   #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   half4 Frag(V v):SV_Target
   {
    Coverage(v);float2 cell=floor(v.uv*32+float2(0,floor(_Phase)));
    float2 pixel=frac((cell+.5)/_SourceRect.zw)*_SourceRect.zw+_SourceRect.xy;
    half3 colour=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,pixel*_MainTex_TexelSize.xy).rgb;
    float3 n=normalize(v.normal);Light key=GetMainLight(TransformWorldToShadowCoord(v.world));
    half3 lighting=half3(.11,.13,.17)+key.color*(.15+.65*saturate(dot(n,key.direction)))*key.shadowAttenuation;
    #if defined(_ADDITIONAL_LIGHTS)
    uint count=GetAdditionalLightsCount();for(uint i=0;i<count;i++){Light l=GetAdditionalLight(i,v.world,half4(1,1,1,1));lighting+=l.color*l.distanceAttenuation*l.shadowAttenuation*saturate(dot(n,l.direction));}
    #endif
    return half4(colour*(lighting*_LightResponse+_Energy),1);
   }
   ENDHLSL
  }
  Pass
  {
   Name "ShadowCaster" Tags {"LightMode"="ShadowCaster"} ZWrite On ColorMask 0 Cull Back
   HLSLPROGRAM
   #pragma vertex ShadowVert
   #pragma fragment ShadowFrag
   #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
   float3 _LightDirection,_LightPosition;
   V ShadowVert(A a)
   {
    V v=Vert(a);
    #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
    float3 direction=normalize(_LightPosition-v.world);
    #else
    float3 direction=_LightDirection;
    #endif
    v.p=ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(v.world,v.normal,direction)));return v;
   }
   half4 ShadowFrag(V v):SV_Target{Coverage(v);return 0;}
   ENDHLSL
  }
  Pass
  {
   Name "DepthOnly" Tags {"LightMode"="DepthOnly"} ZWrite On ColorMask R Cull Back
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Depth
   half4 Depth(V v):SV_Target{Coverage(v);return v.p.z;}
   ENDHLSL
  }
 }
}
