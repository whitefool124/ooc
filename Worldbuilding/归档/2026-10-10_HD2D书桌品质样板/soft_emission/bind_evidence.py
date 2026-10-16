import json, hashlib
from pathlib import Path
root = Path.cwd()
folder = Path(__file__).resolve().parent
p = folder / 'quality_pending.json'
r = json.loads(p.read_text(encoding='utf8'))
changed = ['UnityProject/Assets/Game/Resources/Art/MainMenuE/Hd2D/Hd2DSurface.shader',
           'UnityProject/Assets/Game/Runtime/Presentation/Hd2DLoadoutScene.cs',
           'UnityProject/Assets/Game/Runtime/Presentation/Hd2DDeskArtCandidate.cs']
def bound(path):
    return {'path': path, 'sha256': hashlib.sha256((root/path).read_bytes()).hexdigest()}
r['changed_files'] = changed
for name in changed:
    if name not in [i['path'] for i in r['inputs']]: r['inputs'].append(bound(name))
r['configuration']['material_profile'] = {'reading_content_soft_emission': .16, 'inventory_grid_soft_emission': 0, 'cup_soft_emission': .06, 'wood_and_room_soft_emission': 0, 'method': 'Tinted albedo additive emission, existing PBR and crystal emission preserved'}
r['scope'] = 'Local desk only; existing shader callers confined to desk presentation. No texture, UV, camera, exposure or pipeline change.'
r['captures'] = [bound('UnityProject/ReviewEvidence/20261010_hd2d_desk/'+name+'.png') for name in ['softglow_default_1920','softglow_no_bloom_1920','softglow_default_960','softglow_spell_960']]
r['runtime_records'] = [bound('Worldbuilding/归档/2026-10-10_HD2D书桌品质样板/'+name+'.json') for name in ['response_softglow_compile_confirm','response_softglow_play','response_softglow_entry_status','response_softglow_readback','response_softglow_console','response_softglow_restore_size','response_softglow_edit']]
r['runtime'].update(normal_entry=True, interaction_pass=True, console_clear=False)
r['blockers'] = ['Sample remains QA_PENDING: human approval, complete density/construction/motion review and Player performance unavailable', 'Existing tool journal file error prevented durable prepare_editor tracking; actual Editor readiness and material/shader readback verified', 'Existing Console illegal-byte-sequence exception remains; ShaderUtil messages empty']
p.write_text(json.dumps(r, ensure_ascii=False, indent=2), encoding='utf8')
