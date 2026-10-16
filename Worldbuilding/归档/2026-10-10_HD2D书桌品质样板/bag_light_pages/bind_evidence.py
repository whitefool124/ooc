import difflib
import hashlib
import json
from pathlib import Path

folder = Path(__file__).resolve().parent
archive = folder.parent
root = folder.parents[3]
report_path = folder / 'quality_pending.json'
report = json.loads(report_path.read_text(encoding='utf-8'))
names = ['Hd2DLoadoutScene.cs', 'Hd2DLoadoutScene.Art.cs']
changed = [root / 'UnityProject/Assets/Game/Runtime/Presentation' / name for name in names]

def bound(path):
    return {'path': path.relative_to(root).as_posix(), 'sha256': hashlib.sha256(path.read_bytes()).hexdigest()}

def result(name):
    envelope = json.loads((archive / (name + '.json')).read_text(encoding='utf-8'))
    return json.loads(envelope['result']['content'][0]['text'])

def legacy(name):
    return json.loads(result(name)['data']['result'])

assert result('bag_light_a5_ready')['data']['operation']['status'] == 'ready'
assert result('bag_light_a5_entry_status')['data']['result'] == 'PASS:entry'
for name in ['open', 'spell_open', 'select_spell', 'equipment_return', 'return_map', 'reward_bag_open', 'return_reward']:
    assert result('bag_light_a5_' + name)['data']['hit'], name
assert any(b['name'] == 'map.node.B2' for b in legacy('bag_light_a5_return_map_state')['buttons'])
assert any(b['name'] == '按钮_返回战利品' for b in legacy('bag_light_a5_reward_bag_buttons')['buttons'])
assert legacy('bag_light_a5_reward_before') == legacy('bag_light_a5_reward_after')
current = legacy('bag_light_a5_density')
simple = [item for item in current if item['active'] and item['alpha'] >= .01 and 'tiled/sliced' not in item['note']]
assert simple and all(abs(pitch - 1/32) < 1e-6 for item in simple for pitch in item['simple_texel_world_pitch'])
lights = result('bag_light_a5_lights')['data']['returnValue']
assert lights['cellCasters'] if 'cellCasters' in lights else True
preflight = legacy('bag_light_final_preflight')
assert not preflight['play'] and all(not s['isDirty'] for s in preflight['scenes'])
assert preflight['activeScene'] == 'Assets/Scenes/EpicToonVfxShowcase.unity'

captures = [root / 'UnityProject/ReviewEvidence/20261010_hd2d_desk' / (prefix + resolution + '.png')
            for prefix in ['bag_light_a5_', 'bag_light_spell_a5_'] for resolution in ['1920','960']]
paths = [root / entry['path'] for entry in report['inputs']] + changed + captures
paths += list(archive.glob('bag_light_*.json'))
paths += [root / 'UnityProject/Assets/Game/Resources/Art' / p for p in ['VictorBattle/victor_static.asset','Hd2DContentFrames/简洁纸边.asset','Hd2DContentFrames/简洁纸边.png','Hd2DContent20261009/student_dossier.png','MainMenuE/Stage/desk.png']]
report['inputs'] = [bound(p) for p in sorted(set(paths)) if p.is_file()]
report['changed_files'] = [p.relative_to(root).as_posix() for p in changed]
report['configuration'].update(hdr=True, msaa=False, field_of_view=24)
report['local_implementation'] = {
    'scope':'Existing equipment/backpack and spell preparation desk presentation; no new gameplay or formal asset replacement',
    'world_pitch':1/32,
    'lighting':lights,
    'changes':'Local window key light, reduced fills, matte paper, dielectric textured wood; flat grid guides do not cast shadows. Native-sized character, equipment, tactical symbols and dossier; compact text-only spell rows, native selected spell illustration; book/lamp/contact positioning repairs.'}
report['runtime'].update(normal_entry=True, interaction_pass=True, console_clear=False,
    record=bound(archive / 'bag_light_a5_entry_status.json'))
report['validation_scope'] = {
    'normal_entry':'PASS:entry and actual map > backpack > spells > select second spell > equipment > map clicks',
    'reward':'Disposable B1 in-memory reward > organise inventory > return reward; pending state unchanged. Not a complete combat journey or full-backpack test.',
    'density':'Visible simple Image sprite rectangles in the initial equipment page have 1/32 world pitch. Source cluster uniformity, model/environment completeness and populated backpack remain pending.',
    'resolution':'Actual Game View/render framebuffer 1920x1080 and 960x540; static screenshots inspected. No standalone Player performance measurements.',
    'console':'Compilation ready, no compilation errors. Cached error sample contains QuickInstaller ExecutionEngineException (illegal byte sequence in assembly CodeBase). Not resolved by this local presentation change.',
    'cleanup':'Original Game View index 3 restored, verified temporary size removed; original EpicToonVfxShowcase clean, Edit Mode; no scene save. Audit verification slots restore on Play exit.'}
report['current_screenshots'] = [bound(p) for p in captures]
report['blockers'] = [
    'Populated/full backpack, drag/drop and save/reload require verification; not cleared for full preparation-page quality promotion.',
    'Complete scene pixel density/construction inventory, motion/no-post/light extremes and genuine human approval are pending.',
    'Unity QuickInstaller cached Editor exception remains unresolved.',
    'Standalone Player 60 FPS / P95 <=16.7 ms at both resolutions is unmeasured.']
report['performance']['device'] = lights['cpu'] + ' / ' + lights['gpu']
for sample in report['performance']['measurements']:
    sample.update(target_fps=60, p95_budget_ms=16.7)
report_path.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
diffs = []
for p in changed:
    before = folder / (p.stem + '.before.cs')
    diffs.extend(difflib.unified_diff(before.read_text(encoding='utf-8-sig').splitlines(True),p.read_text(encoding='utf-8-sig').splitlines(True),fromfile=before.name,tofile=p.name))
(folder / 'source_changes.diff').write_text(''.join(diffs),encoding='utf-8')
print('Local interactions and current equipment sprite rectangle pitch checks PASS. Overall QA_PENDING.')
