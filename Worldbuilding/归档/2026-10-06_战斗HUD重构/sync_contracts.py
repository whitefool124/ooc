from pathlib import Path
import json, re, shutil
root=Path(__file__).resolve().parents[3]
archive=Path(__file__).resolve().parent
def edit(path,fn):
    p=root/path
    text=p.read_text(encoding='utf-8-sig')
    p.write_text(fn(text),encoding='utf-8')
intro=(archive/'new-intro.txt').read_text(encoding='utf-8')
layout=(archive/'new-layout.txt').read_text(encoding='utf-8')
def plan(s):
    s=re.sub(r'战场、HUD 与指令区见图；[^\n]+',intro,s,count=1)
    s=s.replace('上部地图75%、右HUD25%','上部地图使用全宽，左上人物状态、顶部偏右行动顺序与右侧悬停抽屉按A.4执行')
    s=s.replace('1408×768','1888×768').replace('7×4','9×4')
    s=s.replace('## A.4 右侧信息栏安全区','## A.4 战斗悬浮HUD与行动值抽屉')
    s=re.sub(r'右侧信息栏参考坐标为[^\n]+',layout,s,count=1)
    s=s.replace('右侧HUD','左上人物HUD')
    return s
edit(Path('Worldbuilding/策划案/OCC_项目总策划案_v1.0.md'),plan)
edit(Path('AGENTS.md'),lambda s:s.replace('当前战斗 UI 基准为 1920×1080、左侧地图 75%、右侧 HUD 25%。','当前战斗 UI 基准为1920×1080；底部指令区以上战场使用完整宽度，人物状态在左上，预计行动顺序在顶部偏右，右侧行动值浮层默认收起、悬停展开，不挤压地图或改变镜头（用户确认，2026-10-06）。'))
edit(Path('UnityProject/AGENTS.md'),lambda s:s.replace('1920×1080、左地图75%/右HUD25%','1920×1080、全宽战场、左上人物状态、顶部偏右行动顺序与右侧悬停行动值浮层（2026-10-06用户确认）'))
def spec(s):
    s=re.sub(r'^ART-UI-SPLIT,[^\n]+','ART-UI-SPLIT,界面,全宽战场与悬浮HUD,视口（16／80／1888／768）；左上人物（16／80／416／248）；顶部偏右行动顺序（1032／80／824／96）,中性与状态色,取消固定右栏；行动顺序每单位一次且跟随真实值速度与合法预览；展开不重排视口',s,flags=re.M)
    s=re.sub(r'^ART-COMBAT-STATUS,[^\n]+','ART-COMBAT-STATUS,界面,左上人物状态与右侧行动值抽屉,人物正面416×248背面416×280；行动值浮层（1432／240／416／436）；页签（1860／320／44／136）,状态语义色,默认收起；悬停200ms打开移出350ms收起；页签和浮层内保持展开；点击固定；模态或退出清悬停；沿用行动条与记录翻面',s,flags=re.M)
    return s.replace('一屏 7×4','一屏 9×4').replace('一屏7×4','一屏9×4')
edit(Path('Worldbuilding/数据表/OCC_美术与界面规格表_v1.0.csv'),spec)
def config(s):
    s=s.replace('"battlefieldWidth": 1440','"battlefieldWidth": 1920').replace('"hudWidth": 480','"hudWidth": 0')
    replacements={'combat.rightConsole':('top-right',-72,-240,416,436),'combat.hero':('top-left',0,0,416,248),'combat.timeline':('top-left',0,0,416,436)}
    for key,(anchor,x,y,w,h) in replacements.items():
        s=re.sub(r'\{ "id": "'+re.escape(key)+r'"[^\n]+',f'{{ "id": "{key}", "anchor": "{anchor}", "x": {x}, "y": {y}, "width": {w}, "height": {h} }},',s)
    json.loads(s)
    return s
edit(Path('UnityProject/Assets/Game/Resources/Config/OccPixelUiV02.json'),config)
edit(Path('UnityProject/Assets/Game/Runtime/Campaign/BattlefieldPresentationAdapter.cs'),lambda s:s.replace('BattlefieldWidth = 1440f','BattlefieldWidth = 1920f').replace('1408×768','1888×768').replace('7×4','9×4'))
def machine(s):
    s=re.sub(r'"right_console_reference": \[[^\]]+\]', '"right_console_reference": [1432, 240, 416, 436]',s,count=1)
    start=s.index('"console_modules_reference":')
    end=s.index('"battlefield_decision_toast_reference":',start)
    s=s[:start]+'''"console_modules_reference": {
      "hero": [16, 80, 416, 248],
      "timeline": [1432, 240, 416, 436]
    },
    "floating_hud_policy": {
      "approved_date": "2026-10-06",
      "battlefield_viewport": [16, 80, 1888, 768],
      "action_order": [1032, 80, 824, 96],
      "timeline_tab": [1860, 320, 44, 136],
      "open_delay_ms": 200,
      "close_delay_ms": 350,
      "default_collapsed": true,
      "click_pin": true,
      "overlay_does_not_resize_or_pan_battlefield": true,
      "current_actor_then_unique_estimated_first_appearances": true
    },
    '''+s[end:]
    s=s.replace('1408','1888').replace('7x4','9x4').replace('7×4','9×4')
    json.loads(s)
    return s
edit(Path('Tools/OCCArt/occ_art_contract_v1.json'),machine)
print('Synced approved layout to local plan, specifications, rules, runtime config and machine mirror.')
