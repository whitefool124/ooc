using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    [ExecuteAlways]
    public sealed class CombatPrototypeBootstrap : MonoBehaviour
    {
        private const float UiWidth = 1920f;
        private const float UiHeight = 1080f;
        private readonly BattlefieldPresentationAdapter battlefield = new BattlefieldPresentationAdapter();
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
        private readonly Dictionary<StatusType, Texture2D> formalStatusTextures = new Dictionary<StatusType, Texture2D>();
        private readonly Texture2D[] formalFiregroundFrames = new Texture2D[6];
        private readonly Texture2D[] formalSmokeFrames = new Texture2D[6];
        private readonly RogueliteSaveGateway saveGateway = new RogueliteSaveGateway(new PlayerPrefsRogueliteSaveStore());
        private CombatVisualFeedback visualFeedback;
        private FireBattleState fireBattle;
        private ArtifactBattleState artifactBattle;
        private string fireLifecycleActiveUnitId;
        private RogueliteSettlementPresentation settlementPresentation;
        private FormalCombatHud formalCombatHud;
        private FormalRogueliteUi formalRogueliteUi;
        private FormalUiInteractionLayer interactionLayer;
        private FormalStartupPresentation startupPresentation;
        private DeveloperConsolePanel developerConsole;
        private TarkovInventoryPanel inventoryPanel;
        private TrainingRangeSession trainingRangeSession;
        private bool trainingRangeActive;
        private int trainingRangeArtifactUsesRemaining;
        private string armedInventoryItemId;
        private RogueliteUiPreferences uiPreferences = new RogueliteUiPreferences();
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
            visualFeedback = gameObject.AddComponent<CombatVisualFeedback>(); visualFeedback.Initialize(this);
            interactionLayer = gameObject.AddComponent<FormalUiInteractionLayer>(); interactionLayer.Initialize(this);
            settlementPresentation = gameObject.AddComponent<RogueliteSettlementPresentation>(); settlementPresentation.Initialize(this);
            formalCombatHud = gameObject.AddComponent<FormalCombatHud>(); formalCombatHud.Initialize(this);
            formalRogueliteUi = gameObject.AddComponent<FormalRogueliteUi>(); formalRogueliteUi.Initialize(this);
            startupPresentation = gameObject.AddComponent<FormalStartupPresentation>(); startupPresentation.Initialize(this);
            developerConsole = gameObject.AddComponent<DeveloperConsolePanel>(); developerConsole.Initialize(this);
            inventoryPanel = gameObject.AddComponent<TarkovInventoryPanel>(); inventoryPanel.Initialize(this);
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
            AddUiPanel(canvasObject, "标题栏", new Vector2(16, -16), new Vector2(640, 44), "OCC // \u786e\u5b9a\u6027\u6218\u6597\u539f\u578b", 18);
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
            formalStatusTextures[StatusType.Burning] = RequiredTexture(FormalArtRegistry.StatusPath("burning"));
            formalStatusTextures[StatusType.Slow] = RequiredTexture(FormalArtRegistry.StatusPath("slow"));
            formalStatusTextures[StatusType.Bound] = RequiredTexture(FormalArtRegistry.StatusPath("bound"));
            formalStatusTextures[StatusType.ArmorBreak] = RequiredTexture(FormalArtRegistry.StatusPath("armor_break"));
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

        private static Rect StaticUnitPresentationRect(UnitState unit, Rect rect)
        {
            float phase = unit.IsHero ? 0f : unit.Position.X * .71f + unit.Position.Y * .37f;
            int offsetY = Mathf.RoundToInt(Mathf.Sin(Time.unscaledTime * 1.8f + phase));
            return new Rect(rect.x, rect.y + offsetY, rect.width, rect.height);
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
            outcomeHandled = false;
        }

        private static GridPosition ScenePosition(CombatSceneMarker marker) => new GridPosition(Mathf.RoundToInt(marker.transform.position.x), Mathf.RoundToInt(marker.transform.position.y));
        public void OpenDeveloperBriefing() { developerFlow.OpenBriefing(); MarkPresentation(UiPresentationArea.Flow); }
        public void StartDeveloperCombat() { developerFlow.BeginCombat(); state = developerFlow.State; fireBattle = new FireBattleState(state); fireLifecycleActiveUnitId = null; visualFeedback?.ResetBattleFeedback(); PublishCombatEffects(CombatResolver.BeginTurn(state, "hero")); RefreshSceneHud(); MarkPresentation(UiPresentationArea.Flow); MarkPresentation(UiPresentationArea.Combat); }
        public void TacticalRestartDeveloperCombat()
        {
            if (trainingRangeActive) { PrepareTrainingRangeCurrent(); return; }
            developerFlow.TacticalRestart(); state = developerFlow.State; fireBattle = new FireBattleState(state); fireLifecycleActiveUnitId = null; visualFeedback?.ResetBattleFeedback(); PublishCombatEffects(CombatResolver.BeginTurn(state, "hero")); developerFlow.ResumeAfterRestart(); RefreshSceneHud(); MarkPresentation(UiPresentationArea.Combat);
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
                    return false;
                }
            }
            else run = new RogueliteMapRun(UnityEngine.Random.Range(1, int.MaxValue), starterId);
            rogueliteFlow.BeginMapRun(run);
            if (!continueSave) saveGateway.SaveMapRun(run);
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
                    "新开推进会替换当前肉鸽地图存档。已完成的本局进度无法从该槽位恢复。", "覆盖并新开"), () =>
                    {
                        if (!saveGateway.DeleteMapRun())
                        {
                            ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Rejected, "旧地图存档无法删除，未启动新推进"));
                            return;
                        }
                        StartMapRoguelite(false, starterId);
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
        private void SaveMapRun() => saveGateway.SaveMapRun(mapRun);
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
        public string CurrentLevelId => currentLevel?.Id;
        public FireBattleState CurrentFireBattle => fireBattle;
        public ArtifactBattleState CurrentArtifactBattle => artifactBattle;
        public string SelectedAction => selectedAction;
        public string SelectedTargetId => selectedTargetId;
        public CombatActionPreview CurrentActionPreview => BuildActionPreview(selectedAction);
        public CombatActionPreview ActionPreview(string action) => BuildActionPreview(action);
        public CombatOutcomePresentation CurrentOutcomePresentation => state == null ? null : CombatInformationPresenter.BuildOutcome(state, mapRun != null);
        public string CurrentPhaseText => CombatInformationPresenter.PhaseText(CurrentFlowPhase, state);
        public EnemyIntentPresentation EnemyIntent(UnitState enemy) => enemy == null || state == null ? null : CombatInformationPresenter.BuildEnemyIntent(state, enemy, state.GetUnit("hero"));
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
            if (spell == null || state == null) return battlefield.BuildPreview(state, action, selectedTargetId);
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
            string effects = string.Join(" → ", spell.Rules.Select(rule => rule.Kind + (rule.Amount > 0 ? " " + rule.Amount : string.Empty)));
            string contract = spell.CombatAffinity + " / " + spell.DeliveryMode + " / " + spell.WeaponRequirement +
                " / " + spell.TargetKind + " / " + spell.Shape + " / " + spell.TriggerWindow + " / " + spell.ConsumptionRule;
            return new CombatActionPreview(action, contract, spell.ActionPointCost + " AP + " + spell.ManaCost + " 魔力", effects, valid, failure);
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
        public void ToggleDeveloperConsole() { developerConsole?.Toggle(); }
        public void StartTrainingRange()
        {
            startupPresentation?.DismissImmediately();
            rogueliteFlow.Reset(); trainingRangeActive = true;
            if (trainingRangeSession == null) trainingRangeSession = new TrainingRangeSession();
            PrepareTrainingRangeCurrent();
        }
        public void SelectTrainingRangeAbility(string abilityId)
        {
            if (trainingRangeSession == null) trainingRangeSession = new TrainingRangeSession();
            trainingRangeSession.Select(abilityId); PrepareTrainingRangeCurrent();
        }
        public void BrowseTrainingRangeAbility(string abilityId)
        {
            if (trainingRangeSession == null) trainingRangeSession = new TrainingRangeSession();
            trainingRangeSession.Select(abilityId);
        }
        public void ShiftTrainingRangePage(int delta)
        {
            if (trainingRangeSession == null) trainingRangeSession = new TrainingRangeSession();
            trainingRangeSession.ShiftPage(delta);
        }
        public void PrepareTrainingRangeCurrent()
        {
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
            visualFeedback?.ResetBattleFeedback(); RefreshSceneHud(); MarkPresentation(UiPresentationArea.Flow); MarkPresentation(UiPresentationArea.Combat);
        }
        public TrainingRangePreviewReport PreviewTrainingRangeCurrent()
        {
            TrainingRangePreviewReport report = trainingRangeSession.PreviewCurrent();
            state.AddLog(trainingRangeSession.CurrentAbility.Id + " // " + report.Summary); MarkPresentation(UiPresentationArea.Combat); return report;
        }
        public TrainingRangeExecutionReport ExecuteTrainingRangeCurrent()
        {
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
            if (trainingRangeSession == null) trainingRangeSession = new TrainingRangeSession();
            TrainingRangeAuditReport report = trainingRangeSession.RunFullAudit();
            state?.AddLog(report.Summary); MarkPresentation(UiPresentationArea.Combat); return report;
        }
        public void RequestTacticalRestart()
        {
            RequestConfirmation(new UiConfirmationRequest(UiConfirmationKind.TacticalRestart, "战术重开？",
                "当前战斗进度将被放弃，并恢复到本场战斗开始时的确定性快照。", "确认重开"), TacticalRestartDeveloperCombat);
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
            saveGateway.SaveUiPreferences(uiPreferences);
            ApplyUiPreferences();
            MarkPresentation(UiPresentationArea.Settings);
        }
        private void ApplyUiPreferences()
        {
            AudioListener.volume = uiPreferences.MasterVolume;
        }
        public void SelectHudAction(string action)
        {
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
        public void EndHeroTurn() { if (state != null) TryCommand(CombatCommand.EndTurn("hero")); }
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
            UnitState enemy = state.GetUnit(state.ActiveUnitId);
            UnitState hero = state.GetUnit("hero");
            if (enemy == null || !enemy.IsAlive) { if (enemy != null) PublishCombatEffects(CombatResolver.EndTurn(state, enemy)); return; }
            try
            {
                if (enemy.ActionPoints > 0) TryCommand(BuildEnemyCommand(enemy, hero));
                if (state.ActiveUnitId == enemy.Id) PublishCombatEffects(CombatResolver.EndTurn(state, enemy));
            }
            catch (InvalidOperationException error) { state.AddLog(error.Message); PublishCombatEffects(CombatResolver.EndTurn(state, enemy)); }
            MarkPresentation(UiPresentationArea.Combat);
        }

        private CombatCommand BuildEnemyCommand(UnitState enemy, UnitState hero)
            => CombatInformationPresenter.BuildEnemyIntent(state, enemy, hero).Command;
        private void OnGUI() { if (!Application.isPlaying || developerFlow == null) return; float scale = Mathf.Min(Screen.width / UiWidth, Screen.height / UiHeight); Vector2 offset = new Vector2((Screen.width - UiWidth * scale) * .5f, (Screen.height - UiHeight * scale) * .5f); Matrix4x4 previous = GUI.matrix; GUI.matrix = Matrix4x4.TRS(offset, Quaternion.identity, Vector3.one * scale); ConfigureGuiSkin(); if (developerFlow.Phase == CombatFlowPhase.DeveloperMenu || developerFlow.Phase == CombatFlowPhase.Briefing || (mapRun != null && mapRun.AwaitingReward)) { GUI.matrix = previous; return; } BattlefieldRect board = battlefield.BoardRect(state.Map.Width, state.Map.Height); DrawGrid(new Rect(board.X, board.Y, board.Width, board.Height)); GUI.matrix = previous; }
        private void ConfigureGuiSkin()
        {
            GUI.skin.font = chineseFont != null ? chineseFont : FormalUiKit.Font;
            GUI.skin.label.fontSize = 18; GUI.skin.button.fontSize = 18; GUI.skin.box.fontSize = 20;
            GUI.skin.button.padding = new RectOffset(12, 12, 8, 8);
            GUI.skin.box.normal.textColor = new Color(.88f, .94f, 1f);
        }
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
        private void DrawGrid(Rect board)
        {
            Event e = Event.current;
            UnitState hoveredEnemy = null;
            for (int y = 0; y < state.Map.Height; y++) for (int x = 0; x < state.Map.Width; x++)
            {
                GridPosition p = new GridPosition(x, y);
                BattlefieldRect cellContract = battlefield.CellRect(new BattlefieldRect(board.x, board.y, board.width, board.height), state.Map.Height, p);
                Rect cell = new Rect(cellContract.X, cellContract.Y, cellContract.Width, cellContract.Height);
                TileState tile = state.Map.GetTile(p);
                GUI.color = Color.white;
                string floorKey = FloorKeyForCurrentLevel(x, y);
                GUI.DrawTexture(cell, formalRelayTextures[floorKey], ScaleMode.StretchToFill, true);
                int environmentFrame = Mathf.FloorToInt(Time.unscaledTime * 8f) % 6;
                if (fireBattle?.HasFireground(p) == true)
                {
                    GUI.color = Color.white;
                    GUI.DrawTexture(cell, formalFiregroundFrames[environmentFrame], ScaleMode.StretchToFill, true);
                }
                else if (tile.SmokeExpiresAt > state.CurrentTime)
                {
                    GUI.color = Color.white;
                    GUI.DrawTexture(cell, formalSmokeFrames[environmentFrame], ScaleMode.StretchToFill, true);
                }

                // Both core tactical ranges stay visible during the hero turn.
                if (IsInMoveRange(p))
                {
                    GUI.color = new Color(1f, 1f, 1f, selectedAction == "\u79fb\u52a8" ? 1f : .45f);
                    GUI.DrawTexture(cell, formalOverlayTextures["move_range"], ScaleMode.StretchToFill, true);
                }
                if (IsInAttackRange(p))
                {
                    GUI.color = new Color(1f, 1f, 1f, selectedAction == "\u653b\u51fb" ? 1f : .65f);
                    GUI.DrawTexture(cell, formalOverlayTextures["attack_range"], ScaleMode.StretchToFill, true);
                }
                int selectedFireSlot = selectedAction == "\u6280\u80fd1" ? 0 : selectedAction == "\u6280\u80fd2" ? 1 : -1;
                FireSpellDefinition selectedFireSpell = selectedFireSlot < 0 ? null : FireSpellInSlot(selectedFireSlot);
                FireSpellPreview fireCellPreview = selectedFireSpell == null ? null : BuildFireSpellPreviewAt(selectedFireSpell, p);
                if (fireCellPreview?.CanCommit == true)
                {
                    GUI.color = Color.white;
                    GUI.DrawTexture(cell, formalOverlayTextures[fireCellPreview.FriendlyFireRisk ? "high_risk" : "attack_range"], ScaleMode.StretchToFill, true);
                }

                GUI.color = Color.white;
                UnitState unit = state.Units.Values.FirstOrDefault(u => u.IsAlive && u.Position == p);
                if (unit != null)
                {
                    if (!unit.IsHero && cell.Contains(e.mousePosition)) hoveredEnemy = unit;
                    if (unit.Id == selectedTargetId) GUI.DrawTexture(cell, formalOverlayTextures["selected"], ScaleMode.StretchToFill, true);
                    Texture2D unitTexture = FormalUnitTexture(unit);
                    if (unitTexture != null)
                    {
                        GUI.color = visualFeedback != null ? visualFeedback.UnitPresentationTint(unit) : Color.white;
                        Rect unitRect = StaticUnitPresentationRect(unit, new Rect(cell.x + 4, cell.y + 4, cell.width - 8, cell.height - 8));
                        Vector2 motionOffset = visualFeedback != null ? visualFeedback.UnitPresentationOffset(unit) : Vector2.zero;
                        unitRect.x += motionOffset.x + (visualFeedback != null ? visualFeedback.UnitShakeOffset(unit) : 0f);
                        unitRect.y += motionOffset.y;
                        GUI.DrawTexture(unitRect, unitTexture, ScaleMode.ScaleToFit, true);
                    }
                    else
                    {
                        GUI.color = unit.IsHero ? new Color(.25f, .72f, 1f) : new Color(1f, .35f, .3f);
                        GUI.Box(new Rect(cell.x + 7, cell.y + 11, cell.width - 14, cell.height - 14), FacingGlyph(unit.Facing));
                    }
                    GUI.color = Color.white;
                    DrawUnitBars(unit, new Rect(cell.x + 5, cell.y + 3, cell.width - 10, 5));
                    bool revealIntent = !unit.IsHero && (unit.Id == selectedTargetId || unit.Id == state.ActiveUnitId || cell.Contains(e.mousePosition));
                    if (revealIntent) GUI.Label(new Rect(cell.x - 14, cell.y - 15, cell.width + 28, 16), GetEnemyIntent(unit));
                    DrawStatusMarkers(unit, cell);
                }
                if (tile.IsObjective)
                {
                    string relayState = tile.IsDestroyed ? "relay_rubble" : tile.Durability < 6 ? "relay_damaged" : "relay_intact";
                    GUI.DrawTexture(new Rect(cell.x + 8, cell.y + 8, cell.width - 16, cell.height - 16), formalRelayTextures[relayState], ScaleMode.ScaleToFit, true);
                    if (!tile.IsDestroyed) GUI.Label(new Rect(cell.x + 2, cell.y + 18, cell.width, 20), "导能柱");
                }
                else if (tile.Cover == CoverType.Light)
                {
                    string coverState = tile.IsDestroyed ? "light_cover_rubble" : tile.Durability < 4 ? "light_cover_damaged" : "light_cover_intact";
                    GUI.DrawTexture(new Rect(cell.x + 8, cell.y + 8, cell.width - 16, cell.height - 16), formalRelayTextures[coverState], ScaleMode.ScaleToFit, true);
                }
                else if (tile.Cover == CoverType.Heavy)
                {
                    string coverState = tile.IsDestroyed ? "heavy_cover_rubble" : tile.Durability < 7 ? "heavy_cover_damaged" : "heavy_cover_intact";
                    GUI.DrawTexture(new Rect(cell.x + 6, cell.y + 6, cell.width - 12, cell.height - 12), formalRelayTextures[coverState], ScaleMode.ScaleToFit, true);
                }
                else if (trainingRangeActive && tile.IsDevice)
                {
                    GUI.DrawTexture(new Rect(cell.x + 8, cell.y + 8, cell.width - 16, cell.height - 16), formalRelayTextures[tile.IsDestroyed ? "heavy_cover_rubble" : "heavy_cover_intact"], ScaleMode.ScaleToFit, true);
                    GUI.Label(new Rect(cell.x + 2, cell.y + 18, cell.width, 20), "设备");
                }
                if (trainingRangeActive && tile.IsWater)
                {
                    GUI.color = new Color(.38f, .82f, .94f, .92f);
                    GUI.Label(new Rect(cell.x + 2, cell.y + 18, cell.width, 20), "水面"); GUI.color = Color.white;
                }
                if (state.Loot != null && state.Loot.Position == p)
                {
                    GUI.color = Color.white;
                    Texture2D lootTexture = state.Loot.IsLooted ? formalRelayTextures["loot_crate_empty"] : formalLootTexture;
                    if (lootTexture != null)
                        GUI.DrawTexture(new Rect(cell.x + 12, cell.y + 12, cell.width - 24, cell.height - 24), lootTexture, ScaleMode.ScaleToFit, true);
                    else
                    {
                        GUI.color = new Color(1f, .78f, .18f);
                        GUI.Box(new Rect(cell.x + 15, cell.y + 16, cell.width - 30, cell.height - 30), "\u7269");
                    }
                    GUI.color = Color.white;
                }
            }
            if (hoveredEnemy != null) DrawEnemyHoverCard(hoveredEnemy, e.mousePosition);
            if (e.type == EventType.MouseDown && battlefield.TryResolveCell(new BattlefieldRect(board.x, board.y, board.width, board.height), state.Map.Width, state.Map.Height, e.mousePosition.x, e.mousePosition.y, out GridPosition clicked))
            {
                if (e.button == 1) { HandleInspectionClick(clicked); e.Use(); }
                else if (e.button == 0) { HandleCellClick(clicked); e.Use(); }
            }
        }

        public static Rect EnemyHoverCardRect(Vector2 pointer)
        {
            const float width = 456f, height = 218f, margin = 16f, battlefieldRight = 1440f, commandsTop = 900f;
            float x = pointer.x + 20f;
            if (x + width > battlefieldRight - margin) x = pointer.x - width - 20f;
            x = Mathf.Clamp(x, margin, battlefieldRight - margin - width);
            float y = Mathf.Clamp(pointer.y + 20f, 64f, commandsTop - margin - height);
            return new Rect(x, y, width, height);
        }

        private void DrawEnemyHoverCard(UnitState enemy, Vector2 pointer)
        {
            string details = CombatInformationPresenter.BuildEnemyHoverDetails(state, enemy, state.GetUnit("hero"));
            if (string.IsNullOrEmpty(details)) return;
            Rect card = EnemyHoverCardRect(pointer);
            GUI.color = new Color(.025f, .045f, .052f, .98f);
            GUI.Box(card, string.Empty);
            DrawOutline(card, new Color(FormalUiTheme.Cyan.r, FormalUiTheme.Cyan.g, FormalUiTheme.Cyan.b, .9f));
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold, wordWrap = false };
            GUIStyle bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true, alignment = TextAnchor.UpperLeft };
            GUI.color = FormalUiTheme.Amber;
            GUI.Label(new Rect(card.x + 16f, card.y + 10f, card.width - 32f, 26f), enemy.DisplayName + " // 敌情悬浮", titleStyle);
            GUI.color = FormalUiTheme.Text;
            GUI.Label(new Rect(card.x + 16f, card.y + 40f, card.width - 32f, card.height - 50f), details, bodyStyle);
            GUI.color = Color.white;
        }

        private void HandleInspectionClick(GridPosition position)
        {
            string nextTargetId = CombatInformationPresenter.EnemyInspectionTargetAt(state, position);
            if (selectedTargetId == nextTargetId) return;
            selectedTargetId = nextTargetId;
            MarkPresentation(UiPresentationArea.Combat);
            if (!string.IsNullOrEmpty(nextTargetId)) PublishUiVisual(new UiVisualEvent(UiVisualEventKind.CombatTargetConfirmed, nextTargetId));
        }

        private static void DrawOutline(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.x + 1, rect.y + 1, rect.width - 2, 3), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x + 1, rect.yMax - 4, rect.width - 2, 3), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x + 1, rect.y + 1, 3, rect.height - 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - 4, rect.y + 1, 3, rect.height - 2), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
        private void DrawPanel(Rect rect) { GUI.Box(rect, "\u6218\u6597\u63a7\u5236\u53f0"); UnitState active = state.GetUnit(state.ActiveUnitId); UnitState hero = state.GetUnit("hero"); GUI.Label(new Rect(rect.x + 14, rect.y + 34, 280, 22), $"\u884c\u52a8\u5355\u4f4d：{active.DisplayName} | AP {active.ActionPoints}"); GUI.Label(new Rect(rect.x + 14, rect.y + 60, 280, 20), $"\u4e3b\u89d2\u8d44\u6e90：{hero.Health}/{hero.MaxHealth} HP  {hero.Shield} \u62a4\u76fe  {hero.Mana}/{hero.MaxMana} \u4ee5\u592a"); GUI.Label(new Rect(rect.x + 14, rect.y + 82, 280, 20), GetRangeDescription()); string[] actions = { "\u79fb\u52a8", "\u653b\u51fb", "\u65bd\u672f", "\u9053\u5177", "\u4e92\u52a8" }; for (int i = 0; i < actions.Length; i++) if (GUI.Toggle(new Rect(rect.x + 14 + (i % 2) * 136, rect.y + 108 + (i / 2) * 34, 128, 28), selectedAction == actions[i], actions[i], "Button")) selectedAction = actions[i]; if (GUI.Button(new Rect(rect.x + 14, rect.y + 216, 128, 30), "\u7ed3\u675f\u884c\u52a8")) TryCommand(CombatCommand.EndTurn("hero")); if (GUI.Button(new Rect(rect.x + 150, rect.y + 216, 128, 30), "\u6218\u672f\u91cd\u5f00")) { state = snapshot.Clone(); PublishCombatEffects(CombatResolver.BeginTurn(state, "hero")); } GUI.Label(new Rect(rect.x + 14, rect.y + 256, 280, 20), "\u884c\u52a8\u6761\uff1a\u6570\u503c\u8d8a\u4f4e\u8d8a\u5148\u884c\u52a8"); int row = 0; foreach (UnitState unit in state.Units.Values) { GUI.Label(new Rect(rect.x + 14, rect.y + 280 + row * 27, 125, 20), $"{unit.DisplayName} HP{unit.Health} \u62a4{unit.Shield}"); GUI.HorizontalScrollbar(new Rect(rect.x + 142, rect.y + 284 + row * 27, 130, 16), Math.Min(100, unit.InitiativeTime) / 100f, .12f, 0f, 1f); row++; } GUI.Label(new Rect(rect.x + 14, rect.y + 410, 280, 20), "\u654c\u4eba\u610f\u56fe\u548c\u6218\u6597\u8bb0\u5f55"); for (int i = 0; i < Math.Min(6, state.EventLog.Count); i++) GUI.Label(new Rect(rect.x + 14, rect.y + 434 + i * 18, 280, 18), state.EventLog[i]); }
        private void ApplyBuild(int build)
        {
            UnitState hero = state.GetUnit("hero");
            StageTwoBuilds.Apply(hero, build);
            state.AddLog($"\u5de5\u574a\u5df2\u5207\u6362\u4e3a{hero.MainHand.DisplayName}\u6784\u7b51\u3002");
        }

        private static string GetStatusText(UnitState unit)
        {
            if (unit.Statuses.Count == 0) return "\u65e0";
            return string.Join(" ", unit.Statuses.Select(entry => $"{StatusName(entry.Key)}{entry.Value}"));
        }

        private static string StatusName(StatusType status) => CombatFeedbackCatalog.For(CombatFeedbackCatalog.ForStatus(status)).ShortLabel;

        private void DrawStatusMarkers(UnitState unit, Rect cell)
        {
            int index = 0;
            foreach (KeyValuePair<StatusType, int> status in unit.Statuses)
            {
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(cell.x + 4 + index * 15, cell.yMax - 17, 14, 14), formalStatusTextures[status.Key], ScaleMode.ScaleToFit, true);
                index++;
            }
            GUI.color = Color.white;
        }

        private static Color StatusColor(StatusType status)
        {
            CombatFeedbackSemantic semantic = CombatFeedbackCatalog.For(CombatFeedbackCatalog.ForStatus(status));
            return ColorUtility.TryParseHtmlString(semantic.ColorHex, out Color color) ? color : Color.white;
        }

        private string GetRangeDescriptionStageTwo()
        {
            int count = 0;
            for (int y = 0; y < state.Map.Height; y++) for (int x = 0; x < state.Map.Width; x++) if (IsInSelectedRange(new GridPosition(x, y))) count++;
            UnitState hero = state.GetUnit("hero");
            string rule = selectedAction == "\u79fb\u52a8" ? "\u79fb\u52a8 3 \u683c" : selectedAction == "\u653b\u51fb" ? $"{hero.MainHand.DisplayName} {hero.MainHand.Range} \u683c" : selectedAction == "\u6280\u80fd1" ? $"{hero.SkillOne.DisplayName} {hero.SkillOne.Range} \u683c" : selectedAction == "\u6280\u80fd2" ? $"{hero.SkillTwo.DisplayName} {hero.SkillTwo.Range} \u683c" : selectedAction == "\u641c\u522e" ? "\u641c\u522e\uff1a\u76f8\u90bb 1 \u683c" : "\u4ea4\u4e92\uff1a\u76f8\u90bb 1 \u683c";
            return $"\u5f53\u524d\uff1a{rule} | \u9ad8\u4eae {count} \u683c";
        }

        private void HandleCellClick(GridPosition p)
        {
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
            state.AddLog(artifact.DisplayName + " // " + execution.Steps.Count + " 项确定性结果");
            selectedTargetId = null; MarkPresentation(UiPresentationArea.Combat);
            PublishUiVisual(new UiVisualEvent(UiVisualEventKind.CombatCommandSubmitted, artifact.Id));
            visualFeedback?.NotifyArtifact(artifact, source, preview.Cells, execution);
            if (!trainingRangeActive && state.ActiveUnitId == "hero" && state.GetUnit("hero").ActionPoints == 0)
                PublishCombatEffects(CombatResolver.EndTurn(state, state.GetUnit("hero")));
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
            state.AddLog(spell.DisplayName + " // " + execution.Steps.Count + " 项确定性结果");
            selectedTargetId = null; MarkPresentation(UiPresentationArea.Combat);
            PublishUiVisual(new UiVisualEvent(UiVisualEventKind.CombatCommandSubmitted, spell.Id));
            visualFeedback?.NotifyFireSpell(spell, source, preview.Cells);
            if (!trainingRangeActive && state.ActiveUnitId == "hero" && state.GetUnit("hero").ActionPoints == 0) PublishCombatEffects(CombatResolver.EndTurn(state, state.GetUnit("hero")));
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
        private void TryCommand(CombatCommand command)
        {
            try
            {
                UnitState commandUnit = state.GetUnit(command.UnitId);
                SkillDefinition deliveredSkill = command.Type == CombatCommandType.UseSkill && commandUnit != null
                    ? (command.SlotIndex == 0 ? commandUnit.SkillOne : commandUnit.SkillTwo) : null;
                GridPosition deliverySource = commandUnit?.Position ?? command.Destination;
                GridPosition movementSource = deliverySource;
                UnitState commandTarget = string.IsNullOrWhiteSpace(command.TargetUnitId) ? null : state.GetUnit(command.TargetUnitId);
                GridPosition deliveryTarget = commandTarget?.Position ??
                    (deliveredSkill != null && (deliveredSkill.TargetRule == SkillTargetRule.GridCell || deliveredSkill.TargetRule == SkillTargetRule.Destructible)
                        ? command.Destination : deliverySource);
                CombatEffectExecution execution;
                IReadOnlyList<FireSpellExecution> fireTriggers = Array.Empty<FireSpellExecution>();
                if (command.Type == CombatCommandType.Attack)
                {
                    if (fireBattle == null || fireBattle.Combat != state) fireBattle = new FireBattleState(state);
                    FireWeaponAttackResolution attack = FireSpellEngine.ResolveWeaponAttack(fireBattle, command.UnitId,
                        command.TargetUnitId);
                    execution = attack.WeaponExecution;
                    fireTriggers = attack.TriggerExecutions;
                }
                else execution = CombatResolver.Resolve(state, command);
                string actionResult = CombatInformationPresenter.BuildActionResult(state, command, execution);
                if (!string.IsNullOrEmpty(actionResult)) state.AddLog(actionResult);
                if (trainingRangeActive && deliveredSkill != null) trainingRangeSession?.RecordExternal(execution);
                if (command.Type == CombatCommandType.Move && commandUnit != null && fireBattle != null)
                {
                    fireBattle.ResolveEntry(commandUnit, movementSource);
                    PublishFireExecutions(FireSpellEngine.TriggerMarkedTargetMove(fireBattle, commandUnit.Id, movementSource));
                    PublishFireExecutions(FireSpellEngine.TriggerEnemyEntry(fireBattle, commandUnit.Id));
                }
                selectedTargetId = null;
                MarkPresentation(UiPresentationArea.Combat);
                PublishUiVisual(new UiVisualEvent(UiVisualEventKind.CombatCommandSubmitted, command.Type.ToString()));
                PublishCombatEffects(execution);
                PublishFireExecutions(fireTriggers);
                visualFeedback?.NotifySkillDelivery(deliveredSkill, deliverySource, deliveryTarget);
                if (!trainingRangeActive && state.ActiveUnitId == "hero" && state.GetUnit("hero").ActionPoints == 0)
                    PublishCombatEffects(CombatResolver.EndTurn(state, state.GetUnit("hero")));
                developerFlow.RefreshOutcome();
            }
            catch (InvalidOperationException error)
            {
                state.AddLog(error.Message);
                PublishUiVisual(new UiVisualEvent(UiVisualEventKind.CombatCommandRejected, command.Type.ToString(), message: error.Message));
            }
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
                state.AddLog((spell?.DisplayName ?? "火术触发") + " // " + execution.Steps.Count + " 项触发结果");
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
        private void DrawUnitBars(UnitState unit, Rect rect) { GUI.color = Color.black; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = unit.IsHero ? new Color(.2f, .85f, .45f) : new Color(.9f, .22f, .22f); GUI.DrawTexture(new Rect(rect.x + 1, rect.y + 1, (rect.width - 2) * unit.Health / unit.MaxHealth, rect.height - 2), Texture2D.whiteTexture); GUI.color = Color.white; }
        private string GetEnemyIntent(UnitState enemy)
        {
            return EnemyIntent(enemy)?.CompactText ?? "无可用意图";
        }
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
        private static string FacingGlyph(Facing facing) => facing == Facing.North ? "^" : facing == Facing.South ? "v" : facing == Facing.East ? ">" : "<";
    }
}
