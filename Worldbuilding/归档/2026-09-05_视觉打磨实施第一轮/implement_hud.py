from pathlib import Path
import json, csv
root=Path('E:/数据库/OCC_Codex')
task=root/'Worldbuilding/归档/2026-09-05_视觉打磨实施第一轮'
doc=json.loads((task/'evidence/master_latest.json').read_text(encoding='utf-8-sig'))['data']['document']
assert '12.4 右侧信息栏安全区' in doc['content']
(root/'Worldbuilding/策划案/OCC_项目总策划案_v1.0.md').write_text(doc['content'].rstrip()+'\n',encoding='utf-8')
def change(path, pairs):
 p=root/path; s=p.read_text(encoding='utf-8-sig')
 backup=task/'before/hud'/path
 assert not backup.exists()
 backup.parent.mkdir(parents=True,exist_ok=True); backup.write_bytes(p.read_bytes())
 for old,new in pairs:
  assert s.count(old)==1,(path,old,s.count(old))
  s=s.replace(old,new)
 p.write_text(s,encoding='utf-8')
change('UnityProject/Assets/Game/Resources/Config/OccPixelUiV02.json',[
 ('"width": 448, "height": 840','"width": 448, "height": 768'),
 ('"y": -16, "width": 416, "height": 112','"y": -16, "width": 416, "height": 96'),
 ('"y": -140, "width": 416, "height": 260','"y": -124, "width": 416, "height": 244'),
 ('"y": -414, "width": 416, "height": 270','"y": -380, "width": 416, "height": 264'),
 ('"y": -698, "width": 416, "height": 126','"y": -656, "width": 416, "height": 96')])
change('UnityProject/Assets/Game/Runtime/Presentation/FormalCombatHud.cs',[
 *[(f'FormalUiKit.LayoutPanel("{name}", side.transform, "combat.{key}", panel)',f'ConsoleModule("{name}", side.transform, "combat.{key}")') for name,key in [('本轮行动','selected'),('英雄概况','hero'),('行动序列模块','timeline'),('现场记录模块','log')]],
 ('new Vector2(224, 40)','new Vector2(224, 32)'),
 ('new Vector2(154, 40)','new Vector2(154, 32)'),
 ('new Vector2(380, 72)','new Vector2(380, 48)'),
 ('Label("英雄", heroModule.transform, new Vector2(16, -4), new Vector2(360, 40)','Label("英雄", heroModule.transform, new Vector2(16, -4), new Vector2(72, 32)'),
 ('Label("主手装备", heroModule.transform, new Vector2(16, -40), new Vector2(330, 40)','Label("主手装备", heroModule.transform, new Vector2(96, -4), new Vector2(248, 32)'),
 ('new Vector2(364, -44)','new Vector2(364, -4)'),
 ('new Vector2(16, -76), new Vector2(226, 40)','new Vector2(16, -40), new Vector2(226, 32)'),
 ('new Vector2(244, -76), new Vector2(90, 40)','new Vector2(244, -40), new Vector2(90, 32)'),
 ('new Vector2(16, -104), FormalUiTheme.Health','new Vector2(16, -76), FormalUiTheme.Health'),
 ('new Vector2(16, -150), FormalUiTheme.Shield','new Vector2(16, -128), FormalUiTheme.Shield'),
 ('new Vector2(16, -196), FormalUiTheme.Magic','new Vector2(16, -180), FormalUiTheme.Magic'),
 ('Label("刚刚发生", logModule.transform, new Vector2(16, -4), new Vector2(380, 40)','Label("刚刚发生", logModule.transform, new Vector2(16, -4), new Vector2(380, 32)'),
 ('new Vector2(380, 80), FormalUiTheme.BodyFontSize, muted, TextAnchor.UpperLeft','new Vector2(380, 48), FormalUiTheme.BodyFontSize, muted, TextAnchor.UpperLeft'),
 ('new Vector2(338 + index * 23, -86)','new Vector2(338 + index * 22, -46)'),
 ('Label(title, parent, position, new Vector2(200, 40)','Label(title, parent, position, new Vector2(200, 32)'),
 ('Label(title + "数值", parent, position, new Vector2(374, 40)','Label(title + "数值", parent, position, new Vector2(374, 32)'),
 ('new Vector2(390, 24), FormalUiTheme.ResourceTrack','new Vector2(384, 20), FormalUiTheme.ResourceTrack'),
 ('        private void CreateTimelineSlot(int index)', '''        private static GameObject ConsoleModule(string name, Transform parent, string layoutId)
        {
            GameObject module = FormalUiKit.FlatPanel(name, parent, Vector2.zero, Vector2.zero,
                Vector2.zero, Vector2.zero, panel);
            RectTransform rect = module.GetComponent<RectTransform>();
            FormalUiKit.ApplyLayout(rect, layoutId);
            module.GetComponent<Image>().raycastTarget = true;
            FormalUiKit.ThinFrame(module.transform, rect.sizeDelta, FormalUiTheme.WithAlpha(line, .55f));
            return module;
        }

        private void CreateTimelineSlot(int index)''')])
change('UnityProject/Assets/Game/Runtime/Presentation/CombatVisualFeedback.cs',[
 ('canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 60;', 'canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 60; canvas.pixelPerfect = true;'),
 ('scaler.referenceResolution = new Vector2(1920, 1080);','scaler.referenceResolution = new Vector2(UiLayoutContract.ReferenceWidth, UiLayoutContract.ReferenceHeight); scaler.matchWidthOrHeight = UiLayoutContract.MatchWidthOrHeight;')])
change('UnityProject/Assets/Game/Tests/EditMode/FormalUiThemeTests.cs',[
 ('Assert.That(track.sizeDelta.y, Is.EqualTo(24f));','Assert.That(track.sizeDelta.y, Is.EqualTo(20f));')])
contract=root/'Tools/OCCArt/occ_art_contract_v1.json'
d=json.loads(contract.read_text(encoding='utf-8-sig'))
d['battlefield_presentation']['right_console_reference']=[1456,80,448,768]
d['battlefield_presentation']['console_modules_reference']={'selected':[16,16,416,96],'hero':[16,124,416,244],'timeline':[16,380,416,264],'log':[16,656,416,96]}
contract.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
spec=root/'Worldbuilding/数据表/OCC_美术与界面规格表_v1.0.csv'
with spec.open(encoding='utf-8-sig',newline='') as f: rows=list(csv.DictReader(f))
fields=list(rows[0]); assert not any(r['ID']=='ART-HUD-SAFE-AREA' for r in rows)
rows.append(dict(zip(fields,['ART-HUD-SAFE-AREA','界面','右侧栏与底栏安全区','右栏1456/80/448/768；底栏顶864','内层2px细框；正文24；资源行32+20','四模块96/244/264/96；保留五个行动位和三资源；摘要两行；详情悬停'])))
with spec.open('w',encoding='utf-8-sig',newline='') as f:
 w=csv.DictWriter(f,fieldnames=fields,lineterminator='\n'); w.writeheader(); w.writerows(rows)
print('HUD safety layout applied; master revision',doc.get('revision_id'))
