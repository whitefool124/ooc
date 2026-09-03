# OCC ART-NEEDS-TRIAL-72 后续美术需求与试验品验证（2026-08-30）

## 目标

盘点当前正式资产之后仍缺失、未完成或需等待内容冻结的美术需求，并为可立即验证的每个主要资产族制作一件独立试验品；本轮只积累审核原料，不导入 Unity、不实装。

## 交付

- 新建 `Worldbuilding/05_美术与音频/OCC_后续美术资产需求表_v0.1.md`，把需求区分为可试验、需返工、内容阻塞和当前不需要，避免重复已有学院战斗、火系图标／特效、正式 UI 与既有物件资产。
- 新建 `正式美术生产/M-A32/` 跨类别试验批次，包含 12 份 manifest、最终提示词目录、原始输出、规范化资产、逐件 QA 证据、跨类别接触表、试验简报与产品评审记录。
- 在唯一美术规范与机器镜像中补齐 `character_portrait_b_384x576`、`character_performance_c_192x288` 和 `vfx_frame_32` 三个缺失角色；合同审计通过。
- 旧版 `OCC_正式美术资产需求表_v0.1.md` 标记为历史生产基线，后续当前缺口统一由新表维护。

## 验证结果

- 12/12 规范化试验品通过 `validate_occ_art_asset.py`：尺寸、调色板、硬透明、非透明边界与证据文件均符合各自角色合同。
- `test_occ_art_contract.py` 与 `validate_occ_art_asset.py --audit-contract` 通过。
- 每件均保留 1×、4×、灰阶、棋盘格证据；总接触表位于 `M-A32/QA/contacts/m_a32_trial_contact.png`。
- 所有 manifest 仍为 `QA_PENDING`，产品人工审核仍待完成；没有试验品被标成 `FORMAL_CANDIDATE` 或 `FORMAL`。
- 仅使用内置图像生成能力；未使用本地生图工作台、localhost、私有 relay 或自动回退。
- 未向 `UnityProject/Assets/` 写入本批资产，未进入 Play Mode，未保存场景。

## 下一步

由产品按 T01–T12 分别给出“通过生产／返工／暂缓”。只有通过的资产族才建立独立正式批次；地图真实占格、角色身份、玩法 ID 或页面语义未冻结的类别继续保持阻塞。
