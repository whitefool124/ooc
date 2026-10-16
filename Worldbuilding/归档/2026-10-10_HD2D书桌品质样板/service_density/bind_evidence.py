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
changed = [root / 'UnityProject/Assets/Game/Runtime/Presentation' / n for n in ('Hd2DServiceScene.cs', 'OccItemSurface.cs')]
paths = [root / i['path'] for i in report['inputs']] + changed
paths += list(archive.glob('density2_*.json'))
paths += list((root / 'UnityProject/ReviewEvidence/20261010_hd2d_desk').glob('density2_*.png'))
paths += [root / 'UnityProject/Assets/Game/Resources/Art' / n for n in (
    'FormalArtifactIcons32/breach_wedge.png', 'FormalArtifactIcons32/phase_spindle.png',
    'FormalAcademyEquipmentIcons32/aca_eq_hd01.png', 'FormalAcademyEquipmentIcons32/aca_eq_ch01.png',
    'FormalResourceIcons12/parts.png', 'FormalResourceIcons12/contribution.png', 'FormalResourceIcons12/health.png',
    'Hd2DContentFrames/简洁纸边.png')]
report['inputs'] = [bound(p) for p in sorted(set(paths)) if p.is_file()]
report['changed_files'] = [p.relative_to(root).as_posix() for p in changed]
report['status'] = 'QA_PENDING'
report['configuration']['hdr'] = True
report['configuration']['msaa'] = False
report['local_implementation'] = {
    'target_world_pitch': 1/32,
    'scope': 'service UI image placement only; no new bitmap, re-paint, gameplay change or formal asset promotion',
    'shop': 'three native 32x32 product illustrations at 1/32 world units per texel; duplicate header icons removed. Full-density display pads and enlarged papers retain prices, effect text and purchase controls',
    'workshop': 'native 12x12 material and 32x32 selected equipment size at scene density; tabs moved above selected card. Duplicate preview icon omitted; readable description retained',
    'medical': 'native 12x12 health symbol at scene density with text inset. Shallow meal note and hot-drink button use clear text without downscaled decorative symbols',
    'small_controls': 'redundant short navigation and header gold icons omitted; text and callbacks retained',
    'material_inspection': 'existing shop hover hit footprint follows visible product; OccItemSurface.Retarget preserves animation preference',
}
report['validation_scope'] = {
    'normal_entry': 'fresh v6 scope for final entry checks. Prior occupied verification-slot failure retained; no player or occupied verification data deleted',
    'local_services': 'disposable in-memory W/M/S; shop purchase 8→5 gold and G-T08 4/4, sold disabled, actual return; workshop target selection, moved tab click and return. These are local callbacks, not full journey/save acceptance',
    'revision_policy': 'v2-v5 images retained as intermediate evidence; v4 shop/workshop interactions precede medical-only branch revision. v6 images are current final revision. No old sample approval inherited',
    'density': 'runtime sprite rectangle measurements are technical evidence only; tiled paper uses mesh UV density, not rectangle/source size. Visible source cluster uniformity and complete scene inventory still require review',
    'final_compile': 'density2_v6_compile_ready: ready 2026-10-10 13:24:21.368 +08:00, no compilation errors',
    'final_normal_entry': 'density2_v6_entry_completed: PASS:entry',
    'final_medical': 'actual 960 meal click, gold 8→5 and return checked in v6; treatment was full-health disabled, not a treatment-success test',
    'console_cleanup': 'density2_v6_console and density2_v6_final_state bind the final cache check and restoration; verified temporary size removed, original clean scene restored without save',
    'console_limitation': 'final 900-second cache contains one QuickInstaller RuntimeAssembly.CodeBase path encoding ExecutionEngineException. Script compilation succeeded; Console is not error-free, plugin issue not fixed this turn',
}
report['current_screenshots'] = [bound(root / 'UnityProject/ReviewEvidence/20261010_hd2d_desk' / name)
    for name in ('density2_v6_shop_1920.png', 'density2_v6_shop_actual_960.png',
                 'density2_v6_workshop_target_1920.png', 'density2_v6_workshop_target_actual_960.png',
                 'density2_v6_medical_1920.png', 'density2_v6_medical_actual_960.png')]
report['blockers'] = [
    '完整场景（背景、其他人物、节点图标等）的像素密度清单与人工同框复核未完成；本轮只修复服务页已观察到的缩小图标。',
    '正常完整服务路线、工坊实际安装、非满血治疗及保存恢复未完成本轮验收。',
    '没有本轮独立 Player 试玩包、性能与运动记录，没有人工美术样板批准。',
    '最终Console有一条Editor QuickInstaller路径编码异常，未本轮修复；不能将编译通过写成Console全清。',
]
report_path.write_text(json.dumps(report, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
print('Current source/evidence bound; QA_PENDING remains.')
