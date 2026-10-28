using System;
using UnityEngine;
using UnityEngine.UI;
namespace OCC.Combat.Presentation
{
    public sealed class MainMenuDeskScene : MonoBehaviour
    {
        public const string ResourceRoot = "Art/MainMenuE/", PrefabPath = "UI/MainMenuEDesk";
        public const float OpenDuration = .55f;
        public static bool HasCompleteSceneArt => Resources.Load<GameObject>(PrefabPath) != null;
        [SerializeField] private RawImage sceneImage;
        [SerializeField] private CanvasGroup menuGroup;
        [SerializeField] private RectTransform sheet;
        [SerializeField] private Image documentTransition;
        [SerializeField] private Button startButton, continueButton, archiveButton, settingsButton, quitButton;
        private Material sceneMaterial;
        private MainMenuPixelStage pixelStage;
        private MainMenuHd2DStage hdStage;
        internal MainMenuHd2DStage Stage => hdStage;
        private Func<float> intensity;
        private Action handoff;
        private float elapsed, clock;
        private bool opening, handedOff;
        private Canvas ownerCanvas;
        private GameObject letterbox;
        public bool IsOpening => opening;
        public float OpenProgress => opening ? Mathf.Clamp01(elapsed / OpenDuration) : 0f;
        public static MainMenuDeskScene Create(Transform parent, Func<float> motion)
        {
            GameObject template = Resources.Load<GameObject>(PrefabPath);
            GameObject go = template != null ? Instantiate(template, parent, false) : new GameObject("书桌过渡测试", typeof(RectTransform), typeof(MainMenuDeskScene));
            if (template == null) go.transform.SetParent(parent, false);
            FormalUiKit.Stretch((RectTransform)go.transform);
            var scene = go.GetComponent<MainMenuDeskScene>(); scene.intensity = motion;
            scene.ownerCanvas = parent.GetComponentInParent<Canvas>();
            if (scene.ownerCanvas != null)
            {
                scene.letterbox = new GameObject("主界面整数网格留边", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                scene.letterbox.transform.SetParent(scene.ownerCanvas.transform, false);
                scene.letterbox.transform.SetAsFirstSibling(); FormalUiKit.Stretch((RectTransform)scene.letterbox.transform);
                scene.letterbox.GetComponent<Image>().color = new Color(.07f, .065f, .055f); scene.letterbox.GetComponent<Image>().raycastTarget = false;
            }
            if (scene.sceneImage != null && scene.sceneImage.material != null)
            { scene.sceneMaterial = new Material(scene.sceneImage.material); scene.sceneImage.material = scene.sceneMaterial; }
            var stagePrefab=Resources.Load<MainMenuPixelStage>("UI/MainMenuEPixelStage");
            var hdPrefab=Resources.Load<MainMenuHd2DStage>("UI/MainMenuEHD2DStage");
            if(Application.isPlaying && hdPrefab!=null && scene.sceneImage!=null)
            {
                scene.hdStage=Instantiate(hdPrefab,new Vector3(500,500,500),Quaternion.identity);
                scene.hdStage.Initialize();scene.hdStage.BindDisplay(scene.sceneImage);scene.sceneImage.texture=scene.hdStage.Output;
                scene.sceneImage.material=null;
                if(scene.sceneMaterial!=null)Destroy(scene.sceneMaterial);
                scene.sceneMaterial=null;
                foreach(var title in scene.GetComponentsInChildren<Text>())
                    if(title.name=="游戏标题")
                    {
                        title.color=ArchiveUiStyle.Ink;title.fontSize=72;
                        title.rectTransform.anchoredPosition=new Vector2(112,-100);
                        var shadow=title.GetComponent<Shadow>()??title.gameObject.AddComponent<Shadow>();
                        shadow.effectColor=new Color(.1f,.08f,.05f,.8f);
                    }
            }
            else if(Application.isPlaying && stagePrefab!=null && scene.sceneImage!=null)
            {
                scene.pixelStage=Instantiate(stagePrefab,new Vector3(10000,10000,0),Quaternion.identity);
                scene.pixelStage.Initialize();scene.sceneImage.texture=scene.pixelStage.Output;
                scene.sceneImage.material=null;
                var presentation=Resources.Load<Material>("Art/MainMenuE/Stage/presentation");
                if(presentation!=null)
                {
                    if(scene.sceneMaterial!=null)Destroy(scene.sceneMaterial);
                    scene.sceneMaterial=new Material(presentation);
                    scene.sceneImage.material=scene.sceneMaterial;
                }
            }
            return scene;
        }
        public Button[] Configure(bool resume, Action start, Action keepGoing, Action archive, Action settings, Action quit)
        {
            continueButton.gameObject.SetActive(resume);
            sheet.anchoredPosition = new Vector2(112, -204);
            sheet.sizeDelta = new Vector2(560, resume ? 700 : 596);
            AetherPaperChrome.ApplyOverlay(sheet.GetComponent<Image>(),false);
            foreach(var title in GetComponentsInChildren<Text>(true))if(title.name=="游戏标题"){title.transform.SetAsLastSibling();title.rectTransform.anchoredPosition=new Vector2(144,-232);title.rectTransform.sizeDelta=new Vector2(496,112);title.alignment=TextAnchor.MiddleLeft;}
            Button[] active = resume ? new[] { continueButton, startButton, archiveButton, settingsButton, quitButton } : new[] { startButton, archiveButton, settingsButton, quitButton };
            for (int i = 0; i < active.Length; i++)
            {
                var rect=(RectTransform)active[i].transform;
                rect.anchoredPosition = new Vector2(32, -152 - i * 100);
                rect.sizeDelta = new Vector2(496,96);
                AetherPaperChrome.ApplyOverlay(active[i].GetComponent<Image>(),true);
                if(i>0)AetherPaperChrome.Quiet(active[i]);
                foreach(var label in active[i].GetComponentsInChildren<Text>(true)){label.fontSize=24;label.fontStyle=FontStyle.Normal;label.resizeTextForBestFit=false;label.color=ArchiveUiStyle.Ink;label.alignment=TextAnchor.MiddleCenter;label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;label.rectTransform.offsetMin=new Vector2(36,20);label.rectTransform.offsetMax=new Vector2(-36,-20);}
                if(i==0){active[i].GetComponent<Image>().color=Color.Lerp(ArchiveUiStyle.Paper,FormalUiTheme.Cyan,.15f);FormalUiKit.FlatPanel("主入口标记",active[i].transform,new Vector2(0,1),new Vector2(0,1),new Vector2(24,-20),new Vector2(4,56),FormalUiTheme.Cyan);}
            }
            Bind(startButton, () => BeginOpen(start)); Bind(continueButton, () => BeginOpen(keepGoing));
            Bind(archiveButton, archive); Bind(settingsButton, settings); Bind(quitButton, quit);
            return active;
        }
        private static void Bind(Button button, Action action) { button.onClick.RemoveAllListeners(); button.onClick.AddListener(() => action?.Invoke()); }
        public void BindMenu(CanvasGroup group) => menuGroup = group;
        public bool BeginOpen(Action destination)
        {
            if (!isActiveAndEnabled || opening || handedOff || destination == null) return false;
            handoff = destination; opening = true; elapsed = 0;
            if (menuGroup != null) { menuGroup.interactable = false; menuGroup.blocksRaycasts = true; }
            if (intensity == null || intensity() <= 0) FinishOpen();
            return true;
        }
        private void Update() => Advance(Time.unscaledDeltaTime);
        private void LateUpdate()
        {
            if (ownerCanvas == null) return;
            if(hdStage!=null)
            {
                var hdScaler=ownerCanvas.GetComponent<CanvasScaler>();
                if(hdScaler!=null){hdScaler.uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize;hdScaler.scaleFactor=Mathf.Min(Screen.width/1920f,Screen.height/1080f);}
                var hdAdapter=ownerCanvas.GetComponent<ReferenceResolutionCanvas>();
                if(hdAdapter!=null)hdAdapter.Content.anchoredPosition=Vector2.zero;
                if(letterbox!=null)letterbox.SetActive(false);
                return;
            }
            if (letterbox != null && !letterbox.activeSelf) letterbox.SetActive(true);
            // Independently decoded 64px objects are assembled intact on this native scene.
            float tier = Mathf.Max(1, Mathf.Floor(Mathf.Min(Screen.width / 320f, Screen.height / 180f)));
            CanvasScaler scaler = ownerCanvas.GetComponent<CanvasScaler>();
            if (scaler != null) { scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize; scaler.scaleFactor = tier / 6f; }
            ReferenceResolutionCanvas adapter = ownerCanvas.GetComponent<ReferenceResolutionCanvas>();
            if (adapter != null) adapter.Content.anchoredPosition = new Vector2(
                (Mathf.Floor((Screen.width - 320 * tier) / 2f) + 160 * tier - Screen.width / 2f) * 6 / tier,
                (Mathf.Floor((Screen.height - 180 * tier) / 2f) + 90 * tier - Screen.height / 2f) * 6 / tier);
        }
        public void Advance(float seconds)
        {
            if (!isActiveAndEnabled) { CancelOpen(); return; }
            float motion = intensity == null ? 0 : Mathf.Clamp01(intensity()); clock += Mathf.Max(0, seconds);
            if (sceneMaterial != null) { sceneMaterial.SetFloat("_Clock", clock); sceneMaterial.SetFloat("_Motion", motion); }
            if(pixelStage!=null)pixelStage.SetState(0,1,0,clock,motion);
            if(hdStage!=null)hdStage.SetState(0,0,clock,motion);
            if (!opening) return;
            if (motion <= 0) { FinishOpen(); return; }
            elapsed += Mathf.Max(0, seconds); float p = Mathf.Clamp01(elapsed / OpenDuration);
            if (sceneMaterial != null)
            {
                sceneMaterial.SetFloat("_Open", Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.20f, .76f, p)));
                sceneMaterial.SetFloat("_Zoom", 1);
                sceneMaterial.SetFloat("_Focus", Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.04f, .72f, p)));
            }
            if(pixelStage!=null)pixelStage.SetState(
                Mathf.SmoothStep(0,1,Mathf.InverseLerp(.20f,.76f,p)),1,
                Mathf.SmoothStep(0,1,Mathf.InverseLerp(.04f,.72f,p)),clock,motion);
            if(hdStage!=null)hdStage.SetState(Mathf.SmoothStep(0,1,Mathf.InverseLerp(.20f,.76f,p)),Mathf.SmoothStep(0,1,Mathf.InverseLerp(.04f,.72f,p)),clock,motion);
            if(documentTransition!=null)
            {
                float unfold=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.78f,1,p));
                documentTransition.color=new Color(ArchiveUiStyle.LightPaper.r,ArchiveUiStyle.LightPaper.g,ArchiveUiStyle.LightPaper.b,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.78f,.94f,p)));
                documentTransition.rectTransform.sizeDelta=new Vector2(Mathf.Round(Mathf.Lerp(714,1696,unfold)/6)*6,Mathf.Round(Mathf.Lerp(216,780,unfold)/6)*6);
            }
            if (menuGroup != null) menuGroup.alpha = 1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01(p / .24f));
            if (p >= 1) FinishOpen();
        }
        private void FinishOpen() { if (!opening) return; opening = false; handedOff = true; Action callback = handoff; handoff = null; callback?.Invoke(); }
        private void OnEnable(){if(pixelStage!=null)pixelStage.gameObject.SetActive(true);if(hdStage!=null)hdStage.gameObject.SetActive(true);}
        private void OnDisable(){CancelOpen();if(pixelStage!=null)pixelStage.gameObject.SetActive(false);if(hdStage!=null)hdStage.gameObject.SetActive(false);}
        private void CancelOpen()
        {
            opening = false; handoff = null; elapsed = 0; handedOff = false;
            if (sceneMaterial != null) { sceneMaterial.SetFloat("_Open", 0); sceneMaterial.SetFloat("_Zoom", 1); sceneMaterial.SetFloat("_Focus", 0); }
            if (menuGroup != null) { menuGroup.alpha = 1; menuGroup.interactable = true; }
            if(documentTransition!=null)documentTransition.color=Color.clear;
            if (ownerCanvas != null) ownerCanvas.GetComponent<ReferenceResolutionCanvas>()?.Apply();
            if (letterbox != null) letterbox.SetActive(false);
        }
        private void OnDestroy() { if(hdStage!=null)Destroy(hdStage.gameObject);if(pixelStage!=null)Destroy(pixelStage.gameObject);if (sceneMaterial != null) Destroy(sceneMaterial); if (letterbox != null) Destroy(letterbox); }
#if UNITY_EDITOR
        // Author a saved prefab once. Runtime binds data and never rebuilds this hierarchy.
        public static MainMenuDeskScene CreateTemplate(Texture2D closed, Material material)
        {
            var root = new GameObject("E窗边书桌主界面", typeof(RectTransform), typeof(MainMenuDeskScene));
            var scene = root.GetComponent<MainMenuDeskScene>();
            var plate = new GameObject("统一像素场景", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            plate.transform.SetParent(root.transform, false); FormalUiKit.Stretch((RectTransform)plate.transform);
            scene.sceneImage = plate.GetComponent<RawImage>(); scene.sceneImage.texture = closed; scene.sceneImage.material = material; scene.sceneImage.raycastTarget = false;
            var transition=new GameObject("档案转场纸页",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
            transition.transform.SetParent(root.transform,false);
            var transitionRect=transition.GetComponent<RectTransform>();transitionRect.anchorMin=transitionRect.anchorMax=transitionRect.pivot=new Vector2(.5f,.5f);
            transitionRect.anchoredPosition=new Vector2(0,-20);transitionRect.sizeDelta=new Vector2(714,216);
            scene.documentTransition=transition.GetComponent<Image>();scene.documentTransition.color=Color.clear;scene.documentTransition.raycastTarget=false;
            var menu = new GameObject("主菜单", typeof(RectTransform), typeof(CanvasGroup)); menu.transform.SetParent(root.transform, false); FormalUiKit.Stretch((RectTransform)menu.transform);
            scene.menuGroup = menu.GetComponent<CanvasGroup>();
            var title = FormalUiKit.Label("游戏标题", "以太", menu.transform, Vector2.zero, new Vector2(440, 160), 96, ArchiveUiStyle.Ink, TextAnchor.MiddleCenter);
            Position(title.rectTransform, new Vector2(196, -100), new Vector2(440, 160));
            GameObject paper = FormalUiKit.AnchoredPanel("菜单纸页", menu.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(112, -464), new Vector2(424, 424), ArchiveUiStyle.LightPaper);
            ArchiveUiStyle.PaperPanel(paper, ArchiveUiStyle.LightPaper, false); scene.sheet = (RectTransform)paper.transform;
            scene.continueButton = MenuButton(paper.transform, "继续游戏", true); scene.startButton = MenuButton(paper.transform, "开始游戏", true);
            scene.archiveButton = MenuButton(paper.transform, "行程与行囊", false); scene.settingsButton = MenuButton(paper.transform, "设置", false); scene.quitButton = MenuButton(paper.transform, "退出游戏", false);
            return scene;
        }
        private static Button MenuButton(Transform parent, string label, bool primary)
        {
            var go = new GameObject("按钮_" + label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button)); go.transform.SetParent(parent, false);
            Position((RectTransform)go.transform, Vector2.zero, new Vector2(368, 80)); var image = go.GetComponent<Image>(); image.color = primary ? new Color(.88f, .8f, .62f, .32f) : Color.clear;
            var button = go.GetComponent<Button>(); button.targetGraphic = image; var colors = button.colors;
            colors.normalColor = Color.white; colors.highlightedColor = new Color(.7f, .93f, .94f); colors.pressedColor = new Color(.45f, .78f, .81f); button.colors = colors;
            Text text = FormalUiKit.Label("文字", label, go.transform, Vector2.zero, new Vector2(416, 80), 36, ArchiveUiStyle.Ink, TextAnchor.MiddleCenter); FormalUiKit.Stretch(text.rectTransform); text.raycastTarget = false;
            if (primary) ArchiveUiStyle.PaperPanel(go, image.color, false); return button;
        }
        private static void Position(RectTransform rect, Vector2 at, Vector2 size) { rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1); rect.anchoredPosition = at; rect.sizeDelta = size; }
#endif
    }
}
