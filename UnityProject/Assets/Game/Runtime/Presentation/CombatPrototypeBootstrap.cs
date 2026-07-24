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
        private const float CellSize = 78f;
        private const float UiWidth = 1920f;
        private const float UiHeight = 1080f;
        private CombatState state;
        // Legacy panel helpers still use this editor-only snapshot; active flow restarts use developerFlow.
        private CombatState snapshot;
        private string selectedAction = "\u79fb\u52a8";
        private string selectedTargetId;
        private Font chineseFont;
        private Texture2D barTexture;
        private bool initialized;
        private MissionPreparation developerPreparation;
        private CombatFlowController developerFlow;
        private RogueliteDeveloperRun rogueliteRun;
        private int sandboxTemplateIndex;
        private bool rogueliteMenuOpen;
        private bool outcomeHandled;
        private RogueliteMapRun mapRun;
        private bool mapMenuOpen;
        private readonly Dictionary<string, Texture2D> formalUnitTextures = new Dictionary<string, Texture2D>();
        private Texture2D formalLootTexture;
        private const string RogueliteSaveKey = "occ.roguelite.iron_echoes";
        private const string ShortRogueliteSaveKey = "occ.roguelite.short_run";
        private const string MapRogueliteSaveKey = "occ.roguelite.map_run";
        private CombatVisualFeedback visualFeedback;
        private RogueliteSettlementPresentation settlementPresentation;
        private FormalCombatHud formalCombatHud;
        private DeveloperConsolePanel developerConsole;
        private float mapPanelAlpha = 1f;
        private float mapPanelScale = 1f;
        private float menuPanelAlpha = 1f;
        private float menuPanelScale = 1f;

        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            if (initialized) return;
            initialized = true;
            chineseFont = Resources.Load<Font>("Fonts/SimHei");
            barTexture = Resources.Load<Texture2D>("UI/Bar");
            Transform sceneUi = transform.Find("场景UI");
            if (sceneUi != null) sceneUi.gameObject.SetActive(false);
            developerPreparation = new MissionPreparation().Configure("relay_test", "破坏任务目标并清理威胁", "步枪兵、盾卫、火术师、突袭者、精英先锋");
            visualFeedback = gameObject.AddComponent<CombatVisualFeedback>(); visualFeedback.Initialize(this);
            settlementPresentation = gameObject.AddComponent<RogueliteSettlementPresentation>(); settlementPresentation.Initialize(this);
            formalCombatHud = gameObject.AddComponent<FormalCombatHud>(); formalCombatHud.Initialize(this);
            developerConsole = gameObject.AddComponent<DeveloperConsolePanel>(); developerConsole.Initialize(this);
            BuildCombatFromSceneStageTwo();
            ApplyFormalRelayVisuals();
            LoadFormalUnitTextures();
            formalLootTexture = Resources.Load<Texture2D>("Art/FormalRelay32/loot_crate");
            if (formalLootTexture != null) { formalLootTexture.filterMode = FilterMode.Point; formalLootTexture.wrapMode = TextureWrapMode.Clamp; }
            menuPanelAlpha = 0f; menuPanelScale = .96f;
            DOTween.To(() => menuPanelAlpha, value => menuPanelAlpha = value, 1f, .28f).SetUpdate(true);
            DOTween.To(() => menuPanelScale, value => menuPanelScale = value, 1f, .32f).SetEase(Ease.OutCubic).SetUpdate(true);
        }

        private void Awake()
        {
            if (!Application.isPlaying) return;
            Application.targetFrameRate = 60;
            Camera sceneCamera = FindFirstObjectByType<Camera>();
            if (sceneCamera != null && sceneCamera.CompareTag("Untagged")) sceneCamera.tag = "MainCamera";
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
            GameObject textObject = new GameObject(name + "文字"); textObject.transform.SetParent(panel.transform, false); RectTransform textRect = textObject.AddComponent<RectTransform>(); textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one; textRect.offsetMin = Vector2.zero; textRect.offsetMax = Vector2.zero; Text label = textObject.AddComponent<Text>(); label.text = text; label.alignment = TextAnchor.MiddleCenter; label.color = Color.white; label.fontSize = fontSize; label.font = Resources.Load<Font>("Fonts/SimHei");
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
            string[] names = { "hero", "rifleman", "shieldguard", "pyromancer", "raider", "elite" };
            foreach (string name in names)
            {
                Texture2D texture = Resources.Load<Texture2D>("Art/FormalUnits64/" + name);
                if (texture == null) continue;
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                formalUnitTextures[name] = texture;
            }
        }

        private Texture2D FormalUnitTexture(UnitState unit)
        {
            if (unit == null) return null;
            if (unit.IsHero) return TextureFor("hero");
            string name = unit.DisplayName;
            if (name.Contains("步枪") || name.Contains("狙击")) return TextureFor("rifleman");
            if (name.Contains("盾卫") || name.Contains("结界")) return TextureFor("shieldguard");
            if (name.Contains("火术") || name.Contains("束缚") || name.Contains("净化")) return TextureFor("pyromancer");
            if (name.Contains("突袭")) return TextureFor("raider");
            if (name.Contains("精英") || name.Contains("监工")) return TextureFor("elite");
            return null;
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
            GridMap map = new GridMap(12, 9); CombatSceneMarker[] markers = FindObjectsByType<CombatSceneMarker>();
            foreach (CombatSceneMarker marker in markers) { GridPosition p = ScenePosition(marker); if (marker.MarkerType == CombatSceneMarkerType.LightCover) map.SetTile(p, new TileState { Cover = CoverType.Light, Durability = 4 }); if (marker.MarkerType == CombatSceneMarkerType.HeavyCover) map.SetTile(p, new TileState { Cover = CoverType.Heavy, Durability = 7 }); if (marker.MarkerType == CombatSceneMarkerType.Objective) map.SetTile(p, new TileState { IsObjective = true, Durability = 6 }); }
            List<UnitState> units = new List<UnitState>();
            foreach (CombatSceneMarker marker in markers.Where(m => m.MarkerType == CombatSceneMarkerType.Unit)) { GridPosition p = ScenePosition(marker); bool hero = marker.name.Contains("主角"); string id = hero ? "hero" : marker.name.Contains("步枪") ? "rifle" : marker.name.Contains("盾") ? "guard" : "caster"; string name = hero ? "\u963f\u65af\u7279\u62c9" : id == "rifle" ? "\u6b65\u67aa\u5175" : id == "guard" ? "\u76fe\u536b" : "\u706b\u672f\u5e08"; units.Add(new UnitState(id, hero, p, hero ? Facing.East : Facing.West) { DisplayName = name, Armor = hero ? 1 : id == "guard" ? 2 : 0, Block = id == "guard" ? 2 : hero ? 1 : 0, Speed = hero ? 11 : id == "guard" ? 7 : id == "caster" ? 9 : 8 }); }
            if (units.Count == 0) return;
            state = new CombatState(map, units);
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
            state.ConfigureQuickbar(CombatCatalog.Medkit, CombatCatalog.ShieldCell);
            ApplyShortRunChoices();
            mapRun?.ApplyBuild(state.GetUnit("hero"));
            state.SetLoot(new LootContainer(new GridPosition(2, 0), new InventoryItem("aether_core", "\u4ee5\u592a\u6838\u5fc3", 2, 1)));
            CombatResolver.BeginTurn(state, "hero");
        }

        private void BuildCombatFromSceneStageTwo()
        {
            GridMap map = new GridMap(12, 9);
            CombatSceneMarker[] markers = FindObjectsByType<CombatSceneMarker>();
            foreach (CombatSceneMarker marker in markers)
            {
                GridPosition p = ScenePosition(marker);
                if (marker.MarkerType == CombatSceneMarkerType.LightCover) map.SetTile(p, new TileState { Cover = CoverType.Light, Durability = 4 });
                if (marker.MarkerType == CombatSceneMarkerType.HeavyCover) map.SetTile(p, new TileState { Cover = CoverType.Heavy, Durability = 7 });
                if (marker.MarkerType == CombatSceneMarkerType.Objective) map.SetTile(p, new TileState { IsObjective = true, Durability = 6 });
            }
            List<UnitState> units = new List<UnitState>();
            string encounterId = mapRun == null ? null : mapRun.HasPendingContentCombat ? mapRun.PendingContentCombatMissionId : mapRun.CurrentNodeId;
            RogueliteEncounterDefinition encounter = string.IsNullOrEmpty(encounterId) ? null : RogueliteEncounterCatalog.For(encounterId, mapRun?.RegionBossId);
            int enemyIndex = 0;
            foreach (CombatSceneMarker marker in markers.Where(m => m.MarkerType == CombatSceneMarkerType.Unit).OrderBy(m => m.name, StringComparer.Ordinal))
            {
                bool hero = marker.name.Contains("\u4e3b\u89d2");
                UnitState unit = new UnitState(hero ? "hero" : "enemy_" + enemyIndex, hero, ScenePosition(marker), hero ? Facing.East : Facing.West);
                if (hero) { unit.DisplayName = "\u963f\u65af\u7279\u62c9"; unit.Speed = 11; }
                else
                {
                    string archetypeId = encounter == null ? EnemyArchetypes.All[Math.Min(enemyIndex, EnemyArchetypes.All.Count - 1)].Id : encounter.EnemyArchetypeIds[Math.Min(enemyIndex, encounter.EnemyArchetypeIds.Count - 1)];
                    EnemyArchetypes.Get(archetypeId).Apply(unit); enemyIndex++;
                }
                units.Add(unit);
            }
            if (units.Count == 0 || !units.Any(unit => unit.IsHero)) return;
            state = new CombatState(map, units);
            if (mapRun != null)
            {
                RogueliteMissionDefinition mission = RogueliteDeveloperCatalog.FindMission(mapRun.HasPendingContentCombat ? mapRun.PendingContentCombatMissionId : mapRun.CurrentNodeId);
                developerPreparation = new MissionPreparation().Configure(mission.Id, mission.ObjectiveSummary, DescribeEncounter(encounter, mission.EnemySummary));
                if (mission.ObjectiveType == CombatObjectiveType.Elimination) state.ConfigureObjectives(new EliminationObjective(mission.Id + "_objective"));
                else state.ConfigureObjectives(new DestructionObjective(map.PositionsWith(tile => tile.IsObjective), mission.Id + "_objective"));
            }
            else if (rogueliteRun != null)
            {
                RogueliteMissionDefinition mission = rogueliteRun.CurrentMission;
                developerPreparation = new MissionPreparation().Configure(mission.Id, mission.ObjectiveSummary, mission.EnemySummary);
                if (mission.ObjectiveType == CombatObjectiveType.Elimination)
                    state.ConfigureObjectives(new EliminationObjective(mission.Id + "_objective"));
                else
                    state.ConfigureObjectives(new DestructionObjective(map.PositionsWith(tile => tile.IsObjective), mission.Id + "_objective"));
            }
            state.ConfigureQuickbar(CombatCatalog.Medkit, CombatCatalog.ShieldCell);
            ApplyShortRunChoices();
            mapRun?.ApplyBuild(state.GetUnit("hero"));
            state.SetLoot(new LootContainer(new GridPosition(2, 0), new InventoryItem("aether_core", "\u4ee5\u592a\u6838\u5fc3", 2, 1)));
            developerFlow = new CombatFlowController();
            developerFlow.Configure(developerPreparation, state);
            outcomeHandled = false;
        }

        private static GridPosition ScenePosition(CombatSceneMarker marker) => new GridPosition(Mathf.RoundToInt(marker.transform.position.x), Mathf.RoundToInt(marker.transform.position.y));
        public void OpenDeveloperBriefing() { developerFlow.OpenBriefing(); }
        public void StartDeveloperCombat() { developerFlow.BeginCombat(); state = developerFlow.State; visualFeedback?.ResetBattleFeedback(); CombatResolver.BeginTurn(state, "hero"); RefreshSceneHud(); }
        public void TacticalRestartDeveloperCombat() { developerFlow.TacticalRestart(); state = developerFlow.State; visualFeedback?.ResetBattleFeedback(); CombatResolver.BeginTurn(state, "hero"); developerFlow.ResumeAfterRestart(); RefreshSceneHud(); }
        public void ReturnToDeveloperMenu() { developerFlow.ReturnToDeveloperMenu(); state = developerFlow.State; selectedAction = "移动"; rogueliteRun = null; rogueliteMenuOpen = false; mapRun = null; mapMenuOpen = false; RefreshSceneHud(); }
        public void OpenRogueliteMenu() { rogueliteMenuOpen = true; rogueliteRun = null; }
        public void CloseRogueliteMenu() { rogueliteMenuOpen = false; }
        public void StartRogueliteStory(bool continueSave)
        {
            RogueliteStoryPackage package = continueSave && PlayerPrefs.HasKey(RogueliteSaveKey)
                ? RogueliteStoryPackage.FromJson(PlayerPrefs.GetString(RogueliteSaveKey))
                : RogueliteStoryCatalog.CreateDefault(UnityEngine.Random.Range(1, int.MaxValue));
            rogueliteRun = new RogueliteDeveloperRun(package); BuildCombatFromSceneStageTwo(); developerFlow.OpenBriefing();
        }
        public void StartShortRoguelite(bool continueSave)
        {
            ShortRogueliteRun run = continueSave && PlayerPrefs.HasKey(ShortRogueliteSaveKey)
                ? ShortRogueliteRun.FromJson(PlayerPrefs.GetString(ShortRogueliteSaveKey))
                : new ShortRogueliteRun(UnityEngine.Random.Range(1, int.MaxValue));
            rogueliteRun = new RogueliteDeveloperRun(run); OpenShortRunPhase();
        }
        public void DeleteShortRogueliteSave() { PlayerPrefs.DeleteKey(ShortRogueliteSaveKey); PlayerPrefs.Save(); }
        public bool HasShortRogueliteSave => PlayerPrefs.HasKey(ShortRogueliteSaveKey);
        public void StartMapRoguelite(bool continueSave) { mapRun = continueSave && PlayerPrefs.HasKey(MapRogueliteSaveKey) ? RogueliteMapRun.FromJson(PlayerPrefs.GetString(MapRogueliteSaveKey)) : new RogueliteMapRun(UnityEngine.Random.Range(1, int.MaxValue)); mapMenuOpen = true; rogueliteMenuOpen = false; PlayMapEntrance(); }
        public void DeleteMapRogueliteSave() { PlayerPrefs.DeleteKey(MapRogueliteSaveKey); PlayerPrefs.Save(); }
        public bool HasMapRogueliteSave => PlayerPrefs.HasKey(MapRogueliteSaveKey);
        public void SelectMapNode(string nodeId)
        {
            mapRun.SelectNode(nodeId);
            RogueliteMapNode node = RogueliteMapCatalog.Node(nodeId);
            if (!node.IsCombat)
            {
                SaveMapRun(); PlayMapEntrance(); return;
            }
            SaveMapRun(); BuildCombatFromSceneStageTwo(); developerFlow.OpenBriefing();
        }

        private static string DescribeEncounter(RogueliteEncounterDefinition encounter, string fallback)
        {
            if (encounter == null) return fallback;
            string summary = string.Join("、", encounter.EnemyArchetypeIds.Select(id => EnemyArchetypes.Get(id).DisplayName));
            return (encounter.IsBoss ? "区域首领：" : encounter.IsElite ? "精英编成：" : "区域编成：") + summary;
        }
        public void ChooseMapNodeContent(string choiceId)
        {
            mapRun.ChooseCurrentNodeContent(choiceId);
            if (mapRun.HasPendingContentCombat) { SaveMapRun(); BuildCombatFromSceneStageTwo(); developerFlow.OpenBriefing(); return; }
            SaveMapRun(); PlayMapEntrance();
        }
        public void ClaimMapReward(string rewardId) { mapRun.ClaimReward(rewardId); SaveMapRun(); settlementPresentation?.RefreshNow(); }
        public void EquipMapReward(string rewardId) { mapRun.EquipReward(rewardId); SaveMapRun(); PlayMapEntrance(); }
        public void CalibrateMapAether() { mapRun.CalibrateAether(); SaveMapRun(); PlayMapEntrance(); }
        public void ReturnToMapRun() { developerFlow.ReturnToDeveloperMenu(); state = developerFlow.State; mapMenuOpen = true; RefreshSceneHud(); }
        private void SaveMapRun() { PlayerPrefs.SetString(MapRogueliteSaveKey, mapRun.ToJson()); PlayerPrefs.Save(); }
        public void ChooseShortEvent() { rogueliteRun.ShortRun.ChooseEvent("field_repair"); SaveShortRun(); }
        public void ChooseShortSalvage() { rogueliteRun.ShortRun.ChooseSalvage("shield_cell"); SaveShortRun(); }
        public void ChooseShortUpgrade() { rogueliteRun.ShortRun.ChooseUpgrade("calibrated_rifle"); SaveShortRun(); }
        private void OpenShortRunPhase()
        {
            if (rogueliteRun?.IsShortRun != true) return;
            if (rogueliteRun.ShortRun.Phase == ShortRoguelitePhase.FirstCombat || rogueliteRun.ShortRun.Phase == ShortRoguelitePhase.SecondCombat) { BuildCombatFromSceneStageTwo(); developerFlow.OpenBriefing(); }
            else { developerFlow.ReturnToDeveloperMenu(); state = developerFlow.State; rogueliteMenuOpen = true; }
        }
        private void SaveShortRun() { PlayerPrefs.SetString(ShortRogueliteSaveKey, rogueliteRun.ShortRun.ToJson()); PlayerPrefs.Save(); }
        private void ApplyShortRunChoices()
        {
            if (rogueliteRun?.IsShortRun != true || rogueliteRun.ShortRun.Phase != ShortRoguelitePhase.SecondCombat) return;
            UnitState hero = state.GetUnit("hero");
            if (rogueliteRun.ShortRun.EventChoiceId == "field_repair") hero.Armor += 1;
            if (rogueliteRun.ShortRun.UpgradeChoiceId == "calibrated_rifle") hero.Equip(StageTwoBuilds.CalibratedRifle, CombatCatalog.Shield, CombatCatalog.FireBolt, CombatCatalog.FrostBind);
            if (rogueliteRun.ShortRun.SalvageChoiceId == "shield_cell") state.ConfigureQuickbar(CombatCatalog.Medkit, CombatCatalog.ShieldCell, CombatCatalog.ShieldCell);
        }
        public void StartRogueliteSandbox()
        {
            IReadOnlyList<TaskTemplate> templates = RogueliteDeveloperCatalog.OpenSandboxTemplates;
            rogueliteRun = new RogueliteDeveloperRun(templates[sandboxTemplateIndex % templates.Count].Id, UnityEngine.Random.Range(1, int.MaxValue));
            BuildCombatFromSceneStageTwo(); developerFlow.OpenBriefing();
        }
        public void SelectNextSandboxTemplate() { sandboxTemplateIndex = (sandboxTemplateIndex + 1) % RogueliteDeveloperCatalog.OpenSandboxTemplates.Count; }
        public void DeleteRogueliteSave() { PlayerPrefs.DeleteKey(RogueliteSaveKey); PlayerPrefs.Save(); }
        public bool HasRogueliteSave => PlayerPrefs.HasKey(RogueliteSaveKey);
        public CombatState CurrentState => state;
        public string SelectedAction => selectedAction;
        public string SelectedTargetId => selectedTargetId;
        public void SetSelectedTargetForUi(string unitId)
        {
            selectedTargetId = state != null && state.GetUnit(unitId) != null ? unitId : null;
        }
        public RogueliteMapRun CurrentMapRun => mapRun;
        public bool IsDeveloperCombatActive => developerFlow != null && developerFlow.Phase == CombatFlowPhase.Active;
        public bool IsCombatOutcomeVisible => developerFlow != null && (developerFlow.Phase == CombatFlowPhase.Victory || developerFlow.Phase == CombatFlowPhase.Defeat);
        public void ToggleDeveloperConsole() { developerConsole?.Toggle(); }
        public void SelectHudAction(string action) { selectedAction = action; }
        public void UseQuickbarSlot(int slot) { if (state?.Quickbar[slot] != null) TryCommand(CombatCommand.UseQuickbar("hero", slot)); }
        public void ApplyHudBuild(int build) { if (state != null) ApplyBuild(build); }
        public void EndHeroTurn() { if (state != null) TryCommand(CombatCommand.EndTurn("hero")); }
        private void Update()
        {
            if (!Application.isPlaying || developerFlow == null || state == null) return;
            if (developerFlow.Phase == CombatFlowPhase.Active && !state.IsVictory && !state.IsDefeat && state.ActiveUnitId != "hero") { RunEnemyTurn(); developerFlow.RefreshOutcome(); }
            developerFlow.RefreshOutcome(); HandleRogueliteOutcome();
        }
        private void HandleRogueliteOutcome()
        {
            if (mapRun != null && !outcomeHandled && developerFlow.Phase == CombatFlowPhase.Victory)
            {
                outcomeHandled = true; visualFeedback?.PlayOutcome(true);
                if (mapRun.HasPendingContentCombat) mapRun.CompletePendingContentCombat(); else mapRun.CompleteCurrentCombat();
                SaveMapRun(); settlementPresentation?.RefreshNow(); return;
            }
            if (!outcomeHandled && developerFlow.Phase == CombatFlowPhase.Defeat) { outcomeHandled = true; visualFeedback?.PlayOutcome(false); }
            if (rogueliteRun == null || outcomeHandled || developerFlow.Phase != CombatFlowPhase.Victory) return;
            visualFeedback?.PlayOutcome(true);
            outcomeHandled = true;
            string summary = "胜利 | " + rogueliteRun.CurrentMission.TemplateId + " | 种子 " + rogueliteRun.Package.Seed;
            if (rogueliteRun.Kind == RogueliteLaunchKind.TemplateSandbox) return;
            rogueliteRun.Complete(summary);
            if (rogueliteRun.IsShortRun) SaveShortRun();
            else { PlayerPrefs.SetString(RogueliteSaveKey, rogueliteRun.Package.ToJson()); PlayerPrefs.Save(); }
        }
        public void ContinueRogueliteAfterVictory()
        {
            if (mapRun != null && developerFlow.Phase == CombatFlowPhase.Victory) { developerFlow.ReturnToDeveloperMenu(); state = developerFlow.State; mapMenuOpen = true; RefreshSceneHud(); return; }
            if (rogueliteRun == null || (developerFlow.Phase != CombatFlowPhase.Victory && developerFlow.Phase != CombatFlowPhase.Defeat)) return;
            if (developerFlow.Phase == CombatFlowPhase.Victory && rogueliteRun.IsShortRun) { OpenShortRunPhase(); return; }
            if (developerFlow.Phase == CombatFlowPhase.Victory && rogueliteRun.Kind == RogueliteLaunchKind.StoryChain && !rogueliteRun.Package.IsComplete) { BuildCombatFromSceneStageTwo(); developerFlow.OpenBriefing(); }
            else { developerFlow.ReturnToDeveloperMenu(); state = developerFlow.State; rogueliteRun = null; rogueliteMenuOpen = true; RefreshSceneHud(); }
        }
        public void ForceCurrentOutcome(bool victory)
        {
            if (developerFlow?.Phase != CombatFlowPhase.Active) return;
            state.ResolveDebugOutcome(victory); developerFlow.RefreshOutcome(); HandleRogueliteOutcome();
        }
        private void RefreshSceneHud()
        {
            // The authored HUD is retired in favour of FormalCombatHud; keep it inert during the transition.
            Transform sceneUi = transform.Find("场景UI");
            if (sceneUi == null || !sceneUi.gameObject.activeInHierarchy) return;
            TacticalHudSceneBinder binder = sceneUi.GetComponent<TacticalHudSceneBinder>();
            if (binder != null) binder.RefreshNow();
        }
        private void RunEnemyTurn() { UnitState enemy = state.GetUnit(state.ActiveUnitId); UnitState hero = state.GetUnit("hero"); if (enemy == null || !enemy.IsAlive) { if (enemy != null) CombatResolver.EndTurn(state, enemy); return; } try { if (Distance(enemy.Position, hero.Position) <= 4 && enemy.ActionPoints > 0) CombatResolver.Resolve(state, enemy.Id == "caster" ? CombatCommand.Cast(enemy.Id, hero.Id) : CombatCommand.Attack(enemy.Id, hero.Id)); else if (enemy.ActionPoints > 0) CombatResolver.Resolve(state, CombatCommand.Move(enemy.Id, StepToward(enemy.Position, hero.Position), FacingToward(enemy.Position, hero.Position))); if (state.ActiveUnitId == enemy.Id) CombatResolver.EndTurn(state, enemy); } catch (InvalidOperationException error) { state.AddLog(error.Message); CombatResolver.EndTurn(state, enemy); } }
        private void OnGUI() { if (!Application.isPlaying || developerFlow == null) return; float scale = Mathf.Min(Screen.width / UiWidth, Screen.height / UiHeight); Vector2 offset = new Vector2((Screen.width - UiWidth * scale) * .5f, (Screen.height - UiHeight * scale) * .5f); Matrix4x4 previous = GUI.matrix; GUI.matrix = Matrix4x4.TRS(offset, Quaternion.identity, Vector3.one * scale); ConfigureGuiSkin(); if (developerFlow.Phase == CombatFlowPhase.DeveloperMenu) { if (mapMenuOpen) DrawMapRun(); else if (rogueliteMenuOpen) DrawRogueliteMenu(); else DrawDeveloperMenu(); GUI.matrix = previous; return; } if (developerFlow.Phase == CombatFlowPhase.Briefing) { DrawDeveloperBriefing(); GUI.matrix = previous; return; } DrawGrid(new Rect(24, 112, 12 * CellSize, 9 * CellSize)); GUI.matrix = previous; }
        private void ConfigureGuiSkin()
        {
            GUI.skin.label.fontSize = 18; GUI.skin.button.fontSize = 18; GUI.skin.box.fontSize = 20;
            GUI.skin.button.padding = new RectOffset(12, 12, 8, 8);
            GUI.skin.box.normal.textColor = new Color(.88f, .94f, 1f);
        }
        private void DrawHeader()
        {
            GUI.color = new Color(.035f, .075f, .13f, .98f); GUI.Box(new Rect(24, 20, 1390, 68), ""); GUI.color = Color.white;
            GUI.Label(new Rect(48, 30, 520, 28), "OCC // 正式战斗流程"); GUI.color = new Color(.35f, .9f, 1f); GUI.Label(new Rect(1040, 30, 320, 28), "开发测试构建  ·  1920×1080"); GUI.color = Color.white;
        }
        private void DrawDeveloperMenu()
        {
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = GUI.matrix * Matrix4x4.TRS(new Vector3(960, 540, 0), Quaternion.identity, Vector3.one * menuPanelScale) * Matrix4x4.TRS(new Vector3(-960, -540, 0), Quaternion.identity, Vector3.one);
            GUI.color = new Color(.018f, .028f, .042f, .98f); GUI.DrawTexture(new Rect(0, 0, UiWidth, UiHeight), Texture2D.whiteTexture);
            GUI.color = new Color(.10f, .68f, .78f, .16f * menuPanelAlpha); GUI.DrawTexture(new Rect(120, 140, 8, 760), Texture2D.whiteTexture); GUI.DrawTexture(new Rect(1792, 140, 8, 760), Texture2D.whiteTexture);
            GUI.color = new Color(.045f, .072f, .096f, .98f * menuPanelAlpha); GUI.Box(new Rect(220, 150, 1480, 760), "");
            GUI.color = new Color(.35f, .9f, 1f, menuPanelAlpha); GUI.DrawTexture(new Rect(260, 202, 124, 4), Texture2D.whiteTexture); GUI.Label(new Rect(260, 228, 1080, 42), "OCC // 正式行动入口");
            GUI.color = new Color(.58f, .66f, .71f, menuPanelAlpha); GUI.Label(new Rect(260, 280, 1100, 28), "选择行动模式。战前简报确认目标、敌情与重开规则；开发控制台仅在战斗中由 F1 呼出。");

            DrawMenuInfoPanel(new Rect(260, 350, 650, 210), "剧情行动", "中继器破坏演练", "任务  " + developerPreparation.MissionId + "\n敌情  " + developerPreparation.EnemySummary, new Color(.35f, .9f, 1f));
            DrawMenuInfoPanel(new Rect(1010, 350, 650, 210), "肉鸽区域", "自由回访推进", "20 节点正交网络  /  权限门\n商店、工坊、事件与区域首领", new Color(1f, .76f, .25f));
            if (DrawMenuAction(new Rect(260, 610, 650, 86), "进入战前简报", "剧情测试  /  破坏目标", new Color(.20f, .78f, .94f))) OpenDeveloperBriefing();
            if (DrawMenuAction(new Rect(1010, 610, 650, 86), "开始自由推进", "肉鸽地图  /  新开区域", new Color(1f, .70f, .20f))) StartMapRoguelite(false);
            GUI.color = new Color(.42f, .52f, .57f, menuPanelAlpha); GUI.Label(new Rect(260, 770, 1300, 26), "正式入口  /  战前简报  /  战术行动  /  结算奖励  /  地图回访");
            GUI.color = Color.white; GUI.matrix = previous;
        }

        private static void DrawMenuInfoPanel(Rect rect, string label, string title, string details, Color accent)
        {
            GUI.color = new Color(.025f, .04f, .055f, .98f); GUI.Box(rect, "");
            GUI.color = accent; GUI.DrawTexture(new Rect(rect.x, rect.y, 4, rect.height), Texture2D.whiteTexture); GUI.Label(new Rect(rect.x + 28, rect.y + 22, rect.width - 50, 24), label);
            GUI.color = new Color(.9f, .94f, .96f); GUI.Label(new Rect(rect.x + 28, rect.y + 58, rect.width - 50, 30), title);
            GUI.color = new Color(.58f, .66f, .71f); GUI.Label(new Rect(rect.x + 28, rect.y + 106, rect.width - 50, 74), details);
        }

        private static bool DrawMenuAction(Rect rect, string title, string subtitle, Color accent)
        {
            bool hover = rect.Contains(Event.current.mousePosition);
            GUI.color = hover ? new Color(accent.r, accent.g, accent.b, .22f) : new Color(.055f, .085f, .105f, 1f); GUI.Box(rect, "");
            GUI.color = accent; GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 3), Texture2D.whiteTexture);
            GUI.color = new Color(.92f, .96f, .98f); GUI.Label(new Rect(rect.x + 26, rect.y + 16, rect.width - 52, 28), title);
            GUI.color = new Color(.58f, .66f, .71f); GUI.Label(new Rect(rect.x + 26, rect.y + 48, rect.width - 52, 22), subtitle);
            GUI.color = Color.white;
            return GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }

        private static void DrawConsolePanel(Rect rect, string title, string subtitle, Color accent)
        {
            GUI.color = new Color(.018f, .03f, .043f, .985f); GUI.Box(rect, "");
            GUI.color = accent; GUI.DrawTexture(new Rect(rect.x, rect.y, 4, rect.height), Texture2D.whiteTexture); GUI.DrawTexture(new Rect(rect.x + 26, rect.y + 24, 88, 3), Texture2D.whiteTexture);
            GUI.color = new Color(.9f, .95f, .97f); GUI.Label(new Rect(rect.x + 26, rect.y + 38, rect.width - 52, 32), title);
            GUI.color = new Color(.56f, .66f, .71f); GUI.Label(new Rect(rect.x + 26, rect.y + 78, rect.width - 52, 28), subtitle);
            GUI.color = Color.white;
        }

        private static bool DrawConsoleButton(Rect rect, string title, string subtitle, Color accent)
        {
            bool enabled = GUI.enabled;
            bool hover = enabled && rect.Contains(Event.current.mousePosition);
            GUI.color = enabled ? (hover ? new Color(accent.r, accent.g, accent.b, .22f) : new Color(.04f, .065f, .08f, 1f)) : new Color(.025f, .035f, .045f, 1f);
            GUI.Box(rect, ""); GUI.color = enabled ? accent : new Color(.28f, .32f, .34f); GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 3), Texture2D.whiteTexture);
            GUI.color = enabled ? new Color(.92f, .96f, .98f) : new Color(.38f, .42f, .44f); GUI.Label(new Rect(rect.x + 18, rect.y + 10, rect.width - 36, 25), title);
            GUI.color = enabled ? new Color(.55f, .64f, .69f) : new Color(.3f, .34f, .36f); GUI.Label(new Rect(rect.x + 18, rect.y + 35, rect.width - 36, rect.height - 40), subtitle);
            GUI.color = Color.white;
            return GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }
        private void DrawRogueliteMenu()
        {
            if (rogueliteRun?.IsShortRun == true && rogueliteRun.ShortRun.Phase != ShortRoguelitePhase.FirstCombat && rogueliteRun.ShortRun.Phase != ShortRoguelitePhase.SecondCombat) { DrawShortRunInterlude(); return; }
            IReadOnlyList<TaskTemplate> templates = RogueliteDeveloperCatalog.OpenSandboxTemplates;
            TaskTemplate selected = templates[sandboxTemplateIndex % templates.Count];
            DrawConsolePanel(new Rect(330, 140, 1260, 800), "肉鸽行动配置", "随机种子 / 独立存档 / 无倒计时推进", new Color(1f, .72f, .24f));
            GUI.color = new Color(.58f, .66f, .71f); GUI.Label(new Rect(390, 260, 1080, 26), "故事链：死信号  /  工厂突破  /  最终导管。相同种子的目标与结算保持稳定。");
            DrawConsolePanel(new Rect(390, 330, 1140, 180), "最短完整肉鸽", "第一关 / 事件 / 收获 / 升级 / 第二关 / 结算", new Color(.35f, .9f, 1f));
            if (DrawConsoleButton(new Rect(420, 430, 300, 62), "新开两关肉鸽", "生成新的行动种子", new Color(.35f, .9f, 1f))) StartShortRoguelite(false);
            GUI.enabled = HasShortRogueliteSave; if (DrawConsoleButton(new Rect(760, 430, 300, 62), "继续两关肉鸽", "恢复独立存档", new Color(.35f, .9f, 1f))) StartShortRoguelite(true); GUI.enabled = true;
            if (DrawConsoleButton(new Rect(1100, 430, 300, 62), "删除两关存档", "仅删除最短肉鸽记录", new Color(.8f, .32f, .23f))) DeleteShortRogueliteSave();
            DrawConsolePanel(new Rect(390, 560, 1140, 190), "故事包与模板演练", "用于回归测试的旧版入口，和自由推进地图保持隔离", new Color(.56f, .66f, .71f));
            if (DrawConsoleButton(new Rect(420, 660, 300, 62), "新开旧故事包", "固定三任务故事链", new Color(.56f, .66f, .71f))) StartRogueliteStory(false);
            GUI.enabled = HasRogueliteSave; if (DrawConsoleButton(new Rect(760, 660, 300, 62), "继续旧故事包", "恢复故事包存档", new Color(.56f, .66f, .71f))) StartRogueliteStory(true); GUI.enabled = true;
            if (DrawConsoleButton(new Rect(1100, 660, 300, 62), "开始 " + selected.Type + " 演练", "当前模板", new Color(.56f, .66f, .71f))) StartRogueliteSandbox();
            if (DrawConsoleButton(new Rect(420, 790, 300, 52), "切换演练模板", "当前：" + selected.Type, new Color(.56f, .66f, .71f))) SelectNextSandboxTemplate();
            if (DrawConsoleButton(new Rect(1230, 790, 170, 52), "返回入口", "开发菜单", new Color(.56f, .66f, .71f))) CloseRogueliteMenu();
        }
        private void DrawMapRun()
        {
            Matrix4x4 previous = GUI.matrix; GUI.matrix = GUI.matrix * Matrix4x4.TRS(new Vector3(960, 540, 0), Quaternion.identity, Vector3.one * mapPanelScale) * Matrix4x4.TRS(new Vector3(-960, -540, 0), Quaternion.identity, Vector3.one);
            DrawConsolePanel(new Rect(330, 150, 1260, 760), "肉鸽区域推进", "自由回访网络 / 已清理房间永久安全 / 无时间压力", new Color(1f, .72f, .24f));
            GUI.color = new Color(.68f, .78f, .88f, mapPanelAlpha); GUI.Label(new Rect(390, 244, 560, 28), "种子 " + mapRun.Seed + "  /  等级 " + mapRun.Level + "  /  当前 " + RogueliteMapCatalog.Node(mapRun.CurrentNodeId).DisplayName); GUI.color = Color.white;
            DrawMapResourceChip(new Rect(980, 240, 110, 34), "零件", mapRun.Parts, new Color(.95f, .76f, .36f));
            DrawMapResourceChip(new Rect(1100, 240, 110, 34), "以太", mapRun.Aether, new Color(.35f, .9f, 1f));
            DrawMapResourceChip(new Rect(1220, 240, 110, 34), "补给", mapRun.Supplies, new Color(.48f, .78f, .66f));
            DrawMapResourceChip(new Rect(1340, 240, 110, 34), "权限卡", mapRun.AccessCards, new Color(.82f, .34f, .24f));
            if (mapRun.AwaitingReward)
            {
                GUI.Label(new Rect(390, 320, 900, 30), "战斗结算 // 等级 " + mapRun.Level + "  选择一项构筑奖励");
                IReadOnlyList<RogueliteReward> rewards = mapRun.CurrentRewards;
                for (int i = 0; i < rewards.Count; i++) if (DrawConsoleButton(new Rect(390 + i * 390, 390, 350, 86), rewards[i].DisplayName, rewards[i].BuildPath + " / " + (rewards[i].Kind == RogueliteRewardKind.Weapon ? "武器" : "法术"), new Color(1f, .72f, .24f))) ClaimMapReward(rewards[i].Id);
                GUI.Label(new Rect(390, 520, 900, 28), "选中后返回地图，奖励会注入下一场战斗构筑。"); GUI.matrix = previous; return;
            }
            GUI.Label(new Rect(390, 320, 1100, 30), "完整拓扑公开；相邻房间可自由往返，已清理战斗房永久安全。未知房型保持模糊；权限门不含时间压力。");
            if (DrawMapContentChoices()) { GUI.matrix = previous; return; }
            if (DrawMapWorkshop()) { GUI.matrix = previous; return; }
            DrawIndustrialMapBackdrop();
            DrawMapConnections();
            foreach (RogueliteMapNode node in RogueliteMapCatalog.Nodes) DrawMapNode(node);
            DrawMapLegend();
            if (DrawConsoleButton(new Rect(390, 820, 240, 48), "读取推进", "继续已有地图", new Color(.35f, .9f, 1f))) StartMapRoguelite(true);
            if (DrawConsoleButton(new Rect(1350, 820, 180, 48), "返回入口", "开发菜单", new Color(.56f, .66f, .71f))) ReturnToDeveloperMenu();
            GUI.matrix = previous;
        }
        private void PlayMapEntrance()
        {
            DOTween.Kill(this); mapPanelAlpha = 1f; mapPanelScale = 1f;
            // The map must remain readable even when an editor execution path does not tick tween updates.
            DOTween.To(() => mapPanelScale, value => mapPanelScale = value, 1.015f, .12f).SetLoops(2, LoopType.Yoyo).SetId(this);
        }
        private void DrawMapConnections()
        {
            foreach (RogueliteMapNode node in RogueliteMapCatalog.Nodes)
            foreach (string nextId in node.NextIds.Where(id => string.CompareOrdinal(node.Id, id) < 0))
            {
                RogueliteMapNode next = RogueliteMapCatalog.Node(nextId);
                Vector2 from = MapNodeCenter(node); Vector2 to = MapNodeCenter(next);
                RogueliteMapNodeVisualState fromState = mapRun.VisualStateFor(node.Id); RogueliteMapNodeVisualState toState = mapRun.VisualStateFor(next.Id);
                bool traversable = mapRun.IsNodeAvailable(node.Id) || mapRun.IsNodeAvailable(next.Id);
                bool explored = fromState == RogueliteMapNodeVisualState.Cleared || toState == RogueliteMapNodeVisualState.Cleared || fromState == RogueliteMapNodeVisualState.Visited || toState == RogueliteMapNodeVisualState.Visited;
                Color color = traversable ? new Color(.35f, .9f, 1f, .9f) : explored ? new Color(.34f, .68f, .61f, .65f) : (fromState == RogueliteMapNodeVisualState.Locked || toState == RogueliteMapNodeVisualState.Locked) ? new Color(.82f, .34f, .24f, .9f) : new Color(.23f, .3f, .34f, .55f);
                DrawMapLine(from, to, color);
            }
        }
        private void DrawIndustrialMapBackdrop()
        {
            DrawDistrict(new Rect(400, 332, 300, 430), "01  铁路前线", "入口 / 巡逻 / 补给", new Color(.20f, .48f, .56f, .17f));
            DrawDistrict(new Rect(708, 332, 510, 430), "02  工业网格", "中继 / 工坊 / 铸造", new Color(.42f, .36f, .22f, .16f));
            DrawDistrict(new Rect(1226, 332, 300, 430), "03  核心隔离区", "权限门 / 传输 / 核心", new Color(.52f, .20f, .16f, .17f));
            GUI.color = new Color(.37f, .49f, .52f, .22f);
            for (int y = 378; y <= 714; y += 82) GUI.DrawTexture(new Rect(430, y, 1060, 2), Texture2D.whiteTexture);
            GUI.color = new Color(.35f, .9f, 1f, .16f);
            GUI.DrawTexture(new Rect(444, 372, 1030, 5), Texture2D.whiteTexture);
            for (int x = 452; x < 1470; x += 56) GUI.DrawTexture(new Rect(x, 366, 12, 17), Texture2D.whiteTexture);
            GUI.color = new Color(1f, .72f, .24f, .14f);
            GUI.DrawTexture(new Rect(444, 706, 1030, 3), Texture2D.whiteTexture);
            for (int x = 466; x < 1470; x += 92) GUI.DrawTexture(new Rect(x, 700, 4, 15), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private static void DrawDistrict(Rect rect, string label, string detail, Color color)
        {
            GUI.color = color; GUI.Box(rect, "");
            GUI.color = new Color(.55f, .64f, .68f, .48f); GUI.Label(new Rect(rect.x + 14, rect.y + 12, rect.width - 28, 20), label);
            GUI.color = new Color(.45f, .53f, .57f, .38f); GUI.Label(new Rect(rect.x + 14, rect.y + 34, rect.width - 28, 18), detail);
            GUI.color = Color.white;
        }

        private static void DrawMapLegend()
        {
            GUI.color = new Color(.025f, .04f, .052f, .96f); GUI.Box(new Rect(670, 770, 620, 38), "");
            DrawLegendChip(new Rect(686, 780, 12, 12), new Color(.35f, .9f, 1f), "可走");
            DrawLegendChip(new Rect(790, 780, 12, 12), new Color(.34f, .72f, .62f), "已探索");
            DrawLegendChip(new Rect(910, 780, 12, 12), new Color(.82f, .34f, .24f), "权限门");
            DrawLegendChip(new Rect(1035, 780, 12, 12), new Color(.28f, .34f, .38f), "未知");
            GUI.color = Color.white;
        }

        private static void DrawLegendChip(Rect rect, Color color, string label)
        {
            GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = new Color(.67f, .73f, .76f); GUI.Label(new Rect(rect.x + 18, rect.y - 4, 78, 20), label);
        }

        private static void DrawMapResourceChip(Rect rect, string label, int value, Color accent)
        {
            GUI.color = new Color(.025f, .04f, .052f, .96f); GUI.Box(rect, "");
            GUI.color = accent; GUI.DrawTexture(new Rect(rect.x, rect.y, 3, rect.height), Texture2D.whiteTexture);
            GUI.color = new Color(.56f, .64f, .68f); GUI.Label(new Rect(rect.x + 12, rect.y + 4, rect.width - 18, 14), label);
            GUI.color = new Color(.92f, .95f, .96f); GUI.Label(new Rect(rect.x + 12, rect.y + 16, rect.width - 18, 18), value.ToString());
            GUI.color = Color.white;
        }
        private bool DrawMapContentChoices()
        {
            RogueliteMapNode node = RogueliteMapCatalog.Node(mapRun.CurrentNodeId);
            if (node.IsCombat || node.Type == RogueliteMapNodeType.Start || mapRun.CompletedNodes.Contains(node.Id)) return false;
            IReadOnlyList<RogueliteNodeContentChoice> choices = mapRun.CurrentContentChoices;
            if (choices.Count == 0) return false;
            DrawConsolePanel(new Rect(390, 350, 1140, 360), node.DisplayName + " / " + MapNodeTypeLabel(node.Type), "选择一项已预览的结算  /  当前节点不会自动推进", new Color(1f, .72f, .24f));
            GUI.color = new Color(.68f, .78f, .88f); GUI.Label(new Rect(440, 420, 1040, 44), node.Summary + "\n风险与收益在确认前完整公开；额外战斗不会强制扣血。"); GUI.color = Color.white;
            float gap = 18f; float cardWidth = (1040f - gap * (choices.Count - 1)) / Mathf.Max(1, choices.Count);
            for (int i = 0; i < choices.Count; i++)
            {
                RogueliteNodeContentChoice choice = choices[i];
                Rect card = new Rect(440 + i * (cardWidth + gap), 505, cardWidth, 112);
                if (DrawConsoleButton(card, choice.DisplayName, choice.Preview, new Color(1f, .72f, .24f))) ChooseMapNodeContent(choice.Id);
            }
            return true;
        }
        private bool DrawMapWorkshop()
        {
            RogueliteMapNode node = RogueliteMapCatalog.Node(mapRun.CurrentNodeId);
            if (node.Type != RogueliteMapNodeType.Workshop || !mapRun.CompletedNodes.Contains(node.Id)) return false;
            DrawConsolePanel(new Rect(390, 350, 1140, 360), "野战工坊", "装备替换与以太校准  /  奖励领取后不会自动装备", new Color(.35f, .9f, 1f));
            GUI.color = new Color(.68f, .78f, .88f); GUI.Label(new Rect(440, 420, 1040, 44), "当前配置：武器 " + (mapRun.EquippedWeaponId ?? "制式步枪") + "  /  术式 " + (mapRun.EquippedSpellId ?? "火矢") + "\n校准状态：" + (mapRun.IsAetherCalibrated ? "已完成，本局后续战斗生效" : "未完成，需要 2 以太")); GUI.color = Color.white;
            RogueliteReward[] owned = mapRun.ClaimedRewards.Select(id => RogueliteMapCatalog.Rewards.First(item => item.Id == id)).ToArray();
            for (int i = 0; i < owned.Length && i < 2; i++)
            {
                RogueliteReward reward = owned[i];
                if (DrawConsoleButton(new Rect(440 + i * 300, 510, 270, 70), "装备 " + reward.DisplayName, reward.Kind == RogueliteRewardKind.Weapon ? "武器  /  已拥有" : "术式  /  已拥有", new Color(.35f, .9f, 1f))) EquipMapReward(reward.Id);
            }
            GUI.enabled = !mapRun.IsAetherCalibrated && mapRun.Aether >= 2;
            if (DrawConsoleButton(new Rect(1040, 600, 400, 64), mapRun.IsAetherCalibrated ? "以太校准：已完成" : "以太校准", mapRun.IsAetherCalibrated ? "本局校准已注入" : "消耗 2 以太  /  下一场 +1 护甲", new Color(.35f, .9f, 1f))) CalibrateMapAether();
            GUI.enabled = true;
            return true;
        }
        private static Vector2 MapNodeCenter(RogueliteMapNode node) => new Vector2(486 + node.GridX * 145, 402 + node.GridY * 82);
        private void DrawMapLine(Vector2 from, Vector2 to, Color color)
        {
            Vector2 delta = to - from; float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            Color previous = GUI.color; Matrix4x4 previousMatrix = GUI.matrix; GUI.color = color;
            GUIUtility.RotateAroundPivot(angle, from);
            GUI.DrawTexture(new Rect(from.x, from.y - 2f, delta.magnitude, 4f), barTexture != null ? barTexture : Texture2D.whiteTexture);
            GUI.matrix = previousMatrix; GUI.color = previous;
        }
        private void DrawMapNode(RogueliteMapNode node)
        {
            RogueliteMapNodeVisualState visualState = mapRun.VisualStateFor(node.Id);
            bool available = visualState == RogueliteMapNodeVisualState.Available;
            Vector2 center = MapNodeCenter(node); Rect rect = new Rect(center.x - 54, center.y - 24, 108, 48);
            bool identified = visualState != RogueliteMapNodeVisualState.Unknown;
            string type = identified ? MapNodeTypeLabel(node.Type) : "未识别";
            string name = identified ? node.DisplayName : "未知房间";
            string state = MapNodeStateLabel(visualState, node);
            GUI.enabled = available;
            Color accent = MapNodeStateColor(visualState);
            if (DrawConsoleButton(rect, name, type + " / " + state, accent)) SelectMapNode(node.Id);
            GUI.enabled = true;
        }
        private static string MapNodeTypeLabel(RogueliteMapNodeType type) => type == RogueliteMapNodeType.Combat ? "战斗" : type == RogueliteMapNodeType.Elite ? "精英" : type == RogueliteMapNodeType.Event ? "事件" : type == RogueliteMapNodeType.Workshop ? "工坊" : type == RogueliteMapNodeType.Shop ? "商店" : type == RogueliteMapNodeType.Rest ? "休整" : type == RogueliteMapNodeType.Treasure ? "库房" : type == RogueliteMapNodeType.Finale ? "核心" : "入口";
        private static string MapNodeStateLabel(RogueliteMapNodeVisualState state, RogueliteMapNode node) => state == RogueliteMapNodeVisualState.Current ? "当前位置" : state == RogueliteMapNodeVisualState.Available ? "可进入" : state == RogueliteMapNodeVisualState.Locked ? "需权限卡 " + node.RequiredAccessCards : state == RogueliteMapNodeVisualState.Cleared ? "已清理" : state == RogueliteMapNodeVisualState.Visited ? "已访问" : state == RogueliteMapNodeVisualState.Known ? "已知未探索" : "未知";
        private static Color MapNodeStateColor(RogueliteMapNodeVisualState state) => state == RogueliteMapNodeVisualState.Current || state == RogueliteMapNodeVisualState.Available ? new Color(.35f, .9f, 1f) : state == RogueliteMapNodeVisualState.Cleared ? new Color(.34f, .72f, .62f) : state == RogueliteMapNodeVisualState.Locked ? new Color(.82f, .34f, .24f) : state == RogueliteMapNodeVisualState.Known ? new Color(1f, .72f, .24f) : new Color(.28f, .34f, .38f);
        private void DrawShortRunInterlude()
        {
            ShortRogueliteRun run = rogueliteRun.ShortRun;
            DrawConsolePanel(new Rect(500, 220, 920, 600), run.Phase == ShortRoguelitePhase.Event ? "现场事件：损坏的导流阀" : run.Phase == ShortRoguelitePhase.Salvage ? "收获：回收箱" : run.Phase == ShortRoguelitePhase.Upgrade ? "角色升级：校准台" : "两关肉鸽结算", "短局阶段选择", new Color(1f, .72f, .24f));
            if (run.Phase == ShortRoguelitePhase.Event)
            {
                GUI.Label(new Rect(550, 334, 760, 56), "用以太素修复护甲衬层。第二关获得 +1 护甲。");
                if (DrawConsoleButton(new Rect(550, 460, 350, 54), "执行现场修复", "第二关获得 +1 护甲", new Color(.35f, .9f, 1f))) ChooseShortEvent();
            }
            else if (run.Phase == ShortRoguelitePhase.Salvage)
            {
                GUI.Label(new Rect(550, 334, 760, 56), "回收一枚护盾电池。第二关快捷栏获得额外护盾电池。");
                if (DrawConsoleButton(new Rect(550, 460, 350, 54), "收取护盾电池", "第二关快捷栏追加道具", new Color(.35f, .9f, 1f))) ChooseShortSalvage();
            }
            else if (run.Phase == ShortRoguelitePhase.Upgrade)
            {
                GUI.Label(new Rect(550, 334, 760, 56), "校准主武器。第二关装备校准步枪，伤害从 4 提升到 5。");
                if (DrawConsoleButton(new Rect(550, 460, 350, 54), "安装校准组件", "校准步枪伤害 4 → 5", new Color(.35f, .9f, 1f))) { ChooseShortUpgrade(); OpenShortRunPhase(); }
            }
            else
            {
                GUI.Label(new Rect(550, 334, 760, 80), "两关行动完成。已应用：" + string.Join(" / ", run.Choices));
                if (DrawConsoleButton(new Rect(550, 460, 350, 54), "返回肉鸽配置", "结束当前短局", new Color(.56f, .66f, .71f))) { DeleteShortRogueliteSave(); rogueliteRun = null; }
            }
            if (DrawConsoleButton(new Rect(960, 650, 350, 54), "返回开发菜单", "离开当前流程", new Color(.56f, .66f, .71f))) ReturnToDeveloperMenu();
        }
        private void DrawDeveloperBriefing()
        {
            DrawConsolePanel(new Rect(390, 150, 1140, 760), "战前简报", mapRun == null ? "剧情行动 / 确认任务与撤回路径" : "肉鸽区域 / 确认当前节点与资源状态", new Color(.35f, .9f, 1f));
            GUI.color = new Color(.68f, .78f, .88f); GUI.Label(new Rect(450, 265, 1040, 30), "任务编号：" + developerPreparation.MissionId + "   /   节点：" + (mapRun == null ? "剧情测试" : RogueliteMapCatalog.Node(mapRun.CurrentNodeId).DisplayName)); GUI.Label(new Rect(450, 310, 1040, 30), "任务目标：" + developerPreparation.RulesSummary); GUI.Label(new Rect(450, 355, 1040, 30), "敌方编成：" + developerPreparation.EnemySummary); GUI.color = Color.white;
            GUI.color = new Color(.04f, .065f, .08f, .9f); GUI.Box(new Rect(450, 420, 1040, 110), "");
            GUI.color = new Color(.55f, .64f, .69f); GUI.Label(new Rect(475, 440, 980, 24), "行动规则");
            GUI.color = new Color(.9f, .94f, .96f); GUI.Label(new Rect(475, 472, 980, 42), "无倒计时  /  战术重开恢复本场初始状态  /  战斗结束后返回当前推进节点"); GUI.color = Color.white;
            if (mapRun != null)
            {
                DrawMapResourceChip(new Rect(450, 570, 150, 42), "零件", mapRun.Parts, new Color(.95f, .76f, .36f));
                DrawMapResourceChip(new Rect(620, 570, 150, 42), "以太", mapRun.Aether, new Color(.35f, .9f, 1f));
                DrawMapResourceChip(new Rect(790, 570, 150, 42), "补给", mapRun.Supplies, new Color(.48f, .78f, .66f));
                DrawMapResourceChip(new Rect(960, 570, 150, 42), "权限卡", mapRun.AccessCards, new Color(.82f, .34f, .24f));
            }
            if (rogueliteRun != null) GUI.Label(new Rect(450, 640, 1040, 28), "包：铁之回响 / 种子 " + rogueliteRun.Package.Seed + " / " + (rogueliteRun.Kind == RogueliteLaunchKind.StoryChain ? "故事链" : "模板沙盒"));
            if (DrawConsoleButton(new Rect(450, 745, 490, 64), "开始正式战斗", "进入已确认的任务", new Color(.35f, .9f, 1f))) StartDeveloperCombat();
            if (DrawConsoleButton(new Rect(1000, 745, 490, 64), mapRun != null ? "返回推进地图" : "返回开发菜单", "不改变当前规则", new Color(.56f, .66f, .71f))) { if (mapRun != null) ReturnToMapRun(); else ReturnToDeveloperMenu(); }
        }
        private void DrawDeveloperFlowBar()
        {
            developerFlow.RefreshOutcome();
            GUI.color = new Color(.018f, .03f, .043f, .98f); GUI.Box(new Rect(24, 930, 1866, 72), ""); GUI.color = new Color(.35f, .9f, 1f); GUI.DrawTexture(new Rect(24, 930, 1866, 3), Texture2D.whiteTexture);
            GUI.color = new Color(.9f, .95f, .97f); GUI.Label(new Rect(52, 950, 600, 30), "当前流程 // " + developerFlow.Phase);
            if ((developerFlow.Phase == CombatFlowPhase.Victory || developerFlow.Phase == CombatFlowPhase.Defeat) && DrawConsoleButton(new Rect(1450, 944, 190, 42), "战术重开", "恢复初始状态", new Color(.35f, .9f, 1f))) TacticalRestartDeveloperCombat();
            if (DrawConsoleButton(new Rect(1660, 944, 190, 42), "返回入口", "开发菜单", new Color(.56f, .66f, .71f))) ReturnToDeveloperMenu();
            if (developerFlow.Phase == CombatFlowPhase.Active && rogueliteRun != null)
            {
                if (DrawConsoleButton(new Rect(1240, 944, 95, 42), "胜利", "测试", new Color(.35f, .9f, 1f))) ForceCurrentOutcome(true);
                if (DrawConsoleButton(new Rect(1340, 944, 95, 42), "失败", "测试", new Color(.8f, .32f, .23f))) ForceCurrentOutcome(false);
            }
            if ((developerFlow.Phase == CombatFlowPhase.Victory || developerFlow.Phase == CombatFlowPhase.Defeat) && rogueliteRun != null && DrawConsoleButton(new Rect(1040, 944, 190, 42), developerFlow.Phase == CombatFlowPhase.Victory ? "继续 / 结算" : "返回肉鸽菜单", "推进当前流程", new Color(1f, .72f, .24f))) ContinueRogueliteAfterVictory();
            if ((developerFlow.Phase == CombatFlowPhase.Victory || developerFlow.Phase == CombatFlowPhase.Defeat) && mapRun != null && DrawConsoleButton(new Rect(1040, 944, 190, 42), developerFlow.Phase == CombatFlowPhase.Victory ? "查看战斗结算" : "返回推进地图", "区域推进", new Color(1f, .72f, .24f))) ReturnToMapRun();
        }
        private void DrawGrid(Rect board)
        {
            Event e = Event.current;
            for (int y = 0; y < state.Map.Height; y++) for (int x = 0; x < state.Map.Width; x++)
            {
                GridPosition p = new GridPosition(x, y);
                Rect cell = new Rect(board.x + x * CellSize, board.y + (state.Map.Height - 1 - y) * CellSize, CellSize - 2, CellSize - 2);
                TileState tile = state.Map.GetTile(p);
                GUI.color = tile.IsObjective ? new Color(.55f, .2f, .15f) : tile.Cover == CoverType.Light ? new Color(.3f, .32f, .18f) : tile.Cover == CoverType.Heavy ? new Color(.26f, .25f, .34f) : new Color(.13f, .16f, .18f);
                GUI.Box(cell, "");

                // Both core tactical ranges stay visible during the hero turn.
                if (IsInMoveRange(p))
                {
                    GUI.color = new Color(.1f, .8f, 1f, selectedAction == "\u79fb\u52a8" ? .75f : .35f);
                    GUI.DrawTexture(new Rect(cell.x + 2, cell.y + 2, cell.width - 4, cell.height - 4), Texture2D.whiteTexture);
                }
                if (IsInAttackRange(p)) DrawOutline(cell, new Color(.25f, .58f, 1f, selectedAction == "\u653b\u51fb" ? 1f : .7f));
                if ((selectedAction == "\u6280\u80fd1" || selectedAction == "\u6280\u80fd2") && IsInSelectedRange(p))
                {
                    GUI.color = new Color(1f, .45f, .1f, .75f);
                    GUI.DrawTexture(new Rect(cell.x + 5, cell.y + 5, cell.width - 10, cell.height - 10), Texture2D.whiteTexture);
                }

                GUI.color = Color.white;
                UnitState unit = state.Units.Values.FirstOrDefault(u => u.IsAlive && u.Position == p);
                if (unit != null)
                {
                    if (unit.Id == selectedTargetId) DrawOutline(cell, new Color(1f, .8f, .2f, 1f));
                    Texture2D unitTexture = FormalUnitTexture(unit);
                    if (unitTexture != null)
                    {
                        GUI.color = Color.white;
                        Rect unitRect = StaticUnitPresentationRect(unit, new Rect(cell.x + 4, cell.y + 4, cell.width - 8, cell.height - 8));
                        GUI.DrawTexture(unitRect, unitTexture, ScaleMode.ScaleToFit, true);
                    }
                    else
                    {
                        GUI.color = unit.IsHero ? new Color(.25f, .72f, 1f) : new Color(1f, .35f, .3f);
                        GUI.Box(new Rect(cell.x + 7, cell.y + 11, cell.width - 14, cell.height - 14), FacingGlyph(unit.Facing));
                    }
                    GUI.color = Color.white;
                    DrawUnitBars(unit, new Rect(cell.x + 5, cell.y + 3, cell.width - 10, 5));
                    if (!unit.IsHero) GUI.Label(new Rect(cell.x - 14, cell.y - 15, cell.width + 28, 16), GetEnemyIntent(unit));
                    DrawStatusMarkers(unit, cell);
                }
                if (tile.IsObjective && !tile.IsDestroyed) GUI.Label(new Rect(cell.x + 2, cell.y + 18, cell.width, 20), "\u4e2d\u7ee7\u5668");
                if (state.Loot != null && !state.Loot.IsLooted && state.Loot.Position == p)
                {
                    GUI.color = Color.white;
                    if (formalLootTexture != null)
                        GUI.DrawTexture(new Rect(cell.x + 12, cell.y + 12, cell.width - 24, cell.height - 24), formalLootTexture, ScaleMode.ScaleToFit, true);
                    else
                    {
                        GUI.color = new Color(1f, .78f, .18f);
                        GUI.Box(new Rect(cell.x + 15, cell.y + 16, cell.width - 30, cell.height - 30), "\u7269");
                    }
                    GUI.color = Color.white;
                }
                if (e.type == EventType.MouseDown && e.button == 0 && cell.Contains(e.mousePosition)) { HandleCellClick(p); e.Use(); }
            }
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
        private void DrawPanel(Rect rect) { GUI.Box(rect, "\u6218\u6597\u63a7\u5236\u53f0"); UnitState active = state.GetUnit(state.ActiveUnitId); UnitState hero = state.GetUnit("hero"); GUI.Label(new Rect(rect.x + 14, rect.y + 34, 280, 22), $"\u884c\u52a8\u5355\u4f4d：{active.DisplayName} | AP {active.ActionPoints}"); GUI.Label(new Rect(rect.x + 14, rect.y + 60, 280, 20), $"\u4e3b\u89d2\u8d44\u6e90：{hero.Health}/{hero.MaxHealth} HP  {hero.Shield} \u62a4\u76fe  {hero.Mana}/{hero.MaxMana} \u4ee5\u592a"); GUI.Label(new Rect(rect.x + 14, rect.y + 82, 280, 20), GetRangeDescription()); string[] actions = { "\u79fb\u52a8", "\u653b\u51fb", "\u65bd\u672f", "\u9053\u5177", "\u4e92\u52a8" }; for (int i = 0; i < actions.Length; i++) if (GUI.Toggle(new Rect(rect.x + 14 + (i % 2) * 136, rect.y + 108 + (i / 2) * 34, 128, 28), selectedAction == actions[i], actions[i], "Button")) selectedAction = actions[i]; if (GUI.Button(new Rect(rect.x + 14, rect.y + 216, 128, 30), "\u7ed3\u675f\u884c\u52a8")) TryCommand(CombatCommand.EndTurn("hero")); if (GUI.Button(new Rect(rect.x + 150, rect.y + 216, 128, 30), "\u6218\u672f\u91cd\u5f00")) { state = snapshot.Clone(); CombatResolver.BeginTurn(state, "hero"); } GUI.Label(new Rect(rect.x + 14, rect.y + 256, 280, 20), "\u884c\u52a8\u6761\uff1a\u6570\u503c\u8d8a\u4f4e\u8d8a\u5148\u884c\u52a8"); int row = 0; foreach (UnitState unit in state.Units.Values) { GUI.Label(new Rect(rect.x + 14, rect.y + 280 + row * 27, 125, 20), $"{unit.DisplayName} HP{unit.Health} \u62a4{unit.Shield}"); GUI.HorizontalScrollbar(new Rect(rect.x + 142, rect.y + 284 + row * 27, 130, 16), Math.Min(100, unit.InitiativeTime) / 100f, .12f, 0f, 1f); row++; } GUI.Label(new Rect(rect.x + 14, rect.y + 410, 280, 20), "\u654c\u4eba\u610f\u56fe\u548c\u6218\u6597\u8bb0\u5f55"); for (int i = 0; i < Math.Min(6, state.EventLog.Count); i++) GUI.Label(new Rect(rect.x + 14, rect.y + 434 + i * 18, 280, 18), state.EventLog[i]); }
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

        private static string StatusName(StatusType status) => status == StatusType.Burning ? "\u71c3\u70e7" : status == StatusType.Slow ? "\u7f13\u6162" : status == StatusType.Bound ? "\u675f\u7f1a" : "\u7834\u7532";

        private static void DrawStatusMarkers(UnitState unit, Rect cell)
        {
            int index = 0;
            foreach (KeyValuePair<StatusType, int> status in unit.Statuses)
            {
                GUI.color = StatusColor(status.Key);
                GUI.DrawTexture(new Rect(cell.x + 5 + index * 13, cell.yMax - 9, 10, 5), Texture2D.whiteTexture);
                index++;
            }
            GUI.color = Color.white;
        }

        private static Color StatusColor(StatusType status)
        {
            return status == StatusType.Burning ? new Color(1f, .34f, .2f) :
                status == StatusType.Bound ? new Color(.3f, .86f, 1f) :
                status == StatusType.ArmorBreak ? new Color(1f, .78f, .2f) : new Color(.45f, .78f, .7f);
        }

        private string GetRangeDescriptionStageTwo()
        {
            int count = 0;
            for (int y = 0; y < state.Map.Height; y++) for (int x = 0; x < state.Map.Width; x++) if (IsInSelectedRange(new GridPosition(x, y))) count++;
            UnitState hero = state.GetUnit("hero");
            string rule = selectedAction == "\u79fb\u52a8" ? "\u79fb\u52a8 3 \u683c" : selectedAction == "\u653b\u51fb" ? $"{hero.MainHand.DisplayName} {hero.MainHand.Range} \u683c" : selectedAction == "\u6280\u80fd1" ? $"{hero.SkillOne.DisplayName} {hero.SkillOne.Range} \u683c" : selectedAction == "\u6280\u80fd2" ? $"{hero.SkillTwo.DisplayName} {hero.SkillTwo.Range} \u683c" : selectedAction == "\u641c\u522e" ? "\u641c\u522e\uff1a\u76f8\u90bb 1 \u683c" : "\u4ea4\u4e92\uff1a\u76f8\u90bb 1 \u683c";
            return $"\u5f53\u524d\uff1a{rule} | \u9ad8\u4eae {count} \u683c";
        }

        private void HandleCellClick(GridPosition p) { if (state.ActiveUnitId != "hero" || state.IsVictory || state.IsDefeat) return; UnitState target = state.Units.Values.FirstOrDefault(u => !u.IsHero && u.IsAlive && u.Position == p); if (target != null) selectedTargetId = target.Id; if (selectedAction == "\u79fb\u52a8") TryCommand(CombatCommand.Move("hero", p, FacingToward(state.GetUnit("hero").Position, p))); else if (selectedAction == "\u653b\u51fb" && target != null) TryCommand(CombatCommand.Attack("hero", target.Id)); else if (selectedAction == "\u6280\u80fd1" && target != null) TryCommand(CombatCommand.UseSkill("hero", 0, target.Id)); else if (selectedAction == "\u6280\u80fd2" && target != null) TryCommand(CombatCommand.UseSkill("hero", 1, target.Id)); else if (selectedAction == "\u641c\u522e") TryCommand(CombatCommand.Loot("hero")); else if (selectedAction == "\u4e92\u52a8") TryCommand(CombatCommand.Interact("hero", p)); }
        private void TryCommand(CombatCommand command)
        {
            try
            {
                UnitState target = command.TargetUnitId == null ? null : state.Units.Values.FirstOrDefault(u => u.Id == command.TargetUnitId);
                int healthBefore = target == null ? 0 : target.Health;
                GridPosition source = state.GetUnit(command.UnitId).Position;
                int tileDurabilityBefore = command.Type == CombatCommandType.Interact && state.Map.IsInside(command.Destination) ? state.Map.GetTile(command.Destination).Durability : -1;
                CombatResolver.Resolve(state, command);
                if (target != null && healthBefore > target.Health) visualFeedback?.NotifyAttack(source, target.Position, healthBefore - target.Health, !target.IsAlive);
                if (tileDurabilityBefore >= 0 && state.Map.GetTile(command.Destination).Durability < tileDurabilityBefore) visualFeedback?.NotifyDestructible(command.Destination, state.Map.GetTile(command.Destination));
                if (state.ActiveUnitId == "hero" && state.GetUnit("hero").ActionPoints == 0) CombatResolver.EndTurn(state, state.GetUnit("hero")); developerFlow.RefreshOutcome();
            }
            catch (InvalidOperationException error) { state.AddLog(error.Message); }
        }
        private void DrawUnitBars(UnitState unit, Rect rect) { GUI.color = Color.black; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = unit.IsHero ? new Color(.2f, .85f, .45f) : new Color(.9f, .22f, .22f); GUI.DrawTexture(new Rect(rect.x + 1, rect.y + 1, (rect.width - 2) * unit.Health / unit.MaxHealth, rect.height - 2), Texture2D.whiteTexture); GUI.color = Color.white; }
        private string GetEnemyIntent(UnitState enemy) => Distance(enemy.Position, state.GetUnit("hero").Position) <= 4 ? (enemy.Id == "caster" ? "\u706b\u672f" : "\u653b\u51fb") : "\u9760\u8fd1";
        private string GetRangeDescription() { int count = 0; if (state != null) for (int y = 0; y < state.Map.Height; y++) for (int x = 0; x < state.Map.Width; x++) if (IsInSelectedRange(new GridPosition(x, y))) count++; string rule = selectedAction == "\u79fb\u52a8" ? "\u79fb\u52a8\u8303\u56f4：3 \u683c" : selectedAction == "\u653b\u51fb" ? "\u653b\u51fb\u8303\u56f4：4 \u683c" : selectedAction == "\u65bd\u672f" ? "\u706b\u672f\u8303\u56f4：5 \u683c" : selectedAction == "\u4e92\u52a8" ? "\u4e92\u52d5\u8303\u56f4：1 \u683c" : "\u9053\u5177：\u81ea\u8eab\u4f7f\u7528"; return rule + "  |  \u9ad8\u4eae " + count + " \u683c"; }
        private bool IsInSelectedRange(GridPosition p)
        {
            if (state == null || state.ActiveUnitId != "hero") return false;
            UnitState hero = state.GetUnit("hero");
            int distance = Distance(hero.Position, p);
            if (selectedAction == "\u79fb\u52a8") return IsInMoveRange(p);
            if (selectedAction == "\u653b\u51fb") return IsInAttackRange(p);
            if (selectedAction == "\u6280\u80fd1") return distance > 0 && distance <= hero.SkillOne.Range && state.Map.HasLineOfSight(hero.Position, p);
            if (selectedAction == "\u6280\u80fd2") return distance > 0 && distance <= hero.SkillTwo.Range && state.Map.HasLineOfSight(hero.Position, p);
            if (selectedAction == "\u641c\u522e") return state.Loot != null && !state.Loot.IsLooted && p == state.Loot.Position && distance == 1;
            if (selectedAction == "\u4e92\u52a8") return distance == 1;
            return false;
        }

        private bool IsInMoveRange(GridPosition p)
        {
            if (state == null || state.ActiveUnitId != "hero") return false;
            UnitState hero = state.GetUnit("hero");
            int distance = Distance(hero.Position, p);
            return distance > 0 && distance <= 3 && state.Map.IsInside(p) && !state.Map.IsBlocked(p) && !state.IsOccupied(p);
        }

        private bool IsInAttackRange(GridPosition p)
        {
            if (state == null || state.ActiveUnitId != "hero") return false;
            UnitState hero = state.GetUnit("hero");
            int distance = Distance(hero.Position, p);
            return distance > 0 && distance <= hero.MainHand.Range && state.Map.HasLineOfSight(hero.Position, p);
        }
        private static int Distance(GridPosition a, GridPosition b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
        private static GridPosition StepToward(GridPosition a, GridPosition b) => Math.Abs(b.X - a.X) >= Math.Abs(b.Y - a.Y) ? new GridPosition(a.X + Math.Sign(b.X - a.X), a.Y) : new GridPosition(a.X, a.Y + Math.Sign(b.Y - a.Y));
        private static Facing FacingToward(GridPosition a, GridPosition b) => Math.Abs(b.X - a.X) >= Math.Abs(b.Y - a.Y) ? (b.X >= a.X ? Facing.East : Facing.West) : (b.Y >= a.Y ? Facing.North : Facing.South);
        private static string FacingGlyph(Facing facing) => facing == Facing.North ? "^" : facing == Facing.South ? "v" : facing == Facing.East ? ">" : "<";
    }
}
