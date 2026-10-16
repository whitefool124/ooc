"""Apply the confirmed E menu scope to local mirrors after Feishu block updates."""
from pathlib import Path
import json
import re
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
ARCHIVE = ROOT/'Worldbuilding/归档/2026-10-08_主界面E动态场景'
plan = ROOT/'Worldbuilding/策划案/OCC_项目总策划案_v1.0.md'
content = plan.read_text(encoding='utf-8')
def xml_text(name):
    root = ET.fromstring((ARCHIVE/name).read_text(encoding='utf-8'))
    return ''.join(root.itertext())
description = xml_text('menu_description.xml').replace('界面说明：','**界面说明：**',1)
interaction = xml_text('menu_interaction.xml').replace('交互规则：','**交互规则：**',1)
start = content.index('### 5.1.3 以太主界面（04）')
end = content.index('### 5.1.4 ',start)
section = content[start:end]
section = re.sub(r'\*\*界面说明：\*\*[^\n]*',lambda _:description,section)
section = re.sub(r'\*\*交互规则：\*\*[^\n]*',lambda _:interaction,section)
content = content[:start]+section+content[end:]
old = next(line for line in content.splitlines() if line.startswith('学院阶段的战外档案界面可用纸页'))
content = content.replace(old,xml_text('menu_art_rule.xml'),1)
plan.write_text(content,encoding='utf-8')
csv = ROOT/'Worldbuilding/数据表/OCC_美术与界面规格表_v1.0.csv'
lines = csv.read_text(encoding='utf-8-sig').splitlines()
for i,line in enumerate(lines):
    if line.startswith('ART-UI-MAIN-MENU,'):
        lines[i] = 'ART-UI-MAIN-MENU,界面,以太主界面,1920×1080；E窗边书桌场景与左侧小型纸页菜单；桌面书本承担开书进入演出,暖纸白／木构／浅色学院景观；冷青仅焦点与主动以太,开始游戏为默认主要行动；仅未完成轮次显示继续游戏；行程与行囊／设置／退出为次要入口；不常驻存档卡角色摘要与阶段选择；开书期间锁输入且跳转一次；关闭动画直接进入；不可读或受保护存档不自动覆盖'
    elif line.startswith('ART-UI-ARCHIVE-MATERIAL,'):
        lines[i] = line.replace('首页纸页主栏＋卷轴主要行动','首页E窗边书桌＋小型纸页菜单＋桌面书本开书演出')
csv.write_text('\n'.join(lines)+'\n',encoding='utf-8')
path=ROOT/'Tools/OCCArt/occ_art_contract_v1.json'
contract=json.loads(path.read_text(encoding='utf-8'))
menu=contract['screen_layouts']['main_menu']
menu.update(primary_actions='compact_left_paper_menu',archive_status='not_shown_on_landing',
            scene_direction='approved_E_window_side_desk',
            continue_visible_when='unfinished_authoritative_run_exists',
            new_game_flow='open_desktop_book_then_character_select_without_creating_save',
            continue_flow='open_desktop_book_then_resume_authoritative_blocking_state',
            transition_input='block_repeated_submission_until_single_handoff',
            animation_disabled='immediate_handoff',
            ambient_layers=['lake_glints','cloth','steam','active_aether_lamp','foliage'])
menu.pop('life_stages',None)
menu.pop('archive_status',None)
menu.pop('academy_only_visual_forbidden',None)
path.write_text(json.dumps(contract,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('Updated affected main-menu section, CSV rows and machine mirror.')
