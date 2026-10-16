import copy
import importlib.util
import json
import shutil
from pathlib import Path

root = Path(__file__).resolve().parents[2]
folder = Path(__file__).resolve().parent
web = root / 'ArtSource/MainMenuE_20261008'
for pattern in ('hd2d-*.png', 'hd2d-*.mp4'):
    for item in folder.glob(pattern):
        shutil.copy2(item, web / item.name)

spec = importlib.util.spec_from_file_location('validator', root/'Tools/OCCArt/validate_occ_art_asset.py')
validator = importlib.util.module_from_spec(spec)
spec.loader.exec_module(validator)
contract = validator.read_json(root/'Tools/OCCArt/occ_art_contract_v1.json')
results = []
for name in ('label', 'left', 'right'):
    manifest = validator.read_json(root/f'ArtSource/BookFinal_20261008/{name}/manifest.json')
    manifest['asset_id'] += '.hd2d_application'
    manifest['application'] = {
        'rendering_mode': 'main_menu_hd2d', 'camera_projection': 'perspective',
        'runtime_draw_rect': 'dynamic projected book surfaces at native 32 PPU',
        'default_integer_scale': None, 'minimum_integer_scale': None,
    }
    manifest['display'] = {
        'output_resolution': [1920,1080], 'integer_only': False,
        'source_texture_filter': 'Point', 'screen_pixel_density': 'perspective_dependent',
        'note': 'Original native texels preserved; final 3D perspective image has no uniform screen lattice.',
    }
    manifest['evidence']['application_contact'] = 'ArtSource/MainMenuHD2D_20261008/hd2d-open-final.png'
    manifest['unity_import'].pop('runtime_lattice', None)
    manifest['unity_import']['hd2d_runtime'] = {
        'pass': True, 'native_source_preserved': True, 'contact': True,
        'occlusion': True, 'lighting': True, 'clarity': True,
        'capture': 'ArtSource/MainMenuHD2D_20261008/hd2d-open-final.png',
        'motion': 'ArtSource/MainMenuHD2D_20261008/hd2d-book-loop.mp4',
        'scope': 'Technical application inspection only; human aesthetic approval remains pending.',
    }
    manifest['human_review']['notes'] = 'HD2D application is pending user aesthetic acceptance; source art and GUID unchanged.'
    path = folder/f'{name}-manifest.json'
    path.write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    errors, report = validator.validate_manifest(manifest, contract, root)
    assert not errors, errors
    (folder/f'{name}-validation.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    results.append(f'{name}: PASS / REVIEW_READY')

# Verify the scoped exception cannot bypass the battlefield's lattice gate.
base = copy.deepcopy(manifest)
base['status'] = 'FORMAL'
errors,_ = validator.validate_manifest(base,contract,root)
assert not any('runtime lattice' in e for e in errors), errors
assert any('human' in e for e in errors), 'Human approval gate was bypassed'
base['unity_import']['hd2d_runtime']['lighting'] = False
errors,_ = validator.validate_manifest(base,contract,root)
assert 'FORMAL HD2D runtime.lighting must pass' in errors
base['role'] = next(k for k,v in contract['roles'].items() if v.get('application_scope') != 'E_main_menu_only' and 'native_size' not in v and not v.get('low_resolution_companion'))
errors,_ = validator.validate_manifest(base,contract,root)
assert any('runtime_lattice' in e for e in errors), errors
assert any('default_integer_scale' in e for e in errors), errors
results.append('Scoped HD2D / lighting failure / human approval / non-main-menu lattice gates: PASS')
(folder/'validation-summary.txt').write_text('\n'.join(results)+'\n',encoding='utf-8')
print('\n'.join(results))
