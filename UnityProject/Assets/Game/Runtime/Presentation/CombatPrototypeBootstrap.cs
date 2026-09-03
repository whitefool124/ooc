using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using OCC.Combat.Roguelite;

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
        private readonly CombatSessionLifecycleController combatSession = new CombatSessionLifecycleController();
        private readonly CombatCommandExecutionService commandExecution = new CombatCommandExecutionService();
        private readonly CombatFeedbackPublisher feedbackPublisher = new CombatFeedbackPublisher();
        private readonly CombatSelectionController selection = new CombatSelectionController();
        private readonly CombatSceneSessionBuilder sceneSessionBuilder = new CombatSceneSessionBuilder();
        private readonly CombatTargetForecastService targetForecasts = new CombatTargetForecastService();
        private CombatBattlefieldCellPresenter battlefieldCells;
        private CombatState state;
        private FirstRegionLevelDefinition currentLevel;
        // Legacy panel helpers still use this editor-only snapshot; active flow restarts use developerFlow.
        private CombatState snapshot;
        private Font chineseFont;
        private Texture2D barTexture;
        private bool initialized;
        private MissionPreparation developerPreparation;
        private CombatFlowController developerFlow;
        private readonly RogueliteFlowCoordinator rogueliteFlow = new RogueliteFlowCoordinator();
        private RogueliteDeveloperRun rogueliteRun { get => rogueliteFlow.DeveloperRun; set => rogueliteFlow.SetDeveloperRun(value); }
        private int sandboxTemplateIndex;
        private bool rogueliteMenuOpen { get => rogueliteFlow.IsRogueliteMenuOpen; set => rogueliteFlow.SetRogueliteMenuOpen(value); }
        private readonly CombatOutcomeSettlementCoordinator outcomeSettlement = new CombatOutcomeSettlementCoordinator();
        private RogueliteMapRun mapRun { get => rogueliteFlow.MapRun; set => rogueliteFlow.SetMapRun(value); }
        private bool mapMenuOpen { get => rogueliteFlow.IsMapMenuOpen; set => rogueliteFlow.SetMapMenuOpen(value); }
        private readonly CombatFormalVisualAssets formalAssets = new CombatFormalVisualAssets();
        private CombatBattlefieldCellPresenter BattlefieldCells => battlefieldCells ??
            (battlefieldCells = new CombatBattlefieldCellPresenter(battlefield, formalAssets));
        private readonly RogueliteSaveGateway saveGateway = new RogueliteSaveGateway(new PlayerPrefsRogueliteSaveStore());
        private readonly RogueliteMapSaveCoordinator mapSaves = new RogueliteMapSaveCoordinator(
            new RogueliteSaveGateway(new PlayerPrefsRogueliteSaveStore()));
        private readonly RogueliteMapInteractionService mapInteractions = new RogueliteMapInteractionService();
        private CombatPresentationComposition presentation;
        private CombatVisualFeedback visualFeedback => presentation?.Feedback;
        private RogueliteSettlementPresentation settlementPresentation => presentation?.Settlement;
        private FormalUiInteractionLayer interactionLayer => presentation?.Interaction;
        private FormalStartupPresentation startupPresentation => presentation?.Startup;
        private DeveloperConsolePanel developerConsole => presentation?.DeveloperConsole;
        private TarkovInventoryPanel inventoryPanel => presentation?.Inventory;
        private FireBattleState fireBattle;
        private ArtifactBattleState artifactBattle;
        private TrainingRangeSession trainingRangeSession;
        private bool trainingRangeActive;
        private int trainingRangeArtifactUsesRemaining;
        private string armedInventoryItemId;
        private string armedRogueTacticalItemId;
        private bool battlefieldContextMenuOpen;
        private RogueliteUiPreferences uiPreferences = new RogueliteUiPreferences();
        private bool lastSettingsSaveSucceeded = true;
        private readonly UiVisualEventStream uiVisualEvents = new UiVisualEventStream();
        private readonly UiPresentationVersions uiPresentationVersions = new UiPresentationVersions();

        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            CombatDebugTuning.TemporaryEnemyAssistEnabled = Application.isEditor || Debug.isDebugBuild;
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
            developerPreparation = new MissionPreparation().Configure("relay_test", "完成学院演练并处置任务装置", "盾术陪练生、火矢陪练生、侧锋陪练生、承压检验偶、缚环寻迹兽");
            presentation = CombatPresentationComposition.Attach(gameObject, this);
            BuildCombatFromSceneStageTwo();
            formalAssets.ApplySceneSprites(transform);
            formalAssets.LoadRuntime();
        }

        private void OnDisable()
        {
            if (Application.isPlaying)
                CombatDebugTuning.TemporaryEnemyAssistEnabled = false;
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

        public void EnsureEditorVisuals() => formalAssets.EnsureEditorVisuals(transform);
        public void EnsureEditorMapVisuals() => formalAssets.EnsureEditorMapVisuals(transform);
        public void EnsureEditorUiVisuals() => formalAssets.EnsureEditorUiVisuals(transform);

        private void BuildCombatFromSceneStageTwo()
        {
            selection.EndKeyboardTargeting();
            CombatSceneSessionBuild build = sceneSessionBuilder.Build(mapRun, rogueliteRun,
                FindObjectsByType<CombatSceneMarker>(), developerPreparation);
            if (build == null) return;
            state = build.State;
            developerPreparation = build.Preparation;
            currentLevel = build.Level;
            developerFlow = new CombatFlowController();
            developerFlow.Configure(developerPreparation, state);
            battlefieldViewport = battlefield.CreateViewport(state.Map.Width, state.Map.Height);
            battlefieldViewport.Focus(state.GetUnit("hero").Position);
            outcomeSettlement.Reset();
            ResetEnemyTurnSequence();
        }
        public void OpenDeveloperBriefing() { developerFlow.OpenBriefing(); MarkPresentation(UiPresentationArea.Flow); }
        public void StartDeveloperCombat()
        {
            ApplyCombatSessionActivation(combatSession.Begin(developerFlow, enemyTurn, outcomeSettlement));
            MarkPresentation(UiPresentationArea.Flow);
        }
        public void TacticalRestartDeveloperCombat()
        {
            if (trainingRangeActive) { PrepareTrainingRangeCurrent(); return; }
            ApplyCombatSessionActivation(combatSession.Restart(developerFlow, enemyTurn, outcomeSettlement));
        }
        private void ApplyCombatSessionActivation(CombatSessionActivation activation)
        {
            state = activation.State;
            fireBattle = activation.FireBattle;
            FocusHeroInBattlefield();
            visualFeedback?.CancelEnemyAction();
            visualFeedback?.ResetBattleFeedback();
            PublishCombatEffects(activation.InitialTurnEffects);
            RefreshSceneHud();
            MarkPresentation(UiPresentationArea.Combat);
        }
        public void ReturnToDeveloperMenu()
        {
            if (trainingRangeActive)
            {
                trainingRangeActive = false; rogueliteFlow.Reset();
                developerPreparation = new MissionPreparation().Configure("relay_test", "完成学院演练并处置任务装置", "盾术陪练生、火矢陪练生、侧锋陪练生、承压检验偶、缚环寻迹兽");
                BuildCombatFromSceneStageTwo(); selection.Reset();
                RefreshSceneHud(); MarkPresentation(UiPresentationArea.Flow); MarkPresentation(UiPresentationArea.Combat); return;
            }
            developerFlow.ReturnToDeveloperMenu(); state = developerFlow.State; selection.Reset(); rogueliteFlow.Reset(); RefreshSceneHud(); MarkPresentation(UiPresentationArea.Flow);
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
                    ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Rejected, "这份记录暂时读不开。它没有被改动；请稍后重试，或删除后开始新游戏。"));
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
                    ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Rejected, "这份记录暂时读不开。它没有被改动；请稍后重试，或删除后开始新游戏。"));
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
            RogueliteMapStartResult start = mapSaves.TryStart(continueSave, starterId,
                UnityEngine.Random.Range(1, int.MaxValue));
            if (!start.Success)
            {
                ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Rejected, start.FailureMessage));
                MarkPresentation(UiPresentationArea.Flow);
                return false;
            }
            rogueliteFlow.BeginMapRun(start.Run);
            MarkPresentation(UiPresentationArea.Flow); MarkPresentation(UiPresentationArea.MapStructure);
            return true;
        }
        public void RequestStartMapRoguelite(bool continueSave)
            => RequestStartMapRoguelite(continueSave, FireRogueliteStarterCatalog.Universal);
        public void RequestStartMapRoguelite(bool continueSave, string starterId)
        {
            if (!continueSave && HasMapRogueliteSave)
            {
                RequestConfirmation(new UiConfirmationRequest(UiConfirmationKind.ReplaceExistingRun, "开始新游戏？",
                    MapSavePresentation.ReplacementMessage, "覆盖存档并开始"), () =>
                    {
                        if (!PrepareMapSlotForReplacement())
                        {
                            ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Rejected, "无法覆盖旧存档。旧存档仍然保留；请稍后重试。"));
                            return;
                        }
                        if (!TryStartMapRoguelite(false, starterId)) return;
                        ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Saved, "新的旅程已经记下。现在选一个相邻地点吧。"));
                    });
                return;
            }
            if (!TryStartMapRoguelite(continueSave, starterId)) return;
            ShowUiFeedback(new UiActionFeedback(continueSave ? UiFeedbackKind.Information : UiFeedbackKind.Saved,
                continueSave ? "欢迎回来，继续从当前位置出发。" : "新的旅程已经记下。现在选一个相邻地点吧。"));
        }
        public void DeleteMapRogueliteSave() => mapSaves.Delete();
        public bool HasMapRogueliteSave => mapSaves.HasSave;
        public MapSaveUiPresentation MapSavePresentation => mapSaves.Presentation;
        public string SettingsSaveDetail => lastSettingsSaveSucceeded ? "设置已保存" : "设置已临时生效，但保存失败";

        private bool PrepareMapSlotForReplacement()
            => mapSaves.PrepareSlotForReplacement();
        public void SelectMapNode(string nodeId)
        {
            RogueliteMapInteractionResult result = mapInteractions.SelectNode(mapRun, nodeId);
            MarkPresentation(UiPresentationArea.MapStructure);
            PublishUiVisual(new UiVisualEvent(result.SafeRevisit ? UiVisualEventKind.SafeRevisit : UiVisualEventKind.MapLocationChanged,
                result.SubjectId, message: result.PreviousNodeId + "→" + result.SubjectId));
            if (!result.StartsCombat)
            {
                SaveMapRun(); return;
            }
            SaveMapRun(); BuildCombatFromSceneStageTwo(); developerFlow.OpenBriefing();
            PublishUiVisual(new UiVisualEvent(UiVisualEventKind.BriefingOpened, nodeId));
        }

        public void StartMapNodeCombat(string nodeId)
        {
            if (mapRun == null || string.IsNullOrEmpty(nodeId) || !RogueliteMapCatalog.Nodes.Any(value => value.Id == nodeId && value.IsCombat))
            {
                ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Rejected, "现在还不能进入这场战斗。请回到地图重新选择。"));
                return;
            }

            SelectMapNode(nodeId);
            if (developerFlow != null && developerFlow.Phase == CombatFlowPhase.Briefing)
                StartDeveloperCombat();
        }

        public void ChooseMapNodeContent(string choiceId)
        {
            RogueliteMapInteractionResult result = mapInteractions.ChooseContent(mapRun, choiceId);
            MarkPresentation(UiPresentationArea.MapStructure);
            PublishResourceChanges(result.ResourcesBefore, result.ResourcesAfter);
            if (result.StartsCombat) { SaveMapRun(); BuildCombatFromSceneStageTwo(); developerFlow.OpenBriefing(); PublishUiVisual(new UiVisualEvent(UiVisualEventKind.BriefingOpened, choiceId)); return; }
            SaveMapRun();
        }
        public void ClaimMapReward(string rewardId)
        {
            RogueliteMapInteractionResult result = mapInteractions.ClaimReward(mapRun, rewardId);
            MarkPresentation(UiPresentationArea.Settlement);
            MarkPresentation(UiPresentationArea.MapStructure);
            PublishResourceChanges(result.ResourcesBefore, result.ResourcesAfter);
            PublishUiVisual(new UiVisualEvent(UiVisualEventKind.RewardClaimed, rewardId));
            SaveMapRun(); settlementPresentation?.RefreshNow();
        }
        public void ClaimMapFireSpell(string spellId)
        {
            mapInteractions.ClaimFireSpell(mapRun, spellId);
            MarkPresentation(UiPresentationArea.Settlement); MarkPresentation(UiPresentationArea.MapStructure);
            PublishUiVisual(new UiVisualEvent(UiVisualEventKind.RewardClaimed, spellId));
            SaveMapRun(); settlementPresentation?.RefreshNow();
        }
        public void EquipMapFireSpell(string spellId, int slot) { mapInteractions.EquipFireSpell(mapRun, spellId, slot); SaveMapRun(); MarkPresentation(UiPresentationArea.MapStructure); MarkPresentation(UiPresentationArea.Combat); }
        public void EquipNextMapFireSpell(int slot)
        {
            if (!mapInteractions.TryEquipNextFireSpell(mapRun, slot)) return;
            SaveMapRun(); MarkPresentation(UiPresentationArea.MapStructure); MarkPresentation(UiPresentationArea.Combat);
        }
        public void EquipMapReward(string rewardId) { mapInteractions.EquipReward(mapRun, rewardId); SaveMapRun(); MarkPresentation(UiPresentationArea.MapStructure); }
        public void CalibrateMapAether()
        {
            RogueliteMapInteractionResult result = mapInteractions.CalibrateAether(mapRun);
            MarkPresentation(UiPresentationArea.MapStructure);
            PublishResourceChanges(result.ResourcesBefore, result.ResourcesAfter);
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
            bool saved = mapSaves.Save(mapRun);
            if (!saved)
            {
                ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Rejected, RogueliteMapSaveCoordinator.ActiveRunSaveFailure));
                MarkPresentation(UiPresentationArea.Flow);
            }
            return saved;
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
        public bool CanQuickMoveTo(GridPosition position) => state != null &&
            string.IsNullOrEmpty(battlefield.InvalidReasonForCell(state, "移动", position));
        public bool ShouldDeferPrimaryClickForQuickMove(GridPosition position) =>
            selection.Action != "移动" && CanQuickMoveTo(position);
        public void SubmitBattlefieldQuickMove(GridPosition position)
        {
            if (!CanQuickMoveTo(position))
            {
                string reason = state == null ? "战场尚未准备好。" : battlefield.InvalidReasonForCell(state, "移动", position);
                ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Rejected,
                    string.IsNullOrWhiteSpace(reason) ? "现在不能移动到那里。" : reason));
                return;
            }
            SelectHudAction("移动");
            HandleCellClick(position);
        }
        public IReadOnlyList<BattlefieldContextAction> ContextActionsAt(GridPosition position)
        {
            var actions = new List<BattlefieldContextAction>();
            if (state == null || !state.Map.IsInside(position) || state.IsVictory || state.IsDefeat) return actions;
            UnitState hero = state.GetUnit("hero");
            if (hero == null || !hero.IsAlive || state.ActiveUnitId != hero.Id) return actions;
            UnitState clicked = state.Units.Values.FirstOrDefault(unit => unit.IsAlive && unit.Position == position);

            AddContextActionIfLegal(actions, position, "移动", "move", "移动到这里", "1 行动点");
            AddContextActionIfLegal(actions, position, "攻击", "attack", "攻击" +
                (clicked != null && !clicked.IsHero ? "「" + clicked.DisplayName + "」" : string.Empty), "1 行动点");
            AddContextActionIfLegal(actions, position, "搜刮", "loot", "搜刮这里", "1 行动点");
            AddContextActionIfLegal(actions, position, "互动", "interact", "与这里互动", "1 行动点");
            for (int slot = 0; slot < RogueRuntimeConstants.SpellSlotCount; slot++)
            {
                if (!TryBuildContextSpellAction(slot, position, clicked, out BattlefieldContextAction action)) continue;
                actions.Add(action);
            }
            return actions;
        }
        public void SubmitBattlefieldContextAction(GridPosition position, string actionId)
        {
            battlefieldContextMenuOpen = false;
            if (string.IsNullOrWhiteSpace(actionId)) return;
            if (actionId == "move") { SubmitBattlefieldQuickMove(position); return; }
            if (actionId == "attack") { SelectHudAction("攻击"); HandleCellClick(position); return; }
            if (actionId == "loot") { SelectHudAction("搜刮"); HandleCellClick(position); return; }
            if (actionId == "interact") { SelectHudAction("互动"); HandleCellClick(position); return; }
            if (actionId.StartsWith("spell:", StringComparison.Ordinal) &&
                int.TryParse(actionId.Substring(6), out int slot) &&
                string.IsNullOrEmpty(SpellShortcutFailureReason(slot)))
            {
                SelectHudAction("技能" + (slot + 1));
                HandleCellClick(position);
            }
        }
        public void NotifyBattlefieldContextUnavailable(GridPosition position)
        {
            ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Information,
                "这里暂时没有可执行行动；请检查行动点、魔力、距离与当前回合。"));
        }
        public void SetBattlefieldContextMenuOpen(bool open) => battlefieldContextMenuOpen = open;

        private void AddContextActionIfLegal(List<BattlefieldContextAction> actions, GridPosition position,
            string action, string id, string label, string detail)
        {
            if (string.IsNullOrEmpty(battlefield.InvalidReasonForCell(state, action, position)))
                actions.Add(new BattlefieldContextAction(id, label, detail));
        }

        private bool TryBuildContextSpellAction(int slot, GridPosition position, UnitState clicked,
            out BattlefieldContextAction action)
        {
            action = null;
            if (!string.IsNullOrEmpty(SpellShortcutFailureReason(slot))) return false;
            UnitState hero = state.GetUnit("hero");
            string name;
            string cost;

            if (state.Ruleset == CombatRuleset.Roguelite && state.RogueSpells != null)
            {
                SpellDefinition rogue = state.RogueSpells.DefinitionAtSlot(slot);
                if (rogue == null) return false;
                if (FireSpellCatalog.All.Any(value => value.Id == rogue.DefinitionId))
                {
                    FireSpellDefinition fire = FireSpellCatalog.Get(rogue.DefinitionId);
                    if (fireBattle == null || fireBattle.Combat != state) fireBattle = state.RogueSpells.FireBattle;
                    if (!BuildFireSpellPreviewAt(fire, position).CanCommit) return false;
                }
                else if (rogue.Targeting == "self")
                {
                    if (clicked == null || !clicked.IsHero) return false;
                }
                else
                {
                    if (clicked == null || clicked.IsHero || Distance(hero.Position, position) > rogue.Range) return false;
                    if (rogue.LineOfSightRule != "not_required" && !state.Map.HasLineOfSight(hero.Position, position)) return false;
                }
                name = rogue.DisplayName;
                cost = rogue.ActionPointCost + " 行动点 / " + rogue.ManaCost + " 个人魔力";
            }
            else
            {
                ArtifactDefinition artifact = slot == 0 ? (CurrentArmedArtifact ?? CurrentTrainingRangeArtifact) : null;
                if (artifact != null)
                {
                    EnsureArtifactBattle();
                    if (!BuildArtifactTarget(artifact, position, out ArtifactTarget target) ||
                        !ArtifactEngine.Preview(artifactBattle, "hero", artifact, target, CurrentArmedUses).CanCommit) return false;
                    name = artifact.DisplayName;
                    cost = artifact.ActionPointCost + " 行动点";
                }
                else
                {
                    FireSpellDefinition fire = FireSpellInSlot(slot);
                    if (fire != null)
                    {
                        if (fireBattle == null || fireBattle.Combat != state) fireBattle = new FireBattleState(state);
                        if (!BuildFireSpellPreviewAt(fire, position).CanCommit) return false;
                        name = fire.DisplayName;
                        cost = fire.ActionPointCost + " 行动点 / " + fire.ManaCost + " 以太";
                    }
                    else
                    {
                        if (slot > 1 || !string.IsNullOrEmpty(battlefield.InvalidReasonForCell(state,
                                "技能" + (slot + 1), position))) return false;
                        SkillDefinition skill = slot == 0 ? hero.SkillOne : hero.SkillTwo;
                        name = skill.DisplayName;
                        cost = "1 行动点 / " + skill.ManaCost + " 以太";
                    }
                }
            }

            action = new BattlefieldContextAction("spell:" + slot,
                "[" + (slot + 1) + "] 施放「" + name + "」", cost);
            return true;
        }
        public BattlefieldCellPresentation PresentBattlefieldCell(GridPosition position)
            => BattlefieldCells.Build(state, currentLevel, fireBattle, selection, trainingRangeActive,
                visualFeedback, position, FireSpellInSlot, BuildFireSpellPreviewAt,
                TargetDamageForecast, EnemyIntent);
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
        public string SelectedAction => selection.Action;
        public string SelectedTargetId => selection.TargetId;
        public bool IsKeyboardTargeting => selection.IsKeyboardTargeting;
        public GridPosition KeyboardTargetPosition => selection.KeyboardPosition;
        public CombatActionPreview CurrentActionPreview => BuildActionPreview(selection.Action);
        public CombatActionPreview ActionPreview(string action) => BuildActionPreview(action);
        public CombatOutcomePresentation CurrentOutcomePresentation => state == null ? null : CombatInformationPresenter.BuildOutcome(state, mapRun != null);
        public string CurrentPhaseText => CombatInformationPresenter.PhaseText(CurrentFlowPhase, state);
        public EnemyIntentPresentation EnemyIntent(UnitState enemy) => enemy == null || state == null ? null : enemyPlans.GetPublicIntent(state, enemy, state.GetUnit("hero"));
        public FireSpellDefinition FireSpellInSlot(int slot)
        {
            if (trainingRangeActive) return slot == 0 ? trainingRangeSession?.CurrentFireSpell : null;
            if (slot == 0 && state?.ItemInventory.Get(armedInventoryItemId) is ItemInstance armed) return ItemAbilityCatalog.For(armed.DefinitionId);
            if (state?.Ruleset == CombatRuleset.Roguelite && state.RogueSpells != null)
            {
                OCC.Combat.Roguelite.SpellDefinition rogue = state.RogueSpells.DefinitionAtSlot(slot);
                return rogue != null && FireSpellCatalog.All.Any(value => value.Id == rogue.DefinitionId) ? FireSpellCatalog.Get(rogue.DefinitionId) : null;
            }
            if (mapRun == null || slot < 0 || slot >= mapRun.EquippedFireSpellIds.Count) return null;
            string id = mapRun.EquippedFireSpellIds[slot];
            return string.IsNullOrEmpty(id) ? null : FireSpellCatalog.Get(id);
        }
        private static int RogueSkillSlot(string action)
        {
            return action != null && action.StartsWith("技能", StringComparison.Ordinal) && int.TryParse(action.Substring(2), out int oneBased) &&
                oneBased >= 1 && oneBased <= OCC.Combat.Roguelite.RogueRuntimeConstants.SpellSlotCount ? oneBased - 1 : -1;
        }
        private CombatActionPreview BuildActionPreview(string action)
        {
            ArtifactDefinition armedArtifact = CurrentArmedArtifact ?? CurrentTrainingRangeArtifact;
            if (action == "技能1" && armedArtifact != null && state != null)
            {
                EnsureArtifactBattle(); int validArtifacts = 0;
                for (int y = 0; y < state.Map.Height; y++) for (int x = 0; x < state.Map.Width; x++)
                    if (BuildArtifactTarget(armedArtifact, new GridPosition(x, y), out ArtifactTarget candidate) &&
                        ArtifactEngine.Preview(artifactBattle, "hero", armedArtifact, candidate,
                            CurrentArmedUses).CanCommit) validArtifacts++;
                return new CombatActionPreview(action, armedArtifact.TargetSummary, armedArtifact.PublicCost,
                    armedArtifact.EffectSummary + "；风险：" + armedArtifact.RiskSummary, validArtifacts,
                    validArtifacts == 0 ? "现在没有可以选择的目标" : string.Empty);
            }
            int slot = RogueSkillSlot(action);
            if (slot >= 0 && state?.Ruleset == CombatRuleset.Roguelite && state.RogueSpells != null)
            {
                OCC.Combat.Roguelite.SpellDefinition rogue = state.RogueSpells.DefinitionAtSlot(slot);
                if (rogue == null) return new CombatActionPreview(action, "空术式槽", "0 行动", "未装备术式", 0, "术式槽为空");
                int validTargets = rogue.Targeting == "self" ? 1 : state.Units.Values.Count(unit => unit.IsAlive && unit.IsHero != state.GetUnit("hero").IsHero);
                return new CombatActionPreview(action, RogueliteSettlementPresentation.RogueSpellTargetSummary(rogue),
                    rogue.ActionPointCost + " 行动 + " + rogue.ManaCost + " 个人魔力",
                    RogueliteSettlementPresentation.RogueSpellPlayerSummary(rogue), validTargets,
                    state.RogueSpells.IsReady(rogue.DefinitionId) ? string.Empty : "术式冷却中");
            }
            FireSpellDefinition spell = slot < 0 ? null : FireSpellInSlot(slot);
            if (spell == null || state == null) return availability.Preview(state, action, selection.TargetId);
            if (fireBattle == null || fireBattle.Combat != state) fireBattle = new FireBattleState(state);
            int valid = 0;
            for (int y = 0; y < state.Map.Height; y++) for (int x = 0; x < state.Map.Width; x++) if (IsFireSpellCellValid(spell, new GridPosition(x, y))) valid++;
            string failure = string.Empty;
            UnitState selected = string.IsNullOrEmpty(selection.TargetId) ? null : state.GetUnit(selection.TargetId);
            if (selected != null)
            {
                FireSpellPreview exact = FireSpellEngine.Preview(fireBattle, "hero", spell, FireSpellTarget.Unit(selected.Id, FacingToward(state.GetUnit("hero").Position, selected.Position)));
                failure = string.Join("；", exact.Failures);
            }
            else if (valid == 0) failure = "现在没有可以选择的目标";
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
            selection.SetTarget(state, unitId);
            MarkPresentation(UiPresentationArea.Combat);
        }
        public bool BeginKeyboardTargeting()
        {
            if (!selection.BeginKeyboardTargeting(state)) return false;
            MarkPresentation(UiPresentationArea.Combat);
            return true;
        }
        public void MoveKeyboardTarget(int deltaX, int deltaY)
        {
            if (!selection.MoveKeyboardTarget(state, deltaX, deltaY)) return;
            MarkPresentation(UiPresentationArea.Combat);
        }
        public void CommitKeyboardTarget()
        {
            if (state == null || !selection.TryCommitKeyboardTarget(out GridPosition position)) return;
            HandleCellClick(position);
            MarkPresentation(UiPresentationArea.Combat);
        }
        public void CancelKeyboardTargeting()
        {
            if (!selection.CancelKeyboardTargeting()) return;
            MarkPresentation(UiPresentationArea.Combat);
            ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Information, "已取消战场目标选择"));
        }
        public void CancelCombatSelectionOrRequestLeave()
        {
            CombatCancelResolution resolution = CombatSelectionNavigation.ResolveCancel(selection.Action, selection.TargetId,
                !string.IsNullOrEmpty(armedInventoryItemId) || !string.IsNullOrEmpty(armedRogueTacticalItemId));
            if (resolution == CombatCancelResolution.ClearTarget)
            {
                selection.ClearTarget();
                MarkPresentation(UiPresentationArea.Combat);
                ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Information, "已取消目标查看"));
                return;
            }
            if (resolution == CombatCancelResolution.ResetAction)
            {
                selection.Reset();
                armedInventoryItemId = null;
                armedRogueTacticalItemId = null;
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
        private RogueTacticalItemInstance CurrentArmedRogueTactical => state?.RogueEquipment?.TacticalItem(armedRogueTacticalItemId);
        private int CurrentArmedUses => CurrentArmedInventoryItem?.RemainingUses ?? CurrentArmedRogueTactical?.ChargesCurrent ?? trainingRangeArtifactUsesRemaining;
        public ArtifactDefinition CurrentArmedArtifact
        {
            get
            {
                if (CurrentArmedInventoryItem != null && ItemCatalog.Get(CurrentArmedInventoryItem.DefinitionId).Category == ItemCategory.Artifact)
                    return ArtifactCatalog.Get(CurrentArmedInventoryItem.DefinitionId);
                return CurrentArmedRogueTactical != null && ArtifactCatalog.All.Any(value => value.Id == CurrentArmedRogueTactical.DefinitionId)
                    ? ArtifactCatalog.Get(CurrentArmedRogueTactical.DefinitionId) : null;
            }
        }
        public bool IsCombatOutcomeVisible => developerFlow != null && (developerFlow.Phase == CombatFlowPhase.Victory || developerFlow.Phase == CombatFlowPhase.Defeat);
        public bool IsInteractionModalOpen => battlefieldContextMenuOpen ||
            (interactionLayer != null && interactionLayer.IsConfirmationOpen) ||
            (inventoryPanel != null && inventoryPanel.IsOpen);
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
            state = prepared.Combat; fireBattle = trainingRangeSession.CurrentFireBattle;
            artifactBattle = (prepared as ArtifactTrainingRangeCase)?.Battle ?? new ArtifactBattleState(state);
            trainingRangeArtifactUsesRemaining = trainingRangeSession.CurrentArtifact?.MaximumUses ?? 0;
            developerPreparation = new MissionPreparation().Configure("training_range", "能力验证与确定性回归", "标准靶兵、友军、掩体、设备、水面与核心样本");
            developerFlow = new CombatFlowController(); developerFlow.Configure(developerPreparation, state); developerFlow.OpenBriefing(); developerFlow.BeginCombat();
            selection.Reset("技能1"); selection.SetKnownTarget(prepared.RecommendedUnitId); outcomeSettlement.Reset();
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
            RequestConfirmation(new UiConfirmationRequest(UiConfirmationKind.TacticalRestart, "重新开始这场战斗？",
                "本场战斗中的移动、伤害和道具消耗都会恢复到开战前。", "重新开始"), TacticalRestartDeveloperCombat);
        }
        public void RequestLeaveCombat()
        {
            if (!IsDeveloperCombatActive) return;
            RequestConfirmation(new UiConfirmationRequest(UiConfirmationKind.LeaveCombat, "离开这场战斗？",
                "离开后，这场战斗中的收获和损失都不会留下。你会回到地图。", "离开并返回地图"), () =>
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

        private void PublishResourceChanges(RogueliteMapResources before, RogueliteMapResources after)
        {
            if (after.UsesRogue11)
            {
                PublishResourceChange("金币", before.Gold, after.Gold); PublishResourceChange("学院贡献", before.StageContribution, after.StageContribution);
                PublishResourceChange("学期进度", before.StageTime, after.StageTime); return;
            }
            PublishResourceChange("零件", before.Parts, after.Parts);
            PublishResourceChange("以太", before.Aether, after.Aether);
            PublishResourceChange("补给", before.Supplies, after.Supplies);
            PublishResourceChange("侦测", before.Scouting, after.Scouting);
            PublishResourceChange("权限卡", before.AccessCards, after.AccessCards);
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
                ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Rejected, "设置已临时生效，但保存失败。下次启动会恢复原设置。"));
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
            selection.SelectAction(action);
            MarkPresentation(UiPresentationArea.Combat);
            PublishUiVisual(new UiVisualEvent(UiVisualEventKind.CombatActionSelected, action));
            PublishUiVisual(new UiVisualEvent(UiVisualEventKind.CombatRangeRevealed, action, message: GetRangeDescription()));
        }
        public bool TrySelectSpellShortcut(int slot)
        {
            string failure = SpellShortcutFailureReason(slot);
            if (!string.IsNullOrEmpty(failure))
            {
                ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Rejected, failure));
                PublishUiVisual(new UiVisualEvent(UiVisualEventKind.CombatCommandRejected,
                    "技能" + (slot + 1), message: failure));
                return false;
            }

            string action = "技能" + (slot + 1);
            SelectHudAction(action);
            ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Information,
                "已进入「" + SpellShortcutDisplayName(slot) + "」模式；左键选择目标，Esc 取消。"));
            return true;
        }

        private string SpellShortcutFailureReason(int slot)
        {
            if (slot < 0 || slot >= RogueRuntimeConstants.SpellSlotCount) return "没有这个术式槽。";
            if (state == null || state.IsVictory || state.IsDefeat) return "战斗尚未处于可行动状态。";
            UnitState hero = state.GetUnit("hero");
            if (hero == null || !hero.IsAlive) return "现在无法使用术式。";
            if (state.ActiveUnitId != hero.Id) return "还没轮到你；请等敌方行动结束。";

            if (state.Ruleset == CombatRuleset.Roguelite && state.RogueSpells != null)
            {
                SpellDefinition rogue = state.RogueSpells.DefinitionAtSlot(slot);
                if (rogue == null) return "术式槽 " + (slot + 1) + " 为空；请在战斗外重新编组。";
                int cooldown = state.RogueSpells.CooldownRemaining(rogue.DefinitionId);
                if (cooldown > 0) return rogue.DisplayName + "还需等待 " + cooldown + " 个自身回合。";
                if (hero.ActionPoints < rogue.ActionPointCost)
                    return "行动点不足：" + rogue.DisplayName + "需要 " + rogue.ActionPointCost + " 点。";
                if (hero.Mana < rogue.ManaCost)
                    return "个人魔力不足：" + rogue.DisplayName + "需要 " + rogue.ManaCost + " 点。";
                return string.Empty;
            }

            ArtifactDefinition artifact = slot == 0 ? (CurrentArmedArtifact ?? CurrentTrainingRangeArtifact) : null;
            if (artifact != null)
            {
                if (CurrentArmedUses <= 0) return artifact.DisplayName + "已经用完；请换一个仍有次数的道具。";
                if (hero.ActionPoints < artifact.ActionPointCost)
                    return "行动点不足：" + artifact.DisplayName + "需要 " + artifact.ActionPointCost + " 点。";
                if (hero.Mana < artifact.ManaCost)
                    return "以太不足：" + artifact.DisplayName + "需要 " + artifact.ManaCost + " 点。";
                return string.Empty;
            }

            FireSpellDefinition fire = FireSpellInSlot(slot);
            if (fire != null)
            {
                if (fireBattle != null && fireBattle.Cooldown(hero.Id, fire.Id) > 0)
                    return fire.DisplayName + "还在冷却中。";
                if (hero.ActionPoints < fire.ActionPointCost)
                    return "行动点不足：" + fire.DisplayName + "需要 " + fire.ActionPointCost + " 点。";
                if (hero.Mana < fire.ManaCost)
                    return "以太不足：" + fire.DisplayName + "需要 " + fire.ManaCost + " 点。";
                return string.Empty;
            }

            SkillDefinition skill = slot == 0 ? hero.SkillOne : slot == 1 ? hero.SkillTwo : null;
            if (skill == null) return "术式槽 " + (slot + 1) + " 为空；请先装备术式。";
            if (hero.Cooldown(skill) > 0) return skill.DisplayName + "还需冷却 " + hero.Cooldown(skill) + " 回合。";
            if (hero.ActionPoints < CombatResolver.BasicActionPointCost)
                return "行动点不足：" + skill.DisplayName + "需要 1 点。";
            if (hero.Mana < skill.ManaCost)
                return "以太不足：" + skill.DisplayName + "需要 " + skill.ManaCost + " 点。";
            return string.Empty;
        }

        private string SpellShortcutDisplayName(int slot)
        {
            if (state?.Ruleset == CombatRuleset.Roguelite && state.RogueSpells != null)
                return state.RogueSpells.DefinitionAtSlot(slot)?.DisplayName ?? "空术式槽";
            ArtifactDefinition artifact = slot == 0 ? (CurrentArmedArtifact ?? CurrentTrainingRangeArtifact) : null;
            if (artifact != null) return artifact.DisplayName;
            FireSpellDefinition fire = FireSpellInSlot(slot);
            if (fire != null) return fire.DisplayName;
            UnitState hero = state?.GetUnit("hero");
            return (slot == 0 ? hero?.SkillOne : slot == 1 ? hero?.SkillTwo : null)?.DisplayName ?? "术式槽 " + (slot + 1);
        }
        public void SearchCurrentLoot() { if (state != null) { TryCommand(CombatCommand.SearchLoot("hero")); PersistCombatInventory(); } }
        public void TakeCurrentLoot(string instanceId) { if (state != null) { TryCommand(CombatCommand.TakeLoot("hero", instanceId)); PersistCombatInventory(); } }
        public void EquipInventoryQuickbar(string instanceId, int slot) { if (state != null) { TryCommand(CombatCommand.EquipInventoryQuickbar("hero", instanceId, slot)); PersistCombatInventory(); } }
        public void ActivateInventoryQuickbar(int slot)
        {
            if (state?.Ruleset == CombatRuleset.Roguelite && state.RogueEquipment != null)
            {
                if (slot < 0 || slot >= RogueRuntimeConstants.ItemQuickbarSize) return;
                string instanceId = state.RogueEquipment.ItemQuickbarInstanceIds[slot]; RogueTacticalItemInstance tactical = state.RogueEquipment.TacticalItem(instanceId);
                TacticalItemDefinition definition = state.RogueEquipment.TacticalDefinitionFor(instanceId);
                if (tactical == null || definition == null) return;
                if (tactical.ChargesCurrent <= 0) { ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Rejected, "这个道具已经用完。请换一个有剩余次数的道具。")); return; }
                if (!ArtifactCatalog.All.Any(value => value.Id == tactical.DefinitionId))
                { ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Rejected, "这个道具目前不能在战斗中使用。请换一个道具。")); return; }
                armedInventoryItemId = null; armedRogueTacticalItemId = tactical.InstanceId; selection.SelectAction("技能1"); EnsureArtifactBattle();
                state.AddLog("已拿出" + definition.DisplayName + "；请选择一个亮起的目标。"); MarkPresentation(UiPresentationArea.Combat); return;
            }
            if (state == null || slot < 0 || slot >= state.ItemQuickbar.Length) return; ItemInstance item = state.ItemInventory.Get(state.ItemQuickbar[slot]); if (item == null) return;
            if (ItemCatalog.Get(item.DefinitionId).Category == ItemCategory.Artifact)
            {
                if (item.DefinitionId == "G-T13")
                {
                    armedInventoryItemId = null;
                    state.AddLog("定锚支架已在快捷栏待机；受到推拉时自动抵消并消耗 1 次。");
                    MarkPresentation(UiPresentationArea.Combat); return;
                }
                armedInventoryItemId = item.InstanceId; selection.SelectAction("技能1");
                EnsureArtifactBattle(); state.AddLog("已拿出" + ItemCatalog.Get(item.DefinitionId).DisplayName + "；请选择一个亮起的目标。");
                MarkPresentation(UiPresentationArea.Combat); return;
            }
            FireSpellDefinition ability = ItemAbilityCatalog.For(item.DefinitionId);
            if (ability == null) { TryCommand(CombatCommand.UseQuickbar("hero", slot)); PersistCombatInventory(); return; }
            armedInventoryItemId = item.InstanceId; selection.SelectAction("技能1"); state.AddLog("已拿出" + ItemCatalog.Get(item.DefinitionId).DisplayName + "；请选择一个亮起的格子。"); MarkPresentation(UiPresentationArea.Combat);
        }
        public void NotifyInventoryChanged() { PersistCombatInventory(); MarkPresentation(UiPresentationArea.Combat); }
        public bool MoveRogueBackpackItem(string instanceId, int x, int y, bool rotated)
            => MutateRogueInventory(runtime => runtime.MoveBackpack(instanceId, x, y, rotated), "已移动", "该位置无法放置");
        public bool RotateRogueBackpackItem(string instanceId)
            => MutateRogueInventory(runtime => runtime.RotateBackpack(instanceId), "已旋转", "当前位置无法旋转");
        public bool EquipRogueEquipment(string instanceId, OCC.Combat.Roguelite.EquipmentSlot slot)
        {
            if (developerFlow != null && developerFlow.Phase == CombatFlowPhase.Active)
            { ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Rejected, "战斗中不能更换装备。请先离开战斗。")); return false; }
            return MutateRogueInventory(runtime => runtime.Equip(instanceId, slot), "已装备", "槽位不匹配、被占用或副手被双手武器锁定");
        }
        public bool EquipOrReplaceRogueEquipment(string instanceId, OCC.Combat.Roguelite.EquipmentSlot slot)
        {
            if (developerFlow != null && developerFlow.Phase == CombatFlowPhase.Active)
            { ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Rejected, "战斗中不能更换装备。请先离开战斗。")); return false; }
            return MutateRogueInventory(runtime => runtime.EquipOrReplace(instanceId, slot), "已装备；原装备已放回背包", "槽位不匹配、背包空间不足或副手被双手武器锁定");
        }
        public bool UnequipRogueEquipment(OCC.Combat.Roguelite.EquipmentSlot slot)
        {
            if (developerFlow != null && developerFlow.Phase == CombatFlowPhase.Active)
            { ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Rejected, "战斗中不能卸下装备。请先离开战斗。")); return false; }
            return MutateRogueInventory(runtime => runtime.Unequip(slot), "已放回背包", "背包空间不足或槽位为空");
        }
        public bool UnequipRogueEquipmentTo(OCC.Combat.Roguelite.EquipmentSlot slot, int x, int y, bool rotated)
        {
            if (developerFlow != null && developerFlow.Phase == CombatFlowPhase.Active)
            { ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Rejected, "战斗中不能卸下装备。请先离开战斗。")); return false; }
            return MutateRogueInventory(runtime => runtime.UnequipToBackpack(slot, x, y, rotated), "已放入指定背包格", "该位置无法放置或槽位为空");
        }
        public bool AssignRogueQuickbar(string instanceId, int slot)
            => MutateRogueInventory(runtime => runtime.AssignQuickbar(slot, instanceId), "已关联战术栏 " + (slot + 1), "只有背包中的战术道具可以关联");
        private bool MutateRogueInventory(Func<RogueEquipmentRuntime, bool> operation, string success, string failure)
        {
            if (mapRun == null || !mapRun.UsesRogue11) return false;
            bool combatRuntime = state != null && state.Ruleset == CombatRuleset.Roguelite && state.RogueEquipment != null;
            RogueEquipmentRuntime runtime = combatRuntime ? state.RogueEquipment : RogueEquipmentRuntime.FromDto(mapRun.RogueRunState);
            if (!operation(runtime)) { ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Rejected, failure)); return false; }
            if (combatRuntime) PersistCombatInventory();
            else { runtime.WriteToDto(mapRun.RogueRunState); SaveMapRun(); }
            MarkPresentation(combatRuntime ? UiPresentationArea.Combat : UiPresentationArea.MapStructure);
            ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Success, success)); return true;
        }
        private void PersistCombatInventory() { if (mapRun == null || state == null) return; mapRun.CaptureCombatInventory(state); SaveMapRun(); }
        public void ApplyHudBuild(int build) { if (state != null) ApplyBuild(build); }
        public void EndHeroTurn() { if (state != null) TryCommand(CombatCommand.EndTurn("hero"), true); }
        private void Update()
        {
            if (!Application.isPlaying || developerFlow == null || state == null) return;
            if (!trainingRangeActive)
            {
                CombatUnitLifecycleAdvance lifecycle = combatSession.ObserveActiveUnit(state.ActiveUnitId);
                if (lifecycle.Changed && !string.IsNullOrEmpty(lifecycle.UnitId))
                {
                    fireBattle?.BeginUnitTurn(lifecycle.UnitId);
                    EnsureArtifactBattle();
                    artifactBattle.BeginUnitTurn(lifecycle.UnitId);
                }
            }
            CombatFlowPhase phaseBeforeUpdate = developerFlow.Phase;
            if (!trainingRangeActive && developerFlow.Phase == CombatFlowPhase.Active && !state.IsVictory && !state.IsDefeat &&
                !string.IsNullOrEmpty(state.ActiveUnitId) && state.ActiveUnitId != "hero") { RunEnemyTurn(); developerFlow.RefreshOutcome(); }
            else if (state.ActiveUnitId == "hero" && enemyTurn.IsRunning) ResetEnemyTurnSequence();
            developerFlow.RefreshOutcome(); HandleRogueliteOutcome();
            if (developerFlow.Phase != phaseBeforeUpdate) { MarkPresentation(UiPresentationArea.Flow); MarkPresentation(UiPresentationArea.Combat); }
        }
        private void HandleRogueliteOutcome()
        {
            CombatOutcomeSettlement settlement = outcomeSettlement.Process(developerFlow.Phase, state, mapRun, rogueliteRun);
            if (!settlement.HandledNow) return;
            visualFeedback?.PlayOutcome(settlement.Victory);
            if (settlement.Persistence == CombatOutcomePersistence.MapRun)
            {
                MarkPresentation(UiPresentationArea.Settlement);
                MarkPresentation(UiPresentationArea.MapStructure);
                SaveMapRun();
            }
            else if (settlement.Persistence == CombatOutcomePersistence.ShortRun) SaveShortRun();
            else if (settlement.Persistence == CombatOutcomePersistence.Story) saveGateway.SaveStory(rogueliteRun.Package);
            if (settlement.RefreshSettlement) settlementPresentation?.RefreshNow();
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
            int slot = RogueSkillSlot(selection.Action);
            FireSpellDefinition fireSpell = slot < 0 ? null : FireSpellInSlot(slot);
            bool artifactArmed = slot == 0 && (CurrentArmedArtifact != null || CurrentTrainingRangeArtifact != null);
            CombatTargetForecastResult result = targetForecasts.Evaluate(
                battlefield, state, fireBattle, selection.Action, enemy, fireSpell, artifactArmed);
            fireBattle = result.FireBattle;
            return result.Forecast;
        }

        private void HandleInspectionClick(GridPosition position)
        {
            selection.EndKeyboardTargeting();
            string nextTargetId = CombatInformationPresenter.EnemyInspectionTargetAt(state, position);
            if (!selection.SetKnownTarget(nextTargetId)) return;
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
            selection.EndKeyboardTargeting();
            UnitState clickedUnit = state.Units.Values.FirstOrDefault(unit => unit.IsAlive && unit.Position == p);
            UnitState enemy = clickedUnit != null && !clickedUnit.IsHero ? clickedUnit : null;
            int fireSlot = RogueSkillSlot(selection.Action);
            if (fireSlot >= 0 && state.Ruleset == CombatRuleset.Roguelite && state.RogueSpells != null)
            {
                OCC.Combat.Roguelite.SpellDefinition rogue = state.RogueSpells.DefinitionAtSlot(fireSlot);
                if (rogue == null) { state.AddLog("术式槽为空。"); MarkPresentation(UiPresentationArea.Combat); return; }
                CombatCommand command = rogue.Targeting == "self" ? CombatCommand.UseSkill("hero", fireSlot, "hero") :
                    clickedUnit != null ? CombatCommand.UseSkill("hero", fireSlot, clickedUnit.Id) : CombatCommand.UseSkillAt("hero", fireSlot, p, FacingToward(state.GetUnit("hero").Position, p));
                TryCommand(command); return;
            }
            FireSpellDefinition fireSpell = fireSlot < 0 ? null : FireSpellInSlot(fireSlot);
            if (fireSpell != null)
            {
                TryFireSpellCell(fireSpell, clickedUnit, p);
                return;
            }
            ArtifactDefinition artifact = CurrentArmedArtifact ?? CurrentTrainingRangeArtifact;
            if (selection.Action == "技能1" && artifact != null) { TryArtifactCell(artifact, clickedUnit, p); return; }
            string invalidReason = battlefield.InvalidReasonForCell(state, selection.Action, p);
            if (!string.IsNullOrEmpty(invalidReason))
            {
                PublishUiVisual(new UiVisualEvent(UiVisualEventKind.CombatCommandRejected, selection.Action, message: invalidReason));
                return;
            }
            if (selection.Action == "\u79fb\u52a8") TryCommand(CombatCommand.Move("hero", p, FacingToward(state.GetUnit("hero").Position, p)));
            else if (selection.Action == "\u653b\u51fb" && enemy != null) TryCommand(CombatCommand.Attack("hero", enemy.Id));
            else if (selection.Action == "\u6280\u80fd1") TrySkillCell(0, state.GetUnit("hero").SkillOne, clickedUnit, p);
            else if (selection.Action == "\u6280\u80fd2") TrySkillCell(1, state.GetUnit("hero").SkillTwo, clickedUnit, p);
            else if (selection.Action == "\u641c\u522e") TryCommand(CombatCommand.Loot("hero"));
            else if (selection.Action == "\u4e92\u52a8") TryCommand(CombatCommand.Interact("hero", p));
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
            int uses = CurrentArmedUses;
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
            else if (CurrentArmedRogueTactical != null)
            {
                RogueTacticalItemInstance tactical = CurrentArmedRogueTactical;
                execution = ArtifactEngine.Execute(artifactBattle, "hero", artifact, target, uses);
                tactical.Consume(); if (tactical.ChargesCurrent <= 0) armedRogueTacticalItemId = null;
                PersistCombatInventory();
            }
            else { execution = ArtifactEngine.Execute(artifactBattle, "hero", artifact, target, uses); trainingRangeArtifactUsesRemaining--; }
            state.AddLog(artifact.DisplayName + "已经生效。");
            selection.ClearTarget(); MarkPresentation(UiPresentationArea.Combat);
            PublishUiVisual(new UiVisualEvent(UiVisualEventKind.CombatCommandSubmitted, artifact.Id));
            visualFeedback?.NotifyArtifact(artifact, source, preview.Cells, execution);
            developerFlow?.RefreshOutcome();
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
            selection.SetKnownTarget(clickedUnit?.Id);
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
            state.AddLog(spell.DisplayName + "已经生效。");
            selection.ClearTarget(); MarkPresentation(UiPresentationArea.Combat);
            PublishUiVisual(new UiVisualEvent(UiVisualEventKind.CombatCommandSubmitted, spell.Id));
            visualFeedback?.NotifyFireSpell(spell, source, preview.Cells);
            developerFlow?.RefreshOutcome();
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
            selection.ClearTarget();
            enemyPlans.Invalidate();
            MarkPresentation(UiPresentationArea.Combat);
            PublishUiVisual(new UiVisualEvent(UiVisualEventKind.CombatCommandSubmitted, command.Type.ToString()));
            PublishCombatEffects(result.Execution);
            PublishFireExecutions(result.AttackFireExecutions);
            visualFeedback?.NotifySkillDelivery(result.DeliveredSkill, result.DeliverySource, result.DeliveryTarget);
            developerFlow?.RefreshOutcome();
        }

        private void PublishFireExecutions(IEnumerable<FireSpellExecution> executions)
            => feedbackPublisher.PublishFireExecutions(state, visualFeedback, executions,
                message => state.AddLog(message));
        private void PublishCombatEffects(CombatEffectExecution execution)
            => feedbackPublisher.PublishCombatEffects(state, visualFeedback, execution);
        public static bool CanSubmitTurnCommand(CombatCommand command, bool explicitHeroEndTurn) =>
            CombatCommandExecutionService.CanSubmit(command, explicitHeroEndTurn);

        private string GetRangeDescription() { int count = 0; if (state != null) for (int y = 0; y < state.Map.Height; y++) for (int x = 0; x < state.Map.Width; x++) if (IsInSelectedRange(new GridPosition(x, y))) count++; string rule = selection.Action == "\u79fb\u52a8" ? "\u79fb\u52a8\u8303\u56f4：3 \u683c" : selection.Action == "\u653b\u51fb" ? "\u653b\u51fb\u8303\u56f4：4 \u683c" : selection.Action == "\u65bd\u672f" ? "\u706b\u672f\u8303\u56f4：5 \u683c" : selection.Action == "\u4e92\u52a8" ? "\u4e92\u52d5\u8303\u56f4：1 \u683c" : "\u9053\u5177：\u81ea\u8eab\u4f7f\u7528"; return rule + "  |  \u9ad8\u4eae " + count + " \u683c"; }
        private bool IsInSelectedRange(GridPosition p)
        {
            int slot = RogueSkillSlot(selection.Action);
            FireSpellDefinition spell = slot < 0 ? null : FireSpellInSlot(slot);
            if (spell != null)
            {
                if (fireBattle == null || fireBattle.Combat != state) fireBattle = new FireBattleState(state);
                return IsFireSpellCellValid(spell, p);
            }
            return battlefield.IsInSelectedRange(state, selection.Action, p);
        }
        private bool IsSkillTargetInRange(SkillDefinition skill, GridPosition position) => battlefield.IsSkillTargetInRange(state, skill, position);
        private bool IsInMoveRange(GridPosition p) => battlefield.IsInMoveRange(state, p);
        private bool IsInAttackRange(GridPosition p) => battlefield.IsInAttackRange(state, p);
        private static int Distance(GridPosition a, GridPosition b) => BattlefieldPresentationAdapter.Distance(a, b);
        private static GridPosition StepToward(GridPosition a, GridPosition b) => BattlefieldPresentationAdapter.StepToward(a, b);
        private static Facing FacingToward(GridPosition a, GridPosition b) => BattlefieldPresentationAdapter.FacingToward(a, b);
    }
}
