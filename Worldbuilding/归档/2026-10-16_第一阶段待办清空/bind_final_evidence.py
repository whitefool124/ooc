from pathlib import Path
import hashlib,json,subprocess,sys
import xml.etree.ElementTree as ET

root=Path(__file__).resolve().parents[3]
folder=Path(__file__).resolve().parent
def binding(path):
    p=Path(path)
    if not p.is_absolute():p=root/p
    return {'path':p.relative_to(root).as_posix(),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()}

names={'BattlefieldArtResources.cs','CombatFormalVisualAssets.cs','CombatBattlefieldCellPresenter.cs','FormalArtRegistry.cs','EnemyArchetypes.cs','FormalArtImportPostprocessor.cs','Hd2DBattlefieldTactics.cs','Hd2DServiceReadingText.cs','Hd2DServiceScene.cs','Hd2DContentGeometry.cs','FormalRogueliteUi.cs','AcademyMealCatalog.cs','RogueliteMapRun.cs','RogueRunDto.cs','RogueEquipmentRuntime.cs','RogueSpellRuntime.cs','FireSpellRuntime.cs','OccSoundEffects.cs'}
changed=[p for p in (root/'UnityProject/Assets/Game').rglob('*.cs') if p.name in names]
adoptions=json.loads((folder/'adopted_assets.json').read_text('utf-8'))
for item in adoptions:
    p=root/'UnityProject'/item['target'];changed.extend([p,Path(str(p)+'.meta')])
validation=json.loads((folder/'adopted_manifest_validation.json').read_text('utf-8'))
changed.extend(root/p for p in validation['updated'])
assert len(adoptions)==24 and all(x['pass'] for x in validation['validation'])
runtime=folder/'runtime'
assert json.loads((runtime/'cleanup.json').read_text('utf-8'))=={'play':False,'dirty':False,'sceneSaved':False}
assert not (runtime/'failure.json').exists()
captures=sorted(runtime.glob('[0-9][0-9]_*.json'))
assert len(captures)==13
tests={t.attrib['fullname']:t.attrib['result'] for name in ['tests.xml','tests_meal_completed.xml','tests_weapon_meals.xml'] for t in ET.parse(folder/name).iter('test-case')}
assert len(tests)==55 and all(result=='Passed' for result in tests.values())
(folder/'tests_effective_summary.json').write_text(json.dumps(tests,ensure_ascii=False,indent=2),'utf-8')
for p in captures:
    data=json.loads(p.read_text('utf-8'))
    assert data['focusRequested']==False and data['saveWritten']==False
    assert p.with_suffix('.png').is_file() and p.with_name(p.stem+'_960.png').is_file()
for kind,scope in [('battlefield','battlefield'),('services','desk')]:
    path=folder/f'quality_{kind}.json';report=json.loads(path.read_text('utf-8'))
    report['changed_files']=[p.relative_to(root).as_posix() for p in changed]
    inputs={x['path']:binding(x['path']) for x in report['inputs']}
    inputs.update({x['path']:x for x in map(binding,changed)})
    report['inputs']=list(inputs.values())
    # Local implementation evidence is not a substitute for the full baseline cases.
    report['local_evidence']=[binding(p) for p in sorted(runtime.iterdir()) if p.suffix in ('.json','.png')]
    report['blockers']=['Whole-scene density, motion, performance and genuine human aesthetic approval remain outside this production closure; no APPROVED_BASELINE claim']
    path.write_text(json.dumps(report,ensure_ascii=False,indent=2),'utf-8')
    result=subprocess.run([sys.executable,'-X','utf8',str(root/'Tools/OCCArt/validate_hd2d_quality.py'),str(path),'--scope',scope],cwd=root,capture_output=True,text=True,encoding='utf-8')
    (folder/f'quality_gate_{kind}.json').write_text(result.stdout,'utf-8')
    print(kind,'baseline gate:',result.returncode,'(approval remains pending)')
(folder/'closure.json').write_text(json.dumps({'scope':'Current production backlog only; excludes acceptance and unresolved master aggregate counts','production_remaining':0,'adopted_assets':24,'source_validation_passed':24,'unique_local_tests_passed':55,'runtime_cases':13,'resolutions':[[1920,1080],[960,540]],'scene_saved':False,'player_save_written':False,'quality_baseline':'QA_PENDING','evidence':[binding(p) for p in captures]},ensure_ascii=False,indent=2),'utf-8')
print('Production closure evidence bound: 24 assets, 13 local cases; no baseline approval claimed')
