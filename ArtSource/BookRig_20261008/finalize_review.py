from pathlib import Path
import json, shutil
import numpy as np
from PIL import Image

root=Path(__file__).resolve().parent
repo=root.parent.parent
capture=repo/'UnityProject/ReviewEvidence/MainMenuE'
web=repo/'ArtSource/MainMenuE_20261008'
for name in ['rig-closed','rig-mid','rig-late','rig-open','rig-dossier','rig-continue']:
    shutil.copy2(capture/(name+'.png'),root/(name+'.png'))
shutil.copy2(repo/'UnityProject/Library/FunplayMcp/Recordings/game-view-20261008-093133-64f18896217a4fcc82803c002575190a.mp4',root/'rig-continue.mp4')
a=np.array(Image.open(root/'rig-open.png').convert('RGB'))[480:672,780:1152].reshape(32,6,62,6,3)
mixed=int(np.any(a!=a[:,0:1,:,0:1,:],axis=(1,3,4)).sum())
proof={'date':'2026-10-08','project':'E:/数据库/OCC_Codex/UnityProject','geometry':{'poses':[0,.25,.5,.75,1],'fixed_hinge':True,'closed_cover_height_native':2.996253,'open_cover_height_native':-3.054738e-7,'pass':True},'runtime_lattice':{'tier':6,'crop':[780,480,372,192],'blocks':1984,'mixed_blocks':mixed,'pass':mixed==0},'render_texture':{'size':[320,180],'filter':'Point'},'ui':{'start':'PASS:click:开始游戏','destination':'角色档案','continue':'PASS:click:继续游戏','continue_destination':'已保存学院地图','setup':'实际按钮创建本轮存档，主菜单使用 EnterMainMenuFromOpening 初始化；从主菜单点击继续游戏。','isolated_save_scope':True},'videos':{'loop':{'seconds':10,'frames':248,'purpose':'LateUpdate review driver; no document overlay, not an entry flow'},'start':{'seconds':8,'frames':152,'purpose':'actual button path'},'continue':{'seconds':8,'frames':143,'purpose':'actual button path'}}}
(root/'runtime-proof.json').write_text(json.dumps(proof,ensure_ascii=False,indent=2),encoding='utf-8')
m=json.loads((root/'manifest.json').read_text(encoding='utf-8'))
m['unity_import']['runtime_lattice']=proof['runtime_lattice']|{'scope':'Open-book region of actual 1920x1080 Game View; 6px blocks. Material texels are foreshortened by physical surfaces before 320x180 rasterization.'}
m['display']['note']='All physical surfaces rasterize into the shared 320x180 Point output. Static native subject stays 21x26; it is not enlarged to fill its 64x64 canvas.'
(root/'manifest.json').write_text(json.dumps(m,ensure_ascii=False,indent=2),encoding='utf-8')
for path in root.glob('rig-*'):
    if path.suffix in ['.mp4','.png']:shutil.copy2(path,web/path.name)
for name in ['one_x.png','four_x.png','checker.png','grayscale.png']:
    shutil.copy2(root/name,web/('rig-'+name))
print(json.dumps(proof,ensure_ascii=False))
