# OCC M-A40 生产与评审记录 v0.1

> 状态：`6/6 REVIEW_READY / PRODUCT REVIEW PENDING`，2026-08-30。  
> 总接触表：`QA/contacts/m_a40_production_contact.png`。  
> 材料接触：`QA/contacts/m_a40_semantic_material_contact.png`。

## 生产与验证

- 6 件均在 manifest 建立后由内置图像生成能力独立生产；未调用本地工作台、localhost、私有 relay 或 CLI 回退。
- 维修线、铜铆钉、皮补片、硬木栓、炭滤棒与划线粉笔，共 6/6 通过机器合同；合同单测 6/6、合同审计 PASS。
- 每件保留原生 `24×24` 源图，并以 `(4,4)` 无缩放嵌入 `32×32` 交付画布；均具备哈希、1×／4×／灰阶／棋盘格证据。
- 组级接触确认六件材料在 24px 下各自只保留一个物质结构，没有完整工具、文字、魔法光或黑色背景。
- 未修改或导入 `UnityProject/Assets/`；不冻结配方、消耗量、价格、掉落或正式内容 ID，不标记 `FORMAL_CANDIDATE` 或 `FORMAL`。

## 拒绝版本

- `copper_rivet_pouch v01`：铜件与深布袋粘成一团，24px 下像绳团／杂料，三枚铆钉圆头不可数；已进入 `rejected/`。
- 现行 v02 使用浅灰袋口和三枚可分离的铜圆头／短钉身，重新规范化与验证后通过。

## 状态边界

本批只增加可复用维修物质库存，不创造 gameplay 内容。产品可以按资产或整批给出通过／返工；即使全部通过，也因“不实装”要求保持非 Unity 状态。
