"""Register actual local readback. Pending human review is never replaced by PASS."""
import hashlib
import json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[3];ARCHIVE=Path(__file__).resolve().parent
FAMILIES=['AcademyCourt','Conservatory','StoneRoad','RuinSite','InteriorHall','AetherTower']
def bind(path):
    return {'path':path.relative_to(ROOT).as_posix(),'sha256':hashlib.sha256(path.read_bytes()).hexdigest()}

report_path=ARCHIVE/'quality_candidate_20261012_r1.json'
report=json.loads(report_path.read_text(encoding='utf-8-sig'))
inputs={i['path']:i for i in report['inputs']}
def register(path):
    item=bind(path);inputs[item['path']]=item;return item

records=[]
for family in FAMILIES:
    folder=ARCHIVE/'profiles'/family
    live=json.loads((folder/'actual_render_configuration.json').read_text(encoding='utf-8'))
    density=json.loads((folder/'local_density_configuration.json').read_text(encoding='utf-8'))
    capture=json.loads((folder/'capture.json').read_text(encoding='utf-8'))
    cleanup=json.loads((folder/'cleanup.json').read_text(encoding='utf-8'))
    assert capture['environment']==family and not cleanup['play'] and not cleanup['dirty']
    assert not density['unresolvedColourSources'],density['unresolvedColourSources']
    sources=density['visibleTextures']
    assert all(row['sourceTexelWorldPitchMin'] is not None and row['sourceTexelWorldPitchMax'] is not None for row in sources)
    assets=[];models=[]
    placement=register(folder/'local_density_configuration.json')
    geometry_sample=register(ROOT/'UnityProject/Assets/Game/Runtime/Presentation/Hd2DBattlefieldSample.cs')
    geometry_environment=register(ROOT/'UnityProject/Assets/Game/Runtime/Presentation/Hd2DAcademyEnvironment.cs')
    for i,row in enumerate(sources):
        source=register(ROOT/row['path'])
        assert source['sha256']==row['sha256']
        identity=f'{family}_{i:03}_{row["objectName"]}'
        assets.append({'id':identity,'source':source,'placement':placement,
            'density_basis':row['sampling']+'; native source dimensions '+str([row['width'],row['height']])+'; runtime-copy identity explicitly traced',
            'source_uniformity':'PENDING','source_macro_pixel_unit':'PENDING_SOURCE_REVIEW',
            'effective_world_pitch_xy':[row['sourceTexelWorldPitchMin'],row['sourceTexelWorldPitchMax']],
            'runtime_copy_resolved':row['runtimeCopyResolved']})
        if row['construction']=='simple_polygonal_mesh':
            shader=ROOT/('UnityProject/Assets/Game/Resources/Shaders/'+{'OCC/HD2D/BakedArchitecture':'Hd2DBakedArchitecture.shader','OCC/HD2D/WetCourtyard':'Hd2DWetCourtyard.shader'}[row['shader']])
            models.append({'id':identity,'role':'environment_structure','style':'simple_polygonal',
                'rationale':'Runtime foundation, courtyard paving or combined structure; simple scene support with native pixel texture, not substitute for sprite detail',
                'geometry':geometry_sample if row['objectName']=='真实地台厚度' else geometry_environment,
                'material':register(shader),'pixel_texture':source,'runtime_inventory':placement,
                'vertices':row['vertices'],'human_model_card_consistency':'PENDING'})
    comparisons=[]
    for resolution in [[1920,1080],[960,540]]:
        path=folder/f'full_output_offscreen_{resolution[0]}x{resolution[1]}.png'
        comparison={'resolution':resolution,'capture':register(path),'sampled_asset_ids':[a['id'] for a in assets],
            'same_frame_verdict':'PENDING','projection_verdict':'PENDING','motion_verdict':'PENDING',
            'motion_record':register(folder/'timed_motion_capture.json') if (folder/'timed_motion_capture.json').exists() else None,
            'motion_scope':'Timed legal live camera path; human stability/scene-granularity review pending'}
        comparisons.append(comparison)
    density_path=folder/'pixel_density_inventory.json'
    density_manifest={'schema':'occ-hd2d-pixel-density-v1','status':'QA_PENDING','world_pixel_pitch':1/32,
        'reference_asset_id':next(a['id'] for a,row in zip(assets,sources) if row['width']==32 and row['height']==32),
        'all_visible_pixel_assets_reviewed':False,'assets':assets,'comparisons':comparisons,
        'coverage':'All current frustum-visible enabled colour-textured MeshRenderers, excluding shadow-only proxies; no active texture-free weather fixture',
        'unresolved_colour_sources':density['unresolvedColourSources'],'human_review':'PENDING'}
    density_path.write_text(json.dumps(density_manifest,ensure_ascii=False,indent=2),encoding='utf-8')
    construction_path=folder/'construction_inventory.json'
    construction_path.write_text(json.dumps({'schema':'occ-hd2d-construction-v1','status':'QA_PENDING','visible_models':models,
        'coverage':'Current visible source-textured non-quad meshes; structural source and actual readback bound',
        'inventory_completeness_human_review':'PENDING','human_model_card_consistency':'PENDING'},ensure_ascii=False,indent=2),encoding='utf-8')
    config={key:live[key] for key in ['hdr','msaa','output_policy','tone_mapping','exposure','bloom','depth_of_field','shadows','material_profile','source_sampling']}
    config['main_light']=next(l for l in live['lights'] if l['name']=='战场统一日光')
    config['fill_light']=next(l for l in live['lights'] if l['name']=='战场柔和补光')
    config['local_lights']={'lights':[l for l in live['lights'] if l['type']!='Directional'],'captured_count':sum(l['type']!='Directional' for l in live['lights'])}
    config['construction_manifest']=register(construction_path);config['pixel_density_manifest']=register(density_path)
    records.append({'family':family,'level':capture['level'],'configuration':config,'live_readback':register(folder/'actual_render_configuration.json'),
        'visible_source_entries':len(assets),'runtime_copies_resolved':sum(a['runtime_copy_resolved'] for a in assets),'visible_models':len(models),
        'source_texel_pitch_range':[density['sourceTexelWorldPitchMin'],density['sourceTexelWorldPitchMax']],
        'unresolved_colour_sources':density['unresolvedColourSources'],'human_review':'PENDING'})
summary_path=ARCHIVE/'six_family_actual_configuration.json'
summary_path.write_text(json.dumps({'schema':'occ-six-family-actual-configuration-v1','status':'QA_PENDING','profiles':records,
    'scope':'Actual local Editor readback, not approved baseline or complete pixel-granularity review'},ensure_ascii=False,indent=2),encoding='utf-8')
register(summary_path)
register(ROOT/'UnityProject/Assets/Game/Editor/Hd2DBattleConfigurationEvidence.cs')
register(ROOT/'UnityProject/Assets/Game/Editor/Hd2DBattleLocalEvidence.cs')
register(ROOT/'UnityProject/Assets/Game/Editor/Hd2DBattleMotionCapture.cs')
report['configuration'].update(records[0]['configuration'])
report['inputs']=list(inputs.values())
report['actual_family_configurations']=bind(summary_path)
report['blockers']=['Scene pixel-source granularity and human model/card review remain pending',
    'Normal-entry, continuous-motion/interaction and Player/full-frame GPU performance evidence pending',
    'Material-region alignment and overall scene aesthetic approval pending']
report_path.write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'profiles':len(records),'status':'QA_PENDING','coverage':[{'family':r['family'],'visible_sources':r['visible_source_entries'],'models':r['visible_models'],'resolved_copies':r['runtime_copies_resolved'],'pitch_range':r['source_texel_pitch_range']} for r in records]}))
