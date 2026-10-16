Shader "OCC/UI/MainMenuPixelScene"
{
    Properties
    {
        [PerRendererData] _MainTex ("Closed scene", 2D) = "white" {}
        _OpenTex ("Open scene", 2D) = "white" {}
        _Open ("Book open", Range(0,1)) = 0
        _Zoom ("Camera zoom", Float) = 1
        _Focus ("Camera focus", Range(0,1)) = 0
        _Clock ("Atmosphere time", Float) = 0
        _Motion ("Motion", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="False" }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            sampler2D _MainTex, _OpenTex;
            float _Open, _Zoom, _Focus, _Clock, _Motion;
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; return o; }
            float box(float2 p, float2 lo, float2 hi) { return step(lo.x,p.x)*step(lo.y,p.y)*step(p.x,hi.x)*step(p.y,hi.y); }
            fixed4 frag(v2f i):SV_Target
            {
                // Art is assembled from intact decoded subjects on a shared native grid.
                float2 grid=float2(320,180);
                float2 outputUV=(floor(i.uv*grid)+.5)/grid;
                float2 focus=lerp(float2(.5,.5),float2(.528125,.12),_Focus);
                float2 uv=saturate(focus+(outputUV-.5)/max(1,_Zoom));
                uv=(floor(uv*grid)+.5)/grid;
                fixed4 closed=tex2D(_MainTex,uv);
                fixed4 opened=tex2D(_OpenTex,uv);
                // Only the book and its desktop contact transition, never the whole vista.
                float edge=min(min(uv.x-.34,.72-uv.x),min(uv.y-.025,.28-uv.y));
                float book=saturate(edge*120);
                fixed4 c=lerp(closed,opened,_Open*book);
                float t=floor(_Clock*12)/12;
                float2 p=floor(uv*grid);
                float motion=_Motion*(1-saturate(_Open));
                // Legacy fallback; the layered stage bypasses this shader.
                float contact=step(length((p-float2(169,16))/float2(25,1.25)),1);
                contact=max(contact,step(length((p-float2(244,25))/float2(14,1.25)),1));
                contact=max(contact,step(length((p-float2(289,19))/float2(6,1.25)),1));
                c.rgb*=1-.16*contact;
                // Sparse moving reflected highlights within the lake, not over buildings.
                for(int n=0;n<7;n++)
                {
                    float x=208+n*9+floor(sin(t*.38+n)*2);
                    float y=77+(n%3)*3;
                    float ripple=box(p,float2(x,y),float2(x+2+(n%3),y));
                    c.rgb+=ripple*(.035+.025*sin(t*.8+n))*motion;
                }
                // Steam rises from the actual cup rim in quantized cells.
                for(int k=0;k<3;k++)
                {
                    float phase=frac(t*.2+k*.3333);
                    float2 steam=float2(242+floor(sin(phase*5+k)*2),43+floor(phase*10));
                    c.rgb+=box(p,steam,steam+float2(0,1))*sin(phase*3.14159)*.11*motion;
                }
                // Low-amplitude aether light and changing window illumination.
                float lantern=max(0,1-length((p-float2(289,37))/float2(6,11)));
                c.rgb+=float3(.015,.06,.07)*floor(lantern*4)/4*(.85+.15*sin(t*1.4))*motion;
                float light=box(p,float2(22,58),float2(71,125))*(.003*sin(t*.35));
                c.rgb+=float3(1,.86,.56)*light*motion;
                return c*i.color;
            }
            ENDCG
        }
    }
}
