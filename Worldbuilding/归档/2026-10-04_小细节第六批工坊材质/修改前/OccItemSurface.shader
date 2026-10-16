Shader "OCC/UI/ItemSurface"
{
 Properties
 {
  [PerRendererData] _MainTex("Sprite",2D)="white"{}
  _Color("Tint",Color)=(1,1,1,1)
  _Phase("Phase",Float)=-2
  _Glass("Glass",Float)=0
  _Rough("Roughness",Float)=0
  _SideLight("Material side light",Float)=0
  _StencilComp("Stencil Comparison",Float)=8
  _Stencil("Stencil ID",Float)=0
  _StencilOp("Stencil Operation",Float)=0
  _StencilWriteMask("Stencil Write Mask",Float)=255
  _StencilReadMask("Stencil Read Mask",Float)=255
  _ColorMask("Color Mask",Float)=15
  [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Use Alpha Clip",Float)=0
 }
 SubShader
 {
  Tags{"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="False"}
  Stencil{Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask]}
  Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
  Blend SrcAlpha OneMinusSrcAlpha ColorMask [_ColorMask]
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
   #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
   #include "UnityCG.cginc"
   #include "UnityUI.cginc"
   struct appdata{float4 vertex:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
   struct v2f{float4 vertex:SV_POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;float4 world:TEXCOORD1;};
   sampler2D _MainTex;float4 _MainTex_TexelSize,_Color,_ClipRect;float _Phase,_Glass,_Rough,_SideLight;
   v2f vert(appdata v){v2f o;o.world=v.vertex;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o;}
   float4 frag(v2f i):SV_Target
   {
    float4 original=tex2D(_MainTex,i.uv);float4 c=original;
    // Static left-side material light respects the original dark outline and alpha.
    float luma=dot(original.rgb,float3(.299,.587,.114));
    float side=saturate(1-i.uv.x)*step(.20,luma)*original.a*_SideLight;
    c.rgb+=side*float3(1,.90,.72);
    if(_Phase>=0 && _Phase<=1)
    {
     float band=saturate(1-abs(i.uv.x+i.uv.y*.20-(_Phase*1.5-.25))/.075);
     // One native texel refraction only inside the lenses; never move the frame or alpha contour.
     float2 left=(i.uv-float2(.328125,.421875))/float2(.0625,.09375);
     float2 right=(i.uv-float2(.671875,.265625))/float2(.0625,.09375);
     float lens=step(min(dot(left,left),dot(right,right)),.70);
     if(_Glass>.5 && lens>.5 && luma>.01)
     {
      float2 offset=float2((_Phase<.5?-1:1)*_MainTex_TexelSize.x,0);
      float4 refracted=tex2D(_MainTex,i.uv+offset);
      c.rgb=lerp(c.rgb,refracted.rgb,.35*refracted.a*band);
     }
     float metalHighlight=step(.35,dot(original.rgb,float3(.299,.587,.114)))*saturate(1-_Rough*4);
     c.rgb+=band*.12*original.a*(_Glass>.5?lens*step(.01,luma):metalHighlight);
    }
    c*=i.color;c.a=original.a*i.color.a;
    #ifdef UNITY_UI_CLIP_RECT
    c.a*=UnityGet2DClipping(i.world.xy,_ClipRect);
    #endif
    #ifdef UNITY_UI_ALPHACLIP
    clip(c.a-.001);
    #endif
    return c;
   }
   ENDCG
  }
 }
}
