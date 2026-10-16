from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
path=ROOT/'UnityProject/Assets/Game/Runtime/Presentation/FormalFirstExperienceUi.cs'
s=path.read_text(encoding='utf-8')
s=s.replace('private FormalHoverTooltip hoverTooltip;','private FormalHoverTooltip hoverTooltip;\n        private MainMenuDeskScene deskScene;')
s=s.replace('flow.IsEncyclopediaOpen && hoverTooltip', '(flow.IsEncyclopediaOpen || flow.IsArchiveOpen) && hoverTooltip')
s=s.replace('if (!flow.IsEncyclopediaOpen && Keyboard.current?', 'if (!flow.IsEncyclopediaOpen && !flow.IsArchiveOpen && (deskScene == null || !deskScene.IsOpening) && Keyboard.current?')
s=s.replace('buttons.Clear();','buttons.Clear();\n            deskScene = null;',1)
s=s.replace('flow.DebugOpeningBrandMarkIndex + "|" + flow.HasSave', 'flow.DebugOpeningBrandMarkIndex + "|" + flow.HasUnfinishedRun')
start=s.index('        private void BuildLanding()')
end=s.index('        private Button LandingInfoIcon',start)
s=s[:start]+'''        private void BuildLanding()
        {
            deskScene = MainMenuDeskScene.Create(page.transform, () => flow.AnimationIntensity);
            GameObject menu = FormalUiKit.Create("窗边主菜单", page.transform);
            RectTransform menuRect = menu.AddComponent<RectTransform>(); FormalUiKit.Stretch(menuRect);
            CanvasGroup group = menu.AddComponent<CanvasGroup>();
            deskScene.BindMenu(group);
            Label("游戏标题", "以太", menu.transform, new Vector2(108, -94), new Vector2(430, 144),
                96, ArchiveUiStyle.Ink, TextAnchor.MiddleCenter);
            float y = flow.HasUnfinishedRun ? 330f : 432f;
            GameObject sheet = FormalUiKit.AnchoredPanel("小型纸页菜单", menu.transform,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(72, -y),
                new Vector2(496, flow.HasUnfinishedRun ? 594 : 492), ArchiveUiStyle.LightPaper);
            ArchiveUiStyle.PaperPanel(sheet, ArchiveUiStyle.LightPaper, false);
            float row = 30f;
            if (flow.HasUnfinishedRun)
            {
                Button resume = AddButton("继续游戏", "继续游戏", sheet.transform, new Vector2(28, -row),
                    new Vector2(440, 84), () => deskScene.BeginOpen(flow.ContinueGameFromDesk));
                ArchiveUiStyle.ScrollButton(resume, () => UiMotionProfile.FromIntensity(flow.AnimationIntensity));
                resume.GetComponentInChildren<Text>().fontSize = 36;
                row += 102f;
            }
            Button start = AddButton("开始游戏", "开始游戏", sheet.transform, new Vector2(28, -row),
                new Vector2(440, 84), () => deskScene.BeginOpen(flow.StartGameFromDesk));
            ArchiveUiStyle.ScrollButton(start, () => UiMotionProfile.FromIntensity(flow.AnimationIntensity));
            start.GetComponentInChildren<Text>().fontSize = 36;
            row += 112f;
            AddButton("行程与行囊", "行程与行囊", sheet.transform, new Vector2(28, -row),
                new Vector2(440, 76), flow.OpenJourneyArchive);
            row += 102f;
            AddButton("设置", "设置", sheet.transform, new Vector2(28, -row),
                new Vector2(440, 76), flow.OpenSettings);
            row += 102f;
            AddButton("退出游戏", "退出游戏", sheet.transform, new Vector2(28, -row),
                new Vector2(440, 76), flow.QuitToDesktop);
        }

''' +s[end:]
# The run abandonment warning is accurate for the approved run-based entry.
s=s.replace('flow.IsPendingOverwrite ? "确认覆盖档案" : "确认建立档案"','flow.IsPendingOverwrite ? "开始新的游戏？" : "确认建立档案"')
s=s.replace('"原有进度将被新档案替换。该操作不可撤回。"','"当前未完成的轮次将被新轮次替换。确认后选择角色；确认角色前不会覆盖原记录。"')
path.write_text(s,encoding='utf-8')
print('Patched only landing layout, archive overlay input guard and run confirmation copy.')
