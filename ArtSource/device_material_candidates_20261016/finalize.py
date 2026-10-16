"""Preserve intact decoded subjects; place, outline and register QA candidates."""
import hashlib,json,shutil,subprocess,sys
from pathlib import Path
from PIL import Image,ImageOps
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[1]
UNITY=ROOT/'UnityProject/Assets/Game/Resources/Art/DeviceMaterialCandidates32';UNITY.mkdir(parents=True,exist_ok=True)
def rel(p):return p.relative_to(ROOT).as_posix()
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
for name,r,id in [('overload',2,'device_overload'),('ward',2,'device_ward_generator'),('tower',1,'device_tower_mechanism'),('shard',1,'material_crystal_shards')]:
    base=HERE/name;qa=base/'qa';report=qa/f'r{r}_decode.json';d=json.loads(report.read_text())
    assert d['status']=='PASS_LOSSLESS_TARGET' and d['subject_lattice_confidence']>=1.2
    image=Image.open(qa/f'r{r}_candidate.png').convert('RGBA');assert max(image.size)<=64
    crop=image.crop(image.getbbox());image=Image.new('RGBA',image.size)
    image.alpha_composite(crop,((image.width-crop.width)//2,(image.height-crop.height)//2 if name=='shard' else image.height-2-crop.height))
    alpha=image.getchannel('A')
    for y in range(image.height):
        for x in range(image.width):
            if alpha.getpixel((x,y)) and any(nx<0 or ny<0 or nx>=image.width or ny>=image.height or not alpha.getpixel((nx,ny)) for nx,ny in [(x-1,y),(x+1,y),(x,y-1),(x,y+1)]):image.putpixel((x,y),(0,0,0,255))
    outdir=base/'asset';outdir.mkdir(exist_ok=True);out=outdir/f'{id}.png';image.save(out)
    image.save(qa/'1x.png');image.resize((image.width*4,image.height*4),Image.Resampling.NEAREST).save(qa/'4x.png')
    gray=ImageOps.grayscale(image).convert('RGBA');gray.putalpha(alpha);gray.save(qa/'grayscale.png')
    checker=Image.new('RGBA',(image.width*4,image.height*4),(210,210,210,255))
    for y in range(0,checker.height,16):
        for x in range(0,checker.width,16):
            if (x//16+y//16)%2:checker.paste((245,245,245,255),(x,y,x+16,y+16))
    checker.alpha_composite(image.resize(checker.size,Image.Resampling.NEAREST));checker.save(qa/'checker.png')
    contact=Image.new('RGBA',image.size,(0,0,0,0))
    floor=Image.open(ROOT/'UnityProject/Assets/Game/Resources/Art/FormalAcademyIndependentFloors32/academy_block_court_b.png').convert('RGBA')
    for x in range(0,image.width,32):contact.alpha_composite(floor,(x,image.height-32))
    contact.alpha_composite(image);contact.save(qa/'contact.png')
    source=base/'raw'/f'r{r}_source.png';m=json.loads((ROOT/'Tools/OCCArt/occ_art_manifest_template_v1.json').read_text(encoding='utf-8-sig'))
    m.update(asset_id=id+'_candidate_20261016',role='single_cell_prop_32' if name=='shard' else 'single_cell_prop_adaptive_32ppu',status='QA_PENDING')
    m['provenance'].update(source_path=rel(source),source_sha256=sha(source),source_descriptor='Independent Codex built-in generation, subject-grid inferred, no geometric resizing')
    m['delivery'].update(output_path=rel(out),output_sha256=sha(out),native_ppu=32,logical_cells=[1,1],palette_max=10)
    m['display']['note']='Same 32 PPU as battlefield. Whole subject preserved on smallest fitting 32-multiple canvas; ground material is a horizontal card.'
    m['application'].update(runtime_draw_rect='One logical cell; horizontal32x32 material' if name=='shard' else 'One logical cell; visual extent from native canvas /32, bottom centre anchor',review_scope='Editor review only; semantic readability first; runtime/human review separate')
    m['evidence']={k:rel(qa/v) for k,v in dict(one_x='1x.png',four_x='4x.png',grayscale='grayscale.png',checker='checker.png',application_contact='contact.png').items()}
    m['human_review']['notes']='QA_PENDING. Prior user asked semantic readability before further polish; no automatic formal promotion.';m['unity_import']=None
    m['processing']={'decode_report':rel(report),'resize':False,'new_colours':'black external one-native-pixel outline only','alpha_preserved':True,'visible_bounds':list(image.getbbox()),'world_pixel_pitch':1/32,'native_ppu':32}
    path=outdir/f'{id}.occ-art-manifest-v1.json';path.write_text(json.dumps(m,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    subprocess.run([sys.executable,str(ROOT/'Tools/OCCArt/validate_occ_art_asset.py'),str(path),'--report',str(qa/'validation.json')],check=True,stdout=subprocess.DEVNULL)
    shutil.copyfile(out,UNITY/out.name);print(name,image.size,'machine PASS; QA_PENDING',flush=True)
