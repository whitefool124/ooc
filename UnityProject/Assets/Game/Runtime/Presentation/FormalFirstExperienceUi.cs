using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    public sealed class FormalFirstExperienceUi : MonoBehaviour
    {
        private FirstExperiencePrototypeController flow;
        private Canvas canvas;
        private GameObject page;
        private string signature = string.Empty;
        private readonly List<Button> buttons = new List<Button>();

        public bool IsVisible => page != null && page.activeSelf;

        public void Initialize(FirstExperiencePrototypeController source)
        {
            flow = source;
            if (canvas == null) canvas = FormalUiKit.CanvasRoot("首次体验正式界面", UiLayoutContract.InteractionSortingOrder + 10);
            Rebuild();
        }

        private void Update()
        {
            if (flow == null || canvas == null) return;
            bool visible = flow.enabled;
            if (canvas.gameObject.activeSelf != visible) canvas.gameObject.SetActive(visible);
            if (!visible) return;
            string next = flow.CurrentStage + "|" + flow.IsSelectingContinue + "|" + flow.IsPendingOverwrite + "|" +
                flow.ResolutionIndex + "|" + Mathf.RoundToInt(flow.PendingVolume * 100f) + "|" + flow.PendingFullscreen + "|" +
                flow.DebugSelectedSlot + "|" + flow.DebugOpeningBrandMarkIndex + "|" + flow.HasSave;
            if (next != signature) Rebuild();
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true) flow.GoBack();
        }

        private void Rebuild()
        {
            if (flow == null || canvas == null) return;
            if (page != null) Destroy(page);
            buttons.Clear();
            page = FormalUiKit.Create("页面_" + flow.CurrentStage, canvas.transform);
            RectTransform pageRect = page.AddComponent<RectTransform>(); FormalUiKit.Stretch(pageRect);
            BuildBackground(page.transform, flow.CurrentStage);
            switch (flow.CurrentStage)
            {
                case FirstExperiencePrototypeController.FlowStage.Branding: BuildBranding(); break;
                case FirstExperiencePrototypeController.FlowStage.FirstSettings: BuildSettings(); break;
                case FirstExperiencePrototypeController.FlowStage.WorldOpening: BuildOpening(); break;
                case FirstExperiencePrototypeController.FlowStage.Landing: BuildLanding(); break;
                case FirstExperiencePrototypeController.FlowStage.SaveSelection: BuildSlots(); break;
                case FirstExperiencePrototypeController.FlowStage.SaveConfirmation: BuildConfirmation(); break;
                case FirstExperiencePrototypeController.FlowStage.Configuration: BuildConfiguration(); break;
                case FirstExperiencePrototypeController.FlowStage.AcademyIntro: BuildAcademyIntro(); break;
                default: BuildHandoff(); break;
            }
            LinkNavigation();
            if (buttons.Count > 0) RuntimeUiEventSystem.Select(buttons[0].gameObject);
            signature = flow.CurrentStage + "|" + flow.IsSelectingContinue + "|" + flow.IsPendingOverwrite + "|" +
                flow.ResolutionIndex + "|" + Mathf.RoundToInt(flow.PendingVolume * 100f) + "|" + flow.PendingFullscreen + "|" +
                flow.DebugSelectedSlot + "|" + flow.DebugOpeningBrandMarkIndex + "|" + flow.HasSave;
        }

        private static void BuildBackground(Transform parent, FirstExperiencePrototypeController.FlowStage stage)
        {
            Image background = parent.gameObject.AddComponent<Image>();
            string id = stage == FirstExperiencePrototypeController.FlowStage.Landing || stage == FirstExperiencePrototypeController.FlowStage.SaveSelection || stage == FirstExperiencePrototypeController.FlowStage.Configuration
                ? "landing" : stage == FirstExperiencePrototypeController.FlowStage.FirstSettings ? "settings" : "startup";
            FormalUiEffects.ApplyBackdrop(background, id);
            background.color = stage == FirstExperiencePrototypeController.FlowStage.WorldOpening
                ? FormalUiTheme.WithAlpha(Color.white, .18f) : Color.white;
            background.raycastTarget = true;
            FormalUiEffects.AddPageDecorations(parent, id, 1f);
        }

        private void BuildBranding()
        {
            GameObject panel = CenterPanel(new Vector2(980, 420));
            Label("印记", flow.DebugOpeningBrandMarkIndex == 0 ? "OC 以太工业联合印记" : "OCC 生命档案局", panel.transform,
                new Vector2(50, -86), new Vector2(880, 86), 48, FormalUiTheme.Text, TextAnchor.MiddleCenter);
            Label("副标", "档案协议正在装订", panel.transform, new Vector2(50, -208), new Vector2(880, 52), 24, FormalUiTheme.Muted, TextAnchor.MiddleCenter);
            AddButton("跳过品牌", "跳过品牌", panel.transform, new Vector2(650, -318), new Vector2(280, 64), flow.FinishBranding);
        }

        private void BuildSettings()
        {
            Header(flow.IsSettingsFromLanding ? "辅助设置" : "首启设置", "显示与声音会保存到本机，可随时从以太主界面修改。");
            GameObject sheet = FormalUiKit.AnchoredPanel("设置档案", page.transform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, -20), new Vector2(1440, 690), FormalUiTheme.SurfaceRaised);
            SettingRow(sheet.transform, 52, "主音量", Mathf.RoundToInt(flow.PendingVolume * 100f) + "%", "降低", () => flow.PendingVolume -= .1f, "提高", () => flow.PendingVolume += .1f);
            SettingRow(sheet.transform, 210, "分辨率", flow.ResolutionLabel, "切换", flow.CycleResolution, null, null);
            SettingRow(sheet.transform, 368, "显示模式", flow.PendingFullscreen ? "全屏" : "窗口", "更改", () => flow.PendingFullscreen = !flow.PendingFullscreen, null, null);
            AddButton("应用设置", "应用设置", sheet.transform, new Vector2(962, -548), new Vector2(410, 90), flow.ApplyPendingSettings, FormalUiTheme.Cyan);
            if (flow.IsSettingsFromLanding) AddButton("取消", "取消", sheet.transform, new Vector2(52, -548), new Vector2(280, 90), flow.GoBack);
        }

        private void BuildOpening()
        {
            GameObject header = FormalUiKit.AnchoredPanel("开场抬头", page.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(36, -32), new Vector2(920, 96), FormalUiTheme.WithAlpha(FormalUiTheme.Ink, .92f));
            Label("标题", "以太历 413 年　学院北门", header.transform, new Vector2(28, -18), new Vector2(830, 52), 24, FormalUiTheme.OnInk, TextAnchor.MiddleLeft);
            AddButton("跳过动画", "跳过动画", page.transform, new Vector2(1580, -42), new Vector2(280, 70), flow.FinishWorldOpening);
        }

        private void BuildLanding()
        {
            GameObject left = FormalUiKit.Panel("以太主界面主栏", page.transform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(1220, 1080), FormalUiTheme.WithAlpha(FormalUiTheme.Ink, .96f));
            RectTransform leftRect = left.GetComponent<RectTransform>(); leftRect.anchorMin = new Vector2(0, 0); leftRect.anchorMax = new Vector2(0, 1); leftRect.pivot = new Vector2(0, 1); leftRect.anchoredPosition = Vector2.zero;
            Label("卷宗", "以太人生档案", left.transform, new Vector2(84, -92), new Vector2(1020, 80), 48, FormalUiTheme.OnInk, TextAnchor.MiddleLeft);
            Label("说明", "从学院出发，记录战争与人生转折。当前开放：学院阶段。", left.transform, new Vector2(84, -202), new Vector2(1020, 54), 24, FormalUiTheme.WithAlpha(FormalUiTheme.OnInk, .72f), TextAnchor.MiddleLeft);
            AddButton("开始新游戏", "新建档案", left.transform, new Vector2(84, -330), new Vector2(520, 90), flow.StartNewRun, FormalUiTheme.Cyan);
            Button resume = AddButton("继续游戏", "继续档案", left.transform, new Vector2(632, -330), new Vector2(500, 90), flow.ContinueRun);
            resume.interactable = flow.HasSave;
            AddButton("设置", "辅助设置", left.transform, new Vector2(84, -452), new Vector2(520, 78), flow.OpenSettings);
            AddButton("重看开场", "重看世界观", left.transform, new Vector2(632, -452), new Vector2(500, 78), flow.ReplayWorldOpening);
            for (int slot = 0; slot < 3; slot++)
            {
                float x = 84 + slot * 360;
                GameObject card = FormalUiKit.Panel("档案卡" + slot, left.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, -674), new Vector2(320, 238), FormalUiTheme.SurfaceRaised);
                Label("编号", "档案 0" + (slot + 1), card.transform, new Vector2(24, -22), new Vector2(272, 44), 24, FormalUiTheme.Text, TextAnchor.MiddleLeft);
                Label("状态", flow.SlotExists(slot) ? flow.SlotDisplaySummary(slot) : "空白卷宗", card.transform, new Vector2(24, -82), new Vector2(272, 112), 24, flow.SlotExists(slot) ? FormalUiTheme.Safe : FormalUiTheme.Muted, TextAnchor.UpperLeft);
            }
        }

        private void BuildSlots()
        {
            Header(flow.IsSelectingContinue ? "选择继续档案" : "选择新档案位置", flow.IsSelectingContinue ? "只显示可继续的卷宗。" : "已有卷宗会在下一步要求确认覆盖。");
            for (int slot = 0; slot < 3; slot++)
            {
                int captured = slot; bool exists = flow.SlotExists(slot);
                GameObject card = FormalUiKit.AnchoredPanel("档案选择卡" + slot, page.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(140 + slot * 570, -300), new Vector2(500, 390), FormalUiTheme.SurfaceRaised);
                Label("编号", "档案 0" + (slot + 1), card.transform, new Vector2(32, -34), new Vector2(430, 52), 24, FormalUiTheme.Amber, TextAnchor.MiddleLeft);
                Label("摘要", exists ? flow.SlotDisplaySummary(slot) : "空白卷宗\n尚未建立学生档案", card.transform, new Vector2(32, -114), new Vector2(430, 130), 24, exists ? FormalUiTheme.Text : FormalUiTheme.Muted, TextAnchor.UpperLeft);
                Button choose = AddButton("选择档案" + slot, flow.IsSelectingContinue ? "继续" : exists ? "覆盖此档案" : "在此建立", card.transform, new Vector2(32, -282), new Vector2(436, 72), () => flow.ChooseSlot(captured));
                choose.interactable = !flow.IsSelectingContinue || exists;
            }
            AddButton("返回", "返回以太主界面", page.transform, new Vector2(140, -850), new Vector2(360, 80), flow.GoBack);
        }

        private void BuildConfirmation()
        {
            GameObject card = CenterPanel(new Vector2(1120, 520));
            Label("标题", flow.IsPendingOverwrite ? "确认覆盖档案" : "确认建立档案", card.transform, new Vector2(56, -54), new Vector2(1008, 74), 48, flow.IsPendingOverwrite ? FormalUiTheme.Danger : FormalUiTheme.Text, TextAnchor.MiddleLeft);
            Label("正文", flow.IsPendingOverwrite ? "原有进度将被新档案替换。该操作不可撤回。" : "建立固定学生档案并进入基础配置。", card.transform, new Vector2(56, -166), new Vector2(1008, 110), 24, FormalUiTheme.Text, TextAnchor.UpperLeft);
            AddButton("取消", "取消", card.transform, new Vector2(56, -386), new Vector2(360, 78), flow.GoBack);
            Button confirm = AddButton("确认", flow.IsPendingOverwrite ? "确认覆盖" : "确认建立", card.transform, new Vector2(632, -386), new Vector2(432, 78), flow.ConfirmSelectedSlot, flow.IsPendingOverwrite ? FormalUiTheme.Danger : FormalUiTheme.Cyan);
            RuntimeUiEventSystem.Select(buttons[0].gameObject);
        }

        private void BuildConfiguration()
        {
            Header("选择人生档案主角", "当前仅开放维克多·维恩；确认前不会建立或覆盖存档。");

            GameObject profile = FormalUiKit.AnchoredPanel("角色档案", page.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(80, -230), new Vector2(1050, 500), FormalUiTheme.SurfaceRaised);
            Label("姓名", "维克多·维恩", profile.transform, new Vector2(38, -28), new Vector2(960, 68), 48, FormalUiTheme.Text, TextAnchor.MiddleLeft);
            Label("身份", "雾桥村出身　学院工读学生\n火系　公共修缮与实地安全", profile.transform, new Vector2(40, -104), new Vector2(960, 68), 24, FormalUiTheme.Amber, TextAnchor.UpperLeft);
            Label("简介", "通过公开考核与工读进入学院的普通学生。习惯先登记风险，再处理最坏的泄漏点；擅长把火用成可控、可复核的工程力量。", profile.transform, new Vector2(40, -166), new Vector2(940, 116), 24, FormalUiTheme.Text, TextAnchor.UpperLeft);
            Label("特性标题", "核心特性", profile.transform, new Vector2(40, -310), new Vector2(240, 38), 24, FormalUiTheme.Muted, TextAnchor.MiddleLeft);
            Label("特性", "稳定施术　风险复核\n地形与器材协同　余温复写", profile.transform, new Vector2(40, -354), new Vector2(940, 64), 24, FormalUiTheme.Cyan, TextAnchor.UpperLeft);
            Label("限制", "限制：长时间施术会导致手腕痉挛、脱水与注意力下降。", profile.transform, new Vector2(40, -414), new Vector2(940, 42), 24, FormalUiTheme.Muted, TextAnchor.MiddleLeft);

            GameObject portrait = FormalUiKit.AnchoredPanel("角色立绘", page.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(1160, -230), new Vector2(680, 500), FormalUiTheme.WithAlpha(FormalUiTheme.Ink, .96f));
            // The archive page uses the dedicated portrait candidate rather than the
            // tiny combat unit sprite, so facial features remain readable at 320px.
            Sprite heroSprite = Resources.Load<Sprite>("Art/FormalUICharacterPortraits/victor_portrait");
            GameObject portraitObject = FormalUiKit.Create("维克多像素立绘", portrait.transform);
            RectTransform portraitRect = portraitObject.AddComponent<RectTransform>(); portraitRect.anchorMin = portraitRect.anchorMax = new Vector2(.5f, .5f); portraitRect.pivot = new Vector2(.5f, .5f); portraitRect.anchoredPosition = new Vector2(0, 24); portraitRect.sizeDelta = new Vector2(320, 320);
            Image portraitImage = portraitObject.AddComponent<Image>(); portraitImage.sprite = heroSprite; portraitImage.preserveAspect = true; portraitImage.raycastTarget = false;
            Label("代号", "档案代号：灰签", portrait.transform, new Vector2(40, -422), new Vector2(600, 40), 24, FormalUiTheme.OnInk, TextAnchor.MiddleCenter);

            GameObject fixedConfig = FormalUiKit.AnchoredPanel("首次体验固定配置", page.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(80, -758), new Vector2(1050, 212), FormalUiTheme.SurfaceRaised);
            Label("配置标题", "首次体验固定配置", fixedConfig.transform, new Vector2(28, -18), new Vector2(980, 36), 24, FormalUiTheme.Amber, TextAnchor.MiddleLeft);
            string[] titles = { "学生背景", "就地接线", "借障导流" };
            string[] details = { "公开考核与工读入学", "邻接掩体后回盾回魔", "借掩体获得护盾与移动" };
            for (int i = 0; i < 3; i++)
            {
                float x = 28 + i * 334;
                GameObject card = FormalUiKit.AnchoredPanel("固定配置卡" + i, fixedConfig.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, -66), new Vector2(310, 112), FormalUiTheme.Surface);
                Label("名称", titles[i], card.transform, new Vector2(18, -12), new Vector2(274, 36), 24, FormalUiTheme.Text, TextAnchor.MiddleLeft);
                Label("说明", details[i], card.transform, new Vector2(18, -54), new Vector2(274, 38), 18, FormalUiTheme.Muted, TextAnchor.MiddleLeft);
            }

            GameObject queue = FormalUiKit.AnchoredPanel("角色队列", page.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(1160, -758), new Vector2(680, 108), FormalUiTheme.SurfaceRaised);
            for (int i = 0; i < 6; i++)
            {
                GameObject slot = FormalUiKit.AnchoredPanel("角色槽" + i, queue.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(16 + i * 110, -14), new Vector2(94, 78), i == 0 ? FormalUiTheme.WithAlpha(FormalUiTheme.Cyan, .28f) : FormalUiTheme.Surface);
                Label("槽位", i == 0 ? "维克多" : "未开放", slot.transform, new Vector2(4, -12), new Vector2(86, 48), i == 0 ? 18 : 14, i == 0 ? FormalUiTheme.Cyan : FormalUiTheme.Muted, TextAnchor.MiddleCenter);
            }
            AddButton("返回角色选择", "返回", page.transform, new Vector2(1160, -890), new Vector2(220, 80), flow.GoBack);
            AddButton("以此角色开始", "以此角色开始", page.transform, new Vector2(1400, -890), new Vector2(440, 80), flow.ConfirmConfiguration, FormalUiTheme.Cyan);
        }

        private void BuildAcademyIntro()
        {
            Header("学院阶段　入学实操", "每份档案首次进入时展示一次。");
            GameObject module = FormalUiKit.AnchoredPanel("学院说明", page.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(112, -254), new Vector2(1020, 610), FormalUiTheme.WithAlpha(FormalUiTheme.Ink, .95f));
            Label("目标", "完成学院实地调查", module.transform, new Vector2(52, -52), new Vector2(900, 72), 48, FormalUiTheme.OnInk, TextAnchor.MiddleLeft);
            Label("路径", "三场普通战  →  三个事件  →  工坊加工  →  健康确认  →  精英挑战", module.transform, new Vector2(52, -162), new Vector2(900, 126), 24, FormalUiTheme.WithAlpha(FormalUiTheme.OnInk, .82f), TextAnchor.UpperLeft);
            string[] labels = { "01 观察场地", "02 整理行囊", "03 完成节点" };
            for (int i = 0; i < 3; i++)
            {
                GameObject card = FormalUiKit.AnchoredPanel("入学步骤" + i, page.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(1190, -254 - i * 176), new Vector2(610, 142), FormalUiTheme.SurfaceRaised);
                Label("步骤", labels[i], card.transform, new Vector2(28, -28), new Vector2(554, 64), 24, i == 0 ? FormalUiTheme.Cyan : FormalUiTheme.Text, TextAnchor.MiddleLeft);
            }
            AddButton("进入首次地图", "进入学院地图", page.transform, new Vector2(1310, -850), new Vector2(470, 86), flow.EnterAcademy, FormalUiTheme.Cyan);
        }

        private void BuildHandoff()
        {
            Header("进入学院地图", string.IsNullOrEmpty(flow.HandoffError) ? "正在连接地图、档案与真实战斗运行态。" : flow.HandoffError);
            AddButton("重试进入地图", "重试进入地图", page.transform, new Vector2(1310, -850), new Vector2(470, 86), flow.EnterAcademy, FormalUiTheme.Cyan);
        }

        private void Header(string title, string subtitle)
        {
            GameObject header = FormalUiKit.AnchoredPanel("页面抬头", page.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(64, -50), new Vector2(1792, 150), FormalUiTheme.WithAlpha(FormalUiTheme.Ink, .95f));
            Label("标题", title, header.transform, new Vector2(42, -22), new Vector2(1660, 70), 48, FormalUiTheme.OnInk, TextAnchor.MiddleLeft);
            Label("副标题", subtitle, header.transform, new Vector2(42, -92), new Vector2(1660, 42), 24, FormalUiTheme.WithAlpha(FormalUiTheme.OnInk, .72f), TextAnchor.MiddleLeft);
        }

        private GameObject CenterPanel(Vector2 size) => FormalUiKit.AnchoredPanel("中心档案卡", page.transform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, size, FormalUiTheme.SurfaceRaised);

        private void SettingRow(Transform parent, float y, string name, string value, string leftLabel, Action left, string rightLabel, Action right)
        {
            GameObject row = FormalUiKit.Panel("设置行_" + name, parent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(52, -y), new Vector2(1336, 126), FormalUiTheme.Surface);
            Label("名称", name, row.transform, new Vector2(30, -28), new Vector2(360, 58), 24, FormalUiTheme.Text, TextAnchor.MiddleLeft);
            Label("值", value, row.transform, new Vector2(430, -28), new Vector2(390, 58), 24, FormalUiTheme.Cyan, TextAnchor.MiddleCenter);
            if (left != null) AddButton("设置_" + name + "_左", leftLabel, row.transform, new Vector2(850, -24), new Vector2(right == null ? 430 : 200, 70), left);
            if (right != null) AddButton("设置_" + name + "_右", rightLabel, row.transform, new Vector2(1080, -24), new Vector2(220, 70), right);
        }

        private Button AddButton(string name, string title, Transform parent, Vector2 position, Vector2 size, Action action, Color? tone = null)
        {
            Color surface = tone ?? FormalUiTheme.Interactive;
            Button button = FormalUiKit.Button("按钮_" + name, title, parent, position, size, surface);
            button.onClick.AddListener(() => action?.Invoke());
            FormalUiKit.ConfigureButtonFeedback(button, FormalUiButtonPalette.ForAccent(surface, FormalUiTheme.Cyan),
                () => UiMotionProfile.FromIntensity(flow == null ? 1f : flow.AnimationIntensity), null);
            buttons.Add(button);
            return button;
        }

        private static Text Label(string name, string text, Transform parent, Vector2 position, Vector2 size, int fontSize, Color color, TextAnchor anchor) =>
            FormalUiKit.Label(name, text, parent, position, size, fontSize, color, anchor);

        private void LinkNavigation()
        {
            for (int i = 0; i < buttons.Count; i++)
            {
                Navigation navigation = new Navigation { mode = Navigation.Mode.Explicit };
                navigation.selectOnUp = buttons[(i - 1 + buttons.Count) % buttons.Count];
                navigation.selectOnLeft = navigation.selectOnUp;
                navigation.selectOnDown = buttons[(i + 1) % buttons.Count];
                navigation.selectOnRight = navigation.selectOnDown;
                buttons[i].navigation = navigation;
            }
        }

        private void OnDestroy()
        {
            if (canvas != null) Destroy(canvas.gameObject);
        }
    }
}
