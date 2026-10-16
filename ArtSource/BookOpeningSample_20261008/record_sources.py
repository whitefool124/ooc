import json,hashlib,subprocess,sys
from pathlib import Path
root=Path(__file__).resolve().parent
records=[]
for attempt in ['r1','r2','r3']:
    source=root/f'source-{attempt}.png'
    qa=json.loads((root/attempt/'sheet-qa.json').read_text())
    reason='Frame lattices disagree; some confidence below 1.2; no sequence accepted.'
    manifest={'schema':'occ-art-manifest-v1','contract_version':1,'asset_id':f'ui.main_menu_e.book_opening_sample.{attempt}','role':'ui_detail_adaptive','status':'REJECTED','provenance':{'source_channel':'codex_builtin_imagegen','source_descriptor':'Independent six-key-pose book opening source; rejected as sequence','source_path':source.relative_to(Path.cwd()).as_posix(),'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest()},'delivery':{'output_path':None,'output_sha256':None,'logical_cells':None,'palette_max':16},'human_review':{'overall':'PENDING','notes':'Not offered for approval or imported: machine sequence rejection precedes aesthetic gate'},'rejection':{'reason':reason,'frame_diagnostics':qa},'known_cost':None}
    path=root/attempt/'manifest.json';path.write_text(json.dumps(manifest,ensure_ascii=False,indent=2))
    subprocess.run([sys.executable,'Tools/OCCArt/validate_occ_art_asset.py',str(path),'--report',str(root/attempt/'validation.json')],capture_output=True)
    records.append({'attempt':attempt,'status':'REJECTED','provider':'Codex built-in image_gen','known_cost':None,'reason':reason,'grid_report':f'{attempt}/sheet-qa.json'})
(root/'run-log.json').write_text(json.dumps({'calls':3,'cost':None,'records':records},ensure_ascii=False,indent=2))
contract=json.loads((root/'contract.json').read_text());contract['status']='REJECTED after 3 independent rounds; no generated frame imported';(root/'contract.json').write_text(json.dumps(contract,ensure_ascii=False,indent=2))
print('Recorded 3 independent rejected source sequences; no delivery or formal promotion.')
