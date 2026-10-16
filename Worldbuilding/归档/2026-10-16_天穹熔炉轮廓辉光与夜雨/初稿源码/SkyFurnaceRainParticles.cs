using UnityEngine;

namespace OCC.Combat.Presentation
{
    public sealed class SkyFurnaceRainParticles
    {
        public readonly ParticleSystem[] Systems=new ParticleSystem[2];
        public readonly ParticleSystemRenderer[] Renderers=new ParticleSystemRenderer[2];
        private float last=-1;
        public int Count=>Systems[0].particleCount+Systems[1].particleCount;
        private readonly ParticleSystem.Particle[] buffer=new ParticleSystem.Particle[512];
        public int Fingerprint(){int hash=Count;unchecked{foreach(var ps in Systems){int count=ps.GetParticles(buffer);for(int i=0;i<count;i++){hash=hash*31+buffer[i].position.GetHashCode();hash=hash*31+buffer[i].remainingLifetime.GetHashCode();}}}return hash;}
        public bool TextureFree=>Renderers[0].sharedMaterial.GetTexturePropertyNames().Length==0&&Renderers[1].sharedMaterial.GetTexturePropertyNames().Length==0;
        public SkyFurnaceRainParticles(Transform parent,int layer,Vector3 centre,Mesh column,Mesh cube,Material rain)
        {
            for(int i=0;i<2;i++)
            {
                bool drop=i==0;var ps=SkyFurnaceVoxelEmitter.Create(parent,layer,drop?"夜雨 · 一像素细雨柱":"触地雨花 · 一像素立方",centre+Vector3.up*(drop?6.7f:.07f),drop?column:cube,rain,(uint)(41983+i*37),drop?512:48);
                Systems[i]=ps;Renderers[i]=ps.GetComponent<ParticleSystemRenderer>();
                var main=ps.main;main.loop=true;main.startLifetime=drop?1.2f:.32f;main.startColor=drop?new Color(.33f,.47f,.65f,1):new Color(.40f,.52f,.65f,1);main.gravityModifier=drop?0:.6f;
                var shape=ps.shape;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(13,.05f,12);
                var velocity=ps.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;velocity.x=drop?.42f:.06f;velocity.y=drop?-5.9f:.38f;velocity.z=drop?.10f:0;
                var emission=ps.emission;emission.rateOverTime=drop?290:42;
            }
        }
        public void Evaluate(float clock)
        {
            float time=clock+2;bool restart=last<0||time<last||time-last>.25f;
            foreach(var ps in Systems)SkyFurnaceVoxelEmitter.Advance(ps,time,restart,Mathf.Max(0,time-last));last=time;
        }
        public void Clear(){foreach(var ps in Systems)ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);last=-1;}
    }
}
