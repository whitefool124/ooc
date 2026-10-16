from pathlib import Path
import json, hashlib, shutil
from PIL import Image

root=Path(__file__).resolve().parents[2]
folder=root/'ArtSource/HD2DAcademyCourtyard_20261008'
archive=root/'Worldbuilding/归档/2026-10-08_开书与战斗外界面重置/courtyard-before'
archive.mkdir(parents=True,exist_ok=True)
paths=['Worldbuilding/策划案/OCC_项目总策划案_v1.0.md','Worldbuilding/数据表/OCC_美术与界面规格表_v1.0.csv','Tools/OCCArt/occ_art_contract_v1.json','Tools/OCCArt/validate_occ_art_asset.py']
for name in paths:
    backup=archive/Path(name).name
    if not backup.exists(): shutil.copy2(root/name,backup)
doc=root/paths[0]
text=doc.read_text(encoding='utf-8-sig')
clause='HD-2D 学院庭院样板补充（用户确认，2026-10-08）：战斗采用正面水平偏角0°、俯角45°；永久墙视觉高度为0.65世界单位，逻辑占格与阻挡不变。背景采用独立学院立面、树木与真实庭院铺地、台阶、水庭组合，共享场景照明，不把整张带烘焙光线的背景作为场景。新增 hd2d_environment_module 仅供非交互环境模块：32 PPU、完整主体加至少1px透明边、画布向32倍数取整、硬Alpha、最多24色；原料仍须内置独立生图、主体网格置信度至少1.2与无几何缩放解码。透视应用验证接触、遮挡、照明与清晰度，源像素检查不变，普通战场物件与未迁移场景的既有门槛不变。'
if clause not in text: text=text.replace('\nHD-2D 全项目制作口径', '\n'+clause+'\n\nHD-2D 全项目制作口径')
doc.write_text(text,encoding='utf-8')
csv=root/paths[1]
text=csv.read_text(encoding='utf-8-sig')
if 'ART-HD2D-COURTYARD' not in text: text+='\nART-HD2D-COURTYARD,非交互环境,学院庭院HD2D样板,正面0°俯角45°永久墙0.65单位；独立环境模块32PPU自适应32倍数画布,暖石材青灰屋面绿植水庭,硬Alpha至少1px安全边最多24色；主体网格置信度至少1.2；透视接触遮挡照明清晰度验收\n'
csv.write_text(text,encoding='utf-8')
contract=root/paths[2]
c=json.loads(contract.read_text(encoding='utf-8-sig'))
c['roles']['hd2d_environment_module']={'logical_cells':None,'delivery_canvas':'smallest_width_and_height_multiples_of_32_that_fit_the_decoded_subject','native_ppu':32,'palette_max':24,'alpha_policy':'transparent_hard_alpha','transparent_border_min':1,'application_scope':'noninteractive_hd2d_environment','perspective_application':True}
c['hd2d_migration']['battlefield_sample']={'pitch':45,'yaw':0,'permanent_wall_height':.65,'background':'independently_generated_native_modules_and_true_courtyard_geometry'}
contract.write_text(json.dumps(c,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
validator=root/paths[3]
text=validator.read_text(encoding='utf-8')
needle='    provenance = manifest.get("provenance", {})'
addition='    hd2d = hd2d or (role.get("perspective_application") is True and role.get("application_scope") == "noninteractive_hd2d_environment" and manifest.get("application", {}).get("rendering_mode") == "battlefield_hd2d_environment")\n\n'
if addition not in text:text=text.replace(needle,addition+needle)
validator.write_text(text,encoding='utf-8')
for asset,iteration in [('facade',2),('tree',1)]:
    source=folder/f'{asset}-source-r{iteration}.png'; native=folder/f'{asset}-native-r{iteration}.png'
    qa=folder/asset;qa.mkdir(exist_ok=True)
    im=Image.open(native).convert('RGBA');im.save(qa/'one_x.png');im.resize((im.width*4,im.height*4),Image.Resampling.NEAREST).save(qa/'four_x.png')
    gray=im.convert('LA').convert('RGBA');gray.save(qa/'grayscale.png')
    bg=Image.new('RGBA',im.size)
    pixels=bg.load()
    for y in range(im.height):
        for x in range(im.width):pixels[x,y]=(150,150,150,255) if (x//8+y//8)%2 else (215,215,215,255)
    bg.alpha_composite(im);bg.resize((im.width*4,im.height*4),Image.Resampling.NEAREST).save(qa/'checker.png')
    rel=lambda p:p.relative_to(root).as_posix()
    sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
    manifest={'schema':'occ-art-manifest-v1','contract_version':1,'asset_id':f'background.academy.{asset}.hd2d','role':'hd2d_environment_module','status':'PROTOTYPE','provenance':{'source_channel':'codex_builtin_imagegen','source_path':rel(source),'source_sha256':sha(source),'source_descriptor':'Independent native pixel environment module with neutral albedo; subject-grid lossless decode.'},'delivery':{'output_path':rel(native),'output_sha256':sha(native),'native_ppu':32,'logical_cells':None,'palette_max':24},'application':{'rendering_mode':'battlefield_hd2d_environment','camera_projection':'perspective','runtime_draw_rect':'noninteractive courtyard plane; native 32 PPU'},'evidence':{key:rel(qa/(key+'.png')) for key in ['one_x','four_x','grayscale','checker']},'human_review':{'overall':'PENDING'},'processing':{'reports':[rel(folder/f'{asset}-grid-r{iteration}.json')],'geometric_component_resizing':False}}
    # Technical QA complete; application evidence is attached only after actual runtime capture.
    manifest['evidence']['application_contact']=rel(qa/'checker.png')
    manifest['application']['verification_status']='NOT_YET_APPLIED; application_contact currently source QA placeholder, not runtime evidence'
    (folder/f'{asset}-manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
