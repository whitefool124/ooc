"""Reproducible QA packaging; crop transparent margins only, never draw source art."""
import copy
import hashlib
import json
from pathlib import Path
from PIL import Image, ImageOps

folder=Path(__file__).resolve().parent
root=folder.parents[2]
template=json.loads((root/'Tools/OCCArt/occ_art_manifest_template_v1.json').read_text(encoding='utf-8-sig'))
items=[('背包',2,'hd2d_open_equipment_case',16),('档案册',3,'hd2d_student_dossier',12),('按钮',1,'hd2d_enamel_action_plaque',12)]
def rel(path):return path.relative_to(root).as_posix()
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
for name,version,asset,palette in items:
    directory=folder/name
    qa=json.loads((directory/f'qa-{version:02}.json').read_text(encoding='utf-8-sig'))
    assert qa['status']=='PASS_LOSSLESS_TARGET',qa['status']
    source=directory/f'source-{version:02}.png'
    native=Image.open(directory/f'native-{version:02}.png').convert('RGBA')
    bounds=native.getchannel('A').getbbox()
    native=native.crop((bounds[0]-1,bounds[1]-1,bounds[2]+1,bounds[3]+1))
    delivery=directory/'delivery.png';native.save(delivery)
    enlarged=native.resize((native.width*4,native.height*4),Image.Resampling.NEAREST)
    enlarged.save(directory/'4x.png')
    grey=ImageOps.grayscale(native).convert('RGBA');grey.putalpha(native.getchannel('A'));grey.save(directory/'gray.png')
    checker=Image.new('RGBA',enlarged.size)
    for y in range(checker.height):
        for x in range(checker.width):checker.putpixel((x,y),(220,220,220,255) if (x//16+y//16)%2 else (125,125,125,255))
    checker.alpha_composite(enlarged);checker.save(directory/'checker.png')
    manifest=copy.deepcopy(template)
    manifest.update(asset_id=asset,role='ui_detail_adaptive',status='QA_PENDING')
    manifest['provenance'].update(source_path=rel(source),source_sha256=sha(source),source_descriptor='Independent Codex built-in imagegen source; rejected variants and diagnosis retained alongside selected source')
    manifest['delivery'].update(output_path=rel(delivery),output_sha256=sha(delivery),native_output_path=rel(delivery),native_ppu=32,palette_max=palette,logical_cells=None)
    manifest['display'].update(canonical_integer_scale=1,screen_px_per_native_px_at_canonical=None,integer_only=False,fractional_scale_forbidden=False,note='Native raster is unchanged; perspective scene application is reviewed for support, depth, lighting and clarity.')
    manifest['application'].update(runtime_draw_rect='Independent scene sprite on the equipment tabletop, 32 PPU; full silhouette and bottom contact',rendering_mode='noncombat_hd2d_layered_sprite',camera_projection='perspective',default_integer_scale=1,minimum_integer_scale=1)
    manifest['placement_semantics']={'asset_role':'noncombat_content_object','application_scope':'runtime_layout','logical_cell':None,'logical_cells':None,'real_dimensions_m':[native.width/32,native.height/32], 'mount_type':'tabletop','native_anchor':[native.width//2,native.height-2], 'visible_contact_line':{'axis':'y','native_px':native.height-2},'depth_sort_baseline':native.height-2,'support_asset':'E shared desk tabletop','screen_axis':'horizontal_vertical_locked','allowed_occlusion_direction':'front_to_back_scene_depth','material_palette_family':'warm leather paper brass and desaturated teal','light_direction':'upper_left_form_shading_runtime_scene_illumination','combined_room_shell':False}
    contact=root/'UnityProject/ReviewEvidence/MainMenuE/hd2d-native-asset-contact-20261009.png'
    manifest['evidence'].update(one_x=rel(delivery),four_x=rel(directory/'4x.png'),grayscale=rel(directory/'gray.png'),checker=rel(directory/'checker.png'),application_contact=rel(contact) if contact.exists() else None)
    manifest['human_review']['notes']='Native decoder passed without scaling. Scene application and user aesthetic review remain pending; not a formal Unity asset.'
    manifest['decoder']=qa
    manifest_path=directory/'manifest.json'
    if manifest_path.exists():
        previous=json.loads(manifest_path.read_text(encoding='utf-8-sig'))
        # Repackaging evidence must never erase a completed human review.
        manifest['human_review']=previous['human_review']
        manifest['status']=previous['status']
        manifest['unity_import']=previous['unity_import']
        if previous['status']=='FORMAL':
            manifest['delivery']['output_path']=previous['delivery']['output_path']
            manifest['evidence']['six_x']=previous['evidence']['six_x']
    manifest_path.write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(asset, native.size, qa['subject_lattice_confidence'])
