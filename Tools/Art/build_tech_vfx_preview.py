import base64
import json
from pathlib import Path

root = Path(__file__).resolve().parents[2]
fragment_path = root / 'Tools/Art/tech-vfx-preview.html'
asset_paths = {
    'floor': 'UnityProject/Assets/Game/Resources/Art/FormalAcademyIndependentFloors32/academy_block_court_a.png',
    'hero': 'UnityProject/Assets/Game/Resources/Art/FormalUnits64/hero.png',
    'enemy': 'UnityProject/Assets/Game/Resources/Art/FormalUnits64/pyromancer.png',
}
assets = {key: 'data:image/png;base64,' + base64.b64encode((root / path).read_bytes()).decode()
          for key, path in asset_paths.items()}
fragment = fragment_path.read_text(encoding='utf-8')
if '__OCC_ASSETS__' not in fragment:
    raise RuntimeError('Asset placeholder missing; use the editable template for regeneration.')
output = root / 'Worldbuilding/归档/2026-10-02_实时技术特效预览'
output.mkdir(parents=True, exist_ok=True)
out_path = output / 'occ-tech-vfx.html'
out_path.write_text(fragment.replace('__OCC_ASSETS__', json.dumps(assets)), encoding='utf-8')
(output / '来源.json').write_text(json.dumps({'assets': asset_paths, 'kind': 'WebGL shader study',
    'unity_imported': False, 'new_raster_art': False}, ensure_ascii=False, indent=2), encoding='utf-8')
print(out_path)
