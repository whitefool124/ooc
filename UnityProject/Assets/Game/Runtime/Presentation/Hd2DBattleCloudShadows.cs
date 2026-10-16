using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OCC.Combat.Presentation
{
    // Numeric light transmittance, not pixel artwork or a ground-colour overlay.
    // The cookie belongs exclusively to the owned outdoor key light.
    public sealed class Hd2DBattleCloudShadows : MonoBehaviour
    {
        private const int Resolution=128;
        private Light sun;
        private UniversalAdditionalLightData lightData;
        private Texture2D cookie;
        private Texture previousCookie;
        private Vector2 previousSize,previousOffset,span,velocity;
        private Vector3 centre;
        private byte[] coverage,transmittance;
        private float opacity,baseOpacity,phase;
        private bool initialized,visible=true;
        public bool Visible=>visible;

        internal void Initialize(Light source,Transform stage,int width,int height,Hd2DBattlefieldProfile profile)
        {
            if(initialized)throw new InvalidOperationException("Cloud shadows already initialized");
            if(source==null||source.type!=LightType.Directional||!profile.OutdoorClouds)
                throw new InvalidOperationException("Cloud shadows require an owned outdoor directional light");
            var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if(pipeline==null||!pipeline.supportsLightCookies)
                throw new InvalidOperationException("Current battle renderer does not support light cookies");
            sun=source;lightData=sun.GetUniversalAdditionalLightData();
            previousCookie=sun.cookie;previousSize=lightData.lightCookieSize;previousOffset=lightData.lightCookieOffset;
            span=new Vector2(Mathf.Max(20,width+14),Mathf.Max(28,height+18));
            velocity=profile.CloudVelocity;baseOpacity=profile.CloudOpacity;
            centre=stage.TransformPoint(new Vector3(width*.5f,12,height*.5f));
            coverage=new byte[Resolution*Resolution];transmittance=new byte[coverage.Length];
            for(int y=0;y<Resolution;y++)for(int x=0;x<Resolution;x++)
            {
                // Integer-frequency waves give a seamless, wide cloud bank.
                // Continuous light can be smooth while source pixels stay crisp.
                float u=(x+.5f)/Resolution*Mathf.PI*2,v=(y+.5f)/Resolution*Mathf.PI*2;
                float field=.5f+.22f*Mathf.Cos(u+.35f*Mathf.Sin(v))+
                    .12f*Mathf.Cos(2*u-v+.55f*Mathf.Sin(2*v))+.055f*Mathf.Cos(u+3*v+1.3f);
                float bank=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.23f,.78f,field));
                coverage[y*Resolution+x]=(byte)Mathf.RoundToInt(bank*255);
            }
            cookie=new Texture2D(Resolution,Resolution,TextureFormat.R8,false,true)
                {name="Owned numeric outdoor cloud transmittance",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Bilinear};
            initialized=true;opacity=-1;SetAtmosphere(0,0,0);
        }

        internal void SetAtmosphere(float sunset,float night,float rain)
        {
            if(!initialized)return;
            float strength=baseOpacity*(1-Mathf.Clamp01(night))*Mathf.Lerp(1,.55f,Mathf.Clamp01(sunset))*
                Mathf.Lerp(1,1.2f,Mathf.Clamp01(rain));
            if(Mathf.Abs(strength-opacity)<.0005f)return;
            opacity=Mathf.Clamp(strength,0,.2f);
            if(opacity<=.001f){Detach();return;}
            // Upload only on a time/weather transition. Steady-state motion
            // updates two projection coordinates without rebuilding any texture.
            for(int i=0;i<coverage.Length;i++)
                transmittance[i]=(byte)Mathf.RoundToInt(255-coverage[i]*opacity);
            cookie.SetPixelData(transmittance,0);cookie.Apply(false,false);
            if(visible&&isActiveAndEnabled)Attach();
        }
        internal void SetVisible(bool value)
        {
            visible=value;if(!initialized)return;
            if(visible&&opacity>.001f&&isActiveAndEnabled)Attach();else Detach();
        }
        private void Update()
        {
            if(!initialized||!visible||opacity<=.001f||sun==null||!sun.isActiveAndEnabled)return;
            phase+=Time.unscaledDeltaTime;
            MoveProjection();
        }
        private void Attach()
        {if(sun==null||cookie==null)return;sun.cookie=cookie;lightData.lightCookieSize=span;MoveProjection();}
        private void MoveProjection()
        {
            var anchor=sun.transform.InverseTransformPoint(centre);
            lightData.lightCookieOffset=new Vector2(anchor.x+Mathf.Repeat(phase*velocity.x,span.x),
                anchor.y+Mathf.Repeat(phase*velocity.y,span.y));
        }
        private void Detach()
        {
            if(sun==null||sun.cookie!=cookie)return;
            sun.cookie=previousCookie;
            if(lightData!=null){lightData.lightCookieSize=previousSize;lightData.lightCookieOffset=previousOffset;}
        }
        private void OnEnable(){if(initialized&&visible&&opacity>.001f)Attach();}
        private void OnDisable()=>Detach();
        private void OnDestroy(){Detach();initialized=false;if(cookie!=null)Destroy(cookie);cookie=null;coverage=null;transmittance=null;}
    }
}
