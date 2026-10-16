"""Apply the user's final modeling correction to the mother first, then mirrors."""
import csv
import difflib
import html
import json
from pathlib import Path
import shutil
import subprocess
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[3]
OUT = Path(__file__).resolve().parent
DOC = "Y1KydFPjMocJjYxMmFEcTblcnLf"
CLI = shutil.which("lark-cli.cmd") or shutil.which("lark-cli")
LOCAL = ROOT / "Worldbuilding/策划案/OCC_项目总策划案_v1.0.md"
RULE = "建模与贴图（用户确认，2026-10-10）：尽量少用建模，常规内容优先使用不同角度的独立像素纸板、厚纸板及分层组合。建模以简洁的多边形结构为主，主要用于场景本身和大型场景物件；每个可见模型必须配套强化2D感觉的像素贴图，贴图承担材质、色簇与主要细节，不用纯色模型、几何堆叠或高密度曲面代替像素美术。房间、地面、墙体和大型书桌可按此范围使用模型；杯碟、灯具、纸页等常规小物件优先使用独立像素纸板或厚纸板。真实相机、光照、深度和阴影服务于统一的2D像素表现。每个样板提交可见模型清单、使用理由、对应贴图与实际应用证据；无配套贴图、模型细节主导画面或与纸板画风不一致均阻断品质晋升。"


def call(name, *args):
    p = subprocess.run([CLI, "docs", *args, "--as", "user", "--format", "json"], cwd=ROOT,
                       capture_output=True, encoding="utf-8")
    (OUT / (name + ".json")).write_text(p.stdout or p.stderr, encoding="utf-8")
    r = json.loads(p.stdout or p.stderr)
    if p.returncode or not r.get("ok") or r.get("data", {}).get("result") not in (None, "success") or r.get("data", {}).get("warnings"):
        raise RuntimeError(name + ": inspect saved result before retrying")
    return r["data"]["document"]


def patch(index, keyword, transform):
    name = "modeling_" + str(index)
    saved = OUT / (name + "_after.json")
    if saved.exists():
        r = json.loads(saved.read_text(encoding="utf-8"))["data"]["document"]
        blocks = list(ET.fromstring(r["content"]))
        if len(blocks) != 1:
            raise RuntimeError("Inspect previous verified write before resume")
        return keyword, "".join(blocks[0].itertext())
    r = call(name + "_before", "+fetch", "--doc", DOC, "--scope", "keyword", "--keyword", keyword, "--detail", "full")
    blocks = [b for b in ET.fromstring(r["content"]) if keyword in "".join(b.itertext())]
    if len(blocks) != 1 or blocks[0].tag != "p":
        raise RuntimeError("Unexpected matched structure: " + keyword)
    old = "".join(blocks[0].itertext())
    new = transform(old)
    xml = OUT / (name + ".xml")
    xml.write_text("<p>" + html.escape(new) + "</p>", encoding="utf-8")
    call(name + "_write", "+update", "--doc", DOC, "--command", "block_replace", "--block-id", blocks[0].get("id"),
         "--content", "@./" + xml.relative_to(ROOT).as_posix())
    check = call(name + "_after", "+fetch", "--doc", DOC, "--scope", "keyword", "--keyword", new[:18], "--detail", "full")
    if not any("".join(b.itertext()) == new for b in ET.fromstring(check["content"])):
        raise RuntimeError("Mother readback mismatch")
    return keyword, new


def main():
    if (OUT / "modeling_sync_completed.json").exists():
        raise RuntimeError("Already completed; do not replay")
    targets = [LOCAL, ROOT / "Worldbuilding/数据表/OCC_美术与界面规格表_v1.0.csv",
               ROOT / "Tools/OCCArt/occ_art_contract_v1.json", ROOT / "Tools/OCCArt/hd2d_baselines_v1.json",
               ROOT / ".codex/skills/occ-hd2d-quality/SKILL.md", ROOT / "AGENTS.md",
               Path("C:/Users/FNHF/.codex/skills/occ-art-direction/SKILL.md")]
    before = {}
    for i, target in enumerate(targets):
        dest = OUT / "modeling_backups" / str(i) / target.name
        dest.parent.mkdir(parents=True, exist_ok=True)
        if not dest.exists():
            shutil.copy2(target, dest)
        before[target] = dest.read_text(encoding="utf-8-sig")
    updates = []
    updates.append(patch(1, "战斗外HD-2D内容制作补充", lambda t: t.replace(
        "减少建模，优先生产独立的容器、纸页、翻盖、边沿和按钮贴片，少量几何只用于承载、遮挡和光影代理，不为每个格子建立实体盒模型。",
        "尽量少用建模，优先生产独立的容器、纸页、翻盖、边沿和按钮贴片，不为每个格子建立实体盒模型。场景结构与大型场景物件可使用简洁多边形模型，但必须配套强化2D感觉的像素贴图；可见模型和代理均须与像素纸板的画风一致。")))
    updates.append(patch(2, "房间、桌面、书本厚度与投影", lambda t: t.replace(
        "房间、桌面、书本厚度与投影使用实际几何和统一3D光源；",
        "房间、桌面等场景结构与大型物件按简洁多边形模型加配套像素贴图制作，书本与小物件优先采用像素纸板、厚纸板及分层组合；厚度、投影与统一3D光源服务于2D像素感觉，不以实体建模细节代替贴图；")))
    updates.append(patch(3, "相机与真实空间。", lambda t: t + " " + RULE))
    updates.append(patch(4, "运行稳定与性能。", lambda t: t +
        " 首个书桌样板性能基准已由用户确认（2026-10-10）：当前电脑，1920×1080与960×540，目标60 FPS，P95帧时间不超过16.7ms；登记实际硬件与质量档，Editor和正式试玩包分开测量与报告，Editor结果不能代替试玩包性能验收。"))
    local = before[LOCAL]
    for keyword, new in updates:
        candidates = [p for p in local.split("\n\n") if p.startswith(keyword)]
        if len(candidates) != 1:
            raise RuntimeError("Local paragraph ambiguous: " + keyword)
        local = local.replace(candidates[0], new, 1)
    LOCAL.write_text(local, encoding="utf-8")
    csv_path = targets[1]
    rows = list(csv.reader(before[csv_path].splitlines()))
    for row in rows:
        if not row:
            continue
        if row[0] == "ART-HD2D-RUNTIME":
            row[5] = row[5].replace("真实模型和同相机投影", "场景及大型物件用简洁多边形模型与配套像素贴图；其他内容优先纸板厚纸板；同相机投影").replace("静止模型", "静止场景物件")
        elif row[0] == "ART-HD2D-CONTENT-LAYERS":
            row[5] = "尽量少用建模；优先独立像素纸板厚纸板；模型主要用于场景与大型物件且必须配套像素贴图强化2D感觉；不以纯色模型或几何堆叠代替像素美术；不以旧屏幕贴图受光网格冒充迁移；源分类审核不变；验证接触遮挡阅读与输入"
    rows.append(["ART-HD2D-MODELING-20261010", "共用表现", "建模与2D质感", "独立像素纸板厚纸板优先；简洁多边形模型主要用于场景本身及大型场景物件", "可见模型必须配套统一像素贴图；材质色簇主要细节由贴图承担", "样板须提交模型清单使用理由对应贴图及应用证据；纯色模型几何细节主导或画风不一致阻断批准；书桌可建模但不自动通过；杯碟灯具纸页优先纸板厚纸板"])
    rows.append(["ART-HD2D-DESK-PERF-20261010", "性能验收", "首个书桌样板预算", "当前电脑；1920×1080与960×540；60 FPS；P95≤16.7ms", "记录实际设备及质量档", "用户确认2026-10-10；Editor与正式试玩包分别测量；未测量不记通过"])
    with csv_path.open("w", encoding="utf-8", newline="") as f:
        csv.writer(f, lineterminator="\n").writerows(rows)
    c = json.loads(before[targets[2]])
    migration = c["hd2d_migration"]
    migration["content_production"]["authorized"] = "2026-10-10 final user correction: minimize modeling; polygonal models mainly for environments and large props; matching pixel textures reinforce 2D appearance"
    migration["content_production"]["geometry"] = "pixel cards and thick cards preferred; simple polygonal environment/large-prop models require matching pixel textures; no untextured visible primitives or modeled detail substituting for pixel art"
    migration["battlefield_sample"]["background"] = "independent pixel modules; simple polygonal environment structures paired with matching pixel textures"
    migration["quality_status"] = "QA_PENDING: connection status is not quality approval; current desk needs textured-model/card construction review"
    policy = c["hd2d_quality_policy"]
    policy["version"] = "2026-10-10.2"
    policy["construction_policy"] = {"priority": "pixel_cards_and_thick_cards", "primary_model_scope": ["environment_structure", "large_scene_prop"], "outside_primary_scope": "document exception rationale and include in bound human model/card consistency review; primarily is not a blanket ban", "model_style": "simple_polygonal", "visible_model_texture_required": True,
        "evidence": "bound visible-model inventory with rationale, geometry/material/texture inputs, application captures and genuine human review", "blockers": ["untextured_visible_model", "geometry_detail_dominates_pixel_art", "model_card_style_mismatch"]}
    policy["dimensions"].append("model_texture_2d_consistency")
    policy["required_configuration"].append("construction_manifest")
    targets[2].write_text(json.dumps(c, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    registry = json.loads(before[targets[3]])
    registry["baselines"]["desk-v1"]["reason"] = "QA_PENDING: current stage source contains untextured visible primitive furniture and modeled cup; rebuild/review card and textured polygon construction before sample promotion."
    registry["baselines"]["desk-v1"]["performance_budget"] = {"device": "current_computer_2026-10-10", "quality_tier": "pending_actual_configuration", "approved_by": "user", "approved_at": "2026-10-10", "resolutions": {r: {"target_fps": 60, "p95_budget_ms": 16.7} for r in ["1920x1080", "960x540"]}, "measurement_policy": "record actual hardware and tier; Editor and standalone Player separately; current labels require measured device identity before promotion"}
    targets[3].write_text(json.dumps(registry, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    for path in [targets[4], targets[6]]:
        text = before[path]
        text += "\n\nOCC modeling policy (final user correction, 2026-10-10): minimize modeling; prefer pixel cards/thick cards. Simple polygonal models mainly serve environments and large scene props, and every visible model needs matching pixel textures reinforcing the 2D style. Before promoting a sample, bind a construction manifest listing model rationale, geometry/material/texture inputs and application evidence; review model/card consistency. A table is not rejected merely for being modeled; untextured visible primitives or geometry-driven detail are quality failures.\n"
        path.write_text(text, encoding="utf-8")
    targets[5].write_text(before[targets[5]] + "\n- **HD-2D建模口径（用户最终修正，2026-10-10）**：尽量少用建模，优先不同角度的像素纸板和厚纸板；模型以简洁多边形为主，主要用于场景本身与大型场景物件，且必须配套像素贴图强化2D感觉。书桌可在此范围建模，小物件优先纸板；可见模型清单、用途、贴图及应用效果纳入A.2.4验收，不把建模本身判错，也不以纯色模型或几何堆叠代替像素美术。\n", encoding="utf-8")
    diffs = []
    for path in targets:
        diffs.extend(difflib.unified_diff(before[path].splitlines(True), path.read_text(encoding="utf-8").splitlines(True), fromfile=str(path)+":before", tofile=str(path)+":after"))
    (OUT / "modeling_policy.diff").write_text("".join(diffs), encoding="utf-8")
    (OUT / "modeling_sync_completed.json").write_text(json.dumps({"mother_readback_verified": True, "paragraphs": len(updates), "final_user_correction": RULE, "runtime_changed": False, "scene_saved": False}, ensure_ascii=False, indent=2), encoding="utf-8")
    print("Mother readback and scoped mirrors complete; sample remains QA_PENDING.")


if __name__ == "__main__":
    main()
