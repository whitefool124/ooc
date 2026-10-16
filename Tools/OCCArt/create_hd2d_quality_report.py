"""Create QA_PENDING evidence record; never fabricates a passing result."""
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--scene", choices=["desk", "battlefield"], required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    output = (ROOT / args.output).resolve()
    if not output.is_relative_to(ROOT) or output.exists():
        parser.error("output must be a new file within the repository")
    contract_path = ROOT / "Tools/OCCArt/occ_art_contract_v1.json"
    contract = json.loads(contract_path.read_text(encoding="utf-8-sig"))
    policy = contract["hd2d_quality_policy"]
    profile = policy["profiles"][args.scene]
    report = {
        "schema": "occ-hd2d-quality-report-v1", "policy_version": policy["version"],
        "contract_sha256": hashlib.sha256(contract_path.read_bytes()).hexdigest(),
        "scene_kind": args.scene, "baseline_id": args.scene + "-v1", "status": "QA_PENDING",
        "changed_files": [], "inputs": [], "configuration": {
            **{key: None for key in policy["required_configuration"]}, **profile["locked_parameters"]},
        "evidence": [], "runtime": {"project_path": str(ROOT / "UnityProject").replace("\\", "/"),
            "normal_entry": False, "console_clear": False, "interaction_pass": False,
            "isolation_pass": False, "resource_lifetime_pass": False, "record": None},
        "performance": {"device": None, "quality_tier": None,
            "measurements": [{"resolution": r, "target_fps": None, "p95_budget_ms": None,
                "p95_frame_ms": None, "cpu_frame_ms": None, "gpu_frame_ms": None,
                "memory_mb": None, "switch_peak_mb": None} for r in policy["resolutions"]], "record": None},
        "human_review": {"status": "PENDING", "reviewer": None, "reviewed_at": None,
            "dimensions": {d: "PENDING" for d in policy["dimensions"]}, "record": None},
        "blockers": ["Runtime, performance and human sample approval are pending"]
    }
    for value in profile["required_inputs"]:
        path = ROOT / value
        report["inputs"].append({"path": value, "sha256": hashlib.sha256(path.read_bytes()).hexdigest()})
    for resolution in policy["resolutions"]:
        for case in profile["static_cases"] + profile["motion_cases"]:
            report["evidence"].append({"case": case, "resolution": resolution,
                "path": None, "sha256": None, "verdict": "PENDING"})
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"created": str(output), "status": "QA_PENDING"}, ensure_ascii=False))


if __name__ == "__main__":
    main()
