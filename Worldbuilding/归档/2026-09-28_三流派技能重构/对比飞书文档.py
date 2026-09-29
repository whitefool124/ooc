"""Read the live personal Doc and compare its 101 displayed skill rows to local CSV."""
import csv
import json
import subprocess
from pathlib import Path

root = Path(__file__).resolve().parents[3]
folder = Path(__file__).parent
cmd = ['C:/nvm4w/nodejs/node.exe', 'C:/nvm4w/nodejs/node_modules/@larksuite/cli/scripts/run.js',
       'docs', '+fetch', '--doc', 'https://jcnlgassfnsm.feishu.cn/docx/HwkDduboro2q6vxPyvGcSLRQnvf',
       '--scope', 'full', '--doc-format', 'markdown', '--as', 'user']
response = json.loads(subprocess.run(cmd, capture_output=True, text=True, encoding='utf-8', check=True).stdout)
assert response['ok']
doc = response['data']['document']
markdown = doc['content']
(folder / '飞书文档_用户修改快照.md').write_text(markdown, encoding='utf-8')
with (root / 'Worldbuilding/数据表/OCC_技能配置表_v1.0.csv').open(encoding='utf-8-sig', newline='') as f:
    local = {row['技能ID']: row for row in csv.DictReader(f)}
remote = {}
for line in markdown.splitlines():
    if not line.startswith('| '):
        continue
    parts = [p.strip() for p in line.strip('|').split('|')]
    if len(parts) != 6 or parts[0] not in local:
        continue
    assert parts[0] not in remote
    remote[parts[0]] = parts
assert len(remote) == 101, len(remote)
differences = []
for skill_id, parts in remote.items():
    row = local[skill_id]
    for i, field in enumerate(('技能ID', '名称', 'AP', '魔力', '冷却', '卡面文案')):
        if parts[i] != row[field]:
            differences.append({'id': skill_id, 'field': field, 'local': row[field], 'feishu': parts[i]})
print(json.dumps({'revision': doc['revision_id'], 'remote_rows': len(remote), 'changes': differences}, ensure_ascii=False, indent=2))
