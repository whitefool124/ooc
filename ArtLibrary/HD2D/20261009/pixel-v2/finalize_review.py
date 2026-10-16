import json
import re
from pathlib import Path

HERE=Path(__file__).resolve().parent
ROOT=next(p for p in HERE.parents if (p/'UnityProject').is_dir())
parent=HERE.parent
catalog=json.loads((HERE/'catalog.json').read_text(encoding='utf-8'))
html=(HERE/'index.html').read_text(encoding='utf-8')
for x in catalog:
    pattern="(id:'"+x['id']+"',name:'[^']*',w:)\\d+(,h:)\\d+"
    html=re.sub(pattern,lambda m:m[1]+str(x['width'])+m[2]+str(x['height']),html)
html=html.replace('第一批 8 件，素材评审版。','第一批 8 件 · 像素版 v2。保留基础 32 位时代像素语言，尺寸自适应。')
html=html.replace('OCC · HD2D 可复用素材库 / 01','OCC · HD2D 像素素材库 / 01 v2')
html=html.replace('.asset img{display:block;','.asset img{image-rendering:pixelated;display:block;')
html=html.replace("areas:[[.20,.51,.60,.34]]","areas:[[.43,.22,.50,.52]]")
(HERE/'index.html').write_text(html,encoding='utf-8')
(parent/'index.html').write_text('<!doctype html><meta charset="utf-8"><meta http-equiv="refresh" content="0;url=pixel-v2/index.html"><a href="pixel-v2/index.html">打开像素版素材库 v2</a>',encoding='utf-8')
readme=(HERE/'README.md').read_text(encoding='utf-8')
readme=readme.replace('8 件独立内置生图原料及透明交付图。','8 件重新生成的像素风独立原料及透明交付图。用户补充：必须保留基础 32 位时代像素语言；自由尺寸不等于允许高清手绘。')
readme=readme.replace('后 7 件仅引用档案册统一材质与光影','后 7 件引用新像素档案册统一像素轮廓、色簇、材质与光影')
readme=readme.replace('Bilinear','Point')
readme += '\n旧手绘版已因用户指出画风不符撤出候选，原料仍在上级目录保留。新档案册首轮因书页透明错误淘汰，第二轮为当前母版。没有把旧手绘图程序像素化冒充新生图。\n'
(HERE/'README.md').write_text(readme,encoding='utf-8')
oldbatch=json.loads((parent/'batch-manifest.json').read_text(encoding='utf-8'))
oldbatch.update(status='REJECTED_STYLE',rejection='User 2026-10-09: must retain basic 32-bit-era pixel art. Painted version is withdrawn.',replacement='pixel-v2/batch-manifest.json')
(parent/'batch-manifest.json').write_text(json.dumps(oldbatch,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
for x in oldbatch['items']:
    path=parent/x['id']/'manifest.json'
    m=json.loads(path.read_text(encoding='utf-8'))
    m['human_review']={'overall':'FAIL','style':'FAIL','reason':'User rejected smooth painted style; retain basic 32-bit-era pixel art.'}
    m['candidate_withdrawn']=True
    path.write_text(json.dumps(m,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')

# Only clarify the new scoped exemption; retain all existing battlefield rules.
doc=ROOT/'Worldbuilding/策划案/OCC_项目总策划案_v1.0.md'
s=doc.read_text(encoding='utf-8')
s=s.replace('以美观、画风统一、完整剪影和实际可读性为优先，不要求严格像素对齐','必须保留既有32位时代像素美术语言、清晰像素色簇与阶梯轮廓，以美观、画风统一、完整剪影和实际可读性为优先，不要求严格像素对齐',1)
doc.write_text(s,encoding='utf-8')
agents=ROOT/'AGENTS.md'
s=agents.read_text(encoding='utf-8').replace('独立场景陈设优先美观与画风统一','独立场景陈设必须保留既有32位时代像素美术语言、清晰像素色簇与阶梯轮廓，优先美观与画风统一',1)
agents.write_text(s,encoding='utf-8')
csv=ROOT/'Worldbuilding/数据表/OCC_美术与界面规格表_v1.0.csv'
s=csv.read_text(encoding='utf-8').replace('美观和画风统一优先；允许Alpha处理与交付缩放','保留32位时代像素语言与清晰阶梯轮廓；美观和画风统一优先；允许Alpha处理与交付缩放',1)
csv.write_text(s,encoding='utf-8')
contract=ROOT/'Tools/OCCArt/occ_art_contract_v1.json'
c=json.loads(contract.read_text(encoding='utf-8-sig'))
c['roles']['hd2d_library_display']['pixel_style_required']=True
c['roles']['hd2d_library_display']['style_policy']='User clarified 2026-10-09: retain established 32-bit-era coarse pixel game art, visible clusters and stepped silhouette; no smooth painted illustration. Flexible display canvas does not mean forced 32x32.'
contract.write_text(json.dumps(c,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('Pixel v2 review activated; painted v1 withdrawn; scoped style requirement clarified.')
