import json,hashlib
from pathlib import Path
root=Path(__file__).resolve().parents[3]
p=root/'ArtSource/desk_candidate_lamp_scale_20261010/cup/manifest.occ-art-manifest-v1.json'
m=json.loads(p.read_text(encoding='utf-8'))
e=root/'UnityProject/ReviewEvidence/20261010_hd2d_desk/lamp_scale_cup_menu_1920.png'
m['evidence']['application_contact']=e.relative_to(root).as_posix()
m['evidence']['application_contact_sha256']=hashlib.sha256(e.read_bytes()).hexdigest()
m['application']['verification_mode']='1920x1080 real Play Mode in-memory preview; not persistent asset import'
p.write_text(json.dumps(m,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
