"""Decode independent menu sources, preserving rejected trials and review provenance.

No generated source is copied to Unity. Only decoded, machine-passed review sprites
are staged; promotion remains separate from application and human review.
"""
from pathlib import Path
import concurrent.futures
import hashlib
import json
import subprocess
import sys
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
BATCH = ROOT / 'ArtSource/MainMenuE_20261008'
GENERATED = Path('C:/Users/FNHF/.codex/generated_images/01a1194c-ffa1-74a3-83af-fe2ec28eca2e')
SOURCES = {
    '03_open_book_r3': 'exec-8105d923-2bda-42b1-8b71-21a9c61b8684.png',
    '07_vine_r2': 'exec-63dbf221-ff0c-40ab-88ba-778aaf8e0bac.png',
    '08_pen_r2': 'exec-cc972baf-afea-47d5-bf76-426338cb121f.png',
    '02_closed_book': 'exec-6bfccaf3-4d54-42a7-bba3-7628a979e4dd.png',
    '03_open_book': 'exec-07cc9828-8782-4cb7-8d5f-5ca95a5b2f6f.png',
    '04_cup': 'exec-03ad618f-c404-49d1-b132-87a7755467d6.png',
    '05_aether_lamp': 'exec-8cd40657-19e3-45ee-baa3-b443c00109b1.png',
    '06_banner': 'exec-406de607-5ca6-42e5-8955-1d0d92c5e233.png',
    '07_vine': 'exec-4e0eef62-c58e-4bd5-9211-0f53c5e31e13.png',
    '08_pen': 'exec-f36ba31e-3c84-4866-a123-9c09fb20c820.png',
    '03_open_book_r2': 'exec-25e5c95b-8344-46b1-a643-2e66691aa5ac.png',
    '04_cup_r2': 'exec-d75f8272-c821-4d8f-9b53-af795edd3130.png',
}

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def relative(path):
    return path.relative_to(ROOT).as_posix()

def decode(stem, filename):
    folder = BATCH / stem
    folder.mkdir(parents=True, exist_ok=True)
    source = folder / 'source.png'
    if not source.exists():
        source.write_bytes((GENERATED / filename).read_bytes())
    qa = folder / 'grid.json'
    output = folder / 'native.png'
    if not qa.exists():
        subprocess.run([sys.executable, str(ROOT/'Tools/OCCArt/decode_generated_pixel_grid.py'),
                        '--source', str(source), '--decoded', str(folder/'decoded.png'),
                        '--output-32', str(output), '--qa', str(qa), '--palette-max', '16'],
                       check=True, stdout=subprocess.PIPE)
    report = json.loads(qa.read_text(encoding='utf-8'))
    passed = report['status'] == 'PASS_LOSSLESS_TARGET'
    manifest = json.loads((ROOT/'Tools/OCCArt/occ_art_manifest_template_v1.json').read_text(encoding='utf-8'))
    manifest.update(asset_id='ui.main_menu_e.'+stem, role='ui_detail_adaptive', status='REVIEW_READY' if passed else 'REJECTED')
    manifest['provenance'].update(source_path=relative(source), source_sha256=sha(source),
                                  source_descriptor='Independent Codex built-in image generation, approved E scene direction; trial '+stem)
    manifest['delivery'].update(output_path=relative(output), output_sha256=sha(output) if passed else 'REJECTED',
                                native_ppu=32, palette_max=16, logical_cells=None)
    manifest['display']['canonical_integer_scale'] = 4
    manifest['display']['screen_px_per_native_px_at_canonical'] = 4
    manifest['application'] = {'scope':'nonbattlefield_display_only', 'placement':'tabletop' if 'book' in stem or 'cup' in stem or 'pen' in stem or 'lamp' in stem else 'wall_mounted',
                               'support':'window-side desktop' if 'book' in stem or 'cup' in stem or 'pen' in stem or 'lamp' in stem else 'window lintel',
                               'logical_cells':None, 'default_integer_scale':4, 'minimum_integer_scale':2}
    for key in ('one_x','four_x','grayscale','checker'):
        manifest['evidence'][key] = relative(folder/(key+'.png'))
    manifest['evidence']['application_contact'] = 'Worldbuilding/归档/2026-10-08_主界面E动态场景/application_1920.png'
    manifest['human_review']['notes'] = 'User approved concept E. Each new asset and application remain review candidates; no FORMAL claim.'
    if passed:
        native = Image.open(output).convert('RGBA')
        bounds = native.getchannel('A').getbbox()
        # Adaptive UI props may crop transparent padding, never opaque subject pixels.
        native = native.crop((max(0,bounds[0]-1),max(0,bounds[1]-1),min(native.width,bounds[2]+1),min(native.height,bounds[3]+1)))
        native.save(output)
        manifest['delivery']['output_sha256'] = sha(output)
        native.save(folder/'one_x.png')
        native.resize((native.width*4,native.height*4),Image.Resampling.NEAREST).save(folder/'four_x.png')
        native.convert('LA').save(folder/'grayscale.png')
        checker = Image.new('RGBA',native.size,(220,220,220,255))
        for y in range(native.height):
            for x in range(native.width):
                if (x//4+y//4)%2:
                    checker.putpixel((x,y),(245,245,245,255))
        checker.alpha_composite(native)
        checker.save(folder/'checker.png')
    (folder/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    return {'asset':stem,'status':report['status'],'confidence':report['subject_lattice_confidence'], 'size':report['subject_logical_size']}

if __name__ == '__main__':
    BATCH.mkdir(parents=True,exist_ok=True)
    with concurrent.futures.ThreadPoolExecutor(max_workers=3) as pool:
        for result in pool.map(lambda item:decode(*item),SOURCES.items()):
            print(json.dumps(result,ensure_ascii=False),flush=True)
