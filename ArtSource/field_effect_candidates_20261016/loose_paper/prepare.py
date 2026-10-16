"""Finalize the intact inferred paper cluster; no geometric resizing."""
import hashlib,json
from pathlib import Path
from PIL import Image,ImageOps

HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[2]
QA=HERE/'qa'
decode=json.loads((QA/'selected_decode.json').read_text(encoding='utf-8'))
assert decode['status']=='PASS_LOSSLESS_TARGET' and decode['subject_lattice_confidence']>=1.2
image=Image.open(QA/'selected_candidate.png').convert('RGBA')
alpha=image.getchannel('A')
for y in range(32):
    for x in range(32):
        if alpha.getpixel((x,y)) and any(nx<0 or ny<0 or nx>=32 or ny>=32 or not alpha.getpixel((nx,ny))
            for nx,ny in [(x-1,y),(x+1,y),(x,y-1),(x,y+1)]):
            image.putpixel((x,y),(0,0,0,255))
output=HERE/'asset/field_loose_paper_32.png'
image.save(output);image.save(QA/'1x.png')
image.resize((128,128),Image.Resampling.NEAREST).save(QA/'4x.png')
image.resize((192,192),Image.Resampling.NEAREST).save(QA/'6x.png')
gray=ImageOps.grayscale(image).convert('RGBA');gray.putalpha(alpha);gray.save(QA/'grayscale.png')
checker=Image.new('RGBA',(128,128),(210,210,210,255))
for y in range(0,128,16):
    for x in range(0,128,16):
        if (x//16+y//16)%2:checker.paste((245,245,245,255),(x,y,x+16,y+16))
checker.alpha_composite(image.resize((128,128),Image.Resampling.NEAREST));checker.save(QA/'checker.png')
floor=Image.open(ROOT/'UnityProject/Assets/Game/Resources/Art/FormalAcademyIndependentFloors32/academy_block_court_b.png').convert('RGBA')
floor.alpha_composite(image);floor.save(QA/'floor_contact_1x.png')
floor.resize((192,192),Image.Resampling.NEAREST).save(QA/'floor_contact_6x.png')
def relative(path):return path.relative_to(ROOT).as_posix()
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
source=HERE/'raw/r2_source.png'
manifest=json.loads((ROOT/'Tools/OCCArt/occ_art_manifest_template_v1.json').read_text(encoding='utf-8'))
manifest.update(asset_id='field_loose_paper_candidate_20261016',role='single_cell_prop_32',status='QA_PENDING')
manifest['provenance'].update(source_path=relative(source),source_sha256=sha(source),
    source_descriptor='Independent Codex built-in generated flat loose-paper cluster; subject grid inferred; no resizing')
manifest['delivery'].update(output_path=relative(output),output_sha256=sha(output),native_ppu=32,logical_cells=[1,1],palette_max=10)
manifest['display']['note']='Flat horizontal ground-effect card: same 32 PPU and 0.03125m authored pixel as current battlefield ground.'
manifest['application'].update(runtime_draw_rect='One 32x32 horizontal cell card; nonblocking loose-paper effect, not a blocking prop',
    review_scope='2D floor contact and opt-in Editor field preview; game camera and human approval pending')
manifest['evidence']={k:relative(QA/v) for k,v in dict(one_x='1x.png',four_x='4x.png',six_x='6x.png',grayscale='grayscale.png',checker='checker.png',application_contact='floor_contact_1x.png').items()}
manifest['human_review']['notes']='PENDING: floor contact is a 2D composition, not an in-game capture.'
manifest['unity_import']=None
manifest['processing']=dict(decode_report=relative(QA/'selected_decode.json'),geometric_resize=False,
    outline='replace outermost opaque ring only; unchanged alpha',world_pixel_pitch=1/32,canvas=[32,32],
    alpha_unchanged=True,palette=len({p[:3] for p in image.get_flattened_data() if p[3]}))
(HERE/'manifest.occ-art-manifest-v1.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps(manifest['processing']))
