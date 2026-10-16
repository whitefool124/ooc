"""Install authorized local HD2D quality routing and machine mirror."""
import csv
import json
from pathlib import Path
import shutil

ROOT = Path(__file__).resolve().parents[3]
ARCHIVE = Path(__file__).resolve().parent
RUNTIME = "UnityProject/Assets/Game/Runtime/Presentation/"
COMMON = ["Worldbuilding/策划案/OCC_项目总策划案_v1.0.md",
          "Tools/OCCArt/hd2d_baselines_v1.json", RUNTIME + "Hd2DViewProfile.cs",
          RUNTIME + "PixelPerfectDisplayGuard.cs"]


def backup(path, label):
    target = ARCHIVE / "backups" / label / path.name
    target.parent.mkdir(parents=True, exist_ok=True)
    if not target.exists():
        shutil.copy2(path, target)


def main():
    contract_path = ROOT / "Tools/OCCArt/occ_art_contract_v1.json"
    d = json.loads(contract_path.read_text(encoding="utf-8-sig"))
    d["revision_date"] = "2026-10-10"
    d["revision_note"] += "; 2026-10-10 user authorized HD2D rule unification and durable quality gates; sample approvals remain pending."
    policy = {
        "schema": "occ-hd2d-quality-policy-v1", "version": "2026-10-10.1",
        "canonical_section": "A.2.4 HD-2D 技术表现与品质门槛",
        "baseline_registry": "Tools/OCCArt/hd2d_baselines_v1.json",
        "validator": "Tools/OCCArt/validate_hd2d_quality.py",
        "report_generator": "Tools/OCCArt/create_hd2d_quality_report.py",
        "skill": ".codex/skills/occ-hd2d-quality/SKILL.md",
        "resolutions": [[1920, 1080], [960, 540]],
        "dimensions": ["space_contact", "material_consistency", "lighting_integration", "reading_input", "motion_stability", "reference_direction"],
        "shared_changes_require": ["desk", "battlefield"],
        "statuses": ["QA_PENDING", "APPROVED_BASELINE"],
        "evidence_policy": "current bound implementation/assets plus captures, runtime, target-device budgets and genuine human review; machine integrity is not aesthetic proof",
        "profiles": {
            "desk": {
                "required_inputs": COMMON + [RUNTIME + "MainMenuHd2DStage.cs", RUNTIME + "Hd2DLoadoutScene.cs",
                    "UnityProject/Assets/Game/Resources/Art/MainMenuE/Hd2D/HD2DAtmosphere.asset"],
                "locked_parameters": {"pitch": 35, "yaw": 0, "field_of_view": 24, "hdr": True},
                "static_cases": ["default", "no_post", "lamp_0", "lamp_40", "lamp_100", "reading_contact"],
                "motion_cases": ["normal_entry_interaction_return", "camera_focus_resize_page_switch"]
            },
            "battlefield": {
                "required_inputs": COMMON + [RUNTIME + "Hd2DBattlefieldSample.cs", RUNTIME + "Hd2DBattlefieldTactics.cs",
                    RUNTIME + "Hd2DAcademyEnvironment.cs"],
                "locked_parameters": {"pitch": 45, "yaw": 0, "wall_height": 0.65, "unit_ground_angle": 45, "hdr": True},
                "static_cases": ["default", "no_post", "overview", "closest_legal_zoom", "wall_contact_occlusion", "range_ground", "strongest_existing_emission"],
                "motion_cases": ["normal_entry_action_result_return", "camera_pan_zoom_resize"]
            }
        }
    }
    d["hd2d_quality_policy"] = policy
    d["battlefield_display_policy"]["application_scope"] = "unmigrated_legacy_pixel_scenes_only; migrated_hd2d uses hd2d_quality_policy"
    d["primary_map_reference"]["review_policy"] = "Native source review retains role rules; fixed 6x output review applies only to unmigrated scenes. Migrated HD2D review uses hd2d_quality_policy."
    d["hd2d_migration"]["quality_status"] = "QA_PENDING: implementation_status is historical connection status, not approved visual quality"
    d["hd2d_migration"]["source_asset_rules_unchanged"] = "battlefield source rules retained; noncombat_hd2d library uses authorized 2026-10-09 exception"
    d["hd2d_migration"]["content_production"]["acceptance"] = "A.2.4 scene quality gate plus role-specific source QA; noncombat library exception retained"
    d["unified_pixel_asset_pipeline"]["hd2d_quality_reference"] = "hd2d_quality_policy; noncombat library exception takes precedence over generic strict source decode rules"
    contract_path.write_text(json.dumps(d, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    table = ROOT / "Worldbuilding/数据表/OCC_美术与界面规格表_v1.0.csv"
    with table.open(encoding="utf-8-sig", newline="") as file:
        rows = list(csv.reader(file))
    for row in rows[1:]:
        if row and row[0] == "ART-UI-MAIN-MENU":
            row[3] = "1920×1080高清HD-2D；35度俯角／0度水平偏角／24度纵向视野；战斗外素材自适应画布"
            row[5] = "按总案A.2.2素材库例外与A.2.4品质门槛；独立内置生图和来源记录；允许Alpha处理及交付缩放；不强制64×64、1.2网格、固定色数或整帧整数放大；空间接触、阅读和命中须验收"
        if row and row[0] == "ART-HD2D-RUNTIME":
            row[5] += "；接入状态不等于品质批准，APPROVED_BASELINE须通过A.2.4当前证据与人工审美"
    rows.append(["ART-HD2D-QUALITY-20261010", "共用表现", "HD2D正式技术品质门槛", "总案A.2.4；书桌与战斗两套批准样板；1920×1080和960×540", "共用配置／版本／文件指纹／静态及运动证据／目标设备预算／人工审美", "共享渲染变更验收两套样板；局部改动验证受影响配置；旧证据指纹改变即失效；阻断项不得平均抵消；现有样板QA_PENDING"])
    with table.open("w", encoding="utf-8", newline="") as file:
        csv.writer(file, lineterminator="\n").writerows(rows)

    agents = ROOT / "AGENTS.md"
    agents.write_text(agents.read_text(encoding="utf-8-sig") + "\n\n## HD-2D 稳定品质执行（2026-10-10 用户授权）\n\n"
        "- 涉及OCC相机、受光材质、光影、后处理、输出采样、场景资产或表现验收时，读取 [.codex/skills/occ-hd2d-quality/SKILL.md](.codex/skills/occ-hd2d-quality/SKILL.md)，按总案A.2.4执行受影响范围的品质门槛。純文档维护只核对规范及引用，不启动Unity。\n"
        "- `Tools/OCCArt/hd2d_baselines_v1.json`只登记批准状态与配置引用，不是第二策划源；QA_PENDING不能充当批准样板。共享渲染变更验证书桌与战斗，局部改动仅验证受影响配置。\n"
        "- 交付前运行 `python Tools/OCCArt/validate_hd2d_quality.py <当前证据报告.json>`；缺证据、指纹改变、关键失败、未获真实人工审美均阻断APPROVED_BASELINE。报告绑定本次改变的Prefab、Shader、代码、配置及资产。机器通过不能替代实机与人工。\n"
        "- 固定320×180／六倍整帧和全局关闭HDR只用于未迁移像素场景；已迁移HD-2D按A.2.4；战斗外素材库按2026-10-09例外。后补适用范围优先于旧通用步骤，不能降低战场源资产门槛。\n"
        "- 品质回退保留上一批准配置，关键失败先修原因，不批量扩展候选。现有接入记录不证明新规范验收通过；首套样板需真实正常流程、运动、性能预算与人工审核。\n", encoding="utf-8")
    unity_agents = ROOT / "UnityProject/AGENTS.md"
    backup(unity_agents, "unity_agents")
    unity_agents.write_text(unity_agents.read_text(encoding="utf-8-sig") + "\n\n## OCC HD-2D 品质入口\n\n"
        "涉及场景表现、相机、材质、Shader、光照或后处理时，读取仓库根目录 `.codex/skills/occ-hd2d-quality/SKILL.md`，遵循总案A.2.4及根AGENTS的品质门槛。源Point采样与高清输出分开；旧像素守卫不得全局覆盖HD-2D相机HDR。共享改动验证书桌与战斗；配置或源文件变化使受影响旧证据失效。未完成真正样板批准时保持QA_PENDING。纯规范维护不启动Editor。\n", encoding="utf-8")
    art_skill = Path("C:/Users/FNHF/.codex/skills/occ-art-direction/SKILL.md")
    text = art_skill.read_text(encoding="utf-8-sig")
    text = text.replace("## Workflow", "## HD-2D applicability\n\nFor migrated OCC HD2D scene work, read the project's `.codex/skills/occ-hd2d-quality/SKILL.md` and A.2.4. Noncombat HD2D text carriers and props use the authorized 2026-10-09 adaptive library exception: independent built-in generation, visible clusters/stepped silhouettes and provenance; alpha processing and delivery resizing allowed; no mandatory 1.2 lattice confidence, fixed palette or hard-alpha decode. The strict production steps below apply to battlefield assets and functional icons, not this library. Source lattice and scene high-resolution output are separate.\n\n## Workflow")
    art_skill.write_text(text, encoding="utf-8")
    art_ref = Path("C:/Users/FNHF/.codex/skills/occ-art-direction/references/occ-pixel-contract.md")
    art_ref.write_text("适用范围补充（2026-10-10）：以下严格网格、色数、硬Alpha与无缩放步骤用于战场与功能图标。战斗外HD2D文字载体及陈设按总案A.2.2的2026-10-09素材库例外；已迁移场景高清输出与品质门槛按A.2.4及项目occ-hd2d-quality技能。不得把旧通用步骤覆盖例外。\n\n" + art_ref.read_text(encoding="utf-8-sig"), encoding="utf-8")
    router = Path("C:/Users/FNHF/.codex/skills/game-dev-router/SKILL.md")
    text = router.read_text(encoding="utf-8-sig")
    text += "\nFor OCC HD2D scene/camera/lighting/material/postprocess work only, use the project's `.codex/skills/occ-hd2d-quality/SKILL.md` alongside the affected specialty. It defines scene output versus asset role and current evidence gates; do not impose OCC values on other games.\n"
    router.write_text(text, encoding="utf-8")
    print("Installed quality policy, registry, scoped routing and specification rows")


if __name__ == "__main__":
    main()
