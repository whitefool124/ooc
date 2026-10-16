import json,hashlib
from pathlib import Path
root=Path.cwd(); folder=root/'Worldbuilding/归档/2026-10-10_HD2D书桌品质样板/realism_render'
changed=['UnityProject/Assets/Settings/UniversalRP.asset','UnityProject/Assets/Game/Resources/Art/MainMenuE/Hd2D/HD2DRenderer.asset','UnityProject/Assets/Game/Runtime/Presentation/Hd2DLoadoutScene.cs','UnityProject/Assets/Game/Runtime/Presentation/Hd2DLoadoutScene.Lighting.cs','UnityProject/Assets/Game/Runtime/Presentation/PixelPerfectDisplayGuard.cs']
def item(p):return {'path':p,'sha256':hashlib.sha256((root/p).read_bytes()).hexdigest()}
for kind in ['desk','battle']:
 p=folder/(kind+'_quality_pending.json');r=json.loads(p.read_text(encoding='utf8'));r['changed_files']=changed; r['inputs']=[item(i['path']) for i in r['inputs']]
 for name in changed:
  if name not in [i['path'] for i in r['inputs']]:r['inputs'].append(item(name))
 r['configuration'].update(msaa=False,output_policy='renderScale=1, actual GameView 1920x1080 and 960x540',shadows={'soft':True,'resolution':4096,'cascades':2},source_sampling='Point textures unchanged; native world pitch unchanged')
 if kind=='desk':r['configuration'].update(tone_mapping='ACES',exposure=.35,bloom={'intensity':.06,'threshold':1.5},depth_of_field='disabled on loadout working plane',main_light={'intensity':1.65,'shadow_strength':.72},material_profile={'wood_smoothness':.32,'reflection_probe':{'resolution':128,'one_shot':True,'intensity':.7,'texture_observed':True}})
 r['runtime'].update(normal_entry=True,interaction_pass=True,record='Worldbuilding/归档/2026-10-10_HD2D书桌品质样板/realism_entry_a4.json')
 r['performance']['device']='Ryzen 7 5800H / RTX 3060 Laptop GPU';r['performance']['quality_tier']='Editor Very Low, shared URP renderScale 1, soft shadows enabled'
 r['blockers']=['Human artistic approval pending','Standalone Player performance and memory samples pending; 60 FPS / P95 <=16.7ms not certified','Full motion and lighting sweeps pending','Battlefield still contains existing untextured architectural blockouts']
 r['captures']=[item('UnityProject/ReviewEvidence/20261010_hd2d_desk/'+n) for n in (['realism_bag_final_1920.png','realism_bag_960.png','realism_spell_1920.png','realism_spell_960.png'] if kind=='desk' else ['realism_battle_1920.png','realism_battle_960.png'])]
 r['notes']='Shared pipeline repair; per-loadout grade and owned probe only. No scene saved or formal artwork replaced. Quality settings Editor API experiment did not persist; startup display policy enables realtime probes in source.'
 p.write_text(json.dumps(r,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
print('Both shared-scope reports bound to current source/config hashes; status QA_PENDING.')

