import hashlib
import json
from pathlib import Path

folder = Path(__file__).resolve().parent
archive = folder.parent
root = folder.parents[3]
report_path = folder / 'quality_pending.json'
report = json.loads(report_path.read_text(encoding='utf-8-sig'))
def bound(path):
    return {'path': path.relative_to(root).as_posix(), 'sha256': hashlib.sha256(path.read_bytes()).hexdigest()}
changed = [root / 'UnityProject/Assets/Game/Runtime/Presentation' / name for name in (
    'Hd2DDeskArtCandidate.cs', 'Hd2DNativeMaterialMesh.cs')]
report['changed_files'] = [path.relative_to(root).as_posix() for path in changed]
paths = [root / item['path'] for item in report['inputs']] + changed
paths += [root / 'UnityProject/Assets/Game/Resources/Art/MainMenuE/Stage/desk.png']
paths += list(archive.glob('wood_*.json'))
paths += list((root / 'UnityProject/ReviewEvidence/20261010_hd2d_desk').glob('wood_material_*.png'))
report['inputs'] = [bound(path) for path in sorted(set(paths))]
report['status'] = 'QA_PENDING'
report['configuration']['material_profile'] = {
    'shader': 'Universal Render Pipeline/Lit', 'workflow': 'specular',
    'metallic': 0, 'dielectric_specular_rgb': [0.04, 0.04, 0.04],
    'desktop_smoothness': 0.32, 'edge_leg_smoothness': 0.24,
    'source': 'Art/MainMenuE/Stage/desk', 'source_patch_bottom_left': [48, 32, 176, 20],
    'world_ppu': 32, 'filter': 'Point', 'normal_roughness_maps': 'none; no generated or repainted bitmap',
    'lighting': 'existing scene lights unchanged',
}
report['local_validation'] = {
    'compile': 'wood_material_compile_status.json operation.status ready; compilation_errors empty',
    'normal_entry': 'wood_material_entry_status.json PASS:entry, backpack real UI hit',
    'captures': 'main menu actual 1920x1080 and 960x540; widened equipment table 1920x1080',
    'reflection': 'main-menu same camera with wood specular on/off; restored after capture; runtime values read back',
    'density': 'mesh world edge / source UV texel span at 32 PPU for tabletop, edge and all legs, including widened 7.8-unit table; source macro-grid and complete scene approval remain separate',
}
report['blockers'] = [
    '整场景人物及装备等资产像素尺度仍未统一，保留最高优先级阻断项。',
    '完整密度清单、运动证据、人工样板认可和性能验收未完成；本次只核对局部木桌材质。',
    '旧书桌截图与之前 quality report 不再代表当前木桌 UV/材质配置，不继承其批准。',
]
report_path.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print('Current wood material evidence bound; QA_PENDING preserved.')
