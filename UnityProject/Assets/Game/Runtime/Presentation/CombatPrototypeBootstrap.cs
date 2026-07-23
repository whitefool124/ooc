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
        private const string RogueliteSaveKey = "occ.roguelite.iron_echoes";
        private const string ShortRogueliteSaveKey = "occ.roguelite.short_run";
        private const string MapRogueliteSaveKey = "occ.roguelite.map_run";
        private CombatVisualFeedback visualFeedback;
        private RogueliteSettlementPresentation settlementPresentation;
        private float mapPanelAlpha = 1f;
        private float mapPanelScale = 1f;

        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            if (initialized) return;
            initialized = true;
            chineseFont = Resources.Load<Font>("Fonts/SimHei");
            barTexture = Resources.Load<Texture2D>("UI/Bar");
            Transform sceneUi = transform.Find("场景UI");
            if (sceneUi != null) sceneUi.gameObject.SetActive(true);
            developerPreparation = new MissionPreparation().Configure("relay_test", "破坏任务目标并清理威胁", "步枪兵、盾卫、火术师、突袭者、精英先锋");
            visualFeedback = gameObject.AddComponent<CombatVisualFeedback>(); visualFeedback.Initialize(this);
            settlementPresentation = gameObject.AddComponent<RogueliteSettlementPresentation>(); settlementPresentation.Initialize(this);
            BuildCombatFromSceneStageTwo();
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
            Sprite sprite = CreateEditorSprite();
            for (int y = 0; y < 9; y++) for (int x = 0; x < 12; x++)
            {
                GameObject tile = new GameObject("格_" + x + "_" + y); tile.transform.SetParent(root.transform, false); tile.transform.position = new Vector3(x, y, 2f); tile.transform.localScale = Vector3.one * .96f;
                SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.color = new Color(.12f, .19f, .29f, 1f); renderer.sortingOrder = -10;
            }
            AddEditorMarker(root, "轻掩体_A", new Vector3(4, 2, 1), new Color(.42f, .42f, .18f, 1f));
            AddEditorMarker(root, "轻掩体_B", new Vector3(6, 5, 1), new Color(.42f, .42f, .18f, 1f));
            AddEditorMarker(root, "重掩体_A", new Vector3(7, 3, 1), new Color(.34f, .28f, .48f, 1f));
            AddEditorMarker(root, "重掩体_B", new Vector3(8, 6, 1), new Color(.34f, .28f, .48f, 1f));
            AddEditorMarker(root, "目标_中继器", new Vector3(10, 4, 1), new Color(.7f, .25f, .16f, 1f));
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

        private static void AddEditorMarker(GameObject root, string name, Vector3 position, Color color)
        {
            GameObject marker = new GameObject(name); marker.transform.SetParent(root.transform, false); marker.transform.position = position; marker.transform.localScale = Vector3.one * .72f; SpriteRenderer renderer = marker.AddComponent<SpriteRenderer>(); renderer.sprite = CreateEditorSprite(); renderer.color = color; renderer.sortingOrder = -5;
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
        public RogueliteMapRun CurrentMapRun => mapRun;
        public bool IsDeveloperCombatActive => developerFlow != null && developerFlow.Phase == CombatFlowPhase.Active;
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
        private void RefreshSceneHud() { TacticalHudSceneBinder binder = transform.Find("场景UI")?.GetComponent<TacticalHudSceneBinder>(); if (binder != null) binder.RefreshNow(); }
        private void RunEnemyTurn() { UnitState enemy = state.GetUnit(state.ActiveUnitId); UnitState hero = state.GetUnit("hero"); if (enemy == null || !enemy.IsAlive) { if (enemy != null) CombatResolver.EndTurn(state, enemy); return; } try { if (Distance(enemy.Position, hero.Position) <= 4 && enemy.ActionPoints > 0) CombatResolver.Resolve(state, enemy.Id == "caster" ? CombatCommand.Cast(enemy.Id, hero.Id) : CombatCommand.Attack(enemy.Id, hero.Id)); else if (enemy.ActionPoints > 0) CombatResolver.Resolve(state, CombatCommand.Move(enemy.Id, StepToward(enemy.Position, hero.Position), FacingToward(enemy.Position, hero.Position))); if (state.ActiveUnitId == enemy.Id) CombatResolver.EndTurn(state, enemy); } catch (InvalidOperationException error) { state.AddLog(error.Message); CombatResolver.EndTurn(state, enemy); } }
        private void OnGUI() { if (!Application.isPlaying || developerFlow == null) return; float scale = Mathf.Min(Screen.width / UiWidth, Screen.height / UiHeight); Vector2 offset = new Vector2((Screen.width - UiWidth * scale) * .5f, (Screen.height - UiHeight * scale) * .5f); Matrix4x4 previous = GUI.matrix; GUI.matrix = Matrix4x4.TRS(offset, Quaternion.identity, Vector3.one * scale); ConfigureGuiSkin(); if (developerFlow.Phase == CombatFlowPhase.DeveloperMenu) { if (mapMenuOpen) DrawMapRun(); else if (rogueliteMenuOpen) DrawRogueliteMenu(); else DrawDeveloperMenu(); GUI.matrix = previous; return; } if (developerFlow.Phase == CombatFlowPhase.Briefing) { DrawDeveloperBriefing(); GUI.matrix = previous; return; } DrawHeader(); DrawGrid(new Rect(24, 112, 12 * CellSize, 9 * CellSize)); DrawDeveloperFlowBar(); GUI.matrix = previous; }
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
            GUI.color = new Color(.035f, .075f, .13f, .98f); GUI.Box(new Rect(540, 250, 840, 470), ""); GUI.color = Color.white;
            GUI.Label(new Rect(590, 300, 740, 38), "OCC  开发测试菜单"); GUI.color = new Color(.68f, .78f, .88f); GUI.Label(new Rect(590, 352, 740, 28), "从战前简报进入一场完整战斗，并可随时战术重开。"); GUI.color = Color.white;
            GUI.Label(new Rect(590, 410, 740, 28), "任务：" + developerPreparation.MissionId + "  ·  破坏目标"); GUI.Label(new Rect(590, 448, 740, 28), "敌人：" + developerPreparation.EnemySummary);
            if (GUI.Button(new Rect(590, 510, 350, 54), "剧情测试：查看战前简报")) OpenDeveloperBriefing();
            if (GUI.Button(new Rect(980, 510, 350, 54), "肉鸽地图：开始推进")) StartMapRoguelite(false);
            GUI.color = new Color(.35f, .9f, 1f); GUI.Label(new Rect(590, 620, 740, 28), "开发菜单  →  战前简报  →  战斗  →  结算  →  战术重开"); GUI.color = Color.white;
        }
        private void DrawRogueliteMenu()
        {
            if (rogueliteRun?.IsShortRun == true && rogueliteRun.ShortRun.Phase != ShortRoguelitePhase.FirstCombat && rogueliteRun.ShortRun.Phase != ShortRoguelitePhase.SecondCombat) { DrawShortRunInterlude(); return; }
            IReadOnlyList<TaskTemplate> templates = RogueliteDeveloperCatalog.OpenSandboxTemplates;
            TaskTemplate selected = templates[sandboxTemplateIndex % templates.Count];
            GUI.color = new Color(.035f, .075f, .13f, .98f); GUI.Box(new Rect(390, 170, 1140, 720), ""); GUI.color = Color.white;
            GUI.Label(new Rect(440, 216, 960, 38), "OCC  肉鸽测试配置");
            GUI.color = new Color(.68f, .78f, .88f); GUI.Label(new Rect(440, 268, 900, 28), "故事链：死信号  →  工厂突破  →  最终导管"); GUI.Label(new Rect(440, 302, 900, 28), "每次新开生成并保存随机种子；相同种子的目标与结算保持稳定。"); GUI.color = Color.white;
            GUI.Label(new Rect(440, 370, 500, 28), "最短完整肉鸽");
            if (GUI.Button(new Rect(440, 408, 300, 52), "新开两关肉鸽")) StartShortRoguelite(false);
            GUI.enabled = HasShortRogueliteSave; if (GUI.Button(new Rect(760, 408, 300, 52), "继续两关肉鸽")) StartShortRoguelite(true); GUI.enabled = true;
            if (GUI.Button(new Rect(1080, 408, 300, 52), "删除两关存档")) DeleteShortRogueliteSave();
            GUI.Label(new Rect(440, 480, 840, 25), "第一关 → 事件 → 收获 → 升级 → 第二关 → 结算；每次选择都会影响第二关。");
            GUI.Label(new Rect(440, 530, 500, 28), "旧版故事包/模板演练");
            if (GUI.Button(new Rect(440, 568, 300, 52), "新开旧故事包")) StartRogueliteStory(false);
            GUI.enabled = HasRogueliteSave; if (GUI.Button(new Rect(760, 568, 300, 52), "继续旧故事包")) StartRogueliteStory(true); GUI.enabled = true;
            if (GUI.Button(new Rect(1080, 568, 300, 52), "开始 " + selected.Type + " 演练")) StartRogueliteSandbox();
            if (GUI.Button(new Rect(440, 640, 300, 42), "切换演练模板")) SelectNextSandboxTemplate();
            if (GUI.Button(new Rect(1080, 640, 300, 42), "返回测试模式")) CloseRogueliteMenu();
            GUI.color = new Color(.35f, .9f, 1f); GUI.Label(new Rect(440, 706, 900, 28), "事件：现场修复(+1 护甲)  收获：护盾电池  升级：校准步枪(伤害 5)。"); GUI.color = Color.white;
        }
        private void DrawMapRun()
        {
            Matrix4x4 previous = GUI.matrix; GUI.matrix = GUI.matrix * Matrix4x4.TRS(new Vector3(960, 540, 0), Quaternion.identity, Vector3.one * mapPanelScale) * Matrix4x4.TRS(new Vector3(-960, -540, 0), Quaternion.identity, Vector3.one);
            GUI.color = new Color(.035f, .075f, .13f, .98f * mapPanelAlpha); GUI.Box(new Rect(330, 150, 1260, 760), ""); GUI.color = Color.white;
            GUI.Label(new Rect(390, 200, 900, 38), "OCC  肉鸽推进地图");
            GUI.color = new Color(.68f, .78f, .88f); GUI.Label(new Rect(390, 250, 1140, 28), "种子 " + mapRun.Seed + " / 等级 " + mapRun.Level + " / 零件 " + mapRun.Parts + " / 以太 " + mapRun.Aether + " / 补给 " + mapRun.Supplies + " / 信标 " + mapRun.ScoutingBeacons + " / 权限卡 " + mapRun.AccessCards); GUI.color = Color.white;
            if (mapRun.AwaitingReward)
            {
                GUI.Label(new Rect(390, 320, 900, 30), "战斗成功结算：等级 " + mapRun.Level + "。从 3 个随机法术/武器中选择 1 个：");
                IReadOnlyList<RogueliteReward> rewards = mapRun.CurrentRewards;
                for (int i = 0; i < rewards.Count; i++) if (GUI.Button(new Rect(390 + i * 390, 390, 350, 86), rewards[i].DisplayName + " / " + rewards[i].BuildPath + "\n" + (rewards[i].Kind == RogueliteRewardKind.Weapon ? "武器" : "法术"))) ClaimMapReward(rewards[i].Id);
                GUI.Label(new Rect(390, 520, 900, 28), "选中后返回地图，奖励会注入下一场战斗构筑。"); GUI.matrix = previous; return;
            }
            GUI.Label(new Rect(390, 320, 1100, 30), "完整拓扑公开；相邻房间可自由往返，已清理战斗房永久安全。未知房型保持模糊；权限门不含时间压力。");
            if (DrawMapContentChoices()) { GUI.matrix = previous; return; }
            if (DrawMapWorkshop()) { GUI.matrix = previous; return; }
            DrawMapConnections();
            foreach (RogueliteMapNode node in RogueliteMapCatalog.Nodes) DrawMapNode(node);
            if (GUI.Button(new Rect(390, 820, 240, 42), "继续已有地图")) StartMapRoguelite(true);
            if (GUI.Button(new Rect(1350, 820, 180, 42), "返回菜单")) ReturnToDeveloperMenu();
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
                Color color = mapRun.AccessCards >= Math.Max(node.RequiredAccessCards, next.RequiredAccessCards) ? new Color(.22f, .5f, .58f, .8f) : new Color(.62f, .3f, .18f, .85f);
                DrawMapLine(from, to, color);
            }
        }
        private bool DrawMapContentChoices()
        {
            RogueliteMapNode node = RogueliteMapCatalog.Node(mapRun.CurrentNodeId);
            if (node.IsCombat || node.Type == RogueliteMapNodeType.Start || mapRun.CompletedNodes.Contains(node.Id)) return false;
            IReadOnlyList<RogueliteNodeContentChoice> choices = mapRun.CurrentContentChoices;
            if (choices.Count == 0) return false;
            GUI.color = new Color(.06f, .11f, .17f, .98f); GUI.Box(new Rect(430, 385, 1060, 270), ""); GUI.color = Color.white;
            GUI.Label(new Rect(480, 415, 940, 32), node.DisplayName + " / " + node.Type + "：选择一项已预览的结算");
            GUI.color = new Color(.68f, .78f, .88f); GUI.Label(new Rect(480, 455, 940, 50), node.Summary + " 事件失败只会进入标明的额外战斗，不会强制扣血。"); GUI.color = Color.white;
            for (int i = 0; i < choices.Count; i++)
            {
                RogueliteNodeContentChoice choice = choices[i];
                if (GUI.Button(new Rect(480 + i * 480, 530, 440, 72), choice.DisplayName + "\n" + choice.Preview)) ChooseMapNodeContent(choice.Id);
            }
            return true;
        }
        private bool DrawMapWorkshop()
        {
            RogueliteMapNode node = RogueliteMapCatalog.Node(mapRun.CurrentNodeId);
            if (node.Type != RogueliteMapNodeType.Workshop || !mapRun.CompletedNodes.Contains(node.Id)) return false;
            GUI.color = new Color(.06f, .11f, .17f, .98f); GUI.Box(new Rect(430, 385, 1060, 270), ""); GUI.color = Color.white;
            GUI.Label(new Rect(480, 415, 940, 32), "野战工坊 / 仅可装备本局已获得的奖励");
            GUI.Label(new Rect(480, 455, 940, 32), "当前：武器 " + (mapRun.EquippedWeaponId ?? "制式步枪") + " / 术式 " + (mapRun.EquippedSpellId ?? "火矢") + " / 校准 " + (mapRun.IsAetherCalibrated ? "已完成" : "未完成"));
            RogueliteReward[] owned = mapRun.ClaimedRewards.Select(id => RogueliteMapCatalog.Rewards.First(item => item.Id == id)).ToArray();
            for (int i = 0; i < owned.Length && i < 2; i++)
            {
                RogueliteReward reward = owned[i];
                if (GUI.Button(new Rect(480 + i * 300, 520, 270, 54), "装备 " + reward.DisplayName + " / " + (reward.Kind == RogueliteRewardKind.Weapon ? "武器" : "术式"))) EquipMapReward(reward.Id);
            }
            GUI.enabled = !mapRun.IsAetherCalibrated && mapRun.Aether >= 2;
            if (GUI.Button(new Rect(1100, 520, 290, 54), mapRun.IsAetherCalibrated ? "以太校准：已完成" : "以太校准：2 以太 / +1 护甲")) CalibrateMapAether();
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
            bool visited = mapRun.VisitedNodes.Contains(node.Id); bool completed = mapRun.CompletedNodes.Contains(node.Id);
            bool available = mapRun.IsNodeAvailable(node.Id); bool known = mapRun.IsNodeKnown(node.Id);
            Vector2 center = MapNodeCenter(node); Rect rect = new Rect(center.x - 54, center.y - 24, 108, 48);
            bool identified = visited || known;
            string type = identified ? node.Type.ToString() : "???";
            string name = identified ? node.DisplayName : "未知房间";
            string state = node.Id == mapRun.CurrentNodeId ? "当前位置" : completed ? (node.IsCombat ? "安全" : "已访问") : available ? "可进入" : node.RequiredAccessCards > mapRun.AccessCards ? "权限门" : "未接壤";
            GUI.enabled = available;
            if (GUI.Button(rect, name + "\n" + type + " / " + state)) SelectMapNode(node.Id);
            GUI.enabled = true;
        }
        private void DrawShortRunInterlude()
        {
            ShortRogueliteRun run = rogueliteRun.ShortRun;
            GUI.color = new Color(.035f, .075f, .13f, .98f); GUI.Box(new Rect(500, 220, 920, 600), ""); GUI.color = Color.white;
            GUI.Label(new Rect(550, 270, 800, 38), run.Phase == ShortRoguelitePhase.Event ? "现场事件：损坏的导流阀" : run.Phase == ShortRoguelitePhase.Salvage ? "收获：回收箱" : run.Phase == ShortRoguelitePhase.Upgrade ? "角色升级：校准台" : "两关肉鸽结算");
            if (run.Phase == ShortRoguelitePhase.Event)
            {
                GUI.Label(new Rect(550, 334, 760, 56), "用以太素修复护甲衬层。第二关获得 +1 护甲。");
                if (GUI.Button(new Rect(550, 460, 350, 54), "执行现场修复")) ChooseShortEvent();
            }
            else if (run.Phase == ShortRoguelitePhase.Salvage)
            {
                GUI.Label(new Rect(550, 334, 760, 56), "回收一枚护盾电池。第二关快捷栏获得额外护盾电池。");
                if (GUI.Button(new Rect(550, 460, 350, 54), "收取护盾电池")) ChooseShortSalvage();
            }
            else if (run.Phase == ShortRoguelitePhase.Upgrade)
            {
                GUI.Label(new Rect(550, 334, 760, 56), "校准主武器。第二关装备校准步枪，伤害从 4 提升到 5。");
                if (GUI.Button(new Rect(550, 460, 350, 54), "安装校准组件")) { ChooseShortUpgrade(); OpenShortRunPhase(); }
            }
            else
            {
                GUI.Label(new Rect(550, 334, 760, 80), "两关行动完成。已应用：" + string.Join(" / ", run.Choices));
                if (GUI.Button(new Rect(550, 460, 350, 54), "返回肉鸽配置")) { DeleteShortRogueliteSave(); rogueliteRun = null; }
            }
            if (GUI.Button(new Rect(960, 650, 350, 54), "返回开发菜单")) ReturnToDeveloperMenu();
        }
        private void DrawDeveloperBriefing()
        {
            GUI.color = new Color(.035f, .075f, .13f, .98f); GUI.Box(new Rect(500, 220, 920, 580), ""); GUI.color = Color.white;
            GUI.Label(new Rect(550, 270, 800, 38), "OCC  战前简报");
            GUI.color = new Color(.68f, .78f, .88f); GUI.Label(new Rect(550, 326, 800, 28), "任务编号：" + developerPreparation.MissionId); GUI.Label(new Rect(550, 364, 800, 28), "任务目标：" + developerPreparation.RulesSummary); GUI.Label(new Rect(550, 402, 800, 28), "敌方编成：" + developerPreparation.EnemySummary); GUI.color = Color.white;
            string context = rogueliteRun == null ? "行动准则：战术重开会恢复到本次战斗的初始状态。" : "肉鸽测试 | 模板：" + rogueliteRun.CurrentMission.TemplateId + " | 失败条件：" + rogueliteRun.CurrentMission.FailureSummary;
            GUI.Label(new Rect(550, 470, 800, 28), context);
            if (rogueliteRun != null) GUI.Label(new Rect(550, 506, 800, 28), "包：铁之回响 / 种子 " + rogueliteRun.Package.Seed + " / " + (rogueliteRun.Kind == RogueliteLaunchKind.StoryChain ? "故事链" : "模板沙盒"));
            if (GUI.Button(new Rect(550, 620, 350, 54), "开始正式战斗")) StartDeveloperCombat();
            if (GUI.Button(new Rect(960, 620, 350, 54), mapRun != null ? "返回推进地图" : "返回开发菜单")) { if (mapRun != null) ReturnToMapRun(); else ReturnToDeveloperMenu(); }
        }
        private void DrawDeveloperFlowBar()
        {
            developerFlow.RefreshOutcome();
            GUI.color = new Color(.035f, .075f, .13f, .98f); GUI.Box(new Rect(24, 930, 1866, 72), ""); GUI.color = Color.white;
            GUI.Label(new Rect(52, 950, 600, 30), "当前流程：" + developerFlow.Phase);
            if ((developerFlow.Phase == CombatFlowPhase.Victory || developerFlow.Phase == CombatFlowPhase.Defeat) && GUI.Button(new Rect(1450, 944, 190, 42), "战术重开")) TacticalRestartDeveloperCombat();
            if (GUI.Button(new Rect(1660, 944, 190, 42), "返回开发菜单")) ReturnToDeveloperMenu();
            if (developerFlow.Phase == CombatFlowPhase.Active && rogueliteRun != null)
            {
                if (GUI.Button(new Rect(1240, 944, 95, 42), "测试胜利")) ForceCurrentOutcome(true);
                if (GUI.Button(new Rect(1340, 944, 95, 42), "测试失败")) ForceCurrentOutcome(false);
            }
            if ((developerFlow.Phase == CombatFlowPhase.Victory || developerFlow.Phase == CombatFlowPhase.Defeat) && rogueliteRun != null && GUI.Button(new Rect(1040, 944, 190, 42), developerFlow.Phase == CombatFlowPhase.Victory ? "继续/结算" : "返回肉鸽菜单")) ContinueRogueliteAfterVictory();
            if ((developerFlow.Phase == CombatFlowPhase.Victory || developerFlow.Phase == CombatFlowPhase.Defeat) && mapRun != null && GUI.Button(new Rect(1040, 944, 190, 42), developerFlow.Phase == CombatFlowPhase.Victory ? "查看战斗结算" : "返回推进地图")) ReturnToMapRun();
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
                    GUI.color = unit.IsHero ? new Color(.25f, .72f, 1f) : new Color(1f, .35f, .3f);
                    GUI.Box(new Rect(cell.x + 7, cell.y + 11, cell.width - 14, cell.height - 14), FacingGlyph(unit.Facing));
                    GUI.color = Color.white;
                    DrawUnitBars(unit, new Rect(cell.x + 5, cell.y + 3, cell.width - 10, 5));
                    if (!unit.IsHero) GUI.Label(new Rect(cell.x - 14, cell.y - 15, cell.width + 28, 16), GetEnemyIntent(unit));
                    DrawStatusMarkers(unit, cell);
                }
                if (tile.IsObjective && !tile.IsDestroyed) GUI.Label(new Rect(cell.x + 2, cell.y + 18, cell.width, 20), "\u4e2d\u7ee7\u5668");
                if (state.Loot != null && !state.Loot.IsLooted && state.Loot.Position == p)
                {
                    GUI.color = new Color(1f, .78f, .18f);
                    GUI.Box(new Rect(cell.x + 15, cell.y + 16, cell.width - 30, cell.height - 30), "\u7269");
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
