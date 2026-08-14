using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using OCC.Combat.Presentation;

namespace OCC.Combat.Tests
{
    public sealed class FormalArtAssetAuditTests
    {
        [TestCase("top-left", 0f, 1f)]
        [TestCase("top-center", .5f, 1f)]
        [TestCase("top-right", 1f, 1f)]
        [TestCase("bottom-left", 0f, 0f)]
        [TestCase("bottom-center", .5f, 0f)]
        [TestCase("bottom-right", 1f, 0f)]
        [TestCase("center", .5f, .5f)]
        public void LayoutAnchorsResolveExplicitly(string id, float x, float y)
        {
            Assert.That(FormalUiKit.ResolveAnchor(id), Is.EqualTo(new Vector2(x, y)));
        }

        [TestCase(14, 18)]
        [TestCase(15, 20)]
        [TestCase(18, 24)]
        [TestCase(21, 22)]
        [TestCase(38, 38)]
        public void CompactFontSizes_ProjectToWholePixelsAtHalfScale(int source, int expected)
        {
            int actual = FormalUiTheme.PixelAlignedFontSize(source, true);
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actual % 2, Is.Zero);
        }

        [Test]
        public void EveryNonBlockedRegistryEntry_LoadsWithoutFallback()
        {
            HashSet<string> blockedUnits = FormalArtRegistry.Units.Select(entry => entry.AssetId).ToHashSet(StringComparer.Ordinal);
            FormalArtEntry[] active = FormalArtRegistry.All
                .Where(entry => !blockedUnits.Contains(entry.AssetId) && !FormalArtRegistry.Vfx.Contains(entry))
                .ToArray();
            string[] missing = active.Where(entry => Resources.Load<Sprite>(entry.ResourcePath) == null)
                .Select(entry => entry.AssetId + " => " + entry.ResourcePath).ToArray();
            Assert.That(missing, Is.Empty, string.Join("\n", missing));
            Assert.That(active, Has.Length.EqualTo(200), "Formal player UI includes navigation and combat-semantic icon vocabularies.");
            Assert.That(blockedUnits.Count, Is.EqualTo(16), "Character/unit art is explicitly product-blocked, not silently omitted.");
        }

        [Test]
        public void EveryFormalVfx_HasSixIndependentFrames()
        {
            var missing = new List<string>();
            foreach (FormalArtEntry effect in FormalArtRegistry.Vfx)
            for (int frame = 0; frame < 6; frame++)
            {
                string path = effect.ResourcePath + "/frame_" + frame.ToString("00");
                if (Resources.Load<Sprite>(path) == null) missing.Add(path);
            }
            Assert.That(missing, Is.Empty, string.Join("\n", missing));
            Assert.That(FormalArtRegistry.Vfx.Count, Is.EqualTo(30));
        }

        [Test]
        public void EveryFormalTexture_UsesPixelImportContract()
        {
            string[] paths = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Game/Resources/Art" })
                .Select(AssetDatabase.GUIDToAssetPath).Where(path => path.Contains("/Formal", StringComparison.Ordinal)).ToArray();
            var failures = new List<string>();
            foreach (string path in paths)
            {
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                bool semanticMicroIcon = path.Contains("/FormalIntentIcons16/", StringComparison.Ordinal) ||
                                         path.EndsWith("/action_point.png", StringComparison.Ordinal) ||
                                         path.EndsWith("/mana.png", StringComparison.Ordinal) ||
                                         path.EndsWith("/notice.png", StringComparison.Ordinal);
                float expectedPixelsPerUnit = semanticMicroIcon ? 16f : 32f;
                if (importer == null || importer.textureType != TextureImporterType.Sprite || importer.filterMode != FilterMode.Point ||
                    importer.wrapMode != TextureWrapMode.Clamp || importer.mipmapEnabled ||
                    Math.Abs(importer.spritePixelsPerUnit - expectedPixelsPerUnit) > .01f)
                    failures.Add(path);
            }
            Assert.That(paths.Length, Is.GreaterThanOrEqualTo(433));
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [Test]
        public void NavigationIcons_AreIndependent32PixelFormalAssets()
        {
            foreach (FormalArtEntry entry in FormalArtRegistry.Navigation)
            {
                Sprite sprite = Resources.Load<Sprite>(entry.ResourcePath);
                Assert.That(sprite, Is.Not.Null, entry.AssetId);
                Assert.That(sprite.rect.width, Is.EqualTo(32), entry.AssetId);
                Assert.That(sprite.rect.height, Is.EqualTo(32), entry.AssetId);
                TextureImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite)) as TextureImporter;
                Assert.That(importer, Is.Not.Null, entry.AssetId);
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point), entry.AssetId);
                Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp), entry.AssetId);
                Assert.That(importer.mipmapEnabled, Is.False, entry.AssetId);
                Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(32f), entry.AssetId);
            }
        }

        [Test]
        public void CombatSemanticIcons_Are16PixelFormalAssets()
        {
            foreach (FormalArtEntry entry in FormalArtRegistry.Semantics)
            {
                Sprite sprite = Resources.Load<Sprite>(entry.ResourcePath);
                Assert.That(sprite, Is.Not.Null, entry.AssetId);
                Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(16, 16)), entry.AssetId);
                TextureImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite)) as TextureImporter;
                Assert.That(importer, Is.Not.Null, entry.AssetId);
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point), entry.AssetId);
                Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp), entry.AssetId);
                Assert.That(importer.mipmapEnabled, Is.False, entry.AssetId);
                Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(16f), entry.AssetId);
            }
        }

        [Test]
        public void EnemyIntentIcons_AreIndependent16PixelFormalAssets()
        {
            foreach (FormalArtEntry entry in FormalArtRegistry.Intents)
            {
                Sprite sprite = Resources.Load<Sprite>(entry.ResourcePath);
                Assert.That(sprite, Is.Not.Null, entry.AssetId);
                Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(16, 16)), entry.AssetId);
                TextureImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite)) as TextureImporter;
                Assert.That(importer, Is.Not.Null, entry.AssetId);
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point), entry.AssetId);
                Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp), entry.AssetId);
                Assert.That(importer.mipmapEnabled, Is.False, entry.AssetId);
                Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(16f), entry.AssetId);
            }
        }

        [Test]
        public void FireSpellIcons_UseAReviewedNonGenericSet()
        {
            string[] assetPaths = FormalArtRegistry.FireSpells.Select(entry =>
                AssetDatabase.GetAssetPath(Resources.Load<Sprite>(entry.ResourcePath))).ToArray();
            Assert.That(assetPaths.All(path => !string.IsNullOrEmpty(path)), Is.True);
            Assert.That(assetPaths.Distinct(StringComparer.Ordinal).Count(), Is.GreaterThanOrEqualTo(39),
                "v0.2 may deliberately reuse a formally audited icon when the visual semantics match, but the set must not collapse to generic placeholders.");
        }

        [Test]
        public void PixelUiSkin_HasAllSemanticSlicesAndImporterContract()
        {
            string[] ids = { "panel", "panel_elevated", "header", "button_idle", "button_hover", "button_pressed", "button_disabled",
                "tab_idle", "tab_active", "slot", "bar_track", "bar_fill", "focus", "danger", "reward", "panel_console", "panel_module",
                "panel_target", "panel_log", "group_weapon", "group_spell", "group_interaction", "group_item", "button_end_turn",
                "bar_segment_health", "bar_segment_shield", "bar_segment_mana", "badge_cost", "slot_locked", "timeline_node" };
            foreach (string id in ids)
            {
                Sprite sprite = Resources.Load<Sprite>("Art/FormalUISkin16/" + id);
                Assert.That(sprite, Is.Not.Null, id);
                Assert.That(sprite.border, Is.EqualTo(new Vector4(4f, 4f, 4f, 4f)), id);
                TextureImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite)) as TextureImporter;
                Assert.That(importer, Is.Not.Null, id);
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point), id);
                Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp), id);
                Assert.That(importer.mipmapEnabled, Is.False, id);
            }
            Assert.That(FormalUiArtRegistry.Entries.Count, Is.EqualTo(ids.Length));
            Assert.That(FormalUiArtRegistry.Entries.Select(entry => entry.RuntimeId), Is.EquivalentTo(ids));
        }

        [Test]
        public void PixelUiV02_ConfigIsCompleteAndUsesApprovedSplit()
        {
            OccPixelUiConfigData config = OccPixelUiConfig.Data;
            Assert.That(OccPixelUiConfig.Validate(), Is.Empty, string.Join("\n", OccPixelUiConfig.Validate()));
            Assert.That(config.schema, Is.EqualTo(OccPixelUiConfig.RequiredSchema));
            Assert.That(config.visualBaseline, Does.Contain("v02_strong_pixel"));
            Assert.That(config.battlefieldWidth, Is.EqualTo(1440));
            Assert.That(config.hudWidth, Is.EqualTo(480));
            Assert.That(config.logicalPixelScale, Is.GreaterThanOrEqualTo(4));
            string[] requiredLayouts = { "global.header", "landing.card", "map.status", "map.board", "map.detail", "briefing.card",
                "settings.card", "archive.card", "modal.confirm", "modal.toast", "map.toast", "combat.toast", "settlement.card", "settlement.rewardCard", "combat.header", "combat.rightConsole",
                "combat.selected", "combat.hero", "combat.timeline", "combat.log", "combat.commands", "combat.outcome" };
            Assert.That(config.layouts.Select(entry => entry.id), Is.SupersetOf(requiredLayouts));
            Assert.That(config.layouts.Select(entry => entry.id), Does.Not.Contain("combat.target"));
            Assert.That(OccPixelUiConfig.Layout("combat.rightConsole").width, Is.LessThanOrEqualTo(config.hudWidth));
            Assert.That(OccPixelUiConfig.Layout("combat.commands").width, Is.LessThanOrEqualTo(config.battlefieldWidth));
            Assert.That(OccPixelUiConfig.StateSkin("button", "selected"), Is.EqualTo("tab_active"));
        }

        [Test]
        public void PeripheralUi_ConfigAndPixelAssetsAreComplete()
        {
            Assert.That(FormalUiEffectsConfig.Validate(), Is.Empty, string.Join("\n", FormalUiEffectsConfig.Validate()));
            OccPeripheralUiData config = FormalUiEffectsConfig.Data;
            Assert.That(config.backdrops.Select(entry => entry.id), Is.SupersetOf(new[] { "landing", "map", "briefing", "inventory", "settlement", "archive", "settings" }));
            Assert.That(config.feedback.Select(entry => entry.id), Is.EquivalentTo(new[] { "click", "success", "rejected" }));
            foreach (string path in new[] { config.startupBackdrop, config.scanlineSprite, config.transitionSprite })
                Assert.That(Resources.Load<Sprite>(path), Is.Not.Null, path);
            foreach (OccPeripheralAssetEntry entry in config.backdrops)
                Assert.That(Resources.Load<Sprite>(entry.resourcePath), Is.Not.Null, entry.id);
            foreach (OccPeripheralFeedbackEntry entry in config.feedback)
            for (int frame = 0; frame < entry.frameCount; frame++)
                Assert.That(Resources.Load<Sprite>(entry.resourcePath + "/frame_" + frame.ToString("00")), Is.Not.Null, entry.id + "/" + frame);

            string[] paths = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Game/Resources/Art/FormalUIBackdrops", "Assets/Game/Resources/Art/FormalUIFeedback" })
                .Select(AssetDatabase.GUIDToAssetPath).ToArray();
            Assert.That(paths, Has.Length.EqualTo(26));
            foreach (string path in paths)
            {
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.That(importer, Is.Not.Null, path);
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point), path);
                Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp), path);
                Assert.That(importer.mipmapEnabled, Is.False, path);
            }
            foreach (string id in new[] { "landing", "map", "briefing", "inventory", "settlement" })
            {
                Sprite sprite = Resources.Load<Sprite>(FormalUiEffectsConfig.BackdropPath(id));
                Assert.That(sprite.texture.width, Is.EqualTo(480), id);
                Assert.That(sprite.texture.height, Is.EqualTo(270), id);
            }
        }
    }
}
