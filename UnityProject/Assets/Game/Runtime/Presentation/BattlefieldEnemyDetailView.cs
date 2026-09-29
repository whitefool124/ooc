using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    /// <summary>Selected enemy: compact attributes and intent, with mechanics in hoverable status terms.</summary>
    public sealed class BattlefieldEnemyDetailView : MonoBehaviour
    {
        public const string ResourcePath = "UI/Prefabs/BattlefieldEnemyDetail";
        [SerializeField] private RawImage portrait;
        [SerializeField] private Text enemyName;
        [SerializeField] private Text vitals;
        [SerializeField] private Text shieldValue;
        [SerializeField] private Text intent;
        [SerializeField] private Text speedValue;
        [SerializeField] private RawImage intentIcon;
        [SerializeField] private RectTransform statusList;
        [SerializeField] private BattlefieldEnemyStatusChipView statusTemplate;
        [SerializeField] private Button closeButton;
        [SerializeField] private RectTransform scrollContent;
        [SerializeField] private ScrollRect scroll;

        private readonly List<BattlefieldEnemyStatusChipView> chips = new List<BattlefieldEnemyStatusChipView>();
        private FormalHoverTooltip tooltip;
        private Action closeRequested;
        private string boundEnemyId;
        private string boundSignature;
        private string currentIntentDetail = "尚未显露";
        private bool currentRoguelite;

        public bool HasRequiredBindings => portrait != null && enemyName != null && vitals != null && shieldValue != null &&
            intent != null && speedValue != null && intentIcon != null && statusList != null &&
            statusTemplate != null && statusTemplate.HasRequiredBindings && closeButton != null &&
            scrollContent != null && scroll != null;
        public FormalHoverTooltip HoverTooltip => tooltip;

        public void Initialize(Action onClose, Canvas canvas)
        {
            if (!HasRequiredBindings) throw new InvalidOperationException("Enemy detail prefab bindings are incomplete.");
            closeRequested = onClose;
            closeButton.onClick.RemoveListener(RequestClose);
            closeButton.onClick.AddListener(RequestClose);
            tooltip = FormalHoverTooltip.Create(canvas);
            tooltip.ConstrainToBattlefield();
            FormalHoverTooltipTrigger trigger = intent.gameObject.GetComponent<FormalHoverTooltipTrigger>() ??
                intent.gameObject.AddComponent<FormalHoverTooltipTrigger>();
            trigger.Configure(tooltip, () => new FormalTooltipContent("当前意图",
                currentIntentDetail, FormalUiTheme.Amber));
            BindAttributeTooltip(speedValue, "速度", "决定行动值增长与出手顺序。");
            statusTemplate.gameObject.SetActive(false);
            Hide();
        }

        public void Bind(CombatState state, UnitState enemy, BattlefieldCellPresentation cell)
        {
            if (state == null || enemy == null || enemy.IsHero || !enemy.IsAlive) { Hide(); return; }
            EnemyIntentPresentation announced = cell?.Intent;
            currentIntentDetail = announced?.DetailedText ?? "尚未显露";
            currentRoguelite = state.Ruleset == CombatRuleset.Roguelite;
            List<StatusTerm> terms = BuildTerms(state, enemy);
            string signature = string.Join("|", enemy.DisplayName, enemy.Health, enemy.MaxHealth,
                enemy.Shield, enemy.MaxShield, enemy.EffectiveArmor, enemy.Block, enemy.EffectiveSpeed,
                announced?.CompactText, currentIntentDetail,
                string.Join("|", terms.Select(term => term.Id + term.Count + term.Detail)));
            bool changedEnemy = !string.Equals(boundEnemyId, enemy.Id, StringComparison.Ordinal);
            UpdatePortrait(cell);
            intentIcon.texture = cell?.IntentTexture;
            intentIcon.color = intentIcon.texture == null ? Color.clear : Color.white;
            if (!changedEnemy && string.Equals(boundSignature, signature, StringComparison.Ordinal))
            {
                RaiseTooltip();
                return;
            }
            boundEnemyId = enemy.Id;
            boundSignature = signature;
            enemyName.text = enemy.DisplayName;
            vitals.text = enemy.Health + "/" + enemy.MaxHealth;
            shieldValue.text = currentRoguelite ? enemy.Shield.ToString() : enemy.Shield + "/" + enemy.MaxShield;
            speedValue.text = "速度 " + enemy.EffectiveSpeed;
            intent.text = "　　" + (announced?.CompactText ?? "尚未显露");
            BindTerms(terms);
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            LayoutElement intentLayout = intent.GetComponent<LayoutElement>();
            if (intentLayout != null) intentLayout.preferredHeight = Mathf.Max(32f, Mathf.Ceil(intent.preferredHeight) + 8f);
            LayoutRebuilder.ForceRebuildLayoutImmediate(statusList);
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollContent);
            if (changedEnemy) scroll.verticalNormalizedPosition = 1f;
            RaiseTooltip();
        }

        public void Hide()
        {
            boundEnemyId = null;
            boundSignature = null;
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }

        public void RaiseTooltip()
        {
            if (tooltip != null) tooltip.transform.SetAsLastSibling();
        }

        private void BindAttributeTooltip(Text value, string title, string description)
        {
            FormalHoverTooltipTrigger trigger = value.transform.parent.GetComponent<FormalHoverTooltipTrigger>() ??
                value.transform.parent.gameObject.AddComponent<FormalHoverTooltipTrigger>();
            trigger.Configure(tooltip, () => new FormalTooltipContent(title,
                currentRoguelite && title != "速度"
                    ? "本场采用普通盾规则，这项数值不参与减伤。"
                    : description, FormalUiTheme.Cyan));
        }

        private void UpdatePortrait(BattlefieldCellPresentation cell)
        {
            portrait.texture = cell?.UnitTexture;
            portrait.uvRect = cell?.UnitUv ?? new Rect(0f, 0f, 1f, 1f);
            portrait.color = cell?.UnitTint ?? Color.white;
            if (portrait.texture == null) return;
            const float frameInnerWidth = 120f;
            const float frameInnerHeight = 168f;
            float scale = Mathf.Min(frameInnerWidth / portrait.texture.width,
                frameInnerHeight / portrait.texture.height);
            float width = Mathf.Round(portrait.texture.width * scale);
            float height = Mathf.Round(portrait.texture.height * scale);
            portrait.rectTransform.sizeDelta = new Vector2(width, height);
            portrait.rectTransform.anchoredPosition = new Vector2(4f + (frameInnerWidth - width) * .5f,
                -4f - (frameInnerHeight - height) * .5f);
        }

        private void BindTerms(IReadOnlyList<StatusTerm> terms)
        {
            for (int i = chips.Count; i < terms.Count; i++)
            {
                BattlefieldEnemyStatusChipView chip = Instantiate(statusTemplate, statusList, false);
                chip.name = "状态词条";
                chips.Add(chip);
            }
            for (int i = 0; i < chips.Count; i++)
            {
                bool visible = i < terms.Count;
                chips[i].gameObject.SetActive(visible);
                if (visible) chips[i].Bind(terms[i].Name, terms[i].Count, terms[i].Detail,
                    terms[i].IconPath, terms[i].Accent, tooltip);
            }
            LayoutElement layout = statusList.GetComponent<LayoutElement>();
            if (layout != null) layout.preferredHeight = Mathf.Max(56f, Mathf.CeilToInt(terms.Count / 2f) * 56f);
        }

        private static List<StatusTerm> BuildTerms(CombatState state, UnitState enemy)
        {
            var terms = new List<StatusTerm>();
            foreach (KeyValuePair<StatusType, int> pair in enemy.Statuses.OrderBy(pair => pair.Key))
            {
                CombatStatusPresentation status = CombatStatusPresentation.From(enemy, pair.Key, state);
                CombatFeedbackSemantic semantic = CombatFeedbackCatalog.For(CombatFeedbackCatalog.ForStatus(pair.Key));
                Color accent = ColorUtility.TryParseHtmlString(semantic.ColorHex, out Color parsed)
                    ? parsed : FormalUiTheme.Danger;
                terms.Add(new StatusTerm("status:" + status.RuntimeId, status.DisplayName, status.ValueText,
                    status.HoverDescription, "Art/FormalStatusIcons32/" + status.RuntimeId, accent));
            }
            foreach (CombatStatusBarEntry entry in state.PassiveEffects.StatusBarEntriesFor(enemy.Id))
                terms.Add(new StatusTerm(entry.RuntimeId, entry.DisplayName, "1",
                    CombatStatusPresentation.FirstSentence(entry.Detail),
                    "Art/FormalResourceIcons32/notice", FormalUiTheme.Amber));
            if (state.Ruleset == CombatRuleset.Roguelite && enemy.EnemyArchetypeId == "shieldguard")
            {
                bool suppressed = enemy.HasStatus(StatusType.BreakStance);
                terms.Add(new StatusTerm("trait:shieldguard-turn-brace", suppressed ? "整盾受阻" : "回合整盾", suppressed ? "0" : "1",
                    suppressed ? "破势期间，回合整盾无法给予护盾。" : "自身回合开始时，若未被破势，获得 2 护盾。",
                    "Art/FormalResourceIcons32/shield", suppressed ? FormalUiTheme.Muted : FormalUiTheme.Amber));
            }
            if (terms.Count == 0)
                terms.Add(new StatusTerm("empty", "无状态", "—", "当前没有被动、增益或减益效果。",
                    "Art/FormalResourceIcons32/notice", FormalUiTheme.Muted));
            return terms;
        }

        private void RequestClose() => closeRequested?.Invoke();
        private void OnDestroy()
        {
            if (closeButton != null) closeButton.onClick.RemoveListener(RequestClose);
            if (tooltip != null && Application.isPlaying) Destroy(tooltip.gameObject);
        }

        private readonly struct StatusTerm
        {
            public readonly string Id, Name, Count, Detail, IconPath;
            public readonly Color Accent;
            public StatusTerm(string id, string name, string count, string detail, string iconPath, Color accent)
            { Id = id; Name = name; Count = count; Detail = detail; IconPath = iconPath; Accent = accent; }
        }
    }
}
