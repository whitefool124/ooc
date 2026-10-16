using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    // Four native-pixel motes, at an executed stop only. No gameplay or random state.
    public sealed class CombatStopDust
    {
        public const float Lifetime = .36f;
        public readonly struct Stop
        {
            public readonly GridPosition Cell;
            public readonly Vector2 Direction;
            public readonly float At;
            public Stop(GridPosition cell, Vector2 direction, float at) { Cell=cell; Direction=direction; At=at; }
        }
        private readonly List<Stop> stops = new List<Stop>(8);
        public IReadOnlyList<Stop> Stops => stops;
        public int Version { get; private set; }
        public void Add(GridPosition cell, Vector2 direction, float at)
        {
            for (int i=0;i<stops.Count;i++) if (stops[i].Cell==cell) { stops.RemoveAt(i); break; }
            if (stops.Count==8) stops.RemoveAt(0);
            stops.Add(new Stop(cell,direction,at)); Version++;
        }
        public void Advance(float now)
        {
            for (int i=stops.Count-1;i>=0;i--) if (now-stops[i].At>=Lifetime) { stops.RemoveAt(i); Version++; }
        }
        public void Clear() { if(stops.Count==0)return; stops.Clear(); Version++; }
    }

    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CombatStopDustGraphic : MaskableGraphic
    {
        private CombatStopDust source;
        private GridPosition cell;
        private float time;
        private bool visible;
        private int key=int.MinValue;
        public void Refresh(CombatStopDust playback, GridPosition position, bool stone, float now)
        {
            source=playback;cell=position;time=now;raycastTarget=false;
            bool show=false;
            if(stone && source!=null) foreach(var stop in source.Stops) if(stop.Cell==position) { show=true; break; }
            int next=show ? source.Version*397 ^ Mathf.FloorToInt(now*24f) : -1;
            if (key==next && visible==show)return;
            key=next;visible=show;SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(!visible || source==null)return;
            Rect r=rectTransform.rect;float scale=r.width/32f;
            foreach(var stop in source.Stops)
            {
                if(stop.Cell!=cell)continue;
                float age=(time-stop.At)/CombatStopDust.Lifetime;if(age<0 || age>=1)continue;
                int phase=Mathf.FloorToInt(age*5);
                Vector2 back=-stop.Direction,side=new Vector2(-back.y,back.x);
                for(int i=0;i<4;i++)
                {
                    // Foot contact is four native pixels above the logical south edge.
                    Vector2 p=new Vector2(16,4)+back*(2+phase)+side*((i%2==0?-1:1)*(4+i/2));
                    p.y+=phase/2;
                    int x=Mathf.Clamp(Mathf.RoundToInt(p.x),1,29),y=Mathf.Clamp(Mathf.RoundToInt(p.y),1,30);
                    int width=i==0 && phase<2?2:1;
                    byte alpha=(byte)(phase<2?110:phase<4?70:35);
                    Color32 tint=i%2==0?new Color32(180,166,137,alpha):new Color32(211,198,170,alpha);
                    int v=vh.currentVertCount;float left=r.xMin+x*scale,bottom=r.yMin+y*scale;
                    vh.AddVert(new Vector3(left,bottom),tint,Vector2.zero);
                    vh.AddVert(new Vector3(left,bottom+scale),tint,Vector2.zero);
                    vh.AddVert(new Vector3(left+width*scale,bottom+scale),tint,Vector2.zero);
                    vh.AddVert(new Vector3(left+width*scale,bottom),tint,Vector2.zero);
                    vh.AddTriangle(v,v+1,v+2);vh.AddTriangle(v,v+2,v+3);
                }
            }
        }
    }
}
