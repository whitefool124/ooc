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
        private FirstExperiencePrototypeController.FlowStage? animatedStage;
        private Slider volumeSlider;
        private Text volumeValue;
        private Text confirmationCountdown;
        private FormalHoverTooltip hoverTooltip;
        private MainMenuDeskScene deskScene;
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
            if (canvas.gameObject.activeSelf != visible)
            {
                if(!visible)Hd2DContentGeometry.PrepareToHide(canvas.gameObject);
                canvas.gameObject.SetActive(visible);
            }
            if (!visible) return;
            if ((flow.IsEncyclopediaOpen || flow.IsArchiveOpen) && hoverTooltip != null && hoverTooltip.IsVisible) hoverTooltip.HideAll();
            string next = PageSignature();
            if (next != signature) Rebuild();
            if (confirmationCountdown != null)
                confirmationCountdown.text = flow.DisplayConfirmationSeconds + " 秒后自动恢复原设置";
            if (volumeSlider != null && volumeValue != null)
            {
                volumeSlider.SetValueWithoutNotify(flow.PendingVolume);
                volumeValue.text = Mathf.RoundToInt(flow.PendingVolume * 100f) + "%";
            }
            if (!flow.IsEncyclopediaOpen && !flow.IsArchiveOpen && (deskScene == null || !deskScene.IsOpening) && Keyboard.current?.escapeKey.wasPressedThisFrame == true) flow.GoBack();
        }

        private void Rebuild()
        {
            if (flow == null || canvas == null) return;
            if (hoverTooltip != null) hoverTooltip.HideAll();
            if (page != null) Destroy(page);
            volumeSlider = null;
            volumeValue = null;
            confirmationCountdown = null;
            buttons.Clear();
            deskScene = null;
            page = FormalUiKit.Create("页面_" + flow.CurrentStage, canvas.transform);
            RectTransform pageRect = page.AddComponent<RectTransform>(); FormalUiKit.Stretch(pageRect);
            if (flow.CurrentStage != FirstExperiencePrototypeController.FlowStage.Landing || !MainMenuDeskScene.HasCompleteSceneArt)
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
                case FirstExperiencePrototypeController.FlowStage.RunCreation: BuildRunCreation(); break;
                case FirstExperiencePrototypeController.FlowStage.TutorialChoice: BuildTutorialChoice(); break;
                case FirstExperiencePrototypeController.FlowStage.TutorialGuide: BuildTutorialGuide(); break;
                default: BuildHandoff(); break;
            }
            LinkNavigation();
            if(flow.CurrentStage!=FirstExperiencePrototypeController.FlowStage.FirstSettings &&
                flow.CurrentStage!=FirstExperiencePrototypeController.FlowStage.Branding)
                Hd2DContentGeometry.Attach(page);
            if (buttons.Count > 0) RuntimeUiEventSystem.Select(buttons[0].gameObject);
            signature = PageSignature();
            if (animatedStage != flow.CurrentStage &&
                flow.CurrentStage != FirstExperiencePrototypeController.FlowStage.Branding &&
                flow.CurrentStage != FirstExperiencePrototypeController.FlowStage.WorldOpening &&
                flow.CurrentStage != FirstExperiencePrototypeController.FlowStage.Landing)
                PixelPresentationMotion.EnterPage(page.transform, flow.AnimationIntensity);
            animatedStage = flow.CurrentStage;
        }

        private string PageSignature()
        {
            return flow.CurrentStage + "|" + flow.TutorialPage + "|" + flow.IsSelectingContinue + "|" + flow.IsPendingOverwrite + "|" +
                flow.ResolutionIndex + "|" + flow.DisplayModeLabel + "|" + flow.PendingVSync + "|" + flow.FrameRateLabel + "|" +
                flow.IsConfirmingDisplay + "|" +
                flow.DebugSelectedSlot + "|" + flow.DebugOpeningBrandMarkIndex + "|" + flow.HasUnfinishedRun + "|" +
                flow.HandoffError + "|" + flow.IsSlotWriteProtected(0) + flow.IsSlotWriteProtected(1) + flow.IsSlotWriteProtected(2);
        }

        private static void BuildBackground(Transform parent, FirstExperiencePrototypeController.FlowStage stage)
        {
            Image background = parent.gameObject.AddComponent<Image>();
            if(stage!=FirstExperiencePrototypeController.FlowStage.Branding)
            {
                AcademyDeskBackdrop.Attach(background,stage==FirstExperiencePrototypeController.FlowStage.Configuration);
                if (stage == FirstExperiencePrototypeController.FlowStage.FirstSettings ||
                    stage == FirstExperiencePrototypeController.FlowStage.AcademyIntro ||
                    stage == FirstExperiencePrototypeController.FlowStage.TutorialGuide)
                    background.GetComponent<AcademyDeskBackdrop>().SetDocumentFraming(-2f, .22f);
                background.raycastTarget=stage==FirstExperiencePrototypeController.FlowStage.FirstSettings;
                return;
            }
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
            Label("副标", "以太历 413 年", panel.transform, new Vector2(50, -208), new Vector2(880, 52), 24, FormalUiTheme.Muted, TextAnchor.MiddleCenter);
            AddButton("跳过品牌", "跳过品牌", panel.transform, new Vector2(650, -318), new Vector2(280, 64), flow.FinishBranding);
        }

        private void BuildSettings()
        {
            if (flow.IsConfirmingDisplay)
            {
                GameObject confirmation = CenterPanel(new Vector2(1100, 460));
                Label("确认标题", "保留画面设置？", confirmation.transform, new Vector2(50, -45), new Vector2(1000, 70), 40, FormalUiTheme.Text, TextAnchor.MiddleCenter);
                confirmationCountdown = Label("倒计时", flow.DisplayConfirmationSeconds + " 秒后自动恢复原设置", confirmation.transform, new Vector2(50, -145), new Vector2(1000, 70), 28, FormalUiTheme.Text, TextAnchor.MiddleCenter);
                AddButton("恢复画面设置", "恢复原设置", confirmation.transform, new Vector2(80, -290), new Vector2(420, 90), flow.RevertDisplaySettings);
                AddButton("保留画面设置", "保留设置", confirmation.transform, new Vector2(600, -290), new Vector2(420, 90), flow.ConfirmDisplaySettings);
                return;
            }
            GameObject heading = FormalUiKit.AnchoredPanel("设置页眉", page.transform, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(64, -64), new Vector2(1120, 104), ArchiveUiStyle.Paper);
            ArchiveUiStyle.PaperPanel(heading, ArchiveUiStyle.Paper, true);
            Label("标题", flow.IsSettingsFromLanding ? "辅助设置" : "首启设置", heading.transform,
                new Vector2(42, -18), new Vector2(1000, 70), 48, ArchiveUiStyle.Ink, TextAnchor.MiddleLeft);

            GameObject sheet = FormalUiKit.AnchoredPanel("设置档案", page.transform, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(64, -192), new Vector2(1120, 790), ArchiveUiStyle.LightPaper);
            ArchiveUiStyle.PaperPanel(sheet, ArchiveUiStyle.LightPaper, true);
            Label("卷宗标题", "本机偏好", sheet.transform, new Vector2(52, -20), new Vector2(600, 42), 28,
                ArchiveUiStyle.Ink, TextAnchor.MiddleLeft);
            FormalUiKit.Line(sheet.transform, new Vector2(52, -70), new Vector2(1016, 1), ArchiveUiStyle.Rule, "标题分隔");
            ArchiveVolumeSliderRow(sheet.transform, 88);
            ArchiveSettingRow(sheet.transform, 218, "分辨率", flow.ResolutionLabel, "切换", flow.CycleResolution, null, null);
            ArchiveSettingRow(sheet.transform, 320, "显示模式", flow.DisplayModeLabel, "更改", flow.CycleDisplayMode, null, null);
            ArchiveSettingRow(sheet.transform, 422, "垂直同步", flow.PendingVSync ? "开启" : "关闭", "更改", flow.ToggleVSync, null, null);
            ArchiveSettingRow(sheet.transform, 524, "帧率上限", flow.FrameRateLabel, "切换", flow.CycleFrameRate, null, null);
            buttons[buttons.Count - 1].interactable = !flow.PendingVSync;
            Button apply = AddButton("应用设置", "应用设置", sheet.transform, new Vector2(678, -672),
                new Vector2(390, 90), flow.ApplyPendingSettings, FormalUiTheme.Cyan);
            ArchiveUiStyle.ScrollButton(apply, () => UiMotionProfile.FromIntensity(flow.AnimationIntensity));
            if (flow.IsSettingsFromLanding)
                ArchiveUiStyle.TabButton(AddButton("取消", "返回", sheet.transform, new Vector2(52, -672),
                    new Vector2(280, 90), flow.GoBack), false,
                    () => UiMotionProfile.FromIntensity(flow.AnimationIntensity));
        }

        private void BuildOpening()
        {
            GameObject header = FormalUiKit.AnchoredPanel("开场抬头", page.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(36, -32), new Vector2(920, 96), FormalUiTheme.WithAlpha(FormalUiTheme.Ink, .92f));
            Label("标题", "以太历 413 年　学院北门", header.transform, new Vector2(28, -18), new Vector2(830, 52), 24, FormalUiTheme.OnInk, TextAnchor.MiddleLeft);
            AddButton("跳过动画", "跳过动画", page.transform, new Vector2(1580, -42), new Vector2(280, 70), flow.FinishWorldOpening);
        }

        private void BuildLanding()
        {
            if (!MainMenuDeskScene.HasCompleteSceneArt) { BuildLegacyLanding(); return; }
            deskScene = MainMenuDeskScene.Create(page.transform, () => flow.AnimationIntensity);
            buttons.AddRange(deskScene.Configure(flow.HasUnfinishedRun, flow.StartGameFromDesk,
                flow.ContinueGameFromDesk, flow.OpenJourneyArchive, flow.OpenSettings, flow.QuitToDesktop));
        }

        private void BuildLegacyLanding()
        {
            GameObject left = FormalUiKit.Panel("以太主界面主栏", page.transform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(1220, 1080), ArchiveUiStyle.Paper);
            RectTransform leftRect = left.GetComponent<RectTransform>(); leftRect.anchorMin = new Vector2(0, 0); leftRect.anchorMax = new Vector2(0, 1); leftRect.pivot = new Vector2(0, 1); leftRect.anchoredPosition = Vector2.zero;
            leftRect.sizeDelta = new Vector2(1220f, 0f);
            ArchiveUiStyle.PaperPanel(left, ArchiveUiStyle.Paper, false);
            FormalUiKit.FlatPanel("装订边", left.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(1206, 0), new Vector2(14, 1080), ArchiveUiStyle.Brass);
            Label("卷宗", "以太人生档案", left.transform, new Vector2(84, -92), new Vector2(1020, 80), 48, ArchiveUiStyle.Ink, TextAnchor.MiddleLeft);
            // Stage availability stays visible; additional guidance is opt-in.
            Button stage = LandingInfoIcon(left.transform);
            Label("当前人生阶段", "学院", left.transform, new Vector2(164, -212), new Vector2(200, 44),
                30, ArchiveUiStyle.QuietInk, TextAnchor.MiddleLeft);
            AttachLandingHint(stage.gameObject, "人生档案", "当前开放学院阶段。新建或继续档案，开始行程。");
            FormalUiKit.Line(left.transform, new Vector2(84, -276), new Vector2(1048, 2), ArchiveUiStyle.Rule, "卷宗分隔");
            Button start = AddButton("开始新游戏", "新建档案", left.transform, new Vector2(84, -330), new Vector2(520, 90), flow.StartNewRun, FormalUiTheme.Cyan);
            start.GetComponentInChildren<Text>().fontSize = 48;
            ArchiveUiStyle.ScrollButton(start, () => UiMotionProfile.FromIntensity(flow.AnimationIntensity));
            AttachLandingHint(start.gameObject, "新建档案", "先选择角色和档案位置；覆盖已有档案前会再次确认。");
            Button resume = AddButton("继续游戏", "继续档案", left.transform, new Vector2(632, -330), new Vector2(500, 90), flow.ContinueRun);
            resume.GetComponentInChildren<Text>().fontSize = 48;
            resume.interactable = flow.HasSave;
            ArchiveUiStyle.ScrollButton(resume, () => UiMotionProfile.FromIntensity(flow.AnimationIntensity));
            AttachLandingHint(resume.gameObject, "继续档案", "选择已有档案，恢复到上次的行程或战斗。");
            ArchiveUiStyle.TabButton(AddButton("百科", "战斗百科", left.transform, new Vector2(84, -452), new Vector2(330, 78), flow.OpenEncyclopedia, FormalUiTheme.Cyan), false,
                () => UiMotionProfile.FromIntensity(flow.AnimationIntensity));
            ArchiveUiStyle.TabButton(AddButton("设置", "辅助设置", left.transform, new Vector2(440, -452), new Vector2(330, 78), flow.OpenSettings), false,
                () => UiMotionProfile.FromIntensity(flow.AnimationIntensity));
            ArchiveUiStyle.TabButton(AddButton("重看开场", "重看世界观", left.transform, new Vector2(796, -452), new Vector2(336, 78), flow.ReplayWorldOpening), false,
                () => UiMotionProfile.FromIntensity(flow.AnimationIntensity));
            foreach (Button button in buttons)
            {
                Text label = button.GetComponentInChildren<Text>();
                if (label != null) label.fontSize = 36;
            }
            Button quit = AddButton("退出游戏", "退出游戏", left.transform, new Vector2(84, -954),
                new Vector2(330, 72), flow.QuitToDesktop);
            ArchiveUiStyle.TabButton(quit, false, () => UiMotionProfile.FromIntensity(flow.AnimationIntensity));
            quit.GetComponentInChildren<Text>().fontSize = 30;
            AttachLandingHint(quit.gameObject, "退出游戏", "关闭游戏并返回桌面。");
            for (int slot = 0; slot < 3; slot++)
            {
                float x = 84 + slot * 360;
                GameObject card = FormalUiKit.Panel("档案卡" + slot, left.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, -674), new Vector2(320, 238), FormalUiTheme.SurfaceRaised);
                ArchiveUiStyle.NotePanel(card);
                Label("编号", "档案 0" + (slot + 1), card.transform, new Vector2(46, -22), new Vector2(250, 44), 24, ArchiveUiStyle.Ink, TextAnchor.MiddleLeft);
                Text status = Label("状态", flow.SlotExists(slot) ? flow.SlotDisplaySummary(slot) : "空白卷宗", card.transform, new Vector2(30, -82), new Vector2(260, 132), 24, flow.SlotExists(slot) ? FormalUiTheme.Safe : ArchiveUiStyle.QuietInk, TextAnchor.UpperLeft);
                status.fontSize = FormalUiTheme.BodyFontSize;
            }
            buttons.Add(stage);
        }

        private Button LandingInfoIcon(Transform parent)
        {
            GameObject node = new GameObject("按钮_人生阶段图标", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            node.transform.SetParent(parent, false);
            RectTransform rect = node.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(84f, -202f);
            rect.sizeDelta = new Vector2(64f, 64f);
            Image icon = node.GetComponent<Image>();
            icon.sprite = Resources.Load<Sprite>(FormalUiEffectsConfig.ChapterMarkerPath("teaching_chalk_clip"));
            icon.preserveAspect = true;
            Button button = node.GetComponent<Button>();
            button.targetGraphic = icon;
            button.transition = UnityEngine.UI.Selectable.Transition.None;
            node.AddComponent<UiButtonFeedback>().Configure(button, Color.white, Color.white, Color.white,
                Color.white, Color.gray, () => UiMotionProfile.FromIntensity(flow.AnimationIntensity), null);
            return button;
        }

        private void AttachLandingHint(GameObject target, string title, string explanation)
        {
            if (hoverTooltip == null) hoverTooltip = FormalHoverTooltip.Create(canvas);
            target.AddComponent<FormalHoverTooltipTrigger>().Configure(hoverTooltip,
                () => new FormalTooltipContent(title, explanation, FormalUiTheme.Amber));
        }

        private void BuildSlots()
        {
            Header(flow.IsSelectingContinue ? "选择继续档案" : "选择新档案位置",
                flow.IsSelectingContinue ? "只显示可继续的卷宗。" : "已有卷宗会在下一步要求确认覆盖。");
            if (!string.IsNullOrEmpty(flow.HandoffError))
                Label("档案提示", flow.HandoffError, page.transform, new Vector2(140, -226), new Vector2(1640, 64),
                    22, FormalUiTheme.Danger, TextAnchor.UpperLeft);
            for (int slot = 0; slot < 3; slot++)
            {
                int captured = slot; bool exists = flow.SlotExists(slot);
                GameObject card = FormalUiKit.AnchoredPanel("档案选择卡" + slot, page.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(140 + slot * 570, -300), new Vector2(500, 390), FormalUiTheme.SurfaceRaised);
                ArchiveUiStyle.NotePanel(card);
                Label("编号", "档案 0" + (slot + 1), card.transform, new Vector2(52, -34), new Vector2(410, 52), 24, ArchiveUiStyle.Brass, TextAnchor.MiddleLeft);
                FormalUiKit.Line(card.transform, new Vector2(32, -94), new Vector2(436, 1), ArchiveUiStyle.Rule, "档案分隔");
                Label("摘要", exists ? flow.SlotDisplaySummary(slot) : "空白卷宗\n尚未建立学生档案", card.transform, new Vector2(32, -114), new Vector2(430, 130), 24, exists ? ArchiveUiStyle.Ink : ArchiveUiStyle.QuietInk, TextAnchor.UpperLeft);
                bool protectedSlot = flow.IsSelectingContinue && flow.IsSlotWriteProtected(slot);
                Button choose = AddButton("选择档案" + slot, flow.IsSelectingContinue ? "继续" : exists ? "覆盖此档案" : "在此建立", card.transform,
                    new Vector2(32, protectedSlot ? -242 : -282), new Vector2(436, protectedSlot ? 58 : 72), () => flow.ChooseSlot(captured));
                choose.interactable = !flow.IsSelectingContinue || exists;
                if (protectedSlot)
                    AddButton("恢复档案" + slot, "验证并恢复旧档", card.transform, new Vector2(32, -312), new Vector2(436, 58),
                        () => flow.RecoverProtectedSlot(captured), FormalUiTheme.Cyan);
            }
            AddButton("返回", "返回以太主界面", page.transform, new Vector2(140, -850), new Vector2(360, 80), flow.GoBack);
        }

        private void BuildConfirmation()
        {
            GameObject card = CenterPanel(new Vector2(1120, 420));
            Label("标题", flow.IsPendingOverwrite ? "开始新的游戏？" : "确认建立档案", card.transform, new Vector2(56, -54), new Vector2(1008, 74), 48, flow.IsPendingOverwrite ? FormalUiTheme.Danger : ArchiveUiStyle.Ink, TextAnchor.MiddleLeft);
            FormalUiKit.Line(card.transform, new Vector2(56, -146), new Vector2(1008, 1), ArchiveUiStyle.Rule, "确认分隔");
            Label("正文", flow.IsPendingOverwrite ? "当前未完成的轮次将被新轮次替换。确认后选择角色；确认角色前不会覆盖原记录。" : "建立固定学生档案并进入基础配置。", card.transform, new Vector2(56, -166), new Vector2(1008, 110), 24, ArchiveUiStyle.Ink, TextAnchor.UpperLeft);
            AddButton("取消", "取消", card.transform, new Vector2(56, -292), new Vector2(360, 78), flow.GoBack);
            Button confirm = AddButton("确认", flow.IsPendingOverwrite ? "确认覆盖" : "确认建立", card.transform, new Vector2(632, -292), new Vector2(432, 78), flow.ConfirmSelectedSlot, flow.IsPendingOverwrite ? FormalUiTheme.Danger : FormalUiTheme.Cyan);
            if (!flow.IsPendingOverwrite)
                ArchiveUiStyle.ScrollButton(confirm, () => UiMotionProfile.FromIntensity(flow.AnimationIntensity));
            RuntimeUiEventSystem.Select(buttons[0].gameObject);
        }

        private void BuildConfiguration()
        {
            var template=Resources.Load<CharacterDossierView>(CharacterDossierView.ResourcePath);
            if(template==null)throw new InvalidOperationException("Missing character dossier prefab");
            var dossier=Instantiate(template,page.transform,false);
            buttons.AddRange(dossier.Bind(flow));
        }
        private void BuildAcademyIntro()
        {
            Header("学院阶段　入学", "维克多的学院生活从这里开始。");
            GameObject module = FormalUiKit.AnchoredPanel("学院说明", page.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(64, -236), new Vector2(1120, 700), ArchiveUiStyle.LightPaper);
            ArchiveUiStyle.PaperPanel(module, ArchiveUiStyle.LightPaper, true);
            Label("标题", "维克多的入学档案", module.transform, new Vector2(60, -50), new Vector2(1000, 74), 46, ArchiveUiStyle.Ink, TextAnchor.MiddleLeft);
            Label("背景", "学院以可测量的以太工程为基础，课程从器材维护和实地安全开始。", module.transform,
                new Vector2(60, -160), new Vector2(1000, 130), 27, ArchiveUiStyle.QuietInk, TextAnchor.UpperLeft);
            Label("入学", "维克多从雾桥村来到学院。通过公开考核后，他一边工读，一边学习如何让火系术式真正用于修缮。", module.transform,
                new Vector2(60, -320), new Vector2(1000, 180), 27, ArchiveUiStyle.QuietInk, TextAnchor.UpperLeft);
            ArchiveUiStyle.ScrollButton(AddButton("进入首次地图", "进入学院地图", module.transform, new Vector2(560, -556), new Vector2(500, 88), flow.EnterAcademy, FormalUiTheme.Cyan),
                () => UiMotionProfile.FromIntensity(flow.AnimationIntensity));
        }

        private void BuildTutorialChoice()
        {
            GameObject card = CenterPanel(new Vector2(1160, 580));
            Label("教程标题", "需要操作教程吗？", card.transform, new Vector2(60, -50), new Vector2(1040, 80), 46, ArchiveUiStyle.Ink, TextAnchor.MiddleCenter);
            Label("教程说明", "初次来到学院？先了解地图，再在首战中跟随高亮练习移动与施法。\n\n选择会保存在当前档案中，也可随时跳过后续教学。无需教程仍可体验学院入门关卡。", card.transform,
                new Vector2(80, -180), new Vector2(1000, 190), 28, ArchiveUiStyle.QuietInk, TextAnchor.UpperLeft);
            AddButton("开启教程", "开启教程", card.transform, new Vector2(640, -430), new Vector2(430, 90), () => flow.ChooseTutorial(true), FormalUiTheme.Cyan);
            AddButton("不需要教程", "不需要教程", card.transform, new Vector2(80, -430), new Vector2(430, 90), () => flow.ChooseTutorial(false));
        }

        private void BuildTutorialGuide()
        {
            string[] titles = { "从学院地图出发", "先看敌人的公开意图", "移动也需要行动点", "施法前检查结果预览", "主动结束回合与保存" };
            string[] text = {
                "学院地图连接着战斗、事件、工坊、医务室与商店。\n\n先阅读出发节点，再选择已开放的战斗节点，查看遭遇说明后进入战斗。战后完成奖励选择，才能继续探索。",
                "在战场上选择敌人，查看右侧的公开意图。\n\n留意它准备攻击的位置、路径与范围，再决定站位。公开意图描述敌人的一个完整回合；尽量离开危险区域或提前处理威胁。",
                "获得操作权后，选择移动，查看可达格与路径。\n\n确认落点之前先检查行动点费用；地形可能改变路径成本。资源允许时可以连续行动，取消预览可以重新选择。",
                "选择底部术式，再选择合法目标或位置。\n\n确认前检查效果预览、行动点、魔力与冷却。右侧术式说明和战斗百科可以帮助你理解术式；取消预览后可换一个方案。",
                "行动完成后，用结束回合推进战斗，留意行动条与敌人意图。\n\n地图选择、战果和奖励等完成操作会保存。退出后使用继续档案返回；战斗失败时按结算界面的重试入口继续。"
            };
            int index = flow.TutorialPage;
            GameObject card = FormalUiKit.AnchoredPanel("教程文件", page.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(64, -140), new Vector2(1120, 820), ArchiveUiStyle.LightPaper);
            ArchiveUiStyle.PaperPanel(card, ArchiveUiStyle.LightPaper, true);
            Label("教程进度", "入门操作教程　第 " + (index + 1) + " 页　共 5 页", card.transform, new Vector2(70, -38), new Vector2(980, 48), 24, ArchiveUiStyle.Brass, TextAnchor.MiddleLeft);
            Label("教程标题", titles[index], card.transform, new Vector2(70, -110), new Vector2(980, 88), 44, ArchiveUiStyle.Ink, TextAnchor.MiddleLeft);
            FormalUiKit.Line(card.transform, new Vector2(70, -220), new Vector2(980, 2), ArchiveUiStyle.Rule, "教程分隔");
            Label("教程内容", text[index], card.transform, new Vector2(80, -270), new Vector2(960, 350), 32, ArchiveUiStyle.QuietInk, TextAnchor.UpperLeft);
            AddButton("教程下一页", index == 4 ? "开始探索" : "下一页", card.transform, new Vector2(750, -676), new Vector2(300, 86), flow.AdvanceTutorial, FormalUiTheme.Cyan);
            if (index > 0) AddButton("教程上一页", "上一页", card.transform, new Vector2(80, -676), new Vector2(260, 86), flow.PreviousTutorialPage);
            AddButton("结束教程", "结束教程", card.transform, new Vector2(380, -676), new Vector2(260, 86), flow.FinishTutorial);
        }

        private void BuildRunCreation()
        {
            Header("进入学院失败", "当前结算档案已保留");
            GameObject card = CenterPanel(new Vector2(1120, 430));
            Label("内容", "新一轮尚未建立。可重试，或返回主界面。", card.transform,
                new Vector2(64, -90), new Vector2(990, 170), 28, ArchiveUiStyle.Ink, TextAnchor.UpperLeft);
            if (!string.IsNullOrEmpty(flow.HandoffError))
                Label("保存提示", flow.HandoffError, card.transform,
                    new Vector2(64, -270), new Vector2(990, 80), 22, FormalUiTheme.Danger, TextAnchor.UpperLeft);
            AddButton("返回入口", "返回以太主界面", card.transform,
                new Vector2(64, -330), new Vector2(410, 74), flow.GoBack);
            AddButton("重试新一轮", "重试进入学院", card.transform,
                new Vector2(610, -330), new Vector2(440, 74), flow.ConfirmSubsequentRound, FormalUiTheme.Cyan);
        }

        private void BuildHandoff()
        {
            Header("进入学院地图", "正在准备旅程");
            if (!string.IsNullOrEmpty(flow.HandoffError))
                Label("进入失败说明", flow.HandoffError, page.transform, new Vector2(170, -280), new Vector2(1580, 140),
                    26, FormalUiTheme.Danger, TextAnchor.UpperLeft);
            AddButton("返回入口", "返回以太主界面", page.transform, new Vector2(140, -850), new Vector2(360, 80), flow.ShowLanding);
            AddButton("重试进入地图", "重试进入地图", page.transform, new Vector2(1310, -850), new Vector2(470, 86), flow.EnterAcademy, FormalUiTheme.Cyan);
        }

        private void Header(string title, string subtitle)
        {
            GameObject header = FormalUiKit.AnchoredPanel("页面抬头", page.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(64, -64), new Vector2(1120, 150), ArchiveUiStyle.Paper);
            ArchiveUiStyle.PaperPanel(header, ArchiveUiStyle.Paper, true);
            Label("标题", title, header.transform, new Vector2(42, -22), new Vector2(1036, 70), 48, ArchiveUiStyle.Ink, TextAnchor.MiddleLeft);
            Label("副标题", subtitle, header.transform, new Vector2(42, -92), new Vector2(1036, 42), 24, ArchiveUiStyle.QuietInk, TextAnchor.MiddleLeft);
        }

        private GameObject CenterPanel(Vector2 size)
        {
            GameObject panel = FormalUiKit.AnchoredPanel("中心档案卡", page.transform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, size, ArchiveUiStyle.LightPaper);
            ArchiveUiStyle.PaperPanel(panel, ArchiveUiStyle.LightPaper, true);
            return panel;
        }

        private void SettingRow(Transform parent, float y, string name, string value, string leftLabel, Action left, string rightLabel, Action right)
        {
            GameObject row = FormalUiKit.Panel("设置行_" + name, parent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(52, -y), new Vector2(1336, 126), FormalUiTheme.Surface);
            Label("名称", name, row.transform, new Vector2(30, -28), new Vector2(360, 58), 24, FormalUiTheme.Text, TextAnchor.MiddleLeft);
            Label("值", value, row.transform, new Vector2(430, -28), new Vector2(390, 58), 24, FormalUiTheme.Cyan, TextAnchor.MiddleCenter);
            if (left != null) AddButton("设置_" + name + "_左", leftLabel, row.transform, new Vector2(850, -24), new Vector2(right == null ? 430 : 200, 70), left);
            if (right != null) AddButton("设置_" + name + "_右", rightLabel, row.transform, new Vector2(1080, -24), new Vector2(220, 70), right);
        }

        private void ArchiveSettingRow(Transform parent, float y, string name, string value,
            string leftLabel, Action left, string rightLabel, Action right)
        {
            GameObject row = FormalUiKit.Panel("设置行_" + name, parent, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(52, -y), new Vector2(1016, 94), ArchiveUiStyle.LightPaper);
            ArchiveUiStyle.PaperContent(row);
            Label("名称", name, row.transform, new Vector2(18, -30), new Vector2(260, 58), 26,
                ArchiveUiStyle.Ink, TextAnchor.MiddleLeft);
            Label("值", value, row.transform, new Vector2(290, -30), new Vector2(330, 58), 26,
                ArchiveUiStyle.Brass, TextAnchor.MiddleCenter);
            if (left != null)
                ArchiveUiStyle.TabButton(AddButton("设置_" + name + "_左", leftLabel, row.transform,
                    new Vector2(660, -28), new Vector2(right == null ? 328 : 150, 70), left), false,
                    () => UiMotionProfile.FromIntensity(flow.AnimationIntensity));
            if (right != null)
                ArchiveUiStyle.TabButton(AddButton("设置_" + name + "_右", rightLabel, row.transform,
                    new Vector2(836, -28), new Vector2(152, 70), right), false,
                    () => UiMotionProfile.FromIntensity(flow.AnimationIntensity));
            FormalUiKit.Line(parent, new Vector2(70, -y - 100), new Vector2(980, 1), ArchiveUiStyle.Rule, "设置分隔");
        }

        private void ArchiveVolumeSliderRow(Transform parent, float y)
        {
            GameObject row = FormalUiKit.Panel("设置行_主音量", parent, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(52, -y), new Vector2(1016, 100), ArchiveUiStyle.LightPaper);
            ArchiveUiStyle.PaperContent(row);
            Label("名称", "主音量", row.transform, new Vector2(18, -30), new Vector2(230, 58), 26,
                ArchiveUiStyle.Ink, TextAnchor.MiddleLeft);
            volumeValue = Label("值", string.Empty, row.transform, new Vector2(890, -30), new Vector2(100, 58), 26,
                ArchiveUiStyle.Brass, TextAnchor.MiddleRight);
            GameObject sliderObject = FormalUiKit.Panel("主音量滑动条", row.transform, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(270, -32), new Vector2(600, 62), Color.clear);
            Image background = FormalUiKit.Panel("轨道", sliderObject.transform, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(8, -25), new Vector2(584, 12), ArchiveUiStyle.Rule).GetComponent<Image>();
            background.raycastTarget = false;
            RectTransform fillArea = FormalUiKit.Panel("填充区", sliderObject.transform, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(8, -25), new Vector2(584, 12), Color.clear).GetComponent<RectTransform>();
            Image fill = FormalUiKit.Panel("已设置音量", fillArea, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, FormalUiTheme.Cyan).GetComponent<Image>();
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;
            fill.raycastTarget = false;
            RectTransform handleArea = FormalUiKit.Panel("滑块范围", sliderObject.transform, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, Color.clear).GetComponent<RectTransform>();
            handleArea.offsetMin = new Vector2(8, 0);
            handleArea.offsetMax = new Vector2(-8, 0);
            Image handle = FormalUiKit.Panel("滑块", handleArea, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                Vector2.zero, new Vector2(36, 48), ArchiveUiStyle.Brass).GetComponent<Image>();
            volumeSlider = sliderObject.AddComponent<Slider>();
            volumeSlider.targetGraphic = handle;
            volumeSlider.fillRect = fill.rectTransform;
            volumeSlider.handleRect = handle.rectTransform;
            volumeSlider.direction = Slider.Direction.LeftToRight;
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.SetValueWithoutNotify(flow.PendingVolume);
            volumeSlider.onValueChanged.AddListener(value => { if (flow != null) flow.PendingVolume = value; });
            volumeValue.text = Mathf.RoundToInt(flow.PendingVolume * 100f) + "%";
            FormalUiKit.Line(parent, new Vector2(70, -y - 110), new Vector2(980, 1), ArchiveUiStyle.Rule, "设置分隔");
        }

        private Button AddButton(string name, string title, Transform parent, Vector2 position, Vector2 size, Action action, Color? tone = null)
        {
            Color surface = tone ?? FormalUiTheme.Interactive;
            Button button = FormalUiKit.Button("按钮_" + name, title, parent, position, size, surface);
            button.onClick.AddListener(() => action?.Invoke());
            FormalUiKit.ConfigureButtonFeedback(button, FormalUiButtonPalette.ForAccent(surface, FormalUiTheme.Cyan),
                () => UiMotionProfile.FromIntensity(flow == null ? 1f : flow.AnimationIntensity), null);
            ArchiveUiStyle.TabButton(button, false, () => UiMotionProfile.FromIntensity(flow == null ? 1f : flow.AnimationIntensity));
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
