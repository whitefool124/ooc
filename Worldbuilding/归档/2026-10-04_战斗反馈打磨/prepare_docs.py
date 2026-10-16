import json,xml.etree.ElementTree as ET,html
from pathlib import Path
out=Path(__file__).parent
root=out.parents[2]
master=root/'Worldbuilding/策划案/OCC_项目总策划案_v1.0.md'
text=master.read_text(encoding='utf-8-sig')
jobs=[]
def replace(cloud_name, block, transform):
    global text
    doc=json.loads((out/cloud_name).read_text(encoding='utf-8-sig'))['data']['document']
    xml=ET.fromstring(doc['content'])
    p=next(p for p in xml.iter('p') if p.get('id')==block)
    old=''.join(p.itertext())
    new=transform(old)
    if text.count(old)!=1: raise RuntimeError('Mirror mismatch: '+block)
    text=text.replace(old,new)
    path=out/(block+'.xml')
    path.write_text('<p>'+html.escape(new)+'</p>',encoding='utf-8')
    jobs.append({'id':block,'path':str(path.relative_to(root)).replace('\\','/'),'text':new})
replace('cloud-A8.json','doxcnGhvSFlO2sxVOazjP5NykVd',lambda s:s+' 敌方动作聚焦280ms，结算后移动／攻击／技能分别停留180／280／380ms，行动者间隔100ms；必须等待实际动作播放结束，关闭动画时跳过这些表现等待而仍逐项结算。全场总览按32屏幕单位的整数档适配并居中完整逻辑棋盘，不再仅恢复开场倍率。普通攻击按当前攻击定义区分轻近战前探、破甲重击蓄势、远程后坐和以太施术，保留静态角色母版及真实命中时序。')
replace('cloud-A14.json','doxcnOcgFLFLexAJQIoEn0pE6d7',lambda s:s.replace('左击己方角色身体进入移动模式，不立即移动，也不切换攻击模式','已选择自身术式时，点击己方身体优先预览该术式，再次点击相同格确认施放；其他动作下左击己方身体进入移动模式，不立即移动，也不切换攻击模式'))
replace('cloud-A16.json','doxcnFd9xK2pWao7EoCsFZzBIjd',lambda s:s+' 每次命中的护盾承伤与生命掉血分别显示实际数字，不跨资源或跨命中合并。数字使用固定字号和1px暗色描边，前80ms上提8px，至200ms保持可读位置，随后上浮至总计52px，480～720ms淡出；关闭动画时静态保留720ms。反馈不缩放字形、不接收输入，重开及离开战斗时清理。')
replace('cloud-input.json','doxcneWSSwK507FAMDLsdmcLh6c',lambda s:'第一次选择格子并保持预览；非移动动作再次左击同一格确认。切换动作、行动者或打开其他交互会取消待确认；移动仍使用双击可达格，不延迟自动提交其他动作。')
replace('cloud-input.json','doxcn9JCgfUDAWWwbMageND4xGe',lambda s:s+' 悬停菜单行预览该行动，点击该行直接确认。')
master.write_text(text,encoding='utf-8',newline='\n')
(out/'doc-jobs.json').write_text(json.dumps(jobs,ensure_ascii=False,indent=2),encoding='utf-8')
spec=root/'Worldbuilding/数据表/OCC_美术与界面规格表_v1.0.csv'
csv=spec.read_text(encoding='utf-8-sig')
csv=csv.replace('保留每条真实结果与原时长合并规则','盾承伤与生命损失分别显示实际数字且跨命中不合并；固定字号及1px暗描边；80ms上提8px后至200ms保持；480～720ms淡出；关闭动画静态720ms')
csv=csv.replace('视口16/80/1408/768；原生格32×32；','视口16/80/1408/768；全场总览按32屏幕单位整数档适配完整逻辑棋盘并居中；原生格32×32；')
csv=csv.replace('暂不接入单位动作换帧；保留真实路径命中时序','按实际攻击定义区分轻近战前探／破甲重击蓄势／远程后坐／以太施术；盾承伤使用较轻的受击位移；暂不接入单位动作换帧；保留真实路径命中时序')
spec.write_text(csv,encoding='utf-8',newline='\n')
