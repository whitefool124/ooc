# OCC M-A37 生产与评审记录 v0.1

> 状态：`1/1 REVIEW_READY / PRODUCT REVIEW PENDING`，2026-08-30。  
> 总接触表：`QA/contacts/m_a37_production_contact.png`。  
> 占格接触：`QA/contacts/m_a37_prop_footprint_contact.png`。

## 生产与验证

- 候选在 manifest 建立后由内置图像生成能力独立生产；未调用本地工作台、localhost、私有 relay 或 CLI 回退。
- 交付严格为 `96×64 / 3×2`，12 色、硬 Alpha；机器合同 PASS，合同单测 6/6、合同审计 PASS。
- 占格接触将候选铺在 M-A33 港区石地并叠加 3×2 评审格线：左右承重墩分别落在外侧列，中央一列从前缘至门下保持连续通道，门楣没有中央落地支撑。
- 本批未覆盖 M-A18 `PROTOTYPE`、未改旧 manifest、未修改或导入 `UnityProject/Assets/`；无稳定 GUID、Importer 和运行时阻挡复核。

## 拒绝版本

- `v01`：通道虽可读，但带黑色环境底、徽记旗帜、明火、植物和过度装饰，且侧墩基脚存在侵入中央列风险；只保留 `rejected/academy_gate_3x2_rework_candidate/v01/` 的原料与规范化图追溯。
- 当前 `v02` 改为朴素浅暖石墩、深木门楣和连续中央石路，不含旗帜、明火、文字或能量装置。

## 状态边界

本件只解决非 Unity 美术候选的轮廓与占格诚实问题。产品通过后仍需另立导入任务，完成旧稳定 ID／GUID 迁移决定、Importer 和真实 `BlockedPositions` 接触，才能考虑 `FORMAL_CANDIDATE`；当前不得替换正式资产。
