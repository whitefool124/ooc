using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace OCC.Combat.Presentation
{
    // Runtime presentation only. No random state, tile mutation or saved footprints.
    public sealed class CombatWaterRipples
    {
        public const float Lifetime=.64f;
        public const int MaximumCount=16;
        public readonly struct Wave
        {
            public readonly Vector2 Foot;
            public readonly float StartedAt;
            public Wave(Vector2 foot,float at) { Foot=foot; StartedAt=at; }
        }
        private readonly List<Wave> waves=new List<Wave>(MaximumCount);
        public IReadOnlyList<Wave> Waves=>waves;
        public int Version {get; private set;}
        public void Add(Vector2 foot,float at)
        {
            foreach(Wave wave in waves) if(wave.Foot==foot && Mathf.Abs(wave.StartedAt-at)<.10f) return;
            if(waves.Count==MaximumCount) waves.RemoveAt(0);
            waves.Add(new Wave(foot,at)); Version++;
        }
        public void Advance(float now)
        {
            for(int i=waves.Count-1;i>=0;i--) if(now-waves[i].StartedAt>=Lifetime) {waves.RemoveAt(i); Version++;}
        }
        public void Clear() {if(waves.Count==0)return; waves.Clear(); Version++;}
        public bool IsNear(GridPosition cell)
        {
            foreach(Wave wave in waves) if(Mathf.Abs(wave.Foot.x-cell.X)<=1f && Mathf.Abs(wave.Foot.y-cell.Y)<=1f)return true;
            return false;
        }
    }
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CombatWaterRippleGraphic:MaskableGraphic
    {
        private CombatWaterRipples playback;
        private GridPosition cell;
        private bool wet;
        private float now;
        private int key=int.MinValue;
        public void Refresh(CombatWaterRipples source,GridPosition position,bool isWater,float time)
        {
            raycastTarget=false; playback=source; cell=position; now=time;
            bool visible=isWater && source!=null && source.IsNear(position);
            int next=visible ? source.Version*397 ^ Mathf.FloorToInt(time*24f) : -1;
            if(key==next && wet==visible)return;
            wet=visible; key=next; SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); if(!wet || playback==null)return;
            float scale=rectTransform.rect.width/32f;
            Rect rect=rectTransform.rect;
            // Preserve one native pixel at every grid border, including shore boundaries.
            for(int y=1;y<31;y++) for(int x=1;x<31;x++)
            {
                byte alpha=0;
                for(int waveIndex=0;waveIndex<playback.Waves.Count;waveIndex++)
                {
                    CombatWaterRipples.Wave wave=playback.Waves[waveIndex];
                    float age=(now-wave.StartedAt)/CombatWaterRipples.Lifetime;
                    if(age<0f || age>=1f)continue;
                    float cx=(wave.Foot.x-cell.X)*32f+16f;
                    float cy=(wave.Foot.y-cell.Y)*32f+4f;
                    float dx=x+.5f-cx,dy=(y+.5f-cy)*2f;
                    float radius=2f+Mathf.Floor(age*9f),distance=dx*dx+dy*dy;
                    if(distance<(radius-.65f)*(radius-.65f) || distance>(radius+.65f)*(radius+.65f))continue;
                    byte strength=(byte)(age<.33f?115:age<.66f?78:38);
                    if(strength>alpha)alpha=strength;
                }
                if(alpha==0)continue;
                int start=vh.currentVertCount;
                Color32 tint=new Color32(184,216,212,alpha);
                float left=rect.xMin+x*scale,bottom=rect.yMin+y*scale;
                vh.AddVert(new Vector3(left,bottom),tint,Vector2.zero);
                vh.AddVert(new Vector3(left,bottom+scale),tint,Vector2.zero);
                vh.AddVert(new Vector3(left+scale,bottom+scale),tint,Vector2.zero);
                vh.AddVert(new Vector3(left+scale,bottom),tint,Vector2.zero);
                vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
            }
        }
    }
}
