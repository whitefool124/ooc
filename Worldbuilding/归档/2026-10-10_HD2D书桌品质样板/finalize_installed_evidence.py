import hashlib
import json
from pathlib import Path

root = Path(__file__).resolve().parents[3]
archive = Path(__file__).resolve().parent
report_path = archive / 'installed_native_quality_pending.json'
report = json.loads(report_path.read_text(encoding='utf-8-sig'))
changed = [
    'UnityProject/Assets/Game/Runtime/Presentation/MainMenuHd2DStage.cs',
    'UnityProject/Assets/Game/Runtime/Presentation/Hd2DDeskArtCandidate.cs',
    'UnityProject/Assets/Game/Runtime/Presentation/Hd2DNativeMaterialMesh.cs',
    'UnityProject/Assets/Game/Runtime/Presentation/Hd2DLoadoutScene.cs',
    'UnityProject/Assets/Game/Runtime/Presentation/Hd2DLoadoutScene.Art.cs',
    'UnityProject/Assets/Game/Runtime/Presentation/Hd2DPaperSkin.cs',
    'UnityProject/Assets/Game/Editor/AcademyJourneyAudit.cs',
]
def bound(path):
    return {'path': path, 'sha256': hashlib.sha256((root / path).read_bytes()).hexdigest()}
report['changed_files'] = [bound(path) for path in changed]
paths = {item['path'] for item in report['inputs']} | set(changed)
for path in archive.glob('implementation_*.json'):
    paths.add(path.relative_to(root).as_posix())
screens = root / 'UnityProject/ReviewEvidence/20261010_hd2d_desk'
for path in screens.glob('installed_native_*.png'):
    paths.add(path.relative_to(root).as_posix())
report['inputs'] = [bound(path) for path in sorted(paths)]
report['status'] = 'QA_PENDING'
report['blockers'] = [
    '整备页人物、装备、资源图标及档案册的同场景像素尺度尚未完成统一与源网格审核，禁止判定整场景品质通过。',
    '本次接入复用 Unity 已导入原料；新生成 r4 杯碟仍在 ArtSource，未绕过先前 File.Copy 审批拒绝导入。',
    '未完成全资产密度 manifest、运动记录及人工样板认可。',
    '本次未测量 Editor 或 Player 性能；60 FPS / P95 16.7 ms 仍为验收目标。',
]
report['implementation_validation'] = {
    'compile': 'Unity 重编译恢复状态 Success，2026-10-10T11:45:05.3552872+08:00',
    'normal_entry_audit': 'PASS:entry，implementation_auditfix_status.json',
    'manual_normal_ui_path': '主菜单→角色确认→学院入口→不需要教程→学院地图→背包→术式编组→火花详情→装备页→返回地图；实际 EventSystem 点击命中记录在 implementation_*.json',
    'resolutions': '1920×1080 与实际 960×540 GameView / Screen / render target，绑定图片及尺寸读回记录',
    'console': 'implementation_final_console.json 本轮最近 600 秒无错误；退出后的存档保护检查另见 implementation_post_restore_console.json',
    'persistent_hook': 'MainMenuHd2DStage.Build 自动 Apply，不依赖 BeginPreview；两个独立 Play 会话验证',
    'limits': '未测试装备拖拽、更换术式、Player 构建、性能或整场景人工认可；纹理 texel 间距测量不能单独证明作者宏像素网格一致',
}
report_path.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print('Bound source and runtime evidence; report remains QA_PENDING.')
