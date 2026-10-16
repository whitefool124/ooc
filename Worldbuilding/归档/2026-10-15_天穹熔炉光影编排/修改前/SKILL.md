---
name: occ-hd2d-quality
description: "OCC HD-2D相机、材质、光影、后处理、场景资产或表现验收变更时，选择批准配置并执行与影响范围对应的品质门槛。"
---

# OCC HD-2D Quality

Highest-priority gate: same-scene pixel size consistency. Before generation, reuse or placement, lock the scene reference and effective world pixel pitch; the noncombat library exception does not waive this. Failures block expansion and sample promotion before other polish.

Asset admission also uses this gate: `FORMAL_CANDIDATE`/`FORMAL` manifests require `application.pixel_density_manifest` (path/hash) and `pixel_density_inputs` for the scene inventory's source/placement files. Bind the asset's exact delivery PNG as a source in that inventory. Genuine `human_review.scene_pixel_size_consistency: PASS` and `reviewed_density_sha256` must refer to that exact current density record. QA_PENDING may retain incomplete evidence but cannot be promoted. This is evidence integrity, not an automatic visual classifier or permission to invent PASS.

Bind `configuration.pixel_density_manifest` (path/SHA256). Schema `occ-hd2d-pixel-density-v1` requires positive finite `world_pixel_pitch`, `reference_asset_id`, `all_visible_pixel_assets_reviewed`, and nonempty `assets`. Each asset has unique `id`, bound `source` and `placement` files also in report inputs, a `density_basis` explaining authored pixel pitch and display-scale/UV conversion, `source_uniformity: PASS`, and `effective_world_pitch_xy` equal to the scene target. Do not confuse raw generated texels with authored macro pixels. Two-resolution `comparisons` each include `resolution`, bound PNG `capture`, bound `motion_record`, all `sampled_asset_ids`, and `same_frame_verdict`, `projection_verdict`, `motion_verdict`. Only real review can set PASS. Machine equality uses floating-point serialization tolerance, not an aesthetic exception. Inventory completeness and actual rendered cluster scale require genuine human review under `scene_pixel_size_consistency`.

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


OCC modeling policy (final user correction, 2026-10-10): minimize modeling; prefer pixel cards/thick cards. Simple polygonal models mainly serve environments and large scene props, and every visible model needs matching pixel textures reinforcing the 2D style. Before promoting a sample, bind a construction manifest listing model rationale, geometry/material/texture inputs and application evidence; review model/card consistency. A table is not rejected merely for being modeled; untextured visible primitives or geometry-driven detail are quality failures.

Bind `configuration.construction_manifest` as a repository-relative path/SHA256 record. Its JSON schema is `occ-hd2d-construction-v1`; `visible_models` lists each model's unique `id`, `role`, `style: simple_polygonal`, `rationale`, and bound `geometry`, `material`, `pixel_texture` files, also included in report inputs. Use an empty list for card-only scenes. The primary roles are `environment_structure` and `large_scene_prop`; other roles require `exception_rationale` and the same genuine human style review. “Mainly” does not forbid every small modeled component. Completeness of the inventory and actual 2D appearance still require scene inspection and human review; file bindings alone cannot establish them.


## 同场景像素大小：最高优先级（用户确认，2026-10-10）

同场景像素大小统一（用户确认，2026-10-10，最高优先级）：同一场景的全部可见像素资产必须使用同一像素密度基准，包含角色、纸板、厚纸板、模型贴图、环境、陈设、场景内装饰和像素特效；美观、细节丰富度、画布自适应及战斗外素材库例外均不得放宽此要求。先选定场景颗粒参照，记录主体像素单元、世界尺寸、显示缩放及模型UV密度，再生成或复用素材；不能只凭图片尺寸、Point采样或粗像素提示词判断一致。以实际游戏镜头验收，相同深度与投影条件下可见像素大小必须统一；远近和斜面只允许同一密度经真实投影产生的变化，不得以透视为逐物件改密度的借口。源图中混用粗细格、同场景杯碟与灯等陈设颗粒不一致，均为最高优先级阻断项，先修复再扩量。书桌当前颗粒参照为咖啡杯右侧蓝金晶灯，只认可其像素大小与色簇表现，不等于批准整个场景。正文和系统文字保持既定清晰度，真实光照、阴影及后处理仍按高清空间表现配置，不强制整帧低清；它们不替代像素资产的一致性检查。入库与样板晋升必须提交全部可见像素资产的密度清单、源与缩放/UV追溯、1920×1080及960×540实际同框对比、运动/缩放检查和真实人工复核；缺失、待定或不一致时保持QA_PENDING。更换资产、尺寸、相机、UV或采样使相关证据失效，重新验证受影响范围，不用旧截图或机器PASS代替场景审核。

执行时先查总案A.2.4与机器合同pixel_density_policy。每个场景只用一个有效世界像素间距；完整清单将源主体像素单元、显示缩放或UV换算到此基准，再检查同框实际颗粒。生成与复用简报必须提供颗粒参照，不能只写coarse/pixel art。机器声明一致不证明图片真实一致；旧证据不得继承为批准。

## 材质生产与参考研究迁移

按总案A.2.4及 `Tools/OCCArt/hd2d_material_brief_template_v1.json` 把颜色、法线、表面属性、金属及发光区域分别登记，检查配套图的主体／画布／锚点／UV／颗粒。不得从颜色亮暗自动推断高度或粗糙度，或凭色相决定金属与发光。缺失数据是候选限制，不是完整PBR。中性光、默认光和移动主光以及关图／关发光对照先成立，再扩量。

参考游戏导出参数、丢失连接后的重建假设与OCC批准参数分开；不能将MPC默认值、跨引擎Gamma偏移、云影速度或AO半径直接注册为OCC基线。局部漫反射、镜面环境及接触遮蔽分别验证，避免以自发光代替补光；局部采集按变化合并、异步回读并在离页恢复或释放。远景失焦边界必须位于当前及隐藏阅读／交互几何之后；不得复制研究城镇的物理景深参数。修改后按既有影响范围提供真实流程与两个实际分辨率证据。
