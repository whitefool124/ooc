import json,hashlib
from pathlib import Path
root=Path.cwd(); folder=Path(__file__).resolve().parent
p=folder/'quality_pending.json'; r=json.loads(p.read_text(encoding='utf8'))
def bound(path): return {'path':path,'sha256':hashlib.sha256((root/path).read_bytes()).hexdigest()}
changed=['UnityProject/Assets/Game/Runtime/Presentation/'+name for name in ['Hd2DLoadoutScene.cs','Hd2DLoadoutScene.Lighting.cs','Hd2DSurfaceProfile.cs','Hd2DDeskArtCandidate.cs']]
r['changed_files']=changed
for path in changed+['UnityProject/Assets/Game/Resources/Art/MainMenuE/Hd2D/Hd2DSurface.shader']:
 if path not in [i['path'] for i in r['inputs']]:r['inputs'].append(bound(path))
r['configuration'].update(exposure=.85,fill_light={'room_bounce':.9},material_profile={'wood_smoothness':.08,'desktop_coat':.06,'edge_coat':.03,'coat_smoothness':.35,'content_emission':.3,'inventory_grid_emission':.12,'cup_emission':.1,'independent_wood_surface_maps':'not authored'},source_sampling='Existing Point textures / UV density unchanged')
r['captures']=[bound('UnityProject/ReviewEvidence/20261010_hd2d_desk/'+name+'.png') for name in ['roughbright_default_1920','roughbright_default_960','roughbright_high_960','roughbright_spell_960']]
r['runtime_records']=[bound('Worldbuilding/归档/2026-10-10_HD2D书桌品质样板/'+name+'.json') for name in ['response_roughbright_ready','response_roughbright_play','response_roughbright_entry_status_final','response_roughbright_readback','response_roughbright_lamp_restore','response_roughbright_console','response_roughbright_edit']]
r['runtime'].update(normal_entry=True,interaction_pass=True,console_clear=False)
r['scope']='desk only: no shared pipeline, battlefield, texture, UV or camera edits'
r['blockers']=['Independent wood grain normal/property maps still missing; this iteration fixes gloss and brightness only','Human full sample approval, density/construction/motion review and Player performance pending','Existing Console Illegal byte sequence exception; current ShaderUtil messages empty']
p.write_text(json.dumps(r,ensure_ascii=False,indent=2),encoding='utf8')
