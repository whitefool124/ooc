"""Sequential same-editor local capture batch; each completed cleanup unlocks next."""
import json
from pathlib import Path
import subprocess
import time
import sys

ROOT=Path(__file__).resolve().parents[3];ARCHIVE=Path(__file__).resolve().parent
families=sys.argv[1:] or ['AcademyCourt','Conservatory','StoneRoad','RuinSite','InteriorHall','AetherTower','Prism','Pressure']
for family in families:
    started=time.time();folder=ARCHIVE/'profiles'/family
    result=subprocess.run(['unity','command','menu','--path',f'OCC/Tools/Capture battle profile/{family} (no focus)',
        '--project-path',str(ROOT/'UnityProject'),'--caller','plugin','--skill','unity-cli','--format','json'],capture_output=True,text=True,encoding='utf-8')
    reply=json.loads(result.stdout)
    if result.returncode or not reply.get('success') or not reply['data']['result'].get('success'):
        raise RuntimeError(result.stdout)
    print('started '+family,flush=True)
    while True:
        failure=folder/'failure.json'
        if failure.exists() and failure.stat().st_mtime>=started:
            raise RuntimeError(failure.read_text(encoding='utf-8'))
        capture=folder/'capture.json';cleanup=folder/'cleanup.json'
        if capture.exists() and cleanup.exists() and min(capture.stat().st_mtime,cleanup.stat().st_mtime)>=started:
            c=json.loads(capture.read_text(encoding='utf-8'));d=json.loads(cleanup.read_text(encoding='utf-8'))
            assert c['status']=='captured' and not d['play'] and not d['dirty'] and d['pipelineRestored'] and d['scenePreserved']
            print(json.dumps({'family':family,'level':c['level'],'registeredNewBatchMaterials':len(c['mappedEnvironment']),'cleanup':d}),flush=True)
            break
        if time.time()-started>120:raise TimeoutError('Capture observation timeout; inspect current Editor state before any restart: '+family)
        time.sleep(1)
print(f'all{len(families)} local scene captures completed',flush=True)
