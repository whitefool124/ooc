import json,hashlib
from pathlib import Path
root=Path.cwd(); folder=root/'Worldbuilding/归档/2026-10-10_HD2D书桌品质样板/material_response'
p=folder/'quality_pending.json';r=json.loads(p.read_text(encoding='utf8'))
files=['Hd2DDeskArtCandidate.cs','Hd2DLoadoutScene.cs','Hd2DLoadoutScene.Art.cs','Hd2DLoadoutScene.Lighting.cs','Hd2DDeskLampControl.cs','Hd2DSurfaceProfile.cs','Hd2DSurfaceProfile.cs.meta']
changed=['UnityProject/Assets/Game/Runtime/Presentation/'+name for name in files]+['UnityProject/Assets/Game/Resources/Art/MainMenuE/Hd2D/Hd2DSurface.shader','UnityProject/Assets/Game/Resources/Art/MainMenuE/Hd2D/Hd2DSurface.shader.meta']
def bound(name):return {'path':name,'sha256':hashlib.sha256((root/name).read_bytes()).hexdigest()}
r['changed_files']=changed
r['inputs']=[bound(i['path']) for i in r['inputs']]
for name in changed:
 if name not in [i['path'] for i in r['inputs']]:r['inputs'].append(bound(name))
r['configuration'].update(msaa=False,output_policy='HD 1920x1080 and actual 960x540 GameView; renderScale 1 unchanged',tone_mapping='ACES',exposure=.35,bloom={'intensity':.45,'threshold':1.3,'scatter':.6},depth_of_field='off for loadout working plane',shadows={'soft':True,'resolution':4096,'cascades':2,'desk_strength':.72},main_light={'intensity':1.65,'rotation':[32,165,0],'colour':[1,.97,.91]},fill_light={'window_point':2.4,'room_bounce_directional':1.1,'room_bounce_rotation':[25,0,0],'local_point':2.5},local_lights={'crystal_spot_intensity':'6 * slider','crystal_mask_emission':'10 * slider','source_brightness_samples':[0,.4,1]},material_profile={'wood':{'smoothness':.52,'grain_relief':1.15,'roughness_variation':.28},'desk_edge_smoothness':.36,'paper_smoothness':.08,'ceramic_smoothness':.65,'brass':{'source_colour_mask':True,'metal_fraction':.85,'smoothness':.72},'probe':{'resolution':256,'intensity':1,'capture':'one shot after construction'}},source_sampling='Point; colour PNGs, world pixel pitch and UV placement unchanged')
r['runtime'].update(normal_entry=True,console_clear=False,interaction_pass=True,resource_lifetime_pass=True,record='Worldbuilding/归档/2026-10-10_HD2D书桌品质样板/materials_entry_status_a2.json')
r['captures']=[bound('UnityProject/ReviewEvidence/20261010_hd2d_desk/materials_'+name+'.png') for name in ['before_1920','default_1920','high_1920','off_1920','default_960','high_960','off_960','spell_1920','spell_960','map_1920','map_960']]
r['runtime_records']=[bound('Worldbuilding/归档/2026-10-10_HD2D书桌品质样板/'+name+'.json') for name in ['materials_ready_a2','materials_readback_a2','materials_lamp_high_1920','materials_lamp_off_1920','materials_lamp_high_960','materials_lamp_restore','materials_lifetime','materials_console_final']]
r['blockers']=['Human artistic and complete pixel-density sample review pending; no baseline promotion','Standalone Player CPU/GPU/P95/memory and page-switch peak measurements pending','Paper/card volume and reflected highlights still need further artistic evaluation','Existing QuickInstaller byte-sequence exception observed in Editor; no surface shader messages']
r['scope']='desk only: candidate surface shader and runtime desk material/light profiles; no shared pipeline or battlefield changes this iteration'
p.write_text(json.dumps(r,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
print('Current desk evidence bound; QA_PENDING retained.')
