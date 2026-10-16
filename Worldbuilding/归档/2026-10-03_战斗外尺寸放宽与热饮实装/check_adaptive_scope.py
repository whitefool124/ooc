import importlib.util,json
from pathlib import Path
folder=Path(__file__).parent;root=folder.parents[2]
spec=importlib.util.spec_from_file_location('asset_validator',root/'Tools/OCCArt/validate_occ_art_asset.py')
module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
c=json.loads((root/'Tools/OCCArt/occ_art_contract_v1.json').read_text(encoding='utf-8-sig'))
p=folder/'hot-drink.png'
adaptive,_=module.validate_image(p,c['roles']['ui_detail_adaptive'],None,10,[])
fixed,_=module.validate_image(p,c['roles']['equipment_icon_32'],None,10,[])
assert not adaptive,adaptive
assert any('delivery size' in error for error in fixed),fixed
(folder/'scope-check.txt').write_text('PASS: 37x37 complete subject accepted for ui_detail_adaptive; same file rejected by fixed32 equipment icon contract. Battlefield roles unchanged.',encoding='utf-8')
