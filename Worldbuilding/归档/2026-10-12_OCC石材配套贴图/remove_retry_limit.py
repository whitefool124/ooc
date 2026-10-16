import json,shutil
from pathlib import Path

folder=Path(__file__).resolve().parent;root=folder.parents[2]
changes=[]
def replace(path,old,new):
    path=Path(path);text=path.read_text(encoding='utf-8-sig')
    if old not in text:raise RuntimeError('Missing exact retry rule: '+str(path))
    label=path.name if path.is_relative_to(root) else 'personal_'+path.parent.name+'_'+path.name
    shutil.copy2(path,folder/'before'/label)
    path.write_text(text.replace(old,new),encoding='utf-8');changes.append(str(path))
replace(root/'AGENTS.md','最多三轮并只修正已识别维度','不设固定重试轮数上限（用户确认，2026-10-12），每轮仍只修正已识别维度')
replace(root/'Worldbuilding/策划案/OCC_项目总策划案_v1.0.md','单项最多进行三轮独立原料生成','美术原料生成与修复重试不设固定轮数上限（用户确认，2026-10-12）')
replace(Path('C:/Users/FNHF/.codex/skills/occ-art-direction/SKILL.md'),'stop after three independent rounds.','OCC art generation and repair retries have no fixed round cap (user confirmed 2026-10-12); retain independent sources, diagnosed changes and the unchanged quality threshold.')
replace(Path('C:/Users/FNHF/.codex/skills/occ-art-direction/references/occ-pixel-contract.md'),'Do not lower the threshold; stop after three rounds.','Do not lower the threshold. OCC art generation and repair retries have no fixed round cap (user confirmed 2026-10-12).')
replace(root/'.codex/skills/occ-pixel-vfx/references/runtime-lessons.md','最多三轮独立生图','独立生图重试不设固定轮数上限（用户确认，2026-10-12）')
for rel,key in [('Tools/OCCArt/native32_generation_profiles.json','maximum_rounds'),('Tools/OCCArt/occ_art_contract_v1.json','maximum_independent_generation_rounds'),('Tools/OCCArt/occ_art_production_brief_template_v1.json','maximum_rounds')]:
    path=root/rel;data=json.loads(path.read_text(encoding='utf-8-sig'));shutil.copy2(path,folder/'before'/path.name)
    def visit(v):
        if isinstance(v,dict):
            for k in list(v):
                if k==key:v[k]=None;v['retry_round_limit_policy']='No fixed cap; user confirmed2026-10-12; quality thresholds unchanged'
                else:visit(v[k])
        elif isinstance(v,list):
            for item in v:visit(item)
    visit(data);path.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');changes.append(str(path))
csv=root/'Worldbuilding/数据表/OCC_美术与界面规格表_v1.0.csv';shutil.copy2(csv,folder/'before'/csv.name)
with csv.open('a',encoding='utf-8') as out:out.write('ART-GENERATION-RETRY-20261012,美术生产,原料生成与修复重试,不设固定重试轮数上限,每轮独立内置生图且只修正已识别失败维度,保留1.2网格门槛与同场景像素密度及来源和实际应用审核；取消上限不等于资产批准\n')
changes.append(str(csv))
(folder/'retry_rule_changes.json').write_text(json.dumps(changes,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('Retry cap removed from',len(changes),'active files')
