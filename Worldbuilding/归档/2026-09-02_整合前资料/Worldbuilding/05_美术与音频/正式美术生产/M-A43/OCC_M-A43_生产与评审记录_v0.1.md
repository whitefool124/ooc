# OCC M-A43 生产与评审记录 v0.1

> 状态：`12/12 REVIEW_READY / PRODUCT REVIEW PENDING`，2026-08-31。  
> 总接触表：`QA/contacts/m_a43_production_contact.png`。  
> 地板族接触：`QA/contacts/m_a43_floor_family_contact.png`。  
> 格位接触：`QA/contacts/m_a43_prop_footprint_contact.png`。  
> 地区应用接触：`QA/contacts/m_a43_region_12x9_contact.png`。

## 生产与验证

- 12 件均在 manifest 建立后由 Codex 内置图像生成能力独立生产；未调用本地工作台、localhost、私有 relay 或自动回退。
- 4 张地板、围栏三态、路标、石涵洞、牲口槽、桶架与猎人驿亭共 12/12 通过单资产机器合同；合同单测 6/6、规范镜像审计 PASS。
- 每件具备源图／交付哈希、1×／4×／灰阶／棋盘格和组级应用接触；12×9 非 Unity 地图接触同时复核了地板混铺、围栏状态、1×1／2×1／2×2 格位、单位对比和临时战术角标。
- 全批未写入或导入 `UnityProject/Assets/`，不进入 `FORMAL_CANDIDATE` 或 `FORMAL`。

## 拒绝版本

- `outer_ring_field_verge v01`：大块高饱和草团在 32px 下更像苔藓地；`v02`：源图白色展示边缘进入交付并形成棋盘状白框。现行 `v03` 为低对比灰褐田埂土。
- `outer_ring_drainage_mud v01`：中心圆形结构反复读成坑／井盖；`v02`：把排水沟本体烘进了地板边缘。现行 `v03` 仅保留整格沟边湿泥语义。
- `outer_ring_stone_culvert_2x1 v01`：轮廓过高，读取为倒塌石墙。现行 `v02` 使用低矮四块盖石和窄排水缝。

## 状态边界

本批只冻结外环猎径地区的可复用地图语汇，不确认迁徙动物种类、污染结论、任务流程、奖励或关卡布局。产品仍可按资产或整批给出通过／返工；即使通过，也因“不实装”要求保持非 Unity 状态。
