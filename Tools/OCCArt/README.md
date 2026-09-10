# OCC Art Contract Tools

`Worldbuilding/策划案/OCC_项目总策划案_v1.0.md` is the only active art-direction
source. `Worldbuilding/数据表/OCC_美术与界面规格表_v1.0.csv` is its structured
companion, and the JSON contract in this folder is only a machine-readable mirror.

Every new asset must copy `occ_art_manifest_template_v1.json`, choose one role,
record repository-relative source/output/evidence paths and hashes, then run:

```powershell
& 'C:/Users/FNHF/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' `
  Tools/OCCArt/validate_occ_art_asset.py `
  path/to/asset.occ-art.json
```

Audit only the canonical documents and machine mirror:

```powershell
& 'C:/Users/FNHF/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' `
  Tools/OCCArt/validate_occ_art_asset.py --audit-contract
```

Audit every registered OCC art manifest in one command:

```powershell
& 'C:/Users/FNHF/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' `
  Tools/OCCArt/validate_all_occ_art.py
```

The validator never draws or repairs art. A machine `PASS` plus human review
permits `FORMAL_CANDIDATE`, not direct Unity import. `FORMAL` additionally needs
a stable GUID, verified importer and runtime application record.
