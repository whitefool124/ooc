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
            float flameAt(float2 p,float2 center,float age)
            {
                float result=0;
                [unroll] for(int j=0;j<5;j++) {
                    float f=j-2,life=max(0,1-age/1.5);
                    float2 tip=center+float2(f*5,9+age*14+sin(f*2+age*12)*3);
                    float2 q=abs((p-tip)/float2((10-abs(f)*2)*life+1,16*life+1));
                    result=max(result,step(q.x+q.y,1));
                }
                return result*step(age,1.5);
            }
            float sparksAt(float2 p,float2 origin,float age)
            {
                float result=0;
                [unroll] for(int j=0;j<18;j++) {
                    float f=j,angle=f*2.399,velocity=22+hash(float2(f,3))*26;
                    float2 q=floor(origin+float2(cos(angle)*velocity,sin(angle)*velocity+22)*age-float2(0,age*age*25));
                    result=max(result,step(max(abs(p.x-q.x),abs(p.y-q.y)),.8)*step(age,1.05));
                }
                return result;
            }
            float4 frag(v2f_img input):SV_Target
            {
                float2 p=floor(input.uv*float2(240,180))+.5;
                float3 c=scene(p); if(_Original>.5) return float4(c,1);
                float t=max(_Age,0),active=step(0,_Age)*step(t,2.39);
                float3 cyan=float3(.24,.85,.9),white=float3(.87,1,.93),orange=float3(1,.33,.035),gold=float3(1,.83,.28);
                if(_Mode<1.5)
                {
                    float impactAge=_Mode==0?max(t-.55,0):t;
                    float impactOn=_Mode==0?step(.55,t):1;
                    float pulse=max(0,1-impactAge/1.3)*impactOn;
                    float3 energy=lightAt(p,_Hit,orange,4*pulse,85);
                    c=c*.8+c*energy+energy*.14;
                    if(_Mode==0 && t<.55) {
                        float2 origin=float2(48,93),head=floor(lerp(origin,_Hit,saturate(t/.55)));
                        float2 delta=p-head,dir=normalize(_Hit-origin+.01);
                        float along=dot(delta,dir),across=abs(delta.x*dir.y-delta.y*dir.x);
                        float trail=step(-19,along)*step(along,1)*step(across,2);
                        c=lerp(c,orange,trail); c=lerp(c,gold,step(length(delta),3));
                        c+=lightAt(p,head,gold,2,42)*.3;
                    }
                    float plume=flameAt(p,_Hit,impactAge)*impactOn;
                    c=lerp(c,orange,plume);
                    c=lerp(c,gold,flameAt(p,_Hit+float2(0,-2),impactAge+.32)*impactOn);
                    c=lerp(c,white,step(length(p-_Hit),max(0,7-impactAge*16))*impactOn);
                    c=lerp(c,gold,sparksAt(p,_Hit,impactAge)*impactOn);
                    float ground=length((p-_Hit)/float2(1,.45));
                    c=lerp(c,orange,step(abs(ground-impactAge*33),1)*pulse*.7);
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
                    float dist=length(p-_Hit),radius=t*85;
                    float band=step(abs(dist-radius),5)*max(0,1-t*.8);
                    c=scene(p+floor(normalize(p-_Hit+.001)*3*band+.5));
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
                    } else if(_Mode==4) {
                        float2 q=p-_Hit;
                        float envelope=step(0,q.y)*step(q.y,65)*max(0,1-abs(q.x)/24);
                        float shift=sin(floor(q.y/4)*1.5-clock*8)*3*envelope;
                        c=scene(p+float2(floor(shift+.5),0));
                        c+=lightAt(p,_Hit,orange,1.6,65)*.15;
                        c=lerp(c,orange,flameAt(p,_Hit,fmod(clock,1.1))*.65);
                    } else if(_Mode==6) {
                        float pulse=max(0,1-t/1.1),arc=0;
                        [unroll] for(int j=0;j<6;j++) {
                            float angle=j*6.283185/6,cs=cos(angle),sn=sin(angle);
                            float2 delta=p-_Hit;float along=delta.x*cs+delta.y*sn,across=-delta.x*sn+delta.y*cs;
                            float zig=floor((hash(float2(floor(along/5),j+floor(t*14)))-.5)*9);
                            arc=max(arc,step(abs(across-zig),.85)*step(0,along)*step(along,48*pulse+8));
                        }
                        float3 energy=lightAt(p,_Hit,float3(.44,.46,1),5*pulse,85);
                        c=c*.7+c*energy+energy*.2;
                        c=lerp(c,float3(.58,.66,1),arc*step(t,1.1));
                        c=lerp(c,white,step(length(p-_Hit),4*pulse)*step(t,1.1));
                    }
                }
                return float4(saturate(c),1);
            }
            ENDHLSL
        }
    }
}
