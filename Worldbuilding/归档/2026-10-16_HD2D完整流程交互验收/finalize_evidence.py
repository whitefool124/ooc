from pathlib import Path
import hashlib,json,re,subprocess,sys,xml.etree.ElementTree as ET
root=Path(__file__).resolve().parents[3];folder=Path(__file__).resolve().parent
def read(p):return json.loads(p.read_text('utf-8-sig'))
def bound(p):
    p=Path(p)
    if not p.is_absolute():p=root/p
    return {'path':p.relative_to(root).as_posix(),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()}
for name in ['runtime','runtime_branches','runtime_supplement']:
    assert (folder/name/'completed.json').is_file()
    assert not (folder/name/'failure.json').exists()
    assert read(folder/name/'save_isolation.json')['playerSlotsUnchanged']
    assert read(folder/name/'cleanup.json')=={'play':False,'dirty':False,'sceneSaved':False}
tests=ET.parse(folder/'tests_time_gate.xml').getroot();assert tests.get('result')=='Passed' and tests.get('passed')=='2'
changed=[root/'UnityProject/Assets/Game'/p for p in ['Runtime/Presentation/Hd2DPaperSkin.cs','Runtime/Presentation/Hd2DContentGeometry.cs','Runtime/Presentation/Hd2DServiceReadingText.cs','Runtime/Campaign/RogueliteMapRun.cs','Tests/EditMode/AcademyLayerFlowTests.cs','Editor/Hd2DFullJourneyAudit.cs']]
logs=[]
for name in ['editor.log','branches_editor.log','supplement_editor.log']:
    content=(folder/name).read_text('utf-8',errors='replace')
    assert not re.search(r'error CS\d+',content)
    errors=re.findall(r'(?:NullReferenceException|InvalidOperationException|ArgumentException):[^\n]*\n[^\n]*',content)
    editor_only=all('UnityEditor.GameView.OnPlayModeStateChanged' in e for e in errors)
    assert editor_only,(name,errors)
    logs.append({'log':bound(folder/name),'editor_tool_exceptions':len(errors),'game_runtime_exceptions':0})
record={'scope':'Background EventSystem normal save journey, not native-window hardware input or complete aesthetic approval','status':'PASS_BACKEND_FUNCTIONAL_SCOPE','actual_input_screen':[640,480],'offscreen_outputs':[[1920,1080],[960,540]],'normal_entry_victory':read(folder/'runtime/completed.json'),'natural_failure_branches':read(folder/'runtime_branches/completed.json'),'isolated_supplement':read(folder/'runtime_supplement/completed.json'),'time_gate_tests_passed':2,'player_slots_unchanged':True,'scene_saved':False,'console':logs,'changed_inputs':list(map(bound,changed)),'remaining':['Native 1920/960 window mouse/keyboard and resizing','Save failure/bad archive branches and every content choice','Whole-scene density, motion, lighting, performance and genuine human aesthetic approval']}
(folder/'functional_summary.json').write_text(json.dumps(record,ensure_ascii=False,indent=2),'utf-8')
path=folder/'quality_desk.json'
if not path.exists():
    subprocess.run([sys.executable,'-X','utf8',str(root/'Tools/OCCArt/create_hd2d_quality_report.py'),'--scene','desk','--output',str(path)],cwd=root,check=True,capture_output=True)
report=read(path);report['changed_files']=[p.relative_to(root).as_posix() for p in changed]
inputs={x['path']:bound(x['path']) for x in report['inputs']};inputs.update({x['path']:x for x in map(bound,changed)})
report['inputs']=list(inputs.values());report['runtime'].update(normal_entry=True,console_clear=False,interaction_pass=False,isolation_pass=True,resource_lifetime_pass=False,record=bound(folder/'functional_summary.json'))
report['local_evidence']=[bound(p) for directory in ['runtime','runtime_branches','runtime_supplement'] for p in sorted((folder/directory).iterdir()) if p.suffix in ('.json','.png')]
report['blockers']=['Native 1920/960 interactions are not covered by actual Screen640x480 background input','Editor-only GameView experiment exceptions retained in historical successful journey logs; experiment removed','Density, full motion, performance, full configuration and genuine human aesthetic approval pending']
path.write_text(json.dumps(report,ensure_ascii=False,indent=2),'utf-8')
result=subprocess.run([sys.executable,'-X','utf8',str(root/'Tools/OCCArt/validate_hd2d_quality.py'),str(path),'--scope','desk'],cwd=root,capture_output=True,text=True,encoding='utf-8')
(folder/'quality_gate_desk.json').write_text(result.stdout,'utf-8')
print('Functional scope verified; 2 time-gate tests pass; player slots and scenes preserved. Quality baseline gate:',result.returncode,'(QA_PENDING)')
