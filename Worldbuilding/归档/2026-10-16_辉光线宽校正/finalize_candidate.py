from pathlib import Path
import json,hashlib,shutil,sys
sys.path.insert(0,str(Path('Tools/OCCVfx').resolve()))
import occ_vfx_pipeline as pipe
A=Path('Worldbuilding/归档/2026-10-16_辉光线宽校正');O=Path('Artifacts/SkyFurnaceLineWeight_20261016')
old=json.loads(Path('Tools/OCCVfx/recipes/sky_furnace_glow_rain_v2.json').read_text(encoding='utf-8-sig'))
r=old.copy();r['id']='sky_furnace_line_weight_v4';r['reference']=pipe.REFERENCE;r['capture_directory']=A.as_posix();r['review']={'status':'PENDING','scope':'Line weight correction. User rejected v3 apparent thickness. Preserve original source silhouette and restore original Bloom; low-energy optical halo only. Visual review pending.'}
r['optics']=dict(old['optics'],rim_energy=0,contour_energy=.16,pixel_emission_gain=1.05,halo_padding_pixels=6,halo_model='continuous_gaussian',halo_sigma_pixels=1.4,bloom_threshold=1.15,bloom_intensity=.30,bloom_scatter=.60)
r['implementation']=[pipe.binding(i['path']) for i in old['implementation']]
for key,item in r['evidence']['files'].items():
 p=item['path'].replace('2026-10-16_天穹熔炉轮廓辉光与夜雨','2026-10-16_辉光线宽校正').replace('SkyFurnaceGlowRain_20261016/天穹熔炉_辉光与夜雨.mp4','SkyFurnaceLineWeight_20261016/天穹熔炉_线宽校正夜雨.mp4')
 r['evidence']['files'][key]=pipe.binding(p)
r['evidence']['subject_sha256']=pipe.fingerprint(r)
def save(p,j):Path(p).write_text(json.dumps(j,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
save(pipe.CURRENT_RECIPE,r)
ref=json.loads(Path('Tools/OCCVfx/references/sky_furnace_glow_rain_20261016.lock.json').read_text(encoding='utf-8-sig'))
ref['id']='sky_furnace_line_weight_20261016';ref['recipe_id']=r['id'];ref['acceptance']['scope']='Return bright line footprint close to pre-glow v1. v2 hard edges and v3 thick optical overlay were rejected; v4 pending visual review.'
ref['inputs']=[pipe.binding(i['path']) for i in ref['inputs']]
ref['evidence']=list(r['evidence']['files'].values())+[pipe.binding((A/'fixed_film_capture.json').as_posix()),pipe.binding((A/'build_status.json').as_posix()),pipe.binding((O/'夜雨线宽对照.png').as_posix())]
for item in ref['inputs']+ref['evidence']:
 dest=A/'候选快照'/item['path'];dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(item['path'],dest);item['snapshot_path']=dest.as_posix()
save(pipe.REFERENCE,ref)
q={'schema':'occ-hd2d-quality-report-v1','policy_version':'2026-10-10.4','contract_sha256':pipe.digest('Tools/OCCArt/occ_art_contract_v1.json'),'scene_kind':'battlefield','baseline_id':'battlefield-v1','status':'QA_PENDING','changed_files':['UnityProject/Assets/Game/Runtime/Presentation/SkyFurnaceDirector.cs','UnityProject/Assets/Game/Resources/Shaders/SkyFurnaceContourGlow.shader','UnityProject/Assets/Game/Editor/SkyFurnaceAuthoring.cs','UnityProject/Assets/Game/Editor/SkyFurnaceVerification.cs'],'inputs':r['implementation'],'configuration':{'scope':'Local SkyFurnace optical glow correction; shared URP unchanged','optics':r['optics'],'weather':r['weather']},'local_evidence':r['evidence']['files'],'limitations':['Human aesthetic review pending.','Dedicated cinematic local verification; no formal battle integration, full scene density approval or performance baseline claimed.']}
save(A/'quality_candidate.json',q)
save(A/'local_validation.json',pipe.check_recipe(r,'runtime'))
(A/'制作记录.md').write_text('''# 柔和辉光修正

用户否决v2硬像素扩边。本次去掉亮沿能量与max膨胀色带，改为连续高斯光学扩散；本体硬像素、生长与完整几何破碎均保留。法杖参考为已有staffLight点光源及HDR Bloom的柔和光感，环境点光源保持。

本次实际捕获白天/夜雨、关增强/开增强、1920×1080/960×540。夜雨柔光对照的左侧为上轮实机、右侧为本次实机，并非两张同时从当前程序生成。正常16秒播放、暂停、重播、复位、中断、雨与解体清理已测；未抢占窗口。当前仅局部技术验证通过，真实人工审美与正式HD2D基线待确认。

光晕读取已有法阵/核心图的Alpha及生长数据，无新增美术贴图。支撑留白6像素=每侧0.1875米；连续核σ=1.4像素=0.04375米；本体与碎块尺寸和运动域未变。特效几何类别保持一像素厚纸板、一像素截面长柱和立方粒子。
''',encoding='utf-8')
(O/'使用说明.md').write_text('运行夜雨预览.cmd，或Player/SkyFurnace.exe。N切换白天/夜雨，空格重播，P暂停，R复位，Esc跳过。无音频。\n\n本次为柔和光晕候选，替换上一版硬像素扩边；像素主形和几何破碎保留。\n',encoding='utf-8')
(O/'夜雨预览.cmd').write_text('@echo off\r\nstart "" "%~dp0Player\\SkyFurnace.exe" -occ-night-rain\r\n',encoding='utf-8')
(A/'制作记录.md').write_text("# 辉光线宽校正\n\nv3仅改软边，仍保留本体发光1.65倍、强Bloom及0.85能量光层，导致视觉加粗。本次本体降为1.05倍、光层0.16，Bloom恢复加光前1.15/0.30/0.60。原图、原生厚度、模型尺寸、生长与完整几何破碎不变；退场碎粒发光4.8保留。\n\n实际对照使用v1归档06.60.png、v3同刻截图和本次截图。三栏对照下排为无缩放1×裁片；上排为同倍率全景缩略。另有同条件夜雨前后对照。当前技术验证与审美分开，仍为QA_PENDING。\n",encoding='utf-8')
(O/'使用说明.md').write_text('运行夜雨预览.cmd。N切换白天/夜雨，空格重播，P暂停，R复位，Esc跳过。无音频。\n本次恢复接近加光前的亮区宽度，保留轻柔光和带光像素破碎；视觉待确认。\n',encoding='utf-8')
print(json.dumps(pipe.check_recipe(r,'runtime'),ensure_ascii=False))
