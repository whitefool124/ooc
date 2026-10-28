from pathlib import Path
from PIL import Image, ImageOps, ImageDraw
import json, hashlib, shutil

R=Path(__file__).resolve().parents[2]
A=Path(__file__).resolve().parent
G=Path('C:/Users/FNHF/.codex/generated_images/01a15dd0-4607-7e52-894c-a4e209d0be14')
sources={'keeper':'exec-726816a2-f7e1-48e0-b9f2-5729036a7384.png','LK-01':'exec-714d6508-9663-40c2-9dc0-7cd0a00ac779.png','LK-02':'exec-cf0ed54a-9c37-423d-af78-1aa0e0a047e9.png','LK-03':'exec-b8b726c0-6971-4a51-8d95-f08f0a150f20.png','LK-04':'exec-72ea4f3b-0420-4fdf-b145-756828ffd079.png','LK-05':'exec-b9745cfc-a0a8-4856-9dae-c239b10a90a0.png','LK-06':'exec-f4f881bb-9bc3-4b6a-832a-ebbe462c0401.png'}
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
records=[]
contract=json.loads((R/'Tools/OCCArt/occ_art_contract_v1.json').read_text(encoding='utf-8-sig'))
for id,name in sources.items():
    raw=A/'raw'/f'{id}.png';shutil.copy2(G/name,raw)
    image=Image.open(raw).convert('RGBA');bounds=image.getchannel('A').getbbox()
    assert bounds and bounds!=(0,0,image.width,image.height),'Expected native transparent source'
    subject=image.crop(bounds);limit=84 if id=='keeper' else 44
    ratio=limit/max(subject.size);size=tuple(max(1,round(v*ratio)) for v in subject.size)
    native=subject.resize(size,Image.Resampling.NEAREST)
    canvas=Image.new('RGBA',(64,96) if id=='keeper' else (64,64))
    canvas.alpha_composite(native,((canvas.width-native.width)//2,(canvas.height-native.height)//2))
    output=A/'delivery'/f'{id}.png';canvas.save(output)
    e=A/'evidence'/id;e.mkdir(parents=True,exist_ok=True)
    canvas.save(e/'1x.png');canvas.resize((canvas.width*4,canvas.height*4),Image.Resampling.NEAREST).save(e/'4x.png')
    gray=ImageOps.grayscale(canvas).convert('RGBA');gray.putalpha(canvas.getchannel('A'));gray.save(e/'grayscale.png')
    checker=Image.new('RGBA',canvas.size,(205,205,205,255));d=ImageDraw.Draw(checker)
    for y in range(0,canvas.height,8):
        for x in range(0,canvas.width,8):
            if (x//8+y//8)%2:d.rectangle((x,y,x+7,y+7),fill=(240,240,240,255))
    checker.alpha_composite(canvas);checker.save(e/'checker.png')
    record={'asset_id':id,'source_path':raw.relative_to(R).as_posix(),'source_sha256':sha(raw),'source_size':image.size,'alpha_bounds':bounds,'subject_delivery_size':size,'delivery_canvas':canvas.size,'processing':'Crop transparent margin; uniform nearest-neighbor scaling for noncombat HD2D display role; center with safe border. No repaint, recolor, palette creation or synthetic details.','reference':'UnityProject/Assets/Game/Resources/Art/MainMenuE/Stage/lamp.png','ui_display_scale':4,'density_review':'PENDING: authored cluster comparison and actual scene review; delivery texel equality is not proof of source pixel consistency'}
    records.append(record)
    manifest={'schema':'occ-art-manifest-v1','asset_id':'lampkeeper-'+id,'status':'QA_PENDING','role':'hd2d_library_display','provenance':{'source_channel':'codex_builtin_imagegen','source_descriptor':'Independent built-in image generation; blue-gold academy lamp pixel/material reference; noncombat gift display object','source_path':record['source_path'],'source_sha256':sha(raw)},'delivery':{'output_path':output.relative_to(R).as_posix(),'output_sha256':sha(output),'logical_cells':None,'palette_max':1000000},'application':{'rendering_mode':'noncombat_hd2d_layered_sprite','camera_projection':'perspective','runtime_draw_rect':'keeper 256x384 / gift 256x256 at 1920 reference; all images 4x native delivery texels','pixel_density_reference':record['reference']},'evidence':{k:(e/v).relative_to(R).as_posix() for k,v in [('one_x','1x.png'),('four_x','4x.png'),('grayscale','grayscale.png'),('checker','checker.png')]},'human_review':{'overall':'PENDING','reviewer':'','date':'','notes':'Candidate generation and agent visual inspection do not establish genuine human aesthetic approval.'},'unity_import':None}
    manifest['contract_version']=contract['version']
    manifest['placement_semantics']={'application_scope':'runtime_layout','mount_type':'tabletop','logical_cell':None,
        'native_anchor':[canvas.width//2,canvas.getchannel('A').getbbox()[3]-1],
        'visible_contact_line':{'axis':'y','native_px':canvas.getchannel('A').getbbox()[3]-1},
        'depth_sort_baseline':canvas.getchannel('A').getbbox()[3]-1,'allowed_occlusion_direction':'clipped by parent paper only',
        'screen_axis':'horizontal_vertical_locked','support_asset':'UnityProject/Assets/Game/Resources/UI/HD2D/lampkeeper.prefab'}
    manifest_path=A/'manifests'/f'{id}.json'
    if not manifest_path.exists():
        manifest_path.write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
(A/'processing.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf-8')
board=Image.new('RGBA',(7*288,448),(243,232,204,255))
for i,id in enumerate(sources):
    p=Image.open(A/'delivery'/f'{id}.png');p=p.resize((p.width*4,p.height*4),Image.Resampling.NEAREST);board.alpha_composite(p,(i*288+16,(416-p.height)//2))
    ImageDraw.Draw(board).text((i*288+16,420),id,fill=(35,28,20))
board.convert('RGB').save(A/'contact.png')
print('prepared 7 independent display candidates; no formal promotion')
