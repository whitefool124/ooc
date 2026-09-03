using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    public sealed class UiButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, ISelectHandler, IDeselectHandler, ISubmitHandler
    {
        private Button button;
        private Image image;
        private Image skinOverlay;
        private Image focusFrame;
        private Func<UiMotionProfile> motionProfile;
        private Action<UiActionFeedback> feedback;
        private Color normal;
        private Color hover;
        private Color pressed;
        private Color selected;
        private Color disabled;
        private string disabledReason;
        private Vector2 basePosition;
        private bool hovering;
        private bool pressing;
        private bool selectedState;
        private bool usesFormalButtonSkin;

        public void Configure(Button target, Color normalColor, Color hoverColor, Color pressedColor, Color selectedColor, Color disabledColor,
            Func<UiMotionProfile> profile, Action<UiActionFeedback> feedbackSink, string reason = null)
        {
            button = target;
            image = target == null ? GetComponent<Image>() : target.targetGraphic as Image ?? target.GetComponent<Image>();
            skinOverlay = FormalUiKit.SkinOverlay(image);
            usesFormalButtonSkin = skinOverlay != null && FormalUiKit.IsStandardButtonSkin(skinOverlay.sprite);
            motionProfile = profile;
            feedback = feedbackSink;
            normal = normalColor;
            hover = hoverColor;
            pressed = pressedColor;
            selected = selectedColor;
            disabled = disabledColor;
            disabledReason = reason ?? string.Empty;
            RectTransform rect = transform as RectTransform;
            if (rect != null) basePosition = rect.anchoredPosition;
            // Feedback is presentation-only: preserve authored sprites and image types so
            // pixel borders, nine-slice skins and icon-shaped buttons are never discarded.
            focusFrame = transform.Find("像素焦点框")?.GetComponent<Image>() ?? FormalUiKit.FocusFrame(transform);
            focusFrame.gameObject.SetActive(false);
            if (button != null) button.transition = Selectable.Transition.None;
            ApplyImmediate();
        }

        public void SetAvailability(bool interactable, string reason)
        {
            if (button == null) return;
            button.interactable = interactable;
            disabledReason = reason ?? string.Empty;
            Apply(false);
        }

        public void SetSelectedState(bool value)
        {
            selectedState = value;
            Apply(false);
        }

        public void RefreshLayoutPosition()
        {
            RectTransform rect = transform as RectTransform;
            if (rect == null) return;
            rect.DOKill();
            basePosition = rect.anchoredPosition;
            ApplyImmediate();
        }

        public void OnPointerEnter(PointerEventData eventData) { hovering = true; Apply(false); }
        public void OnPointerExit(PointerEventData eventData) { hovering = false; pressing = false; Apply(false); }
        public void OnPointerDown(PointerEventData eventData)
        {
            if (!IsPrimaryPointer(eventData)) return;
            pressing = true;
            Apply(false);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!IsPrimaryPointer(eventData)) return;
            pressing = false;
            Apply(false);
        }
        public void OnSelect(BaseEventData eventData) { if (focusFrame != null) focusFrame.gameObject.SetActive(true); Apply(false); }
        public void OnDeselect(BaseEventData eventData) { if (focusFrame != null) focusFrame.gameObject.SetActive(false); pressing = false; Apply(false); }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!IsPrimaryPointer(eventData)) return;
            if (button != null && !button.interactable) RejectDisabled();
            else PlayAcceptedFeedback();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (button != null && !button.interactable) RejectDisabled();
            else
            {
                PlaySubmitPulse();
                PlayAcceptedFeedback();
            }
        }

        private static bool IsPrimaryPointer(PointerEventData eventData) => eventData != null && eventData.button == PointerEventData.InputButton.Left;

        private void PlaySubmitPulse()
        {
            UiMotionProfile profile = motionProfile == null ? UiMotionProfile.FromIntensity(1f) : motionProfile();
            if (profile.IsImmediate) return;
            DOTween.Kill(this);
            pressing = true;
            Apply(false);
            DOVirtual.DelayedCall(Mathf.Max(.06f, profile.QuickDuration), () =>
            {
                if (this == null) return;
                pressing = false;
                Apply(false);
            }, true).SetTarget(this);
        }

        private void PlayAcceptedFeedback()
        {
            UiMotionProfile profile = motionProfile == null ? UiMotionProfile.FromIntensity(1f) : motionProfile();
            FormalUiEffects.SpawnLocalFeedback(transform, "click", profile.Intensity);
        }

        private void RejectDisabled()
        {
            UiMotionProfile profile = motionProfile == null ? UiMotionProfile.FromIntensity(1f) : motionProfile();
            FormalUiEffects.SpawnLocalFeedback(transform, "rejected", profile.Intensity);
            feedback?.Invoke(new UiActionFeedback(UiFeedbackKind.Rejected,
                string.IsNullOrWhiteSpace(disabledReason) ? "当前操作不可执行" : disabledReason));
        }

        private void Apply(bool immediate)
        {
            if (image == null) return;
            UiMotionProfile profile = motionProfile == null ? UiMotionProfile.FromIntensity(1f) : motionProfile();
            bool available = button == null || button.interactable;
            bool visiblyPressing = pressing && available;
            bool visiblyHovering = hovering && available;
            Color source = !available ? disabled : visiblyPressing ? pressed : selectedState ? selected : visiblyHovering ? hover : normal;
            Color target = source;
            if (usesFormalButtonSkin)
            {
                skinOverlay.sprite = FormalUiKit.ButtonStateSprite(available, visiblyPressing, selectedState, visiblyHovering);
                skinOverlay.type = Image.Type.Sliced;
            }
            RectTransform rect = transform as RectTransform;
            float pixelOffset = FormalUiTheme.PressedOffset * profile.Intensity;
            Vector2 position = basePosition + new Vector2(visiblyHovering && !visiblyPressing ? pixelOffset : 0f, visiblyPressing ? -pixelOffset : 0f);
            image.DOKill();
            rect?.DOKill();
            if (immediate || profile.IsImmediate)
            {
                image.color = target;
                if (rect != null) rect.anchoredPosition = position;
                return;
            }
            DOTween.To(() => image.color, value => image.color = value, target, profile.QuickDuration).SetTarget(image).SetEase(FormalUiMotionTokens.FeedbackEase).SetUpdate(true);
            if (rect != null) DOTween.To(() => rect.anchoredPosition, value => rect.anchoredPosition = value, position, profile.QuickDuration).SetTarget(rect).SetEase(FormalUiMotionTokens.FeedbackEase).SetUpdate(true);
        }

        private void ApplyImmediate() => Apply(true);

        private void OnDestroy()
        {
            DOTween.Kill(this);
            image?.DOKill();
            (transform as RectTransform)?.DOKill();
        }
    }
}
