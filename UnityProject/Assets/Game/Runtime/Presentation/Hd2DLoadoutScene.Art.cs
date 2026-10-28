using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    internal sealed partial class Hd2DLoadoutScene
    {
        private readonly List<Object> ownedArtwork=new List<Object>();
        private Sprite LoadReviewArtwork(string directory,Vector4 border,Vector2 pivot)
        {
            if(directory=="简洁纸边")
            {var paper=Hd2DPaperSkin.Load();if(paper!=null)return paper;}
            var approved=Resources.Load<Sprite>("Art/Hd2DContentFrames/"+directory);
            if(approved!=null)return approved;
#if UNITY_EDITOR
            string archive=directory=="以太晶灯"?"2026-10-09_以太晶灯":directory=="简洁纸边"?"2026-10-09_简洁纸边":directory=="装订纸页"?"2026-10-09_装订纸页与紧凑整备":"2026-10-09_HD2D边框与灯光/"+directory;
            string path=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,
                "../../Worldbuilding/归档/"+archive+"/delivery.png"));
            if(!System.IO.File.Exists(path))return null;
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
            texture.LoadImage(System.IO.File.ReadAllBytes(path));texture.filterMode=FilterMode.Point;texture.wrapMode=TextureWrapMode.Clamp;
            var sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),pivot,32,0,SpriteMeshType.FullRect,border);
            ownedArtwork.Add(sprite);ownedArtwork.Add(texture);return sprite;
#else
            return null;
#endif
        }
        private void AddNativeFrames(MainMenuHd2DStage stage,RogueLoadoutEquipmentView view)
        {
            var frame=LoadReviewArtwork("简洁纸边",new Vector4(4,4,4,4),new Vector2(.5f,.5f));
            if(frame==null)return;
            foreach(var image in sceneRoot.GetComponentsInChildren<Image>(true))
            {
                if(!image.isActiveAndEnabled||image.color.a<.85f||image.GetComponent<Hd2DContainerStyle>()==null)continue;
                if(image.name.StartsWith("背包格_")||image.name=="战外背包网格")continue;
                // Resources are readable text and icons on their existing support,
                // not additional boxes nested inside an already bordered document.
                if(image.GetComponent<Button>()==null&&image.rectTransform.rect.height<=70)
                {image.sprite=null;image.color=Color.clear;continue;}
                // Complete card artwork owns both paper and continuous rim.
                // Keep one intact corner size for every role; do not crop corner brackets.
                var currentFrame=image.GetComponent<Button>()!=null?AetherPaperChrome.ButtonFrame:frame;
                image.sprite=currentFrame;image.type=Image.Type.Sliced;image.fillCenter=true;
                // Compensate for the authored container scale so edge pixels and
                // corner widths remain identical across every rectangle in the scene.
                var canvas=image.GetComponentInParent<Canvas>();
                if(canvas==null)continue;
                image.pixelsPerUnitMultiplier=canvas.referencePixelsPerUnit*Mathf.Abs(image.transform.lossyScale.x)*
                    Hd2DDeskArtCandidate.ScenePixelsPerUnit/currentFrame.pixelsPerUnit;
                image.color=Color.white;
                if(image.GetComponent<Button>()!=null&&image.rectTransform.rect.height*Mathf.Abs(image.transform.lossyScale.y)<
                    (currentFrame.border.y+currentFrame.border.w+4)/Hd2DDeskArtCandidate.ScenePixelsPerUnit)
                {image.sprite=null;image.type=Image.Type.Simple;image.color=ArchiveUiStyle.Paper;}
                image.GetComponent<UiButtonFeedback>()?.UseSceneSpaceFeedback();
                if(image.GetComponent<Button>()!=null&&image.rectTransform.rect.height<=70)
                {
                    // Navigation uses the same quiet document controls as the
                    // settings and service pages; inventory slots keep their art.
                    image.sprite=null;image.type=Image.Type.Simple;
                    image.color=Color.Lerp(ArchiveUiStyle.Paper,FormalUiTheme.Cyan,.08f);
                    image.GetComponent<UiButtonFeedback>()?.UseSceneSpaceFeedback();
                    int textIndex=0;
                    foreach(var label in image.GetComponentsInChildren<Text>(true))
                    {
                        if(string.IsNullOrEmpty(label.text))continue;
                        if(textIndex++>0){label.gameObject.SetActive(false);continue;}
                        label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;
                        label.rectTransform.offsetMin=new Vector2(2,4);label.rectTransform.offsetMax=new Vector2(-2,-4);
                        label.alignment=TextAnchor.MiddleCenter;label.fontSize=24;
                        label.resizeTextForBestFit=false;
                    }
                }
            }
            Hd2DPaperSkin.FitFrames(sceneRoot);
        }
        private void AddTaskLamp(MainMenuHd2DStage stage)
        {
            var lamp=sceneRoot.transform.Find("独立2D以太晶灯");
            var lightHost=sceneRoot.transform.Find("整备台灯暖光");
            if(lamp==null||lightHost==null)throw new System.InvalidOperationException("Missing authored loadout lamp or light");
            var renderer=lamp.GetComponent<SpriteRenderer>();
            var light=lightHost.GetComponent<Light>();
            if(renderer==null||light==null)throw new System.InvalidOperationException("Invalid loadout lamp prefab");
            var material=new Material(renderer.sharedMaterial);materials.Add(material);renderer.sharedMaterial=material;
            light.cullingMask=1<<stage.gameObject.layer;
            var reference=GetComponentInChildren<Text>(true);
            if(reference==null)reference=sceneRoot.GetComponentInChildren<Text>(true);
            if(reference!=null)
            {
                var authoredControl=sceneRoot.transform.Find("以太晶灯亮度调节");
                if(authoredControl==null)throw new System.InvalidOperationException("Missing authored lamp control");
                var control=authoredControl.gameObject;
                control.AddComponent<Hd2DDeskLampControl>().Initialize(stage,light,material,reference.font,RequestDeskReflection);
            }
        }
    }
}
