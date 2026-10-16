from pathlib import Path
import json,shutil,xml.etree.ElementTree as ET
from html import escape
root=Path(__file__).resolve().parents[3];f=Path(__file__).resolve().parent
files=['UnityProject/Assets/Game/Runtime/Presentation/'+n for n in ['CombatHudPortraitAssets.cs','CombatPortraitCardView.cs','FormalCombatHud.cs']]+['UnityProject/Assets/Game/Editor/CombatHudRedesignVerification.cs','UnityProject/Assets/Game/Resources/UI/Prefabs/FormalCombatOrderStripItem.prefab','Worldbuilding/策划案/OCC_项目总策划案_v1.0.md','Worldbuilding/数据表/OCC_美术与界面规格表_v1.0.csv','Tools/OCCArt/occ_art_contract_v1.json']
for rel in files:
    for suffix in ['', '.meta']:
        src=root/(rel+suffix)
        if src.exists():
            dst=f/'before'/(rel+suffix);dst.parent.mkdir(parents=True,exist_ok=True)
            if not dst.exists():shutil.copy2(src,dst)
d=json.loads((f/'feishu-before.json').read_text(encoding='utf-8-sig'));pairs=[]
for p in ET.fromstring(d['data']['document']['content']):
    old=''.join(p.itertext());new=old
    if old.startswith('战场参考视口'):
        new=old.replace('使用一整块学院象牙底板','使用连续24参考像素高的窄轨道，头像下缘嵌在轨道上，不再使用整高底板').replace('当前单位以独立顶部标记和槽底突出','当前单位以独立小标记突出，无独立槽底').replace('左上人物卡仍使用64×64头肩头像','左上人物卡从64×64头像读取（16，20，32，32）原生头部窗口，在1920与960分别4倍与2倍显示').replace('当前单位使用青色顶部标记和浅色槽底，敌我条','当前单位使用青色短标记，敌我点标')
    if old.startswith('战斗身份卡美术'):
        new=old.replace('在1920与960分别2倍与1倍显示。学院资源条','右上头部窗口在1920与960分别2倍与1倍显示；左上32×32近景窗口分别4倍与2倍显示。学院资源条')
    if new!=old:
        i=len(pairs);(f/f'block-{i}.txt').write_text(p.attrib['id'],encoding='utf-8');(f/f'update-{i}.xml').write_text('<p>'+escape(new)+'</p>',encoding='utf-8');pairs.append([old,new])
assert len(pairs)==2
(f/'pairs.json').write_text(json.dumps(pairs,ensure_ascii=False,indent=2),encoding='utf-8')
print('Backed up and prepared two targeted layout updates')
