using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    // Dynamic spotlights follow the production HUD and viewport, rather than a duplicate combat UI.
    public sealed class CombatTutorialPresenter : MonoBehaviour
    {
        private CombatPrototypeBootstrap host;
        private FirstExperiencePrototypeController opening;
        private FirstRunExperienceState progress;
        private Canvas canvas;
        private GameObject root, card;
        private Image blocker;
        private readonly Image[] shade = new Image[4];
        private readonly Image[] edges = new Image[4];
        private Text title, body;
        private Button next, cancel, skip;
        private string activeId = string.Empty, oldInspection;
        private bool waiting, moveDeferred;
        private string displayedAction;

        public string ActiveId => activeId;
        public bool BlocksInput => !string.IsNullOrEmpty(activeId) && !waiting;
        public bool BlocksSimulation => !string.IsNullOrEmpty(activeId);
        public void Bind(CombatPrototypeBootstrap source, FirstExperiencePrototypeController flow)
        { host = source; opening = flow; }

        public void Tick()
        {
            var run = host == null ? null : host.CurrentMapRun;
            bool eligible = opening != null && !opening.enabled && opening.CombatTutorialEnabled &&
                run?.IsTutorialPhase == true && run.CurrentNodeId == "B1" && host.IsDeveloperCombatActive &&
                !host.IsCombatEntryBlocking && !host.IsCombatActionPlaying;
            if (!eligible) { Hide(); return; }
            var state = host.CurrentState;
            if (state == null || state.IsVictory || state.IsDefeat) { Hide(); return; }
            progress = run.FirstRunExperience;
            if (!string.IsNullOrEmpty(activeId)) return;
            if (host.IsInteractionModalOpen) return;
            string id = progress.PendingTutorialId;
            if (string.IsNullOrEmpty(id))
            {
                var preview = host.CurrentActionPreview;
                id = CombatTutorialPolicy.Next(progress.CompletedTutorialIds, state.ActiveUnitId == "hero",
                    host.SelectedAction ?? string.Empty, preview?.ValidCellCount ?? 0,
                    !string.IsNullOrEmpty(state.ActiveUnitId) && state.ActiveUnitId != "hero", moveDeferred);
                if (string.IsNullOrEmpty(id)) return;
                progress.PendingTutorialId = id;
                progress.PendingTutorialAction = host.SelectedAction ?? string.Empty;
                progress.TutorialActionReady = false;
                if (!host.SaveCombatTutorialProgress()) { progress.PendingTutorialId = string.Empty; return; }
            }
            Open(id, progress.TutorialActionReady);
        }

        public bool AllowActionSelection(string action)
        {
            if (string.IsNullOrEmpty(activeId)) { if (action == "移动") moveDeferred = false; return true; }
            if (!waiting) return false;
            return activeId == "B1-04" ? action == "移动" : action == "攻击" || action.StartsWith("技能");
        }
        public bool AllowCommand(CombatCommand command)
            => CombatTutorialPolicy.Allows(activeId, waiting, command.Type);

        public void OnAcceptedCommand(CombatCommand command)
        {
            if (progress == null || !CombatTutorialPolicy.Completes(activeId, waiting, command.Type)) return;
            progress.CompletedTutorialIds.Add(activeId);
            progress.PendingTutorialId = string.Empty;
            progress.TutorialActionReady = false;
            Hide();
        }

        // Cancelling a preview completes B1-05; cancelling movement never substitutes for moving.
        public bool CancelOperation()
        {
            if (string.IsNullOrEmpty(activeId)) return false;
            if (!waiting) return true;
            string id = activeId;
            if (id == "B1-05" && !Complete()) return true;
            if (id == "B1-04")
            {
                progress.PendingTutorialId = string.Empty;
                progress.TutorialActionReady = false;
                if (!host.SaveCombatTutorialProgress())
                { progress.PendingTutorialId = id; progress.TutorialActionReady = true; return true; }
                moveDeferred = true;
                Hide();
            }
            // The teaching button cancels an action, including the default move mode; it never requests abandonment.
            host.ResetCombatActionSelection();
            return true;
        }

        private void Continue()
        {
            if (CombatTutorialPolicy.IsOperation(activeId))
            {
                progress.TutorialActionReady = true;
                if (!host.SaveCombatTutorialProgress()) { progress.TutorialActionReady = false; return; }
                waiting = true;
                UpdateCopy();
                return;
            }
            Complete();
        }
        private bool Complete()
        {
            string id = activeId;
            progress.CompletedTutorialIds.Add(id);
            progress.PendingTutorialId = string.Empty;
            progress.TutorialActionReady = false;
            if (!host.SaveCombatTutorialProgress())
            { progress.CompletedTutorialIds.Remove(id); progress.PendingTutorialId = id; progress.TutorialActionReady = waiting; return false; }
            Hide();
            return true;
        }

        private void Skip()
        {
            opening.DisableCombatTutorial();
            if (progress != null)
            {
                progress.PendingTutorialId = string.Empty;
                progress.TutorialActionReady = false;
                host.SaveCombatTutorialProgress();
            }
            Hide();
        }

        private void Open(string id, bool ready)
        {
            EnsureUi();
            if (id == "B1-05" && !string.IsNullOrEmpty(progress.PendingTutorialAction))
                host.SelectHudAction(progress.PendingTutorialAction);
            activeId = id;
            waiting = ready;
            root.SetActive(true);
            if (id == "B1-02")
            {
                oldInspection = host.SelectedTargetId;
                var enemy = host.CurrentState.Units.Values.FirstOrDefault(unit => !unit.IsHero && unit.Health > 0);
                if (enemy != null) host.SetSelectedTargetForUi(enemy.Id);
            }
            UpdateCopy();
        }
        private void Hide()
        {
            if (activeId == "B1-02" && host != null) host.SetSelectedTargetForUi(oldInspection);
            activeId = string.Empty;
            waiting = false;
            if (root != null) root.SetActive(false);
        }

        private void UpdateCopy()
        {
            displayedAction = host.SelectedAction;
            int number = int.Parse(activeId.Substring(3));
            string[] titles = { "认识战场", "敌人的公开意图", "你的行动点", "试着完成一次移动", "检查行动预览", "轮到敌人行动" };
            string[] instructions = {
                "本场目标：击倒两名敌人。\n\n亮框内是战场。右侧显示人物与行动序列；底部是术式和结束回合。",
                "已为你展开一名敌人的详情。公开意图对应它的一个完整回合；先看目标与结果，再决定站位。",
                "亮框内是当前行动点。资源允许时，你可以连续移动或施法；结束回合会清空剩余行动点。",
                "双击亮起的可达格，完成一次移动。先看路径与费用；只有成功移动才会完成此步骤。",
                "先查看目标、费用与结果。确认后提交合法动作，也可以取消预览。",
                "亮框内的行动序列会显示轮到谁。敌人执行公开意图后结束回合；点击继续后再播放它的行动。"
            };
            title.text = "首战教学　" + number + " / 6\n" + titles[number - 1];
            body.text = instructions[number - 1];
            if (activeId == "B1-03") body.text += "\n\n当前行动点：" + host.CurrentState.GetUnit("hero").ActionPoints;
            if (activeId == "B1-02")
            {
                var enemy = host.CurrentState.GetUnit(host.SelectedTargetId);
                var intent = host.EnemyIntent(enemy);
                if (intent != null) body.text += "\n\n" + enemy.DisplayName + "：" + intent.ActionName + "\n" + intent.TargetSummary + "\n" + intent.ResultSummary;
            }
            if (activeId == "B1-05")
            {
                var preview = host.CurrentActionPreview;
                if (preview != null) body.text += "\n\n当前费用：" + preview.Cost + "\n" + preview.ExpectedResult;
            }
            next.gameObject.SetActive(!waiting);
            next.GetComponentInChildren<Text>().text = CombatTutorialPolicy.IsOperation(activeId) ? "开始操作" : "继续";
            cancel.gameObject.SetActive(waiting);
            blocker.raycastTarget = !waiting;
        }

        private void EnsureUi()
        {
            if (canvas != null) return;
            canvas = FormalUiKit.CanvasRoot("战斗逐步教学", UiLayoutContract.InteractionSortingOrder + 40);
            root = FormalUiKit.Create("首战教学层", FormalUiKit.ContentParent(canvas.transform));
            FormalUiKit.Stretch(root.AddComponent<RectTransform>());
            for (int i = 0; i < 4; i++)
            {
                shade[i] = Box("教学暗幕" + i, root.transform, Color.black);
                edges[i] = Box("教学高亮边" + i, root.transform, FormalUiTheme.Cyan);
                edges[i].raycastTarget = false;
            }
            blocker = Box("教学输入屏障", root.transform, Color.clear);
            SetRect(blocker.rectTransform, new Rect(0, 0, 1920, 1080));
            card = FormalUiKit.AnchoredPanel("教学说明卡", root.transform, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(420, 570), ArchiveUiStyle.LightPaper);
            ArchiveUiStyle.PaperPanel(card, ArchiveUiStyle.LightPaper, true);
            title = FormalUiKit.Label("教学标题", "", card.transform, new Vector2(24, -20), new Vector2(372, 92), 28, ArchiveUiStyle.Ink, TextAnchor.UpperLeft);
            body = FormalUiKit.Label("教学说明", "", card.transform, new Vector2(24, -124), new Vector2(372, 304), 24, ArchiveUiStyle.QuietInk, TextAnchor.UpperLeft);
            next = FormalUiKit.Button("教学继续", "继续", card.transform, new Vector2(24, -440), new Vector2(372, 54), FormalUiTheme.Cyan);
            next.onClick.AddListener(Continue);
            cancel = FormalUiKit.Button("教学取消行动", "取消行动", card.transform, new Vector2(24, -440), new Vector2(372, 54), FormalUiTheme.Interactive);
            cancel.onClick.AddListener(() => CancelOperation());
            skip = FormalUiKit.Button("跳过后续战斗教学", "跳过后续教学", card.transform, new Vector2(24, -510), new Vector2(372, 44), FormalUiTheme.Interactive);
            skip.onClick.AddListener(Skip);
            root.SetActive(false);
        }

        private void LateUpdate()
        {
            if (root == null || !root.activeSelf || host == null) return;
            if (activeId == "B1-05" && displayedAction != host.SelectedAction)
            {
                progress.PendingTutorialAction = host.SelectedAction ?? string.Empty;
                host.SaveCombatTutorialProgress();
                UpdateCopy();
            }
            Rect focus = FocusBounds();
            float left = Mathf.Clamp(focus.xMin, 0, 1920), right = Mathf.Clamp(focus.xMax, left, 1920);
            float top = Mathf.Clamp(focus.yMin, 0, 1080), bottom = Mathf.Clamp(focus.yMax, top, 1080);
            SetRect(shade[0].rectTransform, new Rect(0, 0, 1920, top));
            SetRect(shade[1].rectTransform, new Rect(0, bottom, 1920, 1080 - bottom));
            SetRect(shade[2].rectTransform, new Rect(0, top, left, bottom - top));
            SetRect(shade[3].rectTransform, new Rect(right, top, 1920 - right, bottom - top));
            for (int i = 0; i < 4; i++) { shade[i].color = new Color(0, 0, 0, waiting ? .20f : .58f); shade[i].raycastTarget = waiting; }
            const float width = 4;
            SetRect(edges[0].rectTransform, new Rect(left, top, right - left, width));
            SetRect(edges[1].rectTransform, new Rect(left, bottom - width, right - left, width));
            SetRect(edges[2].rectTransform, new Rect(left, top, width, bottom - top));
            SetRect(edges[3].rectTransform, new Rect(right - width, top, width, bottom - top));
            // Keep the explanation out of the highlighted console and out of the playable battlefield.
            card.GetComponent<RectTransform>().anchoredPosition = new Vector2(focus.center.x > 1440 && !waiting ? 60 : 1480, -130);
            if (BlocksInput && EventSystem.current != null &&
                (EventSystem.current.currentSelectedGameObject == null || !EventSystem.current.currentSelectedGameObject.transform.IsChildOf(card.transform)))
                RuntimeUiEventSystem.Select(next.gameObject);
        }

        private Rect FocusBounds()
        {
            if (waiting || activeId == "B1-01" || activeId == "B1-04")
            {
                var board = host.CurrentBattlefieldViewport;
                return new Rect(board.X, board.Y, board.Width, board.Height);
            }
            RectTransform target = null;
            if (activeId == "B1-02")
            {
                var detail = FindFirstObjectByType<BattlefieldEnemyDetailView>();
                if (detail != null && detail.gameObject.activeInHierarchy) target = detail.transform as RectTransform;
            }
            if (target == null)
            {
                var hud = host.GetComponent<FormalCombatHud>();
                if (hud != null) target = hud.TutorialFocus(activeId);
            }
            if (target == null) return new Rect(1440, 90, 460, 720);
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            RectTransform frame = root.GetComponent<RectTransform>();
            var a = frame.InverseTransformPoint(corners[0]);
            var b = frame.InverseTransformPoint(corners[2]);
            return new Rect(a.x + 960 - 6, 540 - b.y - 6, b.x - a.x + 12, b.y - a.y + 12);
        }
        private static Image Box(string name, Transform parent, Color color)
            => FormalUiKit.FlatPanel(name, parent, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, Vector2.one, color).GetComponent<Image>();
        private static void SetRect(RectTransform rect, Rect value)
        { rect.anchoredPosition = new Vector2(value.x, -value.y); rect.sizeDelta = new Vector2(value.width, value.height); }
        private void OnDestroy() { if (canvas != null) Destroy(canvas.gameObject); }
    }
}
