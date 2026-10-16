from pathlib import Path
import csv,io,json,shutil,subprocess,xml.etree.ElementTree as ET
from xml.sax.saxutils import escape
root=Path(__file__).resolve().parents[3];folder=Path(__file__).resolve().parent
assert (folder/'runtime/completed.json').exists() and (folder/'runtime_branches/completed.json').exists()
assert ET.parse(folder/'tests_time_gate.xml').getroot().get('result')=='Passed'
doc='Y1KydFPjMocJjYxMmFEcTblcnLf';heading='doxcnpHNYA3yWhjDJuQjaApuQDg'
def call(args,record):
    result=subprocess.run(['lark-cli.cmd','docs',*args,'--as','user','--format','json'],cwd=root,capture_output=True,text=True,encoding='utf-8')
    (folder/record).write_text(result.stdout,'utf-8');data=json.loads(result.stdout)
    assert result.returncode==0 and data.get('ok'),data
    return data
before=call(['+fetch','--doc',doc,'--scope','section','--start-block-id',heading,'--detail','full'],'feishu_acceptance_before.json')
section=ET.fromstring(before['data']['document']['content']);changes=[]
for i,node in enumerate(section):
    old=''.join(node.itertext());new=None;done=False
    if old.startswith('【完整流程验收】'):
        done=True;new='【完整流程验收】正常建档、固定教程、随机学院层、提前申请首领、战斗、领奖及总结的后台正式存档路径已贯通；暂离续档、死亡重试、取消及确认放弃、返回首页已通过。原生窗口操作及故障分支另验'
    elif old.startswith('【HD-2D交互验收】'):
        new='【HD-2D交互验收】后台射线命中、地图空白区域缩放／拖动、背包悬停／拖放保存、服务操作和模态阻挡已验证；原生1920×1080／960×540窗口下的鼠标键盘、缩放和布局仍待验'
    if new:
        content=f'<checkbox done="{str(done).lower()}">{escape(new)}</checkbox>'
        data=call(['+update','--doc',doc,'--command','block_replace','--block-id',node.attrib['id'],'--content',content],f'feishu_acceptance_update_{i}.json')
        assert data['data']['result']=='success',data
        changes.append({'before':old,'after':new,'done':done})
after=call(['+fetch','--doc',doc,'--scope','section','--start-block-id',heading,'--detail','full'],'feishu_acceptance_after.json')
nodes=ET.fromstring(after['data']['document']['content'])
assert all(any(''.join(n.itertext())==c['after'] and n.attrib.get('done')==str(c['done']).lower() for n in nodes) for c in changes)
master=root/'Worldbuilding/策划案/OCC_项目总策划案_v1.0.md';backup=folder/'before_acceptance'/master.relative_to(root);backup.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(master,backup)
text=master.read_text('utf-8-sig');start=text.index('# 9. 三线并行设计待办');end=text.index('# 附录 A',start)
lines=['# 9. 三线并行设计待办','']+['- ['+('x' if n.attrib.get('done')=='true' else ' ')+'] '+''.join(n.itertext()) for n in nodes if n.tag=='checkbox']
master.write_text(text[:start]+'\n'.join(lines)+'\n\n'+text[end:],'utf-8')
(folder/'acceptance_mother_changes.json').write_text(json.dumps(changes,ensure_ascii=False,indent=2),'utf-8')
p=root/'Worldbuilding/数据表/OCC_学院第一阶段全流程内容清单_v1.0.csv';backup=folder/'before_acceptance'/p.relative_to(root);backup.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,backup)
rows=list(csv.reader(p.open(encoding='utf-8-sig',newline='')));changes=[]
for r in rows:
    if len(r)!=11:continue
    old=r.copy()
    if r[3]=='首领门槛':
        r[8]='后台正常存档路线已通过提前终考申请、取消不改当前位置、确认窗阻挡底层出发、再次确认进入首领；发现旧运行门槛28，与总案和生成表24冲突，已修正。23／24边界、结算保护与门槛保存两项定向检查通过；自然满24时序入口另验。'
    elif r[3]=='首领内容':
        r[8]+=' 本次已从正常建档与同一验收档进入随机学院层，经过提前申请进入首领、真实界面指令获胜、领取奖励并生成总结；后台多次重启续行，无临时加资源或直接胜利。'
        r[10]='完整后台主路径已贯通；原生窗口实际输入、窗口缩放和整体品质另验，不把离屏双分辨率截图当作外设实操'
    elif r[3]=='胜利结算':
        r[8]='正常建档至首领奖励、胜利总结的后台正式存档路径已贯通；路线、构筑、人物、资源及学院阶段完成标识已在总结读取。未做保存故障注入。'
        r[10]='原生窗口及保存失败重试另验；首次设备设置、CG和所有奖励分支不纳入本次主路径通过结论'
    elif r[3]=='失败与重试':
        r[8]+=' 本次正常建档B1获胜后进入B2，自然战败；通过结果页与确认窗重试，恢复开战前生命。取消放弃不改战局，确认放弃后保存主动放弃总结并返回首页。玩家三档内容不变。'
        r[10]='死亡后确认封存总结、异常档恢复、保存故障及原生窗口输入另验；本次已覆盖自然死亡重试和主动放弃返回'
    elif r[3]=='内容表现':
        r[8]='当前约定制作已完成；后台正常存档主路径已贯通。修复纸页皮肤误关地图空白输入、通用空间纸页正文模糊与服务注释溢出；地图手势、背包悬停／拖放保存、模态阻挡、页面返回与续档已验证。'
        r[10]='原生1920／960窗口鼠标键盘与缩放、全场景运动／密度／光影／性能／人工审美仍待验。PNG为离屏输出，实际后台Screen640×480。'
    elif r[3]=='服务节点池':
        r[8]+=' 本次正常档实际完成随机工坊选择目标并安装、医务室治疗；商店购买与工坊切页另以隔离内存页面补测通过，不能替代自然同档购买及全库存覆盖。'
    if r!=old:changes.append({'file':p.name,'before':old,'after':r})
out=io.StringIO(newline='');csv.writer(out,quoting=csv.QUOTE_ALL,lineterminator='\n').writerows(rows);p.write_text('\ufeff'+out.getvalue(),'utf-8')
(folder/'acceptance_table_changes.json').write_text(json.dumps(changes,ensure_ascii=False,indent=2),'utf-8')
print('Acceptance synchronized and read back:',len(changes),'table rows; native-window and quality gates retained')
