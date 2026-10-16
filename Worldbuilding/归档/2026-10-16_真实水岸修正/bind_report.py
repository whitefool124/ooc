import hashlib
import json
from pathlib import Path

root = Path(__file__).resolve().parents[3]
directory = Path(__file__).resolve().parent
path = directory / 'quality_report.json'
report = json.loads(path.read_text(encoding='utf-8'))
changed = [
    'UnityProject/Assets/Game/Runtime/Presentation/Hd2DCourtyardWaterMaterial.cs',
    'UnityProject/Assets/Game/Runtime/Presentation/Hd2DBattlefieldSample.cs',
    'UnityProject/Assets/Game/Resources/Shaders/Hd2DCourtyardWater.shader',
    'UnityProject/Assets/Game/Tests/EditMode/Hd2DWaterShorelineTests.cs',
    'UnityProject/Assets/Game/Tests/EditMode/Hd2DWaterShorelineTests.cs.meta',
]
report['changed_files'] = changed
bound = {item['path']: item for item in report['inputs']}
for relative in changed + [str((directory / name).relative_to(root)).replace('\\', '/')
                           for name in ['tests.xml', 'editor.log', '完成记录.md']]:
    bound[relative] = {'path': relative, 'sha256': hashlib.sha256((root / relative).read_bytes()).hexdigest()}
report['inputs'] = list(bound.values())
report['local_checks'] = {'editmode': '5/5 PASS', 'shader_forward_pass': 'PASS',
                          'normal_entry': 'NOT_RUN', 'visual_review': 'PENDING'}
path.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
