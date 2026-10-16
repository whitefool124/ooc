"""Bind current local preview evidence without claiming baseline approval."""
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
ARCHIVE = Path(__file__).resolve().parent
def bound(path):
    return {"path": path.relative_to(ROOT).as_posix(), "sha256": hashlib.sha256(path.read_bytes()).hexdigest()}

report_path = ARCHIVE / "weather_quality_candidate.json"
report = json.loads(report_path.read_text(encoding="utf-8-sig"))
changed = [ROOT / p for p in [
    "UnityProject/Assets/Game/Editor/Hd2DContinuousBattleCapture.cs",
    "UnityProject/Assets/Game/Editor/Hd2DBattleWeatherPreview.cs",
    "UnityProject/Assets/Game/Editor/Hd2DBattleLocalEvidence.cs",
    "UnityProject/Assets/Game/Runtime/Presentation/Hd2DWeatherRainPreview.cs",
    "UnityProject/Assets/Game/Resources/Shaders/Hd2DWeatherPreviewRain.shader"]]
report["changed_files"] = [p.relative_to(ROOT).as_posix() for p in changed]
inputs = {i["path"]: i for i in report["inputs"]}
for path in changed:
    record = bound(path); inputs[record["path"]] = record
captures = []
for preset in ["Sunset", "RainNight"]:
    folder = ARCHIVE / "profiles" / preset
    capture = json.loads((folder / "capture.json").read_text(encoding="utf-8"))
    cleanup = json.loads((folder / "cleanup.json").read_text(encoding="utf-8"))
    assert capture["status"] == "captured"
    assert not cleanup["play"] and not cleanup["dirty"]
    assert cleanup["scenePreserved"] and cleanup["pipelineRestored"] and cleanup["windowPreferencesRestored"]
    for path in sorted(folder.iterdir()):
        if path.suffix in [".png", ".json"] and "failure" not in path.name:
            record = bound(path); inputs[record["path"]] = record
    captures.append({"preset": preset, "runtime": bound(folder / "capture.json"), "cleanup": cleanup,
                     "configuration": bound(folder / "weather_preview.json")})
report["inputs"] = list(inputs.values())
report["blockers"] = ["Local isolated Editor preview only; no normal entry or Player performance evidence", "Complete density and construction review pending", "Weather appearance has not yet received human review"]
report["local_preview"] = captures
report_path.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
approval = {"scope": "Four crystal appearance candidates and replacement permission; later user requested keeping both versions usable",
            "user_approval": "认可，允许正式替换", "latest_direction": "前后两版都可以用啊我感觉 又不是一定要一种",
            "result": "Both original formal PNGs and independently generated candidate PNGs preserved; no destructive replacement",
            "overall_hd2d_status": "QA_PENDING", "does_not_approve": ["whole scene pixel density", "performance", "weather previews"]}
(ARCHIVE / "crystal_two_version_decision.json").write_text(json.dumps(approval, ensure_ascii=False, indent=2), encoding="utf-8")
progress_path = ARCHIVE / "progress_state.json"
progress = json.loads(progress_path.read_text(encoding="utf-8-sig"))
progress["completed"].extend(["Four crystal candidates locally rendered with intact/damaged fixtures; user approved appearance and permitted replacement", "Latest user direction keeps both old and new crystal sources; neither destroyed", "Same-scene Sunset/RainNight local Editor previews captured at1080/540, rain animation frames, cleanup verified"])
progress["approval"] = "Four crystal appearances approved; user permits keeping both versions. Whole HD2D baseline and new weather samples QA_PENDING."
progress["next"] = "Present Sunset/RainNight samples for appearance review; retain durable batch material work and remaining density/construction/runtime/performance evidence."
progress["remaining"] = [p for p in progress["remaining"] if p != "three crystal variants"]
progress_path.write_text(json.dumps(progress, ensure_ascii=False, indent=2), encoding="utf-8")
print(json.dumps({"status": "QA_PENDING", "presets": captures}, ensure_ascii=False))
