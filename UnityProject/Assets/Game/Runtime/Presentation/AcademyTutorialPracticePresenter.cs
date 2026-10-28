using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    public sealed class AcademyTutorialPracticePresenter : MonoBehaviour
    {
        private Canvas canvas;
        private GameObject root, board;
        private Text title, preview, result;
        private Button execute, finish;
        private AcademyTutorialPresenter owner;
        private AcademyTutorialExercise exercise;
        private AcademyTutorialModelExercise model;
        public static bool CanPrepare(string id) => AcademyTutorialExercise.CanPrepare(id) || AcademyTutorialModelExercise.CanPrepare(id);
        public bool IsOpen => root != null && root.activeSelf;

        public void Open(AcademyTutorialPresenter tutorial, string id)
        {
            owner = tutorial;
            try
            {
                exercise = AcademyTutorialExercise.CanPrepare(id) ? AcademyTutorialExercise.Prepare(id) : null;
                model = exercise == null ? AcademyTutorialModelExercise.Prepare(id, owner.Progress.Find(id)?.evidence) : null;
            }
            catch (Exception ex) { Debug.LogError("Teaching practice unavailable " + id + ": " + ex.Message); return; }
            EnsureUi(); root.SetActive(true);
            title.text = owner.Catalog.lessons.First(l => l.id == id).title + "　教学演练";
            preview.text = (exercise?.Instruction ?? model.Prompt) + "\n\n预计变化\n" + (exercise?.Preview ?? model.Preview);
            result.text = "不影响本轮资源、物品与进度。";
            execute.interactable = true; finish.gameObject.SetActive(false); DrawBoard();
            execute.GetComponentInChildren<Text>().text = model?.ActionLabel ?? "执行这次操作";
            finish.GetComponentInChildren<Text>().text = "收起本次结果";
            owner.Preview(id);
        }
        private void Submit()
        {
            if (exercise?.Executed == true || model?.Executed == true) return;
            try
            {
                if (model != null) model.Execute(); else exercise.Execute();
                result.text = model != null ? "本步已执行。请核对右侧实际结果。" : "实际结果\n" + exercise.Result;
                execute.interactable = false; finish.gameObject.SetActive(exercise?.MatchedPreview ?? model.MatchedPreview); DrawBoard();
                finish.GetComponentInChildren<Text>().text = model != null && !model.Executed ? "继续下一步" : "收起本次结果";
            }
            catch (Exception ex) { result.text = "演练未完成，可返回后重试。"; Debug.LogError(ex); }
        }
        private void Finish()
        {
            if (model != null && !model.Executed && model.MatchedPreview)
            {
                model.PrepareNext();
                preview.text = model.Prompt + "\n\n预计变化\n" + model.Preview;
                result.text = "不影响本轮资源、物品与进度。";
                execute.GetComponentInChildren<Text>().text = model.ActionLabel;
                execute.interactable = true; finish.gameObject.SetActive(false); DrawBoard(); return;
            }
            if (exercise != null && (!exercise.Executed || !exercise.MatchedPreview) || model != null && (!model.Executed || !model.MatchedPreview)) return;
            foreach (string proof in exercise?.Proofs ?? model.Proofs) owner.Evidence(exercise?.LessonId ?? model.LessonId, proof);
            owner.DeferPractice(exercise?.LessonId ?? model.LessonId);
            root.SetActive(false); owner.OpenReview();
        }
        private void EnsureUi()
        {
            if (canvas != null) return;
            canvas = FormalUiKit.CanvasRoot("独立教学演练", UiLayoutContract.InteractionSortingOrder + 60);
            root = FormalUiKit.AnchoredPanel("隔离练习盘", FormalUiKit.ContentParent(canvas.transform), Vector2.up, Vector2.up, Vector2.zero, new Vector2(1920, 1080), FormalUiTheme.Panel);
            title = FormalUiKit.Label("演练标题", "", root.transform, new Vector2(48, -32), new Vector2(1500, 64), 32, ArchiveUiStyle.Ink, TextAnchor.MiddleLeft);
            var back = FormalUiKit.Button("退出演练", "返回", root.transform, new Vector2(1620, -32), new Vector2(240, 64), FormalUiTheme.Interactive);
            back.onClick.AddListener(() => { owner.DeferPractice(exercise?.LessonId ?? model?.LessonId); root.SetActive(false); });
            preview = FormalUiKit.Label("正式规则预览", "", root.transform, new Vector2(48, -160), new Vector2(700, 350), 24, ArchiveUiStyle.Ink, TextAnchor.UpperLeft);
            result = FormalUiKit.Label("正式规则结果", "", root.transform, new Vector2(48, -544), new Vector2(700, 350), 24, ArchiveUiStyle.Ink, TextAnchor.UpperLeft);
            board = FormalUiKit.Create("演练棋盘", root.transform);
            var rect = board.AddComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.up;
            rect.anchoredPosition = new Vector2(800, -180); rect.sizeDelta = new Vector2(1056, 700);
            execute = FormalUiKit.Button("执行演练", "执行这次操作", root.transform, new Vector2(800, -940), new Vector2(500, 64), FormalUiTheme.Cyan); execute.onClick.AddListener(Submit);
            finish = FormalUiKit.Button("核对结果", "收起本次结果", root.transform, new Vector2(1330, -940), new Vector2(500, 64), FormalUiTheme.Safe); finish.onClick.AddListener(Finish);
        }
        private void DrawBoard()
        {
            foreach (Transform child in board.transform) Destroy(child.gameObject);
            var combat = exercise?.Combat ?? model?.Combat;
            if (combat == null && model != null)
            {
                FormalUiKit.Label("演练档案", model.MatchedPreview ? "实际结果\n\n" + model.Result : "操作前\n\n" + model.Before, board.transform, new Vector2(24, -24), new Vector2(1000, 640), 28, ArchiveUiStyle.Ink, TextAnchor.UpperLeft);
                return;
            }
            float step = Mathf.Min(88, 1020f / combat.Map.Width, 500f / combat.Map.Height);
            for (int y = 0; y < combat.Map.Height; y++)
            for (int x = 0; x < combat.Map.Width; x++)
            {
                var position = new GridPosition(x, y);
                var unit = combat.Units.Values.FirstOrDefault(u => u.Position == position && !combat.LampVines.IsHiddenFrom(combat, combat.GetUnit("hero"), u));
                var tile = combat.Map.GetTile(position);
                Color color = combat.Map.IsBlocked(position) ? FormalUiTheme.Muted : ArchiveUiStyle.LightPaper;
                if (unit != null) color = unit.IsHero ? FormalUiTheme.Cyan : FormalUiTheme.Amber;
                var cell = FormalUiKit.AnchoredPanel("格" + x + "_" + y, board.transform, Vector2.up, Vector2.up, new Vector2(x * step, -y * step), new Vector2(step - 3, step - 3), color);
                if (unit != null) FormalUiKit.Label("单位", unit.Id == "hero" ? "你" : unit.IsHero ? "友" : "靶", cell.transform, new Vector2(4, -4), new Vector2(step - 11, step - 11), 24, ArchiveUiStyle.Ink, TextAnchor.MiddleCenter);
                string symbol = tile.IsPermanentWall ? "墙" : tile.IsLootChest ? "箱" : tile.IsAetherCrystal ? "晶簇" : tile.Cover == CoverType.Heavy && !tile.IsDestroyed ? "重掩" : tile.Cover == CoverType.Light && !tile.IsDestroyed ? "轻掩" : tile.IsLampVine && tile.Durability > 0 ? combat.LampVines.KnownCells(combat, combat.GetUnit("hero")).Contains(position) ? "已知" : "灯藤" : tile.IsDeviceLike ? "装置" : tile.IsWater ? "浅水" : tile.IsLoosePaper ? "散页" : tile.HasPaperScreen ? "页幕" : tile.HasTrace ? "痕迹" : tile.IsBindingMark ? "约束纹" : tile.SmokeExpiresAt > combat.CurrentTime ? "烟尘" : tile.IsCrystalShard ? "碎晶" : combat.RogueSpells?.FireBattle.HasFireground(position) == true ? "火场" : "";
                if (!string.IsNullOrEmpty(symbol)) FormalUiKit.Label("格子规则", symbol, cell.transform, new Vector2(4, -4), new Vector2(step - 11, step - 11), 24, ArchiveUiStyle.Ink, unit == null ? TextAnchor.MiddleCenter : TextAnchor.LowerCenter);
            }
            if (model != null) FormalUiKit.Label("演练当前读数", model.MatchedPreview ? model.Result : model.Before, board.transform,
                new Vector2(0, -(combat.Map.Height * step + 24)), new Vector2(1000, 176), 24, ArchiveUiStyle.Ink, TextAnchor.UpperLeft);
        }
        private void OnDestroy() { if (canvas != null) Destroy(canvas.gameObject); }
    }
}
