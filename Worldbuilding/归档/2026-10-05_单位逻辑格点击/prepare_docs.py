from pathlib import Path
import json,xml.etree.ElementTree as ET,html,shutil
root=Path('.');out=root/'Worldbuilding/归档/2026-10-05_单位逻辑格点击';master=root/'Worldbuilding/策划案/OCC_项目总策划案_v1.0.md';spec=root/'Worldbuilding/数据表/OCC_美术与界面规格表_v1.0.csv';contract=root/'Tools/OCCArt/occ_art_contract_v1.json'
for f in [master,spec,contract,root/'UnityProject/Assets/Game/Runtime/Presentation/FormalBattlefieldView.cs',root/'UnityProject/Assets/Game/Runtime/Presentation/CombatUnitPixelRaycast.cs',root/'UnityProject/Assets/Game/Tests/EditMode/CombatUnitPixelRaycastTests.cs']:shutil.copy2(f,out/'backup'/f.name)
text=master.read_text(encoding='utf-8-sig');jobs=[]
for filename in ['cloud-A14.json','cloud-A15.json']:
 d=json.loads((out/filename).read_text(encoding='utf-8-sig'));assert d['ok'];xml=ET.fromstring(d['data']['document']['content'])
 target=[p for p in xml.iter('p') if ('角色已绘制的非透明像素' if 'A14' in filename else '单位生命条及其左端护盾') in ''.join(p.itertext())];assert len(target)==1
 p=target[0];old=''.join(p.itertext());assert text.count(old)==1
 if 'A14' in filename:
  new='所有单位（英雄与敌人）的悬停、左键和右键均以单位当前所在的完整逻辑格为入口，不使用身体贴图、透明像素、越格剪影或动作位移扩展热区。点击该格内没有身体像素的空白处仍操作该格单位；贴图越入相邻格时，点击越格部分操作指针所在的相邻格。生命、护盾、魔力条、状态与意图徽记均不拦截战场输入，透传至指针所在逻辑格。已选择自身术式时，点击己方所在格优先预览该术式，再次点击相同格确认施放；其他动作下左击己方所在格进入移动模式，不立即移动，也不切换攻击模式；右键打开指针所在格的情境行动菜单。格位热区随棋盘平移与缩放变化，服从战场视口裁剪；移动与动作播放期间沿用操作门控。'
 else:
  new=old.replace('单位生命条及其左端护盾图标数字组提供所属格的悬停、左键和右键入口；空格、透明像素和装饰轮廓不得拦截操作。','单位生命、护盾、魔力条与头顶状态及意图只显示信息，不提供独立单位点击入口；全部透传至指针所在逻辑格。单位所在格的空白区域仍可选择该格单位，越格贴图、透明像素和装饰轮廓均不得拦截相邻格操作。')
 text=text.replace(old,new);f=out/(p.get('id')+'.xml');f.write_text('<p>'+html.escape(new)+'</p>',encoding='utf-8');jobs.append({'id':p.get('id'),'path':f.as_posix()})
master.write_text(text,encoding='utf-8');(out/'doc-jobs.json').write_text(json.dumps(jobs,ensure_ascii=False),encoding='utf-8')
lines=spec.read_text(encoding='utf-8-sig').splitlines()
for i,line in enumerate(lines):
 if line.startswith('ART-UNIT-PIXEL-INPUT,'):lines[i]='ART-UNIT-PIXEL-INPUT,交互表现,单位逻辑格输入归属,所有英雄与敌人使用所在完整逻辑格；格内空白可选；越格身体透传至指针所在格,格位热区随棋盘平移缩放并服从视口裁剪；沿用真实格位指令与门控,身体生命护盾魔力状态意图不接收战场输入；保留原图轮廓与深度；不以Alpha判断点击；右键菜单和自施术按格位处理'
 if line.startswith('ART-UNIT-ANNOTATIONS,'):lines[i]=line.replace('位移徽记沿用所属格点击悬停','位移徽记不拦截输入；点击悬停均透传至指针所在格')
spec.write_text('\n'.join(lines)+'\n',encoding='utf-8')
c=json.loads(contract.read_text(encoding='utf-8-sig'));c['unit_pixel_input']={'ownership':'all heroes and enemies use their complete logical cell for hover, left and right click; blank pixels inside the occupied cell remain selectable','transparency':'unit bodies, vitals, status and intent annotations are visual only; overhang passes input to the logical cell under the pointer','coordinates':'grid hit surfaces follow board pan/zoom and viewport clipping; sprite Rect/UV/alpha and visual motion do not alter input ownership','cache':'alpha masks remain only for visual occlusion contours; no alpha readback for input','lifecycle':'only battlefield cell roots carry unit targeting pointers; legacy pixel raycast components cannot enable body input','invariants':['existing self-spell preview and same-cell confirmation','existing command eligibility and double-click movement','existing context menu, drag and scroll hierarchy','art files GUID and importer unchanged']};contract.write_text(json.dumps(c,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('Prepared',len(jobs),'exact cloud blocks and synchronized local rules')
