import json, subprocess, time, sys
from pathlib import Path
out=Path(__file__).parent
base=['unity','command']
common=['--project-path','E:/数据库/OCC_Codex/UnityProject','--caller','plugin','--skill','occ-pixel-vfx','--format','json']
def call(args):
    r=subprocess.run(base+args+common,capture_output=True,text=True,encoding='utf-8',errors='replace')
    if r.returncode: raise RuntimeError(r.stdout+r.stderr)
    return json.loads(r.stdout)
for group in sys.argv[1:] or ['CombatIntentChangeTrackerTests','CombatFeedbackEventTests','EnemyTurnSequenceTests','CombatSelectionControllerTests','BattlefieldPresentationAdapterTests']:
    call(['run_tests','--mode','editor','--filter',group,'--filter_type','testName','--async_tests','true','--timeout','300'])
    for attempt in range(60):
        time.sleep(1)
        raw=call(['test_status'])
        result=raw['data']['result']
        if isinstance(result,str): result=json.loads(result)
        if result.get('status')!='completed': continue
        (out/(group+'.json')).write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
        print(group, result.get('summary'),flush=True)
        break
    else: raise RuntimeError('Tests did not finish: '+group)
