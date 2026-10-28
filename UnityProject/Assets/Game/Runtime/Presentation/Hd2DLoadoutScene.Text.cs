using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    internal sealed partial class Hd2DLoadoutScene
    {
        private GameObject readingRoot;
        private Camera readingCamera;
        private Canvas readingCanvas;
        private CanvasGroup readingFade;
        private Hd2DPageVisibility readingVisibility;
        private readonly List<ReadingLabel> readingLabels=new List<ReadingLabel>();
        private readonly Vector3[] readingCorners=new Vector3[4];
        private sealed class ReadingLabel
        {
            internal Text source,display;
            internal RectTransform clip;
            internal CanvasGroup group;
            internal bool heading,authoredEnabled,projectedVisible=true;
            internal readonly Hd2DReadingProjectionCache cache=new Hd2DReadingProjectionCache();
        }
        private void BuildReadingText(MainMenuHd2DStage stage)
        {
            readingCamera=stage.sceneCamera;
            readingRoot=new GameObject("整备页清晰文字",typeof(RectTransform),typeof(Canvas));
            readingRoot.transform.SetParent(null,false);
            var canvas=readingRoot.GetComponent<Canvas>();
            readingCanvas=canvas;
            readingFade=readingRoot.AddComponent<CanvasGroup>();readingFade.blocksRaycasts=false;
            readingVisibility=GetComponent<Hd2DPageVisibility>();
            canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.pixelPerfect=true;canvas.sortingOrder=(GetComponentInParent<Canvas>()?.rootCanvas.sortingOrder??40)+1;
            foreach(var source in sceneRoot.GetComponentsInChildren<Text>(true))
            {
                if(source.name=="整备提示")
                {source.enabled=false;continue;}


                if(source.name=="槽位编号")
                {
                    source.rectTransform.anchoredPosition=new Vector2(24,-20);
                    source.rectTransform.sizeDelta=new Vector2(32,36);
                }
                var go=new GameObject(source.name+" 清晰文字",typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));
                go.transform.SetParent(readingRoot.transform,false);
                var display=go.GetComponent<Text>();display.font=source.font;
                display.fontStyle=FontStyle.Normal;display.raycastTarget=false;
                display.resizeTextForBestFit=false;display.alignByGeometry=true;
                display.horizontalOverflow=source.horizontalOverflow;
                display.verticalOverflow=VerticalWrapMode.Overflow;
                display.alignment=source.alignment;display.lineSpacing=1.2f;
                var rect=display.rectTransform;
                rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
                var clipNode=new GameObject(source.name+" 正文裁切",typeof(RectTransform),typeof(RectMask2D));
                clipNode.transform.SetParent(readingRoot.transform,false);
                var clip=(RectTransform)clipNode.transform;clip.anchorMin=clip.anchorMax=clip.pivot=new Vector2(.5f,.5f);
                display.transform.SetParent(clip,false);
                // Final-resolution FusionPixel strokes stay unmodified.
                readingLabels.Add(new ReadingLabel{source=source,display=display,clip=clip,
                    authoredEnabled=source.enabled,
                    group=source.GetComponentInParent<CanvasGroup>(),
                    heading=source.name=="角色栏标题"||source.name=="装备区标题"||source.name=="快捷栏标题"||source.name=="术式标题"||source.name=="术式名称"});
            }
        }
        private void RefreshReadingText()
        {
            if(readingRoot==null||readingCamera==null)return;
            readingFade.alpha=readingVisibility==null?1f:readingVisibility.ReadingOpacity;
            var viewport=readingCanvas.rootCanvas.pixelRect;float width=viewport.width,height=viewport.height;
            float scale=height/1080f;
            float canvasScale=Mathf.Max(.001f,readingCanvas.rootCanvas.scaleFactor);
            var stage=readingCamera.GetComponentInParent<MainMenuHd2DStage>();
            var output=stage==null?new Rect(0,0,width,height):stage.DisplayRect;
            var projection=readingCamera.projectionMatrix*readingCamera.worldToCameraMatrix;
            foreach(var label in readingLabels)
            {
                var source=label.source;var display=label.display;
                if(source==null){display.enabled=false;continue;}
                source.enabled=false;
                bool visible=label.authoredEnabled&&source.gameObject.activeInHierarchy&&(label.group==null||label.group.alpha>=.5f);
                bool inventoryStatus=source.name=="背包交互状态";
                if(inventoryStatus&&(string.IsNullOrEmpty(source.text)||source.text=="左键拖拽物品　拖拽中按 R 键或右键旋转"))visible=false;
                display.enabled=visible;if(!visible)continue;
                if(!label.cache.NeedsRefresh(source,projection,output,viewport,canvasScale)){display.enabled=label.projectedVisible;continue;}
                source.rectTransform.GetWorldCorners(readingCorners);
                Vector2 minimum=new Vector2(float.PositiveInfinity,float.PositiveInfinity),maximum=-minimum;
                bool inFront=true;
                for(int i=0;i<4;i++)
                {
                    var point=readingCamera.WorldToViewportPoint(readingCorners[i]);
                    if(point.z<=0)inFront=false;
                    var screen=output.position+new Vector2(point.x*output.width,point.y*output.height);
                    minimum=Vector2.Min(minimum,screen);maximum=Vector2.Max(maximum,screen);
                }
                if(!inFront){label.projectedVisible=false;display.enabled=false;continue;}
                // FusionPixel's original 12px glyphs use whole multiples, without
                // perspective resampling or best-fit shrinkage of the letter strokes.
                var size=maximum-minimum;
                Hd2DReadingLayout.Fit(display,source,size/canvasScale,label.heading,output.height,canvasScale);
                var centre=(minimum+maximum)*.5f;
                label.clip.anchoredPosition=new Vector2(Mathf.Round(centre.x-width*.5f),Mathf.Round(centre.y-height*.5f))/canvasScale;
                label.clip.sizeDelta=display.rectTransform.sizeDelta;display.rectTransform.anchoredPosition=Vector2.zero;
                label.projectedVisible=true;
            }
        }
    }
}
