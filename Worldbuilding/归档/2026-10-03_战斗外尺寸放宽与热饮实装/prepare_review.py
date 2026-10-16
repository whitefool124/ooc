from pathlib import Path
import hashlib,json
from PIL import Image

folder=Path(__file__).parent
root=folder.parents[2]
for rel,name,replacements in [
 ('C:/Users/FNHF/.codex/skills/occ-art-direction/SKILL.md','art-direction-skill.before.md', [('fixed `32×32` equipment, relic, spell and non-battlefield assets;', 'fixed `32×32` equipment, relic and spell icons; non-battlefield display objects on a complete-subject-plus-safe-border adaptive canvas (user confirmed 2026-10-03), judged by consistent style and natural UI scale;')]),
 ('C:/Users/FNHF/.codex/skills/pixel-asset-pipeline/SKILL.md','asset-pipeline-skill.before.md',[('fixed 32px equipment and non-battlefield assets;', 'fixed 32px equipment icons; non-battlefield display objects use complete-subject-plus-safe-border adaptive canvases without a fixed size or 32px-multiple requirement (user confirmed 2026-10-03);')]),
 ('C:/Users/FNHF/.codex/skills/occ-art-direction/references/occ-pixel-contract.md','pixel-contract.before.md',[('| Equipment, relic, spell and non-battlefield asset |','| Equipment, relic and spell icons |')])]:
    p=Path(rel);s=p.read_text(encoding='utf-8-sig');(folder/name).write_text(s,encoding='utf-8')
    for old,new in replacements:s=s.replace(old,new)
    if 'occ-pixel-contract' in rel:s+='\nNon-battlefield display objects (user confirmed 2026-10-03) use a canvas that fits the complete subject and safe border, without a fixed32px size or 32px-multiple rule. Judge consistent art style, natural UI scale and actual support contact. Battlefield rules and functional icon contracts remain unchanged.\n'
    p.write_text(s,encoding='utf-8')
im=Image.open(folder/'hot-drink.png').convert('RGBA')
im.save(folder/'one-x.png');im.resize((148,148),Image.Resampling.NEAREST).save(folder/'four-x.png')
im.convert('LA').convert('RGBA').resize((148,148),Image.Resampling.NEAREST).save(folder/'grayscale.png')
m=json.loads((root/'Tools/OCCArt/occ_art_manifest_template_v1.json').read_text(encoding='utf-8-sig'))
rel=lambda p:p.relative_to(root).as_posix()
digest=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
source=root/'Worldbuilding/归档/2026-10-03_医务室热饮样板/source-02.png'
m.update(asset_id='medical_hot_drink',role='ui_detail_adaptive',status='QA_PENDING')
m['provenance'].update(source_path=rel(source),source_sha256=digest(source))
m['delivery'].update(output_path=rel(folder/'hot-drink.png'),output_sha256=digest(folder/'hot-drink.png'),native_ppu=32,palette_max=10)
m['application'].update(runtime_draw_rect='Medical MEAL-AETHER button local (40,+8), 74x74; rim/steam share 2x native grid; cup and saucer fixed.',default_integer_scale=2,minimum_integer_scale=1)
for key,name in [('one_x','one-x.png'),('four_x','four-x.png'),('grayscale','grayscale.png'),('checker','checker.png'),('application_contact','steam-00.png')]:m['evidence'][key]=rel(folder/name)
m['human_review']['notes']='User loosened nonbattlefield canvas restriction. Specific cup aesthetic approval remains pending; decoded asset supplied only to Editor review harness.'
(folder/'hot-drink.manifest.json').write_text(json.dumps(m,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
times=[float(v) for v in (folder/'frame-times.txt').read_text().splitlines()]
frames=[Image.open(folder/f'steam-{i:02}.png').convert('RGB') for i in range(len(times))]
durations=[max(20,round(((times[i+1]-t)*1000 if i+1<len(times) else 80)/10)*10) for i,t in enumerate(times)]
for name,crop in [('detail-preview.gif',(1028,566,1426,676)),('full-preview.gif',None)]:
    imgs=[(f.crop(crop).resize((796,220),Image.Resampling.NEAREST) if crop else f).quantize(colors=256,dither=Image.Dither.NONE) for f in frames]
    imgs[0].save(folder/name,save_all=True,append_images=imgs[1:],duration=durations,loop=0,disposal=2)
