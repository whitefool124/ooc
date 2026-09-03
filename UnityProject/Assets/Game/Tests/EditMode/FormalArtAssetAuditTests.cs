using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using OCC.Combat.Presentation;
using OCC.Combat.Roguelite;

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

        [TestCase(14, 24)]
        [TestCase(15, 24)]
        [TestCase(18, 24)]
        [TestCase(21, 24)]
        [TestCase(38, 48)]
        public void CompactFontSizes_StayOnApprovedNativeGridTiers(int source, int expected)
        {
            int actual = FormalUiTheme.PixelAlignedFontSize(source, true);
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actual % FormalUiTheme.NativeFontGrid, Is.Zero);
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
            Assert.That(active, Has.Length.EqualTo(297), "Formal player UI includes the registered eight-element and sixteen-resource icon domains.");
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
            Assert.That(FormalArtRegistry.Vfx.Count, Is.EqualTo(39));
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
                bool semanticMicroIcon = path.Contains("/FormalCommandIcons16/", StringComparison.Ordinal) ||
                                         path.Contains("/FormalIntentIcons16/", StringComparison.Ordinal) ||
                                         path.Contains("/FormalMapStateIcons16/", StringComparison.Ordinal) ||
                                         path.Contains("/FormalItemSemanticIcons16/", StringComparison.Ordinal) ||
                                         path.Contains("/FormalEquipmentSlotIcons16/", StringComparison.Ordinal);
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
        public void CombatSemanticIcons_AreIndependent32PixelFormalAssets()
        {
            foreach (FormalArtEntry entry in FormalArtRegistry.Semantics)
            {
                Sprite sprite = Resources.Load<Sprite>(entry.ResourcePath);
                Assert.That(sprite, Is.Not.Null, entry.AssetId);
                Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(32, 32)), entry.AssetId);
                TextureImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite)) as TextureImporter;
                Assert.That(importer, Is.Not.Null, entry.AssetId);
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point), entry.AssetId);
                Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp), entry.AssetId);
                Assert.That(importer.mipmapEnabled, Is.False, entry.AssetId);
                Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(32f), entry.AssetId);
            }
        }

        [Test]
        public void ElementIcons_AreIndependent32PixelFormalAssets()
        {
            foreach (FormalArtEntry entry in FormalArtRegistry.Elements)
            {
                Sprite sprite = Resources.Load<Sprite>(entry.ResourcePath);
                Assert.That(sprite, Is.Not.Null, entry.AssetId);
                Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(32, 32)), entry.AssetId);
                TextureImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite)) as TextureImporter;
                Assert.That(importer, Is.Not.Null, entry.AssetId);
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point), entry.AssetId);
                Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp), entry.AssetId);
                Assert.That(importer.mipmapEnabled, Is.False, entry.AssetId);
                Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(32f), entry.AssetId);
            }
        }

        [Test]
        public void RogueResourceIcons_AreIndependent32PixelFormalAssets()
        {
            foreach (FormalArtEntry entry in FormalArtRegistry.ResourceMetrics)
            {
                Sprite sprite = Resources.Load<Sprite>(entry.ResourcePath);
                Assert.That(sprite, Is.Not.Null, entry.AssetId);
                Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(32, 32)), entry.AssetId);
                TextureImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite)) as TextureImporter;
                Assert.That(importer, Is.Not.Null, entry.AssetId);
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point), entry.AssetId);
                Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp), entry.AssetId);
                Assert.That(importer.mipmapEnabled, Is.False, entry.AssetId);
                Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(32f), entry.AssetId);
            }
        }

        [Test]
        public void EquipmentSlotIcons_UseTheSemantic16PixelContract()
        {
            foreach (FormalArtEntry entry in FormalArtRegistry.EquipmentSlots)
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
        public void AcademyEquipment_HasIndependentContentAndExactFootprintArt()
        {
            EquipmentDefinition[] definitions = RogueContentCatalog.CreateAcademyV01().Equipment.ToArray();
            Assert.That(definitions, Has.Length.EqualTo(32));
            Assert.That(FormalArtRegistry.EquipmentItems.Select(entry => entry.RuntimeId),
                Is.EquivalentTo(definitions.Select(definition => definition.DefinitionId)));
            foreach (EquipmentDefinition definition in definitions)
            {
                Sprite icon = Resources.Load<Sprite>(FormalArtRegistry.EquipmentIconPath(definition.DefinitionId));
                Sprite footprint = Resources.Load<Sprite>(FormalArtRegistry.EquipmentFootprintPath(definition.DefinitionId));
                Assert.That(icon, Is.Not.Null, definition.DefinitionId + " content icon");
                Assert.That(footprint, Is.Not.Null, definition.DefinitionId + " inventory footprint");
                Assert.That(icon.rect.size, Is.EqualTo(new Vector2(32, 32)), definition.DefinitionId);
                Assert.That(footprint.rect.size, Is.EqualTo(new Vector2(definition.Width * 32, definition.Height * 32)), definition.DefinitionId);
                foreach (Sprite sprite in new[] { icon, footprint })
                {
                    TextureImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite)) as TextureImporter;
                    Assert.That(importer, Is.Not.Null, definition.DefinitionId);
                    Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point), definition.DefinitionId);
                    Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp), definition.DefinitionId);
                    Assert.That(importer.mipmapEnabled, Is.False, definition.DefinitionId);
                    Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(32f), definition.DefinitionId);
                }
            }
            Assert.That(FormalArtRegistry.EquipmentItems.Select(entry => entry.IconResourcePath).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(32));
            Assert.That(FormalArtRegistry.EquipmentItems.Select(entry => entry.FootprintResourcePath).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(32));
        }

        [Test]
        public void MapStateIcons_AreIndependent16PixelFormalAssets()
        {
            foreach (FormalArtEntry entry in FormalArtRegistry.MapStates)
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
        public void AcademyMapArt_HasExactIndependentPixelAssets()
        {
            FormalArtEntry board = FormalArtRegistry.Required(FormalArtRegistry.MapDecor, "academy_network");
            AssertSpriteContract(board, new Vector2(670, 393));
            AssertSpriteContract(FormalArtRegistry.Required(FormalArtRegistry.MapDecor, "route_joint"), new Vector2(8, 8));
            foreach (FormalArtEntry entry in FormalArtRegistry.MapNodeFrames) AssertSpriteContract(entry, new Vector2(77, 39));
            foreach (FormalArtEntry entry in FormalArtRegistry.MapRegions) AssertSpriteContract(entry, new Vector2(32, 32));
        }

        [Test]
        public void AcademyMapSupportingArt_HasLegacyAtlasAndCompactNodeMarkers()
        {
            AssertSpriteContract(FormalArtRegistry.Required(FormalArtRegistry.MapDecor, "academy_coastal"), new Vector2(1600, 900));
            foreach (FormalArtEntry entry in FormalArtRegistry.MapNodeMarkers) AssertSpriteContract(entry, new Vector2(32, 32));
        }

        [Test]
        public void AcademyMapNodes_ResolveAcrossAllSixVisualRegions()
        {
            string[] ids = RogueliteMapCatalog.Nodes.Select(FormalRogueliteUi.MapRegionId).ToArray();
            Assert.That(ids, Has.Length.EqualTo(40));
            Assert.That(ids.Distinct(StringComparer.Ordinal), Is.EquivalentTo(FormalArtRegistry.MapRegions.Select(entry => entry.RuntimeId)));
        }

        private static void AssertSpriteContract(FormalArtEntry entry, Vector2 expectedSize)
        {
            Sprite sprite = Resources.Load<Sprite>(entry.ResourcePath);
            Assert.That(sprite, Is.Not.Null, entry.AssetId);
            Assert.That(sprite.rect.size, Is.EqualTo(expectedSize), entry.AssetId);
            TextureImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite)) as TextureImporter;
            Assert.That(importer, Is.Not.Null, entry.AssetId);
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point), entry.AssetId);
            Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp), entry.AssetId);
            Assert.That(importer.mipmapEnabled, Is.False, entry.AssetId);
            Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(32f), entry.AssetId);
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
            Assert.That(FormalArtRegistry.IntentPath("move"), Is.EqualTo(FormalArtRegistry.CommandPath("move")),
                "movement intent must use the approved arrow instead of the ambiguous legacy footprint pixels");
        }

        [Test]
        public void CombatCoreSemanticIcons_UseNative16And32PixelContracts()
        {
            AssertCombatSemanticGroup(FormalArtRegistry.Commands, 16);
            AssertCombatSemanticGroup(FormalArtRegistry.Intents, 16);
            AssertCombatSemanticGroup(FormalArtRegistry.Statuses, 32);
            AssertCombatSemanticGroup(FormalArtRegistry.Feedback, 32);
        }

        private static void AssertCombatSemanticGroup(IEnumerable<FormalArtEntry> entries, int nativeSize)
        {
            foreach (FormalArtEntry entry in entries)
            {
                Sprite sprite = Resources.Load<Sprite>(entry.ResourcePath);
                Assert.That(sprite, Is.Not.Null, entry.AssetId);
                Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(nativeSize, nativeSize)), entry.AssetId);
                TextureImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite)) as TextureImporter;
                Assert.That(importer, Is.Not.Null, entry.AssetId);
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point), entry.AssetId);
                Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp), entry.AssetId);
                Assert.That(importer.mipmapEnabled, Is.False, entry.AssetId);
                Assert.That(importer.spritePixelsPerUnit, Is.EqualTo((float)nativeSize), entry.AssetId);
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
            Assert.That(OccPixelUiConfig.Layout("combat.commands").width, Is.EqualTo(1888));
            Assert.That(OccPixelUiConfig.StateSkin("button", "selected"), Is.EqualTo("tab_active"));
        }

        [Test]
        public void PeripheralUi_ConfigAndPixelAssetsAreComplete()
        {
            Assert.That(FormalUiEffectsConfig.Validate(), Is.Empty, string.Join("\n", FormalUiEffectsConfig.Validate()));
            OccPeripheralUiData config = FormalUiEffectsConfig.Data;
            Assert.That(config.backdrops.Select(entry => entry.id), Is.SupersetOf(new[] { "startup", "landing", "map", "briefing", "inventory", "settlement", "archive", "settings" }));
            Assert.That(config.decorations.Select(entry => entry.id), Is.EquivalentTo(new[] { "binding_spine", "index_tab", "measure_ruler", "corner_clasp", "folded_corner", "status_clip" }));
            Assert.That(config.illustrations.Select(entry => entry.id), Is.EquivalentTo(new[] { "empty_archive_tray", "empty_inventory_pouch", "empty_route_case", "empty_reward_crate", "empty_loadout_rack", "locked_document_satchel" }));
            Assert.That(config.chapterDividers.Select(entry => entry.id), Is.EquivalentTo(new[] { "teaching_record", "workshop_record", "infirmary_record", "field_survey", "sealed_dossier" }));
            Assert.That(config.chapterMarkers.Select(entry => entry.id), Is.EquivalentTo(new[] { "teaching_chalk_clip", "workshop_caliper_clip", "infirmary_bandage_clip", "field_leaf_clip", "sealed_red_clip", "reward_brass_tag" }));
            Assert.That(config.feedback.Select(entry => entry.id), Is.EquivalentTo(new[] { "click", "success", "rejected" }));
            foreach (string path in new[] { config.startupBackdrop, config.scanlineSprite, config.transitionSprite })
                Assert.That(Resources.Load<Sprite>(path), Is.Not.Null, path);
            foreach (OccPeripheralAssetEntry entry in config.backdrops)
                Assert.That(Resources.Load<Sprite>(entry.resourcePath), Is.Not.Null, entry.id);
            foreach (OccPeripheralAssetEntry entry in config.decorations)
                Assert.That(Resources.Load<Sprite>(entry.resourcePath), Is.Not.Null, entry.id);
            foreach (OccPeripheralAssetEntry entry in config.illustrations)
                Assert.That(Resources.Load<Sprite>(entry.resourcePath), Is.Not.Null, entry.id);
            foreach (OccPeripheralAssetEntry entry in config.chapterDividers)
                Assert.That(Resources.Load<Sprite>(entry.resourcePath), Is.Not.Null, entry.id);
            foreach (OccPeripheralAssetEntry entry in config.chapterMarkers)
                Assert.That(Resources.Load<Sprite>(entry.resourcePath), Is.Not.Null, entry.id);
            foreach (OccPeripheralFeedbackEntry entry in config.feedback)
            for (int frame = 0; frame < entry.frameCount; frame++)
                Assert.That(Resources.Load<Sprite>(entry.resourcePath + "/frame_" + frame.ToString("00")), Is.Not.Null, entry.id + "/" + frame);

            string[] paths = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Game/Resources/Art/FormalUIBackdrops", "Assets/Game/Resources/Art/FormalUIFeedback", "Assets/Game/Resources/Art/FormalUITrims", "Assets/Game/Resources/Art/FormalUIEmptyIllustrations", "Assets/Game/Resources/Art/FormalUIChapterDividers", "Assets/Game/Resources/Art/FormalUIChapterMarkers" })
                .Select(AssetDatabase.GUIDToAssetPath).ToArray();
            Assert.That(paths, Has.Length.EqualTo(51));
            foreach (string path in paths)
            {
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.That(importer, Is.Not.Null, path);
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point), path);
                Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp), path);
                Assert.That(importer.mipmapEnabled, Is.False, path);
            }
            foreach (string id in new[] { "startup", "landing", "map", "briefing", "inventory", "settlement", "archive", "settings" })
            {
                Sprite sprite = Resources.Load<Sprite>(FormalUiEffectsConfig.BackdropPath(id));
                Assert.That(sprite.texture.width, Is.EqualTo(480), id);
                Assert.That(sprite.texture.height, Is.EqualTo(270), id);
            }
            var decorationSizes = new Dictionary<string, Vector2Int>
            {
                { "binding_spine", new Vector2Int(32, 64) }, { "index_tab", new Vector2Int(64, 32) },
                { "measure_ruler", new Vector2Int(64, 32) }, { "corner_clasp", new Vector2Int(32, 32) },
                { "folded_corner", new Vector2Int(64, 64) }, { "status_clip", new Vector2Int(32, 32) }
            };
            foreach (KeyValuePair<string, Vector2Int> pair in decorationSizes)
            {
                Sprite sprite = Resources.Load<Sprite>(FormalUiEffectsConfig.DecorationPath(pair.Key));
                Assert.That(sprite.texture.width, Is.EqualTo(pair.Value.x), pair.Key);
                Assert.That(sprite.texture.height, Is.EqualTo(pair.Value.y), pair.Key);
            }
            foreach (OccPeripheralAssetEntry entry in config.illustrations)
            {
                Sprite sprite = Resources.Load<Sprite>(entry.resourcePath);
                Assert.That(sprite.texture.width, Is.EqualTo(64), entry.id);
                Assert.That(sprite.texture.height, Is.EqualTo(64), entry.id);
            }
            foreach (OccPeripheralAssetEntry entry in config.chapterDividers)
            {
                Sprite sprite = Resources.Load<Sprite>(entry.resourcePath);
                Assert.That(sprite.texture.width, Is.EqualTo(128), entry.id);
                Assert.That(sprite.texture.height, Is.EqualTo(32), entry.id);
            }
            foreach (OccPeripheralAssetEntry entry in config.chapterMarkers)
            {
                Sprite sprite = Resources.Load<Sprite>(entry.resourcePath);
                Assert.That(sprite.texture.width, Is.EqualTo(32), entry.id);
                Assert.That(sprite.texture.height, Is.EqualTo(32), entry.id);
            }
        }
    }
}
