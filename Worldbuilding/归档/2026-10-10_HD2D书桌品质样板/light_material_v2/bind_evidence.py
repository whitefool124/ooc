import json,hashlib
from pathlib import Path
root=Path.cwd(); folder=root/'Worldbuilding/归档/2026-10-10_HD2D书桌品质样板/light_material_v2'
p=folder/'quality_pending.json';r=json.loads(p.read_text(encoding='utf8'))
names=['Hd2DDeskArtCandidate.cs','Hd2DSurfaceProfile.cs','Hd2DLoadoutScene.cs','Hd2DLoadoutScene.Art.cs','Hd2DLoadoutScene.Lighting.cs','Hd2DDeskLampControl.cs']
changed=['UnityProject/Assets/Game/Runtime/Presentation/'+n for n in names]+['UnityProject/Assets/Game/Resources/Art/MainMenuE/Hd2D/Hd2DSurface.shader']
def bound(name):return {'path':name,'sha256':hashlib.sha256((root/name).read_bytes()).hexdigest()}
r['changed_files']=changed
r['inputs']=[bound(i['path']) for i in r['inputs']]
for name in changed:
 if name not in [i['path'] for i in r['inputs']]:r['inputs'].append(bound(name))
previous=json.loads((folder.parent/'material_response/quality_pending.json').read_text(encoding='utf8'))
r['configuration'].update({k:v for k,v in previous['configuration'].items() if k not in ['construction_manifest','pixel_density_manifest']})
r['configuration'].update(main_light={'intensity':2.3,'rotation':[32,165,0],'colour':[1,.97,.91]},fill_light={'window_point':1.3,'room_bounce_directional':.6,'room_bounce_rotation':[25,0,0],'local_point':1.1},shadows={'soft':True,'resolution':4096,'cascades':2,'desk_strength':.88,'crystal_spot_strength':.75},material_profile={'wood_base_smoothness':.18,'wood_albedo_derived_relief':0,'wood_albedo_derived_roughness':0,'desktop_coat':.28,'edge_coat':.16,'architecture_coat':0,'coat_smoothness':.8,'independent_maps':'Inputs implemented; no independent normal/property map authored or bound this iteration'},probe_update={'resolution':256,'mode':'IndividualFaces','settle_seconds':.15,'minimum_interval_seconds':.4,'trigger':'construction and visible crystal slider changes'},source_sampling='Existing native colour textures and 32 PPU UVs unchanged; Point sampling',depth_of_field='off on working plane; no focus change this iteration')
r['captures']=[bound('UnityProject/ReviewEvidence/20261010_hd2d_desk/response_v2_'+name+'.png') for name in ['before_1920','final_default_1920','final_no_bloom_1920','final_high_1920','final_off_1920','final_default_960','final_no_bloom_960','final_high_960','final_off_960','final_spell_1920']]
r['runtime_records']=[bound('Worldbuilding/归档/2026-10-10_HD2D书桌品质样板/'+name+'.json') for name in ['response_v2_final_editor_state','response_v2_final_entry_status','response_v2_final_default_readback','response_v2_final_high_readback','response_v2_final_off_readback','response_v2_final_quiet_readback','response_v2_final_lifetime','response_v2_final_console']]
r['runtime'].update(normal_entry=True,interaction_pass=True,resource_lifetime_pass=True,console_clear=False,record='Worldbuilding/归档/2026-10-10_HD2D书桌品质样板/response_v2_final_entry_status.json')
r['scope']='desk only; Surface shader references audited to desk presentation only; no shared URP asset/pipeline changes or battlefield use'
r['blockers']=['Human visual and full density/motion review pending; no approved baseline promotion','Independent normal/property maps and spatial indirect illumination remain future work; inputs alone are not completed authored materials','Standalone Player performance / P95 and probe-refresh peak unmeasured','Existing QuickInstaller byte-sequence Console exception remains']
p.write_text(json.dumps(r,ensure_ascii=False,indent=2),encoding='utf8')

