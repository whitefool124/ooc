# OCC M-A44 生产与评审记录 v0.1

> 状态：`8/8 REVIEW_READY / PRODUCT REVIEW PENDING`，2026-08-31。  
> 总接触表：`QA/contacts/m_a44_production_contact.png`。  
> 图标／占格应用接触：`QA/contacts/m_a44_task_item_inventory_contact.png`。  
> 通用格位接触：`QA/contacts/m_a44_prop_footprint_contact.png`。

## 生产与验证

- 迁徙足迹拓片、排水沉积样本、折断猎人标牌和异味饲料样本各有一张独立 `32×32` 内容图标与一张独立 `1×1` 证物物件，共 8 件。
- 所有 manifest 均先于图像生成建立；图标与物件分别调用 Codex 内置图像生成能力，不存在图标缩放、描摹、拼板切图、本地工作台、localhost、私有 relay 或自动回退。
- 8/8 通过尺寸、透明边界、硬 alpha、调色板与证据路径机器合同；合同单测 6/6、规范镜像审计 PASS。
- `6×10` 库存摘录确认四种证物在 32px 下可以区分，同一证物的图标与物件保持同物身份但构图独立；没有药水色、毒性特效、文字徽记或可确认的幕后责任。
- 未修改或导入 `UnityProject/Assets/`，不创建 GUID，不标记 `FORMAL_CANDIDATE` 或 `FORMAL`。

## 拒绝与通道记录

- `strange_fodder_sample_footprint v01`：缩小后读取为三卷白色绷带，未建立饲料样本语义；已进入 `rejected/strange_fodder_sample_footprint/v01/`。现行 v02 为单个灰橄榄封样包、交叉束绳和清晰枯草纤维。
- `broken_hunter_tag_footprint` 的原始长提示请求连续三次遇到内置生图网络失败，均未形成图片或生产版本；缩短为同一冻结物理身份后由同一内置通道成功生成。未启用替代通道。

## 状态边界

本批冻结证物外观和可携带 1×1 占格，不确认迁徙物种、污染／毒性结论、责任人、获取顺序、任务奖励、售价或战斗效果。产品仍可按资产或整批给出通过／返工；即使通过，也因“不实装”要求保持非 Unity 状态。
