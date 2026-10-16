from pathlib import Path
import json

root=Path(__file__).resolve().parents[3]
master=root/'Worldbuilding/策划案/OCC_项目总策划案_v1.0.md'
s=master.read_text(encoding='utf-8-sig')
s=s.replace('装备、法宝、术式图标与非战场单格资产保持32×32，','装备、法宝、术式图标保持32×32；战斗外界面展示物件按完整主体与安全边确定画布，不强制32×32或32倍数，以画风统一、自然界面尺度与承载接触成立为准（用户确认，2026-10-03）；')
s=s.replace('固定尺寸的地面、图标和界面资产保持各自合同尺寸；','固定尺寸的地面、战术与功能图标保持各自合同尺寸；战斗外界面物件采用完整主体加安全边的自适应画布，不因超过32×32而淘汰；')
master.write_text(s,encoding='utf-8')
csv=root/'Worldbuilding/数据表/OCC_美术与界面规格表_v1.0.csv'
s=csv.read_text(encoding='utf-8-sig').replace('ART-ASSET-32,资产,装备法宝术式与非战场单格物件,32×32,','ART-ASSET-32,资产,装备法宝术式图标,32×32,')
s+='\nART-UI-DETAIL-ADAPTIVE,战斗外界面,杯碟等展示物件,完整主体加安全边自适应画布不要求32×32或32倍数,与所在页面统一的像素材质与画风,尺度自然且有真实承托；不挤压文字或按钮；保留来源透明度调色板与人工审美；不覆盖战场资产合同\n'
csv.write_text(s,encoding='utf-8')
path=root/'Tools/OCCArt/occ_art_contract_v1.json'
c=json.loads(path.read_text(encoding='utf-8-sig'));c['roles']['ui_detail_adaptive']={'delivery_canvas':'nonbattlefield_adaptive','logical_cells':None,'palette_max':16,'transparent_border_min':1,'unity_ppu':32,'scope':'nonbattlefield_display_only','acceptance':'Unified art style, complete silhouette, natural UI scale and actual support contact; no fixed 32px canvas.'}
path.write_text(json.dumps(c,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
path=root/'Tools/OCCArt/native32_generation_profiles.json';c=json.loads(path.read_text(encoding='utf-8-sig'));c['nonbattlefield_display_exception']={'confirmed':'2026-10-03','canvas':'complete subject plus safe border; no fixed size or 32px multiple','priority':'consistent art style and natural UI scale','battlefield_contract_unchanged':True};path.write_text(json.dumps(c,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
path=root/'AGENTS.md';s=path.read_text(encoding='utf-8-sig');s+='\n- **战斗外展示物件尺寸（用户确认，2026-10-03）**：杯碟等界面物件以画风统一、完整剪影、自然显示尺度与承载接触为准；画布按主体加安全边自适应，不强套32×32或32倍数。该条优先于上文对非战场物件固定尺寸的旧表述，不改变战场及既有战术／功能图标规格；保留来源和人工审美记录。\n';path.write_text(s,encoding='utf-8')
