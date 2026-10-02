using System;
using System.Linq;
using OCC.Combat.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace OCC.Combat.EditorTools
{
    public static class PixelVfxShowcaseBuilder
    {
        public const string ScenePath = "Assets/Scenes/PixelVfxShowcase.unity";
        [MenuItem("OCC/VFX/Open Pixel Showcase")]
        public static void Open()
        {
            EnsureClean();
            EditorSceneManager.OpenScene(ScenePath);
        }
        private static void EnsureClean()
        {
            if (Application.dataPath.Replace('\\','/') != "E:/数据库/OCC_Codex/UnityProject/Assets")
                throw new InvalidOperationException("Wrong OCC project.");
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before authoring the showcase.");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
                if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Existing scene has unsaved edits. Nothing was changed.");
        }
        [MenuItem("OCC/VFX/Update Showcase Effects")]
        public static void UpdateEffects()
        {
            EnsureClean();
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(scene.path!=ScenePath) throw new InvalidOperationException("Open the dedicated showcase first.");
            var lab=UnityEngine.Object.FindFirstObjectByType<PixelVfxShowcase>();
            if(lab==null || lab.modeButtons.Length!=PixelVfxShowcase.Names.Length)
                throw new InvalidOperationException("Missing showcase or unexpected button count.");
            foreach(var text in lab.integerFrame.GetComponentsInChildren<Text>(true))
            {
                Undo.RecordObject(text,"Update showcase copy");
                if(text.name=="Title") text.text="以太现象展示场";
                if(text.name=="Subtitle") text.text="火焰　电弧　折射　照明";
            }
            for(int i=0;i<lab.modeButtons.Length;i++)
                lab.modeButtons[i].GetComponentInChildren<Text>().text=PixelVfxShowcase.Names[i];
            lab.replayButton.GetComponentInChildren<Text>().text="触发效果或重播";
            lab.statusLabel.text="火焰爆破\n点击场地改变落点\n空格重播   P 暂停   O 对比";
            var subject=GameObject.Find("Shield subject");
            if(subject!=null){Undo.RecordObject(subject,"Rename VFX subject");subject.name="Effect subject";}
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        [MenuItem("OCC/VFX/Create Pixel Showcase")]
        public static void Build()
        {
            EnsureClean();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
                throw new InvalidOperationException("Showcase scene already exists. Open it instead of replacing it.");
            Shader unlit=Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            Shader effect=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Game/Runtime/Presentation/Shaders/PixelVfxShowcase.shader");
            Sprite floor=Resources.Load<Sprite>("Art/FormalAcademyIndependentFloors32/academy_block_court_a");
            Sprite hero=Resources.Load<Sprite>("Art/FormalUnits64/hero");
            Sprite enemy=Resources.Load<Sprite>("Art/FormalUnits64/pyromancer");
            Font font=Resources.Load<Font>("Fonts/FusionPixel12ProportionalZhHans");
            if(unlit==null || effect==null || floor==null || hero==null || enemy==null || font==null)
                throw new InvalidOperationException("Missing showcase shader, existing sprite or pixel font.");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var material=new Material(unlit){name="Pixel VFX Stage Unlit"};
            const string materialPath="Assets/Game/Runtime/Presentation/Shaders/PixelVfxStage.mat";
            if(AssetDatabase.LoadAssetAtPath<Material>(materialPath)!=null)
                throw new InvalidOperationException("Stage material path already exists.");
            AssetDatabase.CreateAsset(material,materialPath);
            var stage=new GameObject("Pixel VFX Stage");
            Undo.RegisterCreatedObjectUndo(stage,"Create VFX stage");
            for(int y=0;y<6;y++) for(int x=0;x<8;x++)
                Place(stage.transform,"Court "+x+" "+y,floor,new Vector2(x*32,y*32),material,0);
            Place(stage.transform,"Effect subject",hero,new Vector2(88,57),material,100);
            Place(stage.transform,"Left light occluder",enemy,new Vector2(40,65),material,90);
            Place(stage.transform,"Right light occluder",enemy,new Vector2(178,45),material,110);
            var worldCamera=new GameObject("Stage Camera").AddComponent<Camera>();
            worldCamera.transform.position=new Vector3(120f/32,90f/32,-10);
            worldCamera.orthographic=true; worldCamera.orthographicSize=90f/32;
            worldCamera.cullingMask=1<<30; worldCamera.clearFlags=CameraClearFlags.SolidColor;
            worldCamera.backgroundColor=new Color(.08f,.07f,.06f); worldCamera.depth=-1;
            worldCamera.allowHDR=false; worldCamera.allowMSAA=false;
            var screenCamera=new GameObject("Main Camera").AddComponent<Camera>();
            screenCamera.tag="MainCamera"; screenCamera.cullingMask=0;
            screenCamera.clearFlags=CameraClearFlags.SolidColor; screenCamera.backgroundColor=new Color(.06f,.055f,.045f);
            var canvas=new GameObject("Showcase Canvas",typeof(Canvas),typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            RectTransform frame=Rect(canvas.transform,"Integer Frame",new Vector2(1920,1080),Vector2.zero);
            frame.anchorMin=frame.anchorMax=frame.pivot=new Vector2(.5f,.5f);
            RawImage view=Rect(frame,"Stage View",new Vector2(1440,1080),Vector2.zero).gameObject.AddComponent<RawImage>();
            var lab=view.gameObject.AddComponent<PixelVfxShowcase>();
            lab.sourceCamera=worldCamera; lab.display=view; lab.integerFrame=frame; lab.effectShader=effect;
            lab.heroTexture=hero.texture; lab.enemyTexture=enemy.texture;
            RectTransform hud=Rect(frame,"Effects HUD",new Vector2(480,1080),new Vector2(1440,0));
            hud.gameObject.AddComponent<Image>().color=new Color(.95f,.92f,.84f);
            Label(hud,"Title","以太现象展示场",font,48,new Vector2(24,20),new Vector2(432,72));
            Label(hud,"Subtitle","火焰　电弧　折射　照明",font,24,new Vector2(24,100),new Vector2(432,36));
            string[] labels=PixelVfxShowcase.Names;
            lab.modeButtons=new Button[labels.Length];
            for(int i=0;i<labels.Length;i++) lab.modeButtons[i]=Button(hud,"Mode "+i,labels[i],font,new Vector2(24,156+i*66),new Vector2(432,54));
            lab.replayButton=Button(hud,"Replay","触发效果或重播",font,new Vector2(24,630),new Vector2(432,54));
            lab.pauseButton=Button(hud,"Pause","暂停播放",font,new Vector2(24,696),new Vector2(210,54));
            lab.slowButton=Button(hud,"Slow","三倍慢放",font,new Vector2(246,696),new Vector2(210,54));
            lab.originalButton=Button(hud,"Original","对比原画",font,new Vector2(24,762),new Vector2(432,54));
            lab.pauseLabel=lab.pauseButton.GetComponentInChildren<Text>();
            lab.slowLabel=lab.slowButton.GetComponentInChildren<Text>();
            lab.originalLabel=lab.originalButton.GetComponentInChildren<Text>();
            Label(hud,"Intensity label","光照强度",font,24,new Vector2(24,830),new Vector2(432,36));
            var slider=Rect(hud,"Light intensity",new Vector2(432,42),new Vector2(24,866)).gameObject.AddComponent<Slider>();
            slider.minValue=.2f;slider.maxValue=2;slider.value=1;
            var track=Rect(slider.transform,"Track",new Vector2(432,12),new Vector2(0,15));
            track.gameObject.AddComponent<Image>().color=new Color(.25f,.24f,.22f);
            var handleArea=Rect(slider.transform,"Handle area",new Vector2(408,42),new Vector2(12,0));
            var handle=Rect(handleArea,"Handle",new Vector2(24,42),Vector2.zero);
            Image handleImage=handle.gameObject.AddComponent<Image>();handleImage.color=new Color(.15f,.55f,.58f);
            slider.handleRect=handle;slider.targetGraphic=handleImage;lab.lightStrength=slider;
            lab.statusLabel=Label(hud,"Status","火焰爆破",font,24,new Vector2(24,930),new Vector2(432,132));
            var events=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            Undo.RegisterCreatedObjectUndo(canvas.gameObject,"Create VFX HUD");
            EditorSceneManager.SaveScene(scene,ScenePath);
            Debug.Log("OCC_PIXEL_VFX_SCENE_CREATED "+ScenePath);
        }
        private static void Place(Transform parent,string name,Sprite sprite,Vector2 lowerLeft,Material material,int order)
        {
            var obj=new GameObject(name);obj.transform.SetParent(parent,false);obj.layer=30;
            obj.transform.position=(Vector3)((lowerLeft+sprite.pivot)/32f);
            var renderer=obj.AddComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sharedMaterial=material;renderer.sortingOrder=order;
        }
        private static RectTransform Rect(Transform parent,string name,Vector2 size,Vector2 topLeft)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);
            rect.sizeDelta=size;rect.anchoredPosition=new Vector2(topLeft.x,-topLeft.y);return rect;
        }
        private static Text Label(Transform parent,string name,string value,Font font,int size,Vector2 at,Vector2 dimensions)
        {
            Text text=Rect(parent,name,dimensions,at).gameObject.AddComponent<Text>();
            text.font=font;text.fontSize=size;text.text=value;text.color=new Color(.13f,.12f,.1f);
            text.raycastTarget=false;text.supportRichText=false;return text;
        }
        private static Button Button(Transform parent,string name,string value,Font font,Vector2 at,Vector2 dimensions)
        {
            var rect=Rect(parent,name,dimensions,at);Image image=rect.gameObject.AddComponent<Image>();image.color=new Color(.84f,.8f,.72f);
            Button button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;
            Text label=Label(rect,"Label",value,font,24,Vector2.zero,dimensions);label.alignment=TextAnchor.MiddleCenter;
            return button;
        }
    }
}
