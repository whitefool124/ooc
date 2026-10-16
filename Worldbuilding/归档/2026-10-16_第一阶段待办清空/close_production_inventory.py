from pathlib import Path
import csv,io,json,shutil
root=Path(__file__).resolve().parents[3];folder=Path(__file__).resolve().parent
changes=[]
for name in ['OCC_学院第一阶段美术资产生产清单_v1.0.csv','OCC_第一阶段战斗地图与敌人美术缺口矩阵_v1.0.csv','OCC_学院第一阶段全流程内容清单_v1.0.csv']:
 p=root/'Worldbuilding/数据表'/name;backup=folder/'table_backups'/name;backup.parent.mkdir(exist_ok=True)
 if not backup.exists():shutil.copy2(p,backup)
 rows=list(csv.reader(io.StringIO(p.read_text('utf-8-sig'))))
 for row in rows[1:]:
  before=row.copy()
  if name.startswith('OCC_学院第一阶段美术'):
   kind=row[2]
   if kind=='第一阶段敌人静态单位':
    row[6]='楔角及四个新敌人均已默认绑定各自独立图片；原GUID与原生画布保留，生产入口不依赖Editor候选开关。'
    row[7]='本轮默认接入完成；不重复生成已有形象；整场审美另记'
   elif kind=='通用场地库':
    row[6]='散页、痕迹、约束纹、页幕、烟尘／火场6帧、三种装置与碎晶全部进入默认取图入口；upright方向与碎晶独立叠层在Player代码中生效。'
    row[7]='本轮默认接入完成；保留实际用途文字和原结算，场景品质另验'
   elif kind=='区域地表与结构套件':
    row[6]+=' 当前6类环境与外围陈设按现行紧凑地图复用，旧111处坐标不是现行制作缺口。'
    row[7]='保留当前关卡陈设组合；不按旧12×9结构坐标补无效摆位'
   elif kind=='事件插图':
    row[2]='事件内容页与场景';row[3]='现行事件ID';row[4]='共用场景＋逐事件内容'
    row[6]='已有HD2D事件身份页、交互纸页、事件名称与逐ID选项；本轮同步服务页清晰正文；复用现有陈设，没有额外生成事件插画。'
    row[7]='基础辨识按现行HD2D内容页完成；不依据旧8项汇总重生整套插画'
   elif kind=='服务节点场景与标识':
    row[6]='工坊目标／材料／预览位、医务室治疗／餐食位、商店商品陈列位均已接入独立HD2D布局；本轮补清晰屏幕文字层，保留实际按钮与触发。'
    row[7]='本轮基础服务辨识收尾完成；复用独立纸页、托盘、陈设和既有功能图标'
   elif kind=='餐食与消耗品图标':
    row[5]='实际增益功能图标＋名称／费用／悬停效果；导能热饮复用杯碟'
    row[6]='8餐食ID统一映射现有功能图标与文案；教程仍固定3种，常规医务室从8种无重复抽3种。费用、实际增益、3场期限、互斥和存档均已接线；未生成8张独立餐点插画。'
    row[7]='本轮逐ID功能辨识和规则补缺完成；复用既有食材、材料与物品图标'
   elif kind=='单轮总结与失败表现':
    row[6]='胜利、战败、主动放弃已有不同结果文字；本轮补不同语义图标与色彩，路线／资源／构筑原记录保持。'
    row[7]='本轮结果标识制作完成；完整自然旅程验收另记'
   elif kind=='局内对话表现':
    row[6]='现行总案已冻结四场固定战斗开战单句，代码与来源映射已接入；随机遭遇不配置开场对话。'
    row[7]='复用现有头像／静态单位回退及字幕框；不再以历史未冻结记录另开对白生产'
   elif kind=='环境与界面动效':
    row[6]='已有地图展开、奖励与服务事件反馈、单位位移／局部抖动、烟火序列、热饮蒸汽及对应声音；本轮沿真实结果复用，未新增成套角色动作。'
    row[7]='当前基础反馈制作收口；整体美术与运动品质验收独立保留'
   elif kind=='教程精英敌人':
    row[6]+=' 本轮在足点资源旁常显单位姓名，部分身体被物件挡住时仍可辨认身份。'
  elif name.startswith('OCC_第一阶段战斗'):
   if any(id in row[4] for id in ['elder_tracker_hound','signal_keeper','wind_librarian','legacy_storekeeper']):
    row[6]='对应新敌人独立图片已进入默认注册；不再复用其他敌人剪影'
    row[7]='基础形象与场地默认接线完成；保留现行地图、敌群与数值'
   if row[1]=='FIRST-X':row[7]='独立三角头默认接入；足点常显楔角姓名，遮挡时保持身份辨识；静态形象配合抖动和公开文字'
   if row[1]=='B01':row[5]='首领、共用机关机座、护障／显影／冲压及放行状态均有默认表现和用途文字';row[7]='本轮机关基础辨识已完成；不新增专用机关图形标识'
  elif name.startswith('OCC_学院第一阶段全流程'):
   if row[3]=='内容表现':row[8]='本轮制作清单已清空：24项既有敌人／场地／装置进入默认入口，单位足点身份可读，服务与事件有HD2D独立布局和清晰正文，8餐食功能辨识与实际规则补齐，三种结算结果明确。整场品质与完整自然旅程验收不在本轮清空范围。'
   if row[3]=='服务系统' or '医务室' in row[4] and row[3]!='内容表现':row[8]+=' 本轮另补8餐食随机3选项、增益生效／3场到期／互斥／候选存档及功能图标。'
  if row!=before:changes.append({'file':name,'before':before,'after':row})
 out=io.StringIO(newline='');csv.writer(out,quoting=csv.QUOTE_ALL,lineterminator='\n').writerows(rows);p.write_text('\ufeff'+out.getvalue(),'utf-8')
(folder/'production_table_changes.json').write_text(json.dumps(changes,ensure_ascii=False,indent=2),'utf-8')
print('Production facts updated:',len(changes),'rows; no master content counts changed')
