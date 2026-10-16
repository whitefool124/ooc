using System;
using System.Collections.Generic;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    // Replace the complete paper with all its solid texels before moving any of them.
    // Fracture changes geometry, never paper coverage or particle opacity.
    public sealed class SkyFurnacePixelDisassembly
    {
        private struct Cell
        { public Vector3 local, velocity; public Color32 colour; public float release, life; public uint id; }
        private sealed class Track
        { public string name; public Cell[] cells; public Func<Matrix4x4> pose; public float start; }
        private readonly List<Track> tracks=new List<Track>();
        public readonly ParticleSystem[] Systems;
        public readonly ParticleSystemRenderer[] Renderers;
        private ParticleSystem.Particle[] buffer=new ParticleSystem.Particle[0],readback=new ParticleSystem.Particle[0];
        private int nextId=1;
        private float clock;
        public int Count=>Systems[0].particleCount;
        public int Peak {get;private set;}
        public float MaximumTravel {get;private set;}
        public Vector3 PeakSpan {get;private set;}
        public int SourcePixels=>buffer.Length;
        public bool TextureFree=>Renderers[0].sharedMaterial.GetTexturePropertyNames().Length==0;
        public SkyFurnacePixelDisassembly(Transform parent,int layer,Mesh cube,Material material)
        {
            var ps=SkyFurnaceVoxelEmitter.Create(parent,layer,"完整法阵几何破碎 · 无遮罩消失",Vector3.zero,cube,material,731,32768);
            var shape=ps.shape;shape.enabled=false;var emission=ps.emission;emission.enabled=false;
            var colours=ps.colorOverLifetime;colours.enabled=false;var sizes=ps.sizeOverLifetime;sizes.enabled=false;
            Systems=new[]{ps};Renderers=new[]{ps.GetComponent<ParticleSystemRenderer>()};ps.Pause(false);
        }
        public void Add(Texture2D source,float start,float end,int seed,Func<Matrix4x4> pose)
        {
            int w=source.width,h=source.height;var pixels=source.GetPixels32();var cells=new List<Cell>();
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)
            {
                if(pixels[y*w+x].a<128)continue;
                float nx=(x+.5f)/w,ny=(y+.5f)/h;
                // Adjacent one-pixel cubes initially travel as 3x3 ink fragments.
                // Different fragment velocities open actual gaps in the complete shape.
                uint hash=Hash((uint)(x/3+(y/3)*113+seed*104729));float noise=(hash&65535)/65535f;
                float angle=noise*Mathf.PI*2,speed=.19f+.16f*((hash>>16)&255)/255f;
                var colour=((Color)pixels[y*w+x]).linear;
                colour.r=Mathf.Max(colour.r,.42f);colour.g=Mathf.Max(colour.g,.12f);colour.b=Mathf.Max(colour.b,.015f);colour.a=1;
                float cx=(x/3*3+1.5f)/w,cy=(y/3*3+1.5f)/h;
                float release=start+.08f+Mathf.Min(.65f,(end-start)*.5f)*(.65f*(1-cy)+.35f*cx);
                cells.Add(new Cell{local=new Vector3(nx-.5f,ny-.5f,0),colour=colour,
                    release=release,life=2.10f+.15f*((hash>>24)&255)/255f,
                    velocity=new Vector3(.11f+Mathf.Cos(angle)*speed,.28f+.10f*noise,.055f+Mathf.Sin(angle)*speed),id=(uint)nextId++});
            }
            tracks.Add(new Track{name=source.name,cells=cells.ToArray(),pose=pose,start=start});
            int capacity=nextId-1;buffer=new ParticleSystem.Particle[capacity];readback=new ParticleSystem.Particle[capacity];
            var main=Systems[0].main;main.maxParticles=Mathf.Max(1,capacity);
        }
        private static uint Hash(uint x){unchecked{x^=x>>16;x*=0x7feb352d;x^=x>>15;x*=0x846ca68b;return x^(x>>16);}}
        private static Vector3 Drift(Cell cell,float time)
        {
            float age=Mathf.Max(0,time-cell.release);
            var d=cell.velocity*age+Vector3.up*(.025f*age*age);
            float pitch=SkyFurnaceDirector.PixelPitch;
            return new Vector3(Mathf.Round(d.x/pitch),Mathf.Round(d.y/pitch),Mathf.Round(d.z/pitch))*pitch;
        }
        public void Evaluate(float time)
        {
            clock=time;int count=0;var bounds=new Bounds();
            foreach(var track in tracks)
            {
                if(time<track.start)continue;Matrix4x4 pose=track.pose();
                foreach(var cell in track.cells)
                {
                    float age=Mathf.Max(0,time-cell.release);if(age>=cell.life)continue;
                    Vector3 drift=Drift(cell,time);MaximumTravel=Mathf.Max(MaximumTravel,drift.magnitude);
                    var position=pose.MultiplyPoint3x4(cell.local)+drift;
                    if(count==0)bounds=new Bounds(position,Vector3.one*SkyFurnaceDirector.PixelPitch);
                    else bounds.Encapsulate(new Bounds(position,Vector3.one*SkyFurnaceDirector.PixelPitch));
                    buffer[count++]=new ParticleSystem.Particle{position=position,startColor=cell.colour,startSize=1,
                        startLifetime=cell.life,remainingLifetime=cell.life-age,randomSeed=cell.id};
                }
            }
            Systems[0].SetParticles(buffer,count);Systems[0].Pause(false);
            Peak=Mathf.Max(Peak,Count);PeakSpan=Vector3.Max(PeakSpan,bounds.size);
        }
        public object Audit()
        {
            var ps=Systems[0];int actual=ps.GetParticles(readback);float minAlpha=1,maxAlpha=0,minSize=float.MaxValue;
            for(int i=0;i<actual;i++)
            {var p=readback[i];float alpha=p.GetCurrentColor(ps).a/255f;minAlpha=Mathf.Min(minAlpha,alpha);maxAlpha=Mathf.Max(maxAlpha,alpha);minSize=Mathf.Min(minSize,p.GetCurrentSize(ps));}
            var items=new List<object>();
            foreach(var track in tracks)
            {
                int live=0,moved=0;float maxMove=0;var pose=track.pose();
                foreach(var cell in track.cells)
                {if(clock<track.start||clock-cell.release>=cell.life)continue;live++;float move=Drift(cell,clock).magnitude;maxMove=Mathf.Max(maxMove,move);if(move>=SkyFurnaceDirector.PixelPitch)moved++;}
                var sample=track.cells[track.cells.Length/2];Vector3 original=pose.MultiplyPoint3x4(sample.local);
                Vector3? actualPosition=null;Color32? actualColour=null;
                for(int i=0;i<actual;i++)if(readback[i].randomSeed==sample.id){actualPosition=readback[i].position;actualColour=readback[i].GetCurrentColor(ps);break;}
                items.Add(new{name=track.name,start=track.start,sourceTexels=track.cells.Length,liveTexels=live,movedTexels=moved,maximumMove=maxMove,
                    trackedPixel=sample.id,original=new[]{original.x,original.y,original.z},
                    position=actualPosition.HasValue?new[]{actualPosition.Value.x,actualPosition.Value.y,actualPosition.Value.z}:null,
                    rgba=actualColour.HasValue?new[]{(int)actualColour.Value.r,actualColour.Value.g,actualColour.Value.b,actualColour.Value.a}:null});
            }
            return new{nativeReadbackCount=actual,minAlpha=actual>0?minAlpha:-1,maxAlpha=actual>0?maxAlpha:-1,
                minSize=actual>0?minSize:-1,colourOverLifetime=ps.colorOverLifetime.enabled,sizeOverLifetime=ps.sizeOverLifetime.enabled,
                materialEnergy=Renderers[0].sharedMaterial.GetFloat("_Energy"),materialAlpha=Renderers[0].sharedMaterial.GetColor("_Tint").a,tracks=items};
        }
        public int Fingerprint(){int hash=Count;unchecked{for(int i=0;i<Count;i++)hash=hash*31+buffer[i].position.GetHashCode();}return hash;}
        public void Clear(){Systems[0].Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);}
    }
}
