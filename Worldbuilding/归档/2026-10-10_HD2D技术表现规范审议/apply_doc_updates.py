"""Scoped authorized HD2D mother-document update; preserve unrelated product text."""
import difflib
import html
import json
from pathlib import Path
import shutil
import subprocess
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[3]
ARCHIVE = Path(__file__).resolve().parent
DOC = "Y1KydFPjMocJjYxMmFEcTblcnLf"
CLI = shutil.which("lark-cli.cmd") or shutil.which("lark-cli")
LOCAL = ROOT / "Worldbuilding/策划案/OCC_项目总策划案_v1.0.md"


def call(name, *args):
    result = subprocess.run([CLI, "docs", *args, "--as", "user", "--format", "json"],
                            cwd=ROOT, capture_output=True, encoding="utf-8")
    (ARCHIVE / (name + ".json")).write_text(result.stdout or result.stderr, encoding="utf-8")
    data = json.loads(result.stdout or result.stderr)
    if result.returncode or not data.get("ok"):
        raise RuntimeError(f"{name} failed: see archived response; do not retry unknown writes")
    if data.get("data", {}).get("result") not in (None, "success"):
        raise RuntimeError(f"{name} incomplete: inspect response before proceeding")
    if data.get("data", {}).get("warnings"):
        raise RuntimeError(f"{name} warnings: inspect response before proceeding")
    return data["data"]["document"]


def fetch(name, keyword):
    d = call(name, "+fetch", "--doc", DOC, "--scope", "keyword", "--keyword", keyword, "--detail", "full")
    return ET.fromstring(d["content"])


def replace(name, keyword, change):
    r = fetch(name + "_before", keyword)
    matches = [e for e in r if keyword in "".join(e.itertext())]
    if len(matches) != 1:
        raise RuntimeError(f"{name}: expected exactly one block, got {len(matches)}")
    e = matches[0]
    text = change("".join(e.itertext()))
    attrs = ' done="true"' if e.tag == "checkbox" else ""
    patch = ARCHIVE / (name + ".xml")
    patch.write_text(f"<{e.tag}{attrs}>{html.escape(text)}</{e.tag}>", encoding="utf-8")
    call(name, "+update", "--doc", DOC, "--command", "block_replace", "--block-id", e.get("id"),
         "--content", "@./" + patch.relative_to(ROOT).as_posix())
    check = fetch(name + "_after", text[:20])
    if not any("".join(x.itertext()) == text for x in check):
        raise RuntimeError(name + " readback mismatch")


def main():
    if (ARCHIVE / "sync_completed.json").exists():
        raise RuntimeError("Already completed; inspect state rather than replaying writes")
    before_local = LOCAL.read_text(encoding="utf-8-sig")
    backup = ARCHIVE / "backups"
    targets = [LOCAL, ROOT / "AGENTS.md", ROOT / "Tools/OCCArt/occ_art_contract_v1.json",
               ROOT / "Worldbuilding/数据表/OCC_美术与界面规格表_v1.0.csv",
               Path("C:/Users/FNHF/.codex/skills/occ-art-direction/SKILL.md"),
               Path("C:/Users/FNHF/.codex/skills/occ-art-direction/references/occ-pixel-contract.md"),
               Path("C:/Users/FNHF/.codex/skills/game-dev-router/SKILL.md")]
    for n, path in enumerate(targets):
        dest = backup / str(n) / path.name
        dest.parent.mkdir(parents=True, exist_ok=True)
        if not dest.exists():
            shutil.copy2(path, dest)
    (backup / "index.json").write_text(json.dumps([str(x) for x in targets], ensure_ascii=False, indent=2), encoding="utf-8")
    cloud_before = json.loads((ARCHIVE / "feishu_before_full_markdown.json").read_text(encoding="utf-8-sig"))["data"]["document"]["content"]
    (ARCHIVE / "cloud_local_before.diff").write_text("".join(difflib.unified_diff(cloud_before.splitlines(True), before_local.splitlines(True), fromfile="cloud", tofile="local")), encoding="utf-8")
    replace("legacy_display_scope", "1920×1080参考画布，上部地图使用全宽", lambda t:
            "本段整帧320×180、六倍整数放大及二维绘制规则仅适用于未迁移场景；已迁移HD-2D使用A.2.4高清输出及品质门槛。源资产原生尺寸、32 PPU与逻辑占格分别遵循当前资产分类。" + t)
    replace("legacy_hdr_scope", "【美术表现线】像素显示管线第一阶段：全局像素基础守卫", lambda t:
            "【美术表现线】像素显示管线第一阶段（历史实现记录，适用于未迁移场景）：关闭MSAA与相机HDR、源纹理Point／关Mipmap／关压缩，预留固定内部分辨率与整数输出。已迁移HD-2D按A.2.4配置HDR和高清输出；历史完成标记不表示HD-2D品质验收通过。")
    replace("main_menu_asset_scope", "E 主菜单像素制作规则（用户确认，2026-10-08）", lambda t:
            "E 主菜单像素制作规则（统一口径，2026-10-10）：主菜单背景与每项物件保留独立Codex内置生图原料，不以整张绘画缩小后放大作为HD-2D制作路线。书本、杯碟、晶灯及场景陈设沿用2026-10-09战斗外HD2D素材库例外：完整主体加安全边自适应画布，允许裁透明边、Alpha处理和交付缩放，不强制64×64、固定宏像素格、1.2网格置信度、16色、硬Alpha或无缩放解码。保留像素色簇、阶梯轮廓、统一材质密度、来源与处理记录，不烘焙外部投影、光束或光晕。高清场景输出按A.2.4，不强制整场景整数显示；战场单位、地块、物件和功能图标不适用本例外。机器检查、实际接触、人工审美和正式导入分别记录。")
    # Only already-approved HD2D technical paragraphs absent from the mother are synchronized.
    prefixes = ["战斗外HD-2D内容制作补充", "HD-2D 学院庭院样板补充", "HD-2D 全项目制作口径", "房间、桌面、书本厚度与投影", "场景组合生产规则"]
    additions = [p for p in before_local.split("\n\n") if any(p.startswith(x) for x in prefixes)]
    heading = fetch("a22_latest", "A.2.2 统一像素资产生产流程")
    anchor = next(e.get("id") for e in heading if e.tag == "h3")
    current = call("cloud_before_insert", "+fetch", "--doc", DOC, "--doc-format", "markdown")["content"]
    additions = [p for p in additions if p[:30] not in current]
    if additions:
        patch = ARCHIVE / "approved_hd2d_sync.xml"
        patch.write_text("\n".join("<p>" + html.escape(p) + "</p>" for p in additions), encoding="utf-8")
        call("approved_hd2d_sync", "+update", "--doc", DOC, "--command", "block_insert_after", "--block-id", anchor,
             "--content", "@./" + patch.relative_to(ROOT).as_posix())
        fetch("approved_hd2d_sync_readback", "HD-2D 全项目制作口径")
    section = call("a23_latest", "+fetch", "--doc", DOC, "--scope", "section", "--start-block-id", "doxcnrvnGtKsT4LmoxAy45LTyTd", "--detail", "full")
    anchor = list(ET.fromstring(section["content"]))[-1].get("id")
    formal = (ARCHIVE / "HD2D正式技术条款.md").read_text(encoding="utf-8").replace("战場", "战场")
    elements = []
    for paragraph in formal.strip().split("\n\n"):
        tag = "h3" if paragraph.startswith("### ") else "p"
        text = paragraph[4:] if tag == "h3" else paragraph.replace("**", "")
        elements.append(f"<{tag}>{html.escape(text)}</{tag}>")
    patch = ARCHIVE / "quality_norm.xml"
    patch.write_text("\n".join(elements), encoding="utf-8")
    call("quality_norm", "+update", "--doc", DOC, "--command", "block_insert_after", "--block-id", anchor,
         "--content", "@./" + patch.relative_to(ROOT).as_posix())
    fetch("quality_norm_readback", "A.2.4 HD-2D 技术表现与品质门槛")
    final = call("feishu_after_full", "+fetch", "--doc", DOC, "--doc-format", "markdown")
    cloud = final["content"]
    # Sync only the authorized art ranges; preserve and report unrelated divergent gameplay.
    local = before_local
    for start, end in [("## A.2 像素与布局", "## A.3" )]:
        a, b = cloud.index(start), cloud.index(end, cloud.index(start))
        i, j = local.index(start), local.index(end, local.index(start))
        local = local[:i] + cloud[a:b] + local[j:]
    old = next(x for x in local.splitlines() if "像素显示管线第一阶段：全局像素基础守卫" in x)
    new = next(x for x in cloud.splitlines() if "像素显示管线第一阶段（历史实现记录" in x)
    local = local.replace(old, new)
    LOCAL.write_text(local, encoding="utf-8")
    (ARCHIVE / "sync_completed.json").write_text(json.dumps({"document_id": DOC, "revision": final["revision_id"],
         "scope": "A.2 through A.2.4 plus legacy guard history entry", "unrelated_divergence_preserved": True}, ensure_ascii=False, indent=2), encoding="utf-8")
    print("Mother updated and read back; scoped local art mirror synchronized. Revision", final["revision_id"])


if __name__ == "__main__":
    main()
