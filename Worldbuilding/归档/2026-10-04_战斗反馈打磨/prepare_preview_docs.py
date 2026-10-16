from pathlib import Path
import json,xml.etree.ElementTree as ET,html
out=Path(__file__).parent
root=out.parents[2]
master=root/'Worldbuilding/策划案/OCC_项目总策划案_v1.0.md'
text=master.read_text(encoding='utf-8-sig')
xml=ET.fromstring(json.loads((out/'cloud-A17.json').read_text(encoding='utf-8-sig'))['data']['document']['content'])
replacements=[
('战場', ''),
]
new=[
'战场格预览统一使用当前方框与填色语言，由同一个运行时绘制组件管理：移动可达为内缩青色方框与低透明填色；攻击与术式可选格为深绿色方框；玩家实际作用区为绿色填色与连通区域外边线；敌人可能攻击范围为琥珀色内缩框，敌人实际作用区为红色填色与连通区域外边线。移动叠加敌方范围时，青框继续向内缩，让橙色威胁边线保持可见；移动警戒风险与术式友军风险以中心红色小块区分。以上取代旧细角框与六档固定不透明度，当前焦点仍以白色角标表达。标记出现时160ms淡入，并按0.91～1.0轻微脉冲，每格相位错开；关闭动画时使用稳定形态。',
'预览只读取真实射程、路径、视线、行动点、魔力、冷却与回合条件。被墙体或烟幕截断、超出最大射程、近身死区或不能投递的格，不得呈现为可提交目标；主角自身格仅在合法自身动作时作为作用目标。行动结束、回合改变或动作切换时清除过期预览；攻击与术式的有效格数只统计真实可提交格。',
'单锚点术式即使当前没有合法目标，也要在射程内且视线可达的格上显示可选范围，使玩家能读到本术式打多远；自身术式与带形状的术式不显示无关的方阵射程范围，其实际作用格来自施术者自身或所选方向。射程与实际结算共用同一套几何和失败原因，不另写距离与视线规则。',
'敌人移动意图使用橙色路径节点与落点框，落点中心保留橙色方向标记；图形由原生32格整数像素几何绘制，保持在地形物件和单位之下。公开意图的行动、目标、路径、落点或作用区改变时，对所属意图图标和新路径／作用格反馈850ms：图标暖色闪烁、前300ms轻微整数像素上提，并在原徽记范围内显示短暂“变”字；格子以琥珀色边线闪烁。初次显示及重复读取同一计划不反馈；关闭动画时保留短暂稳定颜色与“变”字。离开、重开或更换战斗时清除记录，不改变AI选择和结算。',
'右键行动菜单打开时，棋盘只显示菜单锚点焦点，不显示已选动作范围；悬停菜单行时预览该行动的可选范围与作用区，移出后回到仅锚点，关闭菜单后恢复已选动作范围。左击格子选点后，白色角标与目标预览保持，指针离开仍保留所选点；非移动动作再次点击同一格确认，移动使用双击可达格。确认成功时给予浅绿角标400ms，拒绝时给予红色角标400ms并保留失败原因；所有反馈不接收输入。'
]
jobs=[]
for old,newtext in zip(list(xml.iter('p')),new):
    oldtext=''.join(old.itertext())
    if text.count(oldtext)!=1: raise RuntimeError('Mirror mismatch: '+old.get('id'))
    text=text.replace(oldtext,newtext)
    path=out/(old.get('id')+'.xml')
    path.write_text('<p>'+html.escape(newtext)+'</p>',encoding='utf-8')
    jobs.append({'id':old.get('id'),'path':str(path.relative_to(root)).replace('\\','/')})
master.write_text(text,encoding='utf-8',newline='\n')
(out/'preview-doc-jobs.json').write_text(json.dumps(jobs,ensure_ascii=False,indent=2),encoding='utf-8')
spec=root/'Worldbuilding/数据表/OCC_美术与界面规格表_v1.0.csv'
lines=spec.read_text(encoding='utf-8-sig').splitlines()
records={
'ART-MARKER-LADDER':'ART-MARKER-LADDER,战场,战场预览方框与填色,原生32格像素几何；160ms淡入×0.91～1.0错相脉冲；选点白角标；确认浅绿／拒绝红角标400ms,移动青框／可选深绿框／实际作用绿填色及连通外边／敌方范围琥珀框／敌方作用红填色及连通外边,移动与敌方范围重叠时青框内缩；警戒或友军风险用中心红块；关闭动画使用稳定形态；不接收输入；清理过期预览',
'ART-MOVE-RANGE':'ART-MOVE-RANGE,战场,步数真实性,高亮／有效格数／失败原因共用真实路径与当前步数预算,移动可达用青色内缩方框及低透明填色,束缚／AP／回合／结局门控；水藤代价与敏捷出身修正及障碍占位；只读缓存与变化失效',
'ART-ATTACK-RANGE':'ART-ATTACK-RANGE,战场,攻击射程与可提交目标,真实射程／视线／行动点／魔力与可投递条件,可选深绿框与作用绿填色；敌方范围琥珀框与作用红填色,视线截断／近身死区／超射程不得呈现为可提交目标；自身格仅合法自身动作；有效格数只计可提交格',
'ART-CONTEXT-PREVIEW':'ART-CONTEXT-PREVIEW,交互表现,选点与右键菜单预览,首次左击选点并保持；非移动动作再次同格确认；移动双击可达格；菜单行悬停预览点击确认,白色焦点角标；确认浅绿与拒绝红角标400ms,菜单打开仅锚点；悬停行预览该行动；移出仅锚点；关闭恢复动作范围；换动作／回合／交互取消待确认；不延迟提交其他动作',
'ART-ENEMY-DESTINATION':'ART-ENEMY-DESTINATION,战场,敌人路径与落点,原生32格整数像素路径节点与落点框及中心方向标记,橙色节点与落点框；公开意图改变时850ms暖色及琥珀边线反馈,意图徽记在原范围内显示短暂变字；初次显示及不变不提示；关闭动画保持稳定颜色；退出重开清理；不改变AI与结算'
}
for i,line in enumerate(lines):
    key=line.split(',')[0]
    if key in records: lines[i]=records[key]
spec.write_text('\n'.join(lines)+'\n',encoding='utf-8',newline='\n')
