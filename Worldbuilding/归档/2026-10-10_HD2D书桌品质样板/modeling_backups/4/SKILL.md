---
name: occ-hd2d-quality
description: "OCC HD-2D相机、材质、光影、后处理、场景资产或表现验收变更时，选择批准配置并执行与影响范围对应的品质门槛。"
---

# OCC HD-2D Quality

Read the affected clauses in the mother document/local mirror A.2.4 and asset classification in A.2.2. `Tools/OCCArt/occ_art_contract_v1.json:hd2d_quality_policy` is the machine mirror; do not create another art direction. Classify changes as shared rendering, desk, battlefield, or local asset/page before editing. This skill does not authorize new gameplay, formal asset replacement or scene saving.

## Before changing presentation

- Inspect `Tools/OCCArt/hd2d_baselines_v1.json`. Reuse APPROVED_BASELINE profiles and registered overrides; QA_PENDING is not an approved reference. Missing first approval is a quality gap to resolve on one complete sample before expanding production, not a reason to refuse unrelated authorized fixes.
- Keep source lattice/PPU/canvas separate from final scene output. Migrated HD2D uses high-resolution output; strict battlefield source rules and the relaxed noncombat library rules are different roles. Do not force the noncombat library through 1.2 confidence, fixed palette or hard-alpha decode.
- Shared camera/material/shadow/HDR/exposure/Volume/output/sampling changes invalidate both desk and battlefield evidence. Local content changes invalidate its affected profile and interactions. Bind every changed asset, prefab, shader, configuration and implementation file in the report; asset GUID alone does not prove unchanged content.
- Preserve last approved configuration. New candidates must not silently overwrite it. Discover Unity CLI and verify the main project's dataPath and scene dirty state when actually operating the Editor; document/skill maintenance does not require Editor startup.

## Evidence and promotion

Generate a pending record with `python Tools/OCCArt/create_hd2d_quality_report.py --scene desk --output <archive-relative-report.json>` (use `battlefield` for battle). Record actual configuration values, not just intended defaults. Add current normal-entry runtime record, images, recordings, target-device performance and human aesthetic review. Evidence records require actual files and SHA256; changing a bound file invalidates the report. When evidence is stale, rerun affected validation; do not merely replace the hash in old evidence and call it current.

Run `python Tools/OCCArt/validate_hd2d_quality.py <report.json> --scope desk` for a local desk change (`battlefield` for battle). Shared changes use the default shared scope and `--also-report <other-scene-report.json>`; one scene cannot satisfy it. Exit 1 blocks APPROVED_BASELINE. `--audit-contract` checks only references and policy presence. It cannot prove runtime image quality, current Editor configuration or human judgment; explicitly distinguish these checks in delivery.

Performance budgets belong to the approved registry entry (device, tier, per-resolution target_fps and p95_budget_ms); never relax them in the report to pass. The human record is JSON containing status, reviewer, reviewed_at, dimensions and `subject_sha256` from `validate_hd2d_quality.review_fingerprint(report)`. Obtain the review against those exact inputs and evidence; this detects stale records but is not an authenticated signature or independent proof of the reviewer's identity.

The registry is deliberately QA_PENDING until actual sample approval. Populate a candidate report and configuration, obtain genuine human approval, then register the approved snapshot and run the complete gate. Do not manufacture a reviewer, inherit old screenshots, or use test fixtures as production evidence. Asset FORMAL gates remain separate. Shared changes require passing records for both profiles; never treat one report as global approval.

First samples: normal-entry shared loadout desk, then FR-03 rain-after-lamp-court battle. Check both supported resolutions, default/no-post images, low/high light or emission, contact/occlusion, camera motion, legal zoom, interaction, page isolation and resource lifetime. Stop expansion on critical readability, contact, occlusion, input, exposure or resource failures. Fix the demonstrated cause and retest only affected checks; retain failed evidence and the previous approved version.

## Delivery

Report what users can complete, what was tested, which baseline/inputs were used and the worst remaining defect. Record technical/runtime and human outcomes independently. Until approved samples exist, say quality control is installed and samples are pending; do not claim Octopath-equivalent quality or promise that automatic validation guarantees beauty.
