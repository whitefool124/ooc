from pathlib import Path
import csv,io,json,shutil,subprocess,xml.etree.ElementTree as ET
from xml.sax.saxutils import escape
root=Path(__file__).resolve().parents[3];folder=Path(__file__).resolve().parent
snapshot=json.loads((folder/'feishu_section9_before.json').read_text('utf-8-sig'))
section=ET.fromstring(snapshot['data']['document']['content'])
updates={
'【游戏内容线】完成4个服务节点':(True,'【游戏内容线】完成工坊、医务室和商店三类共用服务；固定教程及随机层节点按第6章、第7章映射复用'),
'【美术表现线】完成14场战斗界面与表现':(True,'【美术表现线·制作】固定教程四战及随机层12个普通、4个精英节点位与B01首领均已接入当前战斗目录、单位、场地和基础反馈；共用底图按节点映射复用，整场品质及完整流程另验'),
'【美术表现线】完成8个事件界面与表现':(True,'【美术表现线·制作】3个固定教程事件及随机层EV01、EV08、EV09、T02已接入HD-2D内容页、逐事件选项与结果表现；复用现有纸页和陈设，不另要求8张插画'),
'【美术表现线】完成4个服务节点界面与表现':(True,'【美术表现线·制作】工坊、医务室、商店共用服务页面已接入独立布局、功能标识、清晰正文与实际交易；8种餐食使用增益图标和说明'),
'【美术表现线】完成1个首领界面与表现':(True,'【美术表现线·制作】古塔核心首领、三类共用机关机座、护障／光带／冲压及公开名称、状态和效果说明已接入；不另要求专用机关图形标识'),
'【美术表现线】完成文案、像素资产、动效、音效和最终验收':(True,'【美术表现线·制作】当前约定范围的文案、静态单位、场地资源、图标、局部位移／抖动、烟火序列与声音已接入；胜利／战败／放弃总结已有结果标识和清晰正文；不要求成套角色动作帧'),
'【美术表现线】像素显示管线第一阶段验收：在实际运行入口检查 1×/2×/4× 输出、窗口缩放与镜头移动':(False,'【完整流程验收】从正常入口建档，完成固定教程、随机学院层、首领、领奖与总结；覆盖暂离续档、死亡重试、主动放弃和返回入口'),
'【美术表现线】像素显示管线第二阶段：连续地表与战术网格分层重构':(True,'【HD-2D架构·制作】现行战场已使用空间地表、独立战术投影及交互层；旧二维连续地表重构不再作为第一阶段新增生产项'),
'【美术表现线】像素显示管线第三阶段：角色、物件、UI、字体与 VFX 的统一像素密度母版':(False,'【HD-2D品质验收】按A.2.4核对角色、物件、地表、纸板与模型贴图的同场景像素密度、接触、遮挡、光影及性能；像素VFX按尺度例外单独核对，源像素与清晰正文分别检查'),
'【美术表现线】像素显示管线第四阶段：移除非整数缩放动画并改为像素稳定过渡':(False,'【HD-2D交互验收】在1920×1080及960×540检查实际点击命中、悬停、拖放、滚动、弹窗阻挡、场景投影、页面切换与返回；发现问题修复后回归'),
}
for node in section:
    text=''.join(node.itertext())
    if text.startswith('【美术表现线】按 A.2 收口战场显示倍率'):
        updates[text]=(False,'【HD-2D显示与运动验收】已迁移场景按A.2.4使用高清输出；验证窗口缩放、实际镜头与合法缩放下的像素稳定、文字可读和点击对应，不以旧整帧六倍／固定可见格数验收HD-2D场景')
history='【美术表现线】像素显示管线第一阶段（历史实现记录，适用于未迁移场景）'
for node in section:
    text=''.join(node.itertext())
    if text.startswith(history):updates[text]=(True,'【历史记录】未迁移二维场景保留Point／关Mipmap／关压缩和整数输出实现；已迁移HD-2D使用A.2.4，不全局关闭HDR，也不以历史完成标记替代品质验收')

responses=[]
for index,node in enumerate(section):
    old=''.join(node.itertext())
    if old not in updates:continue
    done,new=updates[old]
    content=f'<checkbox done="{str(done).lower()}">{escape(new)}</checkbox>'
    args=['lark-cli.cmd','docs','+update','--doc','Y1KydFPjMocJjYxMmFEcTblcnLf','--as','user','--command','block_replace','--block-id',node.attrib['id'],'--content',content,'--format','json']
    result=subprocess.run(args,cwd=root,capture_output=True,text=True,encoding='utf-8')
    (folder/f'feishu_update_{index}.json').write_text(result.stdout,'utf-8')
    data=json.loads(result.stdout)
    assert result.returncode==0 and data.get('ok') and data['data']['result']=='success',data
    responses.append({'old':old,'new':new,'done':done})
result=subprocess.run(['lark-cli.cmd','docs','+fetch','--doc','Y1KydFPjMocJjYxMmFEcTblcnLf','--as','user','--scope','section','--start-block-id',section[0].attrib['id'],'--detail','full','--format','json'],cwd=root,capture_output=True,text=True,encoding='utf-8')
(folder/'feishu_section9_after.json').write_text(result.stdout,'utf-8')
after=json.loads(result.stdout);updated=ET.fromstring(after['data']['document']['content'])
assert all(any(''.join(n.itertext())==r['new'] and n.attrib.get('done')==str(r['done']).lower() for n in updated) for r in responses)
master=root/'Worldbuilding/策划案/OCC_项目总策划案_v1.0.md'
back=folder/'before/Worldbuilding/策划案';back.mkdir(parents=True,exist_ok=True);shutil.copy2(master,back/master.name)
text=master.read_text('utf-8-sig');start=text.index('# 9. 三线并行设计待办');end=text.index('# 附录 A',start)
lines=['# 9. 三线并行设计待办','']
for node in updated:
    if node.tag=='checkbox':lines.append('- ['+('x' if node.attrib.get('done')=='true' else ' ')+'] '+''.join(node.itertext()))
master.write_text(text[:start]+'\n'.join(lines)+'\n\n'+text[end:],'utf-8')
(folder/'backlog_sync_changes.json').write_text(json.dumps(responses,ensure_ascii=False,indent=2),'utf-8')
print('Mother chapter 9 updated and read back; local chapter mirrored:',len(responses),'items')
