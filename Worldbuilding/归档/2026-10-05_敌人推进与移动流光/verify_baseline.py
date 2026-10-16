import subprocess,json,time
from pathlib import Path
p=Path(__file__).parent
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

source=Path('UnityProject/Assets/Game/Runtime/Combat/EnemyArchetypes.cs')
current=source.read_bytes()
try:
 source.write_bytes((p/'backup/EnemyArchetypes.cs').read_bytes())
 compile()
 subprocess.run(['python',str(p/'run_checks.py'),'EnemyPackTests'],check=True)
 (p/'EnemyPackTests.baseline.json').write_bytes((p/'EnemyPackTests.json').read_bytes())
finally:
 source.write_bytes(current)
 compile()
 print('Current movement implementation restored and compiled',flush=True)
subprocess.run(['python',str(p/'run_checks.py'),'EnemyPackTests'],check=True)
