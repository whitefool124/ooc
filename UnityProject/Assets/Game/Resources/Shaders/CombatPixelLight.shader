Shader "OCC/Combat Pixel Light"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Tint ("Tint", Color) = (1,0.7,0.24,1)
        _Progress ("Progress", Float) = 0
        _Family ("Family", Float) = 0
        _Intensity ("Intensity", Float) = 1
        _NativePixels ("Native Pixels", Float) = 96
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha One
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex:POSITION; fixed4 color:COLOR; float4 uv:TEXCOORD0; float4 data:TEXCOORD1; };
            struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float4 uv:TEXCOORD0; float4 data:TEXCOORD1; float4 world:TEXCOORD2; };
            float4 _ClipRect, _Tint;
            float _Progress, _Family, _Intensity, _NativePixels;
            v2f vert(appdata v) { v2f o; o.world=v.vertex; o.vertex=UnityObjectToClipPos(v.vertex); o.color=v.color; o.uv=v.uv; o.data=v.data; return o; }
            fixed4 frag(v2f i):SV_Target
            {
                float n=max(32,_NativePixels);
                float2 p=(floor(i.uv.xy*n)+.5)/n-.5;
                float t=floor(saturate(_Progress)*8)/8;
                float family=_Family;
                float envelope=(1-t)*(1-t);
                float radius=family==3 ? .2 : .36;
                float light=floor(saturate(1-length(p)/radius)*6)/6 * .26 * envelope;
                float sparks=0;
                [unroll] for(int k=0;k<8;k++)
                {
                    float a=k*.785398 + family*.25;
                    float2 dir=float2(cos(a),sin(a));
                    float2 q=dir*(.04+t*.31);
                    if(family==1) q.y+=t*t*.09;
                    if(family==2) q=dir*(.3-t*.24);
                    float2 delta=abs(p-q);
                    sparks=max(sparks, step(max(delta.x,delta.y),1.0/n)*envelope);
                }
                float diamond=abs(p.x)+abs(p.y);
                float wave=step(abs(diamond-(.06+t*.38)),1/n)*envelope*.6;
                float core=step(abs(p.x-p.y*.55),1/n)*step(abs(p.y),.08*(1-t))*envelope;
                float alpha=saturate(light+sparks+wave+core)*_Intensity;
                #ifdef UNITY_UI_CLIP_RECT
                alpha*=UnityGet2DClipping(i.world.xy,_ClipRect);
                #endif
                return fixed4(lerp(_Tint.rgb,float3(1,1,1),saturate(sparks+core)*.65),alpha);
            }
            ENDCG
        }
    }
}
