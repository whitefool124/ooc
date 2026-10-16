Shader "OCC/HD2D/SkyFurnaceSolidPixel"
{
 Properties { [HDR] _Tint("Pixel colour",Color)=(1,.6,.12,1) _Energy("Emission",Float)=1 _LightResponse("Scene light",Float)=1 _ClipHeight("Column length",Float)=999 _GroundHeight("Floor clip",Float)=-999 _ZWrite("Depth write",Float)=1 }
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent"}
  Pass
  {
   Tags {"LightMode"="UniversalForward"}
   Blend One Zero ZWrite [_ZWrite] Cull Back
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile _ _ADDITIONAL_LIGHTS
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   struct A {float4 p:POSITION;float3 n:NORMAL;half4 colour:COLOR;};
   struct V {float4 p:SV_POSITION;float3 world:TEXCOORD0;float3 n:TEXCOORD1;float height:TEXCOORD2;half4 colour:COLOR;};
   CBUFFER_START(UnityPerMaterial)
   half4 _Tint;float _Energy,_LightResponse,_ClipHeight,_GroundHeight,_ZWrite;
   CBUFFER_END
   V Vert(A a){V v;v.world=TransformObjectToWorld(a.p.xyz);v.p=TransformWorldToHClip(v.world);v.n=TransformObjectToWorldNormal(a.n);v.height=a.p.y;v.colour=a.colour;return v;}
   half4 Frag(V v):SV_Target
   {
    clip(_ClipHeight-v.height);clip(v.world.y-_GroundHeight);
    float3 n=normalize(v.n);Light key=GetMainLight();
    half3 light=half3(.22,.24,.29)+key.color*(.2+.6*saturate(dot(n,key.direction)));
    #if defined(_ADDITIONAL_LIGHTS)
    uint count=GetAdditionalLightsCount();for(uint i=0;i<count;i++){Light l=GetAdditionalLight(i,v.world);light+=l.color*l.distanceAttenuation*(.18+.55*saturate(dot(n,l.direction)));}
    #endif
    return half4(_Tint.rgb*v.colour.rgb*(light*_LightResponse+_Energy),1);
   }
   ENDHLSL
  }
 }
}
