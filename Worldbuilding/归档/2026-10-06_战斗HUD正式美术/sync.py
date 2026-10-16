from pathlib import Path
import json,re,xml.etree.ElementTree as ET
root=Path(__file__).resolve().parents[3]; folder=Path(__file__).resolve().parent
after=json.loads((folder/'feishu-after.json').read_text(encoding='utf-8-sig'))
new=(folder/'layout-new.txt').read_text(encoding='utf-8')
assert new in ''.join(ET.fromstring(after['data']['document']['content']).itertext())
p=root/'Worldbuilding/策划案/OCC_项目总策划案_v1.0.md';s=p.read_text(encoding='utf-8-sig');s=re.sub(r'战场参考视口为[^\n]+',new,s,count=1)
s=s.replace('只用于角色选择、角色档案、敌方入场及详情，不缩小替代战棋','用于角色选择、角色档案、敌方入场及详情；战斗左上与行动顺序使用独立64×64头肩头像，缺省角色从既有展示立绘取头像区域，不使用战棋纹理。展示立绘不缩小替代战棋')
s += '\n\n### A.4.1 战斗身份卡美术（2026-10-06用户确认）\n\n人物卡与行动顺序采用学院象牙底板、暖灰内凹区、炭墨轮廓与少量黄铜结构角，头像框为独立原生32×32硬Alpha美术，头肩头像为独立64×64原生画布、最多32色、四边至少2px安全边。头像在1920与960分别2倍与1倍显示，头像框在两档分别4倍与2倍显示；不改变战棋资产。学院资源条沿用现有正式轨道与裁切填充。主体底板复用正式面板九宫格，语义焦点、选中与阵营条为独立运行时图层，不重着色生成原料。每项新原料与重试独立内置生图，经网格推断置信度至少1.2、无损解码、manifest与应用验证；人工审美未确认时保留QA_PENDING，不冒称正式资产。\n'
p.write_text(s,encoding='utf-8')
p=root/'Tools/OCCArt/occ_art_contract_v1.json';d=json.loads(p.read_text(encoding='utf-8-sig'))
for role,size,palette in [('combat_hud_portrait_64',[64,64],32),('combat_hud_portrait_frame_32',[32,32],12)]:
 d['roles'][role]={'delivery_size':size,'logical_cells':None,'palette_max':palette,'transparent_border_min':2 if size[0]==64 else 1,'unity_ppu':32,'alpha_policy':'transparent_hard_alpha','application_scope':'combat_hud_identity_only','integer_only':True}
d['combat_hud_portrait_design_20261006']={'hero':[16,80,480,288],'hero_back':[480,280],'order_right':1904,'order_top':80,'order_height':192,'portrait_native':[64,64],'portrait_reference':[128,128],'slots_max':7,'width_policy':'content_sized','tactical_texture_fallback':False}
p.write_text(json.dumps(d,ensure_ascii=False,indent=2),encoding='utf-8')
p=root/'Tools/OCCArt/native32_generation_profiles.json';d=json.loads(p.read_text(encoding='utf-8-sig'));d['display_policy']['battlefield_viewport']=[1888,768];d['display_policy']['visible_gameplay_cells_at_canonical']=[9,4];p.write_text(json.dumps(d,ensure_ascii=False,indent=2),encoding='utf-8')
p=root/'Worldbuilding/数据表/OCC_美术与界面规格表_v1.0.csv';s=p.read_text(encoding='utf-8-sig');s=s.replace('16／80／416／248','16／80／480／288').replace('顶部偏右行动顺序（1032／80／824／96）','顶部靠右行动顺序（右边界1904／顶部80／高192／按数量收短）').replace('人物正面416×248背面416×280','人物正面480×288背面480×280');s+='\nART-COMBAT-PORTRAIT-64,界面,战斗头肩立绘头像,独立64×64原生画布；最多32色；硬Alpha；2px安全边；1920与960分别2倍与1倍,角色身份色,左上与行动顺序共用；不得回退战棋纹理；缺失时展示名称占位\nART-COMBAT-PORTRAIT-FRAME-32,界面,战斗身份头像框,独立32×32原生图；最多12色；硬Alpha；1px安全边；1920与960分别4倍与2倍,象牙暖灰炭墨黄铜,内孔透明；当前行动选中焦点与敌我条独立叠加；不把反馈颜色烘焙进头像\n';p.write_text(s,encoding='utf-8')
p=root/'UnityProject/Assets/Game/Resources/Config/OccPixelUiV02.json';s=p.read_text(encoding='utf-8-sig');s=re.sub(r'("id": "combat.hero"[^\n]+"width": )416(, "height": )248',r'\g<1>480\g<2>288',s);json.loads(s);p.write_text(s,encoding='utf-8')
print('Synced approved portrait usage and layout')
