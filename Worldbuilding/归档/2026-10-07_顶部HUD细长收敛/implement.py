from pathlib import Path
import json
root=Path(__file__).resolve().parents[3];f=Path(__file__).resolve().parent
def edit(path, pairs):
    p=root/path;s=p.read_text(encoding='utf-8-sig')
    for old,new in pairs:
        assert old in s,(path,old);s=s.replace(old,new)
    p.write_text(s,encoding='utf-8')
edit('UnityProject/Assets/Game/Runtime/Presentation/FormalCombatHud.cs',[
('HeroBarTop = 52f','HeroBarTop = 40f'),('compactStatusIcons = new Image[5]','compactStatusIcons = new Image[2]'),
('new Vector2(352, -8), new Vector2(112, 36), "详情"','new Vector2(424, -8), new Vector2(40, 36), "⋯"'),
('new Vector2(16, -8), new Vector2(328, 36)','new Vector2(160, -8), new Vector2(176, 32)'),
('            Line(heroFront.transform, new Vector2(16, -44), new Vector2(448, 2), line);',''),
('new Vector2(148, -52), new Vector2(2, 164)','new Vector2(148, -16), new Vector2(2, 128)'),
('rect.anchoredPosition = new Vector2(16, -52)','rect.anchoredPosition = new Vector2(16, -16)'),
('new Vector2(224, -52)','new Vector2(224, -40)'),
('            Line(heroFront.transform, new Vector2(16, -184), new Vector2(448, 2), line);',''),
('new Vector2(16 + i * 48, -188)','new Vector2(336 + i * 40, -8)'),
('new Vector2(16, -184), new Vector2(448, 40)','new Vector2(408, -8), new Vector2(16, 32)'),
('new Vector2(56, -12), new Vector2(2, 88)','new Vector2(56, -12), new Vector2(2, 64)'),
('new Vector2(8, -36)','new Vector2(8, -24)'),('new Vector2(width, 112)','new Vector2(width, 88)'),
('new Vector2(width - 48, -36)','new Vector2(width - 48, -24)'),
('new Vector2(480, showBack ? 280 : 224)','new Vector2(480, showBack ? 280 : 160)'),
('showBack, "概况", "详情"','showBack, "‹", "⋯"'),('heroFlipButton, "详情"','heroFlipButton, "⋯"'),
('heroStatusRows.Count == 0 ? "无持续状态" : heroStatusRows.Count > 5 ? "+" + (heroStatusRows.Count - 5) : ""','heroStatusRows.Count > compactStatusIcons.Length ? "+" : ""'),
('new Vector2(heroStatusRows.Count > 0 ? 384 : 16, -184)','new Vector2(408, -8)'),
('new Vector2(heroStatusRows.Count > 0 ? 80 : 448, FormalUiTheme.MinimumTextSlotHeight(compactStatusOverflow.fontSize))','new Vector2(16, 32)')])
edit('UnityProject/Assets/Game/Runtime/Presentation/CombatHudPortraitAssets.cs',[('sizeY = 48f / source.height','sizeY = 36f / source.height')])
edit('UnityProject/Assets/Game/Tests/EditMode/FormalCombatHudShellPrefabTests.cs',[('new Vector2(480, 224)','new Vector2(480, 160)'),('new Vector2(912, 112)','new Vector2(912, 88)')])
edit('UnityProject/Assets/Game/Editor/CombatHudRedesignVerification.cs',[('2026-10-07_顶部HUD压缩/verification','2026-10-07_顶部HUD细长收敛/verification')])
edit('UnityProject/Assets/Game/Resources/Config/OccPixelUiV02.json',[('"width": 480, "height": 224','"width": 480, "height": 160')])
p=root/'Worldbuilding/策划案/OCC_项目总策划案_v1.0.md';s=p.read_text(encoding='utf-8-sig');old=(f/'old.txt').read_text(encoding='utf-8');assert old in s;s=s.replace(old,(f/'new.txt').read_text(encoding='utf-8'));p.write_text(s,encoding='utf-8')
edit('Worldbuilding/数据表/OCC_美术与界面规格表_v1.0.csv',[('480／224','480／160'),('高112','高88'),('正面480×224','正面480×160'),('48原生头像窗口','48×36原生头像窗口')])
p=root/'Tools/OCCArt/occ_art_contract_v1.json';d=json.loads(p.read_text(encoding='utf-8-sig'));hud=d['combat_hud_portrait_design_20261006'];hud['hero']=[16,80,480,160];hud['order_height']=88;hud['order_style']='continuous icon-only shared strip; native48x36 head window; 96x72 reference pixels; information in hover';p.write_text(json.dumps(d,ensure_ascii=False,indent=2),encoding='utf-8')
print('Slim HUD source and local contracts updated')
