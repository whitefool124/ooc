"""Pixel audit of real 2x icons captured inside the encyclopedia stencil mask."""
from pathlib import Path
import csv,json
import numpy as np
from PIL import Image
folder=Path(__file__).resolve().parent
repo=folder.parents[3]
results={}
for name in ['aca_eq_ch03','aca_eq_ch01','aca_eq_hd01']:
    path=repo/'UnityProject/Assets/Game/Resources/Art/FormalAcademyEquipmentIcons32'/f'{name}.png'
    source=np.asarray(Image.open(path).convert('RGBA'))
    solid=np.repeat(np.repeat(source[:,:,3]>0,2,0),2,1)
    black=np.repeat(np.repeat((source[:,:,:3].max(axis=2)==0)&(source[:,:,3]>0),2,0),2,1)
    images=[Image.open(folder/f'{name}-{i:02}.png').convert('RGB') for i in range(22)]
    frames=np.stack([np.asarray(im) for im in images])
    moving=np.any(frames!=frames[-1],axis=(0,3))
    unlit=np.asarray(Image.open(folder/f'{name}-unlit.png').convert('RGB'))
    lit=np.asarray(Image.open(folder/f'{name}-lit.png').convert('RGB'))
    side=np.any(unlit!=lit,axis=2)
    assert frames.shape[1:3]==solid.shape==(64,64)
    assert side.sum()>0 and not (side & ~solid).any() and not (side & black).any(),name
    print(name,'moving',int(moving.sum()),'outside',int((moving & ~solid).sum()),'black',int((moving & black).sum()))
    assert not (moving & ~solid).any() and not (moving & black).any(),name
    if name=='aca_eq_ch01':assert moving.sum()==0,'Cloth glinted like metal'
    else:assert moving.sum()>0,'No visible hover effect'
    if name=='aca_eq_hd01':
        y,x=np.mgrid[:32,:32];u=(x+.5)/32;v=1-(y+.5)/32
        lens=(((u-.328125)/.0625)**2+((v-.421875)/.09375)**2<=.70)|(((u-.671875)/.0625)**2+((v-.265625)/.09375)**2<=.70)
        lens=np.repeat(np.repeat(lens,2,0),2,1)
        assert not (moving & ~lens).any(),'Refraction escaped lenses'
    results[name]=dict(side_light_changed_pixels=int(side.sum()),hover_changed_pixels=int(moving.sum()),
        changed_outside_alpha=0,changed_black_outline_pixels=0,frames=22)
    times=[float(line.split()[0]) for line in (folder/f'{name}-times.txt').read_text().splitlines()]
    durations=[max(20,round((times[i+1]-times[i])*1000)) for i in range(21)]+[1200]
    enlarged=[im.resize((384,384),Image.Resampling.NEAREST) for im in images]
    enlarged[0].save(folder/f'{name}-runtime.gif',save_all=True,append_images=enlarged[1:],duration=durations,loop=0)
(folder/'pixel-evidence.json').write_text(json.dumps(results,ensure_ascii=False,indent=2),encoding='utf-8')
ledger=repo/'Worldbuilding/归档/2026-10-03_六十项小细节实装/执行状态.csv'
with ledger.open(encoding='utf-8-sig',newline='') as f:rows=list(csv.DictReader(f))
updates={10:('LOCAL_VERIFIED_PIXELS','第四批实际金属悬停像素通过；布料无动态反光，外轮廓不变'),
31:('LOCAL_VERIFIED','第四批金属／布料／玻璃低强度侧光实机像素通过；范围为已识别装备材质'),
33:('LOCAL_VERIFIED_PIXELS','第四批图鉴Mask渲染副本相位同步，实际金属与镜片反光通过，动画0及切页清理通过')}
for row in rows:
    if int(row['编号']) in updates:row['状态'],row['说明']=updates[int(row['编号'])]
assert len(rows)==60
with ledger.open('w',encoding='utf-8-sig',newline='') as f:
    w=csv.DictWriter(f,fieldnames=['编号','模式','状态','说明']);w.writeheader();w.writerows(rows)
print(json.dumps(results,ensure_ascii=False))
