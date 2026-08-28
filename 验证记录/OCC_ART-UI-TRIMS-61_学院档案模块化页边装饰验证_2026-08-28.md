# OCC ART-UI-TRIMS-61 学院档案模块化页边装饰验证

日期：2026-08-28  
范围：剧情／肉鸽共用 UI 表现层；不改页面结构、文字、按钮、交互、玩法、数值或存档。

## 结论

6 件学院档案页边装饰已完成独立生成、规范化、应用接触、人工审美、正式导入与运行时接线，全部晋级 `FORMAL`。运行时不再用矩形／直线脚本几何冒充页脊和登记纹，而是加载真实透明 Sprite；装饰位于背景之后、正文内容之前，且 `raycastTarget=false`。

生产只使用 Codex 内建 imagegen，一件一次调用；未使用拼板切图、本地生图工作台、localhost API、私有 relay 或自动回退。

## 资产清单

| 资产 ID | 尺寸 | 色数 | 用途 | 审美结果 |
|---|---:|---:|---|---|
| `ui.trim.binding_spine` | 32×64 | 7 | 可重复布面装订脊 | PASS；低权重纵向节奏 |
| `ui.trim.index_tab` | 64×32 | 9 | 空白索引页签 | PASS；无文字／伪字 |
| `ui.trim.measure_ruler` | 64×32 | 9 | 木制调查测量尺 | PASS；仅长短刻口，无数字 |
| `ui.trim.corner_clasp` | 32×32 | 10 | 锻铁档案角扣 | PASS；铁／旧铜可辨 |
| `ui.trim.folded_corner` | 64×64 | 10 | 暖纸折页 | PASS；固定左上光向，不旋转 |
| `ui.trim.status_clip` | 32×32 | 10 | 状态纸夹 | PASS；封存红面积受控 |

## 机器与 Unity 门禁

- 6/6：硬 Alpha、至少 1px 透明边、≤10 色，尺寸严格匹配机器角色。
- 6/6：1×、2×、4×、灰阶、棋盘格证据齐全。
- 6/6：`occ-art-manifest-v1` 为 `FORMAL`；`validate_occ_art_asset.py` PASS；合同审计 PASS。
- 6/6：Sprite／Point／Clamp／PPU32／Uncompressed／无 mipmap；GUID 唯一。
- `OccPeripheralUiV01.decorations` 注册 6/6，Resources 加载 6/6。
- `FormalUiEffects.AddAmbientScanlines` 实际创建 10 个 Sprite Image：5 段装订脊及其余 5 件；全部不接收射线。
- 首次正式运行时接触发现装饰容器被背景遮盖，原因是 `SetAsFirstSibling()`；改为背景之后的 sibling 索引后复测通过。

批次报告：`UnityProject/Artifacts/UiTrims6/validation_report.json`。  
Unity 导入审计：`UnityProject/Artifacts/UiTrims6/import_audit.json`。  
Manifest：`Worldbuilding/05_美术与音频/正式美术生产/M-A28/manifests/`。

## 三页双分辨率接触

| 页面 | 1920×1080 | 960×540 | 结果 |
|---|---|---|---|
| 入口 | `landing_formal_trims_1920x1080.png` | `landing_formal_trims_960x540.png` | PASS |
| 地图 | `map_formal_trims_1920x1080.png` | `map_formal_trims_960x540.png` | PASS |
| 库存 | `inventory_formal_trims_1920x1080.png` | `inventory_formal_trims_960x540.png` | PASS |

接触目录：`UnityProject/Artifacts/UiTrims6/contacts/`。单件 4× 棋盘格总览为 `ui_trims_6_review.png`；正式运行时总接触为 `ui_trims_formal_runtime_1920x1080.png` 与 `ui_trims_formal_runtime_960x540.png`。

人工检查确认：三种明暗背景下，装订脊与纸签保持第二层阅读；折页、尺和状态夹未侵入主卡；正文、标题、按钮和点击净空优先级不变。所有方向性资产保持原方向，未旋转或镜像。

## 回归

- 主工作树：`Application.dataPath = E:/数据库/OCC_Codex/UnityProject/Assets`。
- 聚焦 EditMode：`1/1 PASS`。
- 全量 `OCC.Combat.EditModeTests`：`652/652 PASS`。
- 编译：错误 0、警告 0。
- Console：error 0。
- 活动场景：`Assets/Scenes/CombatPrototype.unity`，`isDirty=false`。
- 未进入 Play Mode，未保存场景。

## 下一批建议

若继续，可另立少量“空状态／章节分隔”插图任务，例如空档案托盘、封存书袋、课程分章标牌；仍须避免常驻页面密度继续上升。页边装饰本批暂不扩量，只按真实裁切或遮挡问题做单件返修。
