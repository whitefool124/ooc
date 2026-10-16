using System;
using UnityEngine;
using UnityEngine.UI;
namespace OCC.Combat.Presentation
{
    // Survives field refreshes, but is reset when leaving the workshop overlay.
    public sealed class WorkshopClampMotion
    {
        private string target=string.Empty;
        private float selectedAt=float.NegativeInfinity,committedAt=float.NegativeInfinity;
        private bool committed;
        public bool HasTarget=>!string.IsNullOrEmpty(target);
        public bool Completed {get;private set;}
        public int CommitCount {get;private set;}
        public void Bind(string key,bool completed,float now)
        {
            key=key??string.Empty;
            if(key!=target){target=key;selectedAt=completed?float.NegativeInfinity:now;committedAt=float.NegativeInfinity;committed=false;}
            Completed=completed;
        }
        public bool Commit(float now)
        {
            if(!HasTarget || !Completed || committed)return false;
            committed=true;selectedAt=float.NegativeInfinity;committedAt=now;CommitCount++;return true;
        }
        public int JawOffset(float now,float intensity)
        {
            if(!HasTarget)return 3;
            if(intensity<=.01f)return 0;
            if(now-selectedAt<.24f)return Mathf.RoundToInt(3f*(1f-Mathf.Clamp01((now-selectedAt)/.24f)));
            float t=now-committedAt;
            return t>=0f && t<.36f ? (t<.10f?1:t<.18f?0:t<.24f?1:0) : 0;
        }
        public int LeverOffset(float now,float intensity)
        {
            if(intensity<=.01f)return 0;
            float t=now-committedAt;
            return t>=0f && t<.32f ? (t<.08f?-1:t<.16f?1:0) : 0;
        }
        public void CancelTransient(){selectedAt=float.NegativeInfinity;committedAt=float.NegativeInfinity;}
        public void Reset(){target=string.Empty;Completed=false;committed=false;CommitCount=0;CancelTransient();}
    }
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class WorkshopClampDetail:MaskableGraphic
    {
        public const string ArtPath="Art/FormalAcademyStructures32/academy_workbench_2x1";
        private WorkshopClampMotion motion;
        private Func<float> intensity;
        private Sprite bench;
        private bool foreground;
        private int jaws=int.MinValue,lever=int.MinValue;
        public WorkshopClampMotion Motion=>motion;
        public override Texture mainTexture=>bench==null?base.mainTexture:bench.texture;
        public static WorkshopClampDetail Create(Transform parent,WorkshopClampMotion state,string key,bool completed,Func<float> animation,bool front=false)
        {
            var go=new GameObject(front?"工坊夹口前景":"工坊定位夹具",typeof(RectTransform),typeof(CanvasRenderer));go.transform.SetParent(parent,false);
            var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(.5f,.5f);
            rect.anchoredPosition=new Vector2(176,-48);rect.sizeDelta=new Vector2(192,96);
            var detail=go.AddComponent<WorkshopClampDetail>();detail.motion=state;detail.intensity=animation;detail.foreground=front;
            detail.bench=Resources.Load<Sprite>(ArtPath);detail.raycastTarget=false;
            if(detail.bench==null)throw new InvalidOperationException("Missing approved workshop bench: "+ArtPath);
            state.Bind(key,completed,Time.unscaledTime);detail.Sample();return detail;
        }
        public void PlayCommitted(){if(motion.Commit(Time.unscaledTime))Sample();}
        private void Update()=>Sample();
        private void Sample()
        {
            if(motion==null)return;
            float level=intensity==null?0:intensity();
            if(level<=.01f)motion.CancelTransient();
            int nextJaws=motion.JawOffset(Time.unscaledTime,level),nextLever=motion.LeverOffset(Time.unscaledTime,level);
            if(jaws==nextJaws && lever==nextLever)return;
            jaws=nextJaws;lever=nextLever;SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(bench==null)return;
            // The approved native bench supplies the body, vice faces and handle.
            // The body stays fixed; native texture regions are translated without repainting.
            // Cut moving parts out of the base, so no second jaw remains underneath.
            Rect left=new Rect(29,8,4,7),right=new Rect(41,7,4,8),handle=new Rect(52,1,3,4);
            if(foreground)
            {
                Region(vh,left,new Vector2(-jaws,0));
                Region(vh,right,new Vector2(jaws,0));
                Region(vh,handle,new Vector2(0,lever));
                return;
            }
            for(int row=0;row<32;row++)
            {
                int start=0;
                foreach(Rect part in new[]{left,right,handle})
                {
                    if(row<part.yMin || row>=part.yMax)continue;
                    if(part.xMin>start)Region(vh,new Rect(start,row,part.xMin-start,1),Vector2.zero);
                    start=(int)part.xMax;
                }
                if(start<64)Region(vh,new Rect(start,row,64-start,1),Vector2.zero);
            }
            // Exposed tabletop uses adjacent native wood columns, not the UI paper below.
            Region(vh,left,Vector2.zero,new Rect(27,8,1,7));
            Region(vh,right,Vector2.zero,new Rect(46,7,1,8));
        }
        private void Region(VertexHelper vh,Rect native,Vector2 offset,Rect? source=null)
        {
            Vector4 uv=UnityEngine.Sprites.DataUtility.GetOuterUV(bench);
            Rect sample=source??native;
            Rect rect=rectTransform.rect;float scale=3f;
            float x=rect.xMin+(native.x+offset.x)*scale;
            float y=rect.yMax-(native.y+native.height+offset.y)*scale;
            float u0=Mathf.Lerp(uv.x,uv.z,sample.x/64f),u1=Mathf.Lerp(uv.x,uv.z,sample.xMax/64f);
            float v0=Mathf.Lerp(uv.y,uv.w,1f-sample.yMax/32f),v1=Mathf.Lerp(uv.y,uv.w,1f-sample.y/32f);
            int start=vh.currentVertCount;Color32 tint=Color.white;
            vh.AddVert(new Vector3(x,y),tint,new Vector2(u0,v0));
            vh.AddVert(new Vector3(x,y+native.height*scale),tint,new Vector2(u0,v1));
            vh.AddVert(new Vector3(x+native.width*scale,y+native.height*scale),tint,new Vector2(u1,v1));
            vh.AddVert(new Vector3(x+native.width*scale,y),tint,new Vector2(u1,v0));
            vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
        }
    }
}
