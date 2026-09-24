using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using OCC.Combat.Roguelite;

namespace OCC.Combat.Presentation
{
    [ExecuteAlways]
    public sealed class CombatPrototypeBootstrap : MonoBehaviour, ICombatPresentationCompositionHost, ITacticalHudHost, ICombatActionPresentationHost
    {
        private const float UiWidth = 1920f;
        private const float UiHeight = 1080f;
        private readonly BattlefieldPresentationAdapter battlefield = new BattlefieldPresentationAdapter();
        private BattlefieldViewport battlefieldViewport;
        private bool followHeroMovement;
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
        private CombatFlowTransitionPresentation flowTransition => presentation?.FlowTransition;
        private CombatEntrySequencePresentation entrySequence => presentation?.EntrySequence;
        private FormalCombatHud combatHud => presentation?.CombatHud;
        private FormalBattlefieldView battlefieldView => presentation?.Battlefield;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private DeveloperConsolePanel developerConsole => presentation?.DeveloperConsole;
#endif
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
        private int displayedHeroTurnSequence = -1;

        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            CombatDebugTuning.TemporaryEnemyAssistEnabled = false;
            // The dedicated arena owns its own entry screen. Do not construct
            // the main first-experience landing page behind it: that page is a
            // real input surface and would intercept clicks on the test picker.
            if (GetComponent<CombatTestArenaEntry>() != null)
            {
                firstExperienceFrontEndComplete = true;
                InitializeRuntime();
                return;
            }
            if (!initialized && !firstExperienceFrontEndComplete)
            {
                FirstExperiencePrototypeController opening = GetComponent<FirstExperiencePrototypeController>();
                if (opening == null) opening = gameObject.AddComponent<FirstExperiencePrototypeController>();
                opening.Bind(this);
                return;
            }
            InitializeRuntime();
        }

        private bool firstExperienceFrontEndComplete;

        private void InitializeRuntime()
        {
            if (initialized) return;
            initialized = true;
            chineseFont = FormalUiKit.Font;
            barTexture = Resources.Load<Texture2D>("UI/Bar");
            uiPreferences = saveGateway.LoadUiPreferences();
            ApplyUiPreferences();
            developerPreparation = new MissionPreparation().Configure("relay_test", "完成学院演练并处置任务装置", "盾术生、火矢生、侧锋生、替身偶、寻迹兽");
            presentation = CombatPresentationComposition.Attach(gameObject, this);
            formalAssets.LoadRuntime();
        }

        public bool EnterFirstExperienceFromOpening(bool continueSave)
        {
            firstExperienceFrontEndComplete = true;
            InitializeRuntime();
            startupPresentation?.DismissImmediately();
            if (!TryStartMapRoguelite(continueSave, FireRogueliteStarterCatalog.Universal)) return false;
            if (mapRun != null && mapRun.IsFirstRunExperience && !mapRun.FirstRunExperience.Origin.Acknowledged)
            {
                mapInteractions.AcknowledgeFirstRunOrigin(mapRun);
                if (!SaveMapRun()) return false;
            }
            presentation?.RogueliteUi?.SetFrontEndSuppressed(false);
            return true;
        }

        public void EnterMainMenuFromOpening()
        {
            OpenFirstExperienceLanding();
        }

        private void OpenFirstExperienceLanding()
        {
            FirstExperiencePrototypeController opening = GetComponent<FirstExperiencePrototypeController>();
            if (opening == null) opening = gameObject.AddComponent<FirstExperiencePrototypeController>();
            opening.Bind(this);
            presentation?.RogueliteUi?.SetFrontEndSuppressed(true);
            firstExperienceFrontEndComplete = false;
            opening.ShowLanding();
        }

        public void OpenFirstExperienceFrontEnd(bool continueSave)
        {
            FirstExperiencePrototypeController opening = GetComponent<FirstExperiencePrototypeController>();
            if (opening == null) opening = gameObject.AddComponent<FirstExperiencePrototypeController>();
            opening.Bind(this);
            presentation?.RogueliteUi?.SetFrontEndSuppressed(true);
            firstExperienceFrontEndComplete = false;
            opening.enabled = true;
            if (continueSave) opening.ContinueRun(); else opening.StartNewRun();
        }

        public void ReplayOpeningCg()
        {
            FirstExperiencePrototypeController opening = GetComponent<FirstExperiencePrototypeController>();
            if (opening == null) opening = gameObject.AddComponent<FirstExperiencePrototypeController>();
            opening.Bind(this);
            presentation?.RogueliteUi?.SetFrontEndSuppressed(true);
            firstExperienceFrontEndComplete = false;
            opening.enabled = true;
            opening.ReplayWorldOpening();
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
            void ActivateCombat()
            {
                ApplyCombatSessionActivation(combatSession.Begin(developerFlow, enemyTurn, outcomeSettlement));
                MarkPresentation(UiPresentationArea.Flow);
            }
            if (flowTransition == null) ActivateCombat();
            else flowTransition.Play(ActivateCombat);
        }

        /// <summary>
        /// Direct entry used only by the dedicated CombatTestArena scene. It bypasses
        /// the first-experience front end while keeping the normal scene markers,
        /// presentation stack and developer combat setup intact.
        /// </summary>
        public void StartDedicatedTestArena(string scenarioId = null, bool playEntrySequence = true)
        {
            if (!Application.isPlaying) return;
            CombatTestArenaEntry entry = GetComponent<CombatTestArenaEntry>();
            if (entry == null) entry = FindAnyObjectByType<CombatTestArenaEntry>();
            if (entry != null) entry.CloseSelection();
            firstExperienceFrontEndComplete = true;
            FirstExperiencePrototypeController opening = GetComponent<FirstExperiencePrototypeController>();
            if (opening != null) opening.enabled = false;
            InitializeRuntime();
            startupPresentation?.DismissImmediately();
            presentation?.RogueliteUi?.SetFrontEndSuppressed(true);
            CombatSceneSessionBuild build = CombatTestArenaScenarioCatalog.BuildRandomized(scenarioId);
            state = build.State;
            currentLevel = build.Level;
            developerPreparation = build.Preparation;
            developerFlow = new CombatFlowController();
            developerFlow.Configure(developerPreparation, state);
            rogueliteFlow.Reset();
            trainingRangeActive = false;
            artifactBattle = new ArtifactBattleState(state);
            fireBattle = state.RogueSpells?.FireBattle;
            selection.Reset();
            outcomeSettlement.Reset();
            ResetEnemyTurnSequence();
            battlefieldViewport = battlefield.CreateViewport(state.Map.Width, state.Map.Height);
            battlefieldViewport.Focus(state.GetUnit("hero").Position);
            OpenDeveloperBriefing();
            // Test-arena selection is already the preparation step. Activate the
            // battle synchronously so a disabled/unfinished shared transition
            // can never leave this dedicated scene stranded on its briefing.
            ApplyCombatSessionActivation(combatSession.Begin(developerFlow, enemyTurn, outcomeSettlement), playEntrySequence);
            MarkPresentation(UiPresentationArea.Flow);
        }

        public void OpenDedicatedTestArenaSelection()
        {
            firstExperienceFrontEndComplete = true;
            FirstExperiencePrototypeController opening = GetComponent<FirstExperiencePrototypeController>();
            if (opening != null) opening.enabled = false;
            CombatTestArenaEntry entry = GetComponent<CombatTestArenaEntry>();
            if (entry == null) entry = FindAnyObjectByType<CombatTestArenaEntry>();
            if (entry != null) entry.ShowSelection();
            presentation?.Startup?.DismissImmediately();
            presentation?.RogueliteUi?.SetFrontEndSuppressed(true);
            GameObject formalRogueliteUi = GameObject.Find("OCC 表现层锚点/正式肉鸽UI");
            if (formalRogueliteUi != null) formalRogueliteUi.SetActive(false);
            GameObject formalStartupUi = GameObject.Find("OCC 表现层锚点/正式启动界面");
            if (formalStartupUi != null) formalStartupUi.SetActive(false);
        }
        public void TacticalRestartDeveloperCombat()
        {
            if (trainingRangeActive) { PrepareTrainingRangeCurrent(); return; }
            // A tactical restart resumes an already-introduced fight; replaying the opening
            // cinematic there would only delay the player.
            ApplyCombatSessionActivation(combatSession.Restart(developerFlow, enemyTurn, outcomeSettlement), false);
        }
        private void ApplyCombatSessionActivation(CombatSessionActivation activation, bool playEntrySequence = true)
        {
            state = activation.State;
            fireBattle = activation.FireBattle;
            FocusHeroInBattlefield();
            visualFeedback?.CancelEnemyAction();
            visualFeedback?.ResetBattleFeedback();
            displayedHeroTurnSequence = -1;
            PublishCombatEffects(activation.InitialTurnEffects);
            RefreshSceneHud();
            MarkPresentation(UiPresentationArea.Combat);
            battlefieldView?.QueueCombatEntry();
            combatHud?.QueueCombatEntry();
            // The entry cinematic owns this beat. Until it hands control back, Update() is
            // frozen, which also defers the hero turn banner instead of talking over the tour.
            if (playEntrySequence && entrySequence != null && entrySequence.Play())
            {
                MarkPresentation(UiPresentationArea.Flow);
                return;
            }
            PresentHeroTurnBannerIfNeeded();
        }
        public void ReturnToDeveloperMenu()
        {
            if (trainingRangeActive)
            {
                trainingRangeActive = false; rogueliteFlow.Reset();
                developerPreparation = new MissionPreparation().Configure("relay_test", "完成学院演练并处置任务装置", "盾术生、火矢生、侧锋生、替身偶、寻迹兽");
                state = null; currentLevel = null; developerFlow = null; battlefieldViewport = null;
                artifactBattle = null; fireBattle = null; selection.Reset(); outcomeSettlement.Reset(); ResetEnemyTurnSequence();
                RefreshSceneHud(); MarkPresentation(UiPresentationArea.Flow); MarkPresentation(UiPresentationArea.Combat);
                OpenFirstExperienceLanding();
                return;
            }
            developerFlow.ReturnToDeveloperMenu(); state = developerFlow.State; selection.Reset(); rogueliteFlow.Reset(); RefreshSceneHud(); MarkPresentation(UiPresentationArea.Flow);
            OpenFirstExperienceLanding();
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
        public bool HasFirstExperienceSave => FirstExperiencePrototypeController.HasAnySaveRecord();
        public MapSaveUiPresentation MapSavePresentation => mapSaves.Presentation;
        public bool IsMapRunSaved => mapSaves.LastSaveSucceeded;
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
                if (!SaveMapRun()) RestoreMapRunAfterFailedRewardSave();
                return;
            }
            if (!SaveMapRun()) { RestoreMapRunAfterFailedRewardSave(); return; }
            BuildCombatFromSceneStageTwo(); developerFlow.OpenBriefing();
            PublishUiVisual(new UiVisualEvent(UiVisualEventKind.BriefingOpened, nodeId));
        }

        public void AcknowledgeFirstRunOrigin()
        {
            if (mapRun == null || !mapRun.IsFirstRunExperience) return;
            mapInteractions.AcknowledgeFirstRunOrigin(mapRun);
            if (!SaveMapRun()) { RestoreMapRunAfterFailedRewardSave(); return; }
            MarkPresentation(UiPresentationArea.MapStructure);
        }

        public void CompleteFirstRunForge(string targetId)
            => ExecuteFirstRunService(() => mapInteractions.CompleteFirstRunForge(mapRun, targetId), "装备锻造完成，结果已保存。");

        public void CompleteFirstRunSpecialization(string targetId)
            => ExecuteFirstRunService(() => mapInteractions.CompleteFirstRunSpecialization(mapRun, targetId), "术式专精完成，结果已保存。");

        public void CompleteFirstRunHealthCheck()
            => ExecuteFirstRunService(() => mapInteractions.CompleteFirstRunHealthCheck(mapRun), "健康确认完成，精英入口条件已刷新。");

        public void UseFirstRunHeal()
            => ExecuteFirstRunService(() => mapInteractions.UseFirstRunHeal(mapRun), "治疗完成，生命与学院贡献已更新。");

        public void ChooseFirstRunMeal(string mealId)
            => ExecuteFirstRunService(() => mapInteractions.ChooseFirstRunMeal(mapRun, mealId), "餐食已结算，本轮不会刷新。");

        public void PurchaseFirstRunOffer(string offerId)
            => ExecuteFirstRunService(() => mapInteractions.PurchaseFirstRunOffer(mapRun, offerId), "购买完成，商品已放入背包。");

        public void CompleteFirstRunExperience()
        {
            // 离开商店不再结束本局：同一局继续进入随机层，因此留在学院地图上。
            if (!ExecuteFirstRunService(() => mapInteractions.CompleteFirstRunExperience(mapRun), "固定教学段已完成，学院一层已经展开。")) return;
            MarkPresentation(UiPresentationArea.MapStructure);
            ReturnToMapRun();
        }

        public void CompleteCurrentServiceNode()
        {
            if (mapRun == null || !mapRun.IsInAcademyLayer) return;
            try
            {
                mapRun.SettleCurrentServiceNode();
                MarkPresentation(UiPresentationArea.MapStructure);
                MarkPresentation(UiPresentationArea.MapResources);
                if (!SaveMapRun()) return;
                ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Success, "服务节点已经结算。"));
            }
            catch (Exception exception)
            {
                ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Rejected, exception.Message));
            }
        }

        private bool ExecuteFirstRunService(Func<RogueliteMapInteractionResult> operation, string success)
        {
            if (mapRun == null || !mapRun.IsFirstRunExperience)
            {
                ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Rejected, "当前没有可处理的首次学院旅程。"));
                return false;
            }
            try
            {
                RogueliteMapInteractionResult result = operation();
                PublishResourceChanges(result.ResourcesBefore, result.ResourcesAfter);
                MarkPresentation(UiPresentationArea.MapStructure);
                MarkPresentation(UiPresentationArea.MapResources);
                if (!mapRun.IsInAcademyLayer && !SaveMapRun()) return false;
                ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Success, success));
                return true;
            }
            catch (InvalidOperationException error)
            {
                ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Rejected, FirstRunServiceFailure(error.Message)));
                return false;
            }
        }

        private static string FirstRunServiceFailure(string message)
        {
            if (message == null) return "当前操作无法完成。";
            if (message.Contains("Insufficient gold")) return "金币不足，无法完成这项交易。";
            if (message.Contains("stage contribution")) return "学院贡献不足，无法完成这项服务。";
            if (message.Contains("academy food")) return "没有可用的学院食材。";
            if (message.Contains("Backpack")) return "背包空间不足，请先整理行囊。";
            if (message.Contains("forge")) return "锻造材料不足，或本轮锻造已经完成。";
            if (message.Contains("specialization")) return "专精材料不足，或本轮专精已经完成。";
            if (message.Contains("treatment")) return "当前不能再次治疗。";
            if (message.Contains("meal")) return "当前不能再次选择餐食。";
            return "当前状态不允许这项操作。";
        }

        public void StartMapNodeCombat(string nodeId)
        {
            if (mapRun == null || string.IsNullOrEmpty(nodeId) || !mapRun.MapNodes.Any(value => value.Id == nodeId && value.IsCombat))
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
            if (!result.StartsCombat)
                ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Success, MapSettlementReceipt(result.ResourcesBefore, result.ResourcesAfter)));
            if (result.StartsCombat)
            {
                if (!SaveMapRun()) { RestoreMapRunAfterFailedRewardSave(); return; }
                BuildCombatFromSceneStageTwo(); developerFlow.OpenBriefing(); PublishUiVisual(new UiVisualEvent(UiVisualEventKind.BriefingOpened, choiceId)); return;
            }
            if (!SaveMapRun()) RestoreMapRunAfterFailedRewardSave();
        }

        private static string MapSettlementReceipt(RogueliteMapResources before, RogueliteMapResources after)
        {
            System.Collections.Generic.List<string> changes = new System.Collections.Generic.List<string>();
            if (after.UsesRogue11)
            {
                if (after.Gold != before.Gold) changes.Add("金币 " + Signed(after.Gold - before.Gold));
                if (after.StageContribution != before.StageContribution) changes.Add("学院贡献 " + Signed(after.StageContribution - before.StageContribution));
                if (after.StageTime != before.StageTime) changes.Add("学期进度 " + Signed(after.StageTime - before.StageTime));
                if (after.CurrentHealth != before.CurrentHealth) changes.Add("生命 " + Signed(after.CurrentHealth - before.CurrentHealth));
            }
            else
            {
                if (after.Parts != before.Parts) changes.Add("零件 " + Signed(after.Parts - before.Parts));
                if (after.Aether != before.Aether) changes.Add("以太 " + Signed(after.Aether - before.Aether));
                if (after.Supplies != before.Supplies) changes.Add("补给 " + Signed(after.Supplies - before.Supplies));
                if (after.Scouting != before.Scouting) changes.Add("侦测 " + Signed(after.Scouting - before.Scouting));
            }
            return changes.Count == 0 ? "事件已结算，没有资源变化。" : "事件结算：" + string.Join("　", changes);
        }

        private static string Signed(int value) => value > 0 ? "+" + value : value.ToString();
        public void ClaimMapReward(string rewardId)
        {
            RogueliteMapInteractionResult result = mapInteractions.ClaimReward(mapRun, rewardId);
            PublishResourceChanges(result.ResourcesBefore, result.ResourcesAfter);
            PublishUiVisual(new UiVisualEvent(UiVisualEventKind.RewardClaimed, rewardId));
            CompleteMapRewardClaim();
        }
        public void RequestAbandonMapReward()
        {
            if (mapRun == null || !mapRun.AwaitingReward) return;
            RequestConfirmation(new UiConfirmationRequest(UiConfirmationKind.AbandonReward, "放弃本次奖励？",
                "确认后，本次候选与固定奖励都不会获得，且无法撤回。", "确认放弃"), () =>
                {
                    try
                    {
                        mapInteractions.AbandonReward(mapRun);
                        ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Information, "已记录放弃奖励。"));
                        CompleteMapRewardClaim();
                    }
                    catch (InvalidOperationException error)
                    {
                        ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Rejected, "无法放弃奖励：" + error.Message));
                    }
                });
        }
        public void OpenRewardInventory()
        {
            if (mapRun == null || !mapRun.UsesRogue11) return;
            ReturnToMapRun();
            settlementPresentation?.HideForInventory();
            presentation?.RogueliteUi?.OpenLoadoutForReward();
        }
        public void ClaimMapFireSpell(string spellId)
        {
            mapInteractions.ClaimFireSpell(mapRun, spellId);
            PublishUiVisual(new UiVisualEvent(UiVisualEventKind.RewardClaimed, spellId));
            CompleteMapRewardClaim();
        }
        public void EquipMapFireSpell(string spellId, int slot)
        {
            mapInteractions.EquipFireSpell(mapRun, spellId, slot);
            if (!SaveMapRun()) { RestoreMapRunAfterFailedRewardSave(); return; }
            MarkPresentation(UiPresentationArea.MapStructure); MarkPresentation(UiPresentationArea.Combat);
        }
        public void EquipNextMapFireSpell(int slot)
        {
            if (!mapInteractions.TryEquipNextFireSpell(mapRun, slot)) return;
            if (!SaveMapRun()) { RestoreMapRunAfterFailedRewardSave(); return; }
            MarkPresentation(UiPresentationArea.MapStructure); MarkPresentation(UiPresentationArea.Combat);
        }
        public void EquipMapReward(string rewardId)
        {
            mapInteractions.EquipReward(mapRun, rewardId);
            if (!SaveMapRun()) { RestoreMapRunAfterFailedRewardSave(); return; }
            MarkPresentation(UiPresentationArea.MapStructure);
        }
        public void CalibrateMapAether()
        {
            RogueliteMapInteractionResult result = mapInteractions.CalibrateAether(mapRun);
            if (!SaveMapRun()) { RestoreMapRunAfterFailedRewardSave(); return; }
            MarkPresentation(UiPresentationArea.MapStructure);
            PublishResourceChanges(result.ResourcesBefore, result.ResourcesAfter);
        }
        public void ReturnToMapRun() { developerFlow.ReturnToDeveloperMenu(); state = developerFlow.State; rogueliteFlow.ReturnToMap(); RefreshSceneHud(); MarkPresentation(UiPresentationArea.Flow); MarkPresentation(UiPresentationArea.MapStructure); }
        public void RequestReturnToLanding()
        {
            if (mapRun != null && !SaveMapRun()) return;
            ReturnToDeveloperMenu();
        }
        public void RetryCompleteMapRunSave()
        {
            if (mapRun == null || !mapRun.IsComplete || mapRun.AwaitingReward) return;
            if (SaveMapRun()) MarkPresentation(UiPresentationArea.Flow);
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

        private void CompleteMapRewardClaim()
        {
            // Do not close the still-visible settlement before its claimed state has reached the
            // verified save slot. A failed write must leave the player on the current screen.
            if (!SaveMapRun())
            {
                RestoreMapRunAfterFailedRewardSave();
                return;
            }
            if (ShouldReturnToMapAfterRewardClaim(mapRun))
            {
                ReturnToMapRun();
                MarkPresentation(UiPresentationArea.Settlement);
                return;
            }
            MarkPresentation(UiPresentationArea.Settlement);
            MarkPresentation(UiPresentationArea.MapStructure);
            settlementPresentation?.RefreshNow();
        }

        private void RestoreMapRunAfterFailedRewardSave()
        {
            RogueliteMapStartResult restored = mapSaves.TryStart(true, string.Empty, 0);
            if (!restored.Success) return;
            mapRun = restored.Run;
            rogueliteFlow.SetMapRun(mapRun);
            MarkPresentation(UiPresentationArea.Settlement);
            MarkPresentation(UiPresentationArea.MapStructure);
            MarkPresentation(UiPresentationArea.MapResources);
            settlementPresentation?.RefreshNow();
        }

        public static bool ShouldReturnToMapAfterRewardClaim(RogueliteMapRun run)
            => run != null && !run.AwaitingReward;
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
        public void FocusBattlefieldOnUnit(string unitId)
        {
            UnitState unit = state?.GetUnit(unitId);
            if (unit == null || battlefieldViewport == null) return;
            battlefieldViewport.Focus(unit.Position);
        }
        public void SetTimelineHoveredUnit(string unitId) => battlefieldView?.SetTimelineHoveredUnit(unitId);
        public void SubmitBattlefieldCell(GridPosition position, bool inspection)
        {
            if (state == null || !state.Map.IsInside(position)) return;
            if (inspection) HandleInspectionClick(position);
            else if (IsHeroPosition(position)) SelectHudAction("移动");
            else HandleCellClick(position);
        }

        private bool IsHeroPosition(GridPosition position)
        {
            UnitState hero = state?.GetUnit("hero");
            return hero != null && hero.IsAlive && hero.Position == position;
        }
        public bool CanQuickMoveTo(GridPosition position) => !IsCombatActionPlaying && state != null &&
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
            if (hero == null || !hero.IsAlive || state.ActiveUnitId != hero.Id || IsCombatActionPlaying) return actions;
            UnitState clicked = state.Units.Values.FirstOrDefault(unit => unit.IsAlive && unit.Position == position);

            AddContextActionIfLegal(actions, position, "移动", "move", "移动到这里", "1 行动点");
            AddContextActionIfLegal(actions, position, "攻击", "attack", "攻击" +
                (clicked != null && !clicked.IsHero ? "「" + clicked.DisplayName + "」" : string.Empty), "1 行动点");
            AddContextActionIfLegal(actions, position, "搜刮", "loot", "搜刮这里", "1 行动点");
            AddContextActionIfLegal(actions, position, "互动", "interact", "与这里互动", "1 行动点");
            for (int slot = 0; slot < RogueRuntimeConstants.ItemQuickbarSize; slot++)
            {
                if (!TryBuildContextArtifactAction(slot, position, out BattlefieldContextAction action)) continue;
                actions.Add(action);
            }
            for (int slot = 0; slot < RogueRuntimeConstants.SpellSlotCount; slot++)
            {
                if (!TryBuildContextSpellAction(slot, position, clicked, out BattlefieldContextAction action)) continue;
                actions.Add(action);
            }
            // This is intentionally target-independent: it belongs in every live right-click
            // menu so future contextual actions can share one stable action list.
            actions.Add(new BattlefieldContextAction("end-turn", "结束回合", "放弃剩余行动点"));
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
            if (actionId == "end-turn") { EndHeroTurn(); return; }
            if (actionId.StartsWith("artifact:", StringComparison.Ordinal) &&
                int.TryParse(actionId.Substring(9), out int artifactSlot))
            {
                ActivateInventoryQuickbar(artifactSlot);
                ArtifactDefinition artifact = CurrentArmedArtifact;
                if (artifact != null) TryArtifactCell(artifact,
                    state.Units.Values.FirstOrDefault(unit => unit.IsAlive && unit.Position == position), position);
                return;
            }
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

        /// <summary>
        /// Shows what the right-click menu is offering at <paramref name="position"/> before it is
        /// committed. An empty or unmapped <paramref name="actionId"/> marks only the anchor cell, so
        /// opening the menu does not repaint the board with the committed action's range.
        /// </summary>
        public void PreviewBattlefieldContextAction(GridPosition position, string actionId)
        {
            selection.SetMenuPreview(ContextPreviewAction(actionId), position);
            MarkPresentation(UiPresentationArea.Combat);
        }

        public void ClearBattlefieldContextPreview()
        {
            selection.ClearMenuPreview();
            MarkPresentation(UiPresentationArea.Combat);
        }

        /// <summary>
        /// Artifact rows deliberately preview nothing beyond the anchor: an artifact only becomes
        /// 技能1 after ActivateInventoryQuickbar arms it, so previewing 技能1 here would show whatever
        /// spell currently occupies that slot instead of the row the player is hovering.
        /// </summary>
        private static string ContextPreviewAction(string actionId)
        {
            if (string.IsNullOrEmpty(actionId)) return string.Empty;
            if (actionId == "move") return "移动";
            if (actionId == "attack") return "攻击";
            if (actionId == "loot") return "搜刮";
            if (actionId == "interact") return "互动";
            if (actionId.StartsWith("spell:", StringComparison.Ordinal) &&
                int.TryParse(actionId.Substring(6), out int slot)) return "技能" + (slot + 1);
            return string.Empty;
        }

        private void AddContextActionIfLegal(List<BattlefieldContextAction> actions, GridPosition position,
            string action, string id, string label, string detail)
        {
            if (string.IsNullOrEmpty(battlefield.InvalidReasonForCell(state, action, position)))
                actions.Add(new BattlefieldContextAction(id, label, detail));
        }

        private bool TryBuildContextArtifactAction(int slot, GridPosition position,
            out BattlefieldContextAction action)
        {
            action = null;
            if (state?.Ruleset != CombatRuleset.Roguelite || state.RogueEquipment == null ||
                slot < 0 || slot >= RogueRuntimeConstants.ItemQuickbarSize) return false;
            string instanceId = state.RogueEquipment.ItemQuickbarInstanceIds[slot];
            RogueTacticalItemInstance tactical = state.RogueEquipment.TacticalItem(instanceId);
            if (tactical == null || tactical.ChargesCurrent <= 0 ||
                !ArtifactCatalog.All.Any(value => value.Id == tactical.DefinitionId)) return false;
            ArtifactDefinition artifact = ArtifactCatalog.Get(tactical.DefinitionId);
            // The anchor brace is a quickbar reaction and has no manual target selection.
            if (artifact.Id == "G-T13") return false;
            EnsureArtifactBattle();
            if (!BuildArtifactTarget(artifact, position, out ArtifactTarget target) ||
                !ArtifactEngine.Preview(artifactBattle, "hero", artifact, target, tactical.ChargesCurrent).CanCommit) return false;
            action = new BattlefieldContextAction("artifact:" + slot,
                "[战术栏 " + (slot + 1) + "] 使用「" + artifact.DisplayName + "」", artifact.PublicCost);
            return true;
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
                    if (rogue.LineOfSightRule != "not_required" && !state.HasLineOfSight(hero.Position, position)) return false;
                }
                name = rogue.DisplayName;
                cost = rogue.ActionPointCost + " 行动点　" + rogue.ManaCost + " 个人魔力";
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
                        cost = fire.ActionPointCost + " 行动点　" + fire.ManaCost + " 以太";
                    }
                    else
                    {
                        if (slot > 1 || !string.IsNullOrEmpty(battlefield.InvalidReasonForCell(state,
                                "技能" + (slot + 1), position))) return false;
                        SkillDefinition skill = slot == 0 ? hero.SkillOne : hero.SkillTwo;
                        name = skill.DisplayName;
                        cost = "1 行动点　" + skill.ManaCost + " 以太";
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
        public void SetBattlefieldPreviewPosition(GridPosition position, bool active)
        {
            if (active && state?.Map.IsInside(position) == true) selection.SetPreviewPosition(position);
            else selection.ClearPreviewPosition();
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
        public Texture2D GetTestArenaGround(bool highResolution)
        {
            if (!CombatTestArenaEntry.IsDedicatedTestArena) return null;
            // Retained for tooling compatibility; the modular arena no longer uses a board-wide
            // texture. Callers should render the per-cell tile presentations instead.
            return formalAssets.Academy(highResolution
                ? "academy_test_ground_theme_slate_surface_64"
                : "academy_test_ground_theme_slate_surface_32");
        }
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
                bool hasDelay = armedArtifact.Effects.Any(effect => effect.Kind == ArtifactEffectKind.DelayInitiative);
                ArtifactEffectDefinition delay = armedArtifact.Effects.FirstOrDefault(effect => effect.Kind == ArtifactEffectKind.DelayInitiative);
                string delayTarget = !hasDelay ? string.Empty : delay.Scope == ArtifactEffectScope.Source ? "hero" : selection.TargetId;
                return new CombatActionPreview(action, armedArtifact.TargetSummary, armedArtifact.PublicCost,
                    armedArtifact.EffectSummary + "；风险：" + armedArtifact.RiskSummary, validArtifacts,
                    validArtifacts == 0 ? "现在没有可以选择的目标" : string.Empty,
                    actionValueTargetId: delayTarget, actionValueDelay: hasDelay ? delay.Amount : 0);
            }
            int slot = RogueSkillSlot(action);
            if (slot >= 0 && state?.Ruleset == CombatRuleset.Roguelite && state.RogueSpells != null)
            {
                OCC.Combat.Roguelite.SpellDefinition rogue = state.RogueSpells.DefinitionAtSlot(slot);
                if (rogue == null) return new CombatActionPreview(action, "空术式槽", "0 行动", "未装备术式", 0, "术式槽为空");
                FireSpellDefinition personal = FireSpellCatalog.All.FirstOrDefault(value => value.Id == rogue.DefinitionId);
                if (personal != null) return BuildPersonalFireSpellActionPreview(action, personal);
                int validTargets = rogue.Targeting == "self" ? 1 : state.Units.Values.Count(unit => unit.IsAlive && unit.IsHero != state.GetUnit("hero").IsHero);
                return new CombatActionPreview(action, RogueliteSettlementPresentation.RogueSpellTargetSummary(rogue),
                    rogue.ActionPointCost + " 行动 + " + rogue.ManaCost + " 个人魔力",
                    RogueliteSettlementPresentation.RogueSpellPlayerSummary(rogue), validTargets,
                    state.RogueSpells.IsReady(rogue.DefinitionId) ? string.Empty : "术式冷却中");
            }
            FireSpellDefinition spell = slot < 0 ? null : FireSpellInSlot(slot);
            if (spell == null || state == null) return availability.Preview(state, action, selection.TargetId);
            return BuildPersonalFireSpellActionPreview(action, spell);
        }
        private CombatActionPreview BuildPersonalFireSpellActionPreview(string action, FireSpellDefinition spell)
        {
            if (fireBattle == null || fireBattle.Combat != state)
                fireBattle = state.RogueSpells?.FireBattle ?? new FireBattleState(state);
            int valid = 0;
            for (int y = 0; y < state.Map.Height; y++) for (int x = 0; x < state.Map.Width; x++) if (IsFireSpellCellValid(spell, new GridPosition(x, y))) valid++;
            string failure = string.Empty;
            string before = string.Empty;
            string after = string.Empty;
            string breakdown = string.Empty;
            string statuses = string.Empty;
            int affected = 0;
            bool friendlyFire = false;
            UnitState selected = string.IsNullOrEmpty(selection.TargetId) ? null : state.GetUnit(selection.TargetId);
            if (selected != null)
            {
                FireSpellTarget target = FireSpellTarget.Unit(selected.Id, DirectionToward(state.GetUnit("hero").Position, selected.Position));
                FireSpellPreview exact = FireSpellEngine.Preview(fireBattle, "hero", spell, target);
                failure = string.Join("；", exact.Failures);
                affected = exact.UnitIds.Count;
                friendlyFire = exact.FriendlyFireRisk;
                if (exact.CanCommit && spell.Rules.Any(rule => rule.Kind == FireRuleKind.Damage || rule.Kind == FireRuleKind.WeaponDamage))
                {
                    CombatTargetDamageForecast forecast = CombatTargetDamageForecaster.FireSpell(fireBattle, "hero", spell, target, selected.Id);
                    before = "生命 " + selected.Health + "　护盾 " + selected.Shield;
                    after = "生命 " + forecast.RemainingHealth + "　护盾 " + forecast.RemainingShield;
                    breakdown = forecast.PlayerSummary.Replace('\n', '；');
                }
            }
            else if (valid == 0) failure = "现在没有可以选择的目标";
            string effects = RogueliteSettlementPresentation.FireSpellPlayerSummary(spell);
            string targetSummary = RogueliteSettlementPresentation.FireSpellTargetSummary(spell);
            return new CombatActionPreview(action, targetSummary,
                spell.ActionPointCost + " 行动 + " + spell.ManaCost + " 以太", effects, valid, failure,
                before, after, breakdown, statuses, affected, friendlyFire,
                actionValueTargetId: spell.InitiativeDelay > 0 ? "hero" : string.Empty, actionValueDelay: spell.InitiativeDelay);
        }
        private bool IsFireSpellCellValid(FireSpellDefinition spell, GridPosition position)
        {
            return BuildFireSpellPreviewAt(spell, position).CanCommit;
        }
        private FireSpellPreview BuildFireSpellPreviewAt(FireSpellDefinition spell, GridPosition position)
        {
            UnitState unit = state.Units.Values.FirstOrDefault(candidate => candidate.IsAlive && candidate.Position == position);
            CardinalDirection direction = FireSpellAimDirection(spell.Id, position);
            FireSpellTarget target = unit == null ? FireSpellTarget.At(position, direction) : FireSpellTarget.Unit(unit.Id, direction);
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
        public string CombatObjectiveSummary => CurrentPreparation?.RulesSummary ?? string.Empty;
        public Texture2D UnitPortrait(UnitState unit) => unit == null ? null : formalAssets.Unit(unit);
        public void CompleteCombatEntrySequence()
        {
            // The cinematic parked the camera on the hero; make the interactive pose explicit
            // so the first player command starts from a legal, clamped view.
            FocusHeroInBattlefield();
            MarkPresentation(UiPresentationArea.Combat);
        }
        public bool IsMapMenuOpen => mapMenuOpen;
        public bool IsRogueliteMenuOpen => rogueliteMenuOpen;
        public RogueliteUiPreferences UiPreferences => uiPreferences;
        public UiVisualEventStream UiVisualEvents => uiVisualEvents;
        public UiPresentationVersions UiPresentationVersions => uiPresentationVersions;
        public bool IsDeveloperCombatActive => developerFlow != null && developerFlow.Phase == CombatFlowPhase.Active;
        public bool IsCombatEntryBlocking => entrySequence != null && entrySequence.IsBlockingInput;
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
        public bool IsCombatActionPlaying => visualFeedback?.IsActionPlaying == true;
        public int CombatActionPresentationVersion => visualFeedback?.ActionPresentationVersion ?? 0;
        public UnitState PresentCombatUnit(UnitState unit) => visualFeedback?.PresentedUnit(unit) ?? unit;
        public bool IsInteractionModalOpen => IsCombatActionPlaying || battlefieldContextMenuOpen ||
            (entrySequence != null && entrySequence.IsBlockingInput) ||
            (interactionLayer != null && interactionLayer.IsConfirmationOpen) ||
            (inventoryPanel != null && inventoryPanel.IsOpen) || IsEncyclopediaOpen;
        public bool IsEncyclopediaOpen => presentation?.RogueliteUi?.IsEncyclopediaOpen == true;
        public void OpenEncyclopedia()
        {
            if (IsCombatActionPlaying || entrySequence != null && entrySequence.IsBlockingInput) return;
            InitializeRuntime();
            if (GetComponent<FirstExperiencePrototypeController>()?.enabled == true)
            {
                startupPresentation?.DismissImmediately();
                presentation?.RogueliteUi?.SetFrontEndSuppressed(true);
            }
            presentation?.RogueliteUi?.OpenEncyclopedia();
        }
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
            state.AddLog(trainingRangeSession.CurrentAbility.Id + "　" + report.Summary); MarkPresentation(UiPresentationArea.Combat); return report;
        }
        public TrainingRangeExecutionReport ExecuteTrainingRangeCurrent()
        {
            if (!DeveloperBuildGate.IsEnabled || trainingRangeSession == null) return null;
            if (IsCombatActionPlaying) return null;
            if (trainingRangeSession.CurrentCase == null || trainingRangeSession.CurrentCase.Combat != state) PrepareTrainingRangeCurrent();
            GridPosition source = state.GetUnit("hero").Position;
            TrainingRangePreviewReport preview = trainingRangeSession.PreviewCurrent();
            using var presentation = visualFeedback?.BeginResolvedAction("hero", fireBattle);
            TrainingRangeExecutionReport report = trainingRangeSession.ExecuteCurrent();
            state.AddLog(trainingRangeSession.CurrentAbility.Id + "　" + report.Summary);
            if (report.NativeResult is FireSpellExecution fireExecution)
                visualFeedback?.NotifyFireSpell(fireExecution);
            else if (report.NativeResult is ArtifactExecution artifactExecution)
                visualFeedback?.NotifyArtifact(trainingRangeSession.CurrentArtifact, source, preview.Cells, artifactExecution);
            else if (trainingRangeSession.CurrentSkill != null)
                visualFeedback?.NotifySkillDelivery(trainingRangeSession.CurrentSkill, source, trainingRangeSession.CurrentCase.RecommendedCell);
            presentation?.Complete();
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
            bool returningToTestSelection = CombatTestArenaEntry.IsDedicatedTestArena;
            RequestConfirmation(new UiConfirmationRequest(UiConfirmationKind.LeaveCombat, "离开这场战斗？",
                returningToTestSelection
                    ? "离开后，本场测试的状态不会保留。你会回到测试战斗选择。"
                    : "离开后，这场战斗中的收获和损失都不会留下。你会回到地图。",
                returningToTestSelection ? "离开并返回测试选择" : "离开并返回地图"), () =>
                {
                    if (returningToTestSelection) ReturnToDedicatedTestArenaSelection();
                    else if (mapRun != null) ReturnToMapRun();
                    else ReturnToDeveloperMenu();
                });
        }

        private void ReturnToDedicatedTestArenaSelection()
        {
            developerFlow.ReturnToDeveloperMenu();
            state = developerFlow.State;
            selection.Reset();
            outcomeSettlement.Reset();
            rogueliteFlow.Reset();
            RefreshSceneHud();
            OpenDedicatedTestArenaSelection();
            MarkPresentation(UiPresentationArea.Flow);
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
                if (string.Equals(rogue.Role, "passive", StringComparison.Ordinal))
                    return rogue.DisplayName + "是被动术式，装备后已经自动生效，无需选择目标。";
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
        public void OpenCombatInventoryPanel()
        {
            if (inventoryPanel != null) inventoryPanel.RequestOpen();
        }
        public bool TryOpenCombatInventory()
        {
            UnitState hero = state?.GetUnit("hero");
            if (IsCombatEntryBlocking)
            { ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Rejected, "战场还在展开，请稍候。")); return false; }
            if (!IsDeveloperCombatActive || hero == null || state.ActiveUnitId != hero.Id)
            { ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Rejected, "只能在自己的行动阶段进入整理界面。")); return false; }
            int openCountBefore = state.InventoryOpenCount;
            TryCommand(CombatCommand.OpenInventory(hero.Id));
            bool opened = state.InventoryOpenCount == openCountBefore + 1;
            if (opened) { PersistCombatInventory(); MarkPresentation(UiPresentationArea.Combat); }
            return opened;
        }
        public bool MoveRogueBackpackItem(string instanceId, int x, int y, bool rotated)
            => MutateRogueInventory(runtime => runtime.MoveBackpack(instanceId, x, y, rotated), "已移动", "该位置无法放置");
        public bool RotateRogueBackpackItem(string instanceId)
            => MutateRogueInventory(runtime => runtime.RotateBackpack(instanceId), "已旋转", "当前位置无法旋转");
        public bool EquipRogueEquipment(string instanceId, OCC.Combat.Roguelite.EquipmentSlot slot)
        {
            return MutateRogueInventory(runtime => runtime.Equip(instanceId, slot), "已装备", "槽位不匹配或已被占用");
        }
        public bool EquipOrReplaceRogueEquipment(string instanceId, OCC.Combat.Roguelite.EquipmentSlot slot)
        {
            return MutateRogueInventory(runtime => runtime.EquipOrReplace(instanceId, slot), "已装备；原装备已放回背包", "槽位不匹配或背包空间不足");
        }
        public bool UnequipRogueEquipment(OCC.Combat.Roguelite.EquipmentSlot slot)
        {
            return MutateRogueInventory(runtime => runtime.Unequip(slot), "已放回背包", "背包空间不足或槽位为空");
        }
        public bool UnequipRogueEquipmentTo(OCC.Combat.Roguelite.EquipmentSlot slot, int x, int y, bool rotated)
        {
            return MutateRogueInventory(runtime => runtime.UnequipToBackpack(slot, x, y, rotated), "已放入指定背包格", "该位置无法放置或槽位为空");
        }
        public bool AssignRogueQuickbar(string instanceId, int slot)
            => MutateRogueInventory(runtime => runtime.AssignQuickbar(slot, instanceId), "已关联战术栏 " + (slot + 1), "只有背包中的战术道具可以关联");
        public bool AssignRogueSpell(string spellId, int slot)
        {
            if (mapRun == null || !mapRun.AssignRogueSpell(spellId, slot))
            {
                ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Rejected, "术式未掌握、已在其他槽位，或槽位无效"));
                return false;
            }
            if (!SaveMapRun()) { RestoreMapRunAfterFailedRewardSave(); return false; }
            MarkPresentation(UiPresentationArea.MapStructure);
            MarkPresentation(UiPresentationArea.Combat);
            ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Success, string.IsNullOrEmpty(spellId) ? "已移除槽位术式" : "术式编组已保存"));
            return true;
        }
        private bool MutateRogueInventory(Func<RogueEquipmentRuntime, bool> operation, string success, string failure)
        {
            if (mapRun == null || !mapRun.UsesRogue11) return false;
            bool combatRuntime = state != null && state.Ruleset == CombatRuleset.Roguelite && state.RogueEquipment != null;
            RogueEquipmentRuntime runtime = combatRuntime ? state.RogueEquipment : RogueEquipmentRuntime.FromDto(mapRun.RogueRunState);
            if (!operation(runtime)) { ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Rejected, failure)); return false; }
            if (combatRuntime) PersistCombatInventory();
            else
            {
                runtime.WriteToDto(mapRun.RogueRunState);
                if (!SaveMapRun()) { RestoreMapRunAfterFailedRewardSave(); return false; }
            }
            MarkPresentation(combatRuntime ? UiPresentationArea.Combat : UiPresentationArea.MapStructure);
            ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Success, success)); return true;
        }
        private void PersistCombatInventory() { if (mapRun == null || state == null) return; mapRun.CaptureCombatInventory(state); SaveMapRun(); }
        public void ApplyHudBuild(int build) { if (state != null) ApplyBuild(build); }
        public void EndHeroTurn() { if (state != null) TryCommand(CombatCommand.EndTurn("hero"), true); }
        private void Update()
        {
            if (!Application.isPlaying || developerFlow == null || state == null) return;
            // The entry cinematic drives the camera and owns the opening beat; hold the
            // simulation, the hero-follow panner and the turn banner until it hands control back.
            if (entrySequence != null && entrySequence.IsBlockingInput) return;
            if (followHeroMovement) FollowHeroAtSafeEdge();
            if (IsCombatActionPlaying) return;
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
                !string.IsNullOrEmpty(state.ActiveUnitId) && state.ActiveUnitId != "hero") { RunEnemyTurn(); RefreshCombatOutcomeAfterPresentation(); }
            else if (state.ActiveUnitId == "hero" && enemyTurn.IsRunning) ResetEnemyTurnSequence();
            RefreshCombatOutcomeAfterPresentation(); HandleRogueliteOutcome();
            PresentHeroTurnBannerIfNeeded();
            if (developerFlow.Phase != phaseBeforeUpdate) { MarkPresentation(UiPresentationArea.Flow); MarkPresentation(UiPresentationArea.Combat); }
        }

        private void PresentHeroTurnBannerIfNeeded()
        {
            if (developerFlow?.Phase != CombatFlowPhase.Active || state == null || state.ActiveUnitId != "hero" ||
                state.TurnSequence <= 0 || state.TurnSequence == displayedHeroTurnSequence) return;
            UnitState hero = state.GetUnit("hero");
            if (hero == null) return;
            displayedHeroTurnSequence = state.TurnSequence;
            visualFeedback?.BeginHeroAction(hero, state.TurnSequence);
        }
        private void HandleRogueliteOutcome()
        {
            if (IsCombatActionPlaying) return;
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
            if (mapRun != null && developerFlow.Phase == CombatFlowPhase.Victory) { ReturnToMapRun(); return; }
            if (rogueliteRun == null || (developerFlow.Phase != CombatFlowPhase.Victory && developerFlow.Phase != CombatFlowPhase.Defeat)) return;
            if (developerFlow.Phase == CombatFlowPhase.Victory && rogueliteRun.IsShortRun) { OpenShortRunPhase(); return; }
            if (developerFlow.Phase == CombatFlowPhase.Victory && rogueliteRun.Kind == RogueliteLaunchKind.StoryChain && !rogueliteRun.Package.IsComplete) { BuildCombatFromSceneStageTwo(); developerFlow.OpenBriefing(); return; }
            // 首段（含首次体验）的战斗全部打完：进入并继续首段地图局，回到地图屏，而不是掉回开始界面。
            if (developerFlow.Phase == CombatFlowPhase.Victory && rogueliteRun.Kind == RogueliteLaunchKind.StoryChain &&
                TryStartMapRoguelite(true, FireRogueliteStarterCatalog.Universal))
            {
                rogueliteRun = null;
                ReturnToMapRun();
                return;
            }
            developerFlow.ReturnToDeveloperMenu(); state = developerFlow.State; rogueliteFlow.OpenRogueliteMenu(); RefreshSceneHud();
        }
        public void ForceCurrentOutcome(bool victory)
        {
            if (!DeveloperBuildGate.IsEnabled) return;
            if (developerFlow?.Phase != CombatFlowPhase.Active) return;
            state.ResolveDebugOutcome(victory); developerFlow.RefreshOutcome(); HandleRogueliteOutcome(); MarkPresentation(UiPresentationArea.Flow); MarkPresentation(UiPresentationArea.Combat);
        }

        // 开发线一键过关：只在开发构建可用，不影响正式玩家 UI。
        public bool CanUseDeveloperMapAdvance => DeveloperBuildGate.IsEnabled && mapRun != null && !mapRun.IsComplete;
        public string DeveloperMapAdvanceSummary { get; private set; } = string.Empty;

        /// <summary>
        /// 开发线一键过关（战斗内也生效）：战斗内先按正常胜利路径消灭全部敌人并播放胜利，
        /// 然后停在战后奖励界面由玩家自己挑；不在战斗中时才结算地图节点。
        /// </summary>
        public void DeveloperForceWinEverywhere()
        {
            if (!DeveloperBuildGate.IsEnabled) return;
            if (developerFlow?.Phase == CombatFlowPhase.Active)
            {
                ForceCurrentOutcome(true);
                return;
            }
            if (developerFlow?.Phase == CombatFlowPhase.Victory || developerFlow?.Phase == CombatFlowPhase.Defeat) return;
            if (CanUseDeveloperMapAdvance)
            {
                RogueliteDeveloperAdvanceReport report = RogueliteDeveloperRunPolicy.ForceWinCurrentCombat(mapRun, false);
                PublishDeveloperMapAdvance(report);
            }
        }

        public void DeveloperForceWinCurrentCombat()
        {
            if (!CanUseDeveloperMapAdvance) return;
            RogueliteDeveloperAdvanceReport report = RogueliteDeveloperRunPolicy.ForceWinCurrentCombat(mapRun);
            PublishDeveloperMapAdvance(report);
        }

        public void DeveloperAdvanceToFinale()
        {
            if (!CanUseDeveloperMapAdvance) return;
            RogueliteDeveloperAdvanceReport report = RogueliteDeveloperRunPolicy.AdvanceToFinale(mapRun);
            PublishDeveloperMapAdvance(report);
        }

        public void DeveloperSettleCurrentReward()
        {
            if (!CanUseDeveloperMapAdvance) return;
            RogueliteDeveloperAdvanceReport report = new RogueliteDeveloperAdvanceReport();
            RogueliteDeveloperRunPolicy.TrySettlePendingReward(mapRun, report);
            PublishDeveloperMapAdvance(report);
        }

        private void PublishDeveloperMapAdvance(RogueliteDeveloperAdvanceReport report)
        {
            DeveloperMapAdvanceSummary = report.Summary;
            MarkPresentation(UiPresentationArea.MapStructure);
            MarkPresentation(UiPresentationArea.MapResources);
            MarkPresentation(UiPresentationArea.Settlement);
            if (!SaveMapRun()) return;
            settlementPresentation?.RefreshNow();
            ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Success, report.Summary));
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
                    if (state.ActiveUnitId == enemy.Id)
                        PublishCombatEffects(CombatResolver.EndTurn(state, enemy));
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
            enemyPlans.Invalidate();
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
            if (hero == null || battlefieldViewport == null) { followHeroMovement = false; return; }
            CombatMovementPose pose = visualFeedback?.UnitTravelPose(hero) ??
                new CombatMovementPose(new Vector2(hero.Position.X, hero.Position.Y));
            battlefieldViewport.FollowVisualPosition(pose.Position.x, pose.Position.y);
            if (pose.Position == new Vector2(hero.Position.X, hero.Position.Y)) followHeroMovement = false;
        }

        public CombatTargetDamageForecast TargetDamageForecast(UnitState enemy)
        {
            int slot = RogueSkillSlot(selection.Action);
            FireSpellDefinition fireSpell = slot < 0 ? null : FireSpellInSlot(slot);
            ArtifactDefinition artifact = slot == 0 ? (CurrentArmedArtifact ?? CurrentTrainingRangeArtifact) : null;
            CombatTargetForecastResult result = targetForecasts.Evaluate(
                battlefield, state, fireBattle, selection.Action, enemy, fireSpell, artifact, CurrentArmedUses);
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
            if (fireBattle?.OptionalMoves.Any(offer => offer.SourceUnitId == "hero" && offer.Destination == p) == true)
            {
                try
                {
                    using var optionalPresentation = visualFeedback?.BeginResolvedAction("hero", fireBattle);
                    FireSpellExecution optionalMove = FireSpellEngine.ExecuteOptionalMove(fireBattle, "hero", p);
                    state.AddLog(optionalMove.Preview.Spell.DisplayName + "：已选择移动至破口落点。");
                    selection.ClearTarget(); enemyPlans.Invalidate(); MarkPresentation(UiPresentationArea.Combat);
                    visualFeedback?.NotifyFireSpell(optionalMove);
                    PublishFireExecutions(optionalMove.MovementTriggers);
                    PublishCombatEffects(optionalMove.PressureReaction);
                    optionalPresentation?.Complete(); RefreshCombatOutcomeAfterPresentation();
                }
                catch (InvalidOperationException error)
                {
                    fireBattle.DeclineOptionalMoves();
                    state.AddLog(error.Message); MarkPresentation(UiPresentationArea.Combat);
                }
                return;
            }
            fireBattle?.DeclineOptionalMoves();
            UnitState clickedUnit = state.Units.Values.FirstOrDefault(unit => unit.IsAlive && unit.Position == p);
            UnitState enemy = clickedUnit != null && !clickedUnit.IsHero ? clickedUnit : null;
            ArtifactDefinition artifact = CurrentArmedArtifact ?? CurrentTrainingRangeArtifact;
            if (selection.Action == "技能1" && artifact != null)
            {
                TryArtifactCell(artifact, clickedUnit, p);
                return;
            }
            int fireSlot = RogueSkillSlot(selection.Action);
            if (fireSlot >= 0 && state.Ruleset == CombatRuleset.Roguelite && state.RogueSpells != null)
            {
                OCC.Combat.Roguelite.SpellDefinition rogue = state.RogueSpells.DefinitionAtSlot(fireSlot);
                if (rogue == null) { state.AddLog("术式槽为空。"); MarkPresentation(UiPresentationArea.Combat); return; }
                bool chooseSlow = rogue.DefinitionId == "F-P-U28" &&
                    IsShiftHeld();
                CombatCommand command = rogue.DefinitionId == "F-P-U28"
                    ? CombatCommand.UseSkillAt("hero", fireSlot, state.GetUnit("hero").Position,
                        chooseSlow ? CardinalDirection.West : CardinalDirection.East)
                    : rogue.Targeting == "self" ? CombatCommand.UseSkill("hero", fireSlot, "hero") :
                    clickedUnit != null ? CombatCommand.UseSkill("hero", fireSlot, clickedUnit.Id) : CombatCommand.UseSkillAt("hero", fireSlot, p, FireSpellAimDirection(rogue.DefinitionId, p));
                TryCommand(command); return;
            }
            FireSpellDefinition fireSpell = fireSlot < 0 ? null : FireSpellInSlot(fireSlot);
            if (fireSpell != null)
            {
                TryFireSpellCell(fireSpell, clickedUnit, p);
                return;
            }
            string invalidReason = battlefield.InvalidReasonForCell(state, selection.Action, p);
            if (!string.IsNullOrEmpty(invalidReason))
            {
                PublishUiVisual(new UiVisualEvent(UiVisualEventKind.CombatCommandRejected, selection.Action, message: invalidReason));
                return;
            }
            if (selection.Action == "\u79fb\u52a8") TryCommand(CombatCommand.Move("hero", p));
            else if (selection.Action == "\u653b\u51fb" && enemy != null) TryCommand(CombatCommand.Attack("hero", enemy.Id));
            else if (selection.Action == "\u6280\u80fd1") TrySkillCell(0, state.GetUnit("hero").SkillOne, clickedUnit, p);
            else if (selection.Action == "\u6280\u80fd2") TrySkillCell(1, state.GetUnit("hero").SkillTwo, clickedUnit, p);
            else if (selection.Action == "\u641c\u522e")
            {
                if (state.LootSource != null) OpenCombatInventoryPanel();
                else TryCommand(CombatCommand.Loot("hero"));
            }
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
            if (IsCombatActionPlaying) return;
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
            using var presentation = visualFeedback?.BeginResolvedAction("hero", fireBattle);
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
            visualFeedback?.NotifyArtifact(artifact, source, preview.Cells, execution);
            presentation?.Complete(); RefreshCombatOutcomeAfterPresentation();
        }
        private void TryFireSpellCell(FireSpellDefinition spell, UnitState clickedUnit, GridPosition position)
        {
            if (IsCombatActionPlaying) return;
            if (fireBattle == null || fireBattle.Combat != state) fireBattle = new FireBattleState(state);
            if (trainingRangeSession?.CurrentArtifact != null && trainingRangeArtifactUsesRemaining <= 0)
            {
                const string depleted = "法宝封装次数已耗尽；请打开靶场配置并重新装载。";
                state.AddLog(depleted);
                PublishUiVisual(new UiVisualEvent(UiVisualEventKind.CombatCommandRejected, spell.Id, message: depleted));
                MarkPresentation(UiPresentationArea.Combat);
                return;
            }
            CardinalDirection direction = FireSpellAimDirection(spell.Id, position);
            FireSpellTarget target = clickedUnit == null ? FireSpellTarget.At(position, direction) : FireSpellTarget.Unit(clickedUnit.Id, direction);
            FireSpellPreview preview = FireSpellEngine.Preview(fireBattle, "hero", spell, target);
            selection.SetKnownTarget(clickedUnit?.Id);
            if (!preview.CanCommit)
            {
                string reason = string.Join("；", preview.Failures); state.AddLog(reason);
                PublishUiVisual(new UiVisualEvent(UiVisualEventKind.CombatCommandRejected, spell.Id, message: reason)); MarkPresentation(UiPresentationArea.Combat); return;
            }
            GridPosition source = state.GetUnit("hero").Position;
            using var presentation = visualFeedback?.BeginResolvedAction("hero", fireBattle);
            FireSpellExecution execution = FireSpellEngine.Execute(fireBattle, "hero", spell, target);
            if (trainingRangeSession?.CurrentArtifact != null) trainingRangeArtifactUsesRemaining--;
            if (!trainingRangeActive && !string.IsNullOrEmpty(armedInventoryItemId))
            {
                string usedId = armedInventoryItemId; state.ConsumeInventoryItem(usedId); if (state.ItemInventory.Get(usedId) == null) armedInventoryItemId = null; PersistCombatInventory();
            }
            if (trainingRangeActive) trainingRangeSession?.RecordExternal(preview, execution);
            state.AddLog(spell.DisplayName + "已经生效。");
            selection.ClearTarget(); MarkPresentation(UiPresentationArea.Combat);
            visualFeedback?.NotifyFireSpell(execution);
            presentation?.Complete(); RefreshCombatOutcomeAfterPresentation();
        }

        private void TrySkillCell(int slot, SkillDefinition skill, UnitState clickedUnit, GridPosition position)
        {
            if (!battlefield.IsSkillTargetInRange(state, skill, position))
            {
                PublishUiVisual(new UiVisualEvent(UiVisualEventKind.CombatCommandRejected, skill == null ? "技能" : skill.DisplayName, message: "目标超出有效范围"));
                return;
            }
            if (skill.TargetRule == SkillTargetRule.GridCell || skill.TargetRule == SkillTargetRule.Destructible)
                TryCommand(CombatCommand.UseSkillAt("hero", slot, position, DirectionToward(state.GetUnit("hero").Position, position)));
            else if (skill.TargetRule == SkillTargetRule.Self)
                TryCommand(CombatCommand.UseSkill("hero", slot, null));
            else if (clickedUnit != null)
                TryCommand(CombatCommand.UseSkill("hero", slot, clickedUnit.Id));
        }
        private void TryCommand(CombatCommand command, bool explicitHeroEndTurn = false)
        {
            if (IsCombatActionPlaying) return;
            fireBattle?.DeclineOptionalMoves();
            using var presentation = command.Type == CombatCommandType.Move ? null :
                visualFeedback?.BeginResolvedAction(command.UnitId, fireBattle, command.Type == CombatCommandType.Attack,
                    state.GetUnit(command.TargetUnitId)?.Position ?? command.Destination);
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
            if (result.HeroMoved) followHeroMovement = true;
            selection.ClearTarget();
            enemyPlans.Invalidate();
            MarkPresentation(UiPresentationArea.Combat);
            feedbackPublisher.PublishCombatEffects(state, visualFeedback, result.Execution, result.MovementPath);
            PublishFireExecutions(result.AttackFireExecutions);
            visualFeedback?.NotifySkillDelivery(result.DeliveredSkill, result.DeliverySource, result.DeliveryTarget);
            if (artifactBattle?.Combat == state)
                foreach (ArtifactExecution reaction in artifactBattle.TakeResolvedReactions())
                    visualFeedback?.NotifyArtifact(null, reaction.SourcePosition, Array.Empty<GridPosition>(), reaction);
            presentation?.Complete(); RefreshCombatOutcomeAfterPresentation();
        }

        private void RefreshCombatOutcomeAfterPresentation()
        { if (!IsCombatActionPlaying) developerFlow?.RefreshOutcome(); }

        private void PublishFireExecutions(IEnumerable<FireSpellExecution> executions)
            => feedbackPublisher.PublishFireExecutions(state, visualFeedback, executions,
                message => state.AddLog(message));
        private void PublishCombatEffects(CombatEffectExecution execution)
            => feedbackPublisher.PublishCombatEffects(state, visualFeedback, execution);
        public static bool CanSubmitTurnCommand(CombatCommand command, bool explicitHeroEndTurn) =>
            CombatCommandExecutionService.CanSubmit(command, explicitHeroEndTurn);

        private string GetRangeDescription()
        {
            int count = 0;
            if (state != null)
                for (int y = 0; y < state.Map.Height; y++)
                    for (int x = 0; x < state.Map.Width; x++)
                        if (IsInSelectedRange(new GridPosition(x, y))) count++;
            // Reuse the same preview text the combat HUD shows, so the announced range can never drift
            // from the highlighted cells or from the numbers in the action panel.
            CombatActionPreview preview = BuildActionPreview(selection.Action);
            string rule = preview == null || string.IsNullOrWhiteSpace(preview.TargetRule)
                ? selection.Action : selection.Action + "：" + preview.TargetRule;
            return rule + "　高亮 " + count + " 格";
        }
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
        private CardinalDirection FireSpellAimDirection(string spellId, GridPosition position)
        {
            CardinalDirection direction = DirectionToward(state.GetUnit("hero").Position, position);
            bool shifted = IsShiftHeld();
            if (spellId == "F-P-U28") return shifted ? CardinalDirection.West : CardinalDirection.East;
            if (spellId == "F-P-M21" && shifted)
                return direction == CardinalDirection.East || direction == CardinalDirection.West
                    ? CardinalDirection.North : CardinalDirection.East;
            if (spellId == "F-P-R25")
            {
                int turns = shifted ? 1 : IsControlHeld() ? 2 : IsAltHeld() ? 3 : 0;
                for (int i = 0; i < turns; i++)
                    direction = direction == CardinalDirection.North ? CardinalDirection.East :
                        direction == CardinalDirection.East ? CardinalDirection.South :
                        direction == CardinalDirection.South ? CardinalDirection.West : CardinalDirection.North;
            }
            return direction;
        }
        private static bool IsShiftHeld()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
        }
        private static bool IsControlHeld()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed);
        }
        private static bool IsAltHeld()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && (keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed);
        }
        private static CardinalDirection DirectionToward(GridPosition a, GridPosition b) => BattlefieldPresentationAdapter.DirectionToward(a, b);
    }
}
