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
        private readonly List<ReadingLabel> readingLabels=new List<ReadingLabel>();
        private readonly Vector3[] readingCorners=new Vector3[4];
        private sealed class ReadingLabel
        {
            internal Text source,display;
            internal CanvasGroup group;
            internal bool heading,authoredEnabled;
        }
        private void BuildReadingText(MainMenuHd2DStage stage)
        {
            readingCamera=stage.sceneCamera;
            readingRoot=new GameObject("整备页清晰文字",typeof(RectTransform),typeof(Canvas));
            readingRoot.transform.SetParent(transform,false);
            var canvas=readingRoot.GetComponent<Canvas>();
            readingCanvas=canvas;
            canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=30;
            foreach(var source in sceneRoot.GetComponentsInChildren<Text>(true))
            {
                if(source.name=="整备提示")
                {source.enabled=false;continue;}
                if(source.name=="装备区标题")source.rectTransform.anchoredPosition-=new Vector2(90,0);
                if(source.name=="快捷栏标题")source.rectTransform.anchoredPosition-=new Vector2(120,0);
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
                // Floating labels retain a restrained dark edge over the wood.
                if(source.color.r>.7f||source.name=="背包交互状态")
                {
                    var shadow=go.AddComponent<Shadow>();shadow.effectColor=new Color(.10f,.065f,.035f,.75f);
                    shadow.effectDistance=new Vector2(1,-1);shadow.useGraphicAlpha=true;
                }
                readingLabels.Add(new ReadingLabel{source=source,display=display,
                    authoredEnabled=source.enabled,
                    group=source.GetComponentInParent<CanvasGroup>(),
                    heading=source.name=="角色栏标题"||source.name=="装备区标题"||source.name=="快捷栏标题"||source.name=="术式标题"||source.name=="术式名称"});
            }
        }
        private void RefreshReadingText()
        {
            if(readingRoot==null||readingCamera==null)return;
            float scale=Screen.height/1080f;
            float canvasScale=Mathf.Max(.001f,readingCanvas.rootCanvas.scaleFactor);
            foreach(var label in readingLabels)
            {
                var source=label.source;var display=label.display;
                if(source==null){display.enabled=false;continue;}
                source.enabled=false;
                bool visible=label.authoredEnabled&&source.gameObject.activeInHierarchy&&(label.group==null||label.group.alpha>=.5f);
                bool inventoryStatus=source.name=="背包交互状态";
                if(inventoryStatus&&(string.IsNullOrEmpty(source.text)||source.text=="左键拖拽物品　拖拽中按 R 键或右键旋转"))visible=false;
                display.enabled=visible;if(!visible)continue;
                source.rectTransform.GetWorldCorners(readingCorners);
                Vector2 minimum=new Vector2(float.PositiveInfinity,float.PositiveInfinity),maximum=-minimum;
                bool inFront=true;
                for(int i=0;i<4;i++)
                {
                    var point=readingCamera.WorldToViewportPoint(readingCorners[i]);
                    if(point.z<=0)inFront=false;
                    var screen=new Vector2(point.x*Screen.width,point.y*Screen.height);
                    minimum=Vector2.Min(minimum,screen);maximum=Vector2.Max(maximum,screen);
                }
                if(!inFront){display.enabled=false;continue;}
                // FusionPixel's original 12px glyphs use whole multiples, without
                // perspective resampling or best-fit shrinkage of the letter strokes.
                int screenFontSize=label.heading?(scale<.75f?24:36):(scale<.75f?12:24);
                display.fontSize=Mathf.Max(12,Mathf.RoundToInt(screenFontSize/canvasScale/12)*12);
                display.text=source.name=="目标规则"?source.text.Replace("　射程","\n射程"):source.text;
                display.color=source.color;
                var size=maximum-minimum;
                int lines=1;
                if(display.text!=null)foreach(char character in display.text)if(character=='\n')lines++;
                size.y=Mathf.Max(size.y,screenFontSize*lines*1.2f+4);
                if(source.GetComponentInParent<Button>()!=null&&source.rectTransform.rect.height<=70)
                    size.x=Mathf.Max(size.x,display.preferredWidth*canvasScale);
                display.rectTransform.sizeDelta=new Vector2(Mathf.Ceil(size.x)/canvasScale,Mathf.Ceil(size.y)/canvasScale);
                var centre=(minimum+maximum)*.5f;
                if(source.name=="晶灯亮度")
                {
                    centre+=new Vector2(-114,-62)*scale;
                    centre.x=Mathf.Max(centre.x,size.x*.5f+24*scale);
                }
                if(inventoryStatus)
                {
                    display.color=new Color(.98f,.90f,.71f);
                    display.alignment=TextAnchor.MiddleCenter;
                    display.rectTransform.sizeDelta=new Vector2(Screen.width-48*scale,screenFontSize*1.2f+4)/canvasScale;
                    centre=new Vector2(Screen.width*.5f,32*scale);
                }
                display.rectTransform.anchoredPosition=new Vector2(Mathf.Round(centre.x-Screen.width*.5f),Mathf.Round(centre.y-Screen.height*.5f))/canvasScale;
            }
        }
    }
}
