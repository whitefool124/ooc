from pathlib import Path
import csv,json
root=Path('E:/数据库/OCC_Codex')
p=root/'Tools/OCCArt/occ_art_contract_v1.json'
d=json.loads(p.read_text(encoding='utf-8-sig'))
d['unit_pixel_input']={
    'ownership':'opaque displayed body pixels resolve to owning gameplay cell; existing hover/left/right command handlers retained',
    'transparency':'source alpha >=128 is hittable; transparent pixels pass to next rendered unit or grid',
    'coordinates':'current RectTransform, UV including flip, event camera and parent viewport clipping',
    'cache':'one bit per texel; one capture per distinct texture at binding; non-readable source uses temporary GPU readback without importer mutation; no readback in pointer events',
    'lifecycle':'components created lazily on occupied cells; cache scoped to view; readback failure leaves grid input and logs once',
    'invariants':['no auto attack-mode switch','existing drag/scroll hierarchy','existing command eligibility and double-click movement','art files GUID and importer unchanged'],
    'review':'four production Canvas resolution/zoom cases plus overlap and transparency checked in EditMode; continuous Play input and runtime profiling remain pending'
}
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
p=root/'Worldbuilding/数据表/OCC_美术与界面规格表_v1.0.csv'
with p.open(encoding='utf-8-sig',newline='') as f: rows=list(csv.DictReader(f))
fields=list(rows[0]);rows=[r for r in rows if r['ID']!='ART-UNIT-PIXEL-INPUT']
rows.append(dict(zip(fields,['ART-UNIT-PIXEL-INPUT','交互表现','越格角色输入归属','身体Alpha≥128映射所属格；透明穿透；重叠按绘制深度','当前Rect/UV/事件相机与视口裁剪；沿用格位指令','1bit/像素缓存；绑定时按贴图读取一次；指针事件无读回；组件按需建立；Importer不变；四显示组合射线通过；Play连续输入待验'])))
with p.open('w',encoding='utf-8-sig',newline='') as f:
    w=csv.DictWriter(f,fieldnames=fields,lineterminator='\n');w.writeheader();w.writerows(rows)
print('Unit input mirrors synchronized; gameplay content and v3 approval unchanged.')
