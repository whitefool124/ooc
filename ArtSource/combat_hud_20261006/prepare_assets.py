from pathlib import Path
import hashlib,json,subprocess
from PIL import Image,ImageOps

folder=Path(__file__).resolve().parent;root=folder.parents[1]
for asset_id in ['hero','tether_hound','pyromancer','frame']:
    base=folder/asset_id; output=base/'asset'/f'{asset_id}.png'
    if not output.exists(): continue
    reports=sorted((base/'qa').glob('r*_decode*.json'))
    report=next((p for p in reversed(reports) if json.loads(p.read_text(encoding='utf-8'))['status']=='PASS_LOSSLESS_TARGET'),None)
    if report is None:raise RuntimeError('Unaccepted asset '+asset_id)
    selected=report.stem.split('_')[0];source=base/'raw'/f'{selected}.png'
    im=Image.open(output).convert('RGBA');qa=base/'qa'
    im.save(qa/'1x.png');im.resize((im.width*4,im.height*4),Image.Resampling.NEAREST).save(qa/'4x.png')
    grey=ImageOps.grayscale(im).convert('RGBA');grey.putalpha(im.getchannel('A'));grey.save(qa/'grayscale.png')
    check=Image.new('RGBA',im.size,(212,212,212,255))
    for y in range(im.height):
      for x in range(im.width):
        if (x//4+y//4)%2:check.putpixel((x,y),(242,242,242,255))
    check.alpha_composite(im);check.save(qa/'checker.png')
    contact=Image.new('RGBA',(160,160),(233,225,205,255));scale=4 if asset_id=='frame' else 2
    contact.alpha_composite(im.resize((im.width*scale,im.height*scale),Image.Resampling.NEAREST),(16,16));contact.save(qa/'application.png')
    rel=lambda p:p.relative_to(root).as_posix();sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
    manifest={'schema':'occ-art-manifest-v1','contract_version':1,'asset_id':'combat_hud_'+asset_id,'role':'combat_hud_portrait_frame_34' if asset_id=='frame' else 'combat_hud_portrait_64','status':'QA_PENDING',
    'provenance':{'source_channel':'codex_builtin_imagegen','source_descriptor':'Independent built-in generation; selected '+selected+'; uniform inferred subject grid, no geometric resize','source_path':rel(source),'source_sha256':sha(source)},
    'delivery':{'output_path':rel(output),'native_output_path':rel(output),'output_sha256':sha(output),'native_ppu':32,'logical_cells':None,'palette_max':12 if asset_id=='frame' else 32,'required_color_families':[]},
    'display':{'canonical_integer_scale':scale,'canonical_at':[1920,1080],'screen_px_per_native_px_at_canonical':scale,'integer_only':True,'fractional_scale_forbidden':True,'uniform_pixel_size_with_siblings':True,'note':'Battle HUD identity artwork, no tactical substitution'},
    'application':{'runtime_draw_rect':'128x128 reference units','default_integer_scale':scale,'minimum_integer_scale':scale//2,'integration_authorization':'User requested formal battle HUD redesign and supporting art on 2026-10-06; reversible review integration only'},
    'evidence':{'one_x':rel(qa/'1x.png'),'four_x':rel(qa/'4x.png'),'grayscale':rel(qa/'grayscale.png'),'checker':rel(qa/'checker.png'),'application_contact':rel(qa/'application.png')},
    'human_review':{'overall':'PENDING','reviewer':'','date':'','notes':'Await user aesthetic review of concrete in-game sample; no FORMAL promotion'},'unity_import':None}
    path=base/'manifest.occ-art-manifest-v1.json';path.write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
    result=subprocess.run([str(Path(__import__('sys').executable)),str(root/'Tools/OCCArt/validate_occ_art_asset.py'),str(path),'--root',str(root),'--report',str(qa/'manifest-validation.json')],capture_output=True,text=True,encoding='utf-8',errors='replace')
    print(asset_id,result.returncode,result.stdout[:1000])
    if result.returncode:raise RuntimeError(result.stderr or result.stdout)
