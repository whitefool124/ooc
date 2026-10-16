using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    // Same clear-reading approach as the loadout page: source geometry follows
    // its paper, while letter strokes render once at the final output resolution.
    public sealed class Hd2DServiceReadingText:MonoBehaviour
    {
        private sealed class Entry{public Text Source,Display;public bool Enabled;public CanvasGroup Group;public RectMask2D[] Masks;public RectTransform Clip;}
        private readonly List<Entry> entries=new List<Entry>();
        private readonly Vector3[] corners=new Vector3[4];
        private Camera cameraView;private Canvas canvas;private GameObject root;
        internal void Configure(GameObject world,Camera camera)
        {
            cameraView=camera;root=new GameObject("服务页清晰正文",typeof(RectTransform),typeof(Canvas));root.transform.SetParent(transform,false);
            canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=30;
            foreach(var source in world.GetComponentsInChildren<Text>(true))
            {
                var go=new GameObject(source.name+" 清晰文字",typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));go.transform.SetParent(root.transform,false);
                var display=go.GetComponent<Text>();display.font=source.font;display.fontStyle=source.fontStyle;display.alignment=source.alignment;
                display.raycastTarget=false;display.resizeTextForBestFit=false;display.horizontalOverflow=source.horizontalOverflow;display.verticalOverflow=VerticalWrapMode.Overflow;
                display.rectTransform.anchorMin=display.rectTransform.anchorMax=display.rectTransform.pivot=new Vector2(.5f,.5f);
                var entry=new Entry{Source=source,Display=display,Enabled=source.enabled,Group=source.GetComponentInParent<CanvasGroup>(),Masks=source.GetComponentsInParent<RectMask2D>()};
                if(entry.Masks.Length>0)
                {
                    var clip=new GameObject(source.name+" 正文裁切",typeof(RectTransform),typeof(RectMask2D));clip.transform.SetParent(root.transform,false);
                    entry.Clip=(RectTransform)clip.transform;entry.Clip.anchorMin=entry.Clip.anchorMax=entry.Clip.pivot=new Vector2(.5f,.5f);
                    display.transform.SetParent(entry.Clip,false);
                }
                entries.Add(entry);
                source.enabled=false;
            }
            Refresh();
        }
        private void OnEnable(){Canvas.willRenderCanvases+=Refresh;}
        private void OnDisable(){Canvas.willRenderCanvases-=Refresh;}
        private void LateUpdate()=>Refresh();
        public void Refresh()
        {
            if(canvas==null||cameraView==null)return;
            var viewport=canvas.rootCanvas.pixelRect;float width=viewport.width,height=viewport.height;
            float scale=Mathf.Max(.001f,canvas.rootCanvas.scaleFactor);
            foreach(var entry in entries)
            {
                var source=entry.Source;var display=entry.Display;
                bool visible=source!=null&&entry.Enabled&&source.gameObject.activeInHierarchy&&(entry.Group==null||entry.Group.alpha>=.5f);
                display.enabled=visible;if(!visible)continue;
                source.enabled=false;source.rectTransform.GetWorldCorners(corners);
                Vector2 min=new Vector2(float.PositiveInfinity,float.PositiveInfinity),max=-min;
                foreach(var corner in corners)
                {
                    var point=cameraView.WorldToViewportPoint(corner);if(point.z<=0){visible=false;break;}
                    var screen=new Vector2(point.x*width,point.y*height);min=Vector2.Min(min,screen);max=Vector2.Max(max,screen);
                }
                if(!visible){display.enabled=false;continue;}
                int font=source.fontSize>=32?(height<800?24:36):(height<800?12:24);
                display.fontSize=Mathf.Max(12,Mathf.RoundToInt(font/scale/12)*12);
                display.text=source.text;display.color=source.color;
                var size=(max-min)/scale;size.y=Mathf.Max(size.y,font*1.25f/scale);
                display.rectTransform.sizeDelta=size;
                // Paper annotations can be narrower than a body-text line. Keep
                // the clear glyph grid, but fit the authored box instead of
                // letting enlarged overlay letters spill onto adjacent cards.
                while(display.fontSize>12&&(display.preferredHeight>size.y+1||
                    display.horizontalOverflow==HorizontalWrapMode.Overflow&&display.preferredWidth>size.x+1))display.fontSize-=12;
                display.verticalOverflow=VerticalWrapMode.Overflow;
                Vector2 clipCentre=Vector2.zero;
                if(entry.Clip!=null)
                {
                    Vector2 clipMin=Vector2.zero,clipMax=new Vector2(width,height);
                    foreach(var mask in entry.Masks)
                    {
                        if(mask==null||!mask.isActiveAndEnabled)continue;mask.rectTransform.GetWorldCorners(corners);
                        Vector2 a=Vector2.positiveInfinity,b=Vector2.negativeInfinity;
                        foreach(var corner in corners){var p=cameraView.WorldToViewportPoint(corner);var screen=new Vector2(p.x*width,p.y*height);a=Vector2.Min(a,screen);b=Vector2.Max(b,screen);}
                        clipMin=Vector2.Max(clipMin,a);clipMax=Vector2.Min(clipMax,b);
                    }
                    if(clipMax.x<=clipMin.x||clipMax.y<=clipMin.y){display.enabled=false;continue;}
                    clipCentre=(clipMin+clipMax-new Vector2(width,height))*.5f/scale;
                    entry.Clip.anchoredPosition=clipCentre;entry.Clip.sizeDelta=(clipMax-clipMin)/scale;
                }
                display.rectTransform.anchoredPosition=new Vector2(Mathf.Round((min.x+max.x-width)*.5f),Mathf.Round((min.y+max.y-height)*.5f))/scale-clipCentre;
            }
        }
        internal void Restore()
        {
            foreach(var entry in entries)if(entry.Source!=null)entry.Source.enabled=entry.Enabled;
            entries.Clear();if(root!=null)Destroy(root);root=null;
        }
        private void OnDestroy(){Canvas.willRenderCanvases-=Refresh;Restore();}
    }
}
