"""Regression checks for production gates; synthetic failures are not Unity evidence."""
import copy
import json
from pathlib import Path
import subprocess
import sys
import unittest
from unittest.mock import patch
import uuid

import occ_vfx_pipeline as pipeline


class ProductionGates(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.recipe = pipeline.read("Tools/OCCVfx/recipes/sky_furnace_v1.json")

    def candidate(self):
        return copy.deepcopy(self.recipe)

    def assert_rejected(self, recipe, text, stage="design"):
        result = pipeline.check_recipe(recipe, stage)
        self.assertFalse(result["ok"])
        self.assertIn(text, " ".join(result["errors"]))

    def test_current_reference_and_measured_candidate(self):
        self.assertTrue(pipeline.verify_reference()["ok"])
        result = pipeline.check_recipe(self.recipe, "runtime")
        self.assertTrue(result["ok"], result)
        self.assertFalse(result["formal_promotion"])

    def test_auxiliary_texture_is_rejected(self):
        recipe = self.candidate()
        next(l for l in recipe["layers"] if l["role"] == "dust")["texture"] = recipe["layers"][0]["texture"]
        self.assert_rejected(recipe, "texture-free")

    def test_bulky_cube_and_thick_paper_are_rejected(self):
        for role, axis in (("fracture", 0), ("magic_seal", 2)):
            recipe = self.candidate()
            next(l for l in recipe["layers"] if l["role"] == role)["world_size_m"][axis] *= 3
            self.assertFalse(pipeline.check_recipe(recipe)["ok"])

    def test_missing_burst_and_coverage_fade_are_rejected(self):
        recipe = self.candidate();recipe["phases"].pop("burst")
        self.assert_rejected(recipe, "burst")
        recipe = self.candidate();recipe["exit"]["method"] = "random_coverage_fade"
        self.assert_rejected(recipe, "fracture the complete source")

    def test_cloned_effect_does_not_inherit_runtime_evidence(self):
        recipe = self.candidate();recipe["id"] = "new_spell"
        recipe["capture_mode"] = "new_capture"
        recipe["capture_directory"] = "Worldbuilding/归档/VFX/new_spell_capture"
        self.assertTrue(pipeline.check_recipe(recipe)["ok"])
        self.assert_rejected(recipe, "different recipe", "runtime")

    def test_gameplay_needs_actual_event_binding(self):
        recipe = self.candidate();recipe["usage"] = "gameplay"
        self.assert_rejected(recipe, "settlement/action")

    def test_whole_motion_not_just_particle_size(self):
        recipe = self.candidate();recipe["effect_motion_limit_m"] = [1, 1, 1]
        recipe["evidence"]["subject_sha256"] = pipeline.fingerprint(recipe)
        self.assert_rejected(recipe, "world-space motion", "runtime")

    def test_runtime_fade_static_pixels_and_source_loss_fail(self):
        filename = self.recipe["evidence"]["files"]["native_samples"]["path"]
        original_read = pipeline.read
        original = original_read(filename)
        mutations = [
            (lambda r: r.update(minAlpha=.5), "fade"),
            (lambda r: r.update(materialAlpha=.5), "fade"),
            (lambda r: r.update(materialEnergy=.01), "energy changes"),
            (lambda r: r["tracks"][0].update(position=[0, 0, 0]), "visibly move"),
            (lambda r: r["tracks"][0].update(liveTexels=0), "source pixels"),
            (lambda r: r["tracks"][0].update(trackedPixel=999), "Same native pixel"),
        ]
        for index, (mutate, expected) in enumerate(mutations):
            data = copy.deepcopy(original)
            for sample in data:
                if 9 <= sample["time"] <= 11.2:
                    # Change a single sample for energy/identity, every sample for stationary positions.
                    if index not in (2, 5) or sample["time"] == 9.6:
                        mutate(sample["burst"]["fragmentReadback"])
            with self.subTest(index=index), patch.object(pipeline, "read", side_effect=lambda f: data if f == filename else original_read(f)):
                self.assert_rejected(self.recipe, expected, "runtime")

    def test_missing_files_and_unknown_adapter_do_not_pass(self):
        recipe = self.candidate();recipe["evidence"]["files"].pop("movie")
        self.assert_rejected(recipe, "movie", "runtime")
        recipe = self.candidate();recipe["evidence"]["adapter"] = "not_implemented"
        self.assert_rejected(recipe, "not implemented", "runtime")

    def test_stale_lock_binding_fails(self):
        reference = pipeline.read(pipeline.REFERENCE);reference["inputs"][0]["sha256"] = "0" * 64
        original_read = pipeline.read
        with patch.object(pipeline, "read", side_effect=lambda f: reference if f == pipeline.REFERENCE else original_read(f)):
            self.assertFalse(pipeline.verify_reference()["ok"])

    def test_reference_requires_recovery_snapshots(self):
        reference = pipeline.read(pipeline.REFERENCE);reference["inputs"][0].pop("snapshot_path")
        original_read = pipeline.read
        with patch.object(pipeline, "read", side_effect=lambda f: reference if f == pipeline.REFERENCE else original_read(f)):
            result = pipeline.verify_reference()
            self.assertFalse(result["ok"])
            self.assertIn("recovery snapshots", " ".join(result["errors"]))

    def test_paths_cannot_escape_checkout(self):
        with self.assertRaises(ValueError):
            pipeline.path("../outside-allowed-project.json")

    def test_new_capture_cannot_overwrite_frozen_evidence(self):
        recipe = self.candidate();recipe["capture_mode"] = "new_capture"
        self.assert_rejected(recipe, "overlaps frozen evidence")

    def test_new_is_draft_and_does_not_overwrite(self):
        output = "Worldbuilding/归档/2026-10-16_特效生产体系固定/fixtures/new_" + uuid.uuid4().hex + ".json"
        args = [sys.executable, str(pipeline.HOME / "occ_vfx_pipeline.py"), "new", "production_probe", "--output", output]
        first = subprocess.run(args, capture_output=True)
        self.assertEqual(first.returncode, 0, first.stderr)
        self.assertEqual(pipeline.read(output)["status"], "DRAFT")
        self.assertFalse(pipeline.check_recipe(pipeline.read(output))["ok"])
        before = pipeline.digest(output)
        self.assertEqual(subprocess.run(args, capture_output=True).returncode, 1)
        self.assertEqual(pipeline.digest(output), before)

    def test_batch_reports_duplicate_ids(self):
        filename = "Tools/OCCVfx/recipes/sky_furnace_v1.json"
        run = subprocess.run([sys.executable, str(pipeline.HOME / "occ_vfx_pipeline.py"), "check", filename, filename], capture_output=True)
        result = json.loads(run.stdout.decode("utf-8"))
        self.assertEqual(run.returncode, 1)
        self.assertTrue(result["results"][0]["ok"])
        self.assertIn("Duplicate effect id", " ".join(result["results"][1]["errors"]))


if __name__ == "__main__":
    unittest.main()
