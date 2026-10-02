Shader "OCC/Pixel VFX Showcase"
{
    Properties { _MainTex("Scene",2D)="white"{} _UnitTex("Unit mask",2D)="black"{} _EnemyTex("Enemy mask",2D)="black"{} }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            HLSLPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _MainTex, _UnitTex, _EnemyTex;
            float _Mode, _Age, _Clock, _Original, _Strength;
            float2 _Hit;
            float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float4 unitAt(float2 p)
            {
                float2 u=(p-float2(88,57))/float2(64,64);
                if(any(u<0)||any(u>1)) return 0;
                return tex2D(_UnitTex,u);
            }
            float enemyAt(float2 p,float2 origin)
            {
                float2 u=(p-origin)/float2(32,64);
                if(any(u<0)||any(u>1)) return 0;
                return tex2D(_EnemyTex,u).a;
            }
            float maskAt(float2 p) { return max(unitAt(p).a,max(enemyAt(p,float2(40,65)),enemyAt(p,float2(178,45)))); }
            float3 scene(float2 p) { return tex2D(_MainTex,saturate(p/float2(240,180))).rgb; }
            // A screen-space bevel/rim approximation; no authored normal map is implied.
            float3 normalAt(float2 p)
            {
                float2 tile=fmod(p,32);
                float2 bevel=float2(step(29,tile.x)-step(tile.x,2),step(29,tile.y)-step(tile.y,2));
                float2 edge=float2(maskAt(p+float2(-1,0))-maskAt(p+float2(1,0)),maskAt(p+float2(0,-1))-maskAt(p+float2(0,1)));
                return normalize(float3(lerp(bevel*.8,edge*1.8,maskAt(p)),1));
            }
            float visibility(float2 p,float2 light)
            {
                float blocked=0;
                [unroll] for(int i=1;i<=12;i++) blocked=max(blocked,maskAt(lerp(p,light,(float)i/13)));
                return lerp(1-blocked*.8,1,maskAt(p));
            }
            float3 lightAt(float2 p,float2 light,float3 tint,float strength,float radius)
            {
                float2 delta=light-p; float fall=max(0,1-length(delta)/radius); fall*=fall;
                float facing=.35+.65*max(0,dot(normalAt(p),normalize(float3(delta,25))));
                return tint*floor(fall*facing*strength*_Strength*7)/7*visibility(p,light);
            }
            float4 frag(v2f_img input):SV_Target
            {
                float2 p=floor(input.uv*float2(240,180))+.5;
                float3 c=scene(p); if(_Original>.5) return float4(c,1);
                float t=max(_Age,0),active=step(0,_Age)*step(t,2.39);
                float3 cyan=float3(.24,.85,.9),white=float3(.87,1,.93),dark=float3(.1,.36,.48);
                float2 d=(p-float2(120,86))/float2(27,38); float r=length(d);
                float ring=step(.94,r)*step(r,1.04),inside=step(r,.94);
                if(_Mode<1.5)
                {
                    float loss=_Mode==1?smoothstep(.15,.8,t):0;
                    float keep=step(loss,hash(floor(p/3)));
                    float grid=max(1-step(1,fmod(p.x+floor(p.y/5)*3,8)),1-step(1,fmod(p.y,5)));
                    c=lerp(c,dark,.13*inside*keep); c=lerp(c,cyan,.17*grid*inside*keep);
                    c=lerp(c,cyan,ring*.8*keep);
                    float wave=step(abs(length(p-_Hit)-t*67),1.3)*inside*max(0,1-t*1.65)*active;
                    c=lerp(c,white,wave);
                    if(_Mode==1) {
                        [unroll] for(int i=0;i<20;i++) {
                            float f=i,a=f*6.283185/20,q=max(t-.18,0);
                            float2 start=float2(120,86)+float2(cos(a)*27,sin(a)*38);
                            float2 pos=floor(start+float2(cos(a),sin(a))*q*(18+hash(float2(f,1))*22)-float2(0,q*q*16));
                            c=lerp(c,cyan,step(max(abs(p.x-pos.x),abs(p.y-pos.y)),1.5)*step(.18,t)*step(t,1.25));
                        }
                    }
                }
                if(_Mode==2) {
                    float dissolve=smoothstep(.2,1.5,t),threshold=(121-p.y)/64*.65+hash(floor(p/2))*.35;
                    float gone=step(threshold,dissolve)*unitAt(p).a;
                    // Reveal the identical tile underneath the unit at a neighbouring row.
                    c=lerp(c,scene(float2(p.x,p.y-64)),gone);
                    c=lerp(c,white,step(abs(threshold-dissolve),.06)*unitAt(p).a*step(.01,dissolve));
                    [unroll] for(int i=0;i<24;i++) {
                        float f=i; float2 start=float2(106+hash(float2(f,2))*28,60+hash(float2(f,3))*56);
                        float q=max(t-(start.y-57)/64*.6-.2,0);
                        float2 pos=floor(start+float2((hash(float2(f,5))-.5)*28,24)*q);
                        c=lerp(c,cyan,step(max(abs(p.x-pos.x),abs(p.y-pos.y)),.8)*step(.01,q)*step(q,.8));
                    }
                }
                if(_Mode==3 && active>.5) {
                    float dist=length(p-float2(120,77)),radius=t*85;
                    float band=step(abs(dist-radius),5)*max(0,1-t*.8);
                    c=scene(p+floor(normalize(p-float2(120,77)+.001)*3*band+.5));
                    c=lerp(c,white,step(abs(dist-radius),.8)*max(0,1-t*.8)*.8);
                }
                if(_Mode>=4) {
                    float clock=_Clock;
                    float2 light=float2(120+sin(clock*1.35-.9)*67,87+cos(clock*1.35-.9)*24);
                    if(_Mode==5) {
                        float3 energy=lightAt(p,light,float3(1,.58,.23),2.3,100);
                        c=c*.48+c*energy*1.4+energy*.17;
                        c=lerp(c,float3(1,.91,.61),step(length(p-light),2.8));
                        c+=float3(1,.42,.1)*floor(max(0,1-length(p-light)/13)*5)/5*.38;
                    } else {
                        float pulse=exp(-t*2.1),ripple=sin(length(p-_Hit)*.4-clock*11)*pulse;
                        float2 offset=floor(d*(8*(1-min(r,1))*inside)+normalize(p-_Hit+.01)*ripple*3*inside+.5);
                        c=scene(p+offset);
                        if(_Mode==6) {
                            float3 energy=lightAt(p,_Hit,cyan,1+pulse*4,88)+lightAt(p,float2(120,60),cyan,.6,64);
                            c=c*.62+c*energy*1.4+energy*.11;
                            c=lerp(c,white,step(abs(length(p-_Hit)-t*52),1)*inside*max(0,1-t*.8)*.9);
                            float ground=length((p-float2(120,53))/float2(29,7));
                            c+=cyan*step(abs(ground-1),.09)*(.35+pulse*.4);
                        }
                        float fresnel=pow(min(r,1),5)*inside;
                        float sweep=step(abs(d.x*.75+d.y*.35-sin(clock*1.5)*.6),.045)*inside;
                        c=lerp(c,cyan,.1*inside+.18*fresnel); c=lerp(c,cyan,ring*.8); c=lerp(c,white,sweep*.65);
                        if(_Mode==6) {
                            c+=cyan*floor(max(0,1-abs(r-1)/.28)*5)/5*(.16+pulse*.18);
                            c=lerp(c,white,step(length(p-_Hit),2+pulse*3)*(.3+pulse*.7));
                        }
                    }
                }
                return float4(saturate(c),1);
            }
            ENDHLSL
        }
    }
}
