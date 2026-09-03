# OCC ART-ICON-REGEN-48~51 法术与道具图标全量重制验证（2026-08-26）

## 结果

- 143 张已全部晋级 `FORMAL`：火术式 50、运行时技能 27、实体物品 13、库存／掉落语义 22、法宝 20、装备槽位 11。
- 每项均有独立 imagegen 调用、独立原料、独立归一化文件与独立 `occ-art-manifest-v1`；没有生成拼板切图。
- 110 张能力／物件图标交付为 `32×32 / PPU32 / 最多 10 色 / 2px 安全边`；33 张 UI 语义／装备槽交付为 `16×16 / PPU16 / 最多 4 色 / 1px 安全边`。
- 用户审查指出蓝色水晶和青色能量重复后，更新 M-A19 简报的颜色规则并返修 24 张：火系使用白热、琥珀、朱红、深红；维修／救援使用柔白与草绿；护盾用乳白／金色，位移与电弧可用紫蓝／亮黄，污染才使用紫色。旧原料以 `source_v1.png` 保留。

## Unity 接线

- 正式 32px 资源覆盖原路径且保留已有 `.meta`；新的 16px 资源进入 `FormalItemSemanticIcons16` 与 `FormalEquipmentSlotIcons16`。
- `FormalArtRegistry` 只迁移库存语义和槽位的表现层资源路径；没有修改技能、物品、法宝、装备、背包、掉落、存档或数值定义。
- `FormalArtImportPostprocessor` 和对应审计测试识别两个新的 16px 目录。
- 143/143 正式 PNG 均有非空且唯一 GUID；Sprite／Point／Clamp／无 mipmap／Uncompressed／正确 PPU 与尺寸全部通过。
- `FormalArtRegistry` 当前活跃唯一资源路径 131 项，`Resources.Load<Sprite>` 缺失 0，尺寸／PPU 错误 0。活跃唯一数少于 143 是因为 v0.2 火术式明确复用已审核的旧 50 图标子集，且清单同时保留全部本轮被查看并重制的正式资产，不是回退或加载缺失。

## 验证

- `validate_occ_art_asset.py`：`143/143 PASS`。
- Unity 正式接触：`1920×1080` 与 `960×540` 均通过；1× 下 16px 语义保持简洁，32px 能力／物件主体可辨，调色返修后没有全组蓝晶模板化。
- 全量 EditMode：`649/649 PASS`（job `87f89e08-0461-49a1-a213-2c29a0b0c73e`）。
- 编译：错误 0、警告 0；Console error 0；dirty scenes 0；`CombatPrototype.unity` 未保存。
- Funplay 项目路径：`Application.dataPath=E:/数据库/OCC_Codex/UnityProject/Assets`。

## 证据索引

- 资产清单：`Worldbuilding/05_美术与音频/正式美术生产/M-A19/icon_regen_143_catalog.json`
- 生产简报：`Worldbuilding/05_美术与音频/正式美术生产/M-A19/OCC_M-A19_法术与道具图标全量重制简报_v0.1.md`
- 143 份 manifest：`Worldbuilding/05_美术与音频/正式美术生产/M-A19/manifests/`
- 正式验证报告：`UnityProject/Artifacts/IconRegen143/validation_report_formal.json`
- Unity Importer／GUID 报告：`UnityProject/Artifacts/IconRegen143/unity_import_report.json`
- 正式接触：`UnityProject/Artifacts/IconRegen143/contacts/unity_formal_contact_1920x1080.png`、`unity_formal_contact_960x540.png`
- 前后对照：`UnityProject/Artifacts/IconRegen143/contacts/before_after_1920x1080.png`
- 分组无标签接触：同目录 `fire_unlabelled.png`、`runtime_unlabelled.png`、`item_unlabelled.png`、`semantic_unlabelled.png`、`artifact_unlabelled.png`、`slot_unlabelled.png`

## 下一批建议

本轮停止，不自动开启下一批。若后续新增内容，先按功能色与主体语法定点生产；水晶仅用于真实储能／棱镜物件，普通术式禁止默认蓝晶或统一青色发光芯。
