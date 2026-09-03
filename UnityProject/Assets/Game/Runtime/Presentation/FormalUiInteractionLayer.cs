using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    public sealed class FormalUiInteractionLayer : MonoBehaviour
    {
        private IInteractionPresentationHost bootstrap;
        private Canvas canvas;
        private GameObject modalRoot;
        private GameObject toastRoot;
        private string toastLayoutId;
        private Action confirmAction;
        private GameObject previousFocus;
        private bool submitting;

        public bool IsConfirmationOpen => modalRoot != null;

        public void Initialize(IInteractionPresentationHost source)
        {
            bootstrap = source;
            bootstrap.UiVisualEvents.Published += OnVisualEvent;
            EnsureCanvas();
        }

        private void OnVisualEvent(UiVisualEvent visualEvent)
        {
            switch (visualEvent.Kind)
            {
                case UiVisualEventKind.ResourceChanged:
                    ShowFeedback(new UiActionFeedback(visualEvent.Delta > 0 ? UiFeedbackKind.Success : UiFeedbackKind.Information,
                        visualEvent.Subject + " " + (visualEvent.Delta > 0 ? "+" : string.Empty) + visualEvent.Delta));
                    break;
                case UiVisualEventKind.SafeRevisit:
                    ShowFeedback(new UiActionFeedback(UiFeedbackKind.Information, "这里已经处理妥当。再来看看不会再次战斗，也没有新的奖励。"));
                    break;
                case UiVisualEventKind.CombatCommandRejected:
                    ShowFeedback(new UiActionFeedback(UiFeedbackKind.Rejected,
                        string.IsNullOrWhiteSpace(visualEvent.Message) ? "现在不能执行这个行动。请选择其他行动或目标。" : visualEvent.Message));
                    break;
                case UiVisualEventKind.CombatCommandSubmitted:
                    ShowFeedback(new UiActionFeedback(UiFeedbackKind.Information, visualEvent.Subject + "完成"));
                    break;
                case UiVisualEventKind.RewardClaimed:
                    ShowFeedback(new UiActionFeedback(UiFeedbackKind.Success, "已经收好。道具放进了行囊，术式收进了术式册。"));
                    break;
            }
        }

        private void Update()
        {
            if (modalRoot != null && RuntimeUiEventSystem.CancelPressedThisFrame()) CancelConfirmation();
            if (toastRoot != null)
            {
                string desiredLayout = CurrentToastLayout();
                if (desiredLayout != toastLayoutId)
                {
                    KillToastTweens(toastRoot);
                    Destroy(toastRoot);
                    toastRoot = null;
                    toastLayoutId = null;
                }
            }
        }

        public void RequestConfirmation(UiConfirmationRequest request, Action onConfirm)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (onConfirm == null) throw new ArgumentNullException(nameof(onConfirm));
            if (modalRoot != null) return;
            EnsureCanvas();
            bootstrap?.PublishUiVisual(new UiVisualEvent(UiVisualEventKind.ConfirmationOpened, request.Kind.ToString()));
            previousFocus = EventSystem.current == null ? null : EventSystem.current.currentSelectedGameObject;
            confirmAction = onConfirm;
            submitting = false;

            modalRoot = Panel("正式确认层", canvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, FormalUiTheme.Overlay);
            RectTransform modalRect = modalRoot.GetComponent<RectTransform>();
            modalRect.offsetMin = modalRect.offsetMax = Vector2.zero;
            GameObject card = FormalUiKit.LayoutPanel("确认卡", modalRoot.transform, "modal.confirm", FormalUiTheme.WithAlpha(FormalUiTheme.SurfaceRaised, .99f));
            FormalUiKit.ApplySkin(card.GetComponent<Image>(), "danger", Color.white);
            Label(card.transform, "确认类型", ConfirmationKindLabel(request.Kind), new Vector2(42, -34), new Vector2(636, 24), FormalUiTheme.CaptionFontSize, FormalUiTheme.Amber, TextAnchor.MiddleLeft);
            Label(card.transform, "确认标题", request.Title, new Vector2(42, -70), new Vector2(636, 54), FormalUiTheme.TitleFontSize, FormalUiTheme.Text, TextAnchor.MiddleLeft);
            Label(card.transform, "确认说明", request.Message, new Vector2(42, -138), new Vector2(636, 82), 19, FormalUiTheme.Muted, TextAnchor.UpperLeft);
            Button cancel = Button(card.transform, "取消", new Vector2(42, -264), new Vector2(292, 64), request.CancelLabel, FormalUiTheme.Interactive);
            Button confirm = Button(card.transform, "确认", new Vector2(386, -264), new Vector2(292, 64), request.ConfirmLabel, Color.Lerp(FormalUiTheme.Interactive, FormalUiTheme.Danger, .18f));
            cancel.onClick.AddListener(CancelConfirmation);
            confirm.onClick.AddListener(Confirm);
            ConfigureButton(cancel, FormalUiTheme.ButtonPalette(FormalUiButtonTone.Neutral));
            ConfigureButton(confirm, FormalUiTheme.ButtonPalette(FormalUiButtonTone.Dangerous));
            AnimateModal(card, modalRoot.GetComponent<Image>());
            RuntimeUiEventSystem.Select(cancel.gameObject);
        }

        public void ShowFeedback(UiActionFeedback feedback)
        {
            if (feedback == null) return;
            EnsureCanvas();
            if (toastRoot != null)
            {
                KillToastTweens(toastRoot);
                Destroy(toastRoot);
            }
            Color accent = feedback.Kind == UiFeedbackKind.Rejected ? FormalUiTheme.Danger :
                feedback.Kind == UiFeedbackKind.Success || feedback.Kind == UiFeedbackKind.Saved ? FormalUiTheme.Safe : FormalUiTheme.Cyan;
            toastLayoutId = CurrentToastLayout();
            toastRoot = FormalUiKit.LayoutPanel("短时提示条", canvas.transform, toastLayoutId, FormalUiTheme.WithAlpha(FormalUiTheme.SurfaceRaised, .98f));
            GameObject shownToast = toastRoot;
            Image image = toastRoot.GetComponent<Image>();
            GameObject edge = Panel("提示边线", toastRoot.transform, new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, new Vector2(4, 0), accent);
            edge.GetComponent<RectTransform>().offsetMin = edge.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            float textWidth = toastRoot.GetComponent<RectTransform>().sizeDelta.x - 70f;
            Label(toastRoot.transform, "提示文字", feedback.Message, new Vector2(58, -6), new Vector2(textWidth, 46), FormalUiTheme.BodyFontSize, FormalUiTheme.Text, TextAnchor.MiddleLeft);
            UiMotionProfile motion = Motion();
            string feedbackId = feedback.Kind == UiFeedbackKind.Rejected ? "rejected" :
                feedback.Kind == UiFeedbackKind.Success || feedback.Kind == UiFeedbackKind.Saved ? "success" : "click";
            float iconX = -toastRoot.GetComponent<RectTransform>().sizeDelta.x * .5f + 36f;
            FormalUiEffects.SpawnLocalFeedback(toastRoot.transform, feedbackId, motion.Intensity, new Vector2(iconX, 0f));
            CanvasGroup group = toastRoot.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;
            foreach (Graphic graphic in toastRoot.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            float holdDuration = FeedbackHoldDuration(feedback.Message);
            if (motion.IsImmediate)
            {
                group.alpha = 1f;
                DOVirtual.DelayedCall(holdDuration, () =>
                {
                    if (toastRoot != shownToast) return;
                    Destroy(shownToast);
                    toastRoot = null;
                    toastLayoutId = null;
                }, true).SetTarget(shownToast);
                return;
            }
            RectTransform rect = toastRoot.GetComponent<RectTransform>();
            Vector2 end = rect.anchoredPosition;
            rect.anchoredPosition = end + new Vector2(0, motion.PageOffset);
            group.alpha = 0f;
            Sequence sequence = DOTween.Sequence().SetUpdate(true).SetTarget(shownToast);
            sequence.Join(DOTween.To(() => group.alpha, value => group.alpha = value, 1f, motion.QuickDuration));
            sequence.Join(DOTween.To(() => rect.anchoredPosition, value => rect.anchoredPosition = value, end, motion.StandardDuration).SetEase(FormalUiMotionTokens.StandardEase));
            sequence.AppendInterval(holdDuration);
            sequence.Append(DOTween.To(() => group.alpha, value => group.alpha = value, 0f, motion.ToastDuration));
            sequence.OnComplete(() =>
            {
                if (toastRoot != shownToast) return;
                Destroy(shownToast);
                toastRoot = null;
                toastLayoutId = null;
            });
        }

        private static float FeedbackHoldDuration(string message)
        {
            int visibleCharacters = string.IsNullOrWhiteSpace(message) ? 0 : message.Replace("\r", string.Empty).Replace("\n", string.Empty).Length;
            return Mathf.Clamp(1.35f + Mathf.Max(0, visibleCharacters - 18) * .035f, 1.35f, 3.25f);
        }

        private static void KillToastTweens(GameObject root)
        {
            if (root == null) return;
            DOTween.Kill(root);
            foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true)) graphic.DOKill();
            foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true)) rect.DOKill();
        }

        private string CurrentToastLayout()
        {
            if (bootstrap != null && bootstrap.IsDeveloperCombatActive) return "combat.toast";
            if (bootstrap != null && bootstrap.IsMapMenuOpen) return "map.toast";
            return "modal.toast";
        }

        public void CancelConfirmation()
        {
            if (modalRoot == null || submitting) return;
            GameObject focusToRestore = previousFocus;
            CloseModal();
            if (focusToRestore != null && focusToRestore.activeInHierarchy) RuntimeUiEventSystem.Select(focusToRestore);
            previousFocus = null;
        }

        private void Confirm()
        {
            if (modalRoot == null || submitting) return;
            submitting = true;
            Action action = confirmAction;
            CloseModal();
            previousFocus = null;
            action?.Invoke();
        }

        private void CloseModal()
        {
            if (modalRoot != null)
            {
                EventSystem eventSystem = EventSystem.current;
                if (eventSystem != null) eventSystem.SetSelectedGameObject(null);
                DOTween.Kill(modalRoot);
                foreach (Graphic graphic in modalRoot.GetComponentsInChildren<Graphic>(true)) graphic.DOKill();
                Destroy(modalRoot);
            }
            modalRoot = null;
            confirmAction = null;
            submitting = false;
        }

        private void AnimateModal(GameObject card, Image veil)
        {
            UiMotionProfile motion = Motion();
            RectTransform rect = card.GetComponent<RectTransform>();
            CanvasGroup group = card.AddComponent<CanvasGroup>();
            if (motion.IsImmediate)
            {
                veil.color = FormalUiTheme.WithAlpha(veil.color, .76f);
                group.alpha = 1f;
                rect.localScale = Vector3.one;
                return;
            }
            Color veilTarget = veil.color;
            veil.color = FormalUiTheme.WithAlpha(veilTarget, 0f);
            group.alpha = 0f;
            rect.localScale = Vector3.one * (1f - motion.ModalScaleOffset);
            DOTween.Sequence().SetUpdate(true).SetTarget(modalRoot)
                .Join(DOTween.To(() => veil.color, value => veil.color = value, veilTarget, motion.StandardDuration))
                .Join(DOTween.To(() => group.alpha, value => group.alpha = value, 1f, motion.StandardDuration))
                .Join(rect.DOScale(1f, motion.StandardDuration).SetEase(FormalUiMotionTokens.StandardEase));
        }

        private UiMotionProfile Motion() => UiMotionProfile.FromIntensity(bootstrap == null ? 1f : bootstrap.UiPreferences.AnimationIntensity);

        private static string ConfirmationKindLabel(UiConfirmationKind kind)
        {
            switch (kind)
            {
                case UiConfirmationKind.ReplaceExistingRun: return "旧旅程会被替换";
                case UiConfirmationKind.TacticalRestart: return "这场战斗会从头开始";
                case UiConfirmationKind.LeaveCombat: return "这场战斗不会留下收获";
                default: return "再确认一次";
            }
        }

        private void ConfigureButton(Button button, FormalUiButtonPalette palette)
        {
            FormalUiKit.ConfigureButtonFeedback(button, palette, Motion, ShowFeedback);
        }

        private void EnsureCanvas()
        {
            if (canvas != null) return;
            canvas = FormalUiKit.CanvasRoot("正式交互层", UiLayoutContract.InteractionSortingOrder);
        }

        private static GameObject Panel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Color color)
        {
            return FormalUiKit.Panel(name, parent, anchorMin, anchorMax, position, size, color);
        }

        private static Text Label(Transform parent, string name, string value, Vector2 position, Vector2 size, int fontSize, Color color, TextAnchor alignment)
        {
            return FormalUiKit.Label(name, value, parent, position, size, fontSize, color, alignment);
        }

        private static Button Button(Transform parent, string name, Vector2 position, Vector2 size, string title, Color color)
        {
            Button button = FormalUiKit.Button(name, title, parent, position, size, color, 18);
            button.GetComponentInChildren<Text>().color = FormalUiTheme.Text;
            return button;
        }

        private void OnDestroy()
        {
            if (bootstrap != null) bootstrap.UiVisualEvents.Published -= OnVisualEvent;
            CloseModal();
            if (toastRoot != null) KillToastTweens(toastRoot);
        }
    }
}
