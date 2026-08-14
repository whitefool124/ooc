using System.Linq;
using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OCC.Combat.Tests
{
    public sealed class FormalUiThemeTests
    {
        [Test]
        public void ComponentTokens_KeepReadablePixelScaleAndMinimumTargets()
        {
            Assert.That(FormalUiTheme.CaptionFontSize, Is.GreaterThanOrEqualTo(14));
            Assert.That(FormalUiTheme.BodyFontSize, Is.GreaterThan(FormalUiTheme.CaptionFontSize));
            Assert.That(FormalUiTheme.HeadingFontSize, Is.GreaterThan(FormalUiTheme.BodyFontSize));
            Assert.That(FormalUiTheme.TitleFontSize, Is.GreaterThan(FormalUiTheme.HeadingFontSize));
            Assert.That(FormalUiTheme.IconSlotSize, Is.EqualTo(32));
            Assert.That(FormalUiTheme.MinimumInteractiveHeight, Is.GreaterThanOrEqualTo(40));
            Assert.That(FormalUiTheme.SpaceMedium, Is.EqualTo(FormalUiTheme.SpaceSmall * 2));
            Assert.That(FormalUiTheme.SpaceLarge, Is.EqualTo(FormalUiTheme.SpaceSmall * 3));
        }

        [TestCase(FormalUiButtonTone.Neutral)]
        [TestCase(FormalUiButtonTone.Primary)]
        [TestCase(FormalUiButtonTone.Positive)]
        [TestCase(FormalUiButtonTone.Warning)]
        [TestCase(FormalUiButtonTone.Dangerous)]
        public void ButtonPalette_ExposesEveryRequiredInteractionState(FormalUiButtonTone tone)
        {
            FormalUiButtonPalette palette = FormalUiTheme.ButtonPalette(tone);

            Assert.That(palette.Hover, Is.Not.EqualTo(palette.Normal));
            Assert.That(palette.Pressed, Is.Not.EqualTo(palette.Normal));
            Assert.That(palette.Selected, Is.Not.EqualTo(palette.Normal));
            Assert.That(palette.Disabled, Is.EqualTo(FormalUiTheme.Disabled));
        }

        [Test]
        public void SemanticColors_DoNotCollapseToOneSignal()
        {
            Assert.That(FormalUiTheme.Cyan, Is.Not.EqualTo(FormalUiTheme.Danger));
            Assert.That(FormalUiTheme.Cyan, Is.Not.EqualTo(FormalUiTheme.Amber));
            Assert.That(FormalUiTheme.Safe, Is.Not.EqualTo(FormalUiTheme.Danger));
            Assert.That(FormalUiTheme.Focus, Is.Not.EqualTo(FormalUiTheme.Disabled));
        }

        [Test]
        public void PageChecklist_CoversEveryFormalPlayerSurfaceWithStableFocusKeys()
        {
            string[] expected =
            {
                "landing", "map", "briefing", "combat", "shop-workshop",
                "inventory-loot", "settlement", "settings", "archive"
            };

            Assert.That(FormalUiPageChecklist.Entries.Select(entry => entry.Id), Is.EquivalentTo(expected));
            Assert.That(FormalUiPageChecklist.Entries.All(entry => !string.IsNullOrWhiteSpace(entry.DefaultFocusKey)), Is.True);
            Assert.That(FormalUiPageChecklist.Entries.Single(entry => entry.Id == "landing").DefaultFocusKey, Is.EqualTo("按钮_近战热压"));
            Assert.That(FormalUiPageChecklist.Entries.Single(entry => entry.Id == "settings").DefaultFocusKey, Is.EqualTo("按钮_设置_0"));
            Assert.That(FormalUiPageChecklist.Entries.Where(entry => entry.Id != "landing").All(entry => entry.HasBackPath), Is.True);
            Assert.That(FormalUiPageChecklist.Entries.Any(entry => entry.CoversDisabledState), Is.True);
            Assert.That(FormalUiPageChecklist.Entries.Any(entry => entry.CoversEmptyState), Is.True);
        }

        [Test]
        public void AccessibilityPreferencesApplyToSharedContrastAndTextTokens()
        {
            FormalUiTheme.ConfigureAccessibility(false, false);
            float baseContrast = ContrastRatio(FormalUiTheme.Text, FormalUiTheme.Panel);
            int baseSize = FormalUiTheme.ResponsiveFontSize(FormalUiTheme.BodyFontSize);
            try
            {
                FormalUiTheme.ConfigureAccessibility(true, true);
                Assert.That(FormalUiTheme.HighContrastEnabled, Is.True);
                Assert.That(FormalUiTheme.LargeTextEnabled, Is.True);
                Assert.That(ContrastRatio(FormalUiTheme.Text, FormalUiTheme.Panel), Is.GreaterThanOrEqualTo(baseContrast));
                Assert.That(FormalUiTheme.ResponsiveFontSize(FormalUiTheme.BodyFontSize), Is.GreaterThan(baseSize));
                Assert.That(ContrastRatio(FormalUiTheme.Muted, FormalUiTheme.Panel), Is.GreaterThanOrEqualTo(4.5f));
            }
            finally
            {
                FormalUiTheme.ConfigureAccessibility(false, false);
            }
        }

        [TestCase("action", "7")]
        [TestCase("aether", "4")]
        [TestCase("notice", "")]
        public void SemanticChip_UsesIconAndKeepsWordOutOfPersistentText(string semanticId, string value)
        {
            GameObject root = new GameObject("root", typeof(RectTransform));
            try
            {
                Text label = FormalUiKit.SemanticChip(semanticId, value, root.transform, Vector2.zero, null);
                Assert.That(label.text, Is.EqualTo(value));
                Image icon = root.GetComponentsInChildren<Image>().Single();
                Assert.That(icon.sprite, Is.Not.Null);
                Assert.That(root.GetComponentsInChildren<Text>().Select(item => item.text), Does.Not.Contain("行动"));
                Assert.That(root.GetComponentsInChildren<Text>().Select(item => item.text), Does.Not.Contain("以太"));
                Assert.That(root.GetComponentsInChildren<Text>().Select(item => item.text), Does.Not.Contain("注意"));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void SemanticIcon_MouseHoverShowsPlayerWord()
        {
            GameObject canvasObject = new GameObject("canvas", typeof(RectTransform), typeof(Canvas));
            GameObject eventSystemObject = new GameObject("events", typeof(EventSystem));
            try
            {
                Canvas canvas = canvasObject.GetComponent<Canvas>();
                FormalHoverTooltip tooltip = canvasObject.AddComponent<FormalHoverTooltip>();
                tooltip.Initialize(canvas);
                FormalUiKit.SemanticChip("action", "2", canvasObject.transform, Vector2.zero, tooltip);
                FormalHoverTooltipTrigger trigger = canvasObject.GetComponentInChildren<FormalHoverTooltipTrigger>();
                trigger.OnPointerEnter(new PointerEventData(eventSystemObject.GetComponent<EventSystem>()) { position = Vector2.zero });

                Assert.That(tooltip.IsVisible, Is.True);
                Text title = canvasObject.GetComponentsInChildren<Text>(true).Single(item => item.gameObject.name == "悬浮标题");
                Assert.That(title.text, Is.EqualTo("行动"));
            }
            finally
            {
                Object.DestroyImmediate(eventSystemObject);
                Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void FireSpellRewardCopy_DescribesPlayerEffectInsteadOfImplementationEnums()
        {
            FireSpellDefinition fireball = FireSpellCatalog.All.Single(spell => spell.Id == "F-P-R01");
            FireSpellDefinition weaponLoad = FireSpellCatalog.All.Single(spell => spell.Id == "F-P-U01");
            Assert.That(RogueliteSettlementPresentation.FireSpellPlayerSummary(fireball), Does.Contain("12 点火焰伤害"));
            Assert.That(RogueliteSettlementPresentation.FireSpellPlayerSummary(weaponLoad), Does.Contain("下一次武器攻击"));
            Assert.That(RogueliteSettlementPresentation.FireSpellPlayerSummary(weaponLoad), Does.Not.Contain("WeaponAttachment"));
            Assert.That(RogueliteSettlementPresentation.FireSpellPlayerSummary(weaponLoad), Does.Not.Contain("OnTrigger"));
        }

        [Test]
        public void EveryFireSpell_UsesPlayerEffectAndTargetCopyWithoutImplementationEnums()
        {
            string[] forbidden = System.Enum.GetNames(typeof(FireCombatAffinity))
                .Concat(System.Enum.GetNames(typeof(FireDeliveryMode)))
                .Concat(System.Enum.GetNames(typeof(FireWeaponRequirement)))
                .Concat(System.Enum.GetNames(typeof(FireTriggerWindow)))
                .Concat(System.Enum.GetNames(typeof(FireConsumptionRule)))
                .Concat(System.Enum.GetNames(typeof(FireTargetKind)))
                .Concat(System.Enum.GetNames(typeof(FireSelectionShape)))
                .Concat(System.Enum.GetNames(typeof(FireRuleKind))).Distinct().ToArray();
            foreach (FireSpellDefinition spell in FireSpellCatalog.All)
            {
                string effect = RogueliteSettlementPresentation.FireSpellPlayerSummary(spell);
                string target = RogueliteSettlementPresentation.FireSpellTargetSummary(spell);
                Assert.That(effect, Is.Not.Empty, spell.Id);
                Assert.That(target, Is.Not.Empty, spell.Id);
                Assert.That(effect, Does.Not.Contain("产生术式效果"), spell.Id);
                foreach (string token in forbidden)
                {
                    Assert.That(effect, Does.Not.Contain(token), spell.Id + " effect exposed " + token);
                    Assert.That(target, Does.Not.Contain(token), spell.Id + " target exposed " + token);
                }
            }
        }

        private static float ContrastRatio(UnityEngine.Color a, UnityEngine.Color b)
        {
            float bright = System.Math.Max(Luminance(a), Luminance(b));
            float dark = System.Math.Min(Luminance(a), Luminance(b));
            return (bright + .05f) / (dark + .05f);
        }

        private static float Luminance(UnityEngine.Color color)
        {
            return .2126f * Linear(color.r) + .7152f * Linear(color.g) + .0722f * Linear(color.b);
        }

        private static float Linear(float value)
        {
            return value <= .03928f ? value / 12.92f : UnityEngine.Mathf.Pow((value + .055f) / 1.055f, 2.4f);
        }
    }
}
