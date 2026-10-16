Shader "OCC/UI/PixelStagePresentation"
{
    Properties { [PerRendererData] _MainTex("Native lit scene",2D)="white"{} _Clock("Time",Float)=0 _Motion("Motion",Float)=1 _Zoom("Zoom",Float)=1 _Focus("Focus",Float)=0 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct a {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
            struct v {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
            sampler2D _MainTex;float _Clock,_Motion,_Zoom,_Focus;
            v vert(a i){v o;o.vertex=UnityObjectToClipPos(i.vertex);o.uv=i.uv;o.color=i.color;return o;}
            float pixel(float2 p,float2 lo,float2 hi){return step(lo.x,p.x)*step(lo.y,p.y)*step(p.x,hi.x)*step(p.y,hi.y);}
            fixed4 frag(v i):SV_Target
            {
                float2 uv=(floor(i.uv*float2(320,180))+.5)/float2(320,180);
                fixed4 c=tex2D(_MainTex,uv);
                // Lighting and shadows already came from the low-resolution scene.
                // This pass only presents it and draws sparse pixel-grid atmosphere.
                float2 centre=round(lerp(float2(160,90),float2(154,36),_Focus));
                float2 p=floor(centre+(uv-.5)*float2(320,180)/max(1,_Zoom));
                float t=floor(_Clock*12)/12;
                for(int k=0;k<3;k++)
                {
                    float phase=frac(t*.2+k*.3333);
                    float2 s=float2(242+floor(sin(phase*5+k)*2),49+floor(phase*10));
                    c.rgb+=pixel(p,s,s+float2(0,1))*sin(phase*3.14159)*.10*_Motion;
                }
                for(int n=0;n<7;n++)
                {
                    float x=n<4?174+n*9:230+(n-4)*9;
                    float2 s=float2(x+floor(sin(t*.38+n)*2),89+(n%2)*3);
                    float water=step(c.r+.08,c.b)*step(c.g+.05,c.b);
                    c.rgb+=pixel(p,s,s+float2(2+n%3,0))*water*(.025+.015*sin(t*.8+n))*_Motion;
                }
                return c*i.color;
            }
            ENDCG
        }
    }
}
