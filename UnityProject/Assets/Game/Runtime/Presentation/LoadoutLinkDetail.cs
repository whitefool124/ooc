using System;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    public sealed class LoadoutLinkDetail : MaskableGraphic
    {
        private Vector2[] points;
        private float started;
        private Func<float> intensity;
        public static void Spawn(RectTransform parent, Vector2 source, Vector2 target, float gutter, float belowRow, Func<float> motion)
        {
            if ((motion?.Invoke() ?? 0) <= .01f) return;
            var go = new GameObject("战术栏关联反馈",typeof(RectTransform),typeof(CanvasRenderer));
            go.transform.SetParent(parent,false);
            var rect = (RectTransform)go.transform; rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one;
            rect.offsetMin=rect.offsetMax=Vector2.zero;
            var effect=go.AddComponent<LoadoutLinkDetail>(); effect.intensity=motion; effect.started=Time.unscaledTime;
            effect.raycastTarget=false; effect.color=FormalUiTheme.Safe;
            // Source is in the backpack column; use the gutter, then the tactical row.
            effect.points = new[] {source,new Vector2(gutter,source.y),new Vector2(gutter,belowRow),new Vector2(target.x,belowRow),target};
        }
        private void Update()
        {
            if ((intensity?.Invoke() ?? 0) <= .01f || Time.unscaledTime-started >= .42f) { Destroy(gameObject); return; }
            SetVerticesDirty();
        }
        protected override void OnDisable() { base.OnDisable(); Destroy(gameObject); }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear(); if(points==null)return;
            float t=Mathf.Clamp01((Time.unscaledTime-started)/.42f);
            float total=0; for(int i=1;i<points.Length;i++)total+=Vector2.Distance(points[i-1],points[i]);
            float head=t*total, tail=Mathf.Max(0,head-72), walked=0;
            for(int i=1;i<points.Length;i++)
            {
                Vector2 a=points[i-1],b=points[i]; float length=Vector2.Distance(a,b);
                if(length<.01f)continue;
                float from=Mathf.Clamp((tail-walked)/length,0,1),to=Mathf.Clamp((head-walked)/length,0,1);
                if(to>from)
                {
                    Vector2 p=PixelPresentationMotion.Snap(Vector2.Lerp(a,b,from)),q=PixelPresentationMotion.Snap(Vector2.Lerp(a,b,to));
                    LoadoutBuckleDetail.Quad(mesh,Mathf.Min(p.x,q.x)-1,Mathf.Min(p.y,q.y)-1,
                        Mathf.Max(2,Mathf.Abs(q.x-p.x)),Mathf.Max(2,Mathf.Abs(q.y-p.y)),color);
                }
                walked+=length;
            }
        }
    }
}
