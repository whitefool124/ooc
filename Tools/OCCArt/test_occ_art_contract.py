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
V01 = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A18/QA/asset_stability_test_v01"
V02 = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A18/QA/asset_stability_test_v02"


class OccArtContractTests(unittest.TestCase):
    def validate(self, manifest_path: Path):
        manifest = VALIDATOR.read_json(manifest_path)
        return VALIDATOR.validate_manifest(manifest, CONTRACT, ROOT)

    def test_contract_documents_are_consistent(self):
        self.assertEqual(VALIDATOR.audit_contract(CONTRACT, ROOT), [])

    def test_two_stability_batches_pass_the_single_contract(self):
        manifests = [
            V01 / "academy_communal_wood_table.occ-art.json",
            V01 / "seasoning_bottle.occ-art.json",
            V02 / "wooden_sapphire_staff.occ-art.json",
            V02 / "academy_dormitory_bed.occ-art.json",
        ]
        for path in manifests:
            with self.subTest(path=path.name):
                errors, result = self.validate(path)
                self.assertEqual(errors, [], result)
                self.assertEqual(result["status"], "PASS")

    def test_semantic_blue_loss_is_rejected(self):
        manifest = VALIDATOR.read_json(V02 / "wooden_sapphire_staff.occ-art.json")
        manifest = copy.deepcopy(manifest)
        manifest["delivery"]["required_color_families"][0]["min_opaque_pixels"] = 999
        errors, _ = VALIDATOR.validate_manifest(manifest, CONTRACT, ROOT)
        self.assertTrue(any("required color family blue" in error for error in errors), errors)

    def test_two_cell_bed_cannot_be_declared_as_single_cell(self):
        manifest = VALIDATOR.read_json(V02 / "academy_dormitory_bed.occ-art.json")
        manifest = copy.deepcopy(manifest)
        manifest["role"] = "single_cell_prop_32"
        manifest["delivery"]["logical_cells"] = [1, 1]
        errors, _ = VALIDATOR.validate_manifest(manifest, CONTRACT, ROOT)
        self.assertTrue(any("delivery size" in error for error in errors), errors)

    def test_forbidden_generation_route_is_rejected(self):
        manifest = VALIDATOR.read_json(V02 / "wooden_sapphire_staff.occ-art.json")
        manifest = copy.deepcopy(manifest)
        manifest["provenance"]["source_channel"] = "local_workbench"
        manifest["provenance"]["source_descriptor"] = "localhost fallback"
        errors, _ = VALIDATOR.validate_manifest(manifest, CONTRACT, ROOT)
        self.assertTrue(any("source channel is not approved" in error for error in errors), errors)
        self.assertTrue(any("forbidden source route" in error for error in errors), errors)

    def test_formal_candidate_without_human_review_is_rejected(self):
        manifest = VALIDATOR.read_json(V01 / "academy_communal_wood_table.occ-art.json")
        manifest = copy.deepcopy(manifest)
        manifest["human_review"]["overall"] = "PENDING"
        manifest["human_review"]["application"] = "PENDING"
        errors, _ = VALIDATOR.validate_manifest(manifest, CONTRACT, ROOT)
        self.assertTrue(any("human_review.overall PASS" in error for error in errors), errors)
        self.assertTrue(any("human review dimension must PASS: application" in error for error in errors), errors)


if __name__ == "__main__":
    unittest.main()
