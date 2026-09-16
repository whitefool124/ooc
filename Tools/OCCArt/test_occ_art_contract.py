from __future__ import annotations

import copy
import importlib.util
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
MODULE_PATH = Path(__file__).resolve().with_name("validate_occ_art_asset.py")
SPEC = importlib.util.spec_from_file_location("occ_art_validator", MODULE_PATH)
VALIDATOR = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(VALIDATOR)

CONTRACT = VALIDATOR.read_json(Path(__file__).resolve().with_name("occ_art_contract_v1.json"))
CHARACTER = ROOT / "UnityProject/Reports/CombatTestArena/native32_character_test/adaptive_canvas_v1/field_technician_native32x64_manifest.json"
CHEST = ROOT / "UnityProject/Reports/CombatTestArena/native_prop_adaptive_test/chest_pitch_compare/chest_pitch20_manifest.json"


class OccArtContractTests(unittest.TestCase):
    def validate(self, manifest_path: Path):
        manifest = VALIDATOR.read_json(manifest_path)
        return VALIDATOR.validate_manifest(manifest, CONTRACT, ROOT)

    def test_contract_documents_are_consistent(self):
        self.assertEqual(VALIDATOR.audit_contract(CONTRACT, ROOT), [])

    def test_approved_adaptive_candidates_pass_the_single_contract(self):
        manifests = [CHARACTER, CHEST]
        for path in manifests:
            with self.subTest(path=path.name):
                errors, result = self.validate(path)
                self.assertEqual(errors, [], result)
                self.assertEqual(result["status"], "PASS")

    def test_semantic_blue_loss_is_rejected(self):
        manifest = VALIDATOR.read_json(CHARACTER)
        manifest = copy.deepcopy(manifest)
        manifest["delivery"]["required_color_families"][0]["min_opaque_pixels"] = 999
        errors, _ = VALIDATOR.validate_manifest(manifest, CONTRACT, ROOT)
        self.assertTrue(any("required color family cyan" in error for error in errors), errors)

    def test_adaptive_single_cell_prop_cannot_claim_two_cells(self):
        manifest = VALIDATOR.read_json(CHEST)
        manifest = copy.deepcopy(manifest)
        manifest["delivery"]["logical_cells"] = [2, 1]
        errors, _ = VALIDATOR.validate_manifest(manifest, CONTRACT, ROOT)
        self.assertTrue(any("logical_cells" in error for error in errors), errors)

    def test_new_battlefield_roles_use_32ppu_adaptive_canvases(self):
        roles = CONTRACT["roles"]
        self.assertEqual(VALIDATOR.expected_size(roles["floor_tile_32"], [1, 1]), (32, 32))
        self.assertEqual(VALIDATOR.expected_size(roles["tactical_unit_32x64"], [1, 1]), (32, 64))
        self.assertIsNone(VALIDATOR.expected_size(roles["single_cell_prop_adaptive_32ppu"], [1, 1]))
        self.assertEqual(roles["single_cell_prop_adaptive_32ppu"]["unity_ppu"], 32)
        self.assertEqual(roles["single_cell_prop_adaptive_32ppu"]["logical_cells"], [1, 1])

    def test_legacy_64px_role_cannot_create_a_new_candidate(self):
        manifest = VALIDATOR.read_json(CHEST)
        manifest = copy.deepcopy(manifest)
        manifest["role"] = "battlefield_single_cell_prop_64"
        manifest["status"] = "QA_PENDING"
        errors, _ = VALIDATOR.validate_manifest(manifest, CONTRACT, ROOT)
        self.assertTrue(any("historical FORMAL assets only" in error for error in errors), errors)

    def test_forbidden_generation_route_is_rejected(self):
        manifest = VALIDATOR.read_json(CHARACTER)
        manifest = copy.deepcopy(manifest)
        manifest["provenance"]["source_channel"] = "local_workbench"
        manifest["provenance"]["source_descriptor"] = "localhost fallback"
        errors, _ = VALIDATOR.validate_manifest(manifest, CONTRACT, ROOT)
        self.assertTrue(any("source channel is not approved" in error for error in errors), errors)
        self.assertTrue(any("forbidden source route" in error for error in errors), errors)

    def test_formal_candidate_without_human_review_is_rejected(self):
        manifest = VALIDATOR.read_json(CHARACTER)
        manifest = copy.deepcopy(manifest)
        manifest["human_review"]["overall"] = "PENDING"
        manifest["human_review"]["application"] = "PENDING"
        errors, _ = VALIDATOR.validate_manifest(manifest, CONTRACT, ROOT)
        self.assertTrue(any("human_review.overall PASS" in error for error in errors), errors)
        self.assertTrue(any("human review dimension must PASS: application" in error for error in errors), errors)


if __name__ == "__main__":
    unittest.main()
