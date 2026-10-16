import csv
import json
from pathlib import Path
import shutil

ROOT = Path(__file__).resolve().parents[3]
ARCHIVE = Path(__file__).resolve().parent
brief = Path("C:/Users/FNHF/.codex/skills/occ-art-direction/references/brief-contract.md")
backup = ARCHIVE / "backups/brief_contract" / brief.name
backup.parent.mkdir(parents=True, exist_ok=True)
if not backup.exists():
    shutil.copy2(brief, backup)
text = brief.read_text(encoding="utf-8-sig")
text = text.replace("- Generated pixel source: the complete subject", "- Strict battlefield/icon generated source (not the noncombat HD2D library): the complete subject")
text = text.replace("orthographic 12x9 tactical reading", "approved battlefield camera with readable logical grid, foot contacts and occlusion")
text = text.replace("1920x1080, left map 75%, right HUD 25%", "1920x1080, full-width battlefield above bottom commands; upper-left unit status, upper-right turn order and hover action-value drawer")
if "For noncombat HD2D text carriers and props" not in text:
    text += "\nFor noncombat HD2D text carriers and props, use A.2.2's adaptive library exception; preserve coarse clusters and provenance without forcing a fixed lattice, palette or hard-alpha decode. For migrated scene lighting/output, read A.2.4 and the project occ-hd2d-quality skill before drafting.\n"
brief.write_text(text, encoding="utf-8")

path = ROOT / "Tools/OCCArt/occ_art_contract_v1.json"
d = json.loads(path.read_text(encoding="utf-8"))
d["screen_layouts"]["main_menu"]["native_object_baseline"] = None
d["screen_layouts"]["main_menu"]["object_canvas_policy"] = "adaptive complete subject plus safe border; 2026-10-09 noncombat library exception; no mandatory 64x64 canvas"
d["screen_layouts"]["main_menu"]["runtime_validation"] = "hd2d_quality_policy plus role-specific source and formal asset checks"
d["unified_pixel_asset_pipeline"]["scope"] = "strict battlefield assets and functional icons; noncombat HD2D library uses its authorized exception"
d["formal_requirements"]["main_menu_hd2d_exception"]["historical_scope"] = "main-menu-only exception superseded for migrated scene output by A.2.4; source QA remains role-specific"
d["formal_requirements"]["migrated_hd2d_scene_output"] = "A.2.4 application and quality validation replaces whole-frame integer lattice proof for migrated scenes; six_x source review does not impose six-times scene output"
for check in d["consistency_checks"]:
    if check["path"] == d["canonical_document"]:
        if "### A.2.4 HD-2D 技术表现与品质门槛" not in check["required"]:
            check["required"].append("### A.2.4 HD-2D 技术表现与品质门槛")
    if check["path"] == "AGENTS.md":
        if "occ-hd2d-quality/SKILL.md" not in check["required"]:
            check["required"].append("occ-hd2d-quality/SKILL.md")
path.write_text(json.dumps(d, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
table = ROOT / "Worldbuilding/数据表/OCC_美术与界面规格表_v1.0.csv"
with table.open(encoding="utf-8", newline="") as file:
    rows = list(csv.reader(file))
for row in rows[1:]:
    if not row:
        continue
    if row[0] in {"ART-GRID-01", "ART-BATTLE-EDGE-64", "ART-BATTLE-NATIVE-32", "ART-VIEWPORT-OVERVIEW"}:
        row[5] = "以下整帧整数倍率要求仅适用于未迁移场景；已迁移HD-2D按A.2.4高清输出，源尺寸／PPU／占格不变。" + row[5]
    if row[0] == "ART-PIXEL-PIPELINE":
        row[5] += "；战斗外HD2D素材库按2026-10-09例外，严格网格和无缩放要求不适用于该类；场景输出按A.2.4"
with table.open("w", encoding="utf-8", newline="") as file:
    csv.writer(file, lineterminator="\n").writerows(rows)
print("Legacy display/source scopes and actual skill references unified")
