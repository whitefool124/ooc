"""Authorized E-menu scene resampling, NOT lossless lattice decoding.
Independent built-in generated scene states share one 240x135 grid and source-medoid palette.
"""
from pathlib import Path
import hashlib, json, shutil
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT/'ArtSource/MainMenuE_Coherent_20261008'
GEN = Path('C:/Users/FNHF/.codex/generated_images/01a1194c-ffa1-74a3-83af-fe2ec28eca2e')
SOURCES = {'scene_closed':'exec-4b01a184-492a-4f3b-86f4-d685b2bd6786.png',
           'scene_open':'exec-9eb948ad-84cd-473f-92b1-b0e2842051e9.png'}
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def rel(p):return p.relative_to(ROOT).as_posix()
OUT.mkdir(parents=True,exist_ok=True)
native={}
for name,source in SOURCES.items():
    folder=OUT/name;folder.mkdir(exist_ok=True)
    shutil.copyfile(GEN/source,folder/'source.png')
    native[name]=Image.open(folder/'source.png').convert('RGB').resize((240,135),Image.Resampling.NEAREST)

# Shared weighted medoids: every delivered RGB is an actual observed source RGB.
allpixels=np.concatenate([np.asarray(im).reshape(-1,3) for im in native.values()])
colors,counts=np.unique(allpixels,axis=0,return_counts=True)
values=colors.astype(np.float64); centers=[int(counts.argmax())]
distance=np.full(len(colors),np.inf)
for _ in range(23):
    distance=np.minimum(distance,((values-values[centers[-1]])**2).sum(axis=1))
    centers.append(int((distance*counts).argmax()))
palette=values[centers]
for _ in range(16):
    distances=((values[:,None,:]-palette[None,:,:])**2).sum(axis=2)
    assignments=distances.argmin(axis=1);updated=[]
    for k in range(24):
        members=np.flatnonzero(assignments==k)
        if not len(members):updated.append(palette[k]);continue
        center=np.average(values[members],axis=0,weights=counts[members])
        medoid=members[((values[members]-center)**2).sum(axis=1).argmin()]
        updated.append(values[medoid])
    updated=np.asarray(updated)
    if np.array_equal(updated,palette):break
    palette=updated
palette=palette.astype(np.uint8)
(OUT/'palette.json').write_text(json.dumps(palette.tolist()),encoding='utf-8')
for name,im in native.items():
    folder=OUT/name;px=np.asarray(im);flat=px.reshape(-1,3).astype(float)
    mapped=palette[((flat[:,None,:]-palette[None,:,:])**2).sum(axis=2).argmin(axis=1)].reshape(135,240,3)
    result=Image.fromarray(mapped).convert('RGBA');result.save(folder/'native.png');result.save(folder/'one_x.png')
    result.resize((960,540),Image.Resampling.NEAREST).save(folder/'four_x.png')
    result.resize((1920,1080),Image.Resampling.NEAREST).save(folder/'eight_x.png')
    result.convert('LA').save(folder/'grayscale.png');result.save(folder/'checker.png')
    manifest=json.loads((ROOT/'Tools/OCCArt/occ_art_manifest_template_v1.json').read_text(encoding='utf-8'))
    manifest.update(asset_id='ui.main_menu_e.'+name,role='ui_main_menu_scene_240x135',status='REVIEW_READY')
    manifest['provenance'].update(source_path=rel(folder/'source.png'),source_sha256=sha(folder/'source.png'),source_descriptor='Independent built-in generated whole scene; authorized E-menu scene_grid_resampling, 2026-10-08; not lossless lattice decoding')
    manifest['processing']={'method':'scene_grid_resampling','size':[240,135],'filter':'NEAREST','palette':'shared_observed_source_medoids','dither':False,'scope':'E main menu only'}
    manifest['delivery'].update(output_path=rel(folder/'native.png'),output_sha256=sha(folder/'native.png'),native_ppu=32,palette_max=24)
    manifest['display'].update(canonical_integer_scale=8,screen_px_per_native_px_at_canonical=8,note='All E scene states, atmosphere and camera sample one 240x135 native grid.')
    manifest['application']={'runtime_draw_rect':[0,0,1920,1080],'default_integer_scale':8,'minimum_integer_scale':4}
    for k in ('one_x','four_x','grayscale','checker'):manifest['evidence'][k]=rel(folder/(k+'.png'))
    manifest['evidence']['application_contact']=rel(folder/'four_x.png')
    manifest['human_review']['notes']='User selected E and authorized production methods unrestricted. This processed result remains review-ready, no formal aesthetic acceptance claim.'
    (folder/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
    print(name,'240x135',len(result.convert('RGB').getcolors(256)),'source-medoid colors')
