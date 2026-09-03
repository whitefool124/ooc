# OCC ART-UI-EMPTY-62 学院档案空状态插图验证（2026-08-28）

## 结论

PASS。六件独立透明空状态插图完成 `occ-art-manifest-v1 → QA_PENDING → FORMAL_CANDIDATE → FORMAL` 全流程；只对既有空档案分支增加表现层图片，其余五件注册备用，不改变任何空状态条件、交互、玩法、数值、地图或存档。

## 资产与接触表

| ID | 语义／材质 | 正式路径 | 当前接触 |
|---|---|---|---|
| `empty_archive_tray` | 空档案托盘；旧木／锻铁 | `Art/FormalUIEmptyIllustrations/empty_archive_tray` | 既有 `ArchivedMapRun == null` |
| `empty_inventory_pouch` | 空补给袋；灰绿布／皮革 | `Art/FormalUIEmptyIllustrations/empty_inventory_pouch` | 库存空状态接触样张；已注册 |
| `empty_route_case` | 闭合测绘筒；皮革／旧铜 | `Art/FormalUIEmptyIllustrations/empty_route_case` | 路线锁定接触样张；已注册 |
| `empty_reward_crate` | 空奖励浅箱；旧木／锻铁 | `Art/FormalUIEmptyIllustrations/empty_reward_crate` | 奖励空状态接触样张；已注册 |
| `empty_loadout_rack` | 空器材挂架；锻铁／旧木 | `Art/FormalUIEmptyIllustrations/empty_loadout_rack` | 配装空状态接触样张；已注册 |
| `locked_document_satchel` | 封存文书袋；灰绿布／封存红 | `Art/FormalUIEmptyIllustrations/locked_document_satchel` | 未解锁章节接触样张；已注册 |

## 生产与验证

- 生图：Codex 内建 imagegen 6 次独立调用；原料归档于 `UnityProject/Artifacts/UiEmptyIllustrations6/*/source.png`。未使用拼板、本地生图工作台、localhost、私有 relay 或自动回退。
- 像素门禁：6/6 为 `64×64`、硬 Alpha、11–12 色、透明边 ≥2px；1×、2×、4×、灰阶、棋盘格证据齐全。
- manifest／合同：`UnityProject/Artifacts/UiEmptyIllustrations6/validation_report.json` 为 6/6 PASS，机器合同审计 PASS。
- Unity：6/6 Sprite、PPU32、Point、Clamp、无 Mipmap、无压缩，Resources 6/6，GUID 唯一 6/6；审计见 `UnityProject/Artifacts/UiEmptyIllustrations6/import_audit.json`。
- 接触：`ui_empty_application_1920x1080.png`／`960x540.png` 覆盖六类状态；`ui_empty_before_after_1920x1080.png`／`960x540.png` 覆盖档案、库存、奖励前后对照；正式资源另有两档 runtime 接触。
- 审美：低饱和器物不抢标题和按钮；托盘／浅箱内部为空，挂架无装备，测绘筒闭合，文书袋以小面积红色约束带表达封存；没有蓝晶和冷青能量模板。

## Unity 回归

- 主工作树门禁：`Application.dataPath = E:/数据库/OCC_Codex/UnityProject/Assets`。
- 聚焦 EditMode：1/1 PASS。
- 全量 `OCC.Combat.EditModeTests`：652/652 PASS。
- 编译：error 0，warning 0；Console error 0。
- 场景：`CombatPrototype.unity` dirty false；未进入 Play Mode，未保存场景。

## 下一批建议

若继续，独立立项 4–6 件 `128×32` 章节分隔横幅，优先“教学记录／工坊记录／医务记录／郊野调查／封存卷宗”；仍禁止常驻高密度装饰和通用蓝晶／冷青能量模板。
