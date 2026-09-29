using System;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    public sealed class BattlefieldHoverCardView : MonoBehaviour
    {
        public const string ResourcePath = "UI/Prefabs/BattlefieldHoverCard";
        public const float ExpandedWidth = 760f;
        public const float UnitWindowHeight = 216f;
        public const float DetailWindowWidth = 312f;
        public const float StatusWindowHeight = 80f;
        public const float MaxStatusWindowHeight = 192f;
        public const float TerrainWindowHeight = 68f;
        public const float MaxTerrainWindowHeight = 124f;
        public const float WindowGap = 6f;

        [SerializeField] private RectTransform cardRoot;
        [SerializeField] private GameObject unitCard;
        [SerializeField] private RawImage unitPortrait;
        [SerializeField] private Text unitName;
        [SerializeField] private Text unitIdentity;
        [SerializeField] private Text unitVitals;
        [SerializeField] private Text unitShield;
        [SerializeField] private Text unitCombat;
        [SerializeField] private Image unitWeaponIcon;
        [SerializeField] private Text unitWeapon;
        [SerializeField] private RawImage unitIntentIcon;
        [SerializeField] private Text unitIntent;
        [SerializeField] private RectTransform[] statusCards = new RectTransform[6];
        [SerializeField] private RawImage[] statusIcons = new RawImage[6];
        [SerializeField] private Text[] statusLabels = new Text[6];
        [SerializeField] private RectTransform surfaceCard;
        [SerializeField] private Text surfaceText;
        [SerializeField] private RectTransform terrainEffectCard;
        [SerializeField] private Text terrainEffectText;
        [SerializeField] private RectTransform objectCard;
        [SerializeField] private Text objectText;

        public bool IsVisible => gameObject.activeSelf;
        public bool IsUnitWindowVisible => unitCard != null && unitCard.activeSelf;
        public Vector2 CurrentSize => cardRoot == null ? Vector2.zero : cardRoot.sizeDelta;
        public int VisibleStatusWindowCount
        {
            get
            {
                int count = 0;
                foreach (RectTransform card in statusCards)
                    if (card != null && card.gameObject.activeSelf) count++;
                return count;
            }
        }
        public bool IsSurfaceWindowVisible => surfaceCard != null && surfaceCard.gameObject.activeSelf;
        public bool IsTerrainEffectWindowVisible => terrainEffectCard != null && terrainEffectCard.gameObject.activeSelf;
        public bool IsObjectWindowVisible => objectCard != null && objectCard.gameObject.activeSelf;
        public bool HasRequiredBindings => cardRoot != null && unitCard != null && unitPortrait != null &&
            unitName != null && unitIdentity != null && unitVitals != null && unitShield != null && unitCombat != null &&
            unitWeaponIcon != null && unitWeapon != null && unitIntentIcon != null && unitIntent != null &&
            surfaceCard != null && surfaceText != null && terrainEffectCard != null && terrainEffectText != null &&
            objectCard != null && objectText != null && statusCards != null && statusIcons != null &&
            statusLabels != null && statusCards.Length == 6 && statusIcons.Length == 6 && statusLabels.Length == 6;

        public void Bind(BattlefieldCellPresentation model, CombatState state)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (!HasRequiredBindings) throw new InvalidOperationException("Battlefield hover card prefab bindings are incomplete.");

            UnitState unit = model.Unit;
            bool hasUnit = unit != null && !unit.IsHero;
            unitCard.SetActive(hasUnit);
            if (hasUnit) BindUnit(model, state, unit);

            float stackHeight = BindDetailStack(model, hasUnit);
            cardRoot.sizeDelta = new Vector2(hasUnit ? ExpandedWidth : DetailWindowWidth,
                Mathf.Max(hasUnit ? UnitWindowHeight : 0f, stackHeight));
            if (hasUnit)
                FormalUiKit.ThinFrame(unitCard.transform, ((RectTransform)unitCard.transform).sizeDelta,
                    FormalUiTheme.Rule, "悬浮窗细框");
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void SetTopLeft(Vector2 position)
        {
            if (cardRoot != null) cardRoot.anchoredPosition = position;
        }

        public void Hide()
        {
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }

        private void BindUnit(BattlefieldCellPresentation model, CombatState state, UnitState unit)
        {
            unitPortrait.texture = model.UnitTexture;
            unitPortrait.uvRect = model.UnitUv;
            unitPortrait.color = model.UnitTint;
            if (model.UnitTexture != null)
            {
                float scale = Mathf.Min(96f / model.UnitTexture.width, 160f / model.UnitTexture.height);
                float width = Mathf.Round(model.UnitTexture.width * scale);
                float height = Mathf.Round(model.UnitTexture.height * scale);
                unitPortrait.rectTransform.sizeDelta = new Vector2(width, height);
                unitPortrait.rectTransform.anchoredPosition = new Vector2(4f + (96f - width) * .5f,
                    -4f - (160f - height) * .5f);
            }
            unitName.text = unit.DisplayName;
            unitName.color = FormalUiTheme.Danger;
            unitIdentity.text = "敌方单位";
            unitVitals.text = unit.Health + "/" + unit.MaxHealth;
            unitShield.text = state.Ruleset == CombatRuleset.Roguelite ? unit.Shield.ToString() :
                unit.Shield + "/" + unit.MaxShield;
            unitCombat.gameObject.SetActive(false);
            unitWeaponIcon.gameObject.SetActive(false);
            unitWeapon.gameObject.SetActive(false);

            bool hasIntent = model.Intent != null && model.IntentTexture != null;
            unitIntentIcon.gameObject.SetActive(hasIntent);
            unitIntent.gameObject.SetActive(true);
            unitIntentIcon.texture = model.IntentTexture;
            unitIntent.text = hasIntent ? FormalBattlefieldView.CompactIntent(model.Intent) : "意图尚未显露";
        }

        private float BindDetailStack(BattlefieldCellPresentation model, bool hasUnit)
        {
            float x = hasUnit ? 432f : 0f;
            float y = 0f;
            int visibleStatuses = hasUnit ? Math.Min(statusCards.Length, model.Statuses.Count) : 0;
            for (int index = 0; index < statusCards.Length; index++)
            {
                bool active = index < visibleStatuses;
                statusCards[index].gameObject.SetActive(active);
                if (!active) continue;
                BattlefieldStatusVisual status = model.Statuses[index];
                statusIcons[index].texture = status.Texture;
                CombatStatusPresentation description = status.Presentation;
                statusLabels[index].text = description.DisplayName + " " + description.ValueText + "\n" +
                    description.HoverDescription;
                statusLabels[index].rectTransform.sizeDelta = new Vector2(240f, MaxStatusWindowHeight - 16f);
                float height = Mathf.Clamp(Mathf.Ceil(statusLabels[index].preferredHeight) + 32f,
                    StatusWindowHeight, MaxStatusWindowHeight);
                statusLabels[index].rectTransform.sizeDelta = new Vector2(240f, height - 16f);
                Place(statusCards[index], x, ref y, height);
            }

            BindTerrainWindow(surfaceCard, surfaceText, "地表瓦片", model.SurfaceHoverText, x, ref y);
            BindTerrainWindow(terrainEffectCard, terrainEffectText, "地形效果", model.TerrainEffectHoverText, x, ref y);
            BindTerrainWindow(objectCard, objectText, "物块", model.ObjectHoverText, x, ref y);
            return Mathf.Max(0f, y - WindowGap);
        }

        private static void BindTerrainWindow(RectTransform card, Text label, string category, string value,
            float x, ref float y)
        {
            bool active = !string.IsNullOrWhiteSpace(value);
            card.gameObject.SetActive(active);
            if (!active) return;
            label.text = category + "\n" + value.Trim();
            label.rectTransform.sizeDelta = new Vector2(280f, MaxTerrainWindowHeight - 16f);
            float height = Mathf.Clamp(Mathf.Ceil(label.preferredHeight) + 24f,
                TerrainWindowHeight, MaxTerrainWindowHeight);
            label.rectTransform.sizeDelta = new Vector2(280f, height - 16f);
            Place(card, x, ref y, height);
        }

        private static void Place(RectTransform card, float x, ref float y, float height)
        {
            card.anchoredPosition = new Vector2(x, -y);
            card.sizeDelta = new Vector2(DetailWindowWidth, height);
            FormalUiKit.ThinFrame(card, card.sizeDelta, FormalUiTheme.Rule, "悬浮窗细框");
            y += height + WindowGap;
        }

        private static string WeaponIconPath(UnitState unit)
        {
            string weaponId = unit?.MainHand?.Id;
            foreach (FormalArtEntry entry in FormalArtRegistry.Items)
                if (string.Equals(entry.RuntimeId, weaponId, StringComparison.OrdinalIgnoreCase)) return entry.ResourcePath;
            return FormalArtRegistry.ItemPath("category_weapon");
        }
    }
}
