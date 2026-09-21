using System;
using System.Collections.Generic;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    public sealed class BattlefieldContextAction
    {
        public string Id { get; }
        public string Label { get; }
        public string Detail { get; }

        public BattlefieldContextAction(string id, string label, string detail)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("An action id is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("An action label is required.", nameof(label));
            Id = id;
            Label = label;
            Detail = detail ?? string.Empty;
        }
    }

    public interface IUiPreferenceHost
    {
        RogueliteUiPreferences UiPreferences { get; }
    }

    public interface IUiFeedbackHost
    {
        void ShowUiFeedback(UiActionFeedback feedback);
    }

    public interface ICombatFeedbackHost : IUiPreferenceHost
    {
        CombatState CurrentState { get; }
        BattlefieldRect CurrentBattlefieldViewport { get; }
        bool IsDeveloperCombatActive { get; }
        Vector2 GridToFeedbackPosition(GridPosition position);
    }

    public interface ICombatHudHost : IUiPreferenceHost, IUiFeedbackHost
    {
        CombatState CurrentState { get; }
        RogueliteMapRun CurrentMapRun { get; }
        FireBattleState CurrentFireBattle { get; }
        CombatActionPreview CurrentActionPreview { get; }
        CombatOutcomePresentation CurrentOutcomePresentation { get; }
        ItemInstance CurrentArmedInventoryItem { get; }
        ArtifactDefinition CurrentArmedArtifact { get; }
        ArtifactDefinition CurrentTrainingRangeArtifact { get; }
        UiPresentationVersions UiPresentationVersions { get; }
        string CurrentPhaseText { get; }
        string SelectedAction { get; }
        string SelectedTargetId { get; }
        bool IsCombatOutcomeVisible { get; }
        bool IsDeveloperCombatActive { get; }
        bool IsInteractionModalOpen { get; }
        bool IsKeyboardTargeting { get; }
        GridPosition KeyboardTargetPosition { get; }
        int TrainingRangeArtifactUsesRemaining { get; }
        EnemyIntentPresentation EnemyIntent(UnitState enemy);
        CombatActionPreview ActionPreview(string action);
        FireSpellDefinition FireSpellInSlot(int slot);
        void SelectHudAction(string action);
        bool TrySelectSpellShortcut(int slot);
        void OpenCombatInventoryPanel();
        bool TryOpenCombatInventory();
        void ActivateInventoryQuickbar(int slot);
        void EndHeroTurn();
        bool BeginKeyboardTargeting();
        void MoveKeyboardTarget(int deltaX, int deltaY);
        void CommitKeyboardTarget();
        void CancelKeyboardTargeting();
        void CancelCombatSelectionOrRequestLeave();
        void RequestLeaveCombat();
        void RequestTacticalRestart();
        void ReturnToDeveloperMenu();
        void FocusBattlefieldOnUnit(string unitId);
        void SetTimelineHoveredUnit(string unitId);
    }

    public interface IRogueliteUiHost : IUiPreferenceHost, IUiFeedbackHost
    {
        RogueliteMapRun CurrentMapRun { get; }
        RogueliteMapRun ArchivedMapRun { get; }
        CombatFlowPhase CurrentFlowPhase { get; }
        MissionPreparation CurrentPreparation { get; }
        MapSaveUiPresentation MapSavePresentation { get; }
        UiPresentationVersions UiPresentationVersions { get; }
        UiVisualEventStream UiVisualEvents { get; }
        string SettingsSaveDetail { get; }
        bool IsInteractionModalOpen { get; }
        bool IsMapMenuOpen { get; }
        bool HasFirstExperienceSave { get; }
        void AcknowledgeFirstRunOrigin();
        void CompleteFirstRunForge(string targetId);
        void CompleteFirstRunSpecialization(string targetId);
        void CompleteFirstRunHealthCheck();
        void UseFirstRunHeal();
        void ChooseFirstRunMeal(string mealId);
        void PurchaseFirstRunOffer(string offerId);
        void CompleteFirstRunExperience();
        void CompleteCurrentServiceNode();
        void SelectMapNode(string nodeId);
        void StartMapNodeCombat(string nodeId);
        void ChooseMapNodeContent(string choiceId);
        void EquipMapReward(string rewardId);
        void EquipNextMapFireSpell(int slot);
        void CalibrateMapAether();
        bool MoveRogueBackpackItem(string instanceId, int x, int y, bool rotated);
        bool RotateRogueBackpackItem(string instanceId);
        bool EquipRogueEquipment(string instanceId, OCC.Combat.Roguelite.EquipmentSlot slot);
        bool EquipOrReplaceRogueEquipment(string instanceId, OCC.Combat.Roguelite.EquipmentSlot slot);
        bool UnequipRogueEquipment(OCC.Combat.Roguelite.EquipmentSlot slot);
        bool UnequipRogueEquipmentTo(OCC.Combat.Roguelite.EquipmentSlot slot, int x, int y, bool rotated);
        bool AssignRogueQuickbar(string instanceId, int slot);
        bool AssignRogueSpell(string spellId, int slot);
        void NotifyMapNodeSelected(string nodeId);
        void OpenFirstExperienceFrontEnd(bool continueSave);
        void ReplayOpeningCg();
        void RequestReturnToLanding();
        void RequestStartMapRoguelite(bool continueSave);
        void RequestStartMapRoguelite(bool continueSave, string starterId);
        void ReturnToMapRun();
        void StartDeveloperCombat();
        void UpdateUiPreferences(float masterVolume, float animationIntensity, bool screenShake,
            bool floatingText, bool highContrast, bool largeText, bool keyHints);
    }

    public interface IStartupPresentationHost : IUiPreferenceHost
    {
    }

    public interface IInteractionPresentationHost : IUiPreferenceHost
    {
        UiVisualEventStream UiVisualEvents { get; }
        bool IsDeveloperCombatActive { get; }
        bool IsMapMenuOpen { get; }
        void PublishUiVisual(UiVisualEvent visualEvent);
    }

    public interface ISettlementPresentationHost : IUiPreferenceHost, IUiFeedbackHost
    {
        RogueliteMapRun CurrentMapRun { get; }
        UiPresentationVersions UiPresentationVersions { get; }
        void ClaimMapFireSpell(string spellId);
        void ClaimMapReward(string rewardId);
        void RequestAbandonMapReward();
        void OpenRewardInventory();
        void PublishUiVisual(UiVisualEvent visualEvent);
    }

    public interface IInventoryPresentationHost
    {
        CombatState CurrentState { get; }
        bool IsDeveloperCombatActive { get; }
        /// <summary>
        /// True while the combat-entry cinematic still owns the screen.  This panel draws with
        /// IMGUI, which composites above every canvas, so it has to suppress itself rather than
        /// rely on the entry overlay covering it.
        /// </summary>
        bool IsCombatEntryBlocking { get; }
        bool TryOpenCombatInventory();
        void ActivateInventoryQuickbar(int slot);
        void EquipInventoryQuickbar(string instanceId, int slot);
        void NotifyInventoryChanged();
        bool MoveRogueBackpackItem(string instanceId, int x, int y, bool rotated);
        bool RotateRogueBackpackItem(string instanceId);
        bool AssignRogueQuickbar(string instanceId, int slot);
        void SearchCurrentLoot();
        void TakeCurrentLoot(string instanceId);
    }

    public interface ITacticalHudHost
    {
        CombatState CurrentState { get; }
        string SelectedAction { get; }
        bool IsDeveloperCombatActive { get; }
        void ActivateInventoryQuickbar(int slot);
        void ApplyHudBuild(int build);
        void EndHeroTurn();
        void SelectHudAction(string action);
        void TacticalRestartDeveloperCombat();
    }

    public interface IDeveloperConsoleHost
    {
        bool IsDeveloperCombatActive { get; }
        bool IsTrainingRangeActive { get; }
        TrainingRangeSession TrainingRange { get; }
        bool CanUseDeveloperMapAdvance { get; }
        string DeveloperMapAdvanceSummary { get; }
        void BrowseTrainingRangeAbility(string abilityId);
        void DeveloperAdvanceToFinale();
        void DeveloperForceWinCurrentCombat();
        /// <summary>开发线一键过关：战斗内也生效（先按胜利收束当前战斗，再结算地图节点与掉落）。</summary>
        void DeveloperForceWinEverywhere();
        void DeveloperSettleCurrentReward();
        TrainingRangeExecutionReport ExecuteTrainingRangeCurrent();
        void ForceCurrentOutcome(bool victory);
        void PrepareTrainingRangeCurrent();
        TrainingRangePreviewReport PreviewTrainingRangeCurrent();
        void ReturnToDeveloperMenu();
        void SelectTrainingRangeAbility(string abilityId);
        void ShiftTrainingRangePage(int delta);
        void StartTrainingRange();
        void TacticalRestartDeveloperCombat();
    }

    public interface ICombatEntrySequenceHost : IUiPreferenceHost
    {
        CombatState CurrentState { get; }
        BattlefieldViewport BattlefieldViewport { get; }
        bool IsDeveloperCombatActive { get; }
        /// <summary>Player-facing mission goal, shown in full at entry and docked afterwards.</summary>
        string CombatObjectiveSummary { get; }
        /// <summary>Formal battlefield sprite for a unit; reuses the live asset lookup.</summary>
        Texture2D UnitPortrait(UnitState unit);
        /// <summary>Called once the entry cinematic has handed control back to the battle.</summary>
        void CompleteCombatEntrySequence();
    }

    public interface ICombatPresentationCompositionHost : ICombatFeedbackHost, ICombatHudHost,
        IRogueliteUiHost, IStartupPresentationHost, IInteractionPresentationHost,
        ISettlementPresentationHost, IInventoryPresentationHost, IDeveloperConsoleHost, IBattlefieldViewHost,
        ICombatEntrySequenceHost
    {
    }

    public interface IBattlefieldViewHost
    {
        CombatState CurrentState { get; }
        BattlefieldViewport BattlefieldViewport { get; }
        bool IsBattlefieldVisible { get; }
        bool IsInteractionModalOpen { get; }
        string CurrentLevelId { get; }
        BattlefieldCellPresentation PresentBattlefieldCell(GridPosition position);
        void SubmitBattlefieldCell(GridPosition position, bool inspection);
        bool CanQuickMoveTo(GridPosition position);
        bool ShouldDeferPrimaryClickForQuickMove(GridPosition position);
        void SubmitBattlefieldQuickMove(GridPosition position);
        IReadOnlyList<BattlefieldContextAction> ContextActionsAt(GridPosition position);
        void SubmitBattlefieldContextAction(GridPosition position, string actionId);
        void NotifyBattlefieldContextUnavailable(GridPosition position);
        void SetBattlefieldContextMenuOpen(bool open);
        void PreviewBattlefieldContextAction(GridPosition position, string actionId);
        void ClearBattlefieldContextPreview();
        void SetBattlefieldPreviewPosition(GridPosition position, bool active);
        void FocusBattlefieldOnHero();
    }
}
