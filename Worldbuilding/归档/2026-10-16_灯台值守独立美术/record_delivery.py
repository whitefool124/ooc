import hashlib
import json
import re
import xml.etree.ElementTree as ET
from pathlib import Path

folder=Path(__file__).resolve().parent
root=folder.parents[2]
art=root/'ArtSource/enemy_independent_20261016/signal_keeper'
tests=ET.parse(folder/'tests.xml').getroot()
assert tests.get('failed')=='0' and tests.get('passed')=='2'
path=art/'manifest.occ-art-manifest-v1.json'
manifest=json.loads(path.read_text(encoding='utf-8'))
unity='UnityProject/Assets/Game/Resources/Art/EnemyUnitCandidates32x64/signal_keeper.png'
assert hashlib.sha256((root/unity).read_bytes()).hexdigest()==manifest['delivery']['output_sha256']
guid=re.search(r'^guid: (\w+)',(root/(unity+'.meta')).read_text(encoding='utf-8'),re.M).group(1)
manifest['unity_import']={'asset_path':unity,'stable_guid':guid,'importer_verified':True,
    'runtime_verified':False,'filter_mode':'Point','mesh_type':'Full Rect','compression':'None',
    'mipmaps':False,'wrap_mode':'Clamp','native_ppu':32,
    'review_scope':'importer and reversible presentation lookup only; actual camera unverified'}
path.write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
reportpath=folder/'quality_report.json'
report=json.loads(reportpath.read_text(encoding='utf-8'))
changed=[unity,unity+'.meta',
    'UnityProject/Assets/Game/Runtime/Presentation/CombatFormalVisualAssets.cs',
    'UnityProject/Assets/Game/Runtime/Presentation/EnemyUnitArtCandidates.cs',
    'UnityProject/Assets/Game/Editor/EnemyUnitCandidateImporter.cs',
    'UnityProject/Assets/Game/Editor/EnemyUnitCandidateReview.cs',
    'UnityProject/Assets/Game/Tests/EditMode/EnemyUnitArtCandidateTests.cs']
inputs={item['path']:item for item in report['inputs']}
for p in [root/v for v in changed]+[path,folder/'tests.xml',folder/'editor.log',folder/'完成记录.md']:
    relative=p.relative_to(root).as_posix()
    inputs[relative]={'path':relative,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()}
report['changed_files']=changed
report['inputs']=list(inputs.values())
report['local_checks']={'asset_validation':'PASS','editmode':'2/2 PASS',
    'isolated_rendered_contacts':'FAILED: removed unworking capture helper; failures archived','normal_entry':'NOT_RUN',
    'human_review':'PENDING','default_release_registry_changed':False}
reportpath.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('QA_PENDING delivery bound; no human approval asserted')
