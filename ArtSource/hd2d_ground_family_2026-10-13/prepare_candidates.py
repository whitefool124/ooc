"""Register independent generated sources to native cells without resizing art.

39px is diagnosed from the enlarged reference-aligned source edges. Keep the
generic inference and registered-grid results separate; ambiguous generic
confidence and real human registration remain explicitly pending.
"""
import json, hashlib, math, sys, argparse
from pathlib import Path
from collections import Counter
from PIL import Image, ImageOps
root=Path(__file__).resolve().parents[2]
source_batch=Path(__file__).resolve().parent
parser=argparse.ArgumentParser();parser.add_argument('--round',type=int,default=4);parser.add_argument('--set',default='')
args=parser.parse_args()
if args.set not in {'','quiet'}:raise ValueError('Known source set required')
batch=source_batch/'sets'/args.set if args.set else source_batch
batch.mkdir(parents=True,exist_ok=True)
for folder in ['decoded','delivery','diagnostics','evidence']:(batch/folder).mkdir(exist_ok=True)
sys.path.insert(0,str(root/'Tools/OCCArt'))
from decode_generated_pixel_grid import infer_subject_lattice,decode_subject_lattice,consolidate_to_observed_palette,select_ground_window
from validate_occ_art_asset import validate_manifest

def rel(p):return p.relative_to(root).as_posix()
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def dump(p,data):p.write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')

# Numeric surface data from a declared shallow slab lip, not from diffuse tones.
normal=Image.new('RGBA',(32,32));rough=Image.new('RGBA',(32,32))
def height(x,y):
    x=max(0,min(31,x));y=max(0,min(31,y))
    return 0 if min(x,y,31-x,31-y)==0 else .002
for y in range(32):
    for x in range(32):
        dx=(height(x+1,y)-height(x-1,y))*16
        dy=(height(x,y-1)-height(x,y+1))*16
        n=(-dx,-dy,1);length=math.sqrt(sum(v*v for v in n))
        normal.putpixel((x,y),tuple(round((v/length*.5+.5)*255) for v in n)+(255,))
        r=round((.98 if min(x,y,31-x,31-y)==0 else .88)*255)
        rough.putpixel((x,y),(r,r,r,255))
(batch/'material_data').mkdir(exist_ok=True)
normal.save(batch/'material_data/normal.png');rough.save(batch/'material_data/roughness.png')
dump(batch/'material_data/recipe.json',dict(schema='occ-authored-ground-surface-v1',world_pixel_pitch=1/32,
    geometry='flat slab interior at 2mm, outer native-cell seam at zero; same geometry for every variant',
    diffuse_brightness_used=False,roughness=dict(stone=.88,seam=.98),metallic=0,emission=0,
    normal='linear RGB +Y tangent space, A=1, native point sampling',human_review='PENDING'))

contract=json.loads((root/'Tools/OCCArt/occ_art_contract_v1.json').read_text(encoding='utf-8-sig'))
outputs=[]
for variant in ['a','b','c']:
    asset='academy_court_limestone_'+variant
    src=source_batch/'raw'/f'{asset}_r{args.round}.png'
    image=Image.open(src).convert('RGBA')
    if image.getchannel('A').getextrema()!=(255,255):raise ValueError('Opaque ground source required')
    auto=infer_subject_lattice(image)
    pitch,x,y,confidence,bounds=infer_subject_lattice(image,39)
    decoded=decode_subject_lattice(image,bounds,pitch,x,y)
    cleaned=consolidate_to_observed_palette(decoded,5)
    # Remove an incomplete duplicate perimeter cell only; keep all four complete edges.
    origin=(int(0<x<pitch/2),int(0<y<pitch/2))
    output,window=select_ground_window(cleaned,(32,32),origin)
    if confidence<1.2 or output is None:raise ValueError(f'{asset}: registered lattice unavailable {confidence}')
    palette=Counter(output.get_flattened_data())
    perimeter=[output.getpixel((x,0)) for x in range(32)]+[output.getpixel((x,31)) for x in range(32)]+[output.getpixel((0,y)) for y in range(32)]+[output.getpixel((31,y)) for y in range(32)]
    if len(set(perimeter))!=1:raise ValueError(f'{asset}: incomplete/unregistered perimeter; do not repair source')
    raw_colours=set(image.get_flattened_data())
    if any(c not in raw_colours for c in palette):raise ValueError('Invented colour')
    if palette.most_common(1)[0][1]/1024<.45:raise ValueError('Dominant tone below 45%')
    (batch/'evidence'/asset).mkdir(parents=True,exist_ok=True)
    out=batch/'delivery'/f'{asset}.png';output.save(out)
    decoded.save(batch/'decoded'/f'{asset}_r{args.round}_opaque.png')
    evidence={}
    for label,scale in [('one_x',1),('four_x',4),('six_x',6),('checker',4)]:
        p=batch/'evidence'/asset/f'{label}.png';output.resize((32*scale,32*scale),Image.Resampling.NEAREST).save(p);evidence[label]=rel(p)
    p=batch/'evidence'/asset/'grayscale.png';ImageOps.grayscale(output).save(p);evidence['grayscale']=rel(p)
    contact=Image.new('RGBA',(96,96))
    for cy in range(3):
        for cx in range(3):contact.paste(output,(cx*32,cy*32))
    p=batch/'evidence'/asset/'application_contact.png';contact.resize((384,384),Image.Resampling.NEAREST).save(p);evidence['application_contact']=rel(p)
    registration=dict(status='REGISTERED_GRID_CANDIDATE_HUMAN_PENDING',source_sha256=sha(src),source_size=list(image.size),
        automatic_inference=dict(pitch=auto[0],phase=list(auto[1:3]),confidence=auto[3]),
        registered_inference=dict(pitch=pitch,phase=[x,y],confidence=confidence,confidence_metric='registered single-pitch edge support, not generic winner/runner-up ratio'),
        pitch_basis='Measured coarse source blocks and one-cell frame, enlarged native32 reference',decoded_size=list(decoded.size),window=list(window),
        normalization='lossless ground window',resizing=False,new_colours=False,all_output_colours_observed_in_source=True,
        original_generic_ambiguity_retained=True,human_pixel_registration='PENDING',promotion_limit='REVIEW_READY; no formal admission without real human registration/application review')
    dump(batch/'diagnostics'/f'{asset}_registration.json',registration)
    manifest=dict(schema='occ-art-manifest-v1',contract_version=1,asset_id=asset,role='floor_tile_32',status='QA_PENDING',
        provenance=dict(source_channel='codex_builtin_imagegen',source_descriptor='Independent built-in image edit; enlarged current tile is lattice reference only',source_path=rel(src),source_sha256=sha(src)),
        delivery=dict(output_path=rel(out),output_sha256=sha(out),native_ppu=32,logical_cells=[1,1],palette_max=5,required_color_families=[]),
        display=dict(canonical_integer_scale=6,canonical_at=[1920,1080],screen_px_per_native_px_at_canonical=6,integer_only=True,fractional_scale_forbidden=True,uniform_pixel_size_with_siblings=True,note='Native source audit zoom; migrated HDR scene output follows A.2.4'),
        application=dict(runtime_draw_rect='one 1m square per existing logical cell; peripheral crop keeps 1/32m texel pitch',default_integer_scale=6,minimum_integer_scale=1),evidence=evidence,
        human_review=dict(overall='PENDING',reviewer='',date='',silhouette='PENDING',material='PENDING',perspective='PENDING',style='PENDING',application='PENDING',illumination='PENDING',notes='Candidate only. Registered-grid support is distinct from ambiguous generic confidence. Contacts are technical repeat previews, not runtime acceptance.'),
        material_data=dict(normal=rel(batch/'material_data/normal.png'),roughness=rel(batch/'material_data/roughness.png'),recipe=rel(batch/'material_data/recipe.json'),source='explicit shallow slab geometry; no diffuse inference'),
        registration=rel(batch/'diagnostics'/f'{asset}_registration.json'))
    dump(batch/'delivery'/f'{asset}.occ-art-manifest-v1.json',manifest)
    errors,metrics=validate_manifest(manifest,contract,root)
    dump(batch/'diagnostics'/f'{asset}_asset_check.json',dict(errors=errors,metrics=metrics,human_review='PENDING'))
    if errors:raise ValueError(errors)
    outputs.append(dict(id=asset,source=rel(src),output=rel(out),registration_confidence=round(confidence,4),generic_confidence=round(auto[3],4),phase=[x,y],window=list(window),dominant=round(palette.most_common(1)[0][1]/1024,3)))
dump(batch/'candidate_set.json',dict(status='QA_PENDING',assets=outputs,normal=rel(batch/'material_data/normal.png'),roughness=rel(batch/'material_data/roughness.png'),human_review='PENDING'))
print(json.dumps(outputs))
