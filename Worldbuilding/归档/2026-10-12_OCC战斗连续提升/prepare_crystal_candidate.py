from pathlib import Path
import argparse,importlib.util,json,hashlib
from PIL import Image,ImageOps
ROOT=Path(__file__).resolve().parents[3];OUT=Path(__file__).resolve().parent
parser=argparse.ArgumentParser();parser.add_argument('--folder');parser.add_argument('--id',default='academy_aether_crystal_intact');parser.add_argument('--candidate-name',default='candidate.png');parser.add_argument('--decode-name',default='decode.json');args=parser.parse_args()
folder=(ROOT/args.folder).resolve() if args.folder else OUT/'crystal_density_r4'
assert folder.is_relative_to(OUT),'Candidate source must remain in this archive'
spec=importlib.util.spec_from_file_location('outline',ROOT/'Tools/OCCArt/normalize_native32_generated_asset.py')
module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
source=folder/'raw.png';decoded=folder/args.candidate_name
qa=json.loads((folder/args.decode_name).read_text(encoding='utf-8'));assert qa['status']=='PASS_LOSSLESS_TARGET' and qa['subject_lattice_confidence']>=1.2
im=Image.open(decoded).convert('RGBA');result=module.black_outermost_pixel(im)
assert result.getchannel('A').tobytes()==im.getchannel('A').tobytes()
delivery=folder/'albedo.png';result.save(delivery)
result.save(folder/'1x.png');result.resize((256,256),Image.Resampling.NEAREST).save(folder/'4x.png')
gray=ImageOps.grayscale(result);gray.putalpha(result.getchannel('A'));gray.save(folder/'grayscale.png')
board=Image.new('RGBA',(256,256),(220,220,220,255))
for y in range(0,256,16):
 for x in range(0,256,16):
  if (x//16+y//16)%2:board.paste((170,170,170,255),(x,y,x+16,y+16))
board.alpha_composite(result.resize((256,256),Image.Resampling.NEAREST));board.convert('RGB').save(folder/'checker.png')
floor=Image.open(ROOT/'UnityProject/Assets/Game/Resources/Art/FormalAcademyIndependentFloors32/academy_block_court_b.png').convert('RGBA')
contact=Image.new('RGBA',(96,64),(0,0,0,255))
for y in range(0,64,32):
 for x in range(0,96,32):contact.alpha_composite(floor,(x,y))
contact.alpha_composite(result,(16,0));contact.resize((384,256),Image.Resampling.NEAREST).save(folder/'source_contact.png')
assert args.id in ['academy_aether_crystal_intact','academy_aether_crystal_damaged','academy_pressure_crystal_intact','academy_pressure_crystal_damaged']
dest=ROOT/f'UnityProject/Assets/Game/Resources/Art/HD2DBattleCrystalCandidates/{args.id}.png'
dest.parent.mkdir(parents=True,exist_ok=True);result.save(dest)
def bind(p):return dict(path=p.relative_to(ROOT).as_posix(),sha256=hashlib.sha256(p.read_bytes()).hexdigest())
manifest=dict(schema='occ-art-manifest-v1',contract_version=1,asset_id='academy_aether_crystal_intact_density_candidate_20261012',role='single_cell_prop_adaptive_32ppu',status='QA_PENDING',provenance=dict(source_channel='codex_builtin_imagegen',source_descriptor='Independent built-in mineral base-color generation r4; no source resizing or albedo recolouring',source_path=bind(source)['path'],source_sha256=bind(source)['sha256']),delivery=dict(output_path=bind(dest)['path'],output_sha256=bind(dest)['sha256'],native_output_path=bind(delivery)['path'],native_ppu=32,logical_cells=[1,1],palette_max=24),normalization=dict(record=bind(folder/'decode.json'),source_macro_pixel_pitch=qa['inferred_subject_pixel_pitch'],source_grid_confidence=qa['subject_lattice_confidence'],geometric_resize=False,outline='Replace existing outermost opaque native cells by one black native cell; no alpha expansion or extra ring'),application=dict(runtime_draw_rect='64x64 visual canvas at32PPU, root foot/grid unchanged; only explicitly enabled Editor QA preview',world_pixel_pitch=1/32,formal_replacement=False),evidence={k:bind(folder/f)['path'] for k,f in [('one_x','1x.png'),('four_x','4x.png'),('grayscale','grayscale.png'),('checker','checker.png')]},human_review=dict(overall='PENDING',scene_pixel_size_consistency='PENDING'),unity_import=dict(asset_path=dest.relative_to(ROOT/'UnityProject').as_posix(),native_ppu=32,formal_promotion=False),blockers=['Candidate only; complete scene density, actual application and human art review pending'])
manifest['application'].update(default_integer_scale=1,minimum_integer_scale=1,scale_fields_scope='Native source diagnostics only; actual HD2D remains perspective at32PPU',source_contact_scope='Flat source/tile contact diagnostic, not runtime scene or human approval')
manifest['evidence']['application_contact']=bind(folder/'source_contact.png')['path']
manifest['asset_id']=args.id+'_density_candidate_20261012'
manifest['provenance']['source_descriptor']='Independent built-in mineral base-colour generation, selected source and grid record bound below; no geometric resize'
manifest['normalization']['record']=bind(folder/args.decode_name)
(folder/'albedo_manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps(dict(status='QA_PENDING',canvas=result.size,subject=result.getchannel('A').getbbox(),lattice=qa['subject_lattice_confidence'])))
