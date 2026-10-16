"""Place an accepted decoded subject intact; never resize or redraw anatomy."""
import argparse
import hashlib
import json
from pathlib import Path
from PIL import Image, ImageOps

ROOT = Path(__file__).resolve().parents[2]
BASE = Path(__file__).resolve().parent

def relative(path): return path.relative_to(ROOT).as_posix()
def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def write(path, value): path.write_text(json.dumps(value, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')

parser=argparse.ArgumentParser()
parser.add_argument('unit')
parser.add_argument('round',type=int)
args=parser.parse_args()
folder=BASE/args.unit
qa=folder/'qa'
decode=json.loads((qa/f'r{args.round}_decode.json').read_text(encoding='utf-8'))
assert decode['status']=='PASS_LOSSLESS_TARGET' and decode['subject_lattice_confidence']>=1.2
source=folder/'raw'/f'r{args.round}_source.png'
candidate=Image.open(qa/f'r{args.round}_candidate.png').convert('RGBA')
crop=candidate.crop(candidate.getbbox())
quadruped=args.unit=='elder_tracker_hound'
if quadruped:
    size=tuple(decode['target_canvas'])
    assert max(size)<=64 and all(v%32==0 for v in size)
    assert crop.width<=size[0]-2 and crop.height<=size[1]-2
else:
    assert crop.width<=30 and 46<=crop.height<=58
    size=(32,64)
baseline=size[1]-2
image=Image.new('RGBA',size)
offset=((size[0]-crop.width)//2,baseline+1-crop.height)
image.paste(crop,offset)
alpha=image.getchannel('A')
for y in range(size[1]):
    for x in range(size[0]):
        if alpha.getpixel((x,y)) and any(nx<0 or ny<0 or nx>=size[0] or ny>=size[1] or not alpha.getpixel((nx,ny))
            for nx,ny in [(x-1,y),(x+1,y),(x,y-1),(x,y+1)]):
            image.putpixel((x,y),(0,0,0,255))
output=folder/'asset'/f'{args.unit}_battle_candidate_{size[0]}x{size[1]}.png'
output.parent.mkdir(exist_ok=True)
image.save(output)
image.save(qa/'1x.png')
image.resize((size[0]*4,size[1]*4),Image.Resampling.NEAREST).save(qa/'4x.png')
image.resize((size[0]*6,size[1]*6),Image.Resampling.NEAREST).save(qa/'6x.png')
gray=ImageOps.grayscale(image).convert('RGBA');gray.putalpha(alpha);gray.save(qa/'grayscale.png')
checker=Image.new('RGBA',(size[0]*4,size[1]*4),(210,210,210,255))
for y in range(0,checker.height,16):
    for x in range(0,checker.width,16):
        if (x//16+y//16)%2: checker.paste((245,245,245,255),(x,y,x+16,y+16))
checker.alpha_composite(image.resize(checker.size,Image.Resampling.NEAREST));checker.save(qa/'checker.png')
# Local 2D scale contact only; this is explicitly not a normal-entry screenshot.
reference=Image.open(ROOT/'UnityProject/Assets/Game/Resources/Art/UnitStatic32x64/stone_snare.png').convert('RGBA')
contact=Image.new('RGBA',(64+size[0],72),(108,112,112,255))
contact.alpha_composite(reference,(8,4));contact.alpha_composite(image,(56,4+64-size[1]))
contact.save(qa/'scale_contact_1x.png')
contact.resize((contact.width*6,432),Image.Resampling.NEAREST).save(qa/'scale_contact_6x.png')
manifest=json.loads((ROOT/'Tools/OCCArt/occ_art_manifest_template_v1.json').read_text(encoding='utf-8'))
manifest.update(asset_id=args.unit+'_battle_candidate_20261016',role='tactical_unit_adaptive_32ppu' if quadruped else 'tactical_unit_32x64',status='QA_PENDING')
manifest['provenance'].update(source_path=relative(source),source_sha256=sha(source),
    source_descriptor='Independent Codex built-in generation; subject-inferred native grid; no geometric resizing')
manifest['delivery'].update(output_path=relative(output),output_sha256=sha(output),native_ppu=32,
    logical_cells=[1,1],palette_max=32)
manifest['display']['note']='Native 32 PPU battlefield sprite; migrated HD2D camera projects the same world pixel pitch as current 32 PPU units.'
manifest['application'].update(runtime_draw_rect=f'{size[0]}x{size[1]} at 32 PPU; logical cell 1x1; feet native Y{baseline}',
    review_scope='2D scale contact and opt-in Editor candidate only; normal-entry image and human review pending')
manifest['evidence']={key:relative(qa/name) for key,name in dict(one_x='1x.png',four_x='4x.png',six_x='6x.png',grayscale='grayscale.png',checker='checker.png',application_contact='scale_contact_1x.png').items()}
manifest['human_review']['notes']='Pending genuine human review. Local 2D scale contact is not in-game acceptance.'
manifest['unity_import']=None
manifest['processing']={'decoder_report':relative(qa/f'r{args.round}_decode.json'),'placement_offset':offset,
    'source_logical_size':list(crop.size),'geometric_resize':False,'outline':'Replace existing outermost opaque ring with black; preserve alpha',
    'native_world_pixel_pitch':1/32,'baseline_y':baseline,'palette_count':len({p[:3] for p in image.get_flattened_data() if p[3]})}
write(folder/'manifest.occ-art-manifest-v1.json',manifest)
print(json.dumps({'unit':args.unit,'output':relative(output),'bbox':image.getbbox(),'palette':manifest['processing']['palette_count']},ensure_ascii=False))
