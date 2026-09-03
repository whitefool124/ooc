# ART-ACADEMY-FOOTPRINTS-53 学院 32 件装备背包占格素材重制验证

## 结论

`PASS / FORMAL`。学院阶段 32 件装备的背包占格素材已全部从抽象几何图重制为独立真实多格装备；占格、旋转、重量、槽位、数值、掉落、存档与背包逻辑保持不变。

## 生产与规格

- 32 份 manifest 在生成前登记为 `QA_PENDING`。
- 32 次 Codex 内建 imagegen 单件调用；32 个原料哈希全部唯一。
- 没有拼板切图、M-A20 内容图标拉伸、本地工作台、localhost、私有 relay 或自动回退。
- 六种交付尺寸为 `32×32／32×64／32×96／64×32／64×64／64×96`，严格对应既有逻辑占格。
- `pixel-asset-pipeline` 只执行去背、硬 Alpha、等比适配、最近邻定尺寸、限色与证据输出，没有脚本补画。

## 美术与应用审查

- 1×3 剑、枪和导杖完整使用纵轴；2×3 战锤、弓弩、长盾、服装和背架使用完整矩形，不是中央小图标。
- 2×1 护目镜、护额、握带和护臂横向展开；2×2 盾、靴、胫甲与核心保持宽厚主体；1×1 环、饰品和扣件仍能凭轮廓区分。
- 全部资产无固定投影和强方向阴影，支持现有背包旋转；主材质以锻铁、旧铜、木、皮、粗布、陶瓷和烟灰玻璃为主。
- 两张正式 6×10 背包容纳全部 32 件且仅余 2 格；1920×1080 为 2×、960×540 为 1×整数接触，未发生越格、裁边或假占格。

## 机器与 Unity 结果

- 32/32：硬 Alpha、5–12 色、至少 2px 透明安全边、精确目标尺寸。
- `validate_occ_art_asset.py`：`32 PASS / 0 FAIL`；机器合同审计：`PASS`。
- Funplay 身份门禁：`Application.dataPath=E:/数据库/OCC_Codex/UnityProject/Assets`。
- Resources 32/32 加载；Importer 32/32 为 Sprite／Point／Clamp／PPU32／Uncompressed／无 mipmap。
- GUID 32/32 覆盖前后稳定，32 个唯一。
- 全量 EditMode：`649 passed / 0 failed / 0 skipped`。
- 编译 0 error / 0 warning；Console 0 error；`CombatPrototype.unity dirty=False`。
- 未保存场景，未进入 Play Mode。

## 证据

- 前后对照：`UnityProject/Artifacts/AcademyEquipmentFootprints32/contacts/before_after_1920x1080.png`
- Unity 6×10 背包：`unity_inventory_contact_1920x1080.png`、`unity_inventory_contact_960x540.png`
- 全量规格化接触：`normalized_contact_1920x1080.png`、`normalized_contact_960x540.png`
- 清单：`Worldbuilding/05_美术与音频/正式美术生产/M-A21/academy_equipment_footprints_32_catalog.json`
- 报告：`validation_report_formal.json`、`contract_audit_report.json`、`unity_import_report.json`

## 后续边界

本批完成后停止。若真实拖拽或旋转暴露单件误读，只返修对应 manifest 与多格 PNG，不改变占格矩形或背包逻辑。
