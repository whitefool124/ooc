import json
from pathlib import Path
folder=Path(__file__).parent;root=folder.parents[2]
path=root/'Tools/OCCArt/occ_art_contract_v1.json'
current=json.loads(path.read_text(encoding='utf-8'))
old=(folder/'occ_art_contract_v1.json').read_text(encoding='utf-8-sig')
insertion=json.dumps(current['roles']['ui_detail_adaptive'],ensure_ascii=False)
new=old.replace('"roles": {','"roles": {\n    "ui_detail_adaptive": '+insertion+',',1)
assert json.loads(new)==current
path.write_text(new,encoding='utf-8')
path=root/'Tools/OCCArt/native32_generation_profiles.json'
current=json.loads(path.read_text(encoding='utf-8'))
old=(folder/'native32_generation_profiles.json').read_text(encoding='utf-8-sig').rstrip()
new=old[:-1].rstrip()+',\n  "nonbattlefield_display_exception": '+json.dumps(current['nonbattlefield_display_exception'],ensure_ascii=False)+'\n}\n'
assert json.loads(new)==current
path.write_text(new,encoding='utf-8')
