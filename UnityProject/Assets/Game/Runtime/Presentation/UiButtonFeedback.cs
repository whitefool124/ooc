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
        private Image focusFrame;
        private Sprite normalSprite, hoverSprite, pressedSprite, selectedSprite, disabledSprite;
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

        public void Configure(Button target, Color normalColor, Color hoverColor, Color pressedColor, Color selectedColor, Color disabledColor,
            Func<UiMotionProfile> profile, Action<UiActionFeedback> feedbackSink, string reason = null)
        {
            button = target;
            image = target == null ? GetComponent<Image>() : target.targetGraphic as Image ?? target.GetComponent<Image>();
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
            normalSprite = FormalUiKit.SkinSprite(gameObject.name.Contains("结束行动") ? "button_end_turn" : OccPixelUiConfig.StateSkin("button", "normal"));
            hoverSprite = FormalUiKit.SkinSprite(OccPixelUiConfig.StateSkin("button", "hover"));
            pressedSprite = FormalUiKit.SkinSprite(OccPixelUiConfig.StateSkin("button", "pressed"));
            selectedSprite = FormalUiKit.SkinSprite(OccPixelUiConfig.StateSkin("button", "selected"));
            disabledSprite = FormalUiKit.SkinSprite(OccPixelUiConfig.StateSkin("button", "disabled"));
            if (image != null) { image.type = Image.Type.Sliced; image.sprite = normalSprite; }
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

        public void OnPointerEnter(PointerEventData eventData) { hovering = true; Apply(false); }
        public void OnPointerExit(PointerEventData eventData) { hovering = false; pressing = false; Apply(false); }
        public void OnPointerDown(PointerEventData eventData) { pressing = true; Apply(false); }
        public void OnPointerUp(PointerEventData eventData) { pressing = false; Apply(false); }
        public void OnSelect(BaseEventData eventData) { if (focusFrame != null) focusFrame.gameObject.SetActive(true); Apply(false); }
        public void OnDeselect(BaseEventData eventData) { if (focusFrame != null) focusFrame.gameObject.SetActive(false); pressing = false; Apply(false); }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (button != null && !button.interactable) RejectDisabled();
            else PlayAcceptedFeedback();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (button != null && !button.interactable) RejectDisabled();
            else PlayAcceptedFeedback();
        }

        private void PlayAcceptedFeedback()
        {
            UiMotionProfile profile = motionProfile == null ? UiMotionProfile.FromIntensity(1f) : motionProfile();
            FormalUiEffects.SpawnLocalFeedback(transform, "click", profile.Intensity);
        }

        private void RejectDisabled()
        {
            feedback?.Invoke(new UiActionFeedback(UiFeedbackKind.Rejected,
                string.IsNullOrWhiteSpace(disabledReason) ? "当前操作不可执行" : disabledReason));
        }

        private void Apply(bool immediate)
        {
            if (image == null) return;
            UiMotionProfile profile = motionProfile == null ? UiMotionProfile.FromIntensity(1f) : motionProfile();
            Color source = button != null && !button.interactable ? disabled : pressing ? pressed : selectedState ? selected : hovering ? hover : normal;
            Color target = new Color(Mathf.Lerp(1f, source.r, .22f), Mathf.Lerp(1f, source.g, .22f), Mathf.Lerp(1f, source.b, .22f), source.a);
            image.sprite = button != null && !button.interactable ? disabledSprite : pressing ? pressedSprite : selectedState ? selectedSprite : hovering ? hoverSprite : normalSprite;
            RectTransform rect = transform as RectTransform;
            Vector2 position = basePosition + new Vector2(hovering && !pressing ? profile.PressOffset : 0f, pressing ? -profile.PressOffset : 0f);
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
            image?.DOKill();
            (transform as RectTransform)?.DOKill();
        }
    }
}
