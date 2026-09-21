using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    /// <summary>
    /// Owns formal battlefield asset lookup, caching, and editor-preview construction.
    /// </summary>
    public sealed class CombatFormalVisualAssets
    {
        private readonly Dictionary<string, Texture2D> units = new Dictionary<string, Texture2D>();
        private readonly Dictionary<string, Texture2D[]> enemyAnimations = new Dictionary<string, Texture2D[]>();
        private readonly Dictionary<string, Texture2D> academy = new Dictionary<string, Texture2D>();
        private readonly Dictionary<string, Texture2D> environments = new Dictionary<string, Texture2D>();
        private readonly Dictionary<string, Texture2D> relay = new Dictionary<string, Texture2D>();
        private readonly Dictionary<string, Texture2D> overlays = new Dictionary<string, Texture2D>();
        private readonly Dictionary<string, Texture2D> intents = new Dictionary<string, Texture2D>();
        private readonly Dictionary<string, Texture2D> testArenaUnits = new Dictionary<string, Texture2D>();
        private readonly Dictionary<StatusType, Texture2D> statuses = new Dictionary<StatusType, Texture2D>();
        private readonly Texture2D[] firegroundFrames = new Texture2D[6];
        private readonly Texture2D[] smokeFrames = new Texture2D[6];

        public int EnvironmentFrameCount => firegroundFrames.Length;
        public Texture2D LootClosed => Relay("loot_crate_closed");
        public Texture2D Academy(string id) => academy[id];
        public Texture2D Environment(string id) => environments[id];
        public Texture2D Relay(string id) => relay[id];
        public Texture2D Overlay(string id) => overlays[id];
        public Texture2D Intent(string id) => !string.IsNullOrEmpty(id) && intents.TryGetValue(id, out Texture2D value) ? value : null;
        public Texture2D Status(StatusType status) => statuses.TryGetValue(status, out Texture2D value) ? value : null;
        public Texture2D FiregroundFrame(int frame) => firegroundFrames[frame];
        public Texture2D SmokeFrame(int frame) => smokeFrames[frame];

        public Texture2D Unit(UnitState unit, int animationFrame = -1)
        {
            if (unit == null) return null;
            if (CombatTestArenaEntry.IsDedicatedTestArena)
            {
                if (unit.IsHero && testArenaUnits.TryGetValue("hero", out Texture2D testHero)) return testHero;
                if (!string.IsNullOrEmpty(unit.EnemyArchetypeId) && testArenaUnits.TryGetValue(unit.EnemyArchetypeId, out Texture2D testUnit)) return testUnit;
                if (!unit.IsHero && testArenaUnits.TryGetValue("raider", out Texture2D testFallback)) return testFallback;
            }
            if (unit.IsHero) return TextureFor("hero");
            if (string.IsNullOrEmpty(unit.EnemyArchetypeId)) return null;
            if (animationFrame >= 0 && enemyAnimations.TryGetValue(unit.EnemyArchetypeId, out Texture2D[] frames))
                return frames[Mathf.Clamp(animationFrame, 0, frames.Length - 1)];
            Texture2D texture = TextureFor(unit.EnemyArchetypeId);
            if (texture != null) return texture;
            string artId = EnemyArchetypes.Get(unit.EnemyArchetypeId).ArtId;
            if (animationFrame >= 0 && enemyAnimations.TryGetValue(artId, out frames))
                return frames[Mathf.Clamp(animationFrame, 0, frames.Length - 1)];
            return TextureFor(artId);
        }

        public void LoadRuntime()
        {
            LoadUnits();
            if (CombatTestArenaEntry.IsDedicatedTestArena)
            {
                testArenaUnits["hero"] = LoadOptionalTexture("Art/CombatTestArenaSephiriaDebug/sephiria_unit_side_post_outline_64");
                testArenaUnits["raider"] = LoadOptionalTexture("Art/CombatTestArenaGround/academy_test_raider_pixel_candidate_64");
                testArenaUnits["shieldguard"] = LoadOptionalTexture("Art/CombatTestArenaGround/academy_test_shieldguard_pixel_candidate_64");
                testArenaUnits["pyromancer"] = LoadOptionalTexture("Art/CombatTestArenaGround/academy_test_pyromancer_pixel_candidate_64");
                testArenaUnits["elite_vanguard"] = LoadOptionalTexture("Art/CombatTestArenaGround/academy_test_elite_vanguard_pixel_candidate_64");
            }
            LoadAcademy();
            foreach (FormalArtEntry entry in FormalArtRegistry.Environments)
                environments[entry.RuntimeId] = RequiredTexture(entry.ResourcePath);
            string[] relayIds = { "floor_plain", "floor_industrial", "floor_warning", "floor_hazard", "rail_horizontal", "rail_vertical",
                "light_cover_intact", "light_cover_damaged", "light_cover_rubble", "heavy_cover_intact", "heavy_cover_damaged", "heavy_cover_rubble",
                "relay_intact", "relay_damaged", "relay_rubble", "loot_crate_closed", "loot_crate_open", "loot_crate_empty" };
            foreach (string id in relayIds) relay[id] = RequiredTexture("Art/FormalRelayV01/" + id);
            foreach (string id in new[] { "selected", "move_range", "attack_range", "objective", "high_risk", "unreachable", "line_of_sight" })
            {
                string family = id == "move_range" || id == "attack_range"
                    ? "Art/FormalTacticalOverlays32V2/"
                    : "Art/FormalTacticalOverlays32/";
                overlays[id] = RequiredTexture(family + id);
            }
            foreach (FormalArtEntry entry in FormalArtRegistry.Intents)
                intents[entry.RuntimeId] = RequiredTexture(entry.ResourcePath);
            statuses[StatusType.Burning] = RequiredTexture(FormalArtRegistry.StatusPath("burning"));
            statuses[StatusType.Slow] = RequiredTexture(FormalArtRegistry.StatusPath("slow"));
            statuses[StatusType.Bound] = RequiredTexture(FormalArtRegistry.StatusPath("bound"));
            statuses[StatusType.ArmorBreak] = RequiredTexture(FormalArtRegistry.StatusPath("armor_break"));
            statuses[StatusType.BreakStance] = statuses[StatusType.ArmorBreak];
            statuses[StatusType.FiregroundBoost] = statuses[StatusType.Burning];
            statuses[StatusType.FiregroundVulnerable] = statuses[StatusType.Burning];
            statuses[StatusType.Dazzled] = RequiredTexture(FormalArtRegistry.StatusPath("dazzled"));
            statuses[StatusType.Revealed] = RequiredTexture(FormalArtRegistry.StatusPath("revealed"));
            for (int frame = 0; frame < firegroundFrames.Length; frame++)
            {
                firegroundFrames[frame] = RequiredTexture($"Art/FormalVfx32/fire_burning_ground/frame_{frame:00}");
                smokeFrames[frame] = RequiredTexture($"Art/FormalVfx32/fire_smoke/frame_{frame:00}");
            }
        }

        private void LoadUnits()
        {
            units["hero"] = RequiredTexture(FormalArtRegistry.UnitPath("hero"));
            string[] requiredEnemyIds = { "sigil_mauler", "barrier_mender", "tether_hound", "shieldguard", "pyromancer", "raider",
                "elite_vanguard", "stone_snare", "lantern_revealer", "rune_arbalist", "core_overseer"
            };
            foreach (string id in requiredEnemyIds)
                units[EnemyArchetypes.Get(id).ArtId] = RequiredTexture(FormalArtRegistry.UnitPath(id));
            foreach (string id in requiredEnemyIds)
            {
                enemyAnimations[id] = new[]
                {
                    RequiredTexture($"Art/FormalEnemyAnimations64/{id}/frame_00"),
                    RequiredTexture($"Art/FormalEnemyAnimations64/{id}/frame_05")
                };
            }
        }

        private void LoadAcademy()
        {
            string[] ids =
            {
                "academy_stone_road_a", "academy_stone_road_b", "academy_stone_road_c", "academy_stone_road_d",
                "academy_courtyard_a", "academy_courtyard_b", "academy_courtyard_c", "academy_courtyard_d",
                "academy_ruins_a", "academy_ruins_b", "academy_ruins_c", "academy_ruins_d",
                "academy_aether_inlay_a", "academy_aether_inlay_b", "academy_aether_inlay_c", "academy_aether_inlay_d",
                "academy_packed_earth_a", "academy_packed_earth_b", "academy_packed_earth_c",
                "academy_grass_edge_n", "academy_grass_edge_e", "academy_grass_edge_s", "academy_grass_edge_w",
                "academy_light_stone_bench_intact", "academy_light_stone_bench_damaged", "academy_light_stone_bench_rubble",
                "academy_light_planter_intact", "academy_light_planter_damaged", "academy_light_planter_rubble",
                "academy_heavy_archive_stack_intact", "academy_heavy_archive_stack_damaged", "academy_heavy_archive_stack_rubble",
                "academy_heavy_masonry_screen_intact", "academy_heavy_masonry_screen_damaged", "academy_heavy_masonry_screen_rubble",
                "academy_aether_pillar_intact", "academy_aether_pillar_damaged", "academy_aether_pillar_rubble",
                "academy_aether_crystal_intact", "academy_aether_crystal_damaged",
                "academy_pressure_crystal_intact", "academy_pressure_crystal_damaged", "academy_scorched_lamp_vine",
                "academy_decoy_lantern_active",
                "academy_seal_plinth_intact", "academy_seal_plinth_damaged", "academy_seal_plinth_rubble",
                "academy_loot_chest_closed", "academy_loot_chest_open", "academy_loot_chest_empty",
                "academy_aether_line_straight", "academy_aether_line_corner", "academy_aether_line_tee", "academy_aether_line_cross"
            };
            foreach (string id in ids) academy[id] = RequiredTexture("Art/FormalAcademyCombat32/" + id);
            foreach (string family in new[] { "court", "road", "ruin", "earth" })
            for (char variant = 'a'; variant <= 'd'; variant++)
            {
                string id = $"academy_block_{family}_{variant}";
                academy[id] = RequiredTexture("Art/FormalAcademyIndependentFloors32/" + id);
            }
            foreach (string id in new[] { "academy_tactical_road_edge", "academy_tactical_road_corner", "academy_tactical_road_end" })
                academy[id] = RequiredTexture("Art/FormalAcademyCombat32/" + id);
            foreach (string family in new[] { "court", "road", "ruin", "earth" })
            {
                foreach (string variant in new[] { "", "_b" })
                {
                    string id = $"academy_ground_macro_{family}{variant}_3x3";
                    academy[id] = RequiredTexture("Art/FormalAcademyGroundMacros32/" + id);
                }
            }
            foreach (string id in new[] { "academy_curb_edge", "academy_curb_corner", "academy_curb_opposite",
                         "academy_curb_three", "academy_curb_enclosed" })
                academy[id] = RequiredTexture("Art/FormalAcademyTerrainOverlays32/" + id);
            // Dedicated CombatTestArena ground is a set of independently replaceable
            // theme modules. Each theme has one interior surface and one south exposed
            // edge, plus an explicit 32px companion for the low-resolution viewport.
            foreach (string theme in new[] { "slate", "earth" })
            foreach (string part in new[] { "surface", "edge_s" })
            foreach (string resolution in new[] { "64", "32" })
            {
                string id = $"academy_test_ground_theme_{theme}_{part}_{resolution}";
                academy[id] = RequiredTexture("Art/CombatTestArenaGround/" + id);
            }
            foreach (string resolution in new[] { "64", "32" })
            {
                string id = "academy_test_heavy_cover_intact_" + resolution;
                academy[id] = RequiredTexture("Art/CombatTestArenaSephiriaDebug/sephiria_heavy_cover_post_outline_" + resolution);
                string lightId = "academy_test_book_crate_intact_" + resolution;
                academy[lightId] = RequiredTexture("Art/CombatTestArenaGround/" + lightId);
                foreach (char variant in new[] { 'a', 'b', 'c', 'd' })
                {
                    string courtId = "academy_test_ground_tile_court_" + variant + "_" + resolution;
                    academy[courtId] = RequiredTexture("Art/CombatTestArenaGround/" + courtId);
                }
            }
            academy["academy_test_ground_theme_slate_surface_64"] = RequiredTexture("Art/CombatTestArenaSephiriaDebug/sephiria_ground_64");
            academy["academy_test_ground_theme_slate_surface_32"] = RequiredTexture("Art/CombatTestArenaSephiriaDebug/sephiria_ground_32");
            academy["academy_wall_straight"] = RequiredTexture("Art/FormalAcademyStructures32/academy_wall_straight");
            // Temporary placeholder for indestructible terrain (the planned role is
            // permanent_wall_32x32: a full-cell pillar block). Program-drawn, so it lives
            // outside the formal art folders and must be replaced by the image-generation
            // pipeline result before it can be called FORMAL.
            academy["academy_permanent_pillar_placeholder"] = RequiredTexture("Art/Placeholder/academy_permanent_pillar_placeholder");
            foreach (string id in AcademyBattlefieldLayoutCatalog.CoverVisualAssetIds())
                academy[id] = RequiredTexture("Art/FormalAcademyStructures32/" + id);
        }

        private Texture2D TextureFor(string id) => units.TryGetValue(id, out Texture2D value) ? value : null;

        private static Texture2D LoadOptionalTexture(string path)
        {
            Texture2D texture = Resources.Load<Texture2D>(path);
            if (texture == null) return null;
            Configure(texture);
            return texture;
        }

        private static Texture2D RequiredTexture(string path) =>
            LoadOptionalTexture(path) ?? throw new KeyNotFoundException("Missing formal texture: " + path);

        private static void Configure(Texture2D texture)
        {
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
        }

        private static void AddUiPanel(GameObject parent, string name, Vector2 position, Vector2 size,
            string text, int fontSize)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent.transform, false);
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = panel.AddComponent<Image>();
            image.color = new Color(.07f, .13f, .22f, .72f);
            GameObject textObject = new GameObject(name + "文字");
            textObject.transform.SetParent(panel.transform, false);
            RectTransform textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            Text label = textObject.AddComponent<Text>();
            label.text = text;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.fontSize = FormalUiTheme.ResponsiveFontSize(fontSize);
            label.font = FormalUiKit.Font;
            label.fontStyle = FontStyle.Normal;
            label.resizeTextForBestFit = false;
        }

        private static void AddEditorMarker(GameObject root, string name, Vector3 position, string formalAsset)
        {
            GameObject marker = new GameObject(name);
            marker.transform.SetParent(root.transform, false);
            marker.transform.position = position;
            marker.transform.localScale = Vector3.one * .96f;
            SpriteRenderer renderer = marker.AddComponent<SpriteRenderer>();
            renderer.sprite = LoadFormalSprite(formalAsset) ?? CreateEditorSprite();
            renderer.color = Color.white;
            renderer.sortingOrder = -5;
        }

        private static Sprite LoadFormalSprite(string name)
        {
            Texture2D texture = LoadOptionalTexture("Art/FormalRelay32/" + name);
            return texture == null ? null : Sprite.Create(texture,
                new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 32f);
        }

        private static Sprite RelayFloorSprite(string objectName, Sprite floor, Sprite railFloor, Sprite warningFloor)
        {
            string[] parts = objectName.Split('_');
            if (parts.Length != 3 || !int.TryParse(parts[1], out int x) ||
                !int.TryParse(parts[2], out int y)) return floor;
            if (y == 0 || y == 8) return railFloor;
            if ((x == 5 || x == 6) && y >= 3 && y <= 5) return warningFloor;
            return floor;
        }

        private static Sprite CreateEditorSprite()
        {
            Texture2D texture = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1f);
        }
    }
}
