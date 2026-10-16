import hashlib
import json
from pathlib import Path

folder = Path(__file__).parent
attempts = []
for i in range(1, 4):
    source = folder / f'source-{i:02}.png'
    report = json.loads((folder / f'decoder-{i:02}.json').read_text(encoding='utf-8'))
    attempts.append({'source': source.name, 'sha256': hashlib.sha256(source.read_bytes()).hexdigest(),
                     'decoder_report': f'decoder-{i:02}.json', 'status': report['status'],
                     'subject_lattice_confidence': report['subject_lattice_confidence'],
                     'subject_logical_size': report['subject_logical_size']})
(folder / 'source-provenance.json').write_text(json.dumps({
    'source_channel': 'codex_builtin_imagegen', 'independent_source_per_attempt': True,
    'status': 'REJECTED_RAW_SOURCES', 'unity_imported': False, 'attempts': attempts
}, ensure_ascii=False, indent=2), encoding='utf-8')
