from pathlib import Path
import csv,io,shutil,json
root=Path(__file__).resolve().parents[3]
archive=Path(__file__).resolve().parent
mother=root/'Worldbuilding/策划案/OCC_项目总策划案_v1.0.md'
shutil.copy2(mother,archive/'总案_before.md')
s=mother.read_text('utf-8')
s=s.replace('蓄压、锁线、撞击物块、浅水冷却和干地卸压必须使用不同且可预览的姿态或灯色。','沿用旧版参考图的三角楔头形象；蓄压、锁线、撞击物块、浅水冷却和干地卸压以动作抖动、公开意图和状态文字区分，不再要求专属动作帧或差异灯色。')
scope='第一阶段表现范围：剧情与肉鸽共用。单位保留静态形象，移动、出手和受击使用已有位移与局部抖动反馈，不再将成套角色动作帧列为本阶段待办。三类塔内机关共用机座，通过护障维护、显影巡查、冲压隔离的名称、放行状态和效果说明辨识，不另制作专用机关图形标识。'
assert scope in json.loads((archive/'feishu_after.json').read_text('utf-8-sig'))['data']['document']['content']
s=s.replace('##### 楔角\n','##### 楔角\n\n'+scope+'\n',1)
mother.write_text(s,'utf-8')
changes=[]
for name in ['OCC_学院第一阶段美术资产生产清单_v1.0.csv','OCC_第一阶段战斗地图与敌人美术缺口矩阵_v1.0.csv','OCC_学院第一阶段全流程内容清单_v1.0.csv']:
 p=root/'Worldbuilding/数据表'/name
 shutil.copy2(p,archive/(name+'.before'))
 rows=list(csv.reader(io.StringIO(p.read_text('utf-8-sig'))))
 for row in rows[1:]:
  before=row.copy()
  if name.startswith('OCC_学院第一阶段美术'):
   if row[3]=='victor／hero':row[5]='32 PPU；静态母版配合位移、出手与受击局部抖动';row[7]='保留正式静态母版；复用现有结算驱动反馈，不再要求成套动作帧'
   if row[2]=='教程普通敌人':row[7]='复核当前数据ID、意图和占格映射；动作复用位移和局部抖动'
   if row[2]=='敌人动作与状态姿态':
    row[5]='静态单位＋位移／局部抖动＋公开意图／状态文字'
    row[6]='现有静态单位与动作反馈可复用；旧动画文件保留，不再要求逐角色接入成套动作帧'
    row[7]='用户2026-10-16取消本阶段成套动作资源生产；沿用结算驱动的局部抖动和文字说明'
    row[8]='动作与状态可读，不延迟或伪造结算'
   if row[2]=='首领与机关表现':
    row[6]='首领静态资源与通用机座候选已有；三类机关通过功能名称、放行状态与实际效果说明区分'
    row[7]='不再生产专用机关图形标识；保留真实护障、光带和冲压线路，补齐名称与状态文字'
    row[8]='三类用途、当前放行状态和拆除结果明确可读'
  if name.startswith('OCC_第一阶段战斗'):
   if row[1]=='FIRST-X':row[6]='用户指定复用旧总案三角楔头形象；对应像素待机候选处理中';row[7]='保留旧三角头剪影；位移与抖动配合锁线、冷却、卸压文字，不补成套动作'
   if row[1]=='B01':row[5]='通用机关机座候选已有；功能名称、放行状态和效果改用文字说明';row[7]='共用机座，以三类名称和实际效果说明辨识；取消专用机关图形标识';row[8]='三类用途、放行状态、维护链和三阶段信息公开'
  if row!=before:changes.append({'file':name,'id':row[3] if name.startswith('OCC_学院第一阶段美术') else row[1],'before':before,'after':row})
 out=io.StringIO(newline='');csv.writer(out,quoting=csv.QUOTE_ALL,lineterminator='\n').writerows(rows);p.write_text(out.getvalue(),'utf-8-sig')
(archive/'scope_changes.json').write_text(json.dumps(changes,ensure_ascii=False,indent=2),'utf-8')
print('scope synchronized:',len(changes),'rows')
