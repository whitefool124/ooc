"""Update production facts only; preserve product scope and mother-document decisions."""
import csv,io,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3];HERE=Path(__file__).resolve().parent
d=json.loads((HERE/'bindings.json').read_text(encoding='utf-8-sig'))
def update(name,fn):
    p=ROOT/'Worldbuilding/数据表'/name;raw=p.read_bytes();(HERE/(name+'.before')).write_bytes(raw)
    before=list(csv.reader(io.StringIO(raw.decode('utf-8-sig'),newline='')));after=[row[:] for row in before]
    for row in after[1:]:fn(dict(zip(after[0],row)),row,after[0])
    changes=[{'row':r+1,'column':before[0][c],'old':x,'new':y} for r,(a,b) in enumerate(zip(before,after)) for c,(x,y) in enumerate(zip(a,b)) if x!=y]
    if not changes:return
    out=io.StringIO(newline='');writer=csv.writer(out,quoting=csv.QUOTE_ALL,lineterminator='\r\n' if b'\r\n' in raw else '\n');writer.writerows(after)
    p.write_bytes((b'\xef\xbb\xbf' if raw.startswith(b'\xef\xbb\xbf') else b'')+out.getvalue().encode())
    (HERE/(name+'.changes.json')).write_text(json.dumps(changes,ensure_ascii=False,indent=2),encoding='utf-8')
def setcell(row,keys,key,value):row[keys.index(key)]=value
def production(r,row,keys):
    kind=r['资产域']
    facts={
      '第一阶段敌人静态单位':('四个新单位独立Editor候选齐备且有实机图；正式注册仍使用旧占位；楔角当前ArtId=sigil_mauler，独立外形未补齐。elite_vanguard／prototype_hand已有静态资源。','不重复生成四个新单位；优先补楔角，候选与正式入库分开记录'),
      '敌人动作与状态姿态':(f"FormalEnemyAnimations64已有{d['fileCounts']['enemyAnimationPngs']}张PNG，目录并非空；当前CombatFormalVisualAssets.Unit不读取animationFrame，不能计为动作帧已接入。",'复用现有合格动作原料，先接完整单位动作样板，再扩展；不把文件数算成动作完成度'),
      '通用场地库':('散页、痕迹、约束纹、页幕、烟尘／火场序列、三类装置及碎晶候选已接入Editor预览并有局部实机证据；碎晶独立地表层可与物块／效果共存；正式替换及完整状态图仍独立记录。','基础辨识补缺已收束；保留现有规则，优先补仍误导身份或用途的少数项'),
      '区域地表与结构套件':(f"当前{d['totals']['levels']}图使用{d['totals']['environmentFamilies']}类HD2D环境；{d['totals']['groundPlacements']}处地面附属物覆盖{d['totals']['groundAssetTypes']}种资源；97张结构资源存在；111处旧结构坐标全部被现行地图过滤。已在5图棋盘外围恢复9处书架／工作台／工具柜，不改战术格。",'用途辨识先复用现有陈设；旧大型结构不能按12×9坐标直接恢复，其他区域特色作为后续表现补充'),
      '关卡应用组合':('当前编译目录18张战斗地图已完成资源绑定盘点，无缺失资源；图书馆与工坊有本次1920×1080／960×540隔离实机截图；不等同全部节点自然流程验收。','沿当前目录复用场地；不按旧内容数量重复生产，整场人工与自然流程证据另记'),
      '全阶段术式图标':(f"FormalSkillIcons32已有{d['fileCounts']['skillIconPngs']}张PNG；注册表已有{d['runtimeRegistry']['runtimeSkillBindings']}个运行时术式映射和{d['runtimeRegistry']['fireSpellBindings']}个个人术式映射；目录并非空，部分资源复用。",'停止依据旧“空目录”记录重生整套图标；仅补实际语义冲突或映射缺口'),
      '首领与机关表现':('首领静态资源与通用塔内机关独立候选已存在；机关悬停区分已放行／未放行；护障维护、显影巡查、冲压隔离仍共用外形和通用说明，不能计作三类用途均已清晰。','先补三类机关功能辨识；阶段专属表现按实际结算逐项接入，不把通用机座当成三种用途全部完成')}
    if kind in facts:
        state,action=facts[kind];setcell(row,keys,'现有状态',state);setcell(row,keys,'正式生产动作',action)
    if kind=='全阶段术式图标':
        setcell(row,keys,'范围或ID','现行80个个人术式及运行时基础术式');setcell(row,keys,'数量','80个个人术式映射＋27个运行时映射（可复用资源）')
def content(r,row,keys):
    if r['内容类型']=='内容表现':setcell(row,keys,'制作盘点状态','基础辨识候选齐备；18图陈设绑定盘点完成，5图9处外围家具已恢复；楔角独立外形、角色动作帧接入与塔内机关三类用途辨识仍待补；候选未晋升正式美术')
def matrix(r,row,keys):
    id=r['内容ID']
    if id=='E03':setcell(row,keys,'敌人资产缺口','老寻与灯台值守独立候选已生成并有实机图；正式注册仍为旧剪影占位，勿重复报缺候选')
    if id=='E01':setcell(row,keys,'现有地图资产状态','刻阵工坊外围已复用工作台与工具柜；旧越界结构未恢复；有本次实际关卡截图')
    if id=='E02':setcell(row,keys,'现有地图资产状态','校准室外围已复用工作台与工具柜；资源绑定与格外边界局部检查通过')
    if id=='B01':setcell(row,keys,'现有地图资产状态','首领及通用塔内机关独立候选存在；三类机关功能辨识和阶段专属表现仍需补齐')
update('OCC_学院第一阶段美术资产生产清单_v1.0.csv',production)
update('OCC_学院第一阶段全流程内容清单_v1.0.csv',content)
update('OCC_第一阶段战斗地图与敌人美术缺口矩阵_v1.0.csv',matrix)
print('Synced observed production/binding facts; mother-document scope and decisions unchanged')
