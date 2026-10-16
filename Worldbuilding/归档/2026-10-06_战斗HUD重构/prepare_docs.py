from pathlib import Path
import json, re, html, shutil
root = Path(__file__).resolve().parents[3]
archive = Path(__file__).resolve().parent
intro = '战斗界面取消固定右侧栏，战场使用底部指令区以上的完整宽度。人物生命、护盾、个人魔力与状态集中在左上角；顶部偏右常驻紧凑的预计行动顺序。右侧行动值页签默认收起，悬停展开原行动条，点击可固定展开；展开面板覆盖战场，不改变视口尺寸或镜头。操作流程见3.2.1，快捷键见5.2。人物概况与行动值仍各自支持翻面，Tab打开并固定行动值面板后同时翻转两区。'
layout = '战场参考视口为（16，80，1888，768），不再为右栏预留宽度。人物概况位于左上（16，80），正面416×248，背面按状态内容展开至416×280，使用不透明底色和1px细框。正面显示生命、护盾、个人魔力和状态图标，肉鸽无上限护盾只显示当前值；背面保留玩家状态和被动效果两栏及逐项悬停说明。顶部偏右（1032，80，824，96）常驻行动顺序：突出当前行动单位，其后按当前行动值、有效速度与固定顺序计算预计首次出手顺序，每个单位只出现一次，死亡或退出立即移除；超过七名时显示剩余数量。合法术式预览导致行动值变化时同步重排，取消预览恢复，悬停头像定位单位、点击聚焦。右侧页签位于（1860，320，44，136），明确显示行动值。悬停200ms展开（1432，240，416，436）行动值浮层；鼠标位于页签或面板内保持展开，移出350ms收起；点击页签或固定按钮切换固定展开，键盘及手柄选中页签同样可打开。浮层不挤压战场或移动镜头，收起后不拦截战场输入；打开阻塞弹窗或离开战斗时清除悬停并收起。行动值正面保留0／100／199轴、全局进度、最多五名单位的速度、当前值及预览变化，背面保留最新在前的可滚动真实战斗记录。数值与条形长度平滑过渡，顺位变化沿用绿色上箭头和红色下箭头。悬停行动行或顶部头像时，战场单位头顶显示银白定位箭头；与意图冲突时避让，移出立即隐藏。战斗抬头保留金币、学院贡献、学期剩余时间、当前回合与指令可用性，以及百科、背包、重开、暂离和离开入口；生命归入左上人物卡。关卡目标收回后只显示正式靶标图标。战场左下保留96×96行动点菱形徽章，以正式资源图标和当前／基础上限显示，不使用固定三格或AP文字。底部术式八槽、战术栏四槽和结束回合继续使用现有功能布局。'
data = json.loads((archive/'feishu-layout-before.json').read_text(encoding='utf-8-sig'))
content = data['data']['document']['content']
blocks = re.findall(r'<(p|h2) id="([^"]+)"[^>]*>(.*?)</\1>', content, re.S)
patches=[]
for tag,bid,body in blocks:
    text=re.sub('<[^>]+>','',body)
    new=None
    if '右侧英雄概况卡与行动值卡各自提供翻面按钮' in text: new=intro
    elif text=='A.4 右侧信息栏安全区': new='A.4 战斗悬浮HUD与行动值抽屉'
    elif text.startswith('右侧信息栏参考坐标为'): new=layout
    elif '上部地图75%、右HUD25%' in text:
        newbody=body.replace('上部地图75%、右HUD25%','上部地图使用全宽，左上人物状态、顶部偏右行动顺序与右侧悬停抽屉按A.4执行')
        newbody=newbody.replace('1408×768','1888×768').replace('7×4','9×4')
        patches.append((bid,tag,newbody)); continue
    elif '右侧HUD资源条预制体' in text:
        newbody=body.replace('右侧HUD','左上人物HUD');patches.append((bid,tag,newbody));continue
    if new: patches.append((bid,tag,html.escape(new)))
assert len(patches)>=4, len(patches)
for i,(bid,tag,body) in enumerate(patches):
    (archive/f'feishu-patch-{i}.xml').write_text(f'<{tag}>{body}</{tag}>',encoding='utf-8')
(archive/'feishu-patches.json').write_text(json.dumps([{'id':b,'file':f'feishu-patch-{i}.xml'} for i,(b,t,c) in enumerate(patches)],ensure_ascii=False,indent=2),encoding='utf-8')
(archive/'new-layout.txt').write_text(layout,encoding='utf-8')
(archive/'new-intro.txt').write_text(intro,encoding='utf-8')
print('Prepared targeted Feishu blocks:',len(patches))
