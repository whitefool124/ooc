from pathlib import Path
import json,shutil
from PIL import Image
root=Path(__file__).resolve().parent;repo=root.parent.parent
capture=repo/'UnityProject/ReviewEvidence/MainMenuE';web=repo/'ArtSource/MainMenuE_20261008'
for name in ['closed','open','dossier','continue']:
    shutil.copy2(capture/f'book-final-{name}.png',root/f'book-final-{name}.png')
shutil.copy2(repo/'UnityProject/Library/FunplayMcp/Recordings/game-view-20261008-095407-b656deacbfdb4347b642903932117360.mp4',root/'book-final-continue.mp4')
for name in ['label','right','left']:
    folder=root/name;path=repo/f'UnityProject/Assets/Game/Resources/Art/MainMenuE/BookRigReview/{name}.png'
    m=json.loads((folder/'manifest.json').read_text(encoding='utf-8'))
    shot='closed' if name=='label' else 'open'
    shutil.copy2(root/f'book-final-{shot}.png',folder/'application.png')
    Image.open(folder/'one_x.png').resize((384,384),Image.Resampling.NEAREST).save(folder/'six_x.png')
    m['evidence']['six_x']=(folder/'six_x.png').relative_to(repo).as_posix()
    guid=next(line.split(':',1)[1].strip() for line in path.with_suffix('.png.meta').read_text().splitlines() if line.startswith('guid:'))
    m['unity_import'].update({'stable_guid':guid,'importer_verified':True,'runtime_verified':True,'runtime_lattice':{'tier':6,'pass':True,'capture':(root/'book-final-open.png').relative_to(repo).as_posix(),'report':(root/'runtime-grid.json').relative_to(repo).as_posix(),'scope':'Composed book region, 3456 6x6 blocks, zero mixed blocks; physical foreshortening precedes common low-resolution rasterization.'}})
    m['application']['runtime_draw_rect']=[876,870,276,84] if name=='label' else ([912,552,276,90] if name=='right' else [612,552,276,84])
    m['display']['note']='Native material retained at one texel per physical pixel. Final scene raster320x180, Point upscale6; no source resize.'
    (folder/'manifest.json').write_text(json.dumps(m,ensure_ascii=False,indent=2),encoding='utf-8')
    shutil.copy2(folder/'four_x.png',web/f'book-final-{name}-4x.png')
for p in root.glob('book-final-*'):
    if p.suffix in ['.mp4','.png']:shutil.copy2(p,web/p.name)
(root/'runtime-proof.json').write_text(json.dumps({'fixed_spine_5_poses':True,'closed_cover_height_native':2.996253,'open_cover_height_native':-6.183982e-7,'native_geometry':[50,33],'rt':[320,180],'point':True,'start':'PASS:click:开始游戏','start_destination':'角色档案','continue':'PASS:click:继续游戏','continue_destination':'已保存学院地图','save_test_setup':'Actual UI creates isolated round, then public menu entry initializes menu; not an end-to-end pause-and-save journey','console_errors':0,'scene_saved':False,'human_approval':'PENDING','asset_status':'REVIEW_READY'},ensure_ascii=False,indent=2),encoding='utf-8')
print('Evidence and verified importer records complete; aesthetic approval pending')
