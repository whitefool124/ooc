"""Diagnose independently generated room layers; never fit assets by resizing."""
from pathlib import Path
import sys,json,io,shutil
import hashlib
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
sys.path[:0]=[str(ROOT/'ArtSource/_pixel_tools/deps'),str(ROOT/'ArtSource/_pixel_tools')]
from pixelfixer.api import process
from prepare_main_menu_native64 import background_grid,evidence
from decode_generated_pixel_grid import consolidate_to_observed_palette
ART=ROOT/'ArtSource/MainMenuE_Modular_20261008'
def candidate(name,revision,step=None,strict=False):
    folder=ART/name
    if strict:
        im=Image.open(folder/'decoded.png').convert('RGBA')
        report=json.loads((folder/'subject-qa.json').read_text());assert report['confidence']>=1.2
        report['route']='PASS_SUBJECT_LATTICE'
    elif step:
        result=process(np.array(Image.open(folder/f'source-r{revision}.png').convert('RGBA')),force_step=step)
        im=Image.open(io.BytesIO(result.pop('png'))).convert('RGBA');report=result
        report.update(route='COMMUNITY_RECONSTRUCTION_REVIEW_ONLY',lossless_pass=False,grid_basis='Observed coarse contour/material step; finer half-step reconstruction retains broad pixel clusters. Not a numerical confidence pass.')
    else:
        im=Image.open(folder/(f'auto-r{revision}.png' if (folder/f'auto-r{revision}.png').exists() else 'auto.png')).convert('RGBA')
        report=json.loads((folder/(f'community-grid-r{revision}-qa.json' if (folder/f'community-grid-r{revision}-qa.json').exists() else 'community-grid-qa.json')).read_text())
        report.update(route='COMMUNITY_RECONSTRUCTION_REVIEW_ONLY',lossless_pass=False)
    # Preserve alpha silhouette, harden generated fringe, retain source palette medoids.
    im.putalpha(im.getchannel('A').point(lambda a:255 if a>=128 else 0))
    if name!='vista':
        im=im.crop(im.getchannel('A').getbbox());out=Image.new('RGBA',(im.width+2,im.height+2));out.alpha_composite(im,(1,1));im=out
    im=consolidate_to_observed_palette(im,32 if name=='vista' else 16)
    im.save(folder/'native.png');evidence(folder,im)
    report.update(native_size=list(im.size),arbitrary_target_resizing=False,human_acceptance=False)
    (folder/'candidate-qa.json').write_text(json.dumps(report,indent=2))
    template=json.loads((ROOT/'ArtSource/MainMenuE_Albedo_20261008/background/manifest.json').read_text(encoding='utf8'))
    rel=lambda file:str(file.relative_to(ROOT)).replace('\\','/')
    sha=lambda file:hashlib.sha256(file.read_bytes()).hexdigest()
    template.update(asset_id='ui.main_menu_e.modular.'+name,role='ui_scene_plate_native' if name=='vista' else 'ui_detail_adaptive',status='REVIEW_READY')
    template['provenance'].update(source_path=rel(folder/f'source-r{revision}.png'),source_sha256=sha(folder/f'source-r{revision}.png'),source_descriptor='Independent Codex built-in imagegen asset. '+report['route'])
    template['delivery'].update(output_path=rel(folder/'native.png'),output_sha256=sha(folder/'native.png'),palette_max=32 if name=='vista' else 16)
    template['evidence'].update({k:rel(folder/file) for k,file in [('one_x','one_x.png'),('four_x','four_x.png'),('grayscale','grayscale.png'),('checker','checker.png')]})
    template['evidence']['application_contact']='UnityProject/ReviewEvidence/MainMenuE/menu-modular-room.png'
    template['human_review']['notes']='Empty room + independently generated composition asset; no human acceptance claimed.'
    template['processing']=report
    (folder/'manifest.json').write_text(json.dumps(template,ensure_ascii=False,indent=2),encoding='utf8')
    print('candidate',name,im.size,flush=True)
def diagnose(name,revision=1):
    folder=ART/name;src=Image.open(folder/f'source-r{revision}.png').convert('RGBA')
    _,strict=background_grid(src)
    (folder/f'strict-grid-r{revision}-qa.json').write_text(json.dumps(strict,indent=2))
    result=process(np.array(src),mode='full')
    decoded=Image.open(io.BytesIO(result.pop('png'))).convert('RGBA')
    decoded.save(folder/f'auto-r{revision}.png')
    (folder/f'community-grid-r{revision}-qa.json').write_text(json.dumps(result,indent=2))
    print(name,decoded.size,result,flush=True)
def compose():
    stage=ROOT/'UnityProject/Assets/Game/Resources/Art/MainMenuE/Stage'
    layers=[];canvas=Image.new('RGBA',(320,180),(39,31,26,255))
    room=Image.open(ART/'room/native.png').convert('RGBA')
    left=(320-room.width)//2;top=10
    window=Image.new('L',(320,180));holes=room.getchannel('A').point(lambda a:255 if a==0 else 0)
    # Only enclosed window apertures; transparent safety perimeter is excluded.
    holes.paste(0,(0,0,room.width,2));holes.paste(0,(0,room.height-2,room.width,room.height));holes.paste(0,(0,0,2,room.height));holes.paste(0,(room.width-2,0,room.width,room.height));window.paste(holes,(left,top))
    vista=Image.open(ART/'vista/native.png').convert('RGBA');view=Image.new('RGBA',(320,180));view.alpha_composite(vista,(20,-15));view.putalpha(window)
    view.save(ART/'vista/windowed.png');view.save(stage/'vista.png');canvas.alpha_composite(view)
    layers.append(dict(name='vista',x=0,y=0,width=320,height=180,order=0,lit=False))
    places={'room':(left,top,1),'shelf':(left+4,46,2),'plant':(left+78,70,2),'banner':(left+room.width-40,12,2),'desk':((320-269)//2,120,3)}
    for name,(x,y,order) in places.items():
        im=Image.open(ART/name/'native.png').convert('RGBA');canvas.alpha_composite(im,(x,y));im.save(stage/(name+'.png'))
        normal=Image.new('RGBA',im.size,(128,218,218,255) if name=='desk' else (128,128,255,255));normal.putalpha(im.getchannel('A'));normal.save(stage/(name+'_normal.png'))
        layers.append(dict(name=name,x=x,y=y,width=im.width,height=im.height,order=order,lit=True))
    # Existing accepted direction of independent prop sources remains intact.
    for name,x,y in [('book_closed',137,103),('cup',212,94),('lamp',251,100)]:
        im=Image.open(stage/(name+'.png')).convert('RGBA');canvas.alpha_composite(im,(x,y))
    canvas.save(ART/'assembled.png');canvas.resize((1920,1080),Image.Resampling.NEAREST).save(ART/'assembled-six.png')
    data=dict(layers=layers,grid=[320,180],source='ArtSource/MainMenuE_Modular_20261008',normal_data='technical flat material-plane normals, never new albedo art',pixel_geometry_resized=False,review='PENDING human acceptance')
    (ART/'composition.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf8');(stage/'modular-composition.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf8')
if __name__=='__main__':
    if sys.argv[1:]==['--compose']:compose()
    elif sys.argv[1:]==['--candidates']:
        candidate('room',2);candidate('desk',2,7.5);candidate('vista',1)
        candidate('shelf',1);candidate('plant',1,strict=True);candidate('banner',1,21)
    else:
        for arg in sys.argv[1:]:
            parts=arg.split(':');diagnose(parts[0],int(parts[1]) if len(parts)>1 else 1)
