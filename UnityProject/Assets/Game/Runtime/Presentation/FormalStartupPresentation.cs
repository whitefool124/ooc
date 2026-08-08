using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    public sealed class FormalStartupPresentation : MonoBehaviour
    {
        private CombatPrototypeBootstrap bootstrap;
        private Canvas canvas;
        private GameObject root;
        private float shownAt;
        private bool dismissing;
        private bool inputArmed;

        public bool IsVisible => root != null;

        public void Initialize(CombatPrototypeBootstrap source)
        {
            bootstrap = source;
            Build();
        }

        private void Build()
        {
            canvas = FormalUiKit.CanvasRoot("正式启动界面", UiLayoutContract.InteractionSortingOrder + 20);
            root = canvas.gameObject;
            CanvasGroup startupGroup = root.AddComponent<CanvasGroup>(); startupGroup.alpha = 1f;
            Image background = root.AddComponent<Image>();
            background.sprite = Resources.Load<Sprite>(FormalUiEffectsConfig.Data.startupBackdrop);
            background.type = Image.Type.Simple; background.preserveAspect = false; background.raycastTarget = true;
            Button dismissButton = root.AddComponent<Button>(); dismissButton.targetGraphic = background; dismissButton.transition = Selectable.Transition.None; dismissButton.onClick.AddListener(Dismiss);

            FormalUiEffects.AddAmbientScanlines(root.transform, bootstrap.UiPreferences.AnimationIntensity);
            GameObject veil = FormalUiKit.Create("启动暗角", root.transform);
            RectTransform veilRect = veil.AddComponent<RectTransform>(); FormalUiKit.Stretch(veilRect);
            Image veilImage = veil.AddComponent<Image>(); veilImage.sprite = null; veilImage.color = new Color(.01f, .02f, .025f, .56f); veilImage.raycastTarget = false;

            GameObject title = FormalUiKit.AnchoredPanel("启动标题模块", root.transform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, 52f), new Vector2(1000f, 330f), new Color(.025f, .045f, .055f, .94f));
            FormalUiKit.Label("协议", "OCC // AETHER OPERATIONS CONTROL", title.transform, new Vector2(54f, -42f), new Vector2(892f, 34f), 18, FormalUiTheme.Cyan, TextAnchor.MiddleCenter);
            FormalUiKit.Label("标题", "前线行动网络", title.transform, new Vector2(54f, -94f), new Vector2(892f, 78f), 47, FormalUiTheme.Text, TextAnchor.MiddleCenter);
            FormalUiKit.Label("状态", "以太中继已同步  /  战术链路稳定  /  等待操作员接入", title.transform, new Vector2(54f, -190f), new Vector2(892f, 34f), 18, FormalUiTheme.Muted, TextAnchor.MiddleCenter);
            GameObject track = FormalUiKit.Panel("启动同步轨道", title.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(126f, -246f), new Vector2(748f, 8f), new Color(.05f, .10f, .12f, 1f));
            GameObject fill = FormalUiKit.Panel("启动同步进度", track.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, FormalUiTheme.Cyan);
            RectTransform fillRect = fill.GetComponent<RectTransform>(); fillRect.pivot = new Vector2(0f, .5f); fillRect.anchorMax = new Vector2(.18f, 1f); fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
            Text prompt = FormalUiKit.Label("接入提示", "按任意键或点击接入", title.transform, new Vector2(54f, -270f), new Vector2(892f, 40f), 22, FormalUiTheme.Amber, TextAnchor.MiddleCenter);

            UiMotionProfile motion = UiMotionProfile.FromIntensity(bootstrap.UiPreferences.AnimationIntensity);
            shownAt = Time.unscaledTime;
            if (motion.IsImmediate) { fillRect.anchorMax = Vector2.one; return; }
            CanvasGroup group = title.AddComponent<CanvasGroup>(); group.alpha = 0f;
            RectTransform titleRect = title.GetComponent<RectTransform>(); Vector2 end = titleRect.anchoredPosition; titleRect.anchoredPosition += new Vector2(0f, 24f);
            DOTween.Sequence().SetUpdate(true).SetTarget(root)
                .Join(DOTween.To(() => group.alpha, value => group.alpha = value, 1f, .28f * motion.Intensity))
                .Join(DOTween.To(() => titleRect.anchoredPosition, value => titleRect.anchoredPosition = value, end, .34f * motion.Intensity).SetEase(Ease.OutCubic));
            DOTween.To(() => fillRect.anchorMax.x, value => fillRect.anchorMax = new Vector2(value, 1f), 1f, 1.25f * motion.Intensity).SetEase(Ease.OutQuad).SetUpdate(true).SetTarget(root);
            DOTween.To(() => prompt.color.a, value => prompt.color = new Color(prompt.color.r, prompt.color.g, prompt.color.b, value), .35f, .65f * motion.Intensity)
                .SetLoops(-1, LoopType.Yoyo).SetEase(Ease.Linear).SetUpdate(true).SetTarget(root);
        }

        private void Update()
        {
            if (root == null || dismissing) return;
            if (Time.unscaledTime - shownAt < FormalUiEffectsConfig.Data.startupHoldSeconds) return;
            if (!inputArmed)
            {
                if (!RuntimeUiEventSystem.AnyInputIsHeld()) inputArmed = true;
                return;
            }
            if (RuntimeUiEventSystem.AnyInputPressedThisFrame()) Dismiss();
        }

        public void Dismiss()
        {
            if (root == null || dismissing || Time.unscaledTime - shownAt < FormalUiEffectsConfig.Data.startupHoldSeconds) return;
            dismissing = true;
            UiMotionProfile motion = UiMotionProfile.FromIntensity(bootstrap.UiPreferences.AnimationIntensity);
            if (motion.IsImmediate) { DestroyStartup(); return; }
            CanvasGroup group = root.GetComponent<CanvasGroup>() ?? root.AddComponent<CanvasGroup>();
            FormalUiEffects.PlayPageWipe(root.transform, motion.Intensity);
            DOTween.To(() => group.alpha, value => group.alpha = value, 0f, .22f * motion.Intensity).SetDelay(.12f * motion.Intensity).SetUpdate(true).SetTarget(root).OnComplete(DestroyStartup);
        }

        public void DismissImmediately()
        {
            if (root == null) return;
            dismissing = true;
            DestroyStartup();
        }

        private void DestroyStartup()
        {
            if (root != null) { DOTween.Kill(root); Destroy(root); }
            root = null; canvas = null;
        }

        private void OnDestroy()
        {
            if (root != null) DOTween.Kill(root);
        }
    }
}
