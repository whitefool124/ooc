using System;
using System.Collections.Generic;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    /// <summary>Technical timing data, not generated artwork. Propagates over opaque native texel connectivity.</summary>
    public sealed class PixelIgnitionMap
    {
        public readonly Texture2D Texture;
        public readonly int PixelCount,Components;
        public readonly string Pattern;
        private readonly float[] sorted;
        private readonly Vector2[] fronts=new Vector2[64];
        public PixelIgnitionMap(Texture2D source,int choreography=-1)
        {
            Pattern=choreography==0?"Clockwise perimeter circuit":choreography==1?"Six sequential sigil responses":choreography==2?"Six branches converge to core":"Connected ink propagation";
            int w=source.width,h=source.height,n=w*h;var rgba=source.GetPixels32();var distance=new float[n];var opaque=new bool[n];
            int count=0,components=0;for(int i=0;i<n;i++){opaque[i]=rgba[i].a>=128;distance[i]=float.PositiveInfinity;if(opaque[i])count++;}
            var queue=new SortedSet<(float cost,int index)>();float maximum=0;
            while(true)
            {
                int seed=-1;float best=float.NegativeInfinity;
                for(int i=0;i<n;i++)if(opaque[i]&&float.IsPositiveInfinity(distance[i])){float score=i/w-Mathf.Abs(i%w-w*.5f)*.01f;if(score>best){best=score;seed=i;}}
                if(seed<0)break;components++;distance[seed]=maximum+(components==1?0:6);queue.Add((distance[seed],seed));
                while(queue.Count>0)
                {
                    var node=queue.Min;queue.Remove(node);if(node.cost>distance[node.index])continue;
                    maximum=Mathf.Max(maximum,node.cost);int x=node.index%w,y=node.index/w;
                    for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                    {
                        if(dx==0&&dy==0)continue;int a=x+dx,b=y+dy;if(a<0||a>=w||b<0||b>=h)continue;int index=b*w+a;if(!opaque[index])continue;
                        float next=node.cost+(dx!=0&&dy!=0?1.41421356f:1);
                        if(next<distance[index]){distance[index]=next;queue.Add((next,index));}
                    }
                }
            }
            var values=new float[n];var list=new List<float>();var weights=new int[64];
            for(int i=0;i<n;i++)
            {
                values[i]=opaque[i]?distance[i]/Mathf.Max(1,maximum):1;
                if(opaque[i]&&choreography>=0)
                {
                    float x=(i%w+.5f)/w-.5f,y=(i/w+.5f)/h-.5f;
                    float turn=Mathf.Repeat(Mathf.Atan2(x,y)/(2*Mathf.PI),1);
                    float radius=new Vector2(x,y).magnitude;
                    if(choreography==0)values[i]=Mathf.Clamp01(turn*.93f+Mathf.Clamp01((.46f-radius)/.12f)*.06f);
                    else if(choreography==1){float sector=turn*6;values[i]=Mathf.Clamp01((Mathf.Floor(sector)+Mathf.Repeat(sector,1)*.72f+Mathf.Clamp01((.44f-radius)/.16f)*.25f)/6);}
                    else values[i]=Mathf.Clamp01(1-radius/.48f);
                }
                if(!opaque[i])continue;list.Add(values[i]);int bucket=Mathf.Min(63,Mathf.FloorToInt(values[i]*64));
                fronts[bucket]+=new Vector2((i%w+.5f)/w-.5f,(i/w+.5f)/h-.5f);weights[bucket]++;
            }
            for(int i=0;i<64;i++)if(weights[i]>0)fronts[i]/=weights[i];else if(i>0)fronts[i]=fronts[i-1];
            list.Sort();sorted=list.ToArray();PixelCount=count;Components=components;
            Texture=new Texture2D(w,h,TextureFormat.RFloat,false,true){name="Native texel ignition timing: "+source.name,filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            Texture.SetPixelData(values,0);Texture.Apply(false,false);
        }
        public int LitCount(float progress)
        {int lo=0,hi=sorted.Length;while(lo<hi){int mid=(lo+hi)/2;if(sorted[mid]<=progress)lo=mid+1;else hi=mid;}return lo;}
        public float Fraction(float progress)=>LitCount(progress)/(float)Mathf.Max(1,PixelCount);
        public Vector2 Front(float progress)=>fronts[Mathf.Clamp(Mathf.FloorToInt(progress*64),0,63)];
    }
}
