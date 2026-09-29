"""Prepare the two affected specialization material cells."""
import csv
import json
import re
import subprocess
from pathlib import Path

root = Path(__file__).resolve().parents[3]
source = root / 'Worldbuilding/数据表/OCC_强化材料配置表_v1.0.csv'
with source.open(encoding='utf-8-sig', newline='') as f:
    rows = list(csv.DictReader(f))
assert [row['材料ID'] for row in rows] == ['FORGE-CIRCUIT', 'FORGE-LOAD', 'SPEC-AMPLIFY', 'SPEC-EFFICIENT']
cells = [[{'value': rows[2]['适用与效果']}], [{'value': rows[3]['适用与效果']}]]
out = Path(__file__).with_name('飞书专精材料写入.json')
out.write_text(json.dumps(cells, ensure_ascii=False), encoding='utf-8')
print(json.dumps({'range': 'D4:D5', 'rows': 2, 'output': str(out)}, ensure_ascii=False))

cmd = ['C:/nvm4w/nodejs/node.exe', 'C:/nvm4w/nodejs/node_modules/@larksuite/cli/scripts/run.js',
       'sheets', '+csv-get', '--url', 'https://jcnlgassfnsm.feishu.cn/sheets/APWssLgD3h4RL7tqxhgcIOa4nTb',
       '--sheet-name', '04_强化材料配置', '--range', 'D4:D5', '--as', 'user', '--max-chars', '20000']
response = json.loads(subprocess.run(cmd, capture_output=True, text=True, encoding='utf-8', check=True).stdout)
assert response['ok']
data = response['data']
assert data['actual_range'] == 'D4:D5' and not data['has_more']
actual = [re.sub(r'^\[row=\d+\] ', '', line) for line in data['annotated_csv'].splitlines()]
assert actual == [rows[2]['适用与效果'], rows[3]['适用与效果']]
print(json.dumps({'verified_cells': 2, 'revision': data['revision']}, ensure_ascii=False))
