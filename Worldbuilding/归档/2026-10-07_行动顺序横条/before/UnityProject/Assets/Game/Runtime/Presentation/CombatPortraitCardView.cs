using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    public sealed class CombatPortraitCardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        public const string ResourcePath = "UI/Prefabs/FormalCombatPortraitCard";
        [SerializeField] private Button button;
        [SerializeField] private RawImage portrait;
        [SerializeField] private Image frame, focus, activeMark, factionMark;
        [SerializeField] private Text caption;
        [SerializeField] private Text placeholder;
        private Func<bool> blocked;
        private Action<string> hover, click;
        private string unitId;
        private bool pointerOver, keyboardFocused, selected;
        private int rank = -1;
        private UnitState unit;
        private CombatActionPreview projection;
        private Image[] focusGraphics;
        public string UnitId => unitId;
        public Button Button => button;
        public RawImage Portrait => portrait;
        public Text Caption => caption;

        public static CombatPortraitCardView Create(Transform parent)
        {
            var prefab = Resources.Load<CombatPortraitCardView>(ResourcePath);
            if (prefab == null) throw new MissingReferenceException("Missing formal portrait-card prefab");
            return Instantiate(prefab, parent, false);
        }
        public void Configure(Button target, RawImage image, Image bezel, Image focusRing, Image currentMark, Image allegiance, Text label)
        { button = target; portrait = image; frame = bezel; focus = focusRing; activeMark = currentMark; factionMark = allegiance; caption = label; }
        public void Initialize(Func<bool> modal, Action<string> onHover, Action<string> onClick)
        {
            blocked = modal; hover = onHover; click = onClick;
            button.onClick.AddListener(() => { if (blocked?.Invoke() != true && unit != null && unit.IsAlive) click?.Invoke(unitId); });
            button.transition = Selectable.Transition.None;
            focusGraphics = focus.GetComponentsInChildren<Image>(true);
            var sprite = Resources.Load<Sprite>("Art/CombatHudChrome/portrait_frame");
            if (sprite != null)
            {
                frame.sprite = sprite; frame.color = Color.white;
                frame.rectTransform.anchoredPosition = new Vector2(-4, 4);
                frame.rectTransform.sizeDelta = new Vector2(sprite.texture.width * 4, sprite.texture.height * 4);
            }
            ApplyFocus();
        }
        public void Bind(UnitState value, int ordinal, int movement, bool targetSelected, CombatActionPreview preview = null)
        {
            if (unitId != value.Id && (pointerOver || keyboardFocused)) hover?.Invoke(null);
            unit = value; unitId = value.Id; selected = targetSelected;
            projection = preview != null && preview.CanSubmit ? preview : null;
            CombatHudPortraitAssets.Bind(portrait, unit);
            if (placeholder != null) { placeholder.gameObject.SetActive(!portrait.enabled); placeholder.text = unit.DisplayName; }
            bool current = ordinal == 0;
            activeMark.gameObject.SetActive(current);
            activeMark.color = FormalUiTheme.Cyan;
            factionMark.color = unit.IsHero ? FormalUiTheme.Cyan : FormalUiTheme.Danger;
            string name = unit.DisplayName ?? "未识别";
            if (name.Length > 3) name = name.Substring(0, 3);
            caption.text = current ? "当前" : (movement > 0 ? "↑" : movement < 0 ? "↓" : "") + (ordinal + 1) + " " + name;
            caption.color = current ? FormalUiTheme.Cyan : movement > 0 ? FormalUiTheme.Safe : movement < 0 ? FormalUiTheme.Danger : FormalUiTheme.Ink;
            ApplyFocus();
        }
        public void MoveTo(int ordinal, UiMotionProfile motion)
        {
            if (rank == ordinal) return;
            RectTransform rect = (RectTransform)transform;
            Vector2 target = new Vector2(16 + ordinal * 136, -32);
            rect.DOKill();
            if (rank < 0 || motion.IsImmediate) rect.anchoredPosition = target;
            else DOTween.To(() => rect.anchoredPosition, value => rect.anchoredPosition = new Vector2(Mathf.Round(value.x / 2) * 2, Mathf.Round(value.y / 2) * 2), target, motion.StandardDuration).SetUpdate(true).SetTarget(rect);
            rank = ordinal;
        }
        public FormalTooltipContent Tooltip(int ordinal, bool preview)
        {
            if (unit == null || blocked?.Invoke() == true) return default;
            return new FormalTooltipContent(unit.DisplayName,
                (preview ? "预计顺位 " : "行动顺位 ") + (ordinal + 1) + "\n行动值 " + unit.ActionValue + (projection != null && projection.HasActionValuePreview && projection.ActionValueTargetId == unitId ? "　预览 " + CombatActionTimeline.PreviewDelayedValue(unit, projection.ActionValueDelay) : "") + "　速度 " + unit.EffectiveSpeed + "\n点击或确认：聚焦此单位。\n后续状态与行动可能改变预计顺序。", unit.IsHero ? FormalUiTheme.Cyan : FormalUiTheme.Danger);
        }
        private void Update()
        {
            bool available = blocked?.Invoke() != true && unit != null && unit.IsAlive;
            button.interactable = available;
            if (!available && (pointerOver || keyboardFocused)) { pointerOver = keyboardFocused = false; hover?.Invoke(null); GetComponent<FormalHoverTooltipTrigger>()?.OnPointerExit(null); ApplyFocus(); }
        }
        private void ApplyFocus()
        {
            if (focus == null) return;
            focus.gameObject.SetActive(pointerOver || keyboardFocused || selected);
            focus.color = Color.clear;
            Color stroke = selected ? FormalUiTheme.Amber : FormalUiTheme.Cyan;
            foreach (Image edge in focusGraphics ?? Array.Empty<Image>())
                if (edge.name.StartsWith("细边_", StringComparison.Ordinal)) edge.color = stroke;
        }
        public void OnPointerEnter(PointerEventData e) { if (blocked?.Invoke() == true) return; pointerOver = true; hover?.Invoke(unitId); ApplyFocus(); }
        public void OnPointerExit(PointerEventData e) { pointerOver = false; if (!keyboardFocused) hover?.Invoke(null); ApplyFocus(); }
        public void OnSelect(BaseEventData e) { if (blocked?.Invoke() == true) return; keyboardFocused = true; hover?.Invoke(unitId); ApplyFocus(); }
        public void OnDeselect(BaseEventData e) { keyboardFocused = false; if (!pointerOver) hover?.Invoke(null); ApplyFocus(); }
        private void OnDisable()
        {
            if (pointerOver || keyboardFocused) hover?.Invoke(null);
            pointerOver = keyboardFocused = false;
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == button.gameObject) EventSystem.current.SetSelectedGameObject(null);
            ((RectTransform)transform).DOKill(); rank = -1;
            ApplyFocus();
        }
    }
}
