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
        public int PeakParticles {get;private set;}
        public int Count=>Systems[0].particleCount+Systems[1].particleCount;
        public int Fingerprint()
        {
            int hash=17;unchecked{foreach(var ps in Systems){var data=new ParticleSystem.Particle[ps.main.maxParticles];int count=ps.GetParticles(data);hash=hash*31+count;for(int i=0;i<count;i++){hash=hash*31+data[i].position.GetHashCode();hash=hash*31+data[i].remainingLifetime.GetHashCode();}}}return hash;
        }
        private float last=-1;
        public SkyFurnaceSmokeParticles(Transform parent,int layer,Vector3 centre,Mesh paper,Material material,float impact)
        {
            for(int i=0;i<2;i++)
            {
                bool dust=i==0;var node=new GameObject(dust?"粒子系统 · 贴地烟尘":"粒子系统 · 上升烟缕"){layer=layer};node.transform.SetParent(parent,false);node.transform.position=centre+Vector3.up*(dust?.1875f:.25f);node.transform.rotation=Quaternion.Euler(-90,0,0);
                var ps=node.AddComponent<ParticleSystem>();Systems[i]=ps;ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);ps.useAutoRandomSeed=false;ps.randomSeed=(uint)(7823+i*7919);
                var main=ps.main;main.loop=false;main.duration=16;main.playOnAwake=false;main.maxParticles=48;main.simulationSpace=ParticleSystemSimulationSpace.World;main.scalingMode=ParticleSystemScalingMode.Shape;main.useUnscaledTime=true;
                main.startLifetime=new ParticleSystem.MinMaxCurve(dust?2.6f:4.5f,dust?3.8f:5.8f);main.startSpeed=new ParticleSystem.MinMaxCurve(dust?.12f:.38f,dust?.22f:.68f);
                main.startSize3D=true;main.startSizeX=1;main.startSizeY=1;main.startSizeZ=1;main.startColor=Color.white;main.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;
                var shape=ps.shape;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=dust?78:12;shape.radius=dust?.55f:.22f;shape.radiusThickness=1;
                var velocity=ps.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;velocity.x=0;velocity.y=dust?.035f:.14f;velocity.z=0;velocity.radial=dust?.65f:.06f;
                var noise=ps.noise;noise.enabled=true;noise.strength=dust?.055f:.085f;noise.frequency=.65f;noise.scrollSpeed=.12f;noise.quality=ParticleSystemNoiseQuality.Low;
                var colour=ps.colorOverLifetime;colour.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(dust?.36f:.30f,.12f),new GradientAlphaKey(dust?.24f:.23f,.55f),new GradientAlphaKey(0,1)});colour.color=gradient;
                var emission=ps.emission;emission.enabled=true;emission.rateOverTime=0;emission.SetBursts(dust?new[]{new ParticleSystem.Burst(impact+.24f,12),new ParticleSystem.Burst(impact+.54f,6)}:new[]{new ParticleSystem.Burst(impact+.8f,8),new ParticleSystem.Burst(impact+1.5f,6)});
                var renderer=node.GetComponent<ParticleSystemRenderer>();Renderers[i]=renderer;renderer.renderMode=ParticleSystemRenderMode.Mesh;renderer.mesh=paper;renderer.sharedMaterial=material;renderer.alignment=ParticleSystemRenderSpace.View;renderer.sortMode=ParticleSystemSortMode.Distance;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.enableGPUInstancing=false;renderer.minParticleSize=0;renderer.maxParticleSize=1;
                renderer.SetActiveVertexStreams(new List<ParticleSystemVertexStream>{ParticleSystemVertexStream.Position,ParticleSystemVertexStream.Normal,ParticleSystemVertexStream.Color,ParticleSystemVertexStream.UV});
            }
        }
        public void Evaluate(float time)
        {
            bool restart=last<0||time<last||time-last>.25f;float advance=restart?time:Mathf.Max(0,time-last);
            foreach(var ps in Systems)ps.Simulate(advance,false,restart,true);
            last=time;PeakParticles=Mathf.Max(PeakParticles,Count);
        }
        public void Clear(){foreach(var ps in Systems)ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);last=-1;}
    }
}
