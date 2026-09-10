from audit_support import *
import importlib.util
import difflib
import yaml
from collections import Counter

spec=importlib.util.spec_from_file_location('quick', ROOTS['personal']/'.system/skill-creator/scripts/quick_validate.py')
mod=importlib.util.module_from_spec(spec); spec.loader.exec_module(mod)
items=inventory(); save_json('inventory-after.json',items)
results=[]
for item in items:
    p=Path(item['path'])
    if p.name!='SKILL.md': continue
    ok,msg=mod.validate_skill(p.parent)
    results.append(dict(group=item['group'],path=item['path'],valid=ok,message=msg))
save_json('skill-validation.json',results)
links=scan_links(); save_json('links-after.json',links)
placeholders={'url','图片URL','img_key','img_xxx','./a.png','IMAGE_LINK'}
real=[x for x in links if x['target'] not in placeholders and not x['target'].startswith('@./images/')]
save_json('unresolved-real-links.json',real)
changes=json.loads(read(OUT/'changes.json'))
diff=[]; errors=[]
for x in changes:
    p=Path(x['path'])
    if sha(p.read_bytes())!=x['after_sha256']: errors.append({'path':x['path'],'error':'post-edit hash mismatch'})
    before=Path(x['backup']).read_bytes() if x['backup'] else b''
    if x['backup'] and sha(before)!=x['before_sha256']: errors.append({'path':x['path'],'error':'backup hash mismatch'})
    orig=before.decode('utf-8-sig').splitlines(keepends=True)
    current=read(p).splitlines(keepends=True)
    diff.extend(difflib.unified_diff(orig,current,fromfile=x['path']+' (before audit)',tofile=x['path']))
    for line in difflib.unified_diff(orig,current):
        if line.startswith('+') and not line.startswith('+++') and line.rstrip('\r\n').endswith((' ','\t')):
            errors.append({'path':x['path'],'error':'new trailing whitespace'})
(OUT/'review.diff').write_text(''.join(diff),encoding='utf-8')
agent_yaml=[]
for root in ROOTS.values():
    for p in root.rglob('agents/openai.yaml'):
        try:
            data=yaml.safe_load(read(p))
            if not isinstance(data,dict): raise ValueError('not a mapping')
            agent_yaml.append({'path':p.as_posix(),'valid':True})
        except Exception as e: agent_yaml.append({'path':p.as_posix(),'valid':False,'error':str(e)})
save_json('agent-yaml-validation.json',agent_yaml)
save_json('integrity-errors.json',errors)
summary=dict(inventory=dict(Counter(x['group'] for x in items)),changed_files=len(changes),
             changed_skill_entries=sum(Path(x['path']).name=='SKILL.md' for x in changes),
             validation_pass=sum(x['valid'] for x in results),validation_fail=sum(not x['valid'] for x in results),
             unresolved_real_links=real,integrity_errors=errors,agent_yaml_files=len(agent_yaml),agent_yaml_errors=[x for x in agent_yaml if not x['valid']])
save_json('validation-summary.json',summary)
print(json.dumps(summary,ensure_ascii=False))
print(json.dumps([x for x in results if not x['valid']],ensure_ascii=False))
