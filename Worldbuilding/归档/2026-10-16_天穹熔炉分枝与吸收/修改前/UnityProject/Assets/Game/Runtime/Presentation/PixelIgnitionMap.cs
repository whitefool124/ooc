using System;
using System.Collections.Generic;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    /// <summary>Technical timing data, not generated artwork. Propagates over opaque native texel connectivity.</summary>
    public sealed class PixelIgnitionMap
    {
        public readonly Texture2D Texture;
        public readonly int PixelCount,Components,ColonyCount;
        public readonly string Pattern;
        private readonly float[] sorted;
        private readonly Vector2[] fronts=new Vector2[64];
        public PixelIgnitionMap(Texture2D source,int choreography=-1)
        {
            Pattern="Staggered compact native-pixel colonies, neighbour-by-neighbour birth";
            int w=source.width,h=source.height,n=w*h;var rgba=source.GetPixels32();
            var opaque=new bool[n];var assigned=new bool[n];var ordered=new List<int>();
            for(int i=0;i<n;i++)if(rgba[i].a>=128){opaque[i]=true;ordered.Add(i);}
            // Distributed starting points, never a common angle/radius/distance wipe.
            // Randomness selects only colony phase/seed; pixels inside each colony
            // must attach to existing neighbours, so this is growth, not dissolve.
            ordered.Sort((a,b)=>Hash((uint)a+91).CompareTo(Hash((uint)b+91)));
            var values=new float[n];for(int i=0;i<n;i++)values[i]=1;
            var list=new List<float>();var weights=new int[64];int colonies=0;
            foreach(int seed in ordered)
            {
                if(assigned[seed])continue;
                int limit=24+(int)(Hash((uint)seed+17)%25);
                var frontier=new HashSet<int>{seed};var region=new List<int>();
                while(frontier.Count>0&&region.Count<limit)
                {
                    int chosen=-1;float best=float.PositiveInfinity;
                    foreach(int cell in frontier)
                    {
                        int dx=cell%w-seed%w,dy=cell/w-seed/w;
                        float score=(dx*dx+dy*dy)*16+(Hash((uint)cell+31)%100)*.01f;
                        if(score<best){chosen=cell;best=score;}
                    }
                    frontier.Remove(chosen);if(assigned[chosen])continue;
                    assigned[chosen]=true;region.Add(chosen);
                    int x=chosen%w,y=chosen/w;
                    for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                    {
                        if(dx==0&&dy==0)continue;int ax=x+dx,ay=y+dy;
                        if(ax<0||ax>=w||ay<0||ay>=h)continue;
                        int next=ay*w+ax;if(opaque[next]&&!assigned[next])frontier.Add(next);
                    }
                }
                float phase=(float)((colonies*.6180339887498949)%1);
                float begin=.64f*phase;
                float span=.26f+(Hash((uint)seed+47)%100)*.001f;
                for(int rank=0;rank<region.Count;rank++)
                {
                    int cell=region[rank];
                    values[cell]=begin+span*(rank+1)/region.Count;
                }
                colonies++;
            }
            // Connectivity is diagnostic only; it never drives one global advancing front.
            var visited=new bool[n];int components=0;var flood=new Queue<int>();
            foreach(int seed in ordered)
            {
                if(visited[seed])continue;components++;visited[seed]=true;flood.Enqueue(seed);
                while(flood.Count>0)
                {
                    int cell=flood.Dequeue(),x=cell%w,y=cell/w;
                    for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                    {
                        int ax=x+dx,ay=y+dy;if(ax<0||ax>=w||ay<0||ay>=h)continue;
                        int next=ay*w+ax;if(opaque[next]&&!visited[next]){visited[next]=true;flood.Enqueue(next);}
                    }
                }
            }
            for(int i=0;i<n;i++)
            {
                if(!opaque[i])continue;list.Add(values[i]);int bucket=Mathf.Min(63,Mathf.FloorToInt(values[i]*64));
                fronts[bucket]+=new Vector2((i%w+.5f)/w-.5f,(i/w+.5f)/h-.5f);weights[bucket]++;
            }
            ColonyCount=colonies;
            for(int i=0;i<64;i++)if(weights[i]>0)fronts[i]/=weights[i];else if(i>0)fronts[i]=fronts[i-1];
            list.Sort();sorted=list.ToArray();PixelCount=ordered.Count;Components=components;
            Texture=new Texture2D(w,h,TextureFormat.RFloat,false,true){name="Native texel ignition timing: "+source.name,filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            Texture.SetPixelData(values,0);Texture.Apply(false,false);
        }
        private static uint Hash(uint value)
        {unchecked{value^=value>>16;value*=0x7feb352d;value^=value>>15;value*=0x846ca68b;return value^(value>>16);}}
        public int LitCount(float progress)
        {int lo=0,hi=sorted.Length;while(lo<hi){int mid=(lo+hi)/2;if(sorted[mid]<=progress)lo=mid+1;else hi=mid;}return lo;}
        public float Fraction(float progress)=>LitCount(progress)/(float)Mathf.Max(1,PixelCount);
        public Vector2 Front(float progress)=>fronts[Mathf.Clamp(Mathf.FloorToInt(progress*64),0,63)];
    }
}
