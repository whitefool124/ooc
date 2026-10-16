import hashlib
import json
import difflib
from pathlib import Path

folder = Path(__file__).resolve().parent
archive = folder.parent
root = folder.parents[3]
report_path = folder / 'quality_pending.json'
report = json.loads(report_path.read_text(encoding='utf-8-sig'))
names = ['Hd2DRewardGallery.cs', 'Hd2DRewardObject.cs', 'RogueliteSettlementPresentation.cs']
changed = [root / 'UnityProject/Assets/Game/Runtime/Presentation' / name for name in names]
def bound(path):
    return {'path': path.relative_to(root).as_posix(), 'sha256': hashlib.sha256(path.read_bytes()).hexdigest()}
def result(name):
    data = json.loads((archive / (name + '.json')).read_text(encoding='utf-8'))
    return json.loads(data['result']['content'][0]['text'])
for suffix in ['1920', '960']:
    data = json.loads(result('reward_flat_density' + suffix)['data']['result'])
    assert len(data['icons']) == 3
    assert all(abs(pitch - 1/32) < 1e-6 for icon in data['icons'] for pitch in icon['pitch'])
for name in ['reward_flat_open','reward_flat_select','reward_flat_claim','reward_flat_gold','reward_flat_contribution','reward_flat_leave']:
    assert result(name)['data']['hit'] is True, name
map_buttons = json.loads(result('reward_flat_map')['data']['result'])['buttons']
assert any(b['name'] == 'map.node.B2' for b in map_buttons)
assert not any(b['name'] == '离开战利品' for b in map_buttons)
assert result('reward_flat_reload')['data']['status'] == 'Success'
assert result('reward_final_preflight')['success']
paths = [root / item['path'] for item in report['inputs']] + changed
paths += list(archive.glob('reward_*.json'))
paths += [root / 'UnityProject/ReviewEvidence/20261010_hd2d_desk' / name for name in ['reward_flat_1920.png','reward_flat_960.png','reward_before.png']]
paths += [root / 'UnityProject/Assets/Game/Resources/Art' / name for name in ['FormalSkillIcons32/Fire/f-p48.png','FormalSkillIcons32/Fire/f-p37.png','FormalSkillIcons32/Fire/f-p46.png','Hd2DContentFrames/简洁纸边.asset','Hd2DContentFrames/简洁纸边.png','MainMenuE/Stage/desk.png']]
report['inputs'] = [bound(p) for p in sorted(set(paths)) if p.is_file()]
report['changed_files'] = [p.relative_to(root).as_posix() for p in changed]
report['configuration'].update(hdr=True, msaa=False, field_of_view=24)
report['local_implementation'] = {'scope':'Existing reward reveal page; shared systems and reward/save rules unchanged', 'world_pitch':1/32, 'changes':'Use existing textured dielectric desk; remove untextured display cubes; shared lit image geometry with native sprite dimensions and forced canvas mesh refresh; one paper rim with readable Chinese text'}
report['validation_scope'] = {'normal_entry':'PASS:entry before final text-only adjustments, reward_entry_after_completed.json; no full combat-to-reward journey claim', 'local_reward':'Disposable B1 in-memory preview; final actual 960x540 UI clicks open/select/claim/gold/contribution/leave all hit; map restored. Player save routing isolated and original slots restored on Play exit.', 'density':'Three 32x32 reward icon rectangles and refreshed mesh dimensions at 1/32 world units per texel, actual 1920x1080 and 960x540. Not complete same-scene inventory or human approval.', 'compile':'Final request_recompile recovery Success, reward_flat_reload.json', 'console':'No cached errors in sampled final 180 seconds, reward_flat_console.json; prior QuickInstaller exception not resolved by this task.', 'cleanup':'Original Game View index 3 restored; temporary index 7 removed; original EpicToonVfxShowcase clean, Edit Mode; no scene save.'}
report['current_screenshots'] = [bound(root / 'UnityProject/ReviewEvidence/20261010_hd2d_desk' / ('reward_flat_' + s + '.png')) for s in ['1920','960']]
report['blockers'] = ['Complete visible-scene pixel inventory, motion and human aesthetic approval remain pending.', 'Full normal combat journey, save/reload and backpack-full branch not verified this turn.', 'No standalone Player performance sample; target 60 FPS / P95 <=16.7 ms at both approved resolutions remains unmeasured.']
for sample in report['performance']['measurements']:
    sample.update(target_fps=60,p95_budget_ms=16.7)
report_path.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
diffs=[]
for p in changed:
    before=folder/(p.stem+'.before.cs')
    diffs.extend(difflib.unified_diff(before.read_text(encoding='utf-8-sig').splitlines(True),p.read_text(encoding='utf-8-sig').splitlines(True),fromfile=before.name,tofile=p.name))
(folder/'source_changes.diff').write_text(''.join(diffs),encoding='utf-8')
print('Local reward density and interaction checks PASS; overall quality QA_PENDING.')
