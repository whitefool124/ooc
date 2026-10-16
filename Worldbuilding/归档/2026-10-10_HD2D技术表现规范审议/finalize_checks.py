import difflib
import hashlib
import json
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[3]
ARCHIVE = Path(__file__).resolve().parent
validator = ROOT / "Tools/OCCArt/validate_occ_art_asset.py"
original = validator.read_text(encoding="utf-8-sig")
added = "    # The same art audit also catches broken HD2D quality routing/references.\n    if \"hd2d_quality_policy\" in contract:\n        from validate_hd2d_quality import audit as audit_hd2d\n        errors.extend(audit_hd2d(contract, root))\n"
restored = original.replace(added, "")
backup = ARCHIVE / "backups/art_validator/validate_occ_art_asset.py"
backup.parent.mkdir(parents=True, exist_ok=True)
backup.write_text(restored, encoding="utf-8")
(backup.parent / "note.txt").write_text("Reconstructed pre-hook content by removing only this task's audit hook; not a timestamped original copy.\n", encoding="utf-8")

index = json.loads((ARCHIVE / "backups/index.json").read_text(encoding="utf-8"))
diffs = []
for n, path in enumerate(index):
    before = ARCHIVE / "backups" / str(n) / Path(path).name
    diffs.extend(difflib.unified_diff(before.read_text(encoding="utf-8-sig").splitlines(True), Path(path).read_text(encoding="utf-8-sig").splitlines(True), fromfile=str(before), tofile=path))
(ARCHIVE / "task_changes.diff").write_text("".join(diffs), encoding="utf-8")
print("Backups and scoped diffs recorded")
