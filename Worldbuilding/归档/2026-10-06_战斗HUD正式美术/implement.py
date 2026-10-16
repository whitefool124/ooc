from pathlib import Path
root=Path(__file__).resolve().parents[3]
p=root/'UnityProject/Assets/Game/Runtime/Presentation/FormalCombatHud.cs';s=p.read_text(encoding='utf-8-sig')
s=s.replace('private const float HeroBarTop = 40f','private const float HeroBarTop = 52f')
s=s.replace('private RawImage heroPortrait;', 'private RawImage heroPortrait;\n        private CombatPortraitCardView heroIdentity;\n        private readonly CombatPortraitCardView[] orderCards = new CombatPortraitCardView[7];\n        private RectTransform orderRoot;')
for title,field,col,mult in [('生命','healthValue','HealthFill',''),('护盾','shieldValue','ShieldFill',' + HeroBarPitch'),('个人魔力','manaValue','MagicFill',' + HeroBarPitch * 2f')]:
 old=f'ResourceBar(heroFront.transform, "{title}", new Vector2(16, -(HeroBarTop{mult})), FormalUiTheme.Resource{col}, out {field})' if mult else f'ResourceBar(heroFront.transform, "{title}", new Vector2(16, -HeroBarTop), FormalUiTheme.Resource{col}, out {field})'
 assert old in s,old
 s=s.replace(old,old.replace('new Vector2(16,','new Vector2(160,').replace(f'out {field})',f'out {field}, 304f)'))
s=s.replace('new Vector2(296, -3), new Vector2(104, 36), "状态"','new Vector2(352, -8), new Vector2(112, 36), "详情"',1)
start=s.index('        private void BuildCompactHeroFront()');end=s.index('        private void BuildHeroBack()',start)
s=s[:start]+'''        private void BuildCompactHeroFront()
        {
            FormalUiKit.ApplySkin(heroModule.GetComponent<Image>(), "panel", Color.white);
            heroName = Label("人物姓名", heroFront.transform, new Vector2(16, -8), new Vector2(328, 36), 24, text, TextAnchor.MiddleLeft);
            heroIdentity = CombatPortraitCardView.Create(heroFront.transform);
            heroIdentity.name = "人物立绘头像";
            RectTransform rect = (RectTransform)heroIdentity.transform;
            rect.anchoredPosition = new Vector2(16, -52); rect.sizeDelta = new Vector2(128, 128);
            heroIdentity.Caption.gameObject.SetActive(false);
            heroIdentity.Initialize(() => bootstrap.IsInteractionModalOpen, id => bootstrap.SetTimelineHoveredUnit(id), id => bootstrap.FocusBattlefieldOnUnit(id));
            heroPortrait = heroIdentity.Portrait;
            BindTooltip(heroIdentity.gameObject, () => heroIdentity.Tooltip(0, false));
            Line(heroFront.transform, new Vector2(16, -236), new Vector2(448, 2), line);
            for (int i = 0; i < compactStatusIcons.Length; i++)
            {
                int captured = i;
                GameObject chip = FormalUiKit.FlatPanel("人物状态图标" + i, heroFront.transform, new Vector2(0, 1), new Vector2(0, 1),
                    new Vector2(16 + i * 72, -248), new Vector2(68, 28), Color.clear);
                chip.GetComponent<Image>().raycastTarget = true;
                compactStatusIcons[i] = FormalUiKit.TopLeftIconSlot("状态图标", chip.transform, null, Vector2.zero);
                compactStatusIcons[i].rectTransform.sizeDelta = new Vector2(24, 24);
                Label("状态简称", chip.transform, new Vector2(28, 0), new Vector2(40, 28), 18, text, TextAnchor.MiddleLeft);
                BindTooltip(chip, () => BuildHeroStatusEntryTooltip(captured));
            }
            compactStatusOverflow = Label("状态空态", heroFront.transform, new Vector2(16, -248), new Vector2(448, 28), 18, muted, TextAnchor.MiddleLeft);
            compactStatusOverflow.raycastTarget = true;
            BindTooltip(compactStatusOverflow.gameObject, () => new FormalTooltipContent("全部状态", heroStatusRows.Count == 0 ? "暂无持续状态" : string.Join("\\n", heroStatusRows.Select(row => row.Label + "：" + row.Body)), FormalUiTheme.Cyan));
        }

        private void BuildActionOrder(Transform parent)
        {
            orderRoot = (RectTransform)parent;
            Image background = parent.GetComponent<Image>(); background.color = Color.clear; background.raycastTarget = false;
            Image skin = FormalUiKit.SkinOverlay(background); if (skin != null) skin.gameObject.SetActive(false);
            actionOrderCaption = Label("行动顺序标题", parent, new Vector2(16, -2), new Vector2(240, 28), 18, text, TextAnchor.MiddleLeft);
            actionOrderCaption.text = "预计行动顺序";
            actionOrderCaption.raycastTarget = true;
            BindTooltip(actionOrderCaption.gameObject, () => new FormalTooltipContent("预计行动顺序", "当前行动者在前，每名存活单位显示一次。读取当前行动值和速度；后续速度变化与敌方行动可能改变顺序。", FormalUiTheme.Cyan));
            for (int i = 0; i < orderCards.Length; i++)
            {
                CombatPortraitCardView card = CombatPortraitCardView.Create(parent); orderCards[i] = card;
                card.name = "立绘顺序卡" + (i + 1);
                card.Initialize(() => bootstrap.IsInteractionModalOpen || bootstrap.IsCombatOutcomeVisible,
                    id => bootstrap.SetTimelineHoveredUnit(id), id => bootstrap.FocusBattlefieldOnUnit(id));
                BindTooltip(card.gameObject, () => { var order = CombatActionOrderPresentation.Build(bootstrap.CurrentState, bootstrap.CurrentActionPreview); int ordinal = order.ToList().FindIndex(unit => unit.Id == card.UnitId); return card.Tooltip(Math.Max(0, ordinal), bootstrap.CurrentActionPreview?.HasActionValuePreview == true); });
            }
            actionOrderOverflow = Label("顺序剩余", parent, new Vector2(0, -80), new Vector2(64, 40), 24, text, TextAnchor.MiddleCenter);
            actionOrderOverflow.raycastTarget = true;
            BindTooltip(actionOrderOverflow.gameObject, () => new FormalTooltipContent("其余行动单位", string.Join("\\n", CombatActionOrderPresentation.Build(bootstrap.CurrentState, bootstrap.CurrentActionPreview).Skip(7).Select((unit, index) => (index + 8) + " " + unit.DisplayName)), FormalUiTheme.Cyan));
        }

        private void RefreshActionOrder(CombatState state, CombatActionPreview preview)
        {
            IReadOnlyList<UnitState> order = CombatActionOrderPresentation.Build(state, preview);
            IReadOnlyList<UnitState> baseline = CombatActionOrderPresentation.Build(state);
            bool projected = preview != null && preview.CanSubmit && preview.HasActionValuePreview;
            actionOrderCaption.text = projected ? "预计行动顺序  预览" : "预计行动顺序";
            int count = Math.Min(7, order.Count); bool overflow = order.Count > 7;
            float width = 32 + count * 136 - 8 + (overflow ? 72 : 0);
            orderRoot.sizeDelta = new Vector2(width, 192); orderRoot.anchoredPosition = new Vector2(1904 - width, -80);
            var used = new HashSet<CombatPortraitCardView>(); var visible = new List<CombatPortraitCardView>();
            for (int i = 0; i < count; i++)
            {
                UnitState unit = order[i];
                CombatPortraitCardView card = orderCards.FirstOrDefault(value => value.UnitId == unit.Id);
                if (card == null) card = orderCards.First(value => !used.Contains(value) && !order.Take(count).Any(u => u.Id == value.UnitId));
                used.Add(card); visible.Add(card); card.gameObject.SetActive(true);
                int previous = baseline.ToList().FindIndex(value => value.Id == unit.Id);
                card.Bind(unit, i, projected ? previous - i : 0, bootstrap.SelectedTargetId == unit.Id);
                card.MoveTo(i, UiMotionProfile.FromIntensity(bootstrap.UiPreferences.AnimationIntensity));
            }
            foreach (CombatPortraitCardView card in orderCards) if (!used.Contains(card)) card.gameObject.SetActive(false);
            for (int i = 0; i < visible.Count; i++) visible[i].Button.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = i > 0 ? visible[i - 1].Button : heroFlipButton, selectOnRight = i + 1 < visible.Count ? visible[i + 1].Button : null, selectOnDown = endTurnButton };
            actionOrderOverflow.gameObject.SetActive(overflow); actionOrderOverflow.text = "+" + (order.Count - 7);
            actionOrderOverflow.rectTransform.anchoredPosition = new Vector2(width - 72, -80);
        }

''' +s[end:]
s=s.replace('heroPortrait.texture = TimelinePortrait(hero);','heroIdentity.Bind(hero, state.ActiveUnitId == hero.Id ? 0 : 1, 0, false);')
s=s.replace('timelinePortraits[slot].texture = TimelinePortrait(state.GetUnit(entry.UnitId));','CombatHudPortraitAssets.Bind(timelinePortraits[slot], state.GetUnit(entry.UnitId));')
s=s.replace('new Vector2(416, showBack ? 280 : 248)','new Vector2(480, showBack ? 280 : 288)')
s=s.replace('new Vector2(384, 28), 18, muted','new Vector2(448, 28), 18, muted')
s=s.replace('compactStatusOverflow.text = heroStatusRows.Count == 0 ? "无持续状态" : "";', '''compactStatusOverflow.text = heroStatusRows.Count == 0 ? "无持续状态" : heroStatusRows.Count > 5 ? "+" + (heroStatusRows.Count - 5) : "";
            compactStatusOverflow.rectTransform.anchoredPosition = new Vector2(heroStatusRows.Count > 0 ? 384 : 16, -248);
            compactStatusOverflow.rectTransform.sizeDelta = new Vector2(heroStatusRows.Count > 0 ? 80 : 448, 28);''')
s=s.replace('                if (heroStatusRows.Count >= HeroStatusEntryCount) break;\n','')
s=s.replace('if (pending != null && heroStatusRows.Count < HeroStatusEntryCount)','if (pending != null)')
s=s.replace('Color fillColor, out Text valueLabel)','Color fillColor, out Text valueLabel, float width = 384f)')
s=s.replace('new Vector2(374, 32), CombatHudTypography.ResourceValueFontSize','new Vector2(width - 8, 32), CombatHudTypography.ResourceValueFontSize')
s=s.replace('new Vector2(384f, 24f)','new Vector2(width, 24f)')
# No battle-sprite lookup remains anywhere in the HUD.
start=s.index('        private static Texture TimelinePortrait(');end=s.index('        private void RefreshHistory(',start);s=s[:start]+s[end:]
p.write_text(s,encoding='utf-8')
p=root/'UnityProject/Assets/Game/Runtime/Presentation/FormalBattlefieldView.cs';s=p.read_text(encoding='utf-8-sig').replace('new Vector2(BattlefieldPresentationAdapter.BattlefieldWidth - 132f, -112f)','new Vector2(688f, -8f)');p.write_text(s,encoding='utf-8')
p=root/'UnityProject/Assets/Game/Tests/EditMode/FormalCombatHudShellPrefabTests.cs';s=p.read_text(encoding='utf-8-sig').replace('new Vector2(416, 248)','new Vector2(480, 288)').replace('new Vector2(824, 96)','new Vector2(976, 192)');p.write_text(s,encoding='utf-8')
print('Implemented portraits and content-sized identity order')
