from pathlib import Path
import json
root = Path('E:/数据库/OCC_Codex')
task = root / 'Worldbuilding/归档/2026-09-05_法宝反馈真实性'
document = json.loads((task / 'master_final.json').read_text(encoding='utf-8-sig'))['data']['document']
content = document['content']
assert '784项' in content and '水面/灯藤v3仍QA_PENDING、当前批准0/2' in content
assert '冒险封签反噬' in content
mirror = root / 'Worldbuilding/策划案/OCC_项目总策划案_v1.0.md'
mirror.write_text(content.rstrip('\n') + '\n', encoding='utf-8')
assert mirror.read_text(encoding='utf-8').rstrip('\n') == content.rstrip('\n')
print('Master mirror verified at revision', document['revision_id'])
