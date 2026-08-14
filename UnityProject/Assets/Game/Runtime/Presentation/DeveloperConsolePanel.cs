using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace OCC.Combat.Presentation
{
    // Opt-in development surface. The training range is runtime-only and never writes player progression or scene YAML.
    public sealed class DeveloperConsolePanel : MonoBehaviour
    {
        private IDeveloperConsoleHost bootstrap;
        private bool open;
        private Vector2 resultScroll;
        private string lastError;
        private readonly Dictionary<string, Texture2D> abilityIcons = new Dictionary<string, Texture2D>(StringComparer.Ordinal);

        public void Initialize(IDeveloperConsoleHost source) { bootstrap = source; }
        public void Toggle() { open = !open; }
        public bool IsOpen => open;

        private void OnGUI()
        {
            if (!DeveloperBuildGate.IsEnabled || bootstrap == null || !Application.isPlaying) return;
            GUI.depth = -1000;
            HandleHotkeys();
            float scale = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector2((Screen.width - 1920f * scale) * .5f, (Screen.height - 1080f * scale) * .5f), Quaternion.identity, Vector3.one * scale);
            GUI.skin.font = FormalUiKit.Font;
            GUI.skin.label.fontSize = 18; GUI.skin.button.fontSize = 17; GUI.skin.textArea.fontSize = 16;
            if (!open)
            {
                if (bootstrap.IsTrainingRangeActive) DrawTrainingRangeLauncher();
                GUI.matrix = previous;
                return;
            }
            if (bootstrap.IsTrainingRangeActive) DrawTrainingLoadout(); else DrawHome();
            GUI.matrix = previous;
        }

        private void HandleHotkeys()
        {
            Event current = Event.current;
            if (current == null || current.type != EventType.KeyDown) return;
            if (current.keyCode == KeyCode.F1) { Toggle(); current.Use(); }
            else if (current.keyCode == KeyCode.F2)
            {
                if (!bootstrap.IsTrainingRangeActive) bootstrap.StartTrainingRange();
                open = true; current.Use();
            }
        }

        private void DrawHome()
        {
            Rect panel = new Rect(590, 220, 740, 520); Panel(panel, new Color(.95f, .72f, .24f));
            Title(panel, "开发控制台 // F1 关闭", "F2 可直接进入全游戏术式调试靶场。");
            if (GUI.Button(new Rect(panel.x + 28, panel.y + 126, 684, 72), "进入全游戏术式调试靶场\n77 项能力 · 目标预览 · 确定性施放 · 数值复测"))
            { bootstrap.StartTrainingRange(); lastError = null; }
            GUI.enabled = bootstrap.IsDeveloperCombatActive;
            if (GUI.Button(new Rect(panel.x + 28, panel.y + 224, 212, 58), "战术重开")) bootstrap.TacticalRestartDeveloperCombat();
            if (GUI.Button(new Rect(panel.x + 264, panel.y + 224, 212, 58), "测试胜利")) bootstrap.ForceCurrentOutcome(true);
            if (GUI.Button(new Rect(panel.x + 500, panel.y + 224, 212, 58), "测试失败")) bootstrap.ForceCurrentOutcome(false);
            GUI.enabled = true;
            if (GUI.Button(new Rect(panel.x + 28, panel.y + 310, 332, 58), "返回入口")) bootstrap.ReturnToDeveloperMenu();
            if (GUI.Button(new Rect(panel.x + 380, panel.y + 310, 332, 58), "关闭控制台")) open = false;
            GUI.color = new Color(.58f, .65f, .68f);
            GUI.Label(new Rect(panel.x + 28, panel.y + 400, 684, 70), "靶场不推进剧情或肉鸽存档；敌方 AI、胜负收束与自动结束行动均停用。关闭控制台后仍可在棋盘上手动选格施术。");
            GUI.color = Color.white;
        }

        private void DrawTrainingRange()
        {
            TrainingRangeSession session = bootstrap.TrainingRange;
            Rect panel = new Rect(150, 92, 1620, 896); Panel(panel, new Color(.38f, .84f, .92f));
            Title(panel, "OCC // 术式调试靶场", "F1 关闭覆盖层 · F2 打开 · Bug 复现 · 数值调整 · 无存档写入");
            if (session == null)
            {
                GUI.color = new Color(.92f, .72f, .28f);
                GUI.Label(new Rect(panel.x + 26, panel.y + 100, panel.width - 52, 40), "靶场正在初始化，下一帧自动恢复……");
                GUI.color = Color.white;
                return;
            }
            if (GUI.Button(new Rect(panel.x + 26, panel.y + 86, 190, 52), "重置靶场")) Safe(bootstrap.PrepareTrainingRangeCurrent);
            if (GUI.Button(new Rect(panel.x + 230, panel.y + 86, 190, 52), "预览目标")) Safe(() => bootstrap.PreviewTrainingRangeCurrent());
            if (GUI.Button(new Rect(panel.x + 434, panel.y + 86, 190, 52), "施放术式")) Safe(() => bootstrap.ExecuteTrainingRangeCurrent());
            if (GUI.Button(new Rect(panel.x + 1310, panel.y + 86, 132, 52), "回到入口")) { bootstrap.ReturnToDeveloperMenu(); return; }
            if (GUI.Button(new Rect(panel.x + 1456, panel.y + 86, 136, 52), "关闭 F1")) open = false;

            Rect list = new Rect(panel.x + 26, panel.y + 158, 420, 700); Box(list, "术式目录");
            GUI.Label(new Rect(list.x + 18, list.y + 42, 384, 28), $"第 {session.CurrentPage + 1}/{session.PageCount} 页 // {session.Abilities.Count} 项能力");
            int row = 0;
            foreach (TrainingRangeAbilityEntry ability in session.AbilitiesOnCurrentPage())
            {
                bool selected = ability.Id == session.CurrentAbility.Id;
                Color old = GUI.backgroundColor; GUI.backgroundColor = selected ? new Color(.18f, .48f, .54f) : new Color(.10f, .13f, .14f);
                float rowY = list.y + 78 + row * 52;
                if (GUI.Button(new Rect(list.x + 18, rowY, 384, 44), $"       {ability.Id}  {ability.DisplayName}  // {ability.Group}"))
                    Safe(() => bootstrap.SelectTrainingRangeAbility(ability.Id));
                DrawAbilityIcon(ability, new Rect(list.x + 26, rowY + 6, 32, 32));
                GUI.backgroundColor = old; row++;
            }
            if (GUI.Button(new Rect(list.x + 18, list.y + 616, 178, 48), "◀ 上一页")) Safe(() => bootstrap.ShiftTrainingRangePage(-1));
            if (GUI.Button(new Rect(list.x + 224, list.y + 616, 178, 48), "下一页 ▶")) Safe(() => bootstrap.ShiftTrainingRangePage(1));

            Rect detail = new Rect(panel.x + 466, panel.y + 158, 438, 700); Box(detail, "当前术式 / 数值参数");
            TrainingRangeAbilityEntry current = session.CurrentAbility;
            DrawAbilityIcon(current, new Rect(detail.x + 20, detail.y + 44, 64, 64));
            GUI.color = new Color(.95f, .75f, .28f);
            GUI.Label(new Rect(detail.x + 100, detail.y + 46, 318, 54), current.Id + " // " + current.DisplayName);
            GUI.color = Color.white;
            LabelBlock(detail, 120, "家族", current.Family + " / " + current.Group);
            LabelBlock(detail, 176, "成本", current.Cost);
            LabelBlock(detail, 232, "目标", current.Targeting);
            LabelBlock(detail, 306, "规则链", current.Summary, 108);
            LabelBlock(detail, 430, "图标资源", current.IconPath, 70);
            GUI.color = new Color(.58f, .72f, .75f);
            GUI.Label(new Rect(detail.x + 20, detail.y + 530, 398, 104), "调试流程：选择术式 → 重置靶场 → 预览目标 → 施放。修改数值或规则后重新编译，即可在相同环境中复测。");
            GUI.color = Color.white;

            Rect result = new Rect(panel.x + 924, panel.y + 158, 668, 700); Box(result, "目标预览 / 施放结果");
            TrainingRangePreviewReport preview = session.LastPreview;
            TrainingRangeExecutionReport execution = session.LastExecution;
            GUI.color = preview == null ? new Color(.58f, .65f, .68f) : preview.CanCommit ? new Color(.35f, .86f, .62f) : new Color(.94f, .32f, .25f);
            GUI.Label(new Rect(result.x + 20, result.y + 44, 628, 34), preview == null ? "等待目标预览" : preview.Summary);
            GUI.color = Color.white;
            string resultText = execution == null ? "尚未执行。可先预览，再执行；也可关闭控制台后点击棋盘合法格。" :
                execution.Summary + "\n\n" + string.Join("\n", execution.Steps);
            float resultHeight = Mathf.Max(544, 42 + (execution?.Steps.Count ?? 3) * 30);
            resultScroll = GUI.BeginScrollView(new Rect(result.x + 18, result.y + 86, 632, 520), resultScroll, new Rect(0, 0, 604, resultHeight));
            GUI.TextArea(new Rect(0, 0, 604, resultHeight), resultText);
            GUI.EndScrollView();
            if (!string.IsNullOrEmpty(lastError))
            {
                GUI.color = new Color(.94f, .32f, .25f);
                GUI.Label(new Rect(result.x + 20, result.y + 626, 628, 54), "控制台错误 // " + lastError); GUI.color = Color.white;
            }
        }

        private void DrawTrainingRangeLauncher()
        {
            TrainingRangeAbilityEntry current = bootstrap.TrainingRange?.CurrentAbility;
            string label = current == null ? "靶场配置  F1" : $"靶场配置  F1\n{current.Id} · {current.DisplayName}";
            if (GUI.Button(new Rect(1530, 940, 330, 66), label)) open = true;
        }

        private void DrawTrainingLoadout()
        {
            TrainingRangeSession session = bootstrap.TrainingRange;
            Rect panel = new Rect(370, 110, 1180, 860); Panel(panel, new Color(.38f, .84f, .92f));
            Title(panel, "OCC // 靶场配置", "选择要装载的术式或法宝；装载后回到正常战斗界面测试目标、数值和 Bug。");
            if (session == null)
            {
                GUI.Label(new Rect(panel.x + 28, panel.y + 100, panel.width - 56, 40), "靶场正在初始化……");
                return;
            }

            if (GUI.Button(new Rect(panel.x + 780, panel.y + 82, 214, 52), "装载并重置战斗"))
            {
                Safe(bootstrap.PrepareTrainingRangeCurrent);
                if (string.IsNullOrEmpty(lastError)) open = false;
            }
            if (GUI.Button(new Rect(panel.x + 1010, panel.y + 82, 142, 52), "返回战斗")) open = false;

            Rect list = new Rect(panel.x + 28, panel.y + 154, 520, 676); Box(list, "可选术式与法宝");
            GUI.Label(new Rect(list.x + 18, list.y + 42, 484, 28), $"第 {session.CurrentPage + 1}/{session.PageCount} 页 // 共 {session.Abilities.Count} 项");
            int row = 0;
            foreach (TrainingRangeAbilityEntry ability in session.AbilitiesOnCurrentPage())
            {
                bool selected = ability.Id == session.CurrentAbility.Id;
                Color old = GUI.backgroundColor;
                GUI.backgroundColor = selected ? new Color(.18f, .48f, .54f) : new Color(.10f, .13f, .14f);
                float rowY = list.y + 78 + row * 52;
                string kind = ability.ProviderId == "artifact" ? "法宝" : "术式";
                if (GUI.Button(new Rect(list.x + 18, rowY, 484, 44), $"       {ability.Id}  {ability.DisplayName}  // {kind} · {ability.Group}"))
                    Safe(() => bootstrap.BrowseTrainingRangeAbility(ability.Id));
                DrawAbilityIcon(ability, new Rect(list.x + 26, rowY + 6, 32, 32));
                GUI.backgroundColor = old;
                row++;
            }
            if (GUI.Button(new Rect(list.x + 18, list.y + 610, 224, 48), "◀ 上一页")) Safe(() => bootstrap.ShiftTrainingRangePage(-1));
            if (GUI.Button(new Rect(list.x + 278, list.y + 610, 224, 48), "下一页 ▶")) Safe(() => bootstrap.ShiftTrainingRangePage(1));

            Rect detail = new Rect(panel.x + 568, panel.y + 154, 584, 676); Box(detail, "装载详情");
            TrainingRangeAbilityEntry current = session.CurrentAbility;
            DrawAbilityIcon(current, new Rect(detail.x + 24, detail.y + 48, 96, 96));
            GUI.color = new Color(.95f, .75f, .28f);
            GUI.Label(new Rect(detail.x + 144, detail.y + 50, 410, 48), current.Id + " // " + current.DisplayName);
            GUI.color = Color.white;
            string contentKind = current.ProviderId == "artifact" ? "法宝" : "术式";
            LabelBlock(detail, 160, "类型", contentKind + " / " + current.Family + " / " + current.Group);
            LabelBlock(detail, 218, "成本", current.Cost);
            LabelBlock(detail, 276, "目标", current.Targeting, 62);
            LabelBlock(detail, 350, "效果", current.Summary, 132);
            LabelBlock(detail, 500, "图标", current.IconPath, 54);
            GUI.color = new Color(.58f, .72f, .75f);
            GUI.Label(new Rect(detail.x + 24, detail.y + 570, detail.width - 48, 72), current.ProviderId == "artifact"
                ? "法宝会按真实封装次数运行；次数耗尽后必须重新装载，不能普通充能。"
                : "装载后使用正常战斗 HUD 的技能1、目标高亮与棋盘点击完成测试。");
            GUI.color = Color.white;

            if (!string.IsNullOrEmpty(lastError))
            {
                GUI.color = new Color(.94f, .32f, .25f);
                GUI.Label(new Rect(panel.x + 28, panel.y + 826, panel.width - 56, 28), "配置错误 // " + lastError);
                GUI.color = Color.white;
            }
        }

        private void Safe(Action action)
        {
            try { action(); lastError = null; }
            catch (Exception error) { lastError = error.Message; }
        }

        private void DrawAbilityIcon(TrainingRangeAbilityEntry ability, Rect rect)
        {
            if (!abilityIcons.TryGetValue(ability.IconPath, out Texture2D texture))
            {
                Sprite sprite = Resources.Load<Sprite>(ability.IconPath);
                texture = sprite == null ? null : sprite.texture;
                abilityIcons[ability.IconPath] = texture;
            }
            if (texture == null) return;
            Color previous = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit, true);
            GUI.color = previous;
        }

        private static void Panel(Rect rect, Color accent)
        {
            GUI.color = new Color(.018f, .025f, .03f, 1f); GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = accent; GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 4), Texture2D.whiteTexture); GUI.color = Color.white;
        }

        private static void Box(Rect rect, string title)
        {
            GUI.color = new Color(.035f, .052f, .058f, 1f); GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = Color.white;
            GUI.Box(rect, "");
            GUI.Label(new Rect(rect.x + 18, rect.y + 10, rect.width - 36, 28), title);
        }

        private static void Title(Rect panel, string title, string subtitle)
        {
            GUI.color = new Color(.92f, .95f, .96f); GUI.Label(new Rect(panel.x + 26, panel.y + 18, panel.width - 52, 30), title);
            GUI.color = new Color(.58f, .65f, .68f); GUI.Label(new Rect(panel.x + 26, panel.y + 50, panel.width - 52, 26), subtitle); GUI.color = Color.white;
        }

        private static void LabelBlock(Rect parent, float y, string heading, string value, float height = 42)
        {
            GUI.color = new Color(.50f, .72f, .76f); GUI.Label(new Rect(parent.x + 20, parent.y + y, 88, 28), heading);
            GUI.color = new Color(.90f, .93f, .94f); GUI.Label(new Rect(parent.x + 112, parent.y + y, parent.width - 132, height), value); GUI.color = Color.white;
        }
    }
}
#endif
