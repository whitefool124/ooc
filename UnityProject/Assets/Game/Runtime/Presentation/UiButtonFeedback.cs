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
        private UiSelectionBookmark bookmark;
        private PeripheralPixelVfx focusTrace;
        private bool focused;
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
        private bool sceneSpace;

        internal void UseQuietNavigation()
        {
            // Prefab instances do not retain Configure's nonserialized bindings.
            // Quiet navigation still needs an actual pointer/keyboard state owner.
            if(button==null)button=GetComponent<Button>();
            if(image==null)image=button.targetGraphic as Image;
            if(image==null)return;
            if(motionProfile==null)motionProfile=()=>UiMotionProfile.FromIntensity(0);
            focusFrame=transform.Find("像素焦点框")?.GetComponent<Image>()??FormalUiKit.FocusFrame(transform);
            var focusSkin=FormalUiKit.SkinOverlay(focusFrame);if(focusSkin!=null)focusSkin.gameObject.SetActive(false);
            focusFrame.sprite=null;focusFrame.type=Image.Type.Simple;focusFrame.color=FormalUiTheme.Cyan;
            focusFrame.raycastTarget=false;
            focusFrame.rectTransform.anchorMin=Vector2.zero;focusFrame.rectTransform.anchorMax=new Vector2(1,0);
            focusFrame.rectTransform.offsetMin=new Vector2(20,4);focusFrame.rectTransform.offsetMax=new Vector2(-20,6);
            focusFrame.gameObject.SetActive(focused);
            skinOverlay=FormalUiKit.SkinOverlay(image);
            if(skinOverlay!=null)skinOverlay.gameObject.SetActive(false);
            button.transition=Selectable.Transition.None;usesFormalButtonSkin=false;sceneSpace=true;
            basePosition=((RectTransform)transform).anchoredPosition;
            normal=Color.clear;hover=new Color(.15f,.55f,.6f,.14f);
            pressed=new Color(.15f,.55f,.6f,.24f);selected=hover;disabled=Color.clear;
            ApplyImmediate();
        }

        internal void UseSceneSpaceFeedback()
        {
            var rect=transform as RectTransform;
            rect?.DOKill();
            if(rect!=null)basePosition=rect.anchoredPosition;
            sceneSpace=true;usesFormalButtonSkin=false;
            if(skinOverlay!=null)skinOverlay.color=Color.clear;
            if(image!=null)
            {
                normal=image.color;hover=Color.Lerp(normal,Color.white,.12f);
                pressed=new Color(normal.r*.85f,normal.g*.85f,normal.b*.85f,normal.a);
                selected=hover;disabled=new Color(normal.r*.86f,normal.g*.86f,normal.b*.86f,normal.a);
            }
        }

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
            if (!interactable) CancelFocusTrace();
            Apply(false);
        }

        public void SetSelectedState(bool value)
        {
            selectedState = value;
            if (value && bookmark == null)
            {
                var mark = new GameObject("选中像素书签", typeof(RectTransform), typeof(CanvasRenderer));
                mark.transform.SetParent(transform, false);
                var rect = (RectTransform)mark.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0f, .5f);
                rect.sizeDelta = new Vector2(6f, 12f);
                rect.anchoredPosition = new Vector2(6f, 0f);
                bookmark = mark.AddComponent<UiSelectionBookmark>();
                bookmark.color = FormalUiTheme.Cyan;
                bookmark.raycastTarget = false;
            }
            if (bookmark != null) bookmark.gameObject.SetActive(value);
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

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!hovering) PlayFocusTrace();
            hovering = true;
            Apply(false);
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            hovering = false; pressing = false;
            if (!focused) CancelFocusTrace();
            Apply(false);
        }
        public void OnPointerDown(PointerEventData eventData)
        {
            if (!IsPrimaryPointer(eventData)) return;
            if (button != null && button.interactable && GetComponent<RogueLoadoutDragHandler>()==null) OccSoundEffects.UiButton(SoundLabel());
            pressing = true;
            Apply(false);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!IsPrimaryPointer(eventData)) return;
            pressing = false;
            Apply(false);
        }
        public void OnSelect(BaseEventData eventData)
        {
            if (!focused) PlayFocusTrace();
            focused = true;
            if (focusFrame != null) focusFrame.gameObject.SetActive(true);
            Apply(false);
        }
        public void OnDeselect(BaseEventData eventData)
        {
            focused = false;
            if (focusFrame != null) focusFrame.gameObject.SetActive(false);
            pressing = false;
            if (!hovering) CancelFocusTrace();
            Apply(false);
        }

        private void PlayFocusTrace()
        {
            if (!isActiveAndEnabled || button == null || !button.IsInteractable() || focusTrace != null) return;
            UiMotionProfile profile = motionProfile == null ? UiMotionProfile.FromIntensity(1f) : motionProfile();
            var rect = transform as RectTransform;
            if (rect != null) focusTrace = PeripheralPixelVfx.Spawn(transform, "focus", profile.Intensity,
                rect.rect.size, Vector2.zero, new Vector2(.5f, .5f));
        }

        private void CancelFocusTrace()
        {
            if (focusTrace == null) return;
            GameObject trace = focusTrace.gameObject;
            focusTrace = null;
            trace.SetActive(false);
            if (!Application.isPlaying) DestroyImmediate(trace);
        }

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
                OccSoundEffects.UiButton(SoundLabel());
                PlaySubmitPulse();
                PlayAcceptedFeedback();
            }
        }

        private string SoundLabel()
        {
            Text label = button == null ? null : button.GetComponentInChildren<Text>(true);
            return gameObject.name + " " + (label == null ? string.Empty : label.text);
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
            OccSoundEffects.Rejected();
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
            float pixelOffset = Mathf.Round(FormalUiTheme.PressedOffset * profile.Intensity / 2f) * 2f;
            Vector2 position = basePosition + new Vector2(visiblyHovering && !visiblyPressing ? pixelOffset : 0f, visiblyPressing ? -pixelOffset : 0f);
            image.DOKill();
            rect?.DOKill();
            if (immediate || profile.IsImmediate)
            {
                image.color = target;
                if (rect != null && !sceneSpace) rect.anchoredPosition = position;
                return;
            }
            DOTween.To(() => image != null ? image.color : target,
                    value => { if (image != null) image.color = value; }, target, profile.QuickDuration)
                .SetTarget(image).SetEase(FormalUiMotionTokens.FeedbackEase).SetUpdate(true);
            if (rect != null && !sceneSpace)
                DOTween.To(() => rect != null ? rect.anchoredPosition : position,
                        value => { if (rect != null) rect.anchoredPosition = basePosition + PixelPresentationMotion.Snap(value - basePosition); }, position, profile.QuickDuration)
                    .SetTarget(rect).SetEase(FormalUiMotionTokens.FeedbackEase).SetUpdate(true);
        }

        private void ApplyImmediate() => Apply(true);

        private void OnDisable()
        {
            DOTween.Kill(this);
            CancelFocusTrace();
            hovering = false; pressing = false; focused = false;
            if (focusFrame != null) focusFrame.gameObject.SetActive(false);
            ApplyImmediate();
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
            if (image != null) image.DOKill();
            RectTransform rect = transform as RectTransform;
            if (rect != null) rect.DOKill();
        }
    }
}
