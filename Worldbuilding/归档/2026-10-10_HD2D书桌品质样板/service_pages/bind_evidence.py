import hashlib
import json
from pathlib import Path

folder = Path(__file__).resolve().parent
archive = folder.parent
root = folder.parents[3]
report_path = folder / 'quality_pending.json'
report = json.loads(report_path.read_text(encoding='utf-8-sig'))
def bound(path):
    return {'path': path.relative_to(root).as_posix(), 'sha256': hashlib.sha256(path.read_bytes()).hexdigest()}
changed = root / 'UnityProject/Assets/Game/Runtime/Presentation/Hd2DServiceScene.cs'
report['changed_files'] = [changed.relative_to(root).as_posix()]
paths = [root / item['path'] for item in report['inputs']] + [changed]
paths += [root / 'UnityProject/Assets/Game/Runtime/Presentation' / name for name in (
    'Hd2DDeskArtCandidate.cs', 'Hd2DNativeMaterialMesh.cs', 'Hd2DPaperSkin.cs', 'FormalRogueliteUi.cs')]
paths += [root / 'UnityProject/Assets/Game/Resources/Art' / name for name in (
    'MainMenuE/Stage/desk.png', 'Hd2DContent20261009/student_dossier.png',
    'Hd2DContentFrames/简洁纸边.png', 'FormalAcademyStructures32/academy_prop_potion_case.png',
    'FormalAcademyStructures32/academy_prop_tool_satchel.png')]
paths += list(archive.glob('multiui_*.json'))
paths += list((root / 'UnityProject/ReviewEvidence/20261010_hd2d_desk').glob('service_*.png'))
report['inputs'] = [bound(path) for path in sorted(set(paths)) if path.is_file()]
report['status'] = 'QA_PENDING'
report['configuration']['hdr'] = True
report['configuration']['msaa'] = False
report['local_implementation'] = {
    'screens': ['event/node preview', 'workshop', 'medical', 'shop'],
    'scope': 'existing service-page presentation only; prices, consequences, saved state rules unchanged',
    'density': '32 PPU table UV recalculated after 7.8-unit widening; paper border sampling compensates each world canvas scale; decorative props use imported native source density',
    'material': 'inherits current dielectric wood desktop .32 and edge/legs .24 smoothness; lighting unchanged',
    'runtime_assets': 'archive file loading removed from service presentation; reuse imported Resources in Editor and Player; no new bitmap import, source repaint or formal promotion',
    'readability': 'cup hidden on service desk; optional props moved behind reading areas; identity, treatment, workshop target/material/preview, and shop offers use paper support; narrow strips omit rims rather than shrinking pixels',
}
report['validation_scope'] = {
    'normal': 'normal entry, locked EV1 preview, and map return via actual EventSystem; final delivery records distinguished from earlier iterations',
    'local': 'W/M/S use existing disposable PreviewUiCaptureRun (no player map save writes). Final revision actual EventSystem tab, meal, purchase and return clicks checked at 960x540; both actual resolutions captured. This is local UI evidence, not complete journey acceptance',
    'final_local_transactions': 'multiui_delivery_medical_after_meal: gold 8→5. multiui_delivery_shop_after_buy: gold 8→5 and G-T08 4/4 charges added; multiui_delivery_shop_after_buttons confirms sold-out button disabled',
    'prior_local_transactions': 'meal 8→5 gold; shop 8→5 gold and G-T08 full-charge item added, before final padding-only revision',
    'failed_attempts': 'premature clicks during delayed scene rebuild preserved; later stable clicks recorded separately, no silent conversion of failures to passes',
    'compile': 'multiui_final_compile_recovery.json Success, 2026-10-10T12:51:54.7757319+08:00',
    'console': 'multiui_delivery_console_errors: no cached errors in last 1800 seconds; not an all-time Console claim',
    'cleanup': 'multiui_delivery_restored_state: Edit Mode, correct main project, EpicToonVfxShowcase clean; verified temporary Game View size removed and original selection restored',
}
report['current_screenshots'] = [
    {'path': 'UnityProject/ReviewEvidence/20261010_hd2d_desk/' + name,
     'scope': 'normal locked node preview' if 'event' in name else 'disposable local service UI',
     'aesthetic_approval': False}
    for name in ('service_delivery_event_1920.png', 'service_delivery_event_actual_960.png',
                 'service_delivery_workshop_1920.png', 'service_delivery_workshop_spells_actual_960.png',
                 'service_delivery_medical_1920.png', 'service_delivery_medical_actual_960.png',
                 'service_delivery_shop_1920.png', 'service_delivery_shop_actual_960.png')
]
report['blockers'] = [
    '整备人物、装备、商店内容图和部分图标的源像素密度仍需逐资产复核，未宣称同场景全部一致。',
    '工坊安装成功、非满血治疗、商店完整随机学院路线、保存恢复和独立Player包尚未完成本次验收。',
    '本次没有性能测量、运动记录、全资产密度清单或人工样板批准。',
    '旧service截图含中间版本或局部临时状态；不作为当前样板批准，不继承旧输入SHA的通过结论。',
]
report_path.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print('Service UI source and evidence bound; QA_PENDING preserved.')
