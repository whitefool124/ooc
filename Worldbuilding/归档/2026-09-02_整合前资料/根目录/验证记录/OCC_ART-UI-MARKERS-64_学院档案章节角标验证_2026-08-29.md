# ART-UI-MARKERS-64 学院档案章节角标验证

## 范围

本批只新增六件可复用、非交互的 `32×32` 章节角标与最小表现层注册，不改页面结构、交互条件、玩法、数值、地图、占格或存档。

## 资产清单

| ID | 视觉语义 | 色彩主轴 | 状态 |
|---|---|---|---|
| `teaching_chalk_clip` | 教学／粉笔记录 | 铁灰、暖纸、象牙白 | FORMAL |
| `workshop_caliper_clip` | 工坊／量规记录 | 锻铁、木褐 | FORMAL |
| `infirmary_bandage_clip` | 医务／绷带记录 | 白搪瓷、灰绿 | FORMAL |
| `field_leaf_clip` | 郊野／测绘记录 | 木褐、橄榄绿 | FORMAL |
| `sealed_red_clip` | 封存／限制记录 | 暗铁、封存红 | FORMAL |
| `reward_brass_tag` | 领取／库存记录 | 木褐、氧化黄铜 | FORMAL |

## 生产与验证

- 生成：六次 Codex 内建 imagegen 独立单件生成；未使用拼板、本地生图工作台、localhost API、私有 relay 或自动回退。
- 规范：`ui_chapter_marker_32`；6/6 为 `32×32`、硬 Alpha、10 色、四边至少 2px 透明安全边。
- 审美：1×、4×、灰阶、棋盘格与两档页面接触均可辨；固定左上光，不旋转或镜像复用；无蓝晶、冷青能量、文字、徽章底或按钮底。
- manifest：6/6 `FORMAL`；`validate_ui_chapter_markers_6.py` 与合同审计 PASS。
- Unity：稳定 GUID 唯一 6/6；Sprite、PPU 32、Point、Clamp、MipMap off、Uncompressed；Resources 6/6。
- 表现层：`FormalUiEffects.AddChapterMarker` 为按需入口，`raycastTarget=false`，没有强制常驻页面。

## 接触与工程状态

- 1920×1080：`UnityProject/Artifacts/UiChapterMarkers6/contacts/ui_chapter_markers_formal_1920x1080.png`
- 960×540：`UnityProject/Artifacts/UiChapterMarkers6/contacts/ui_chapter_markers_formal_960x540.png`
- 聚焦 EditMode：1/1 PASS。
- 全量 EditMode：652/652 PASS。
- 编译：错误 0、警告 0。
- 场景：`Assets/Scenes/CombatPrototype.unity`，dirty false；未进入 Play Mode、未保存场景。
