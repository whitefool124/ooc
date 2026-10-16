import difflib
import hashlib
import json
from pathlib import Path

folder=Path(__file__).resolve().parent
archive=folder.parent
root=folder.parents[3]
report_path=folder/'quality_pending.json'
report=json.loads(report_path.read_text(encoding='utf-8'))
def result(name):
    envelope=json.loads((archive/(name+'.json')).read_text(encoding='utf-8'))
    return json.loads(envelope['result']['content'][0]['text'])
def bound(path):
    return {'path':path.relative_to(root).as_posix(),'sha256':hashlib.sha256(path.read_bytes()).hexdigest()}

assert result('text_a6_ready')['data']['operation']['status']=='ready'
assert result('text_a6_entry_status')['data']['result']=='PASS:entry'
for name in ['bag_open','spell_open','select_spell','equipment_return','return_map']:
    assert result('text_a6_'+name)['data']['hit'],name
for resolution,expected in [('1920',{24,36}),('960',{12,24})]:
    texts=result('text_a6_audit'+resolution)['data']['returnValue']
    visible=[t for t in texts if t['name'].endswith('清晰文字')]
    assert visible
    for t in visible:
        assert any(abs(t['projectedFont']-size)<.001 for size in expected),t
        assert not t['resizeTextForBestFit'],t
        assert t['preferred'][0]<=t['size'][0]+1,t
        assert t['preferred'][1]<=t['size'][1]+1,t
    if resolution=='1920':
        assert any(t['name']=='术式名称 清晰文字' and t['text']=='火花' for t in visible)
statuses=result('text_a6_status_audit')['data']['returnValue']
assert any(t['name']=='背包交互状态 清晰文字' and t['text']=='背包空间不足，请先整理或取消放置' for t in statuses)
assert result('text_a6_lifetime')['data']['returnValue']['roots']==[]
preflight=json.loads(result('text_final_preflight')['data']['result'])
assert not preflight['play'] and not preflight['compiling'] and all(not s['isDirty'] for s in preflight['scenes'])

files=[root/'UnityProject/Assets/Game/Runtime/Presentation'/name for name in ['Hd2DLoadoutScene.cs','Hd2DLoadoutScene.Text.cs','Hd2DLoadoutScene.Text.cs.meta']]
captures=[root/'UnityProject/ReviewEvidence/20261010_hd2d_desk'/f'text_{page}_a6_{res}.png' for page in ['bag','spell'] for res in ['1920','960']]
paths=[root/item['path'] for item in report['inputs']]+files+captures+list(archive.glob('text_*.json'))
paths+=list((root/'UnityProject/Assets/Game/Resources/Fonts').glob('FusionPixel12ProportionalZhHans.*'))
report['inputs']=[bound(p) for p in sorted(set(paths)) if p.is_file()]
report['changed_files']=[p.relative_to(root).as_posix() for p in files]
report['current_screenshots']=[bound(p) for p in captures]
report['local_implementation']={
    'scope':'Preparation desk text only; no change to lighting, model/card geometry, sprite density or game rules',
    'reading':'Existing FusionPixel12, integer glyph sizes, projected positions rounded to output pixels, Canvas scale compensated. Body 24px at1080/12px at540; principal headings36px at1080/24px at540. No best-fit shrinking. Source binding remains alive; display labels never intercept input.',
    'layout':'Slot text insets, headings moved clear of cards, range/visibility on separate target line, duplicate drag hint suppressed while current operation feedback remains visible, floating resource text has restrained shadow.'}
report['runtime'].update(normal_entry=True,interaction_pass=True,resource_lifetime_pass=True,console_clear=False,
    record=bound(archive/'text_a6_entry_status.json'))
report['validation_scope']={
    'normal_entry':'PASS:entry; actual backpack/spell/second spell/backpack/map clicks',
    'text':'Displayed spell labels have correct projected font size, no best-fit shrinking, and fit their rectangles at both actual resolutions. Second spell updates to 火花.',
    'feedback':'Temporary text-binding probe displayed changed inventory status. Not an actual backpack-full or drag test.',
    'lifetime':'No reading canvases, including inactive ones, after returning to map.',
    'console':'Compilation ready. Cached QuickInstaller ExecutionEngineException still present; no claim of globally clean Console.',
    'cleanup':'Original Game View index3 restored, verified temporary960 removed; clean original EpicToonVfxShowcase, EditMode; no scene save.'}
report['blockers']=['Complete HD2D sample/human approval and standalone Player performance remain pending.',
    'Populated/full backpack, dragging, longest spell/passive descriptions and uncommon resolutions were not tested in this text-only iteration.',
    'Unity QuickInstaller Editor assembly CodeBase exception remains unresolved.']
for sample in report['performance']['measurements']:sample.update(target_fps=60,p95_budget_ms=16.7)
report_path.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
before=folder/'Hd2DLoadoutScene.before.cs'
current=files[0]
diff=''.join(difflib.unified_diff(before.read_text(encoding='utf-8-sig').splitlines(True),current.read_text(encoding='utf-8-sig').splitlines(True),fromfile=before.name,tofile=current.name))
diff+=''.join(difflib.unified_diff([],files[1].read_text(encoding='utf-8-sig').splitlines(True),fromfile='/dev/null',tofile=files[1].name))
(folder/'source_changes.diff').write_text(diff,encoding='utf-8')
print('Text sizes, text fit, live binding, UI clicks and cleanup verified. Overall HD2D QA_PENDING.')
