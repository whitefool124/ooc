using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    // A single contextual cue; the full index is opened deliberately, never stacked over play.
    public sealed class AcademyTutorialPresenter : MonoBehaviour
    {
        private CombatPrototypeBootstrap host;
        private AcademyTutorialCatalog catalog;
        private Canvas canvas;
        private GameObject cue, review, detail;
        private Text cueTitle, cuePrompt, reviewTitle, detailTitle, detailText;
        private Button practiceButton;
        private AcademyTutorialLesson active;
        private string context = "", dismissedContext = "";
        private string selectedId = "";
        private string lastObservedView = "";
        private int page, slot = -2;
        private float nextCueAt;
        private string safeWindowKey = "";
        private readonly HashSet<string> offeredInWindow = new HashSet<string>();
        private AcademyTutorialProgress progress;
        private string confirmationFingerprint, confirmationKind;
        private readonly AcademyTutorialObservation observation = new AcademyTutorialObservation();
        public bool IsReviewOpen => review != null && review.activeSelf;
        public string ActiveLessonId => active?.id ?? "";
        public AcademyTutorialCatalog Catalog => catalog;
        public AcademyTutorialProgress Progress { get { Reload(); return progress; } }

        public void Bind(CombatPrototypeBootstrap source)
        {
            host = source;
            var resource = Resources.Load<TextAsset>("Config/AcademyTutorialLessons");
            if (resource == null) { Debug.LogError("Missing academy teaching catalog"); enabled = false; return; }
            catalog = JsonUtility.FromJson<AcademyTutorialCatalog>(resource.text);
            host.UiVisualEvents.Published += OnVisual;
            Reload(); EnsureUi();
        }
        private void Reload()
        {
            int current = FirstExperienceSaveRouting.ActiveSlot;
            if (progress == null || slot != current) { slot = current; progress = AcademyTutorialStore.Load(); }
            else progress = AcademyTutorialStore.Load();
        }
        public void Evidence(string id, string fact)
        {
            if (catalog == null) return;
            var lesson = catalog.lessons.FirstOrDefault(l => l.id == id && l.enabled);
            if (lesson == null || !lesson.evidence.Contains(fact)) return;
            Reload();
            bool wasComplete = progress.IsComplete(lesson);
            if (!progress.Observe(id, fact)) return;
            AcademyTutorialStore.Save(progress);
            if (!wasComplete && progress.IsComplete(lesson))
            {
                nextCueAt = Time.unscaledTime + 12f;
                if (active?.id == id) { cue.SetActive(false); active = null; }
            }
        }
        public void Preview(string id)
        { Reload(); progress.Get(id).previewed = true; AcademyTutorialStore.Save(progress); }

        private void Update()
        {
            if (host == null || catalog == null) return;
            if (IsReviewOpen) return;
            if (Time.unscaledTime < nextCueAt) { cue.SetActive(false); return; }
            nextCueAt = Time.unscaledTime + .4f;
            observation.Tick(host.CurrentMapRun, host.CurrentState, host.IsDeveloperCombatActive, Evidence);
            Reload();
            bool available = slot >= 0 && host.CurrentMapRun != null && progress.enabled &&
                host.GetComponent<FirstExperiencePrototypeController>()?.enabled != true &&
                !host.IsCombatEntryBlocking && !host.IsCombatActionPlaying && !host.IsInteractionModalOpen &&
                string.IsNullOrEmpty(host.GetComponent<CombatTutorialPresenter>()?.ActiveId);
            if (!available) { cue.SetActive(false); return; }
            context = CurrentContext();
            string window = ContextKey();
            if (window != safeWindowKey) { safeWindowKey = window; offeredInWindow.Clear(); active = null; }
            if (ContextKey() == dismissedContext) { cue.SetActive(false); return; }
            var lesson = active != null && !progress.IsComplete(active) && FitsContext(active, context) ? active :
                catalog.lessons.Where(l => l.enabled && !progress.IsComplete(l) && FitsContext(l, context) && !offeredInWindow.Contains(l.id))
                    .OrderBy(l => progress.Get(l.id).shown).ThenBy(l => l.phase).FirstOrDefault();
            if (offeredInWindow.Count >= 2 && active == null) lesson = null;
            if (lesson == null) { cue.SetActive(false); return; }
            if (active?.id != lesson.id)
            {
                active = lesson; progress.Get(lesson.id).shown = true; AcademyTutorialStore.Save(progress);
                offeredInWindow.Add(lesson.id);
                cueTitle.text = lesson.title;
                cuePrompt.text = lesson.prompt;
            }
            cue.SetActive(true);
        }

        private string CurrentContext()
        {
            if (host.IsCombatOutcomeVisible) return "result";
            if (host.IsDeveloperCombatActive) return "combat:" + host.SelectedAction;
            var ui = host.GetComponent<FormalRogueliteUi>();
            return ui == null ? "map" : ui.TeachingContext;
        }
        private string ContextKey() => context + "|" + host.CurrentMapRun?.CurrentNodeId + "|" + host.CurrentMapRun?.CompletedNodes.Count;
        private bool FitsContext(AcademyTutorialLesson lesson, string value)
        {
            string id = lesson.id;
            if (value == "settings") return id == "A01" || id == "A02";
            if (value == "gift") return id.StartsWith("G");
            if (value == "archive" || value == "encyclopedia") return id == "A06";
            if (value == "equipment") return id.StartsWith("I") && id != "I06" && id != "I07" && id != "I08";
            if (value == "spells") return id == "G02" || id == "I07" || id == "I08";
            if (value == "reward") return id.StartsWith("R");
            if (value == "result") return id.StartsWith("R") || id == "F01" || id == "F02" || id == "F04" || id == "F05";
            if (value == "workshop") return id == "S01" || id == "S02" || id == "S06";
            if (value == "medical") return id == "S03" || id == "S04";
            if (value == "shop") return id == "S05";
            if (value == "event") return id == "M04" || id == "M05";
            if (value == "briefing") return id == "M02" || id == "M03";
            if (value.StartsWith("combat:"))
            {
                if (id == "C04") return value.EndsWith("移动");
                if (id == "C05") return value.EndsWith("攻击");
                if (id == "C06") return value.Contains("技能");
                // Advanced rules appear in separate safe practice windows, not as a wall of combat prompts.
                return id == "C01" || id == "C02" || id == "C03" || id == "C10" || id == "C11" || id == "C12";
            }
            if (value != "map") return false;
            if (id == "M01" || id == "A06" || id == "M06") return true;
            // Only safe map windows offer missed rules. No combat interruption or compulsory purchase.
            int battles = host.CurrentMapRun.CompletedNodes.Count(node => host.CurrentMapRun.MapNode(node).IsCombat);
            int phase = battles == 0 ? 2 : battles == 1 ? 5 : battles == 2 ? 7 : battles == 3 ? 9 : 12;
            if (host.CurrentMapRun.CanChallengeAcademyFinale) phase = 13;
            if (host.CurrentMapRun.IsComplete) phase = 15;
            if (phase > progress.highestPhase) { progress.highestPhase = phase; AcademyTutorialStore.Save(progress); }
            phase = Math.Max(phase, progress.highestPhase);
            return int.TryParse((lesson.phase ?? "").TrimStart('P'), out int first) && first <= phase &&
                (AcademyTutorialModelExercise.CanPrepare(id) || AcademyTutorialExercise.CanPrepare(id));
        }
        public void ObserveView(string view)
        {
            if (view == "settings") { Evidence("A02", "settings-preview"); }
            if (view == "briefing") Evidence("M02", "briefing-open");
            if (view == "archive") Evidence("A06", "archive-open");
            if (view == "encyclopedia") Evidence("A06", "encyclopedia-open");
            if (view == "return")
            {
                if (lastObservedView == "archive" || lastObservedView == "encyclopedia") Evidence("A06", "return");
                if (lastObservedView == "settings") Evidence("A02", "settings-return");
                if (lastObservedView == "briefing") Evidence("M02", "briefing-cancel");
                lastObservedView = "";
            }
            else lastObservedView = view;
        }
        private void OnVisual(UiVisualEvent visual)
        {
            switch (visual.Kind)
            {
                case UiVisualEventKind.ConfirmationOpened:
                    confirmationKind = visual.Subject;
                    if (host.CurrentMapRun != null)
                        confirmationFingerprint = AcademyTutorialObservation.Fingerprint(host.CurrentMapRun);
                    Evidence("A07", "consequence-preview");
                    if (visual.Subject == UiConfirmationKind.LeaveCombat.ToString()) Evidence("A09", "abandon-preview");
                    break;
                case UiVisualEventKind.MapNodeSelected: Evidence("M01", "node-preview"); break;
                case UiVisualEventKind.MapLocationChanged: Evidence("M01", "node-entered"); break;
                case UiVisualEventKind.BriefingOpened: Evidence("M02", "briefing-open"); break;
                case UiVisualEventKind.CombatCommandRejected: Evidence("C12", "illegal-reason"); break;
                case UiVisualEventKind.CombatRangeRevealed:
                    if (visual.Subject == "移动") Evidence("C04", "move-preview");
                    if (visual.Subject == "攻击") Evidence("C05", "weapon-preview");
                    if (visual.Subject.StartsWith("技能")) Evidence("C06", "spell-preview");
                    if (host.CurrentActionPreview?.ValidCellCount > 0) Evidence("C12", "legal-preview"); break;
                case UiVisualEventKind.SettlementOpened: Evidence("R01", "settlement-view"); break;
                case UiVisualEventKind.RewardClaimed: Evidence("R02", "reward-claim"); break;
                case UiVisualEventKind.ServiceCommitted:
                    if (visual.Subject == "forge") Evidence("S01", "forge-commit");
                    if (visual.Subject == "specialize") Evidence("S02", "specialize-commit");
                    if (visual.Subject == "purchase") Evidence("S05", "purchase");
                    if (visual.Subject == "inventory-rotate") Evidence("I02", "rotate");
                    if (visual.Subject == "inventory-link") Evidence("I04", "link");
                    break;
            }
        }
        public void ObserveConfirmationCancelled()
        {
            if (host.CurrentMapRun == null || string.IsNullOrEmpty(confirmationFingerprint)) return;
            string after = AcademyTutorialObservation.Fingerprint(host.CurrentMapRun);
            if (after != confirmationFingerprint) return;
            Evidence("A07", "cancel-unchanged");
            if (confirmationKind == UiConfirmationKind.AbandonReward.ToString()) Evidence("R04", "leave-cancel");
            if (confirmationKind == UiConfirmationKind.ReplaceExistingRun.ToString()) Evidence("F05", "cancel-preserved");
        }
        public void ObserveCommand(CombatCommand command, int apBefore, int manaBefore)
        {
            if (command.UnitId != "hero") return;
            var hero = host.CurrentState?.GetUnit("hero");
            if (hero == null) return;
            if (hero.ActionPoints < apBefore) Evidence("C03", "action-paid");
            if (command.Type == CombatCommandType.Move) Evidence("C04", "move-accepted");
            if (command.Type == CombatCommandType.Attack) Evidence("C05", "weapon-accepted");
            if (command.Type == CombatCommandType.UseSkill || command.Type == CombatCommandType.Cast) Evidence("C06", "spell-accepted");
            if (command.Type == CombatCommandType.EndTurn) Evidence("C03", "end-turn");
            if (command.Type == CombatCommandType.OpenInventory && hero.ActionPoints < apBefore) Evidence("I06", "inventory-ap");
            if (command.Type == CombatCommandType.SearchLoot && hero.ActionPoints < apBefore) { Evidence("E09", "search-paid"); Evidence("E09", "reveal"); }
            if (command.Type == CombatCommandType.TakeLoot && hero.ActionPoints == apBefore) Evidence("E09", "take-no-charge");
        }

        public void OpenReview()
        {
            if (catalog == null) return;
            Reload(); EnsureUi(); cue.SetActive(false); review.SetActive(true); RebuildIndex();
        }
        public void CloseReview()
        { review?.SetActive(false); nextCueAt = Mathf.Max(nextCueAt, Time.unscaledTime + 8); }
        public void DeferPractice(string id)
        {
            if (!string.IsNullOrEmpty(id)) offeredInWindow.Add(id);
            active = null; cue?.SetActive(false); nextCueAt = Time.unscaledTime + 30;
        }
        private void ToggleTeaching()
        {
            Reload(); bool value = !progress.enabled;
            AcademyTutorialStore.SetEnabled(value);
            host.GetComponent<FirstExperiencePrototypeController>()?.ChooseTutorial(value);
            Reload(); RebuildIndex();
        }
        private void EnsureUi()
        {
            if (canvas != null) return;
            canvas = FormalUiKit.CanvasRoot("学院教学", UiLayoutContract.InteractionSortingOrder + 50);
            var parent = FormalUiKit.ContentParent(canvas.transform);
            cue = FormalUiKit.AnchoredPanel("当前教学提示", parent, Vector2.up, Vector2.up, new Vector2(690, -112), new Vector2(540, 208), ArchiveUiStyle.LightPaper);
            ArchiveUiStyle.PaperPanel(cue, ArchiveUiStyle.LightPaper, true);
            cueTitle = FormalUiKit.Label("教学项目", "", cue.transform, new Vector2(20, -16), new Vector2(500, 42), 24, ArchiveUiStyle.Ink, TextAnchor.MiddleLeft);
            cuePrompt = FormalUiKit.Label("一步提示", "", cue.transform, new Vector2(20, -64), new Vector2(500, 66), 24, ArchiveUiStyle.Ink, TextAnchor.UpperLeft);
            Button("查看操作", cue.transform, new Vector2(20, -144), new Vector2(240, 48), () => { selectedId = active?.id ?? ""; OpenReview(); });
            Button("稍后", cue.transform, new Vector2(280, -144), new Vector2(240, 48), () => { dismissedContext = ContextKey(); active = null; cue.SetActive(false); });
            cue.SetActive(false);
            review = FormalUiKit.AnchoredPanel("教学册", parent, Vector2.up, Vector2.up, Vector2.zero, new Vector2(1920, 1080), FormalUiTheme.Panel);
            reviewTitle = FormalUiKit.Label("教学册标题", "", review.transform, new Vector2(48, -28), new Vector2(1450, 64), 32, ArchiveUiStyle.Ink, TextAnchor.MiddleLeft);
            Button("返回", review.transform, new Vector2(1610, -32), new Vector2(260, 56), CloseReview);
            detail = FormalUiKit.AnchoredPanel("单项教学", review.transform, Vector2.up, Vector2.up, new Vector2(750, -120), new Vector2(1120, 826), ArchiveUiStyle.LightPaper);
            ArchiveUiStyle.PaperPanel(detail, ArchiveUiStyle.LightPaper, true);
            detailTitle = FormalUiKit.Label("项目标题", "", detail.transform, new Vector2(32, -24), new Vector2(1056, 72), 28, ArchiveUiStyle.Ink, TextAnchor.UpperLeft);
            detailText = FormalUiKit.Label("操作说明", "", detail.transform, new Vector2(32, -120), new Vector2(1056, 530), 24, ArchiveUiStyle.Ink, TextAnchor.UpperLeft);
            practiceButton = Button("开始隔离演练", detail.transform, new Vector2(32, -712), new Vector2(500, 64), BeginPractice);
            Button("暂停或恢复提示", review.transform, new Vector2(48, -968), new Vector2(400, 64), ToggleTeaching);
            Button("上一页", review.transform, new Vector2(480, -968), new Vector2(200, 64), () => { page = Math.Max(0, page - 1); RebuildIndex(); });
            Button("下一页", review.transform, new Vector2(710, -968), new Vector2(200, 64), () => { page = Math.Min((catalog.lessons.Count(l => l.enabled) - 1) / 10, page + 1); RebuildIndex(); });
            review.SetActive(false);
        }
        private void RebuildIndex()
        {
            foreach (Transform child in review.transform)
                if (child.name.StartsWith("教学索引_")) Destroy(child.gameObject);
            var enabledLessons = catalog.lessons.Where(l => l.enabled).ToArray();
            reviewTitle.text = "教学册　已核对 " + progress.Completed(enabledLessons) + " 项　总计 " + enabledLessons.Length + " 项　提示" + (progress.enabled ? "开启" : "暂停");
            int index = 0;
            foreach (var lesson in enabledLessons.Skip(page * 10).Take(10))
            {
                var record = progress.Find(lesson.id);
                string state = progress.IsComplete(lesson) ? "已核对" : record?.evidence.Count > 0 ? "进行中" : record?.shown == true ? "已提示" : "待接触";
                var button = Button(state + "　" + lesson.title, review.transform, new Vector2(48, -120 - index++ * 80), new Vector2(660, 70), () => { selectedId = lesson.id; ShowDetail(); });
                button.name = "教学索引_" + lesson.id;
            }
            if (string.IsNullOrEmpty(selectedId)) selectedId = enabledLessons[page * 10].id;
            ShowDetail();
        }
        private void ShowDetail()
        {
            var lesson = catalog.lessons.First(l => l.id == selectedId);
            detailTitle.text = lesson.title;
            detailText.text = lesson.prompt + "\n\n" + lesson.goal;
            if (lesson.id == "C11")
            { detailText.text += "\n\n右键：位置菜单\nT：开始或取消键盘选格\n方向键：移动选格　Enter：确认　Esc：取消"; Evidence("C11", "alternate-entry-view"); }
            var record = progress.Find(lesson.id);
            int checkedFacts = record == null ? 0 : lesson.evidence.Count(record.evidence.Contains);
            detailText.text += "\n\n已核对 " + checkedFacts + " 项作用　总计 " + lesson.evidence.Length + " 项";
            Preview(lesson.id);
            practiceButton.gameObject.SetActive(AcademyTutorialPracticePresenter.CanPrepare(lesson.id));
            practiceButton.GetComponentInChildren<Text>().text = progress.IsComplete(lesson) ? "重新演练" : checkedFacts > 0 ? "继续未核对的演练" : "开始隔离演练";
        }
        private void BeginPractice()
        {
            var practice = host.GetComponent<AcademyTutorialPracticePresenter>();
            if (practice == null) practice = host.gameObject.AddComponent<AcademyTutorialPracticePresenter>();
            practice.Open(this, selectedId);
        }
        private static Button Button(string text, Transform parent, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction click)
        { var button = FormalUiKit.Button(text, text, parent, position, size, FormalUiTheme.Interactive); button.onClick.AddListener(click); return button; }
        private void OnDestroy()
        { if (host != null) host.UiVisualEvents.Published -= OnVisual; if (canvas != null) Destroy(canvas.gameObject); }
    }
}
