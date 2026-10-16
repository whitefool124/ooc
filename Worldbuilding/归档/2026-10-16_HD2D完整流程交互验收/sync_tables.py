from pathlib import Path
import csv,io,json,shutil
root=Path(__file__).resolve().parents[3];folder=Path(__file__).resolve().parent;changes=[]
names=['OCC_学院第一阶段美术资产生产清单_v1.0.csv','OCC_第一阶段战斗地图与敌人美术缺口矩阵_v1.0.csv','OCC_学院第一阶段全流程内容清单_v1.0.csv']
for name in names:
    p=root/'Worldbuilding/数据表'/name;backup=folder/'before/Worldbuilding/数据表'/name;backup.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,backup)
    rows=list(csv.reader(p.open(encoding='utf-8-sig',newline='')))
    for row in rows:
        before=row.copy()
        if name==names[0] and len(row)==9:
            if row[2]=='教程战场地表':
                row[6]='四关已接入现行HD-2D空间地表及环境；旧二维32×8前立面补图要求只适用于未迁移场景，不再作为当前HD-2D制作欠项。'
                row[7]='保留当前空间地表；完整流程核对地表、外沿、战术投影与点击归属，不另生产旧二维前立面'
            elif row[2]=='教程场地物件':
                row[6]='已有正式地表、灯藤、晶簇、掩体和宝箱；稳压晶簇身份及24点耐久已接入，新增场地与装置已默认取图。'
                row[7]='制作接入完成；完整流程检查受损／移除状态、用途文字、遮挡与交互，发现具体缺陷后修复'
            elif row[2] in ['教程装备／法宝／材料图标','战斗语义图标','维克多立绘与头像','学院地图节点系统']:
                row[7]='复用现有正式资源；ID映射、语义、实际页面与操作一致性归入完整流程验收，不因旧泛称待核对而重开整套生产'
            elif row[2]=='首领与机关表现':
                row[6]='首领静态资源、共用机关机座及护障／光带／冲压已接入；三类机关以功能名称、放行状态与效果说明区分。'
                row[7]='当前约定制作完成；完整流程核对名称、状态、线路与真实结算；不新增专用机关图形标识'
            elif row[2]=='关卡应用组合':
                row[8]='正常入口完整流程验收：可走格、阻挡、点击、遮挡与页面操作；整场审美品质独立验收'
        elif name==names[2] and len(row)==11 and row[3]=='内容表现':
            row[8]='当前约定范围的基础内容表现制作完成，详见美术生产清单；已启动正常入口至单轮总结的HD-2D交互验收。不得以局部截图替代完整旅程通过。'
            row[10]='验收HD-2D投影点击、悬停、拖放、滚动、弹窗阻挡、页面隔离、返回与续档；整场像素密度／光影／运动／性能／人工审美另记'
        if row!=before:changes.append({'file':name,'before':before,'after':row})
    out=io.StringIO(newline='');csv.writer(out,quoting=csv.QUOTE_ALL,lineterminator='\n').writerows(rows);p.write_text('\ufeff'+out.getvalue(),'utf-8')
(folder/'production_table_sync.json').write_text(json.dumps(changes,ensure_ascii=False,indent=2),'utf-8')
print('Tables cleaned:',len(changes),'rows; verification states retained')
