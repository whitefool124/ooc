"""Integrity tests use isolated temporary fixtures, never production approval."""
import copy
import json
from pathlib import Path
import tempfile
import unittest
import subprocess
import sys

from PIL import Image
import validate_hd2d_quality as gate


class Hd2DQualityTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.contract = copy.deepcopy(gate.read(gate.CONTRACT))
        self.policy = self.contract["hd2d_quality_policy"]
        for name in [self.contract["canonical_document"], self.policy["skill"], self.policy["validator"], self.policy["baseline_registry"],
                     *self.policy["profiles"]["desk"]["required_inputs"]]:
            path = self.root / name
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text("A.2.4 HD-2D 技术表现与品质门槛\nTEST FIXTURE ONLY", encoding="utf-8")
        self.contract_path = self.root / "contract.json"
        self.contract_path.write_text(json.dumps(self.contract), encoding="utf-8")
        profile = self.policy["profiles"]["desk"]
        config = {name: "TEST ONLY" for name in self.policy["required_configuration"]}
        config.update(profile["locked_parameters"])
        config["construction_manifest"] = self.make_text("construction.json", json.dumps({
            "schema": "occ-hd2d-construction-v1", "visible_models": []}))
        budget = {"device": "TEST ONLY", "quality_tier": "TEST ONLY", "resolutions": {
            "x".join(map(str, r)): {"target_fps": 60, "p95_budget_ms": 20} for r in self.policy["resolutions"]}}
        self.registry = {"baselines": {"desk-test": {"scene_kind": "desk", "status": "APPROVED_BASELINE",
            "configuration": config, "performance_budget": budget}}}
        self.report = {"schema": "occ-hd2d-quality-report-v1", "policy_version": self.policy["version"],
            "contract_sha256": gate.digest(self.contract_path), "scene_kind": "desk", "baseline_id": "desk-test",
            "status": "APPROVED_BASELINE", "configuration": config,
            "changed_files": [profile["required_inputs"][-1]],
            "inputs": [self.bind(x) for x in profile["required_inputs"]], "evidence": [],
            "runtime": {"project_path": str((self.root / "UnityProject").resolve()).replace("\\", "/"),
                "normal_entry": True, "console_clear": True, "interaction_pass": True, "isolation_pass": True,
                "resource_lifetime_pass": True, "record": self.make_text("runtime.json", "TEST ONLY")},
            "performance": {"device": "TEST ONLY", "quality_tier": "TEST ONLY", "measurements": [],
                "record": self.make_text("performance.json", "TEST ONLY")},
            "human_review": {"status": "APPROVED", "reviewer": "TEMPORARY TEST FIXTURE",
                "reviewed_at": "TEST ONLY", "dimensions": {k: "PASS" for k in self.policy["dimensions"]}}, "blockers": []}
        for resolution in self.policy["resolutions"]:
            path = self.root / ("capture_" + str(resolution[0]) + ".png")
            Image.new("RGB", resolution).save(path)
            video = self.make_text("motion_" + str(resolution[0]) + ".mp4", "TEST ONLY: not a real recording")
            for case in profile["static_cases"]:
                self.report["evidence"].append(dict(self.bind(path.name), case=case, resolution=resolution, verdict="PASS"))
            for case in profile["motion_cases"]:
                self.report["evidence"].append(dict(video, case=case, resolution=resolution, verdict="PASS"))
            self.report["performance"]["measurements"].append({"resolution": resolution, "target_fps": 60,
                "p95_budget_ms": 20, "p95_frame_ms": 16, "cpu_frame_ms": 8, "gpu_frame_ms": 9,
                "memory_mb": 500, "switch_peak_mb": 600})
        self.review()

    def bind(self, path):
        return {"path": path, "sha256": gate.digest(self.root / path)}

    def make_text(self, path, content):
        (self.root / path).write_text(content, encoding="utf-8")
        return self.bind(path)

    def review(self):
        review = dict(self.report["human_review"])
        review.pop("record", None)
        review["subject_sha256"] = gate.review_fingerprint(self.report)
        self.report["human_review"]["record"] = self.make_text("review.json", json.dumps(review))

    def errors(self):
        return gate.validate(self.report, self.contract, self.registry, self.root, self.contract_path)

    def test_complete_isolated_record_passes_integrity_only(self):
        self.assertEqual(self.errors(), [])

    def test_missing_case_blocks(self):
        self.report["evidence"].pop()
        self.assertTrue(any("Missing evidence" in e for e in self.errors()))

    def test_changed_source_invalidates_evidence(self):
        path = self.root / self.report["inputs"][-1]["path"]
        path.write_text("CHANGED", encoding="utf-8")
        self.assertTrue(any("stale/tampered" in e for e in self.errors()))

    def test_update_hash_alone_does_not_preserve_human_approval(self):
        binding = self.report["inputs"][-1]
        (self.root / binding["path"]).write_text("CHANGED", encoding="utf-8")
        binding["sha256"] = gate.digest(self.root / binding["path"])
        self.assertTrue(any("Human review refers to stale" in e for e in self.errors()))

    def test_pending_baseline_cannot_pass(self):
        self.registry["baselines"]["desk-test"]["status"] = "QA_PENDING"
        self.assertTrue(any("Baseline is not approved" in e for e in self.errors()))

    def set_model(self, role="large_scene_prop", texture=True):
        model = {"id": "desk", "role": role, "style": "simple_polygonal", "rationale": "TEST ONLY: supporting reading surface"}
        for key in ("geometry", "material", "pixel_texture"):
            if key == "pixel_texture" and not texture:
                continue
            item = self.make_text(key + ".fixture", "TEST ONLY")
            model[key] = item
            self.report["inputs"].append(item)
        self.report["configuration"]["construction_manifest"] = self.make_text("construction.json", json.dumps({
            "schema": "occ-hd2d-construction-v1", "visible_models": [model]}))
        self.review()

    def test_textured_large_polygon_model_is_allowed(self):
        self.set_model()
        self.assertEqual(self.errors(), [])

    def test_missing_model_texture_blocks(self):
        self.set_model(texture=False)
        self.assertTrue(any("pixel_texture" in e for e in self.errors()))

    def test_model_outside_scope_blocks(self):
        self.set_model(role="small_prop")
        self.assertTrue(any("outside primary scope lacks justification" in e for e in self.errors()))

    def test_construction_manifest_change_invalidates_binding(self):
        (self.root / "construction.json").write_text('{"schema":"changed"}', encoding="utf-8")
        self.assertTrue(any("construction manifest: stale/tampered" in e for e in self.errors()))

    def test_unapproved_budget_and_overrun_block(self):
        self.report["performance"]["measurements"][0]["p95_budget_ms"] = 99
        self.report["performance"]["measurements"][1]["p95_frame_ms"] = 30
        errors = self.errors()
        self.assertTrue(any("budget not approved" in e for e in errors))
        self.assertTrue(any("budget failed" in e for e in errors))

    def test_wrong_project_or_blocker_blocks(self):
        self.report["runtime"]["project_path"] = "wrong/project"
        self.report["blockers"] = ["Unreadable text"]
        errors = self.errors()
        self.assertTrue(any("main repository" in e for e in errors))
        self.assertTrue(any("blockers" in e for e in errors))

    def test_corrupt_capture_or_wrong_dimensions_blocks(self):
        item = self.report["evidence"][0]
        (self.root / item["path"]).write_bytes(b"\x89PNG\r\n\x1a\ncorrupt")
        item["sha256"] = gate.digest(self.root / item["path"])
        self.assertTrue(any("Invalid/corrupt screenshot" in e for e in self.errors()))

    def test_escape_outside_repository_blocks(self):
        self.report["inputs"][0]["path"] = "../escaped.txt"
        self.assertTrue(any("remain within repository" in e for e in self.errors()))

    def test_missing_human_dimension_or_runtime_blocks(self):
        self.report["human_review"]["dimensions"]["reading_input"] = "PENDING"
        self.report["runtime"]["interaction_pass"] = False
        errors = self.errors()
        self.assertTrue(any("Human dimension unapproved" in e for e in errors))
        self.assertTrue(any("Runtime gate" in e for e in errors))

    def cli(self, scope):
        report = self.root / "report.json"
        registry = self.root / "registry.json"
        report.write_text(json.dumps(self.report), encoding="utf-8")
        registry.write_text(json.dumps(self.registry), encoding="utf-8")
        return subprocess.run([sys.executable, str(Path(gate.__file__)), str(report),
            "--root", str(self.root), "--contract", str(self.contract_path), "--registry", str(registry),
            "--scope", scope], capture_output=True, encoding="utf-8")

    def test_shared_change_cannot_pass_with_one_scene(self):
        result = self.cli("shared")
        self.assertEqual(result.returncode, 1)
        self.assertIn("requires reports", result.stdout)

    def test_explicit_local_scope_passes_complete_fixture(self):
        result = self.cli("desk")
        self.assertEqual(result.returncode, 0, result.stdout)


if __name__ == "__main__":
    unittest.main()
