using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    [ExecuteAlways]
    public sealed class CombatPrototypeBootstrap : MonoBehaviour, ICombatPresentationCompositionHost, ITacticalHudHost
    {
        private const float UiWidth = 1920f;
        private const float UiHeight = 1080f;
        private readonly BattlefieldPresentationAdapter battlefield = new BattlefieldPresentationAdapter();
        private BattlefieldViewport battlefieldViewport;
        private readonly CombatAvailabilityQuery availability = new CombatAvailabilityQuery();
        private readonly EnemyTurnPlanBook enemyPlans = new EnemyTurnPlanBook();
        private readonly EnemyTurnCoordinator enemyTurn = new EnemyTurnCoordinator();
        private readonly CombatCommandExecutionService commandExecution = new CombatCommandExecutionService();
        private readonly CombatTargetNavigationState targetNavigation = new CombatTargetNavigationState();
        private CombatState state;
        private FirstRegionLevelDefinition currentLevel;
        // Legacy panel helpers still use this editor-only snapshot; active flow restarts use developerFlow.
        private CombatState snapshot;
        private string selectedAction = "\u79fb\u52a8";
        private string selectedTargetId;
        private Font chineseFont;
        private Texture2D barTexture;
        private bool initialized;
        private MissionPreparation developerPreparation;
        private CombatFlowController developerFlow;
        private readonly RogueliteFlowCoordinator rogueliteFlow = new RogueliteFlowCoordinator();
        private RogueliteDeveloperRun rogueliteRun { get => rogueliteFlow.DeveloperRun; set => rogueliteFlow.SetDeveloperRun(value); }
        private int sandboxTemplateIndex;
        private bool rogueliteMenuOpen { get => rogueliteFlow.IsRogueliteMenuOpen; set => rogueliteFlow.SetRogueliteMenuOpen(value); }
        private bool outcomeHandled;
        private RogueliteMapRun mapRun { get => rogueliteFlow.MapRun; set => rogueliteFlow.SetMapRun(value); }
        private bool mapMenuOpen { get => rogueliteFlow.IsMapMenuOpen; set => rogueliteFlow.SetMapMenuOpen(value); }
        private readonly Dictionary<string, Texture2D> formalUnitTextures = new Dictionary<string, Texture2D>();
        private Texture2D formalLootTexture;
        private Texture2D formalLootOpenTexture;
        private readonly Dictionary<string, Texture2D> formalRelayTextures = new Dictionary<string, Texture2D>();
        private readonly Dictionary<string, Texture2D> formalOverlayTextures = new Dictionary<string, Texture2D>();
        private readonly Dictionary<string, Texture2D> formalIntentTextures = new Dictionary<string, Texture2D>();
        private readonly Dictionary<StatusType, Texture2D> formalStatusTextures = new Dictionary<StatusType, Texture2D>();
        private readonly Texture2D[] formalFiregroundFrames = new Texture2D[6];
        private readonly Texture2D[] formalSmokeFrames = new Texture2D[6];
        private readonly RogueliteSaveGateway saveGateway = new RogueliteSaveGateway(new PlayerPrefsRogueliteSaveStore());
        private CombatPresentationComposition presentation;
        private CombatVisualFeedback visualFeedback => presentation?.Feedback;
        private RogueliteSettlementPresentation settlementPresentation => presentation?.Settlement;
        private FormalUiInteractionLayer interactionLayer => presentation?.Interaction;
        private FormalStartupPresentation startupPresentation => presentation?.Startup;
        private DeveloperConsolePanel developerConsole => presentation?.DeveloperConsole;
        private TarkovInventoryPanel inventoryPanel => presentation?.Inventory;
        private FireBattleState fireBattle;
        private ArtifactBattleState artifactBattle;
        private string fireLifecycleActiveUnitId;
        private TrainingRangeSession trainingRangeSession;
        private bool trainingRangeActive;
        private int trainingRangeArtifactUsesRemaining;
        private string armedInventoryItemId;
        private RogueliteUiPreferences uiPreferences = new RogueliteUiPreferences();
        private bool lastMapSaveSucceeded = true;
        private bool lastSettingsSaveSucceeded = true;
        private readonly UiVisualEventStream uiVisualEvents = new UiVisualEventStream();
        private readonly UiPresentationVersions uiPresentationVersions = new UiPresentationVersions();

        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            if (initialized) return;
            initialized = true;
            chineseFont = FormalUiKit.Font;
            barTexture = Resources.Load<Texture2D>("UI/Bar");
            uiPreferences = saveGateway.LoadUiPreferences();
            ApplyUiPreferences();
            Transform sceneUi = transform.Find("场景UI");
            if (sceneUi != null) sceneUi.gameObject.SetActive(false);
            GameObject editorMap = GameObject.Find("地图可视化");
            if (editorMap != null) editorMap.SetActive(false);
            developerPreparation = new MissionPreparation().Configure("relay_test", "破坏任务目标并清理威胁", "盾卫、火术师、突袭者、刻印锤手、缚环猎兽");
            presentation = CombatPresentationComposition.Attach(gameObject, this);
            BuildCombatFromSceneStageTwo();
            ApplyFormalRelayVisuals();
            LoadFormalUnitTextures();
            LoadFormalBattlefieldTextures();
        }

        private void Awake()
        {
            if (!Application.isPlaying) return;
            Application.targetFrameRate = 60;
            Camera sceneCamera = FindAnyObjectByType<Camera>();
            if (sceneCamera != null)
            {
                if (sceneCamera.CompareTag("Untagged")) sceneCamera.tag = "MainCamera";
                sceneCamera.clearFlags = CameraClearFlags.SolidColor;
                sceneCamera.backgroundColor = new Color(.012f, .018f, .025f, 1f);
            }
        }

        public void EnsureEditorVisuals()
        {
            EnsureEditorMapVisuals();
            EnsureEditorUiVisuals();
        }

        public void EnsureEditorMapVisuals()
        {
            if (transform.Find("地图可视化") != null) return;
            GameObject root = new GameObject("地图可视化"); root.transform.SetParent(transform, false);
            Sprite floorSprite = LoadFormalSprite("floor") ?? CreateEditorSprite();
            for (int y = 0; y < 9; y++) for (int x = 0; x < 12; x++)
            {
                GameObject tile = new GameObject("格_" + x + "_" + y); tile.transform.SetParent(root.transform, false); tile.transform.position = new Vector3(x, y, 2f); tile.transform.localScale = Vector3.one * .96f;
                SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>(); renderer.sprite = floorSprite; renderer.color = Color.white; renderer.sortingOrder = -10;
            }
            AddEditorMarker(root, "轻掩体_A", new Vector3(4, 2, 1), "light_cover");
            AddEditorMarker(root, "轻掩体_B", new Vector3(6, 5, 1), "light_cover");
            AddEditorMarker(root, "重掩体_A", new Vector3(7, 3, 1), "heavy_cover");
            AddEditorMarker(root, "重掩体_B", new Vector3(8, 6, 1), "heavy_cover");
            AddEditorMarker(root, "目标_中继器", new Vector3(10, 4, 1), "relay");
        }

        public void EnsureEditorUiVisuals()
        {
            if (transform.Find("场景UI") != null) return;
            GameObject canvasObject = new GameObject("场景UI"); canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 20;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); canvasObject.AddComponent<GraphicRaycaster>();
            AddUiPanel(canvasObject, "标题栏", new Vector2(16, -16), new Vector2(640, 44), "OCC \u6218\u6597\u539f\u578b", 18);
            AddUiPanel(canvasObject, "战斗UI面板占位", new Vector2(658, -74), new Vector2(310, 560), "\u6218\u6597\u4fe1\u606f\u7531\u6218\u6597\u7ba1\u7406\u5668\u66f4\u65b0", 14);
        }

        private static void AddUiPanel(GameObject parent, string name, Vector2 position, Vector2 size, string text, int fontSize)
        {
            GameObject panel = new GameObject(name); panel.transform.SetParent(parent.transform, false); RectTransform rect = panel.AddComponent<RectTransform>(); rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1); rect.anchoredPosition = position; rect.sizeDelta = size; Image image = panel.AddComponent<Image>(); image.color = new Color(.07f, .13f, .22f, .72f);
            GameObject textObject = new GameObject(name + "文字"); textObject.transform.SetParent(panel.transform, false); RectTransform textRect = textObject.AddComponent<RectTransform>(); textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one; textRect.offsetMin = Vector2.zero; textRect.offsetMax = Vector2.zero; Text label = textObject.AddComponent<Text>(); label.text = text; label.alignment = TextAnchor.MiddleCenter; label.color = Color.white; label.fontSize = fontSize; label.font = FormalUiKit.Font;
        }

        private static void AddEditorMarker(GameObject root, string name, Vector3 position, string formalAsset)
        {
            GameObject marker = new GameObject(name); marker.transform.SetParent(root.transform, false); marker.transform.position = position; marker.transform.localScale = Vector3.one * .96f; SpriteRenderer renderer = marker.AddComponent<SpriteRenderer>(); renderer.sprite = LoadFormalSprite(formalAsset) ?? CreateEditorSprite(); renderer.color = Color.white; renderer.sortingOrder = -5;
        }

        private static Sprite LoadFormalSprite(string name)
        {
            Texture2D texture = Resources.Load<Texture2D>("Art/FormalRelay32/" + name);
            if (texture == null) return null;
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 32f);
        }

        private void ApplyFormalRelayVisuals()
        {
            Transform root = transform.Find("地图可视化");
            if (root == null) return;
            Sprite floor = LoadFormalSprite("floor_industrial") ?? LoadFormalSprite("floor");
            Sprite railFloor = LoadFormalSprite("floor_rail") ?? floor;
            Sprite warningFloor = LoadFormalSprite("floor_warning") ?? floor;
            Sprite light = LoadFormalSprite("light_cover");
            Sprite heavy = LoadFormalSprite("heavy_cover");
            Sprite relay = LoadFormalSprite("relay");
            foreach (SpriteRenderer renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                string objectName = renderer.gameObject.name;
                Sprite replacement = objectName.StartsWith("格_") ? RelayFloorSprite(objectName, floor, railFloor, warningFloor) :
                    objectName.StartsWith("轻掩体") ? light :
                    objectName.StartsWith("重掩体") ? heavy :
                    objectName.StartsWith("目标_中继器") ? relay : null;
                if (replacement == null) continue;
                renderer.sprite = replacement;
                renderer.color = Color.white;
            }
        }

        private static Sprite RelayFloorSprite(string objectName, Sprite floor, Sprite railFloor, Sprite warningFloor)
        {
            string[] parts = objectName.Split('_');
            if (parts.Length != 3 || !int.TryParse(parts[1], out int x) || !int.TryParse(parts[2], out int y)) return floor;
            if (y == 0 || y == 8) return railFloor;
            if ((x == 5 || x == 6) && y >= 3 && y <= 5) return warningFloor;
            return floor;
        }

        private void LoadFormalUnitTextures()
        {
            foreach (FormalArtEntry entry in FormalArtRegistry.Units)
            {
                Texture2D texture = Resources.Load<Texture2D>(entry.ResourcePath);
                if (texture == null) continue;
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                formalUnitTextures[entry.RuntimeId] = texture;
            }
            foreach (string artId in EnemyArchetypes.All.Select(archetype => archetype.ArtId).Distinct(StringComparer.Ordinal))
            {
                Texture2D texture = Resources.Load<Texture2D>("Art/FormalUnits64/" + artId);
                if (texture == null) continue;
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                formalUnitTextures[artId] = texture;
            }
        }

        private void LoadFormalBattlefieldTextures()
        {
            string[] relay = { "floor_plain", "floor_industrial", "floor_warning", "floor_hazard", "rail_horizontal", "rail_vertical",
                "light_cover_intact", "light_cover_damaged", "light_cover_rubble", "heavy_cover_intact", "heavy_cover_damaged", "heavy_cover_rubble",
                "relay_intact", "relay_damaged", "relay_rubble", "loot_crate_closed", "loot_crate_open", "loot_crate_empty" };
            foreach (string name in relay) formalRelayTextures[name] = RequiredTexture("Art/FormalRelayV01/" + name);
            foreach (string name in new[] { "selected", "move_range", "attack_range", "objective", "high_risk", "unreachable", "line_of_sight" })
                formalOverlayTextures[name] = RequiredTexture("Art/FormalTacticalOverlays32/" + name);
            foreach (FormalArtEntry entry in FormalArtRegistry.Intents)
                formalIntentTextures[entry.RuntimeId] = RequiredTexture(entry.ResourcePath);
            formalStatusTextures[StatusType.Burning] = RequiredTexture(FormalArtRegistry.StatusPath("burning"));
            formalStatusTextures[StatusType.Slow] = RequiredTexture(FormalArtRegistry.StatusPath("slow"));
            formalStatusTextures[StatusType.Bound] = RequiredTexture(FormalArtRegistry.StatusPath("bound"));
            formalStatusTextures[StatusType.ArmorBreak] = RequiredTexture(FormalArtRegistry.StatusPath("armor_break"));
            formalStatusTextures[StatusType.Dazzled] = RequiredTexture(FormalArtRegistry.StatusPath("dazzled"));
            formalStatusTextures[StatusType.Revealed] = RequiredTexture(FormalArtRegistry.StatusPath("revealed"));
            for (int frame = 0; frame < 6; frame++)
            {
                formalFiregroundFrames[frame] = RequiredTexture($"Art/FormalVfx32/fire_burning_ground/frame_{frame:00}");
                formalSmokeFrames[frame] = RequiredTexture($"Art/FormalVfx32/fire_smoke/frame_{frame:00}");
            }
            formalLootTexture = formalRelayTextures["loot_crate_closed"];
            formalLootOpenTexture = formalRelayTextures["loot_crate_open"];
        }

        private static Texture2D RequiredTexture(string path)
        {
            Texture2D texture = Resources.Load<Texture2D>(path);
            if (texture == null) throw new KeyNotFoundException("Missing formal texture: " + path);
            texture.filterMode = FilterMode.Point; texture.wrapMode = TextureWrapMode.Clamp;
            return texture;
        }

        private Texture2D FormalUnitTexture(UnitState unit)
        {
            if (unit == null) return null;
            if (unit.IsHero) return TextureFor("hero");
            if (string.IsNullOrEmpty(unit.EnemyArchetypeId)) return null;
            Texture2D texture = TextureFor(unit.EnemyArchetypeId);
            return texture != null ? texture : TextureFor(EnemyArchetypes.Get(unit.EnemyArchetypeId).ArtId);
        }

        private Texture2D TextureFor(string name)
        {
            formalUnitTextures.TryGetValue(name, out Texture2D texture);
            return texture;
        }

        private static Sprite CreateEditorSprite()
        {
            Texture2D texture = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave }; texture.SetPixel(0, 0, Color.white); texture.Apply(); return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1f);
        }

        private void BuildCombatFromScene()
        {
            currentLevel = null;
            GridMap map = new GridMap(12, 9); CombatSceneMarker[] markers = FindObjectsByType<CombatSceneMarker>();
            foreach (CombatSceneMarker marker in markers) { GridPosition p = ScenePosition(marker); if (marker.MarkerType == CombatSceneMarkerType.LightCover) map.SetTile(p, new TileState { Cover = CoverType.Light, Durability = 4 }); if (marker.MarkerType == CombatSceneMarkerType.HeavyCover) map.SetTile(p, new TileState { Cover = CoverType.Heavy, Durability = 7 }); if (marker.MarkerType == CombatSceneMarkerType.Objective) map.SetTile(p, new TileState { IsObjective = true, Durability = 6 }); }
            List<UnitState> units = new List<UnitState>();
            foreach (CombatSceneMarker marker in markers.Where(m => m.MarkerType == CombatSceneMarkerType.Unit)) { GridPosition p = ScenePosition(marker); bool hero = marker.name.Contains("主角"); string id = hero ? "hero" : marker.name.Contains("盾") ? "guard" : marker.name.Contains("火术") ? "caster" : "raider"; string name = hero ? "\u963f\u65af\u7279\u62c9" : id == "guard" ? "\u76fe\u536b" : id == "caster" ? "\u706b\u672f\u5e08" : "\u7a81\u88ad\u8005"; units.Add(new UnitState(id, hero, p, hero ? Facing.East : Facing.West) { DisplayName = name, Armor = hero ? 1 : id == "guard" ? 2 : 0, Block = id == "guard" ? 2 : hero ? 1 : 0, Speed = hero ? 11 : id == "guard" ? 7 : id == "caster" ? 9 : 8 }); }
            if (units.Count == 0) return;
            state = new CombatState(map, units);
            fireBattle = new FireBattleState(state);
            fireLifecycleActiveUnitId = null;
            if (mapRun != null)
            {
                RogueliteMissionDefinition mission = RogueliteDeveloperCatalog.FindMission(mapRun.HasPendingContentCombat ? mapRun.PendingContentCombatMissionId : mapRun.CurrentNodeId);
                developerPreparation = new MissionPreparation().Configure(mission.Id, mission.ObjectiveSummary, mission.EnemySummary);
                if (mission.ObjectiveType == CombatObjectiveType.Elimination) state.ConfigureObjectives(new EliminationObjective(mission.Id + "_objective"));
                else state.ConfigureObjectives(new DestructionObjective(map.PositionsWith(tile => tile.IsObjective), mission.Id + "_objective"));
            }
            else if (rogueliteRun != null)
            {
                RogueliteMissionDefinition mission = rogueliteRun.CurrentMission;
                developerPreparation = new MissionPreparation().Configure(mission.Id, mission.ObjectiveSummary, mission.EnemySummary);
                if (mission.ObjectiveType == CombatObjectiveType.Elimination) state.ConfigureObjectives(new EliminationObjective(mission.Id + "_objective"));
                else state.ConfigureObjectives(new DestructionObjective(map.PositionsWith(tile => tile.IsObjective), mission.Id + "_objective"));
            }
            ConfigureCombatInventory();
            ApplyShortRunChoices();
            mapRun?.ApplyBuild(state.GetUnit("hero"));
            state.SetLoot(new LootContainer(new GridPosition(2, 0), new InventoryItem("aether_core", "\u4ee5\u592a\u6838\u5fc3", 2, 1)));
            string lootKey = mapRun == null ? "relay-crate" : mapRun.CurrentNodeId + "-relay-crate";
            ArtifactDefinition lootArtifact = ArtifactRewardPool.RollLoot(mapRun?.Seed ?? 0, lootKey);
            state.SetLootSource(new LootSourceState(lootKey, new GridPosition(2, 0), new[]
            {
                new ItemInstance(lootKey + "-medkit", "medkit", 0),
                new ItemInstance(lootKey + "-scroll-F-S01", "F-S01", 1),
                new ItemInstance(lootKey + "-artifact-" + lootArtifact.Id, lootArtifact.Id, 2)
            }));
            mapRun?.RestoreLootProgress(state.LootSource);
            PublishCombatEffects(CombatResolver.BeginTurn(state, "hero"));
        }

        private void BuildCombatFromSceneStageTwo()
        {
            targetNavigation.End();
            string encounterId = mapRun != null
                ? (mapRun.HasPendingContentCombat ? mapRun.PendingContentCombatMissionId : mapRun.CurrentNodeId)
                : rogueliteRun?.CurrentMission.Id;
            RogueliteEncounterDefinition encounter = string.IsNullOrEmpty(encounterId) ? null : RogueliteEncounterCatalog.For(encounterId, mapRun?.RegionBossId);
            GridMap map;
            if (FirstRegionLevelCatalog.TryFor(encounterId, out FirstRegionLevelDefinition level))
            {
                FirstRegionLevelBuild build = FirstRegionLevelBuilder.Build(level, mapRun?.RegionBossId);
                currentLevel = build.Definition;
                state = build.State;
                map = state.Map;
            }
            else
            {
                currentLevel = null;
                map = new GridMap(12, 9);
                CombatSceneMarker[] markers = FindObjectsByType<CombatSceneMarker>();
                foreach (CombatSceneMarker marker in markers)
                {
                    GridPosition p = ScenePosition(marker);
                    if (marker.MarkerType == CombatSceneMarkerType.LightCover) map.SetTile(p, new TileState { Cover = CoverType.Light, Durability = 4 });
                    if (marker.MarkerType == CombatSceneMarkerType.HeavyCover) map.SetTile(p, new TileState { Cover = CoverType.Heavy, Durability = 7 });
                    if (marker.MarkerType == CombatSceneMarkerType.Objective) map.SetTile(p, new TileState { IsObjective = true, IsDevice = true, Durability = 6 });
                }
                List<UnitState> units = new List<UnitState>();
                int enemyIndex = 0;
                foreach (CombatSceneMarker marker in markers.Where(m => m.MarkerType == CombatSceneMarkerType.Unit).OrderBy(m => m.name, StringComparer.Ordinal))
                {
                    bool hero = marker.name.Contains("\u4e3b\u89d2");
                    if (!hero && encounter != null && enemyIndex >= encounter.EnemyArchetypeIds.Count) continue;
                    UnitState unit = new UnitState(hero ? "hero" : "enemy_" + enemyIndex, hero, ScenePosition(marker), hero ? Facing.East : Facing.West);
                    if (hero) { unit.DisplayName = "\u963f\u65af\u7279\u62c9"; unit.Speed = 11; unit.Equip(CombatCatalog.Hammer, CombatCatalog.Shield, CombatCatalog.FireBolt, CombatCatalog.FrostBind); }
                    else
                    {
                        string archetypeId = encounter == null ? EnemyArchetypes.All[Math.Min(enemyIndex, EnemyArchetypes.All.Count - 1)].Id : encounter.EnemyArchetypeIds[Math.Min(enemyIndex, encounter.EnemyArchetypeIds.Count - 1)];
                        EnemyArchetypes.Get(archetypeId).Apply(unit); enemyIndex++;
                    }
                    units.Add(unit);
                }
                if (units.Count == 0 || !units.Any(unit => unit.IsHero)) return;
                state = new CombatState(map, units);
            }
            if (mapRun != null)
            {
                RogueliteMissionDefinition mission = RogueliteDeveloperCatalog.FindMission(mapRun.HasPendingContentCombat ? mapRun.PendingContentCombatMissionId : mapRun.CurrentNodeId);
                developerPreparation = new MissionPreparation().Configure(mission.Id,
                    currentLevel?.ObjectiveSummary ?? mission.ObjectiveSummary,
                    currentLevel == null ? DescribeEncounter(encounter, mission.EnemySummary) : DescribeEncounter(encounter, currentLevel.EnemySummary(mapRun.RegionBossId)));
                if (currentLevel == null)
                {
                    if (mission.ObjectiveType == CombatObjectiveType.Elimination) state.ConfigureObjectives(new EliminationObjective(mission.Id + "_objective"));
                    else state.ConfigureObjectives(new DestructionObjective(map.PositionsWith(tile => tile.IsObjective), mission.Id + "_objective"));
                }
            }
            else if (rogueliteRun != null)
            {
                RogueliteMissionDefinition mission = rogueliteRun.CurrentMission;
                developerPreparation = new MissionPreparation().Configure(mission.Id,
                    currentLevel?.ObjectiveSummary ?? mission.ObjectiveSummary,
                    currentLevel == null ? mission.EnemySummary : DescribeEncounter(encounter, currentLevel.EnemySummary()));
                if (currentLevel == null)
                {
                    if (mission.ObjectiveType == CombatObjectiveType.Elimination) state.ConfigureObjectives(new EliminationObjective(mission.Id + "_objective"));
                    else state.ConfigureObjectives(new DestructionObjective(map.PositionsWith(tile => tile.IsObjective), mission.Id + "_objective"));
                }
            }
            ConfigureCombatInventory();
            ApplyShortRunChoices();
            mapRun?.ApplyBuild(state.GetUnit("hero"));
            state.SetLoot(new LootContainer(new GridPosition(2, 0), new InventoryItem("aether_core", "\u4ee5\u592a\u6838\u5fc3", 2, 1)));
            string lootKey = mapRun == null ? "relay-crate" : mapRun.CurrentNodeId + "-relay-crate";
            ArtifactDefinition lootArtifact = ArtifactRewardPool.RollLoot(mapRun?.Seed ?? 0, lootKey);
            state.SetLootSource(new LootSourceState(lootKey, new GridPosition(2, 0), new[]
            {
                new ItemInstance(lootKey + "-medkit", "medkit", 0),
                new ItemInstance(lootKey + "-scroll-F-S01", "F-S01", 1),
                new ItemInstance(lootKey + "-artifact-" + lootArtifact.Id, lootArtifact.Id, 2)
            }));
            mapRun?.RestoreLootProgress(state.LootSource);
            developerFlow = new CombatFlowController();
            developerFlow.Configure(developerPreparation, state);
            battlefieldViewport = battlefield.CreateViewport(state.Map.Width, state.Map.Height);
            battlefieldViewport.Focus(state.GetUnit("hero").Position);
            outcomeHandled = false;
            ResetEnemyTurnSequence();
        }

        private static GridPosition ScenePosition(CombatSceneMarker marker) => new GridPosition(Mathf.RoundToInt(marker.transform.position.x), Mathf.RoundToInt(marker.transform.position.y));
        public void OpenDeveloperBriefing() { developerFlow.OpenBriefing(); MarkPresentation(UiPresentationArea.Flow); }
        public void StartDeveloperCombat() { developerFlow.BeginCombat(); state = developerFlow.State; FocusHeroInBattlefield(); fireBattle = new FireBattleState(state); fireLifecycleActiveUnitId = null; ResetEnemyTurnSequence(); visualFeedback?.ResetBattleFeedback(); PublishCombatEffects(CombatResolver.BeginTurn(state, "hero")); RefreshSceneHud(); MarkPresentation(UiPresentationArea.Flow); MarkPresentation(UiPresentationArea.Combat); }
        public void TacticalRestartDeveloperCombat()
        {
            if (trainingRangeActive) { PrepareTrainingRangeCurrent(); return; }
            developerFlow.TacticalRestart(); state = developerFlow.State; FocusHeroInBattlefield(); fireBattle = new FireBattleState(state); fireLifecycleActiveUnitId = null; ResetEnemyTurnSequence(); visualFeedback?.ResetBattleFeedback(); PublishCombatEffects(CombatResolver.BeginTurn(state, "hero")); developerFlow.ResumeAfterRestart(); RefreshSceneHud(); MarkPresentation(UiPresentationArea.Combat);
        }
        public void ReturnToDeveloperMenu()
        {
            if (trainingRangeActive)
            {
                trainingRangeActive = false; rogueliteFlow.Reset();
                developerPreparation = new MissionPreparation().Configure("relay_test", "破坏任务目标并清理威胁", "盾卫、火术师、突袭者、刻印锤手、缚环猎兽");
                BuildCombatFromSceneStageTwo(); selectedAction = "移动"; selectedTargetId = null;
                RefreshSceneHud(); MarkPresentation(UiPresentationArea.Flow); MarkPresentation(UiPresentationArea.Combat); return;
            }
            developerFlow.ReturnToDeveloperMenu(); state = developerFlow.State; selectedAction = "移动"; rogueliteFlow.Reset(); RefreshSceneHud(); MarkPresentation(UiPresentationArea.Flow);
        }
        public void OpenRogueliteMenu() => rogueliteFlow.OpenRogueliteMenu();
        public void CloseRogueliteMenu() => rogueliteFlow.CloseRogueliteMenu();
        public void StartRogueliteStory(bool continueSave)
        {
            RogueliteStoryPackage package;
            if (continueSave)
            {
                if (!saveGateway.TryLoadStory(out package))
                {
                    ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Rejected, "剧情存档无法读取；原始数据已保留，请先删除该存档或修复后重试"));
                    return;
                }
            }
            else package = RogueliteStoryCatalog.CreateDefault(UnityEngine.Random.Range(1, int.MaxValue));
            rogueliteFlow.BeginDeveloperRun(new RogueliteDeveloperRun(package)); BuildCombatFromSceneStageTwo(); developerFlow.OpenBriefing();
        }
        public void StartShortRoguelite(bool continueSave)
        {
            ShortRogueliteRun run;
            if (continueSave)
            {
                if (!saveGateway.TryLoadShortRun(out run))
                {
                    ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Rejected, "短局存档无法读取；原始数据已保留，请先删除该存档或修复后重试"));
                    return;
                }
            }
            else run = new ShortRogueliteRun(UnityEngine.Random.Range(1, int.MaxValue));
            rogueliteFlow.BeginDeveloperRun(new RogueliteDeveloperRun(run)); OpenShortRunPhase();
        }
        public void DeleteShortRogueliteSave() => saveGateway.DeleteShortRun();
        public bool HasShortRogueliteSave => saveGateway.HasShortRun;
        public void StartMapRoguelite(bool continueSave)
        {
            TryStartMapRoguelite(continueSave, FireRogueliteStarterCatalog.Universal);
        }
        public void StartMapRoguelite(bool continueSave, string starterId)
        {
            TryStartMapRoguelite(continueSave, starterId);
        }
        private bool TryStartMapRoguelite(bool continueSave, string starterId)
        {
            RogueliteMapRun run;
            if (continueSave)
            {
                if (!saveGateway.TryLoadMapRun(out run))
                {
                    ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Rejected, DescribeMapSaveFailure()));
                    MarkPresentation(UiPresentationArea.Flow);
                    return false;
                }
            }
            else
            {
                run = new RogueliteMapRun(UnityEngine.Random.Range(1, int.MaxValue), starterId);
                if (!saveGateway.SaveMapRun(run))
                {
                    lastMapSaveSucceeded = false;
                    ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Rejected, "新推进未能写入存档；仍停留在入口，未启动未保存的行动"));
                    MarkPresentation(UiPresentationArea.Flow);
                    return false;
                }
                lastMapSaveSucceeded = true;
            }
            rogueliteFlow.BeginMapRun(run);
            MarkPresentation(UiPresentationArea.Flow); MarkPresentation(UiPresentationArea.MapStructure);
            return true;
        }
        private string DescribeMapSaveFailure()
        {
            switch (saveGateway.LastLoadStatus)
            {
                case RogueliteSaveLoadStatus.Missing: return "没有可继续的地图存档；未创建或覆盖任何数据";
                case RogueliteSaveLoadStatus.CorruptData: return "地图存档文本损坏；主槽与首份备份已保护，明确删槽前不可覆盖";
                case RogueliteSaveLoadStatus.InvalidSemantics: return "地图存档状态不合法；主槽与首份备份已保护，明确删槽前不可覆盖";
                case RogueliteSaveLoadStatus.StoreError: return "存档存储暂时不可用；未把故障当作无存档，也未启动新推进";
                default: return "地图存档无法读取；未启动新推进";
            }
        }
        public void RequestStartMapRoguelite(bool continueSave)
            => RequestStartMapRoguelite(continueSave, FireRogueliteStarterCatalog.Universal);
        public void RequestStartMapRoguelite(bool continueSave, string starterId)
        {
            if (!continueSave && HasMapRogueliteSave)
            {
                RequestConfirmation(new UiConfirmationRequest(UiConfirmationKind.ReplaceExistingRun, "覆盖现有推进？",
                    MapSavePresentation.ReplacementMessage, "覆盖并新开"), () =>
                    {
                        if (!PrepareMapSlotForReplacement())
                        {
                            ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Rejected, "旧地图存档当前无法安全替换，未启动新推进"));
                            return;
                        }
                        if (!TryStartMapRoguelite(false, starterId)) return;
                        ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Saved, "已创建并保存新的首区推进"));
                    });
                return;
            }
            if (!TryStartMapRoguelite(continueSave, starterId)) return;
            ShowUiFeedback(new UiActionFeedback(continueSave ? UiFeedbackKind.Information : UiFeedbackKind.Saved,
                continueSave ? "已读取最近一次地图推进" : "已创建并保存新的首区推进"));
        }
        public void DeleteMapRogueliteSave() => saveGateway.DeleteMapRun();
        public bool HasMapRogueliteSave => saveGateway.HasMapRun;
        public MapSaveUiPresentation MapSavePresentation => MapSaveUiPresentation.From(HasMapRogueliteSave, saveGateway.LastLoadStatus, lastMapSaveSucceeded);
        public string SettingsSaveDetail => lastSettingsSaveSucceeded ? "所有设置已保存" : "设置已生效 · 持久化失败";

        private bool PrepareMapSlotForReplacement()
        {
            if (saveGateway.TryLoadMapRun(out _)) return true;
            if (saveGateway.LastLoadStatus == RogueliteSaveLoadStatus.Missing) return true;
            if (saveGateway.LastLoadStatus == RogueliteSaveLoadStatus.CorruptData || saveGateway.LastLoadStatus == RogueliteSaveLoadStatus.InvalidSemantics)
                return saveGateway.DeleteMapRun();
            return false;
        }
        public void SelectMapNode(string nodeId)
        {
            RogueliteMapNode node = RogueliteMapCatalog.Node(nodeId);
            bool resumesCurrentCombat = node.Id == mapRun.CurrentNodeId && RogueliteUiPreferences.CanOpenCombatBriefing(mapRun, node);
            bool startsCombat = resumesCurrentCombat || RogueliteUiPreferences.StartsCombat(mapRun, node);
            bool safeRevisit = mapRun.CompletedNodes.Contains(nodeId);
            string previousNodeId = mapRun.CurrentNodeId;
            if (!resumesCurrentCombat) mapRun.SelectNode(nodeId);
            MarkPresentation(UiPresentationArea.MapStructure);
            PublishUiVisual(new UiVisualEvent(safeRevisit ? UiVisualEventKind.SafeRevisit : UiVisualEventKind.MapLocationChanged,
                nodeId, message: previousNodeId + "→" + nodeId));
            if (!startsCombat)
            {
                SaveMapRun(); return;
            }
            SaveMapRun(); BuildCombatFromSceneStageTwo(); developerFlow.OpenBriefing();
            PublishUiVisual(new UiVisualEvent(UiVisualEventKind.BriefingOpened, nodeId));
        }

        private static string DescribeEncounter(RogueliteEncounterDefinition encounter, string fallback)
        {
            if (encounter == null) return fallback;
            string summary = string.Join("、", encounter.EnemyArchetypeIds.Select(id => EnemyArchetypes.Get(id).DisplayName));
            return (encounter.IsBoss ? "区域首领：" : encounter.IsElite ? "精英编成：" : "区域编成：") + summary;
        }
        public void ChooseMapNodeContent(string choiceId)
        {
            int parts = mapRun.Parts, aether = mapRun.Aether, supplies = mapRun.Supplies, scouting = mapRun.ScoutingBeacons, access = mapRun.AccessCards;
            mapRun.ChooseCurrentNodeContent(choiceId);
            MarkPresentation(UiPresentationArea.MapStructure);
            PublishResourceChanges(parts, aether, supplies, scouting, access);
            if (mapRun.HasPendingContentCombat) { SaveMapRun(); BuildCombatFromSceneStageTwo(); developerFlow.OpenBriefing(); PublishUiVisual(new UiVisualEvent(UiVisualEventKind.BriefingOpened, choiceId)); return; }
            SaveMapRun();
        }
        public void ClaimMapReward(string rewardId)
        {
            int parts = mapRun.Parts, aether = mapRun.Aether, supplies = mapRun.Supplies, scouting = mapRun.ScoutingBeacons, access = mapRun.AccessCards;
            mapRun.ClaimReward(rewardId);
            MarkPresentation(UiPresentationArea.Settlement);
            MarkPresentation(UiPresentationArea.MapStructure);
            PublishResourceChanges(parts, aether, supplies, scouting, access);
            PublishUiVisual(new UiVisualEvent(UiVisualEventKind.RewardClaimed, rewardId));
            SaveMapRun(); settlementPresentation?.RefreshNow();
        }
        public void ClaimMapFireSpell(string spellId)
        {
            mapRun.ClaimFireSpell(spellId);
            MarkPresentation(UiPresentationArea.Settlement); MarkPresentation(UiPresentationArea.MapStructure);
            PublishUiVisual(new UiVisualEvent(UiVisualEventKind.RewardClaimed, spellId));
            SaveMapRun(); settlementPresentation?.RefreshNow();
        }
        public void EquipMapFireSpell(string spellId, int slot) { mapRun.EquipFireSpell(spellId, slot); SaveMapRun(); MarkPresentation(UiPresentationArea.MapStructure); MarkPresentation(UiPresentationArea.Combat); }
        public void EquipNextMapFireSpell(int slot)
        {
            if (mapRun == null || mapRun.OwnedFireSpellIds.Count == 0 || slot < 0 || slot >= mapRun.EquippedFireSpellIds.Count) return;
            string current = mapRun.EquippedFireSpellIds[slot];
            int currentIndex = mapRun.OwnedFireSpellIds.ToList().FindIndex(id => string.Equals(id, current, StringComparison.Ordinal));
            for (int offset = 1; offset <= mapRun.OwnedFireSpellIds.Count; offset++)
            {
                string candidate = mapRun.OwnedFireSpellIds[(Math.Max(-1, currentIndex) + offset) % mapRun.OwnedFireSpellIds.Count];
                if (mapRun.EquippedFireSpellIds.Where((id, index) => index != slot).Contains(candidate)) continue;
                if (!FireSpellCatalog.IsWeaponCompatible(FireSpellCatalog.Get(candidate), mapRun.EquippedWeapon)) continue;
                EquipMapFireSpell(candidate, slot);
                return;
            }
        }
        public void EquipMapReward(string rewardId) { mapRun.EquipReward(rewardId); SaveMapRun(); MarkPresentation(UiPresentationArea.MapStructure); }
        public void CalibrateMapAether()
        {
            int parts = mapRun.Parts, aether = mapRun.Aether, supplies = mapRun.Supplies, scouting = mapRun.ScoutingBeacons, access = mapRun.AccessCards;
            mapRun.CalibrateAether();
            MarkPresentation(UiPresentationArea.MapStructure);
            PublishResourceChanges(parts, aether, supplies, scouting, access);
            SaveMapRun();
        }
        public void ReturnToMapRun() { developerFlow.ReturnToDeveloperMenu(); state = developerFlow.State; rogueliteFlow.ReturnToMap(); RefreshSceneHud(); MarkPresentation(UiPresentationArea.Flow); MarkPresentation(UiPresentationArea.MapStructure); }
        public void RequestReturnToLanding()
        {
            if (mapRun != null && !SaveMapRun()) return;
            ReturnToDeveloperMenu();
        }
        private bool SaveMapRun()
        {
            lastMapSaveSucceeded = mapRun != null && saveGateway.SaveMapRun(mapRun);
            if (!lastMapSaveSucceeded)
            {
                ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Rejected, "地图进度未能写入；当前状态仍保留在内存中，请勿退出并稍后重试"));
                MarkPresentation(UiPresentationArea.Flow);
            }
            return lastMapSaveSucceeded;
        }
        public void ChooseShortEvent() { rogueliteRun.ShortRun.ChooseEvent("field_repair"); SaveShortRun(); }
        public void ChooseShortSalvage() { rogueliteRun.ShortRun.ChooseSalvage("shield_cell"); SaveShortRun(); }
        public void ChooseShortUpgrade() { rogueliteRun.ShortRun.ChooseUpgrade("calibrated_rifle"); SaveShortRun(); }
        private void OpenShortRunPhase()
        {
            if (rogueliteRun?.IsShortRun != true) return;
            if (rogueliteRun.ShortRun.Phase == ShortRoguelitePhase.FirstCombat || rogueliteRun.ShortRun.Phase == ShortRoguelitePhase.SecondCombat) { BuildCombatFromSceneStageTwo(); developerFlow.OpenBriefing(); }
            else { developerFlow.ReturnToDeveloperMenu(); state = developerFlow.State; rogueliteMenuOpen = true; }
        }
        private void SaveShortRun() => saveGateway.SaveShortRun(rogueliteRun.ShortRun);
        private void ApplyShortRunChoices()
        {
            if (rogueliteRun?.IsShortRun != true || rogueliteRun.ShortRun.Phase != ShortRoguelitePhase.SecondCombat) return;
            UnitState hero = state.GetUnit("hero");
            if (rogueliteRun.ShortRun.EventChoiceId == "field_repair") hero.Armor += 1;
            if (rogueliteRun.ShortRun.UpgradeChoiceId == "calibrated_rifle") hero.Equip(StageTwoBuilds.CalibratedRifle, CombatCatalog.Shield, CombatCatalog.FireBolt, CombatCatalog.FrostBind);
        }

        private void ConfigureCombatInventory()
        {
            if (mapRun != null)
            {
                state.ConfigureItemInventory(mapRun.Inventory, mapRun.ItemQuickbar);
                return;
            }

            InventoryContainerState inventory = new InventoryContainerState();
            List<string> slots = new List<string>();
            AddExplicitCombatItem(inventory, slots, "combat-medkit", "medkit", 0);
            AddExplicitCombatItem(inventory, slots, "combat-shield-cell", "shield_cell", 1);
            if (rogueliteRun?.IsShortRun == true && rogueliteRun.ShortRun.Phase == ShortRoguelitePhase.SecondCombat && rogueliteRun.ShortRun.SalvageChoiceId == "shield_cell")
                AddExplicitCombatItem(inventory, slots, "combat-shield-cell-salvage", "shield_cell", 2);
            state.ConfigureItemInventory(inventory, slots);
        }

        private static void AddExplicitCombatItem(InventoryContainerState inventory, IList<string> slots, string instanceId, string definitionId, int acquisitionOrder)
        {
            InventoryResult result = inventory.AddFirstFit(new ItemInstance(instanceId, definitionId, acquisitionOrder));
            if (!result.Success) throw new InvalidOperationException("Unable to configure explicit combat inventory: " + result.Error);
            slots.Add(instanceId);
        }
        public void StartRogueliteSandbox()
        {
            IReadOnlyList<TaskTemplate> templates = RogueliteDeveloperCatalog.OpenSandboxTemplates;
            rogueliteFlow.BeginDeveloperRun(new RogueliteDeveloperRun(templates[sandboxTemplateIndex % templates.Count].Id, UnityEngine.Random.Range(1, int.MaxValue)));
            BuildCombatFromSceneStageTwo(); developerFlow.OpenBriefing();
        }
        public void SelectNextSandboxTemplate() { sandboxTemplateIndex = (sandboxTemplateIndex + 1) % RogueliteDeveloperCatalog.OpenSandboxTemplates.Count; }
        public void DeleteRogueliteSave() => saveGateway.DeleteStory();
        public bool HasRogueliteSave => saveGateway.HasStory;
        public CombatState CurrentState => state;
        public BattlefieldViewport BattlefieldViewport
        {
            get
            {
                if (state == null) return null;
                if (battlefieldViewport == null)
                    battlefieldViewport = battlefield.CreateViewport(state.Map.Width, state.Map.Height);
                return battlefieldViewport;
            }
        }
        public bool IsBattlefieldVisible => Application.isPlaying && developerFlow != null && state != null &&
            developerFlow.Phase != CombatFlowPhase.DeveloperMenu && developerFlow.Phase != CombatFlowPhase.Briefing &&
            (mapRun == null || !mapRun.AwaitingReward);
        public void FocusBattlefieldOnHero() => FocusHeroInBattlefield();
        public void SubmitBattlefieldCell(GridPosition position, bool inspection)
        {
            if (state == null || !state.Map.IsInside(position)) return;
            if (inspection) HandleInspectionClick(position);
            else HandleCellClick(position);
        }
        public BattlefieldCellPresentation PresentBattlefieldCell(GridPosition position)
        {
            if (state == null || !state.Map.IsInside(position)) return null;
            TileState tile = state.Map.GetTile(position);
            int environmentFrame = Mathf.FloorToInt(Time.unscaledTime * 8f) % formalFiregroundFrames.Length;
            Texture2D environment = fireBattle?.HasFireground(position) == true
                ? formalFiregroundFrames[environmentFrame]
                : tile.SmokeExpiresAt > state.CurrentTime ? formalSmokeFrames[environmentFrame] : null;
            Texture2D move = IsInMoveRange(position) ? formalOverlayTextures["move_range"] : null;
            Texture2D attack = IsInAttackRange(position) ? formalOverlayTextures["attack_range"] : null;
            Texture2D skill = null;
            int fireSlot = selectedAction == "技能1" ? 0 : selectedAction == "技能2" ? 1 : -1;
            FireSpellDefinition fireSpell = fireSlot < 0 ? null : FireSpellInSlot(fireSlot);
            FireSpellPreview firePreview = fireSpell == null ? null : BuildFireSpellPreviewAt(fireSpell, position);
            if (firePreview?.CanCommit == true)
                skill = formalOverlayTextures[firePreview.FriendlyFireRisk ? "high_risk" : "attack_range"];

            UnitState unit = state.Units.Values.FirstOrDefault(candidate => candidate.IsAlive && candidate.Position == position);
            Texture2D unitTexture = FormalUnitTexture(unit);
            Vector2 unitOffset = Vector2.zero;
            Color unitTint = Color.white;
            if (unit != null)
            {
                float phase = unit.IsHero ? 0f : unit.Position.X * .71f + unit.Position.Y * .37f;
                unitOffset.y = Mathf.RoundToInt(Mathf.Sin(Time.unscaledTime * 1.8f + phase));
                if (visualFeedback != null)
                {
                    unitOffset += visualFeedback.UnitPresentationOffset(unit);
                    unitOffset.x += visualFeedback.UnitShakeOffset(unit);
                    unitTint = visualFeedback.UnitPresentationTint(unit);
                }
            }

            CombatTargetDamageForecast forecast = unit != null && !unit.IsHero ? TargetDamageForecast(unit) : null;
            CombatUnitVitalsPresentation vitals = unit == null ? null : CombatUnitVitalsPresentation.From(unit, forecast);
            List<BattlefieldStatusVisual> statuses = unit == null ? new List<BattlefieldStatusVisual>() :
                unit.Statuses.OrderBy(entry => entry.Key).Take(6)
                    .Select(entry => new BattlefieldStatusVisual(CombatStatusPresentation.From(unit, entry.Key),
                        formalStatusTextures.TryGetValue(entry.Key, out Texture2D texture) ? texture : null)).ToList();
            EnemyIntentPresentation intent = unit != null && !unit.IsHero ? EnemyIntent(unit) : null;
            Texture2D intentTexture = intent != null && formalIntentTextures.TryGetValue(intent.IconId, out Texture2D icon)
                ? icon : null;

            Texture2D objectTexture = null;
            string objectLabel = string.Empty;
            Color objectLabelColor = FormalUiTheme.Text;
            if (tile.IsObjective)
            {
                string key = tile.IsDestroyed ? "relay_rubble" : tile.Durability < 6 ? "relay_damaged" : "relay_intact";
                objectTexture = formalRelayTextures[key];
                if (!tile.IsDestroyed) objectLabel = "导能柱";
            }
            else if (tile.Cover == CoverType.Light)
            {
                string key = tile.IsDestroyed ? "light_cover_rubble" : tile.Durability < 4 ? "light_cover_damaged" : "light_cover_intact";
                objectTexture = formalRelayTextures[key];
            }
            else if (tile.Cover == CoverType.Heavy)
            {
                string key = tile.IsDestroyed ? "heavy_cover_rubble" : tile.Durability < 7 ? "heavy_cover_damaged" : "heavy_cover_intact";
                objectTexture = formalRelayTextures[key];
            }
            else if (trainingRangeActive && tile.IsDevice)
            {
                objectTexture = formalRelayTextures[tile.IsDestroyed ? "heavy_cover_rubble" : "heavy_cover_intact"];
                objectLabel = "设备";
            }
            if (trainingRangeActive && tile.IsWater)
            {
                objectLabel = "水面";
                objectLabelColor = new Color(.38f, .82f, .94f, .92f);
            }

            Texture2D loot = state.Loot != null && state.Loot.Position == position
                ? state.Loot.IsLooted ? formalRelayTextures["loot_crate_empty"] : formalLootTexture : null;
            bool selected = targetNavigation.Active && targetNavigation.Position == position ||
                unit != null && unit.Id == selectedTargetId;
            Texture2D selection = selected ? formalOverlayTextures["selected"] : null;
            string hover = unit == null ? string.Empty : unit.IsHero
                ? CombatInformationPresenter.BuildHeroDetails(unit)
                : CombatInformationPresenter.BuildEnemyHoverDetails(state, unit, intent) +
                  (forecast == null ? string.Empty : "\n伤害预览：" + forecast.PlayerSummary);
            Texture2D floor = formalRelayTextures[FloorKeyForCurrentLevel(position.X, position.Y)];
            Rect uv = unitTexture == null ? new Rect(0f, 0f, 1f, 1f) : CombatUnitHudLayout.UnitTextureCropUv(unitTexture.name);
            return new BattlefieldCellPresentation(position, floor, environment, move,
                selectedAction == "移动" ? 1f : .45f, attack, selectedAction == "攻击" ? 1f : .65f,
                skill, selection, unitTexture, uv, unitTint, unitOffset, objectTexture, objectLabel,
                objectLabelColor, loot, unit, vitals, statuses, intent, intentTexture, hover);
        }
        public BattlefieldRect CurrentBattlefieldBoard => battlefieldViewport?.BoardRect ?? battlefield.BoardRect(state?.Map.Width ?? BattlefieldPresentationAdapter.DefaultWidth, state?.Map.Height ?? BattlefieldPresentationAdapter.DefaultHeight);
        public BattlefieldRect CurrentBattlefieldViewport => battlefieldViewport?.ViewportRect ?? battlefield.ViewportRect;
        public Vector2 GridToFeedbackPosition(GridPosition position)
        {
            BattlefieldRect board = CurrentBattlefieldBoard;
            BattlefieldRect cell = battlefield.CellRect(board, state?.Map.Height ?? BattlefieldPresentationAdapter.DefaultHeight, position);
            return new Vector2(cell.X + cell.Width * .5f - UiWidth * .5f, UiHeight * .5f - cell.Y - cell.Height * .5f);
        }
        public EnemyTurnSequencePhase EnemyTurnPresentationPhase => enemyTurn.Phase;
        public string EnemyTurnPresentationUnitId => enemyTurn.UnitId;
        public string CurrentLevelId => currentLevel?.Id;
        public FireBattleState CurrentFireBattle => fireBattle;
        public ArtifactBattleState CurrentArtifactBattle => artifactBattle;
        public string SelectedAction => selectedAction;
        public string SelectedTargetId => selectedTargetId;
        public bool IsKeyboardTargeting => targetNavigation.Active;
        public GridPosition KeyboardTargetPosition => targetNavigation.Position;
        public CombatActionPreview CurrentActionPreview => BuildActionPreview(selectedAction);
        public CombatActionPreview ActionPreview(string action) => BuildActionPreview(action);
        public CombatOutcomePresentation CurrentOutcomePresentation => state == null ? null : CombatInformationPresenter.BuildOutcome(state, mapRun != null);
        public string CurrentPhaseText => CombatInformationPresenter.PhaseText(CurrentFlowPhase, state);
        public EnemyIntentPresentation EnemyIntent(UnitState enemy) => enemy == null || state == null ? null : enemyPlans.GetPublicIntent(state, enemy, state.GetUnit("hero"));
        public FireSpellDefinition FireSpellInSlot(int slot)
        {
            if (trainingRangeActive) return slot == 0 ? trainingRangeSession?.CurrentFireSpell : null;
            if (slot == 0 && state?.ItemInventory.Get(armedInventoryItemId) is ItemInstance armed) return ItemAbilityCatalog.For(armed.DefinitionId);
            if (mapRun == null || slot < 0 || slot >= mapRun.EquippedFireSpellIds.Count) return null;
            string id = mapRun.EquippedFireSpellIds[slot];
            return string.IsNullOrEmpty(id) ? null : FireSpellCatalog.Get(id);
        }
        private CombatActionPreview BuildActionPreview(string action)
        {
            ArtifactDefinition armedArtifact = CurrentArmedInventoryItem != null &&
                ItemCatalog.Get(CurrentArmedInventoryItem.DefinitionId).Category == ItemCategory.Artifact
                ? ArtifactCatalog.Get(CurrentArmedInventoryItem.DefinitionId) : CurrentTrainingRangeArtifact;
            if (action == "技能1" && armedArtifact != null && state != null)
            {
                EnsureArtifactBattle(); int validArtifacts = 0;
                for (int y = 0; y < state.Map.Height; y++) for (int x = 0; x < state.Map.Width; x++)
                    if (BuildArtifactTarget(armedArtifact, new GridPosition(x, y), out ArtifactTarget candidate) &&
                        ArtifactEngine.Preview(artifactBattle, "hero", armedArtifact, candidate,
                            CurrentArmedInventoryItem?.RemainingUses ?? trainingRangeArtifactUsesRemaining).CanCommit) validArtifacts++;
                return new CombatActionPreview(action, armedArtifact.TargetSummary, armedArtifact.PublicCost,
                    armedArtifact.EffectSummary + "；风险：" + armedArtifact.RiskSummary, validArtifacts,
                    validArtifacts == 0 ? "当前没有合法目标" : string.Empty);
            }
            int slot = action == "技能1" ? 0 : action == "技能2" ? 1 : -1;
            FireSpellDefinition spell = slot < 0 ? null : FireSpellInSlot(slot);
            if (spell == null || state == null) return availability.Preview(state, action, selectedTargetId);
            if (fireBattle == null || fireBattle.Combat != state) fireBattle = new FireBattleState(state);
            int valid = 0;
            for (int y = 0; y < state.Map.Height; y++) for (int x = 0; x < state.Map.Width; x++) if (IsFireSpellCellValid(spell, new GridPosition(x, y))) valid++;
            string failure = string.Empty;
            UnitState selected = string.IsNullOrEmpty(selectedTargetId) ? null : state.GetUnit(selectedTargetId);
            if (selected != null)
            {
                FireSpellPreview exact = FireSpellEngine.Preview(fireBattle, "hero", spell, FireSpellTarget.Unit(selected.Id, FacingToward(state.GetUnit("hero").Position, selected.Position)));
                failure = string.Join("；", exact.Failures);
            }
            else if (valid == 0) failure = "当前没有合法目标";
            string effects = RogueliteSettlementPresentation.FireSpellPlayerSummary(spell);
            string targetSummary = RogueliteSettlementPresentation.FireSpellTargetSummary(spell);
            return new CombatActionPreview(action, targetSummary,
                spell.ActionPointCost + " 行动 + " + spell.ManaCost + " 以太", effects, valid, failure);
        }
        private bool IsFireSpellCellValid(FireSpellDefinition spell, GridPosition position)
        {
            return BuildFireSpellPreviewAt(spell, position).CanCommit;
        }
        private FireSpellPreview BuildFireSpellPreviewAt(FireSpellDefinition spell, GridPosition position)
        {
            UnitState unit = state.Units.Values.FirstOrDefault(candidate => candidate.IsAlive && candidate.Position == position);
            Facing facing = FacingToward(state.GetUnit("hero").Position, position);
            FireSpellTarget target = unit == null ? FireSpellTarget.At(position, facing) : FireSpellTarget.Unit(unit.Id, facing);
            return FireSpellEngine.Preview(fireBattle, "hero", spell, target);
        }
        public void SetSelectedTargetForUi(string unitId)
        {
            selectedTargetId = state != null && state.GetUnit(unitId) != null ? unitId : null;
            MarkPresentation(UiPresentationArea.Combat);
        }
        public bool BeginKeyboardTargeting()
        {
            UnitState hero = state?.GetUnit("hero");
            if (hero == null || !hero.IsAlive || state.ActiveUnitId != hero.Id) return false;
            UnitState selected = string.IsNullOrEmpty(selectedTargetId) ? null : state.GetUnit(selectedTargetId);
            targetNavigation.Begin(selected?.Position ?? hero.Position, state.Map.Width, state.Map.Height);
            MarkPresentation(UiPresentationArea.Combat);
            return true;
        }
        public void MoveKeyboardTarget(int deltaX, int deltaY)
        {
            if (state == null || !targetNavigation.Active) return;
            targetNavigation.Move(deltaX, deltaY, state.Map.Width, state.Map.Height);
            UnitState unit = state.Units.Values.FirstOrDefault(candidate => candidate.IsAlive && candidate.Position == targetNavigation.Position);
            selectedTargetId = unit != null && !unit.IsHero ? unit.Id : null;
            MarkPresentation(UiPresentationArea.Combat);
        }
        public void CommitKeyboardTarget()
        {
            if (state == null || !targetNavigation.Active) return;
            GridPosition position = targetNavigation.Position;
            targetNavigation.End();
            HandleCellClick(position);
            MarkPresentation(UiPresentationArea.Combat);
        }
        public void CancelKeyboardTargeting()
        {
            if (!targetNavigation.Active) return;
            targetNavigation.End();
            selectedTargetId = null;
            MarkPresentation(UiPresentationArea.Combat);
            ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Information, "已取消战场目标选择"));
        }
        public void CancelCombatSelectionOrRequestLeave()
        {
            CombatCancelResolution resolution = CombatSelectionNavigation.ResolveCancel(selectedAction, selectedTargetId, !string.IsNullOrEmpty(armedInventoryItemId));
            if (resolution == CombatCancelResolution.ClearTarget)
            {
                selectedTargetId = null;
                MarkPresentation(UiPresentationArea.Combat);
                ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Information, "已取消目标查看"));
                return;
            }
            if (resolution == CombatCancelResolution.ResetAction)
            {
                selectedAction = "移动";
                selectedTargetId = null;
                armedInventoryItemId = null;
                MarkPresentation(UiPresentationArea.Combat);
                ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Information, "已取消当前行动选择"));
                return;
            }
            RequestLeaveCombat();
        }
        public RogueliteMapRun CurrentMapRun => mapRun;
        public RogueliteMapRun ArchivedMapRun
        {
            get
            {
                if (mapRun != null) return mapRun;
                return saveGateway.TryLoadMapRun(out RogueliteMapRun archived) ? archived : null;
            }
        }
        public CombatFlowPhase CurrentFlowPhase => developerFlow == null ? CombatFlowPhase.DeveloperMenu : developerFlow.Phase;
        public MissionPreparation CurrentPreparation => developerFlow?.Preparation ?? developerPreparation;
        public bool IsMapMenuOpen => mapMenuOpen;
        public bool IsRogueliteMenuOpen => rogueliteMenuOpen;
        public RogueliteUiPreferences UiPreferences => uiPreferences;
        public UiVisualEventStream UiVisualEvents => uiVisualEvents;
        public UiPresentationVersions UiPresentationVersions => uiPresentationVersions;
        public bool IsDeveloperCombatActive => developerFlow != null && developerFlow.Phase == CombatFlowPhase.Active;
        public bool IsTrainingRangeActive => trainingRangeActive;
        public TrainingRangeSession TrainingRange => trainingRangeSession;
        public ArtifactDefinition CurrentTrainingRangeArtifact => trainingRangeSession?.CurrentArtifact;
        public int TrainingRangeArtifactUsesRemaining => trainingRangeArtifactUsesRemaining;
        public ItemInstance CurrentArmedInventoryItem => state?.ItemInventory.Get(armedInventoryItemId);
        public ArtifactDefinition CurrentArmedArtifact => CurrentArmedInventoryItem != null &&
            ItemCatalog.Get(CurrentArmedInventoryItem.DefinitionId).Category == ItemCategory.Artifact
            ? ArtifactCatalog.Get(CurrentArmedInventoryItem.DefinitionId) : null;
        public bool IsCombatOutcomeVisible => developerFlow != null && (developerFlow.Phase == CombatFlowPhase.Victory || developerFlow.Phase == CombatFlowPhase.Defeat);
        public bool IsInteractionModalOpen => (interactionLayer != null && interactionLayer.IsConfirmationOpen) || (inventoryPanel != null && inventoryPanel.IsOpen);
        public void ToggleDeveloperConsole()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (DeveloperBuildGate.IsEnabled) developerConsole?.Toggle();
#endif
        }
        public void StartTrainingRange()
        {
            if (!DeveloperBuildGate.IsEnabled) return;
            startupPresentation?.DismissImmediately();
            rogueliteFlow.Reset(); trainingRangeActive = true;
            if (trainingRangeSession == null) trainingRangeSession = new TrainingRangeSession();
            PrepareTrainingRangeCurrent();
        }
        public void SelectTrainingRangeAbility(string abilityId)
        {
            if (!DeveloperBuildGate.IsEnabled) return;
            if (trainingRangeSession == null) trainingRangeSession = new TrainingRangeSession();
            trainingRangeSession.Select(abilityId); PrepareTrainingRangeCurrent();
        }
        public void BrowseTrainingRangeAbility(string abilityId)
        {
            if (!DeveloperBuildGate.IsEnabled) return;
            if (trainingRangeSession == null) trainingRangeSession = new TrainingRangeSession();
            trainingRangeSession.Select(abilityId);
        }
        public void ShiftTrainingRangePage(int delta)
        {
            if (!DeveloperBuildGate.IsEnabled) return;
            if (trainingRangeSession == null) trainingRangeSession = new TrainingRangeSession();
            trainingRangeSession.ShiftPage(delta);
        }
        public void PrepareTrainingRangeCurrent()
        {
            if (!DeveloperBuildGate.IsEnabled) return;
            if (trainingRangeSession == null) trainingRangeSession = new TrainingRangeSession();
            trainingRangeActive = true;
            currentLevel = null;
            ITrainingRangeCase prepared = trainingRangeSession.PrepareCurrent();
            state = prepared.Combat; fireBattle = trainingRangeSession.CurrentFireBattle; fireLifecycleActiveUnitId = state.ActiveUnitId;
            artifactBattle = (prepared as ArtifactTrainingRangeCase)?.Battle ?? new ArtifactBattleState(state);
            trainingRangeArtifactUsesRemaining = trainingRangeSession.CurrentArtifact?.MaximumUses ?? 0;
            developerPreparation = new MissionPreparation().Configure("training_range", "能力验证与确定性回归", "标准靶兵、友军、掩体、设备、水面与核心样本");
            developerFlow = new CombatFlowController(); developerFlow.Configure(developerPreparation, state); developerFlow.OpenBriefing(); developerFlow.BeginCombat();
            selectedAction = "技能1"; selectedTargetId = prepared.RecommendedUnitId; outcomeHandled = false;
            ResetEnemyTurnSequence(); visualFeedback?.ResetBattleFeedback(); RefreshSceneHud(); MarkPresentation(UiPresentationArea.Flow); MarkPresentation(UiPresentationArea.Combat);
        }
        public TrainingRangePreviewReport PreviewTrainingRangeCurrent()
        {
            if (!DeveloperBuildGate.IsEnabled || trainingRangeSession == null) return null;
            TrainingRangePreviewReport report = trainingRangeSession.PreviewCurrent();
            state.AddLog(trainingRangeSession.CurrentAbility.Id + " // " + report.Summary); MarkPresentation(UiPresentationArea.Combat); return report;
        }
        public TrainingRangeExecutionReport ExecuteTrainingRangeCurrent()
        {
            if (!DeveloperBuildGate.IsEnabled || trainingRangeSession == null) return null;
            if (trainingRangeSession.CurrentCase == null || trainingRangeSession.CurrentCase.Combat != state) PrepareTrainingRangeCurrent();
            GridPosition source = state.GetUnit("hero").Position;
            TrainingRangePreviewReport preview = trainingRangeSession.PreviewCurrent();
            TrainingRangeExecutionReport report = trainingRangeSession.ExecuteCurrent();
            state.AddLog(trainingRangeSession.CurrentAbility.Id + " // " + report.Summary);
            if (preview.NativeResult is FireSpellPreview firePreview && trainingRangeSession.CurrentFireSpell != null)
                visualFeedback?.NotifyFireSpell(trainingRangeSession.CurrentFireSpell, source, firePreview.Cells);
            else if (trainingRangeSession.CurrentSkill != null)
                visualFeedback?.NotifySkillDelivery(trainingRangeSession.CurrentSkill, source, trainingRangeSession.CurrentCase.RecommendedCell);
            MarkPresentation(UiPresentationArea.Combat); return report;
        }
        public TrainingRangeAuditReport RunTrainingRangeAudit()
        {
            if (!DeveloperBuildGate.IsEnabled) return null;
            if (trainingRangeSession == null) trainingRangeSession = new TrainingRangeSession();
            TrainingRangeAuditReport report = trainingRangeSession.RunFullAudit();
            state?.AddLog(report.Summary); MarkPresentation(UiPresentationArea.Combat); return report;
        }
        public void RequestTacticalRestart()
        {
            RequestConfirmation(new UiConfirmationRequest(UiConfirmationKind.TacticalRestart, "战术重开？",
                "当前战斗进度将被放弃，并恢复到本场战斗开始时的状态。", "确认重开"), TacticalRestartDeveloperCombat);
        }
        public void RequestLeaveCombat()
        {
            if (!IsDeveloperCombatActive) return;
            RequestConfirmation(new UiConfirmationRequest(UiConfirmationKind.LeaveCombat, "离开未完成战斗？",
                "当前战斗内的行动不会结算。肉鸽地图与战斗开始前存档保持不变。", "离开战斗"), () =>
                {
                    if (mapRun != null) ReturnToMapRun();
                    else ReturnToDeveloperMenu();
                });
        }
        public void RequestConfirmation(UiConfirmationRequest request, Action onConfirm) => interactionLayer?.RequestConfirmation(request, onConfirm);
        public void ShowUiFeedback(UiActionFeedback feedback) => interactionLayer?.ShowFeedback(feedback);
        public void PublishUiVisual(UiVisualEvent visualEvent) => uiVisualEvents.Publish(visualEvent);
        private void MarkPresentation(UiPresentationArea area) => uiPresentationVersions.Mark(area);
        public void NotifyMapNodeSelected(string nodeId)
        {
            if (!string.IsNullOrWhiteSpace(nodeId)) PublishUiVisual(new UiVisualEvent(UiVisualEventKind.MapNodeSelected, nodeId));
        }

        private void PublishResourceChanges(int parts, int aether, int supplies, int scouting, int access)
        {
            PublishResourceChange("零件", parts, mapRun.Parts);
            PublishResourceChange("以太", aether, mapRun.Aether);
            PublishResourceChange("补给", supplies, mapRun.Supplies);
            PublishResourceChange("侦测", scouting, mapRun.ScoutingBeacons);
            PublishResourceChange("权限卡", access, mapRun.AccessCards);
        }

        private void PublishResourceChange(string resource, int before, int after)
        {
            int delta = after - before;
            if (delta != 0)
            {
                MarkPresentation(UiPresentationArea.MapResources);
                PublishUiVisual(new UiVisualEvent(UiVisualEventKind.ResourceChanged, resource, delta));
            }
        }
        public void UpdateUiPreferences(float masterVolume, float animationIntensity, bool screenShake, bool floatingText, bool highContrast, bool largeText, bool keyHints)
        {
            uiPreferences.Configure(masterVolume, animationIntensity, screenShake, floatingText, highContrast, largeText, keyHints);
            lastSettingsSaveSucceeded = saveGateway.SaveUiPreferences(uiPreferences);
            if (!lastSettingsSaveSucceeded)
                ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Rejected, "设置已在本次运行中生效，但未能持久化保存"));
            ApplyUiPreferences();
            MarkPresentation(UiPresentationArea.Settings);
        }
        private void ApplyUiPreferences()
        {
            AudioListener.volume = uiPreferences.MasterVolume;
            FormalUiTheme.ConfigureAccessibility(uiPreferences.HighContrast, uiPreferences.LargeText);
        }
        public void SelectHudAction(string action)
        {
            targetNavigation.End();
            selectedAction = action;
            selectedTargetId = null;
            MarkPresentation(UiPresentationArea.Combat);
            PublishUiVisual(new UiVisualEvent(UiVisualEventKind.CombatActionSelected, action));
            PublishUiVisual(new UiVisualEvent(UiVisualEventKind.CombatRangeRevealed, action, message: GetRangeDescription()));
        }
        public void SearchCurrentLoot() { if (state != null) { TryCommand(CombatCommand.SearchLoot("hero")); PersistCombatInventory(); } }
        public void TakeCurrentLoot(string instanceId) { if (state != null) { TryCommand(CombatCommand.TakeLoot("hero", instanceId)); PersistCombatInventory(); } }
        public void EquipInventoryQuickbar(string instanceId, int slot) { if (state != null) { TryCommand(CombatCommand.EquipInventoryQuickbar("hero", instanceId, slot)); PersistCombatInventory(); } }
        public void ActivateInventoryQuickbar(int slot)
        {
            if (state == null || slot < 0 || slot >= state.ItemQuickbar.Length) return; ItemInstance item = state.ItemInventory.Get(state.ItemQuickbar[slot]); if (item == null) return;
            if (ItemCatalog.Get(item.DefinitionId).Category == ItemCategory.Artifact)
            {
                if (item.DefinitionId == "G-T13")
                {
                    armedInventoryItemId = null;
                    state.AddLog("定锚支架已在快捷栏待机；受到推拉时自动抵消并消耗 1 次。");
                    MarkPresentation(UiPresentationArea.Combat); return;
                }
                armedInventoryItemId = item.InstanceId; selectedAction = "技能1"; selectedTargetId = null;
                EnsureArtifactBattle(); state.AddLog("已从快捷栏装载" + ItemCatalog.Get(item.DefinitionId).DisplayName + "；请选择合法目标。");
                MarkPresentation(UiPresentationArea.Combat); return;
            }
            FireSpellDefinition ability = ItemAbilityCatalog.For(item.DefinitionId);
            if (ability == null) { TryCommand(CombatCommand.UseQuickbar("hero", slot)); PersistCombatInventory(); return; }
            armedInventoryItemId = item.InstanceId; selectedAction = "技能1"; selectedTargetId = null; state.AddLog("已从快捷栏装载" + ItemCatalog.Get(item.DefinitionId).DisplayName + "；请选择目标格。"); MarkPresentation(UiPresentationArea.Combat);
        }
        public void NotifyInventoryChanged() { PersistCombatInventory(); MarkPresentation(UiPresentationArea.Combat); }
        private void PersistCombatInventory() { if (mapRun == null || state == null) return; mapRun.CaptureCombatInventory(state); SaveMapRun(); }
        public void ApplyHudBuild(int build) { if (state != null) ApplyBuild(build); }
        public void EndHeroTurn() { if (state != null) TryCommand(CombatCommand.EndTurn("hero"), true); }
        private void Update()
        {
            if (!Application.isPlaying || developerFlow == null || state == null) return;
            if (state.ActiveUnitId != fireLifecycleActiveUnitId)
            {
                fireLifecycleActiveUnitId = state.ActiveUnitId;
                if (!string.IsNullOrEmpty(fireLifecycleActiveUnitId)) fireBattle?.BeginUnitTurn(fireLifecycleActiveUnitId);
                if (!string.IsNullOrEmpty(fireLifecycleActiveUnitId)) { EnsureArtifactBattle(); artifactBattle.BeginUnitTurn(fireLifecycleActiveUnitId); }
            }
            CombatFlowPhase phaseBeforeUpdate = developerFlow.Phase;
            if (!trainingRangeActive && developerFlow.Phase == CombatFlowPhase.Active && !state.IsVictory && !state.IsDefeat && state.ActiveUnitId != "hero") { RunEnemyTurn(); developerFlow.RefreshOutcome(); }
            else if (state.ActiveUnitId == "hero" && enemyTurn.IsRunning) ResetEnemyTurnSequence();
            developerFlow.RefreshOutcome(); HandleRogueliteOutcome();
            if (developerFlow.Phase != phaseBeforeUpdate) { MarkPresentation(UiPresentationArea.Flow); MarkPresentation(UiPresentationArea.Combat); }
        }
        private void HandleRogueliteOutcome()
        {
            if (mapRun != null && !outcomeHandled && developerFlow.Phase == CombatFlowPhase.Victory)
            {
                outcomeHandled = true; visualFeedback?.PlayOutcome(true);
                RogueliteCombatSettlement.TrySettleVictory(mapRun, state);
                MarkPresentation(UiPresentationArea.Settlement);
                MarkPresentation(UiPresentationArea.MapStructure);
                SaveMapRun(); settlementPresentation?.RefreshNow(); return;
            }
            if (!outcomeHandled && developerFlow.Phase == CombatFlowPhase.Defeat)
            {
                outcomeHandled = true; visualFeedback?.PlayOutcome(false);
                // A defeat deliberately leaves the pre-combat map save untouched. The combat snapshot
                // remains available to CombatFlowController for deterministic tactical restart.
            }
            if (rogueliteRun == null || outcomeHandled || developerFlow.Phase != CombatFlowPhase.Victory) return;
            visualFeedback?.PlayOutcome(true);
            outcomeHandled = true;
            string summary = "胜利 | " + rogueliteRun.CurrentMission.TemplateId + " | 种子 " + rogueliteRun.Package.Seed;
            if (rogueliteRun.Kind == RogueliteLaunchKind.TemplateSandbox) return;
            rogueliteRun.Complete(summary);
            if (rogueliteRun.IsShortRun) SaveShortRun();
            else saveGateway.SaveStory(rogueliteRun.Package);
        }
        public void ContinueRogueliteAfterVictory()
        {
            if (mapRun != null && developerFlow.Phase == CombatFlowPhase.Victory) { developerFlow.ReturnToDeveloperMenu(); state = developerFlow.State; rogueliteFlow.ReturnToMap(); RefreshSceneHud(); return; }
            if (rogueliteRun == null || (developerFlow.Phase != CombatFlowPhase.Victory && developerFlow.Phase != CombatFlowPhase.Defeat)) return;
            if (developerFlow.Phase == CombatFlowPhase.Victory && rogueliteRun.IsShortRun) { OpenShortRunPhase(); return; }
            if (developerFlow.Phase == CombatFlowPhase.Victory && rogueliteRun.Kind == RogueliteLaunchKind.StoryChain && !rogueliteRun.Package.IsComplete) { BuildCombatFromSceneStageTwo(); developerFlow.OpenBriefing(); }
            else { developerFlow.ReturnToDeveloperMenu(); state = developerFlow.State; rogueliteFlow.OpenRogueliteMenu(); RefreshSceneHud(); }
        }
        public void ForceCurrentOutcome(bool victory)
        {
            if (!DeveloperBuildGate.IsEnabled) return;
            if (developerFlow?.Phase != CombatFlowPhase.Active) return;
            state.ResolveDebugOutcome(victory); developerFlow.RefreshOutcome(); HandleRogueliteOutcome(); MarkPresentation(UiPresentationArea.Flow); MarkPresentation(UiPresentationArea.Combat);
        }
        private void RefreshSceneHud()
        {
            // The authored HUD is retired in favour of FormalCombatHud; keep it inert during the transition.
            Transform sceneUi = transform.Find("场景UI");
            if (sceneUi == null || !sceneUi.gameObject.activeInHierarchy) return;
            TacticalHudSceneBinder binder = sceneUi.GetComponent<TacticalHudSceneBinder>();
            if (binder != null) binder.RefreshNow();
        }
        private void RunEnemyTurn()
        {
            float now = Time.unscaledTime;
            UnitState enemy = state.GetUnit(state.ActiveUnitId);
            UnitState hero = state.GetUnit("hero");
            EnemyTurnAdvance advance = enemyTurn.Advance(enemy, now, unit => BuildEnemyCommand(unit, hero));
            try
            {
                if (advance.Kind == EnemyTurnAdvanceKind.BeginAction)
                {
                    visualFeedback?.BeginEnemyAction(enemy, EnemyIntent(enemy),
                        EnemyTurnSequence.FocusSeconds + EnemyTurnSequence.ResultHoldFor(advance.CommandType));
                    MarkPresentation(UiPresentationArea.Combat);
                }
                else if (advance.Kind == EnemyTurnAdvanceKind.ResolveCommand && advance.Command.HasValue)
                {
                    TryCommand(advance.Command.Value);
                    MarkPresentation(UiPresentationArea.Combat);
                }
                else if (advance.Kind == EnemyTurnAdvanceKind.EndAction)
                {
                    if (state.ActiveUnitId == enemy.Id) PublishCombatEffects(CombatResolver.EndTurn(state, enemy));
                    visualFeedback?.CompleteEnemyAction(enemy.Id);
                    MarkPresentation(UiPresentationArea.Combat);
                }
                else if (advance.Kind == EnemyTurnAdvanceKind.ReadyForNext)
                    MarkPresentation(UiPresentationArea.Combat);
                else if (advance.Kind == EnemyTurnAdvanceKind.InvalidActor)
                {
                    visualFeedback?.CancelEnemyAction();
                    if (enemy != null) PublishCombatEffects(CombatResolver.EndTurn(state, enemy));
                }
                else if (advance.Kind == EnemyTurnAdvanceKind.ActorChanged)
                    visualFeedback?.CancelEnemyAction();
            }
            catch (InvalidOperationException error)
            {
                state.AddLog(error.Message);
                if (enemy != null && state.ActiveUnitId == enemy.Id) PublishCombatEffects(CombatResolver.EndTurn(state, enemy));
                if (enemy != null) visualFeedback?.CompleteEnemyAction(enemy.Id);
                enemyTurn.Reset();
                MarkPresentation(UiPresentationArea.Combat);
            }
        }

        private void ResetEnemyTurnSequence()
        {
            enemyTurn.Reset();
            visualFeedback?.CancelEnemyAction();
        }

        private CombatCommand BuildEnemyCommand(UnitState enemy, UnitState hero)
            => enemyPlans.GetExecutionCommand(state, enemy, hero);
        private string FloorKeyForCurrentLevel(int x, int y)
        {
            if (currentLevel == null)
                return y == 0 || y == state.Map.Height - 1 ? "rail_horizontal" :
                    (x == 5 || x == 6) && y >= 3 && y <= 5 ? "floor_warning" : "floor_industrial";
            switch (currentLevel.FloorTheme)
            {
                case FirstRegionFloorTheme.StoneRoad: return y == 4 ? "floor_industrial" : "floor_plain";
                case FirstRegionFloorTheme.Courtyard: return (x + y) % 5 == 0 ? "floor_industrial" : "floor_plain";
                case FirstRegionFloorTheme.Ruins: return (x * 3 + y * 5) % 11 == 0 ? "floor_plain" : "floor_industrial";
                case FirstRegionFloorTheme.AetherMarked: return x == 6 || y == 4 ? "floor_industrial" : "floor_plain";
                default: return "floor_plain";
            }
        }
        private void FocusHeroInBattlefield()
        {
            UnitState hero = state?.GetUnit("hero");
            if (hero == null) return;
            if (battlefieldViewport == null) battlefieldViewport = battlefield.CreateViewport(state.Map.Width, state.Map.Height);
            battlefieldViewport.Focus(hero.Position);
        }

        private void FollowHeroAtSafeEdge()
        {
            UnitState hero = state?.GetUnit("hero");
            if (hero != null && battlefieldViewport != null && battlefieldViewport.IsNearSafeEdge(hero.Position))
                battlefieldViewport.Focus(hero.Position);
        }

        public CombatTargetDamageForecast TargetDamageForecast(UnitState enemy)
        {
            if (enemy == null || enemy.IsHero || !enemy.IsAlive || state == null || state.ActiveUnitId != "hero") return null;
            try
            {
                if (selectedAction == "攻击")
                {
                    if (!string.IsNullOrEmpty(battlefield.InvalidReasonForCell(state, selectedAction, enemy.Position))) return null;
                    if (fireBattle == null || fireBattle.Combat != state) fireBattle = new FireBattleState(state);
                    return CombatTargetDamageForecaster.WeaponAttack(fireBattle, "hero", enemy.Id);
                }

                int slot = selectedAction == "技能1" ? 0 : selectedAction == "技能2" ? 1 : -1;
                if (slot < 0) return null;
                FireSpellDefinition fireSpell = FireSpellInSlot(slot);
                if (fireSpell != null)
                {
                    if (fireBattle == null || fireBattle.Combat != state) fireBattle = new FireBattleState(state);
                    Facing facing = FacingToward(state.GetUnit("hero").Position, enemy.Position);
                    FireSpellTarget target = FireSpellTarget.Unit(enemy.Id, facing);
                    FireSpellPreview preview = FireSpellEngine.Preview(fireBattle, "hero", fireSpell, target);
                    bool canDamage = fireSpell.Rules.Any(rule => rule.Kind == FireRuleKind.Damage ||
                        rule.Kind == FireRuleKind.WeaponDamage || rule.Kind == FireRuleKind.Push);
                    return preview.CanCommit && canDamage
                        ? CombatTargetDamageForecaster.FireSpell(fireBattle, "hero", fireSpell, target, enemy.Id)
                        : null;
                }

                ArtifactDefinition artifact = CurrentArmedInventoryItem != null &&
                    ItemCatalog.Get(CurrentArmedInventoryItem.DefinitionId).Category == ItemCategory.Artifact
                    ? ArtifactCatalog.Get(CurrentArmedInventoryItem.DefinitionId)
                    : CurrentTrainingRangeArtifact;
                if (slot == 0 && artifact != null) return null;

                SkillDefinition skill = slot == 0 ? state.GetUnit("hero").SkillOne : state.GetUnit("hero").SkillTwo;
                if (skill == null || skill.Damage <= 0 ||
                    !string.IsNullOrEmpty(battlefield.InvalidReasonForCell(state, selectedAction, enemy.Position))) return null;
                return CombatTargetDamageForecaster.Skill(state,
                    CombatCommand.UseSkill("hero", slot, enemy.Id), enemy.Id);
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        private void HandleInspectionClick(GridPosition position)
        {
            targetNavigation.End();
            string nextTargetId = CombatInformationPresenter.EnemyInspectionTargetAt(state, position);
            if (selectedTargetId == nextTargetId) return;
            selectedTargetId = nextTargetId;
            MarkPresentation(UiPresentationArea.Combat);
            if (!string.IsNullOrEmpty(nextTargetId)) PublishUiVisual(new UiVisualEvent(UiVisualEventKind.CombatTargetConfirmed, nextTargetId));
        }

        private void ApplyBuild(int build)
        {
            UnitState hero = state.GetUnit("hero");
            StageTwoBuilds.Apply(hero, build);
            state.AddLog($"\u5de5\u574a\u5df2\u5207\u6362\u4e3a{hero.MainHand.DisplayName}\u6784\u7b51\u3002");
        }

        private void HandleCellClick(GridPosition p)
        {
            targetNavigation.End();
            UnitState clickedUnit = state.Units.Values.FirstOrDefault(unit => unit.IsAlive && unit.Position == p);
            UnitState enemy = clickedUnit != null && !clickedUnit.IsHero ? clickedUnit : null;
            int fireSlot = selectedAction == "技能1" ? 0 : selectedAction == "技能2" ? 1 : -1;
            FireSpellDefinition fireSpell = fireSlot < 0 ? null : FireSpellInSlot(fireSlot);
            if (fireSpell != null)
            {
                TryFireSpellCell(fireSpell, clickedUnit, p);
                return;
            }
            ArtifactDefinition artifact = CurrentArmedInventoryItem != null && ItemCatalog.Get(CurrentArmedInventoryItem.DefinitionId).Category == ItemCategory.Artifact
                ? ArtifactCatalog.Get(CurrentArmedInventoryItem.DefinitionId) : CurrentTrainingRangeArtifact;
            if (selectedAction == "技能1" && artifact != null) { TryArtifactCell(artifact, clickedUnit, p); return; }
            string invalidReason = battlefield.InvalidReasonForCell(state, selectedAction, p);
            if (!string.IsNullOrEmpty(invalidReason))
            {
                PublishUiVisual(new UiVisualEvent(UiVisualEventKind.CombatCommandRejected, selectedAction, message: invalidReason));
                return;
            }
            if (selectedAction == "\u79fb\u52a8") TryCommand(CombatCommand.Move("hero", p, FacingToward(state.GetUnit("hero").Position, p)));
            else if (selectedAction == "\u653b\u51fb" && enemy != null) TryCommand(CombatCommand.Attack("hero", enemy.Id));
            else if (selectedAction == "\u6280\u80fd1") TrySkillCell(0, state.GetUnit("hero").SkillOne, clickedUnit, p);
            else if (selectedAction == "\u6280\u80fd2") TrySkillCell(1, state.GetUnit("hero").SkillTwo, clickedUnit, p);
            else if (selectedAction == "\u641c\u522e") TryCommand(CombatCommand.Loot("hero"));
            else if (selectedAction == "\u4e92\u52a8") TryCommand(CombatCommand.Interact("hero", p));
        }
        private void EnsureArtifactBattle()
        {
            if (artifactBattle == null || artifactBattle.Combat != state) artifactBattle = new ArtifactBattleState(state);
        }
        private bool BuildArtifactTarget(ArtifactDefinition artifact, GridPosition position, out ArtifactTarget target)
        {
            UnitState unit = state.Units.Values.FirstOrDefault(candidate => candidate.IsAlive && candidate.Position == position);
            if (artifact.TargetRule == ArtifactTargetRule.TwoAllies && unit != null)
            { target = ArtifactTarget.Pair(unit.Id, "hero", position); return unit.Id != "hero"; }
            target = unit == null ? ArtifactTarget.At(position) : ArtifactTarget.Unit(unit.Id, position); return true;
        }
        private void TryArtifactCell(ArtifactDefinition artifact, UnitState clickedUnit, GridPosition position)
        {
            EnsureArtifactBattle();
            if (!BuildArtifactTarget(artifact, position, out ArtifactTarget target)) return;
            GridPosition source = state.GetUnit("hero").Position;
            int uses = CurrentArmedInventoryItem?.RemainingUses ?? trainingRangeArtifactUsesRemaining;
            ArtifactPreview preview = ArtifactEngine.Preview(artifactBattle, "hero", artifact, target, uses);
            if (!preview.CanCommit)
            {
                string reason = string.Join("；", preview.Failures); state.AddLog(reason);
                PublishUiVisual(new UiVisualEvent(UiVisualEventKind.CombatCommandRejected, artifact.Id, message: reason));
                MarkPresentation(UiPresentationArea.Combat); return;
            }
            ArtifactExecution execution;
            if (CurrentArmedInventoryItem != null)
            {
                string instanceId = CurrentArmedInventoryItem.InstanceId;
                execution = ArtifactEngine.ExecuteInventory(artifactBattle, "hero", instanceId, target);
                if (state.ItemInventory.Get(instanceId) == null) armedInventoryItemId = null;
                PersistCombatInventory();
            }
            else { execution = ArtifactEngine.Execute(artifactBattle, "hero", artifact, target, uses); trainingRangeArtifactUsesRemaining--; }
            state.AddLog(artifact.DisplayName + "：产生 " + execution.Steps.Count + " 项结果");
            selectedTargetId = null; MarkPresentation(UiPresentationArea.Combat);
            PublishUiVisual(new UiVisualEvent(UiVisualEventKind.CombatCommandSubmitted, artifact.Id));
            visualFeedback?.NotifyArtifact(artifact, source, preview.Cells, execution);
            developerFlow.RefreshOutcome();
        }
        private void TryFireSpellCell(FireSpellDefinition spell, UnitState clickedUnit, GridPosition position)
        {
            if (fireBattle == null || fireBattle.Combat != state) fireBattle = new FireBattleState(state);
            if (trainingRangeSession?.CurrentArtifact != null && trainingRangeArtifactUsesRemaining <= 0)
            {
                const string depleted = "法宝封装次数已耗尽；请打开靶场配置并重新装载。";
                state.AddLog(depleted);
                PublishUiVisual(new UiVisualEvent(UiVisualEventKind.CombatCommandRejected, spell.Id, message: depleted));
                MarkPresentation(UiPresentationArea.Combat);
                return;
            }
            Facing facing = FacingToward(state.GetUnit("hero").Position, position);
            FireSpellTarget target = clickedUnit == null ? FireSpellTarget.At(position, facing) : FireSpellTarget.Unit(clickedUnit.Id, facing);
            FireSpellPreview preview = FireSpellEngine.Preview(fireBattle, "hero", spell, target);
            selectedTargetId = clickedUnit?.Id;
            if (!preview.CanCommit)
            {
                string reason = string.Join("；", preview.Failures); state.AddLog(reason);
                PublishUiVisual(new UiVisualEvent(UiVisualEventKind.CombatCommandRejected, spell.Id, message: reason)); MarkPresentation(UiPresentationArea.Combat); return;
            }
            GridPosition source = state.GetUnit("hero").Position;
            FireSpellExecution execution = FireSpellEngine.Execute(fireBattle, "hero", spell, target);
            if (trainingRangeSession?.CurrentArtifact != null) trainingRangeArtifactUsesRemaining--;
            if (!trainingRangeActive && !string.IsNullOrEmpty(armedInventoryItemId))
            {
                string usedId = armedInventoryItemId; state.ConsumeInventoryItem(usedId); if (state.ItemInventory.Get(usedId) == null) armedInventoryItemId = null; PersistCombatInventory();
            }
            if (trainingRangeActive) trainingRangeSession?.RecordExternal(preview, execution);
            state.AddLog(spell.DisplayName + "：产生 " + execution.Steps.Count + " 项结果");
            selectedTargetId = null; MarkPresentation(UiPresentationArea.Combat);
            PublishUiVisual(new UiVisualEvent(UiVisualEventKind.CombatCommandSubmitted, spell.Id));
            visualFeedback?.NotifyFireSpell(spell, source, preview.Cells);
            developerFlow.RefreshOutcome();
        }

        private void TrySkillCell(int slot, SkillDefinition skill, UnitState clickedUnit, GridPosition position)
        {
            if (!battlefield.IsSkillTargetInRange(state, skill, position))
            {
                PublishUiVisual(new UiVisualEvent(UiVisualEventKind.CombatCommandRejected, skill == null ? "技能" : skill.DisplayName, message: "目标超出有效范围"));
                return;
            }
            if (skill.TargetRule == SkillTargetRule.GridCell || skill.TargetRule == SkillTargetRule.Destructible)
                TryCommand(CombatCommand.UseSkillAt("hero", slot, position, FacingToward(state.GetUnit("hero").Position, position)));
            else if (skill.TargetRule == SkillTargetRule.Self)
                TryCommand(CombatCommand.UseSkill("hero", slot, null));
            else if (clickedUnit != null)
                TryCommand(CombatCommand.UseSkill("hero", slot, clickedUnit.Id));
        }
        private void TryCommand(CombatCommand command, bool explicitHeroEndTurn = false)
        {
            CombatCommandExecutionResult result = commandExecution.Execute(state, fireBattle, command, explicitHeroEndTurn);
            fireBattle = result.FireBattle;
            if (!result.Accepted)
            {
                state.AddLog(result.RejectionReason);
                PublishUiVisual(new UiVisualEvent(UiVisualEventKind.CombatCommandRejected,
                    command.Type.ToString(), message: result.RejectionReason));
                return;
            }
            if (!string.IsNullOrEmpty(result.ActionResult)) state.AddLog(result.ActionResult);
            if (trainingRangeActive && result.DeliveredSkill != null) trainingRangeSession?.RecordExternal(result.Execution);
            PublishFireExecutions(result.MovementFireExecutions);
            if (result.HeroMoved) FollowHeroAtSafeEdge();
            selectedTargetId = null;
            enemyPlans.Invalidate();
            MarkPresentation(UiPresentationArea.Combat);
            PublishUiVisual(new UiVisualEvent(UiVisualEventKind.CombatCommandSubmitted, command.Type.ToString()));
            PublishCombatEffects(result.Execution);
            PublishFireExecutions(result.AttackFireExecutions);
            visualFeedback?.NotifySkillDelivery(result.DeliveredSkill, result.DeliverySource, result.DeliveryTarget);
            developerFlow.RefreshOutcome();
        }

        private void PublishFireExecutions(IEnumerable<FireSpellExecution> executions)
        {
            if (executions == null) return;
            foreach (FireSpellExecution execution in executions)
            {
                if (execution == null) continue;
                FireSpellDefinition spell = execution.Preview?.Spell;
                FireSpellResultStep firstStep = execution.Steps.FirstOrDefault();
                UnitState stepTarget = string.IsNullOrEmpty(firstStep.TargetId) ? null : state.GetUnit(firstStep.TargetId);
                if (spell != null) visualFeedback?.NotifyFireSpell(spell,
                    stepTarget?.Position ?? execution.Preview.Cells.FirstOrDefault(), execution.Preview.Cells);
                state.AddLog((spell?.DisplayName ?? "火术触发") + "：产生 " + execution.Steps.Count + " 项结果");
            }
        }
        private void PublishCombatEffects(CombatEffectExecution execution)
        {
            if (visualFeedback == null || execution == null) return;
            foreach (CombatEffectResult result in execution.Results)
            {
                UnitState source = state.GetUnit(result.SourceUnitId);
                GridPosition sourcePosition = source == null ? result.PositionBefore : source.Position;
                if (result.Kind == CombatEffectKind.Move && result.Changed)
                    visualFeedback.NotifyMovement(result.PositionBefore, result.PositionAfter);
                else if (result.Kind == CombatEffectKind.AbsorbShield && result.AppliedAmount > 0)
                    visualFeedback.Publish(new CombatFeedbackEvent(CombatFeedbackKind.ShieldAbsorb, sourcePosition, result.PositionAfter, result.AppliedAmount));
                else if (result.Kind == CombatEffectKind.DamageHealth && result.AppliedAmount > 0)
                {
                    visualFeedback.Publish(new CombatFeedbackEvent(CombatFeedbackKind.Damage, sourcePosition, result.PositionAfter, result.AppliedAmount));
                    if (result.ValueBefore > 0 && result.ValueAfter == 0)
                        visualFeedback.Publish(new CombatFeedbackEvent(CombatFeedbackKind.UnitDefeated, sourcePosition, result.PositionAfter));
                }
                else if (result.Kind == CombatEffectKind.RestoreHealth && result.AppliedAmount > 0)
                    visualFeedback.Publish(new CombatFeedbackEvent(CombatFeedbackKind.Healing, result.PositionAfter, result.AppliedAmount));
                else if (result.Kind == CombatEffectKind.RestoreShield && result.AppliedAmount > 0)
                    visualFeedback.Publish(new CombatFeedbackEvent(CombatFeedbackKind.ShieldRestore, result.PositionAfter, result.AppliedAmount));
                else if (result.Kind == CombatEffectKind.RestoreMana && result.AppliedAmount > 0)
                    visualFeedback.Publish(new CombatFeedbackEvent(CombatFeedbackKind.ManaRestore, result.PositionAfter, result.AppliedAmount));
                else if (result.Kind == CombatEffectKind.ApplyStatus && result.AppliedAmount > 0)
                    visualFeedback.NotifyStatusApplied(result.PositionAfter, result.Status, result.ValueAfter);
                else if (result.Kind == CombatEffectKind.ClearStatus && result.AppliedAmount > 0)
                    visualFeedback.Publish(new CombatFeedbackEvent(CombatFeedbackKind.StatusCleared, result.PositionAfter));
                else if (result.Kind == CombatEffectKind.DamageObject && result.AppliedAmount > 0)
                    visualFeedback.NotifyDestructible(result.PositionAfter, state.Map.GetTile(result.PositionAfter));
            }
        }
        public static bool CanSubmitTurnCommand(CombatCommand command, bool explicitHeroEndTurn) =>
            CombatCommandExecutionService.CanSubmit(command, explicitHeroEndTurn);

        private string GetRangeDescription() { int count = 0; if (state != null) for (int y = 0; y < state.Map.Height; y++) for (int x = 0; x < state.Map.Width; x++) if (IsInSelectedRange(new GridPosition(x, y))) count++; string rule = selectedAction == "\u79fb\u52a8" ? "\u79fb\u52a8\u8303\u56f4：3 \u683c" : selectedAction == "\u653b\u51fb" ? "\u653b\u51fb\u8303\u56f4：4 \u683c" : selectedAction == "\u65bd\u672f" ? "\u706b\u672f\u8303\u56f4：5 \u683c" : selectedAction == "\u4e92\u52a8" ? "\u4e92\u52d5\u8303\u56f4：1 \u683c" : "\u9053\u5177：\u81ea\u8eab\u4f7f\u7528"; return rule + "  |  \u9ad8\u4eae " + count + " \u683c"; }
        private bool IsInSelectedRange(GridPosition p)
        {
            int slot = selectedAction == "技能1" ? 0 : selectedAction == "技能2" ? 1 : -1;
            FireSpellDefinition spell = slot < 0 ? null : FireSpellInSlot(slot);
            if (spell != null)
            {
                if (fireBattle == null || fireBattle.Combat != state) fireBattle = new FireBattleState(state);
                return IsFireSpellCellValid(spell, p);
            }
            return battlefield.IsInSelectedRange(state, selectedAction, p);
        }
        private bool IsSkillTargetInRange(SkillDefinition skill, GridPosition position) => battlefield.IsSkillTargetInRange(state, skill, position);
        private bool IsInMoveRange(GridPosition p) => battlefield.IsInMoveRange(state, p);
        private bool IsInAttackRange(GridPosition p) => battlefield.IsInAttackRange(state, p);
        private static int Distance(GridPosition a, GridPosition b) => BattlefieldPresentationAdapter.Distance(a, b);
        private static GridPosition StepToward(GridPosition a, GridPosition b) => BattlefieldPresentationAdapter.StepToward(a, b);
        private static Facing FacingToward(GridPosition a, GridPosition b) => BattlefieldPresentationAdapter.FacingToward(a, b);
    }
}
