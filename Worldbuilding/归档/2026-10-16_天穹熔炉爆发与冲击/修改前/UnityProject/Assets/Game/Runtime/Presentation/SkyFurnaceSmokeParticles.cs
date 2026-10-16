using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace OCC.Combat.Presentation
{
    /// <summary>Native Particle System emission and simulation; timeline seeks are deterministic.</summary>
    public sealed class SkyFurnaceSmokeParticles
    {
        public readonly ParticleSystem[] Systems=new ParticleSystem[2];
        public readonly ParticleSystemRenderer[] Renderers=new ParticleSystemRenderer[2];
        public readonly List<MeshRenderer> ImageRenderers=new List<MeshRenderer>();
        private readonly MeshRenderer[,] cards=new MeshRenderer[2,48];
        private readonly Mesh[] frames;
        private readonly MaterialPropertyBlock block=new MaterialPropertyBlock();
        public int PeakParticles {get;private set;}
        public const float CanvasMetres=.75f,PixelMetres=CanvasMetres/32;
        
        private readonly ParticleSystem.Particle[] buffer=new ParticleSystem.Particle[48];
        public int FramesSeenMask {get;private set;}
        public float PeakRadius {get;private set;}
        public float PeakHeight {get;private set;}
        private readonly Vector3 origin;
        public int Count=>Systems[0].particleCount+Systems[1].particleCount;
        public int Fingerprint()
        {
            int hash=17;unchecked{foreach(var ps in Systems){var data=new ParticleSystem.Particle[ps.main.maxParticles];int count=ps.GetParticles(data);hash=hash*31+count;for(int i=0;i<count;i++){hash=hash*31+data[i].position.GetHashCode();hash=hash*31+data[i].remainingLifetime.GetHashCode();}}}return hash;
        }
        private float last=-1;
        public SkyFurnaceSmokeParticles(Transform parent,int layer,Vector3 centre,Mesh[] papers,Material material,float impact)
        {
            origin=centre;frames=papers;
            for(int i=0;i<2;i++)
            {
                bool dust=i==0;var node=new GameObject(dust?"粒子系统 · 贴地烟尘":"粒子系统 · 上升烟缕"){layer=layer};node.transform.SetParent(parent,false);node.transform.position=centre+Vector3.up*(dust?.1875f:.25f);node.transform.rotation=Quaternion.Euler(-90,0,0);
                var ps=node.AddComponent<ParticleSystem>();Systems[i]=ps;ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);ps.useAutoRandomSeed=false;ps.randomSeed=(uint)(7823+i*7919);
                var main=ps.main;main.loop=false;main.duration=16;main.playOnAwake=false;main.maxParticles=48;main.simulationSpace=ParticleSystemSimulationSpace.World;main.scalingMode=ParticleSystemScalingMode.Shape;main.useUnscaledTime=true;
                main.startLifetime=new ParticleSystem.MinMaxCurve(dust?1.6f:2.4f,dust?2.25f:3.2f);main.startSpeed=new ParticleSystem.MinMaxCurve(dust?.06f:.12f,dust?.12f:.22f);
                main.startSize3D=true;main.startSizeX=1;main.startSizeY=1;main.startSizeZ=1;main.startColor=Color.white;main.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;
                var shape=ps.shape;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=dust?78:12;shape.radius=dust?.35f:.16f;shape.radiusThickness=1;
                var velocity=ps.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;velocity.x=0;velocity.y=dust?.025f:.025f;velocity.z=0;velocity.radial=dust?.18f:.025f;
                var noise=ps.noise;noise.enabled=true;noise.strength=dust?.025f:.04f;noise.frequency=.65f;noise.scrollSpeed=.12f;noise.quality=ParticleSystemNoiseQuality.Low;
                var colour=ps.colorOverLifetime;colour.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(dust?.36f:.30f,.12f),new GradientAlphaKey(dust?.24f:.23f,.55f),new GradientAlphaKey(0,1)});colour.color=gradient;
                var emission=ps.emission;emission.enabled=true;emission.rateOverTime=0;emission.SetBursts(dust?new[]{new ParticleSystem.Burst(impact+.12f,8),new ParticleSystem.Burst(impact+.38f,4)}:new[]{new ParticleSystem.Burst(impact+.55f,6),new ParticleSystem.Burst(impact+1.1f,4)});
                var renderer=node.GetComponent<ParticleSystemRenderer>();Renderers[i]=renderer;renderer.renderMode=ParticleSystemRenderMode.Mesh;renderer.mesh=papers[0];renderer.enabled=false;renderer.sharedMaterial=material;renderer.alignment=ParticleSystemRenderSpace.View;renderer.sortMode=ParticleSystemSortMode.Distance;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.enableGPUInstancing=false;renderer.minParticleSize=0;renderer.maxParticleSize=1;
                renderer.SetActiveVertexStreams(new List<ParticleSystemVertexStream>{ParticleSystemVertexStream.Position,ParticleSystemVertexStream.Normal,ParticleSystemVertexStream.Color,ParticleSystemVertexStream.UV});
                // Transparent seal layers use orders0..3. Foreground smoke must composite after all four.
                renderer.sortingOrder=8;
                for(int j=0;j<48;j++)
                {
                    var image=new GameObject("原生粒子图片 "+i+"/"+j){layer=layer};image.transform.SetParent(parent,false);image.AddComponent<MeshFilter>().sharedMesh=papers[0];var card=image.AddComponent<MeshRenderer>();card.sharedMaterial=material;card.shadowCastingMode=ShadowCastingMode.Off;card.sortingOrder=8;cards[i,j]=card;ImageRenderers.Add(card);image.SetActive(false);
                }
            }
        }
        public void Evaluate(float time)
        {
            bool restart=last<0||time<last||time-last>.25f;float advance=restart?time:Mathf.Max(0,time-last);
            for(int system=0;system<Systems.Length;system++)
            {
                var ps=Systems[system];
                if(restart)
                {
                    ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);float elapsed=0;bool first=true;
                    while(elapsed<time){float step=Mathf.Min(1f/60,time-elapsed);ps.Simulate(step,false,first,false);elapsed+=step;first=false;}
                    if(first)ps.Simulate(0,false,true,false);
                }
                else ps.Simulate(advance,false,false,false);
                int count=ps.GetParticles(buffer);
                for(int i=0;i<count;i++)
                {
                    float life=1-buffer[i].remainingLifetime/buffer[i].startLifetime;
                    // Deliberate exposure: quick birth, longer curl, then fragmentation and empty frame.
                    int frame=life<.08f?0:life<.20f?1:life<.36f?2:life<.54f?3:life<.68f?4:life<.80f?5:life<.92f?6:7;
                    FramesSeenMask|=1<<frame;
                    var image=cards[system,i];image.gameObject.SetActive(frame<7);image.GetComponent<MeshFilter>().sharedMesh=frames[frame];image.transform.SetPositionAndRotation(buffer[i].position,Quaternion.Euler(45,0,0));block.Clear();block.SetFloat("_Opacity",buffer[i].GetCurrentColor(ps).a/255f);image.SetPropertyBlock(block);
                    var delta=buffer[i].position-origin;PeakRadius=Mathf.Max(PeakRadius,new Vector2(delta.x,delta.z).magnitude+CanvasMetres*.5f);PeakHeight=Mathf.Max(PeakHeight,delta.y+CanvasMetres*.5f);
                }
                for(int i=count;i<48;i++)cards[system,i].gameObject.SetActive(false);
            }
            last=time;PeakParticles=Mathf.Max(PeakParticles,Count);
        }
        public void Clear(){foreach(var ps in Systems)ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);last=-1;foreach(var image in ImageRenderers)image.gameObject.SetActive(false);}
    }
}
