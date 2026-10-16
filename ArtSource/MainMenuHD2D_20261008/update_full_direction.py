import csv
import json
import shutil
from pathlib import Path

root=Path(__file__).resolve().parents[2]
paths=['Worldbuilding/策划案/OCC_项目总策划案_v1.0.md','Worldbuilding/数据表/OCC_美术与界面规格表_v1.0.csv','Tools/OCCArt/occ_art_contract_v1.json','Tools/OCCArt/native32_generation_profiles.json']
archive=root/'Worldbuilding/归档/2026-10-08_开书与战斗外界面重置/HD2D全盘切换前'
archive.mkdir(parents=True,exist_ok=True)
for name in paths:
    backup=archive/Path(name).name
    if not backup.exists():shutil.copy2(root/name,backup)
path=root/paths[0]
text=path.read_text(encoding='utf-8-sig').replace('| 画面 | 正交 2D 像素 |','| 画面 | HD-2D：像素主体＋立体场景＋统一真实光影；全项目分阶段迁移 |')
old=next(line for line in text.splitlines() if line.startswith('HD-2D 主菜单制作口径'))
new='''HD-2D 全项目制作口径（用户确认，2026-10-08）：开场主界面、全部战斗外界面、学院地图及战斗场景统一迁移至像素主体与真实3D空间结合的HD-2D方向。此前仅主菜单的HD-2D范围扩展为全项目目标；保留E窗边书桌、独立物件组合、桌面开书流程，以及所有既定玩法、页面内容和存档结果。主菜单与战斗外书桌采用固定俯视斜角、长焦弱透视的微缩场景构图，首个相机样板为俯角35°、水平偏角12°、纵向视野24°；开书推进保持相机轴向，不从低机位仰拍或改变物件朝向。战斗镜头另以能辨认所有格子、足点、前后遮挡和目标范围的俯视斜角制作样板，不套用侧视回合制的左右站队构图；四向相邻、射程、占格、32 PPU源资产及完整主体规格保持。战斗的地面须铺在真实水平面，单位使用直立像素面片，墙与掩体以真实高度和可控遮挡组合；先完成地面投影、射线选格、单位点击、范围提示及反馈位置的同一相机验证，再迁移正式战斗入口。学院路线图保留既定节点与进入操作，信息页面保留清晰的1920×1080二维文字与控件，HD-2D用于其场景承载、物件与动画，不把正文投影为难读的3D文字。

房间、桌面、书本厚度与投影使用实际几何和统一3D光源；像素物件保留独立原料、原生像素、Point采样和现行资产画布，不重采样原料。高清场景输出允许克制景深、发光和环境动态，主要可交互内容保持清晰。景深和光照与场景相机绑定；同时打开的页面必须隔离渲染层、光源影响层和Volume层，隐藏页面释放离屏场景。源资产网格、Alpha、调色板、Importer、原生主体和人工审美门槛不降低。已迁移的场景不再要求整帧等距像素或固定六倍网格，以实际相机中的清晰度、接触、遮挡、一致光照及操作命中验证取代；未迁移场景保留原有检查，不用新口径绕过旧实现的失败。此口径优先于旧有整场景整数显示及仅主菜单例外的表述。主菜单和共用战斗外书桌先建立可玩样板；学院地图与战斗的资产、输入和反馈迁移未完成前，不报告全项目转换完成。'''
text=text.replace(old,new)
path.write_text(text,encoding='utf-8')
path=root/paths[1]
text=path.read_text(encoding='utf-8-sig')
text=text.replace('主菜单用透视3D空间和高清输出','主菜单与战斗外共用书桌用35度俯角、12度水平偏角、24度纵向视野的长焦3D空间和高清输出')
path.write_text(text,encoding='utf-8')
path=root/paths[2]
contract=json.loads(path.read_text(encoding='utf-8-sig'))
contract['screen_layouts']['main_menu']['camera_rig']={'pitch':35,'yaw':12,'vertical_fov':24,'framing':'diorama_long_lens','fixed_orientation_during_book_transition':True}
contract['hd2d_migration']={
    'authorized':'2026-10-08 user: whole project, pay attention to Octopath-style viewpoint',
    'scope':['opening_main_menu','noncombat_pages','academy_map','battlefield'],
    'source_asset_rules_unchanged':True,'gameplay_grid_unchanged':True,
    'implementation_status':{'opening_main_menu':'camera_revision_in_validation','noncombat_desk_backdrops':'shared_stage_in_validation','academy_map':'pending','battlefield':'pending_projection_input_and_visual_sample'},
    'gate':'Only implemented HD2D scenes use application validation; unmigrated battlefield retains existing lattice validation.'}
path.write_text(json.dumps(contract,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
path=root/paths[3]
profiles=json.loads(path.read_text(encoding='utf-8-sig'))
profiles['hd2d_project_direction']={'scope':'whole project migration','source_rules_unchanged':True,'prototype_camera':{'pitch':35,'yaw':12,'vertical_fov':24},'no_claim_of_completed_battlefield_migration':True}
path.write_text(json.dumps(profiles,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('Full-project target and migration boundaries synchronized; pre-change files backed up.')
