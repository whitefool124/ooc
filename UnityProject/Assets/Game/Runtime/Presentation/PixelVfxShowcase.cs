using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    /// <summary>Standalone presentation lab. Never writes combat state or player preferences.</summary>
    public sealed class PixelVfxShowcase : MonoBehaviour, IPointerDownHandler
    {
        public Camera sourceCamera;
        public RawImage display;
        public RectTransform integerFrame;
        public Shader effectShader;
        public Texture2D heroTexture, enemyTexture;
        public Button[] modeButtons;
        public Button replayButton, pauseButton, slowButton, originalButton;
        public Text statusLabel, pauseLabel, slowLabel, originalLabel;
        public Slider lightStrength;
        public int Mode { get; private set; } = 1;
        public float Age { get; private set; } = -1f;
        public bool Paused { get; private set; }
        public bool Original { get; private set; }
        public RenderTexture Output => output;
        public static readonly string[] Names = { "火矢轨迹", "火焰爆破", "以太消解", "空间冲击", "热浪折射", "移动光源", "地面电弧" };
        private RenderTexture source, output;
        private Material effect;
        private RenderPipelineAsset previousQualityPipeline;
        private UniversalRenderPipelineAsset localPipeline;
        private bool previousRunInBackground;
        private bool slow;
        private float clock;
        private Vector2 hit = new Vector2(156, 62);

        private void Start()
        {
            if (sourceCamera == null || display == null || effectShader == null || !effectShader.isSupported)
            { Debug.LogError("Pixel VFX showcase is missing a camera, display or supported shader.", this); enabled = false; return; }
            previousRunInBackground = Application.runInBackground;
            Application.runInBackground = true;
            // The regular game uses a half-resolution pipeline. Keep this pixel lab native,
            // without editing the shared asset or leaving an override after scene unload.
            previousQualityPipeline = QualitySettings.renderPipeline;
            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset activePipeline)
            {
                localPipeline = Instantiate(activePipeline);
                localPipeline.name = "Pixel VFX native resolution (runtime)";
                localPipeline.renderScale = 1f;
                QualitySettings.renderPipeline = localPipeline;
            }
            source = MakeTarget("Pixel VFX source", 24); output = MakeTarget("Pixel VFX output", 0);
            sourceCamera.targetTexture = source;
            sourceCamera.aspect = 240f / 180f;
            effect = new Material(effectShader) { name = "Pixel VFX runtime material" };
            effect.SetTexture("_UnitTex", heroTexture); effect.SetTexture("_EnemyTex", enemyTexture);
            display.texture = output;
            for (int i = 0; i < modeButtons.Length; i++)
            { int index = i; modeButtons[i].onClick.AddListener(() => SelectMode(index)); }
            replayButton.onClick.AddListener(Replay);
            pauseButton.onClick.AddListener(TogglePause);
            slowButton.onClick.AddListener(ToggleSlow);
            originalButton.onClick.AddListener(ToggleOriginal);
            RenderPipelineManager.endCameraRendering += AfterCamera;
            SelectMode(1);
        }

        private static RenderTexture MakeTarget(string label, int depth)
        {
            var rt = new RenderTexture(240, 180, depth, RenderTextureFormat.ARGB32) {
                name = label, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp,
                antiAliasing = 1, useMipMap = false, autoGenerateMips = false };
            rt.Create(); return rt;
        }

        private void Update()
        {
            int scale = Mathf.Max(1, Mathf.Min(Screen.width / 320, Screen.height / 180));
            integerFrame.localScale = Vector3.one * (scale / 6f);
            if (!Paused)
            {
                // A long first frame/domain reload must not skip the whole demonstration.
                float dt = Mathf.Min(Time.unscaledDeltaTime, .05f) / (slow ? 3f : 1f);
                clock += dt;
                if (Age >= 0) Age = Mathf.Min(Age + dt, 2.4f);
            }
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.spaceKey.wasPressedThisFrame) Replay();
            if (keyboard.pKey.wasPressedThisFrame) TogglePause();
            if (keyboard.oKey.wasPressedThisFrame) ToggleOriginal();
        }

        private void AfterCamera(ScriptableRenderContext context, Camera camera)
        {
            if (camera != sourceCamera || effect == null) return;
            effect.SetFloat("_Mode", Mode); effect.SetFloat("_Age", Age);
            effect.SetFloat("_Clock", clock); effect.SetFloat("_Original", Original ? 1 : 0);
            effect.SetFloat("_Strength", lightStrength.value);
            effect.SetVector("_Hit", hit);
            Graphics.Blit(source, output, effect);
        }

        public void SelectMode(int mode)
        {
            Mode = Mathf.Clamp(mode, 0, 6); Replay();
            for (int i = 0; i < modeButtons.Length; i++)
                modeButtons[i].GetComponent<Image>().color = i == Mode ? new Color(.3f,.67f,.68f) : new Color(.84f,.8f,.72f);
            statusLabel.text = Names[Mode] + "\n点击场地改变落点\n空格重播   P 暂停   O 对比";
        }
        public void Replay() { Age = 0; Paused = false; UpdateLabels(); }
        public void TogglePause() { Paused = !Paused; UpdateLabels(); }
        public void ToggleSlow() { slow = !slow; UpdateLabels(); }
        public void ToggleOriginal() { Original = !Original; UpdateLabels(); }
        private void UpdateLabels()
        {
            pauseLabel.text = Paused ? "继续播放" : "暂停播放";
            slowLabel.text = slow ? "正常速度" : "三倍慢放";
            originalLabel.text = Original ? "恢复光效" : "对比原画";
        }
        public void OnPointerDown(PointerEventData eventData)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(display.rectTransform, eventData.position, eventData.pressEventCamera, out var local)) return;
            Rect rect = display.rectTransform.rect;
            Vector2 p = new Vector2((local.x - rect.xMin) / rect.width * 240, (local.y - rect.yMin) / rect.height * 180);
            hit = new Vector2(Mathf.Clamp(Mathf.Floor(p.x), 12, 228), Mathf.Clamp(Mathf.Floor(p.y), 12, 158)); Replay();
        }
        private void OnDestroy()
        {
            Application.runInBackground = previousRunInBackground;
            RenderPipelineManager.endCameraRendering -= AfterCamera;
            if (sourceCamera != null) sourceCamera.targetTexture = null;
            if (display != null) display.texture = null;
            if (source != null) { source.Release(); Destroy(source); }
            if (output != null) { output.Release(); Destroy(output); }
            if (effect != null) Destroy(effect);
            if (localPipeline != null)
            {
                if (QualitySettings.renderPipeline == localPipeline) QualitySettings.renderPipeline = previousQualityPipeline;
                Destroy(localPipeline);
            }
        }
    }
}
