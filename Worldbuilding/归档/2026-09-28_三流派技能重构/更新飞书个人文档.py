"""Patch only the two notes and seven affected table cells in the personal Doc."""
import json
import subprocess

cli = ['C:/nvm4w/nodejs/node.exe', 'C:/nvm4w/nodejs/node_modules/@larksuite/cli/scripts/run.js']
doc = 'https://jcnlgassfnsm.feishu.cn/docx/HwkDduboro2q6vxPyvGcSLRQnvf'
updates = [
    ('己方摧毁物件的原格 需要一个专有名词代替', '破口：本回合物件被摧毁时占据的原格；己方／自身破口按摧毁来源区分。地块：一个战场格；对地块造成伤害时，分别结算该格内可受击的敌我单位与可破坏物。'),
    ('本回合实际突进路径 需要一个专有名词代替', '突进轨迹：本回合已实际完成的突进路径。'),
    ('选择一个本回合被摧毁物件的地块，将相邻敌我单位各推开1格。', '选择一个破口，将相邻敌我单位各推开1格。'),
    ('在本回合己方摧毁物件的原格或相邻空格生成8耐久的轻掩体。', '在己方破口或其相邻空格生成8耐久的轻掩体。'),
    ('沿经过本回合己方摧毁物件原格的路径移动最多3格。对路径旁敌人各造成6点武器伤害。', '沿经过己方破口的路径移动最多3格。对路径旁敌人各造成6点武器伤害。'),
    ('移至本回合亲手摧毁物件留下的空格。', '移至自身破口。'),
    ('对相邻敌人造成20点伤害；本回合有突进路径时，改为对路径旁敌人各造成20点伤害。', '对相邻敌人造成20点伤害；本回合有突进轨迹时，改为对轨迹旁敌人各造成20点伤害。'),
    ('沿本回合实际突进路径退回最多2格。', '沿本回合突进轨迹退回最多2格。'),
    ('对本回合实际突进路径旁的敌人各造成12点火焰伤害。', '对本回合突进轨迹旁的敌人各造成12点火焰伤害。'),
]
for old, new in updates[1:]:  # First note was verified at revision 73 after a response-schema mismatch.
    result = subprocess.run(cli + ['docs', '+update', '--doc', doc, '--command', 'str_replace', '--pattern', old,
                                   '--content', new, '--as', 'user'], capture_output=True, text=True, encoding='utf-8', check=True)
    payload = json.loads(result.stdout)
    assert payload['ok'] and payload['data']['result'] == 'success', (old, payload)
    print(payload['data']['document']['revision_id'], old[:25])
