using System;
using System.Collections.Generic;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    // One departure record drives both the paper cutout and its untextured cube.
    // Analytic positions make seeking, replay and pause independent of simulation history.
    public sealed class SkyFurnacePixelDisassembly
    {
        private struct Cell { public Vector3 local, velocity; public Color32 colour; public float birth, life; }
        private sealed class Track { public Cell[] cells; public Func<Matrix4x4> pose; }
        private readonly List<Track> tracks=new List<Track>();
        public readonly ParticleSystem[] Systems;
        public readonly ParticleSystemRenderer[] Renderers;
        private ParticleSystem.Particle[] buffer=new ParticleSystem.Particle[0];
        public int Count {get;private set;}
        public int Peak {get;private set;}
        public float MaximumTravel {get;private set;}
        public Vector3 PeakSpan {get;private set;}
        public int SourcePixels=>buffer.Length;
        public bool TextureFree=>Renderers[0].sharedMaterial.GetTexturePropertyNames().Length==0;
        public SkyFurnacePixelDisassembly(Transform parent,int layer,Mesh cube,Material material)
        {
            var ps=SkyFurnaceVoxelEmitter.Create(parent,layer,"原纹样逐像素发光解体",Vector3.zero,cube,material,731,32768);
            var shape=ps.shape;shape.enabled=false;var emission=ps.emission;emission.enabled=false;
            Systems=new[]{ps};Renderers=new[]{ps.GetComponent<ParticleSystemRenderer>()};
            // SetParticles owns time; native automatic advancement must never move these twice.
            ps.Pause(false);
        }
        public Texture2D Add(Texture2D source,float start,float end,int seed,Func<Matrix4x4> pose)
        {
            int w=source.width,h=source.height;var pixels=source.GetPixels32();var values=new float[w*h];var cells=new List<Cell>();
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)
            {
                int index=y*w+x;uint hash=Hash((uint)(index+seed*104729));float noise=(hash&65535)/65535f;
                float nx=(x+.5f)/w,ny=(y+.5f)/h;
                // A ragged peeling front, rather than a simultaneous random dissolve.
                float departure=Mathf.Clamp01(.08f+.70f*(.65f*(1-ny)+.35f*nx)+.20f*noise);
                values[index]=departure;if(pixels[index].a<128)continue;
                float angle=noise*Mathf.PI*2,speed=.16f+.13f*((hash>>16)&255)/255f;
                var colour=((Color)pixels[index]).linear;colour.r=Mathf.Max(colour.r,.42f);colour.g=Mathf.Max(colour.g,.12f);colour.b=Mathf.Max(colour.b,.015f);colour.a=1;
                cells.Add(new Cell{local=new Vector3(nx-.5f,ny-.5f,0),colour=colour,
                    birth=Mathf.Lerp(start,end,departure),life=1.05f+.65f*((hash>>24)&255)/255f,
                    velocity=new Vector3(Mathf.Cos(angle)*speed,.24f+.13f*noise,Mathf.Sin(angle)*speed)});
            }
            tracks.Add(new Track{cells=cells.ToArray(),pose=pose});int capacity=0;foreach(var track in tracks)capacity+=track.cells.Length;
            buffer=new ParticleSystem.Particle[capacity];var main=Systems[0].main;main.maxParticles=Mathf.Max(1,capacity);
            var map=new Texture2D(w,h,TextureFormat.RFloat,false,true){name=source.name+" departure data",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            map.SetPixelData(values,0);map.Apply(false,true);return map;
        }
        private static uint Hash(uint x){unchecked{x^=x>>16;x*=0x7feb352d;x^=x>>15;x*=0x846ca68b;return x^(x>>16);}}
        public void Evaluate(float time)
        {
            int count=0;var bounds=new Bounds();
            foreach(var track in tracks)
            {
                Matrix4x4 pose=track.pose();
                foreach(var cell in track.cells)
                {
                    float age=time-cell.birth;if(age<0||age>=cell.life)continue;
                    // Small rising curl, bounded below one metre; cubes stay full size and opaque.
                    Vector3 drift=cell.velocity*age+Vector3.up*(.06f*age*age);
                    MaximumTravel=Mathf.Max(MaximumTravel,drift.magnitude);
                    var position=pose.MultiplyPoint3x4(cell.local)+drift;
                    if(count==0)bounds=new Bounds(position,Vector3.one*SkyFurnaceDirector.PixelPitch);else bounds.Encapsulate(new Bounds(position,Vector3.one*SkyFurnaceDirector.PixelPitch));
                    buffer[count++]=new ParticleSystem.Particle{position=position,
                        startColor=cell.colour,startSize=1,startLifetime=cell.life,remainingLifetime=cell.life-age,randomSeed=1};
                }
            }
            Systems[0].SetParticles(buffer,count);Systems[0].Pause(false);Count=count;Peak=Mathf.Max(Peak,count);PeakSpan=Vector3.Max(PeakSpan,bounds.size);
        }
        public int Fingerprint(){int hash=Count;unchecked{for(int i=0;i<Count;i++)hash=hash*31+buffer[i].position.GetHashCode();}return hash;}
        public void Clear(){Systems[0].Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);Count=0;}
    }
}
