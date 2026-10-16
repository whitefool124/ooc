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
        public readonly int MaximumBranches,DetachedBirths;
        public readonly string Pattern;
        private readonly float[] sorted;
        private readonly Vector2[] fronts=new Vector2[64];
        private readonly Vector2[] birthPoints;
        public PixelIgnitionMap(Texture2D source,int choreography=-1)
        {
            Pattern="Two attached growth roots, compact buds and rooted branch forks";
            int w=source.width,h=source.height,n=w*h;var rgba=source.GetPixels32();
            var opaque=new bool[n];var assigned=new bool[n];var ordered=new List<int>();
            for(int i=0;i<n;i++)if(rgba[i].a>=128){opaque[i]=true;ordered.Add(i);}
            var values=new float[n];for(int i=0;i<n;i++)values[i]=1;
            var list=new List<float>();var weights=new int[64];
            var frontier=new HashSet<int>();var heads=new List<int>();var births=new List<int>();
            int roots=choreography<0?1:2;
            for(int root=0;root<roots;root++)
            {
                var target=new Vector2(w*(root==0?.26f:.74f),h*(root==0?.22f:.78f));
                int seed=-1;float best=float.PositiveInfinity;
                foreach(int cell in ordered)if(!assigned[cell])
                {float score=(new Vector2(cell%w+.5f,cell/w+.5f)-target).sqrMagnitude;if(score<best){best=score;seed=cell;}}
                if(seed<0)continue;AddBirth(seed,w,h,opaque,assigned,frontier,births);heads.Add(seed);
            }
            int patches=0,extraRoots=0;
            while(births.Count<ordered.Count)
            {
                if(frontier.Count==0)
                {
                    // Only disconnected source ink can require another root; never sprinkle
                    // new points in a still-reachable part of this connected drawing.
                    int isolatedSeed=ordered.Find(cell=>!assigned[cell]);
                    AddBirth(isolatedSeed,w,h,opaque,assigned,frontier,births);heads.Add(isolatedSeed);extraRoots++;
                    if(births.Count==ordered.Count)break;
                }
                int branch=patches%heads.Count;
                int seed=Nearest(frontier,heads[branch],w);
                var bud=new HashSet<int>{seed};int last=seed;
                // Grow a small solid colour block, then advance this same branch.
                for(int step=0;step<20&&bud.Count>0;step++)
                {
                    int cell=Nearest(bud,seed,w);bud.Remove(cell);
                    if(assigned[cell])continue;
                    AddBirth(cell,w,h,opaque,assigned,frontier,births);last=cell;
                    int x=cell%w,y=cell/w;
                    for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                    {int ax=x+dx,ay=y+dy;if(ax<0||ax>=w||ay<0||ay>=h)continue;int next=ay*w+ax;if(opaque[next]&&!assigned[next])bud.Add(next);}
                }
                heads[branch]=last;patches++;
                // A fork starts at an EXISTING grown location, not a new seed in empty ink.
                if(heads.Count<4&&births.Count>ordered.Count*(heads.Count==2?.28f:.55f))
                    heads.Add(births[births.Count/3]);
            }
            for(int rank=0;rank<births.Count;rank++)values[births[rank]]=(rank+1f)/births.Count;
            birthPoints=new Vector2[births.Count];
            for(int i=0;i<births.Count;i++)birthPoints[i]=new Vector2((births[i]%w+.5f)/w-.5f,(births[i]/w+.5f)/h-.5f);
            int colonies=roots+extraRoots;
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
            ColonyCount=colonies;MaximumBranches=heads.Count;
            var prior=new bool[n];int detached=0;
            for(int rank=0;rank<births.Count;rank++)
            {
                int cell=births[rank],x=cell%w,y=cell/w;bool adjacent=false;
                for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){int ax=x+dx,ay=y+dy;if(ax>=0&&ax<w&&ay>=0&&ay<h&&prior[ay*w+ax])adjacent=true;}
                if(rank>=roots&&!adjacent)detached++;prior[cell]=true;
            }
            DetachedBirths=detached;
            for(int i=0;i<64;i++)if(weights[i]>0)fronts[i]/=weights[i];else if(i>0)fronts[i]=fronts[i-1];
            list.Sort();sorted=list.ToArray();PixelCount=ordered.Count;Components=components;
            Texture=new Texture2D(w,h,TextureFormat.RFloat,false,true){name="Native texel ignition timing: "+source.name,filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            Texture.SetPixelData(values,0);Texture.Apply(false,false);
        }
        private static int Nearest(HashSet<int> candidates,int origin,int width)
        {
            int best=-1;long score=long.MaxValue;
            foreach(int cell in candidates)
            {int dx=cell%width-origin%width,dy=cell/width-origin/width;long distance=(long)(dx*dx+dy*dy)*100000+cell;if(distance<score){score=distance;best=cell;}}
            return best;
        }
        private static void AddBirth(int cell,int width,int height,bool[] opaque,bool[] assigned,HashSet<int> frontier,List<int> births)
        {
            assigned[cell]=true;frontier.Remove(cell);births.Add(cell);
            int x=cell%width,y=cell/width;
            for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
            {int ax=x+dx,ay=y+dy;if(ax<0||ax>=width||ay<0||ay>=height)continue;int next=ay*width+ax;if(opaque[next]&&!assigned[next])frontier.Add(next);}
        }
        public int LitCount(float progress)
        {int lo=0,hi=sorted.Length;while(lo<hi){int mid=(lo+hi)/2;if(sorted[mid]<=progress)lo=mid+1;else hi=mid;}return lo;}
        public float Fraction(float progress)=>LitCount(progress)/(float)Mathf.Max(1,PixelCount);
        public Vector2 Front(float progress)=>birthPoints.Length==0?Vector2.zero:birthPoints[Mathf.Clamp(Mathf.FloorToInt(progress*birthPoints.Length),0,birthPoints.Length-1)];
    }
}
