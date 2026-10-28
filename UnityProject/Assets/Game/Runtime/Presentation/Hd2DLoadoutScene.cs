using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    // Authored scene carriers with a shared, readable loadout writing plane.
    internal sealed partial class Hd2DLoadoutScene : MonoBehaviour
    {
        private GameObject sceneRoot;
        private Hd2DPageStructure structure;
        private AcademyDeskBackdrop backdrop;
        private bool built;
        private readonly List<Material> materials=new List<Material>();
        private readonly List<Mesh> meshes=new List<Mesh>();
        private readonly List<Image> sources=new List<Image>();
        private readonly List<MeshRenderer> faces=new List<MeshRenderer>();
        private readonly List<CanvasGroup> groups=new List<CanvasGroup>();
        private const string ArtRoot="Art/Hd2DContent20261009/";
        internal static void Attach(GameObject page){Hd2DPageVisibility.Begin(page);page.AddComponent<Hd2DLoadoutScene>();}
        private void OnEnable(){Canvas.willRenderCanvases+=HideSourceGraphics;if(readingRoot!=null)readingRoot.SetActive(true);}
        private void OnDisable(){Canvas.willRenderCanvases-=HideSourceGraphics;if(readingRoot!=null)readingRoot.SetActive(false);}
        private void HideSourceGraphics()
        {
            foreach(var source in sources)if(source!=null)source.canvasRenderer.SetAlpha(0);
            RefreshReadingText();
        }
        private void LateUpdate()
        {
            if(!built)
            {
                backdrop=GetComponent<AcademyDeskBackdrop>();
                if(backdrop==null||backdrop.Stage==null)return;
                Canvas.ForceUpdateCanvases();Build();built=true;
                Hd2DPageVisibility.Complete(gameObject);
            }
            RefreshReadingText();
            for(int i=0;i<sources.Count;i++)
            {
                var image=sources[i];if(image==null)continue;
                var group=groups[i];
                faces[i].enabled=image.isActiveAndEnabled&&(group==null||group.alpha>=.5f);
                if(faces[i].sharedMaterial.color!=image.color)faces[i].sharedMaterial.color=image.color;
                image.canvasRenderer.SetAlpha(0);
            }
        }
        private void Build()
        {
            var stage=backdrop.Stage;
            Hd2DContainerStyle.PrepareControls(gameObject);
            var view=GetComponentInChildren<RogueLoadoutEquipmentView>();
            var shell=GetComponentInChildren<FormalRogueliteLoadoutShellView>();
            if(shell==null)return;
            structure=Hd2DPageStructure.Create(view!=null?"loadout-equipment":"loadout-spells",stage);
            sceneRoot=structure.gameObject;
            structure.ApplyBackdrop(backdrop,stage);
            if(view!=null)
            {
            RestoreGroupSurfaces(view);
            // The grid is a layout and input plane, not another opaque tray.
            foreach(var image in view.BackpackGrid.GetComponents<Image>())image.color=Color.clear;
            var tacticalWriting=view.transform.Find("装备工作区/战术栏工作区") as RectTransform;
            Place(tacticalWriting,new Vector3(-.10f,1.74f,-.90f),new Vector3(58,0,0),1.40f);
            Place(view.CharacterContentRoot.parent as RectTransform,new Vector3(-3.25f,2.55f,-.10f),new Vector3(16,0,0),1.4375f);
            Place(view.transform.Find("装备工作区") as RectTransform,new Vector3(-.15f,2.45f,.65f),new Vector3(12,0,0),1.50f);
            Place(view.BackpackPanel,new Vector3(3.00f,2.75f,-.70f),new Vector3(58,0,0),1.20f);
            PrepareNativeCharacter(view);
            var backpackSurface=view.BackpackPanel.GetComponent<Image>();
            backpackSurface.sprite=null;backpackSurface.type=Image.Type.Simple;
            backpackSurface.color=new Color(.78f,.68f,.49f,1);
            foreach(var image in view.BackpackPanel.GetComponentsInChildren<Image>(true))
            {
                if(image.name.Contains("empty_inventory")){image.gameObject.SetActive(false);continue;}
                if(image.name.StartsWith("背包格_"))
                {image.sprite=null;image.type=Image.Type.Simple;image.color=ArchiveUiStyle.Paper;image.raycastTarget=false;}
            }
            }
            else
            {
                Place(shell.Body.Find("术式工作区") as RectTransform,new Vector3(-1.38f,2.55f,.05f),new Vector3(18,0,0),1.35f);
                Place(shell.Body.Find("术式详情") as RectTransform,new Vector3(2.20f,2.55f,.05f),new Vector3(18,0,0),1.35f);
            }
            foreach(var label in GetComponentsInChildren<Text>(true))
                if(label.name=="整备摘要")
                {
                    if(view==null){label.gameObject.SetActive(false);continue;}
                    var rect=label.rectTransform;rect.SetParent(view.BackpackPanel,false);
                    rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
                    rect.anchoredPosition=new Vector2(420,-18);rect.sizeDelta=new Vector2(200,42);
                    label.fontSize=20;label.alignment=TextAnchor.MiddleRight;
                }
            int tab=0;
            foreach(var button in shell.Navigation.GetComponentsInChildren<Button>())
            {
                ApplyPlaque(button,true);
                foreach(var icon in button.GetComponentsInChildren<Image>(true))
                    if(icon.name=="操作图标")icon.gameObject.SetActive(false);
                Place(button.transform as RectTransform,new Vector3(-1.45f+tab++*1.95f,1.32f,-1.65f),new Vector3(58,0,0));
            }
            foreach(var button in shell.Footer.GetComponentsInChildren<Button>())
            {
                ApplyPlaque(button,true);
                foreach(var icon in button.GetComponentsInChildren<Image>(true))
                    if(icon.name=="操作图标")icon.gameObject.SetActive(false);
                Place(button.transform as RectTransform,new Vector3(2.45f,1.32f,-1.65f),new Vector3(58,0,0));
            }
            if(view!=null)BuildDossier(stage);
            else PrepareSpellReading();
            ComposeLoadout(stage,view);
            AddNativeFrames(stage,view);
            if(view==null)QuietEmptySpellSlots();
            SizeNativeArtwork(view);
            foreach(var label in GetComponentsInChildren<Text>(true))
            {
                var background=label.GetComponentInParent<Image>();
                label.color=background!=null&&background.color.a>.85f&&background.color.r>.6f
                    ?ArchiveUiStyle.Ink:new Color(.98f,.90f,.71f);
            }
            foreach(var label in sceneRoot.GetComponentsInChildren<Text>(true))
            {
                bool onPaper=false;
                for(var parent=label.transform.parent;parent!=null&&parent!=sceneRoot.transform;parent=parent.parent)
                    if(parent.TryGetComponent<Image>(out var surface)&&surface.color.a>.85f)
                    {onPaper=surface.sprite==null||(surface.GetComponent<Hd2DContainerStyle>()!=null&&surface.sprite.texture.name!="action_plaque");break;}
                label.color=onPaper||label.name=="档案册页签"||label.name=="角色栏标题"?ArchiveUiStyle.Ink:new Color(.98f,.90f,.71f);
            }
            foreach(var overlayImage in GetComponentsInChildren<Image>(true))overlayImage.raycastTarget=false;
            stage.lampLight.enabled=false;
            // Clear desk props where the opened case sits; the architectural room remains.
            foreach(string name in new[]{"独立桌边植物","独立以太灯","灯内晶体","实体茶碟","实体杯身","杯中茶面","杯把外侧","杯把接头","独立像素杯碟候选","桌上固定书脊"})
            {var item=stage.transform.Find(name);if(item!=null)item.gameObject.SetActive(false);}
            AddTaskLamp(stage);
            ConfigureRealisticLighting(stage);
            ApplyWorkshopArt(view);
            BuildReadingText(stage);
            Canvas.ForceUpdateCanvases();
            foreach(var image in sceneRoot.GetComponentsInChildren<Image>(true))
            {
                // The live slider geometry changes with input; leave it on its world Canvas.
                if(image.GetComponentInParent<Hd2DDeskLampControl>()!=null)continue;
                if(!image.isActiveAndEnabled||image.color.a<.85f)continue;
                int depth=1;
                for(var parent=image.transform.parent;parent!=null&&parent.GetComponent<Canvas>()==null;parent=parent.parent)depth++;
                // Depth separates nested faces without making later inventory
                // entries progressively larger or shifting them out of the grid.
                image.rectTransform.localPosition+=new Vector3(0,0,-depth*.10f);
                var generated=image.canvasRenderer.GetMesh();if(generated==null||generated.vertexCount==0)continue;
                var mesh=Instantiate(generated);mesh.RecalculateNormals();
                var vertexColors=new Color32[mesh.vertexCount];
                for(int vertex=0;vertex<vertexColors.Length;vertex++)vertexColors[vertex]=new Color32(255,255,255,255);
                mesh.colors32=vertexColors;meshes.Add(mesh);
                var face=Hd2DPageStructure.CreateSurface(image.transform,image.name+" 场景面");
                face.layer=stage.gameObject.layer;face.transform.SetParent(image.transform,false);
                face.transform.localPosition=new Vector3(0,0,.7f);
                face.GetComponent<MeshFilter>().sharedMesh=mesh;
                var material=Hd2DSurfaceProfile.Create(image.mainTexture,image.color,Hd2DSurfaceRole.Paper,true);
                Hd2DDeskReadability.Current.Apply(material,image);
                materials.Add(material);
                var renderer=face.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
                renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;
                // Flat cell guides sit on the case paper; they are not elevated objects.
                if(image.name.StartsWith("背包格_"))renderer.shadowCastingMode=ShadowCastingMode.Off;
                sources.Add(image);faces.Add(renderer);image.canvasRenderer.SetAlpha(0);
                groups.Add(image.GetComponentInParent<CanvasGroup>());
                if(image.GetComponent<Button>()!=null||(view!=null&&image==view.BackpackPanel.GetComponent<Image>()))
                    AddContactShadow(image,mesh,stage);
                // Sprites carry the visible form. Geometry is reserved for the
                // actual case support, not a separate box model for every cell.
            }
        }
        private void PrepareNativeCharacter(RogueLoadoutEquipmentView view)
        {
            var root=view.CharacterContentRoot;
            Text passiveName=null,passiveSummary=null,passiveHeading=null;
            foreach(var label in root.GetComponentsInChildren<Text>(true))
            {
                if(label.name=="被动名称")passiveName=label;
                if(label.name=="被动说明")passiveSummary=label;
                if(label.name=="被动标题")passiveHeading=label;
            }
            if(passiveName!=null&&passiveName.text=="尚未获得被动术式")
            {
                if(passiveHeading!=null)passiveHeading.text="被动：暂无";
                passiveName.gameObject.SetActive(false);
                if(passiveSummary!=null)passiveSummary.gameObject.SetActive(false);
            }
            foreach(var image in root.GetComponentsInChildren<Image>(true))
                if(image.name=="角色像素像")
                {
                    image.sprite=Resources.Load<Sprite>(FormalArtRegistry.UnitPath("hero"));
                    if(image.transform.parent is RectTransform portrait)
                        portrait.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,400);
                }
            foreach(var rect in root.GetComponentsInChildren<RectTransform>(true))
            {
                float y=rect.name=="资源_生命"||rect.name=="资源_魔力"?-480:
                    rect.name=="资源_金币"||rect.name=="资源_贡献"?-552:
                    rect.name=="被动标题"?-624:rect.name=="被动名称"?-656:
                    rect.name=="被动说明"?-706:float.NaN;
                if(!float.IsNaN(y))rect.anchoredPosition=new Vector2(rect.anchoredPosition.x,y);
            }
        }
        private void PrepareSpellReading()
        {
            // Slot art, identity and cost stay together; full rules remain in the detail sheet.
            var work=sceneRoot.transform.Find("术式工作区/术式工作区");
            if(work==null)return;
            foreach(var image in work.GetComponentsInChildren<Image>(true))
                if(image.name=="操作图标")image.color=Color.white;
        }
        private void QuietEmptySpellSlots()
        {
            foreach(var button in sceneRoot.GetComponentsInChildren<Button>(true))
            {
                var label=button.transform.Find("名称")?.GetComponent<Text>();
                if(label==null||!label.text.Contains("空槽"))continue;
                var art=button.transform.Find("操作图标");if(art!=null)art.gameObject.SetActive(false);
                if(button.targetGraphic is Image image)image.color=new Color(.78f,.75f,.67f);
            }
        }
        private void RestoreGroupSurfaces(RogueLoadoutEquipmentView view)
        {
            var regions=new[]{view.CharacterContentRoot.parent as RectTransform,view.transform.Find("装备工作区") as RectTransform,view.BackpackPanel};
            var titles=new[]{"角色状态","装备与战术栏","背包"};
            var tactical=view.transform.Find("装备工作区/战术栏工作区") as RectTransform;
            if(tactical!=null)
            {
                var image=tactical.GetComponent<Image>();if(image!=null)Hd2DContainerStyle.Tray(image);
                foreach(var label in tactical.GetComponentsInChildren<Text>(true))if(label.name=="快捷栏标题")label.gameObject.SetActive(false);
                FormalUiKit.Label("战术区域标题","战术栏",tactical,new Vector2(24,-12),new Vector2(240,40),24,ArchiveUiStyle.Ink,TextAnchor.MiddleLeft);
            }
            for(int i=0;i<regions.Length;i++)
            {
                var region=regions[i];if(region==null)continue;
                var image=region.GetComponent<Image>();if(image!=null)Hd2DContainerStyle.Tray(image);
                foreach(var label in region.GetComponentsInChildren<Text>(true))
                    if(label.name=="角色栏标题"||label.name=="装备区标题"||label.name=="背包标题")label.gameObject.SetActive(false);
                FormalUiKit.Label("区域标题_"+i,titles[i],region,new Vector2(36,-24),new Vector2(region.rect.width-72,48),24,ArchiveUiStyle.Ink,TextAnchor.MiddleLeft);
            }
        }
        private void SizeNativeArtwork(RogueLoadoutEquipmentView view)
        {
            var spellPanel=view==null?Region("术式工作区"):null;
            foreach(var image in sceneRoot.GetComponentsInChildren<Image>(true))
            {
                var sprite=image.sprite;
                if(sprite==null||image.color.a<.01f||image.GetComponent<Hd2DContainerStyle>()!=null||
                    image.type!=Image.Type.Simple)continue;
                var rect=image.rectTransform;
                var scale=rect.lossyScale;
                if(Mathf.Abs(scale.x)<.00001f||Mathf.Abs(scale.y)<.00001f)continue;
                var centre=rect.TransformPoint(rect.rect.center);
                // User-approved 2D loadout thumbnails: two reference UI pixels
                // per source pixel, independent of the scene's world pixel pitch.
                bool thumbnail=image.name=="图标"&&image.transform.parent.name.StartsWith("背包物品_");
                if(view!=null)foreach(var slot in view.TacticalSlots)if(slot.Icon==image)thumbnail=true;
                bool equipmentIcon=false;
                if(view!=null)foreach(var slot in view.EquipmentSlots)if(slot.Icon==image){equipmentIcon=true;break;}
                bool metricIcon=(image.name=="图标"||image.name=="语义图标")&&image.transform.parent is RectTransform metricParent&&
                    (metricParent.name.StartsWith("指标_")||metricParent.name.StartsWith("资源_"));
                bool spellIcon=spellPanel!=null&&image.name=="操作图标"&&image.transform.IsChildOf(spellPanel);
                if(thumbnail||equipmentIcon||metricIcon||spellIcon)
                {
                    Hd2DUiIconSizing.Apply(image,thumbnail?2:4);
                }
                else
                {
                    rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,sprite.rect.width/(32*Mathf.Abs(scale.x)));
                    rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,sprite.rect.height/(32*Mathf.Abs(scale.y)));
                    rect.position+=centre-rect.TransformPoint(rect.rect.center);
                }
                image.preserveAspect=true;
                if((image.name=="图标"||image.name=="语义图标")&&image.transform.parent is RectTransform metric&&
                    (metric.name.StartsWith("指标_")||metric.name.StartsWith("资源_")))
                {
                    rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
                    rect.anchoredPosition=new Vector2(4,-Mathf.Max(4,(metric.rect.height-rect.rect.height)*.5f));
                    var value=metric.Find("数值")?.GetComponent<Text>()??metric.Find("读数")?.GetComponent<Text>();
                    if(value!=null)
                    {
                        float left=rect.rect.width+12;
                        value.rectTransform.anchoredPosition=new Vector2(left,-Mathf.Max(4,(metric.rect.height-32)*.5f));
                        value.rectTransform.sizeDelta=new Vector2(Mathf.Max(32,metric.rect.width-left-4),32);
                    }
                }
            }
        }
        private void AddContactShadow(Image image,Mesh mesh,MainMenuHd2DStage stage)
        {
            var shadow=Hd2DPageStructure.CreateSurface(image.transform.parent,image.name+" 接触阴影");
            shadow.layer=stage.gameObject.layer;shadow.transform.SetParent(image.transform.parent,false);
            shadow.transform.localPosition=image.transform.localPosition+new Vector3(4,-6,12);
            shadow.transform.localRotation=image.transform.localRotation;shadow.transform.localScale=image.transform.localScale;
            shadow.GetComponent<MeshFilter>().sharedMesh=mesh;
            var shader=Shader.Find("OCC/HD2D/ContactShadow");
            if(shader==null){Destroy(shadow);return;}
            var material=new Material(shader);materials.Add(material);
            material.mainTexture=image.mainTexture;material.color=new Color(.055f,.035f,.025f,.10f);
            var renderer=shadow.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            // Shadow remains on the supporting layer while the control moves.
            var control=image.GetComponent<Hd2DSceneControl>();if(control!=null)control.SetShadow(renderer);
        }
        private void ApplyPlaque(Button button,bool compact=false)
        {
            var sprite=Resources.Load<Sprite>(ArtRoot+"action_plaque");
            var image=button.targetGraphic as Image;
            if(sprite==null||image==null)return;
            // Native artwork determines the physical size; never stretch it to
            // fit the previous screen button's aspect ratio.
            if(compact)
            {
                var tiled=Sprite.Create(sprite.texture,sprite.rect,new Vector2(.5f,.5f),32,0,SpriteMeshType.FullRect,new Vector4(9,3,9,3));
                ownedArtwork.Add(tiled);sprite=tiled;
            }
            image.sprite=sprite;FormalUiChrome.Apply(image,UiChromeRole.Button);image.color=Color.white;
            image.pixelsPerUnitMultiplier=compact?.32f:1;
            var rect=button.transform as RectTransform;
            rect.sizeDelta=compact?new Vector2(460,110):sprite.rect.size/(32*.0032f);
            foreach(var child in button.GetComponentsInChildren<Image>(true))
                if(child.name=="正式皮肤"&&child.transform.parent==button.transform)child.color=Color.clear;
            var colors=button.colors;colors.normalColor=Color.white;
            colors.highlightedColor=new Color(1,1,.90f);colors.selectedColor=colors.highlightedColor;
            colors.pressedColor=new Color(.80f,.85f,.80f);colors.disabledColor=new Color(.45f,.45f,.45f);
            button.colors=colors;
            int labelIndex=0;
            foreach(var label in button.GetComponentsInChildren<Text>(true))
            {
                if(string.IsNullOrEmpty(label.text))continue;
                if(compact&&labelIndex++>0){label.gameObject.SetActive(false);continue;}
                var labelRect=label.rectTransform;
                labelRect.anchorMin=Vector2.zero;labelRect.anchorMax=Vector2.one;
                labelRect.offsetMin=new Vector2(50,20);labelRect.offsetMax=new Vector2(-50,-20);
                label.alignment=TextAnchor.MiddleCenter;label.fontSize=24;label.resizeTextForBestFit=false;
            }
        }
        private void BuildDossier(MainMenuHd2DStage stage)
        {
            var sprite=Resources.Load<Sprite>(ArtRoot+"student_dossier");if(sprite==null)return;
            var referenceLabel=GetComponentInChildren<Text>(true);
            if(referenceLabel==null)referenceLabel=sceneRoot.GetComponentInChildren<Text>(true);
            var book=Hd2DPageStructure.CreateContent("DossierEntry",transform,"实体档案册入口");
            var rect=book.GetComponent<RectTransform>();
            var button=book.GetComponent<Button>();
            button.onClick.AddListener(()=>
            {
                var ui=FindFirstObjectByType<FormalRogueliteUi>();if(ui!=null)ui.OpenJourneyArchive();
            });
            foreach(var icon in book.GetComponentsInChildren<Image>(true))if(icon.gameObject!=book)icon.gameObject.SetActive(false);
            ApplyPlaque(button,true);
            var text=book.GetComponentInChildren<Text>(true);
            if(text!=null){text.gameObject.SetActive(true);text.text="行程与行囊";}
            text.font=referenceLabel!=null?referenceLabel.font:null;
            Place(rect,new Vector3(-3.45f,1.32f,-1.65f),new Vector3(58,0,0));
        }
        private void PrepareReviewTray(RogueLoadoutEquipmentView view)
        {
#if UNITY_EDITOR
            // Decoded review candidate only. It stays outside Resources until
            // the new organiser has its own human acceptance.
            string path=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,
                "../../Worldbuilding/归档/2026-10-09_HD2D分层资产/收纳内盘/delivery.png"));
            if(!System.IO.File.Exists(path))return;
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
            texture.LoadImage(System.IO.File.ReadAllBytes(path));
            texture.filterMode=FilterMode.Point;texture.wrapMode=TextureWrapMode.Clamp;
            var sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),
                32,0,SpriteMeshType.FullRect,new Vector4(3,5,3,4));
            reviewTexture=texture;reviewSprite=sprite;
            var image=view.BackpackPanel.GetComponent<Image>();
            image.sprite=sprite;image.type=Image.Type.Tiled;
            // Repeat original felt pixels between intact corner strips; never
            // interpolate the raster or stretch its native rim pixels.
            image.pixelsPerUnitMultiplier=.32f;image.color=Color.white;
            foreach(var child in view.BackpackPanel.GetComponentsInChildren<Image>(true))
                if(child!=image && child.name.Contains("empty_inventory"))child.gameObject.SetActive(false);
#endif
        }
        private Texture2D reviewTexture;
        private Sprite reviewSprite;
        private void Place(RectTransform region,Vector3 position,Vector3 angles,float displayScale=1f,bool faceCamera=true)
        {
            if(region==null)return;
            var stage=backdrop.Stage;
            Vector2 size=region.rect.size;
            bool authored;var rect=structure.AcquireCarrier(region.name,out authored);
            var host=rect.gameObject;host.layer=stage.gameObject.layer;rect.sizeDelta=size;
            // Readable interface faces share the camera plane while retaining world depth.
            if(!authored)
            {
                rect.position=stage.transform.TransformPoint(position);
                rect.rotation=faceCamera?stage.sceneCamera.transform.rotation:stage.transform.rotation*Quaternion.Euler(angles);
                float parentScale=rect.parent.lossyScale.x;
                rect.localScale=Vector3.one*(.0032f*displayScale*.95f/parentScale);
            }
            var canvas=host.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=stage.sceneCamera;
            canvas.overrideSorting=true;canvas.sortingOrder=0;
            region.SetParent(rect,false);region.anchorMin=region.anchorMax=region.pivot=new Vector2(.5f,.5f);
            region.anchoredPosition=Vector2.zero;region.localScale=Vector3.one;region.localRotation=Quaternion.identity;
            foreach(var button in region.GetComponentsInChildren<Button>(true))
            {
                button.GetComponent<UiButtonFeedback>()?.UseSceneSpaceFeedback();
                if(button.GetComponent<Hd2DSceneControl>()==null)button.gameObject.AddComponent<Hd2DSceneControl>();
            }
            foreach(var child in region.GetComponentsInChildren<Transform>(true))child.gameObject.layer=stage.gameObject.layer;
        }
        private void OnDestroy()
        {
            if(readingRoot!=null)Destroy(readingRoot);
            if(sceneRoot!=null)Destroy(sceneRoot);
            foreach(var material in materials)if(material!=null)Destroy(material);
            foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);
            if(reviewSprite!=null)Destroy(reviewSprite);
            if(reviewTexture!=null)Destroy(reviewTexture);
            foreach(var artwork in ownedArtwork)if(artwork!=null)Destroy(artwork);
        }
    }
}
