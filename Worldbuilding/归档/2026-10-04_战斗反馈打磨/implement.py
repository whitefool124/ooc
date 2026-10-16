from pathlib import Path
root=Path(__file__).resolve().parents[3]
def edit(path, old, new):
    p=root/path
    text=p.read_text(encoding='utf-8-sig')
    if text.count(old)!=1: raise RuntimeError(f'{path}: expected one match, got {text.count(old)}: {old[:80]}')
    p.write_text(text.replace(old,new),encoding='utf-8',newline='\n')
P='UnityProject/Assets/Game/Runtime/Presentation/'
C='UnityProject/Assets/Game/Runtime/Campaign/'
edit(P+'PresentationHostContracts.cs','public interface IBattlefieldViewHost\n','public interface IBattlefieldViewHost : IUiPreferenceHost\n')
edit(P+'PresentationHostContracts.cs','void SubmitBattlefieldCell(GridPosition position, bool inspection);','void SubmitBattlefieldCell(GridPosition position, bool inspection);\n        void PreviewBattlefieldCell(GridPosition position);')
edit(P+'CombatSelectionController.cs','public bool HasPreviewPosition { get; private set; }','public bool HasPointerSelection { get; private set; }\n        public GridPosition PointerPosition { get; private set; }\n        public void SelectPointerPosition(GridPosition position) { PointerPosition = position; HasPointerSelection = true; SetPreviewPosition(position); }\n        public bool HasPreviewPosition { get; private set; }')
# Both Reset and SelectAction clear the locked pointer preview.
p=root/(P+'CombatSelectionController.cs'); t=p.read_text(encoding="utf-8"); t=t.replace('HasPreviewPosition = false;\n            ClearMenuPreview();','HasPreviewPosition = false;\n            HasPointerSelection = false;\n            ClearMenuPreview();'); t=t.replace('public bool ClearTarget() => SetKnownTarget(null);','public bool ClearTarget() { HasPointerSelection = false; return SetKnownTarget(null); }'); p.write_text(t,encoding='utf-8')
edit(P+'CombatPrototypeBootstrap.cs','else if (IsHeroPosition(position)) SelectHudAction("移动");','else if (IsHeroPosition(position) && !IsSelectedSelfAction()) SelectHudAction("移动");')
edit(P+'CombatPrototypeBootstrap.cs','private bool IsHeroPosition(GridPosition position)','public void PreviewBattlefieldCell(GridPosition position)\n        {\n            if (state == null || IsInteractionModalOpen) return;\n            if (IsHeroPosition(position) && !IsSelectedSelfAction()) { SelectHudAction("移动"); return; }\n            HandleInspectionClick(position);\n            selection.SelectPointerPosition(position);\n            MarkPresentation(UiPresentationArea.Combat);\n        }\n\n        private bool IsSelectedSelfAction()\n        {\n            int slot = RogueSkillSlot(selection.Action);\n            if (slot < 0 || state == null) return false;\n            var rogue = state.RogueSpells?.DefinitionAtSlot(slot);\n            if (rogue != null) return rogue.Targeting == "self";\n            var skill = slot == 0 ? state.GetUnit("hero")?.SkillOne : slot == 1 ? state.GetUnit("hero")?.SkillTwo : null;\n            return skill?.TargetRule == SkillTargetRule.Self;\n        }\n\n        private bool IsHeroPosition(GridPosition position)')
edit(P+'CombatPrototypeBootstrap.cs',': selection.HasPreviewPosition ? selection.PreviewPosition : (GridPosition?)null;',': selection.HasPreviewPosition ? selection.PreviewPosition : selection.HasPointerSelection ? selection.PointerPosition : (GridPosition?)null;')
edit(P+'CombatPrototypeBootstrap.cs','public void PublishUiVisual(UiVisualEvent visualEvent) => uiVisualEvents.Publish(visualEvent);','public void PublishUiVisual(UiVisualEvent visualEvent)\n        {\n            if (visualEvent.Kind == UiVisualEventKind.CombatCommandRejected) battlefieldView?.ReactToCommand(false);\n            uiVisualEvents.Publish(visualEvent);\n        }')
edit(P+'CombatPrototypeBootstrap.cs','if (!string.IsNullOrEmpty(result.ActionResult)) state.AddLog(result.ActionResult);','if (command.UnitId == "hero") battlefieldView?.ReactToCommand(true);\n            if (!string.IsNullOrEmpty(result.ActionResult)) state.AddLog(result.ActionResult);')
edit(P+'CombatBattlefieldCellPresenter.cs','selection.PreviewPosition : (GridPosition?)null','selection.PreviewPosition : selection.HasPointerSelection ? selection.PointerPosition : (GridPosition?)null')
edit(P+'FormalBattlefieldView.cs','private bool submitPendingPrimaryOnTimeout;','private string pendingPrimaryAction;\n        private int pendingPrimaryTurn;\n        private float pendingPrimaryAt;\n        private CombatState observedState;\n        private readonly CombatIntentChangeTracker intentChanges = new CombatIntentChangeTracker();\n        private readonly Dictionary<GridPosition, float> intentCellChanges = new Dictionary<GridPosition, float>();')
edit(P+'FormalBattlefieldView.cs','EnsureCells(state.Map.Width, state.Map.Height);','if (observedState != state)\n            {\n                observedState = state; intentChanges.Clear(); intentCellChanges.Clear(); CancelPendingPrimaryClick();\n            }\n            if (hasPendingPrimaryPosition && (pendingPrimaryAction != host.SelectedAction || pendingPrimaryTurn != state.TurnSequence || host.IsInteractionModalOpen)) CancelPendingPrimaryClick();\n            EnsureCells(state.Map.Width, state.Map.Height);')
edit(P+'FormalBattlefieldView.cs','HashSet<GridPosition> intentDestinations = CollectIntentDestinations(intents);','foreach (var model in presented.Values)\n                if (model?.Unit != null && !model.Unit.IsHero && intentChanges.Observe(model.Unit.Id, model.Intent, Time.unscaledTime))\n                {\n                    foreach (var position in model.Intent.AffectedCells.Concat(model.Intent.Route)) intentCellChanges[position] = Time.unscaledTime;\n                    if (model.Intent.HasDestination) intentCellChanges[model.Intent.Destination] = Time.unscaledTime;\n                }\n            HashSet<GridPosition> intentDestinations = CollectIntentDestinations(intents);')
edit(P+'FormalBattlefieldView.cs','playerEdges, isIntentRoute, isIntentDestination);','playerEdges, isIntentRoute, isIntentDestination, model.SelectionOverlayTexture != null,\n                host.UiPreferences.AnimationIntensity,\n                intentCellChanges.TryGetValue(model.Position, out float at) ? Time.unscaledTime - at : CombatIntentChangeTracker.Duration);')
start='''            bool canQuickMove = host.CanQuickMoveTo(position);'''
p=root/(P+'FormalBattlefieldView.cs'); t=p.read_text(encoding="utf-8"); a=t.index(start,t.index('private void SubmitPrimaryCell')); z=t.index('        public static bool IsConfirmedQuickMove',a)
t=t[:a]+'''            bool same = hasPendingPrimaryPosition && pendingPrimaryPosition == position &&
                pendingPrimaryAction == host.SelectedAction && pendingPrimaryTurn == host.CurrentState.TurnSequence;
            bool moving = host.SelectedAction == "移动";
            if (same && (!moving || Time.unscaledTime - pendingPrimaryAt <= DoubleClickWindowSeconds))
            {
                CancelPendingPrimaryClick();
                if (moving) host.SubmitBattlefieldQuickMove(position);
                else host.SubmitBattlefieldCell(position, false);
                return;
            }
            CancelPendingPrimaryClick();
            host.PreviewBattlefieldCell(position);
            pendingPrimaryPosition = position;
            pendingPrimaryAction = host.SelectedAction;
            pendingPrimaryTurn = host.CurrentState.TurnSequence;
            pendingPrimaryAt = Time.unscaledTime;
            hasPendingPrimaryPosition = true;
        }

        public void ReactToCommand(bool accepted)
        {
            GridPosition position = hasHoverPosition ? hoverPosition : pendingPrimaryPosition;
            if (cells.TryGetValue(position, out CellView cell)) cell.RangeGraphic.ShowResult(accepted);
            CancelPendingPrimaryClick();
        }

'''+t[z:]
a=t.index('        private IEnumerator SubmitPendingPrimaryClick()'); z=t.index('        private void CancelPendingPrimaryClick()',a);t=t[:a]+t[z:]
t=t.replace('            submitPendingPrimaryOnTimeout = false;\n',''); t=t.replace('private static void RefreshIntent(CellView cell,','private void RefreshIntent(CellView cell,')
t=t.replace('            cell.IntentIcon.color = Color.white;','''            float age = intentChanges.Age(cell.PresentedUnitId, Time.unscaledTime);
            bool changing = age < CombatIntentChangeTracker.Duration;
            bool animate = host.UiPreferences.AnimationIntensity > .01f;
            float blink = changing && animate ? (Mathf.FloorToInt(age / .12f) % 2 == 0 ? 1f : .25f) : 0f;
            cell.IntentIcon.color = Color.Lerp(Color.white, new Color(1f, .72f, .22f), blink * .7f);
            if (changing && animate && age < .30f) cell.IntentIcon.rectTransform.anchoredPosition += Vector2.up * (Mathf.FloorToInt(age / .075f) % 2 == 0 ? 2f : 0f);
            Image signal = cell.IntentRoot.GetComponent<Image>();
            signal.color = changing ? new Color(1f, .69f, .2f, animate ? blink * .22f : .14f) : Color.clear;''')
t=t.replace('            host?.StopDust?.Clear();\n            CancelPendingPrimaryClick();','            host?.StopDust?.Clear();\n            intentChanges.Clear(); intentCellChanges.Clear(); observedState = null;\n            CancelPendingPrimaryClick();')
t=t.replace('                host.StopDust?.Clear();\n                HideTooltip();','                host.StopDust?.Clear();\n                intentChanges.Clear(); intentCellChanges.Clear(); observedState = null;\n                HideTooltip();')
p.write_text(t,encoding='utf-8')
edit(C+'BattlefieldPresentationAdapter.cs','''            // Restores this board's own opening scale, not a global constant.
            cellSize = defaultCellSize;
            ClampToViewport();''','''            // Overview is an integer native-pixel scale and includes the complete logical board.
            float fit = Math.Min(viewport.Width / mapWidth, viewport.Height / mapHeight);
            cellSize = Math.Max(32f, Math.Min(defaultCellSize, (float)Math.Floor(fit / 32f) * 32f));
            boardX = viewport.X + (viewport.Width - BoardWidth) * .5f;
            boardY = viewport.Y + (viewport.Height - BoardHeight) * .5f;
            ClampToViewport();''')
edit(P+'EnemyTurnSequence.cs','public const float FocusSeconds = .65f;','public const float FocusSeconds = .28f;')
edit(P+'EnemyTurnSequence.cs','public const float ActorGapSeconds = .30f;','public const float ActorGapSeconds = .10f;')
edit(P+'EnemyTurnSequence.cs','private const float MoveResultSeconds = .70f;','private const float MoveResultSeconds = .18f;')
edit(P+'EnemyTurnSequence.cs','private const float AttackResultSeconds = .90f;','private const float AttackResultSeconds = .28f;')
edit(P+'EnemyTurnSequence.cs','private const float SkillResultSeconds = 1.00f;','private const float SkillResultSeconds = .38f;')
edit(P+'EnemyTurnSequence.cs','private const float DefaultResultSeconds = .75f;','private const float DefaultResultSeconds = .18f;')
edit(P+'EnemyTurnSequence.cs','public EnemyTurnSequenceSignal Advance(float now)','public EnemyTurnSequenceSignal Advance(float now, bool animationsEnabled = true)')
edit(P+'EnemyTurnSequence.cs','if (!IsRunning || now < Deadline) return EnemyTurnSequenceSignal.None;','if (!IsRunning || animationsEnabled && now < Deadline) return EnemyTurnSequenceSignal.None;')
edit(P+'EnemyTurnCoordinator.cs','Func<UnitState, CombatCommand> commandFactory)','Func<UnitState, CombatCommand> commandFactory, bool animationsEnabled = true)')
p=root/(P+'EnemyTurnCoordinator.cs');t=p.read_text(encoding="utf-8").replace('sequence.Advance(now)','sequence.Advance(now, animationsEnabled)');p.write_text(t,encoding='utf-8')
edit(P+'CombatPrototypeBootstrap.cs','enemyTurn.Advance(enemy, now, unit => BuildEnemyCommand(unit, hero))','enemyTurn.Advance(enemy, now, unit => BuildEnemyCommand(unit, hero), uiPreferences.AnimationIntensity > .01f)')
edit(P+'FormalCombatHud.cs','private Text actionPointBadgeValue;','private Text actionPointBadgeValue;\n        private Text commandStatus;')
edit(P+'FormalCombatHud.cs','BuildHeaderResources(top.transform);','BuildHeaderResources(top.transform);\n            commandStatus = Label("当前战斗指令", top.transform, new Vector2(580, -8), new Vector2(596, 40), 20, FormalUiTheme.Ink, TextAnchor.MiddleLeft);\n            commandStatus.raycastTarget = false;')
edit(P+'FormalCombatHud.cs','int actionVersion = (bootstrap as ICombatActionPresentationHost)?.CombatActionPresentationVersion ?? 0;','''UnitState active = bootstrap.CurrentState.GetUnit(bootstrap.CurrentState.ActiveUnitId);
            string actionName = bootstrap.SelectedAction;
            int statusSlot = SkillSlot(actionName);
            if (statusSlot >= 0) actionName = bootstrap.CurrentState.RogueSpells?.DefinitionAtSlot(statusSlot)?.DisplayName ?? actionName;
            string status = active?.IsHero == true ? ((bootstrap as ICombatActionPresentationHost)?.IsCombatActionPlaying == true ? "正在结算" : "选点后再击确认") : "敌方行动";
            commandStatus.text = "第 " + bootstrap.CurrentState.TurnSequence + " 回合　" + actionName + "　" + status;
            int actionVersion = (bootstrap as ICombatActionPresentationHost)?.CombatActionPresentationVersion ?? 0;''')
