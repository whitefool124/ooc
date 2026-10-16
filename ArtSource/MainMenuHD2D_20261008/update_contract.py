from pathlib import Path
import json,shutil
root=Path(__file__).resolve().parents[2]
archive=root/'Worldbuilding/归档/2026-10-08_开书与战斗外界面重置/HD2D切换前'
archive.mkdir(parents=True,exist_ok=True)
paths=['Worldbuilding/策划案/OCC_项目总策划案_v1.0.md','Worldbuilding/数据表/OCC_美术与界面规格表_v1.0.csv','Tools/OCCArt/occ_art_contract_v1.json','Tools/OCCArt/native32_generation_profiles.json']
for p in paths:
    target=archive/Path(p).name
    if not target.exists():shutil.copy2(root/p,target)
md=root/paths[0];text=md.read_text(encoding='utf-8-sig')
line='HD-2D 主菜单制作口径（用户确认，2026-10-08）：开场主界面改用像素资产与真实3D空间结合的HD-2D表现，保留E窗边书桌、独立物件组合和桌面开书流程。房间、桌面、书本厚度与投影使用实际几何和统一3D光源；像素物件保留独立原料、原生像素、Point采样和现行资产画布，不为摆进场景重采样原料。主菜单使用透视镜头与高清场景输出，可用克制的景深、发光和环境动态；近远距离的屏幕像素大小随透视变化，不再将整场景320×180／固定6倍整数网格作为该主菜单的表现或入库条件。机器验证改为源资产网格、Alpha、调色板、Importer和实际相机中的清晰度、接触、遮挡及一致光照；人工审美门槛保留。此条优先于下文E主菜单旧有整场景整数显示要求，仅改变开场主界面表现，不改战斗原生32像素格与其他页面口径。\n\n'
marker='E 主菜单像素制作规则（用户确认，2026-10-08）：'
if 'HD-2D 主菜单制作口径' not in text:text=text.replace(marker,line+marker)
md.write_text(text,encoding='utf-8')
csv=root/paths[1];text=csv.read_text(encoding='utf-8-sig')
text=text.replace('E窗边书桌场景与左侧小型纸页菜单','HD-2D的E窗边书桌场景与左侧小型纸页菜单')
text=text.replace('背景和物件共用像素密度与整数显示倍率','源资产保留原生像素与Point采样；主菜单用透视3D空间和高清输出；不强制整场景320×180或6倍整数网格；统一实际光照投影与景深')
csv.write_text(text,encoding='utf-8')
contract=root/paths[2];c=json.loads(contract.read_text(encoding='utf-8'))
m=c['screen_layouts']['main_menu'];m.update({'scene_direction':'HD2D_E_window_side_desk','production_route':'independent_pixel_materials_composed_in_real_3d_space','background_scale':1,'scene_grid':None,'artwork_and_atmosphere_share_grid':False,'rendering_mode':'main_menu_hd2d','scene_output':[1920,1080],'camera_projection':'perspective','source_texture_filter':'Point','screen_pixel_density':'perspective_dependent','runtime_validation':'source_grid_importer_contact_occlusion_lighting_clarity','authorized':'2026-10-08 user: use HD-2D like Octopath Traveler'})
c['formal_requirements']['main_menu_hd2d_exception']={'scope':'main_menu_hd2d only','replaces':'uniform whole-frame six-times lattice proof','requires':['source_grid_pass','Point_importer','native_source_preserved','camera_contact_occlusion_lighting_review','human_aesthetic_acceptance'],'not_applicable_to':'battlefield or other screens'}
contract.write_text(json.dumps(c,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
profiles=root/paths[3];c=json.loads(profiles.read_text(encoding='utf-8'))
c['main_menu_hd2d_display_exception']={'authorized':'2026-10-08','source_grid_confidence_min':1.2,'source_canvas_rules_unchanged':True,'source_geometric_resizing':False,'texture_filter':'Point','scene_output':[1920,1080],'projection':'perspective','whole_scene_integer_scale_required':False,'scope':'E main menu only'}
profiles.write_text(json.dumps(c,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('Main-menu HD2D scope synchronized; original files backed up')
