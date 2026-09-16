using System;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    public sealed class BattlefieldHoverCardView : MonoBehaviour
    {
        public const string ResourcePath = "UI/Prefabs/BattlefieldHoverCard";
        public const float ExpandedWidth = 760f;
        public const float UnitWindowHeight = 340f;
        public const float DetailWindowWidth = 312f;
        public const float StatusWindowHeight = 48f;
        public const float TerrainWindowHeight = 68f;
        public const float MaxTerrainWindowHeight = 124f;
        public const float WindowGap = 6f;

        [SerializeField] private RectTransform cardRoot;
        [SerializeField] private GameObject unitCard;
        [SerializeField] private RawImage unitPortrait;
        [SerializeField] private Text unitName;
        [SerializeField] private Text unitIdentity;
        [SerializeField] private Text unitVitals;
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
            unitName != null && unitIdentity != null && unitVitals != null && unitCombat != null &&
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
            bool hasUnit = unit != null;
            unitCard.SetActive(hasUnit);
            if (hasUnit) BindUnit(model, state, unit);

            float stackHeight = BindDetailStack(model, hasUnit);
            cardRoot.sizeDelta = new Vector2(hasUnit ? ExpandedWidth : DetailWindowWidth,
                Mathf.Max(hasUnit ? UnitWindowHeight : 0f, stackHeight));
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
            unitName.text = unit.DisplayName;
            unitName.color = unit.IsHero ? FormalUiTheme.Cyan : FormalUiTheme.Danger;
            unitIdentity.text = unit.IsHero ? "我方单位" : "敌方单位";
            unitVitals.text = state.Ruleset == CombatRuleset.Roguelite
                ? "生命当前 " + unit.Health + "　上限 " + unit.MaxHealth + "\n护盾当前 " + unit.Shield + "　无上限"
                : "生命当前 " + unit.Health + "　上限 " + unit.MaxHealth + "\n护盾当前 " + unit.Shield + "　上限 " + unit.MaxShield;
            unitCombat.text = "行动点 " + unit.ActionPoints + "　护甲 " + unit.EffectiveArmor + "　格挡 " + unit.Block +
                "\n速度 " + unit.EffectiveSpeed;
            unitWeaponIcon.sprite = Resources.Load<Sprite>(WeaponIconPath(unit));
            unitWeaponIcon.color = unitWeaponIcon.sprite == null ? Color.clear : Color.white;
            unitWeapon.text = unit.MainHand == null ? "未装备武器" :
                unit.MainHand.DisplayName + "\n伤害 " + unit.MainHand.Damage + "　射程 " +
                (unit.MainHand.MinimumRange > 0 ? unit.MainHand.MinimumRange + "–" + unit.MainHand.Range + " 格（近身死区）" : unit.MainHand.Range + " 格");

            bool hasIntent = model.Intent != null && model.IntentTexture != null;
            unitIntentIcon.gameObject.SetActive(hasIntent);
            unitIntent.gameObject.SetActive(hasIntent);
            if (hasIntent)
            {
                unitIntentIcon.texture = model.IntentTexture;
                unitIntent.text = "意图\n" + FormalBattlefieldView.CompactIntent(model.Intent);
            }
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
                statusLabels[index].text = status.Presentation.DisplayName + "　" +
                    status.Presentation.Duration + " 回合";
                Place(statusCards[index], x, ref y, StatusWindowHeight);
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
            float height = Mathf.Clamp(label.preferredHeight + 16f, TerrainWindowHeight, MaxTerrainWindowHeight);
            label.rectTransform.sizeDelta = new Vector2(280f, height - 16f);
            Place(card, x, ref y, height);
        }

        private static void Place(RectTransform card, float x, ref float y, float height)
        {
            card.anchoredPosition = new Vector2(x, -y);
            card.sizeDelta = new Vector2(DetailWindowWidth, height);
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
