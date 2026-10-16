"""Finalize losslessly decoded field candidates; no geometric resizing of assets."""
import hashlib,json,shutil,subprocess,sys
from pathlib import Path
from PIL import Image,ImageOps
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[1]
UNITY=ROOT/'UnityProject/Assets/Game/Resources/Art/FieldEffectCandidates32'
sys.path.insert(0,str(ROOT/'Tools/OCCArt'))
from decode_generated_pixel_grid import consolidate_to_observed_palette
UNITY.mkdir(parents=True,exist_ok=True)
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def rel(p):return p.relative_to(ROOT).as_posix()
def finish(name,key,id,role,upright=False,source=None):
    base=HERE/name;qa=base/'qa';report=qa/f'{key}_decode.json'
    d=json.loads(report.read_text());assert d['status']=='PASS_LOSSLESS_TARGET' and d['subject_lattice_confidence']>=1.2
    src=base/'raw'/(source or f'{key.split("_")[0]}_source.png')
    image=Image.open(qa/f'{key}_candidate.png').convert('RGBA')
    if role=='single_cell_prop_32':image=consolidate_to_observed_palette(image,9)
    bounds=image.getbbox();crop=image.crop(bounds)
    image=Image.new('RGBA',(32,32));image.alpha_composite(crop,((32-crop.width)//2,30-crop.height if upright else (32-crop.height)//2))
    if role=='single_cell_prop_32':
        alpha=image.getchannel('A')
        for y in range(32):
            for x in range(32):
                if alpha.getpixel((x,y)) and any(nx<0 or ny<0 or nx>=32 or ny>=32 or not alpha.getpixel((nx,ny)) for nx,ny in [(x-1,y),(x+1,y),(x,y-1),(x,y+1)]):image.putpixel((x,y),(0,0,0,255))
    asset=base/'asset';asset.mkdir(exist_ok=True);out=asset/f'{id}.png';image.save(out)
    evidence=qa/id;evidence.mkdir(exist_ok=True)
    image.save(evidence/'1x.png');image.resize((128,128),Image.Resampling.NEAREST).save(evidence/'4x.png')
    gray=ImageOps.grayscale(image).convert('RGBA');gray.putalpha(image.getchannel('A'));gray.save(evidence/'grayscale.png')
    checker=Image.new('RGBA',(128,128),(210,210,210,255))
    for y in range(0,128,16):
        for x in range(0,128,16):
            if (x//16+y//16)%2:checker.paste((245,245,245,255),(x,y,x+16,y+16))
    checker.alpha_composite(image.resize((128,128),Image.Resampling.NEAREST));checker.save(evidence/'checker.png')
    floor=Image.open(ROOT/'UnityProject/Assets/Game/Resources/Art/FormalAcademyIndependentFloors32/academy_block_court_b.png').convert('RGBA');floor.alpha_composite(image);floor.save(evidence/'contact.png')
    manifest=json.loads((ROOT/'Tools/OCCArt/occ_art_manifest_template_v1.json').read_text(encoding='utf-8-sig'))
    manifest.update(asset_id=id+'_candidate_20261016',role=role,status='QA_PENDING')
    manifest['provenance'].update(source_path=rel(src),source_sha256=sha(src),source_descriptor='New independent Codex built-in image generation; regular sheet crops if applicable; subject-inferred grid; no geometric resize')
    manifest['delivery'].update(output_path=rel(out),output_sha256=sha(out),native_ppu=32,logical_cells=None if role=='vfx_frame_32' else [1,1],palette_max=12 if role=='vfx_frame_32' else 10)
    manifest['display']['note']='One cell; 32 PPU, source pixel pitch 0.03125m. Upright one-metre canvas with common bottom-centre anchor.' if upright else 'Horizontal one-cell decal, same 32 PPU as battlefield.'
    manifest['application'].update(runtime_draw_rect='1x1 metre, bottom centre at cell centre' if upright else '1x1 metre, horizontal cell',review_scope='Opt-in Editor artwork review; runtime capture and human approval pending')
    manifest['evidence']={k:rel(evidence/v) for k,v in dict(one_x='1x.png',four_x='4x.png',grayscale='grayscale.png',checker='checker.png',application_contact='contact.png').items()}
    manifest['human_review']['notes']='PENDING. Source contact composition is not a game screenshot.';manifest['unity_import']=None
    manifest['processing']={'decode_report':rel(report),'geometric_resize':False,'new_colours':'black one-native-pixel external contour for prop roles only','source_subject_preserved':True,'placement':'bottom centre y30' if upright else 'centre','upright':upright,'native_ppu':32,'world_pixel_pitch':1/32,'visible_bounds':list(image.getbbox())}
    if '_' in key:
        source_index=int(key.split('_')[1]);source_crop=base/'raw'/f'{key.split("_")[0]}_crop_{source_index:02}.png'
        manifest['processing']['source_crop']={'path':rel(source_crop),'sha256':sha(source_crop),'sheet_cell':[source_index%3,source_index//3],'sheet_layout':[3,2],'cropped_only':True,'geometric_rescale':False}
    path=asset/f'{id}.occ-art-manifest-v1.json';path.write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    subprocess.run([sys.executable,str(ROOT/'Tools/OCCArt/validate_occ_art_asset.py'),str(path),'--report',str(evidence/'validation.json')],stdout=subprocess.DEVNULL,check=True)
    check=json.loads((evidence/'validation.json').read_text());assert not check.get('errors'),check
    shutil.copyfile(out,UNITY/out.name)
    return image
for n,k,id in [('trace','r3','field_trace'),('binding','r3','field_binding_mark'),('screen','r4','field_paper_screen')]:finish(n,k,id,'single_cell_prop_32',n=='screen')
for name,r,indices in [('smoke','r3',[0,1,2,4,5,0]),('fire','r2',list(range(6)))]:
    id='field_smoke' if name=='smoke' else 'field_burning';frames=[]
    for slot,source_index in enumerate(indices):frames.append(finish(name,f'{r}_{source_index:02}',f'{id}_{slot:02}','vfx_frame_32',True))
    frames[0].resize((256,256),Image.Resampling.NEAREST).save(HERE/name/'qa/animation.gif',save_all=True,append_images=[f.resize((256,256),Image.Resampling.NEAREST) for f in frames[1:]],duration=125,loop=0,disposal=2)
    (HERE/name/'qa/sequence.json').write_text(json.dumps({'source_round':r,'source_frame_indices':indices,'fps':8,'unique_keyframes':len(set(indices)),'closing_hold':'first pose repeated on closing exposure' if name=='smoke' else None,'rejected_source_frames':[3] if name=='smoke' else [],'world_canvas_m':[1,1],'max_authored_extent_m':[max((f.getbbox()[2]-f.getbbox()[0])/32 for f in frames),max((f.getbbox()[3]-f.getbbox()[1])/32 for f in frames)],'geometric_frame_scale':False},indent=2),encoding='utf-8')
print('Finalized 15 QA_PENDING candidates, including 5 unique smoke keys + closing hold and 6 unique fire keys')
