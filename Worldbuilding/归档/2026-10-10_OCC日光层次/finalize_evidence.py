import hashlib
import json
import shutil
import subprocess
from pathlib import Path

root = Path(__file__).resolve().parents[3]
archive = Path(__file__).resolve().parent
sha = lambda p: hashlib.sha256(p.read_bytes()).hexdigest()
report_path = archive / 'quality_candidate.json'
report = json.loads(report_path.read_text(encoding='utf-8-sig'))
report['changed_files'] = [
    'UnityProject/Assets/Game/Runtime/Presentation/Hd2DLoadoutScene.cs',
    'UnityProject/Assets/Game/Runtime/Presentation/Hd2DLoadoutScene.Lighting.cs',
]
for entry in report['inputs']:
    entry['sha256'] = sha(root / entry['path'])
evidence = archive / 'evidence'
evidence.mkdir(exist_ok=True)
report['additional_evidence'] = []
for name in ['occ_daylight_final_1080.png', 'occ_daylight_final_540.png', 'occ_daylight_spell_540.png', 'occ_daylight_lamp100_540.png', 'occ_daylight_lamp0_540.png']:
    target = evidence / name
    shutil.copy2(root / 'UnityProject/ReviewEvidence' / name, target)
    report['additional_evidence'].append({'path': target.relative_to(root).as_posix(), 'sha256': sha(target)})
report['runtime_observations'] = {}
for name in ['desk1080', 'spell540', 'close_readback']:
    receipt = json.loads((archive / (name + '.json')).read_text(encoding='utf-8-sig'))
    payload = json.loads(receipt['result']['content'][0]['text'])
    report['runtime_observations'][name] = json.loads(payload['data']['result'])
report['runtime_observations']['note'] = 'Light readback includes the disabled template ether light; do not interpret every listed light as enabled. Final Console query: no error entries. Final edit state: correct project, clean CombatPrototype scene, original UniversalRP restored, lamp preference absent, Play exited.'
report['status'] = 'QA_PENDING'
report['blockers'] = ['Full visual gate cases and pixel-density review incomplete', 'Human aesthetic approval pending', 'Player performance measurement pending']
report_path.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
result = subprocess.run(['python', '-X', 'utf8', str(root / 'Tools/OCCArt/validate_hd2d_quality.py'), str(report_path), '--scope', 'desk'], capture_output=True, text=True, encoding='utf-8')
(archive / 'quality_gate_result.json').write_text(result.stdout, encoding='utf-8')
print(json.dumps({'report': str(report_path), 'evidence_count': len(report['additional_evidence']), 'gate_exit_code': result.returncode, 'status': report['status']}, ensure_ascii=False))
