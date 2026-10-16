using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    // Authored scene poses, independent of screen pixels and camera movement.
    internal sealed partial class Hd2DLoadoutScene : MonoBehaviour
    {
        private GameObject sceneRoot;
        private AcademyDeskBackdrop backdrop;
        private bool built;
        private int frames;
        private readonly List<Material> materials=new List<Material>();
        private readonly List<Mesh> meshes=new List<Mesh>();
        private readonly List<Image> sources=new List<Image>();
        private readonly List<MeshRenderer> faces=new List<MeshRenderer>();
        private readonly List<CanvasGroup> groups=new List<CanvasGroup>();
        private const string ArtRoot="Art/Hd2DContent20261009/";
        internal static void Attach(GameObject page)=>page.AddComponent<Hd2DLoadoutScene>();
        private void LateUpdate()
        {
            if(!built)
            {
                if(++frames<3)return;
                backdrop=GetComponent<AcademyDeskBackdrop>();
                if(backdrop==null||backdrop.Stage==null)return;
                Canvas.ForceUpdateCanvases();Build();built=true;
            }
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
            backdrop.SetDocumentFraming(.15f,.38f);
            // Wider physical support for the lamp and opened organiser. Reuse
            // existing desk meshes; do not add a model for each UI element.
            foreach(var item in stage.GetComponentsInChildren<Transform>(true))
                if(item.name=="桌面实体"||item.name.StartsWith("桌面木板"))
                    item.localScale=new Vector3(item.name=="桌面实体"?9.6f:9.55f,item.localScale.y,item.localScale.z);
            stage.GetComponent<Hd2DDeskArtCandidate>()?.RefreshModelUvs();
            Hd2DContainerStyle.PrepareControls(gameObject);
            sceneRoot=new GameObject("场景空间整备陈列");sceneRoot.transform.SetParent(stage.transform,false);
            var view=GetComponentInChildren<RogueLoadoutEquipmentView>();
            var shell=GetComponentInChildren<FormalRogueliteLoadoutShellView>();
            if(shell==null)return;
            if(view!=null)
            {
            // The grid is a layout and input plane, not another opaque tray.
            foreach(var image in view.BackpackGrid.GetComponents<Image>())image.color=Color.clear;
            backdrop.SetDocumentFraming(.15f,.10f);
            Place(view.transform.Find("装备工作区/战术栏工作区") as RectTransform,new Vector3(-.10f,1.74f,-.90f),new Vector3(58,0,0),1.40f);
            var slotCentre=Vector2.zero;
            foreach(var slot in view.EquipmentSlots)slotCentre+=slot.Root.anchoredPosition;
            slotCentre/=view.EquipmentSlots.Length;
            foreach(var slot in view.EquipmentSlots)
            {
                slot.Root.anchoredPosition=slotCentre+(slot.Root.anchoredPosition-slotCentre)*1.44f;
                slot.Root.sizeDelta=new Vector2(240,240);
            }
            foreach(var slot in view.TacticalSlots)slot.Root.sizeDelta=new Vector2(190,170);
            Place(view.CharacterContentRoot.parent as RectTransform,new Vector3(-3.25f,2.55f,-.10f),new Vector3(16,0,0),1.4375f);
            Place(view.transform.Find("装备工作区") as RectTransform,new Vector3(-.15f,2.45f,.65f),new Vector3(12,0,0),1.50f);
            Place(view.BackpackPanel,new Vector3(3.00f,2.75f,-.70f),new Vector3(58,0,0),1.20f);
            PrepareNativeCharacter(view);
            foreach(var label in view.CharacterContentRoot.parent.GetComponentsInChildren<Text>(true))
                if(label.text=="角色状态")label.rectTransform.anchoredPosition+=new Vector2(0,-20);
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
                Place(button.transform as RectTransform,new Vector3(-1.70f+tab++*1.80f,1.32f,-1.65f),new Vector3(58,0,0));
            }
            foreach(var button in shell.Footer.GetComponentsInChildren<Button>())
            {
                ApplyPlaque(button,true);
                Place(button.transform as RectTransform,new Vector3(1.90f,1.32f,-1.65f),new Vector3(58,0,0));
            }
            if(view!=null)BuildDossier(stage);
            else PrepareSpellReading();
            AddNativeFrames(stage,view);
            SizeNativeArtwork();
            if(view!=null)view.BackpackPanel.GetComponent<Image>().color=new Color(.85f,.77f,.64f,1);
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
            var lightObject=new GameObject("整备陈列柔光",typeof(Light));lightObject.transform.SetParent(sceneRoot.transform,false);
            lightObject.transform.position=stage.transform.TransformPoint(new Vector3(1,4,-2));
            var light=lightObject.GetComponent<Light>();light.type=LightType.Point;light.range=10;light.intensity=2.5f;
            light.color=new Color(.96f,.98f,1);light.cullingMask=1<<stage.gameObject.layer;light.shadows=LightShadows.None;
            foreach(var roomLight in stage.GetComponentsInChildren<Light>(true))
            {
                if(roomLight.name=="窗外统一日光")
                {
                    roomLight.intensity=1.10f;roomLight.color=new Color(1,.97f,.91f);
                    roomLight.transform.localRotation=Quaternion.Euler(50,-25,0);
                }
                else if(roomLight.name=="窗边柔和补光")roomLight.intensity=2.4f;
                else if(roomLight.name=="室内暖补光")roomLight.intensity=1.2f;
            }
            AddTaskLamp(stage);
            Canvas.ForceUpdateCanvases();
            int layer=0;
            foreach(var image in sceneRoot.GetComponentsInChildren<Image>(true))
            {
                // The live slider geometry changes with input; leave it on its world Canvas.
                if(image.GetComponentInParent<Hd2DDeskLampControl>()!=null)continue;
                if(!image.isActiveAndEnabled||image.color.a<.85f)continue;
                image.rectTransform.localPosition+=new Vector3(0,0,-layer++*.15f);
                var generated=image.canvasRenderer.GetMesh();if(generated==null||generated.vertexCount==0)continue;
                var mesh=Instantiate(generated);mesh.RecalculateNormals();
                var vertexColors=new Color32[mesh.vertexCount];
                for(int vertex=0;vertex<vertexColors.Length;vertex++)vertexColors[vertex]=new Color32(255,255,255,255);
                mesh.colors32=vertexColors;meshes.Add(mesh);
                var face=new GameObject(image.name+" 场景面",typeof(MeshFilter),typeof(MeshRenderer));
                face.layer=stage.gameObject.layer;face.transform.SetParent(image.transform,false);
                face.transform.localPosition=new Vector3(0,0,.7f);
                face.GetComponent<MeshFilter>().sharedMesh=mesh;
                var material=new Material(stage.bookLitMaterial){mainTexture=image.mainTexture,color=image.color};
                material.SetFloat("_Smoothness",.025f);material.SetFloat("_Metallic",0);materials.Add(material);
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
            // Compact rows carry the slot number and costs. The selected spell
            // keeps its native-sized illustration in the full detail panel.
            var work=sceneRoot.transform.Find("术式工作区 场景承载/术式工作区");
            if(work!=null)
                foreach(var image in work.GetComponentsInChildren<Image>(true))
                    if(image.name=="操作图标")image.color=Color.clear;
            if(work!=null)
                foreach(var button in work.GetComponentsInChildren<Button>(true))
                    foreach(var label in button.GetComponentsInChildren<Text>(true))
                    {
                        label.rectTransform.anchoredPosition=new Vector2(22,label.rectTransform.anchoredPosition.y);
                        label.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,438);
                    }
            var details=sceneRoot.transform.Find("术式详情 场景承载/术式详情");
            if(details!=null)
                foreach(var image in details.GetComponentsInChildren<Image>(true))
                    if(image.name=="图标"&&image.transform.parent.name!="术式图标")
                    {
                        image.color=Color.clear;
                        var label=image.transform.parent.Find("读数")?.GetComponent<Text>();
                        if(label!=null){label.rectTransform.anchoredPosition=new Vector2(0,-12);label.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,164);}
                    }
        }
        private void SizeNativeArtwork()
        {
            foreach(var image in sceneRoot.GetComponentsInChildren<Image>(true))
            {
                var sprite=image.sprite;
                if(sprite==null||image.color.a<.01f||image.GetComponent<Hd2DContainerStyle>()!=null||
                    image.type!=Image.Type.Simple)continue;
                if(image.name=="语义图标")
                {
                    // Compact resource lines already name the resource explicitly.
                    image.color=Color.clear;
                    var label=image.transform.parent.Find("读数")?.GetComponent<Text>();
                    if(label!=null)
                    {
                        label.rectTransform.anchoredPosition=new Vector2(14,-12);
                        label.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,174);
                    }
                    continue;
                }
                var rect=image.rectTransform;
                var scale=rect.lossyScale;
                if(Mathf.Abs(scale.x)<.00001f||Mathf.Abs(scale.y)<.00001f)continue;
                var centre=rect.TransformPoint(rect.rect.center);
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,sprite.rect.width/(32*Mathf.Abs(scale.x)));
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,sprite.rect.height/(32*Mathf.Abs(scale.y)));
                rect.position+=centre-rect.TransformPoint(rect.rect.center);
                image.preserveAspect=true;
            }
        }
        private void AddContactShadow(Image image,Mesh mesh,MainMenuHd2DStage stage)
        {
            var shadow=new GameObject(image.name+" 接触阴影",typeof(MeshFilter),typeof(MeshRenderer));
            shadow.layer=stage.gameObject.layer;shadow.transform.SetParent(image.transform.parent,false);
            shadow.transform.localPosition=image.transform.localPosition+new Vector3(4,-6,12);
            shadow.transform.localRotation=image.transform.localRotation;shadow.transform.localScale=image.transform.localScale;
            shadow.GetComponent<MeshFilter>().sharedMesh=mesh;
            var shader=Shader.Find("OCC/HD2D/ContactShadow");
            if(shader==null){Destroy(shadow);return;}
            var material=new Material(shader);materials.Add(material);
            material.mainTexture=image.mainTexture;material.color=new Color(.055f,.035f,.025f,.24f);
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
            image.sprite=sprite;image.type=compact?Image.Type.Tiled:Image.Type.Simple;image.color=Color.white;
            image.pixelsPerUnitMultiplier=compact?.32f:1;
            var rect=button.transform as RectTransform;
            rect.sizeDelta=compact?new Vector2(460,110):sprite.rect.size/(32*.0032f);
            foreach(var child in button.GetComponentsInChildren<Image>(true))
                if(child!=image)child.color=Color.clear;
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
                label.alignment=TextAnchor.MiddleCenter;label.fontSize=26;
            }
        }
        private void BuildDossier(MainMenuHd2DStage stage)
        {
            var sprite=Resources.Load<Sprite>(ArtRoot+"student_dossier");if(sprite==null)return;
            var referenceLabel=GetComponentInChildren<Text>(true);
            if(referenceLabel==null)referenceLabel=sceneRoot.GetComponentInChildren<Text>(true);
            var book=new GameObject("实体档案册入口",typeof(RectTransform),typeof(Image),typeof(Button));
            book.transform.SetParent(transform,false);
            var image=book.GetComponent<Image>();image.sprite=sprite;image.color=Color.white;
            var rect=book.GetComponent<RectTransform>();rect.sizeDelta=sprite.rect.size/(32*.0032f);
            var button=book.GetComponent<Button>();button.targetGraphic=image;
            button.onClick.AddListener(()=>
            {
                var ui=FindFirstObjectByType<FormalRogueliteUi>();if(ui!=null)ui.OpenJourneyArchive();
            });
            var label=new GameObject("档案册页签",typeof(RectTransform),typeof(Text));
            label.transform.SetParent(rect,false);
            var text=label.GetComponent<Text>();text.font=referenceLabel!=null?referenceLabel.font:null;text.text="角色\n档案";
            text.fontSize=26;text.alignment=TextAnchor.MiddleCenter;text.color=new Color(.24f,.19f,.12f);
            text.raycastTarget=false;var labelRect=text.rectTransform;
            labelRect.anchorMin=new Vector2(.05f,.2f);labelRect.anchorMax=new Vector2(.47f,.8f);
            labelRect.offsetMin=labelRect.offsetMax=Vector2.zero;
            Place(rect,new Vector3(-2.70f,1.75f,-1.00f),new Vector3(78,0,0),.8f,false);
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
        private void BuildNativeProp(MainMenuHd2DStage stage,string name,Vector3 position)
        {
            var sprite=Resources.Load<Sprite>(ArtRoot+name);if(sprite==null)return;
            var prop=new GameObject("原生2D "+name,typeof(SpriteRenderer));
            prop.layer=stage.gameObject.layer;prop.transform.SetParent(sceneRoot.transform,false);
            prop.transform.position=stage.transform.TransformPoint(position);
            prop.transform.rotation=stage.transform.rotation*Quaternion.Euler(58,0,0);
            var renderer=prop.GetComponent<SpriteRenderer>();renderer.sprite=sprite;
            var material=new Material(stage.bookLitMaterial){mainTexture=sprite.texture,color=Color.white};
            material.SetFloat("_Smoothness",.08f);materials.Add(material);
            renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.On;
        }
        private void Place(RectTransform region,Vector3 position,Vector3 angles,float displayScale=1f,bool faceCamera=true)
        {
            if(region==null)return;
            var stage=backdrop.Stage;
            Vector2 size=region.rect.size;
            var host=new GameObject(region.name+" 场景承载",typeof(RectTransform),typeof(Canvas),typeof(Hd2DSceneGraphicRaycaster));
            host.transform.SetParent(sceneRoot.transform,false);host.layer=stage.gameObject.layer;
            var rect=host.GetComponent<RectTransform>();rect.sizeDelta=size;
            rect.position=stage.transform.TransformPoint(position);
            // Readable interface faces share the camera plane while retaining world depth.
            rect.rotation=faceCamera?stage.sceneCamera.transform.rotation:stage.transform.rotation*Quaternion.Euler(angles);
            float parentScale=rect.parent.lossyScale.x;
            rect.localScale=Vector3.one*(.0032f*displayScale*.95f/parentScale);
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
            if(sceneRoot!=null)Destroy(sceneRoot);
            foreach(var material in materials)if(material!=null)Destroy(material);
            foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);
            if(reviewSprite!=null)Destroy(reviewSprite);
            if(reviewTexture!=null)Destroy(reviewTexture);
            foreach(var artwork in ownedArtwork)if(artwork!=null)Destroy(artwork);
        }
    }
    internal sealed class Hd2DSceneGraphicRaycaster : GraphicRaycaster
    {
        public override void Raycast(PointerEventData eventData,List<RaycastResult> results)
        {
            Vector2 original=eventData.position;
            var camera=eventCamera;
            if(camera!=null&&camera.targetTexture!=null)
                eventData.position=new Vector2(original.x*camera.pixelWidth/Screen.width,original.y*camera.pixelHeight/Screen.height);
            base.Raycast(eventData,results);eventData.position=original;
        }
    }
    internal sealed class Hd2DSceneControl:MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerEnterHandler,IPointerExitHandler
    {
        private bool pressed,hovered;
        private float offset;
        private MeshRenderer shadow;
        private MaterialPropertyBlock shadowBlock;
        private Button button;
        private void Awake()=>button=GetComponent<Button>();
        internal void SetShadow(MeshRenderer value){shadow=value;shadowBlock=new MaterialPropertyBlock();}
        private void LateUpdate()
        {
            if(button!=null&&!button.IsInteractable())pressed=hovered=false;
            float next=Mathf.MoveTowards(offset,pressed?5:hovered?-3:0,Time.unscaledDeltaTime*60);
            transform.localPosition+=new Vector3(0,0,next-offset);offset=next;
            if(shadow!=null)
            {
                shadowBlock.SetColor("_BaseColor",new Color(.055f,.035f,.025f,Mathf.Lerp(.18f,.30f,(offset+3)/8)));
                shadow.SetPropertyBlock(shadowBlock);
            }
        }
        public void OnPointerDown(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)pressed=true;}
        public void OnPointerUp(PointerEventData e)=>pressed=false;
        public void OnPointerEnter(PointerEventData e)=>hovered=true;
        public void OnPointerExit(PointerEventData e){pressed=hovered=false;}
    }
}
