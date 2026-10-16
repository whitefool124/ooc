import subprocess,json,time
from pathlib import Path
p=Path('Worldbuilding/归档/2026-10-05_方框动画与幽灵阻挡')
source=Path('UnityProject/Assets/Game/Runtime/Presentation/CombatTestArenaScenarioCatalog.cs')
current=source.read_bytes()
args=['--project-path','E:/数据库/OCC_Codex/UnityProject','--caller','plugin','--skill','occ-pixel-vfx','--format','json']
def call(cmd):
 for attempt in range(5):
  r=subprocess.run(['unity','command']+cmd+args,capture_output=True,encoding='utf-8',errors='replace');d=json.loads(r.stdout)
  if d['success']:return d['data']['result']
  if cmd[0] not in ['editor_status','recompile_status']:raise RuntimeError(d)
  time.sleep(3)
 raise RuntimeError(d)
def compile():
 call(['recompile'])
 for i in range(90):
  time.sleep(1);d=call(['recompile_status']);d=json.loads(d) if isinstance(d,str) else d
  if d.get('status')=='completed':
   if d.get('failed'):raise RuntimeError(d)
   for j in range(60):
    time.sleep(1);e=call(['editor_status'])
    if not e.get('compiling') and not e.get('domainReloadInProgress') and e.get('status')=='ready':
     time.sleep(2);return
   raise RuntimeError('Editor not ready')
 raise RuntimeError('compile timeout')

compile()
print("Editor compiled and ready",flush=True)
subprocess.run(["python",str(p/"run_checks.py"),"CombatRangeTransitionTests","CombatBattlefieldCellPresenterTests","EveryBlockedArenaCellHasVisibleObjectTerrain"],check=True)
