# UI-POLISH-86 按玩家草图重制术式槽验证

日期：2026-09-02  
归属：剧情／肉鸽共用战斗 UI 表现层

## 结果

- 八个术式槽保持 `4×2`、单卡 `268×76px`，底栏五组比例、快捷键绑定、术式数据与施法规则未改动。
- 左侧正式 32px 术式图标以最近邻整数 2× 显示为 `64×64px`，位置／尺寸为 `(6,0)/(64,64)`；`24×24px` 黑色快捷键块位于 `(2,-2)`，叠压图标左上角。
- 术式名称只占中部 `(74,-18)/(122,40)`，水平／垂直居中；超长名称使用省略，不进入资源块或边框区域。
- 右侧为单一深色资源块 `(202,-6)/(60,64)`；行动点行 `(204,-8)/(56,28)`、个人魔力行 `(204,-40)/(56,28)` 上下排列，均含 16px 资源图标与消耗数值，零消耗显示 `0`。
- 已删除旧 `费用数值底` 白色／浅色背景和 `术式费用带` 底部横向区域。资源数字直接落在统一深色资源块内，文字使用高对比浅色；对应资源不足时只把该数字与资源块状态转为危险色。
- 空槽仍保留八卡节奏但禁用；选中、冷却和资源不足状态沿用卡框、资源块与文字差异，不以额外白框承载状态。

## 自动验证

- Funplay 身份门禁：`Application.dataPath = E:/数据库/OCC_Codex/UnityProject/Assets`；活动场景为 `Assets/Scenes/CombatPrototype.unity`，修改前场景未脏。
- 编译：0 error，0 warning。
- 聚焦 EditMode：`FormalUiThemeTests.SpellSlots_PresentKeyIconNameAndTwoResourceCostsAsOneReadableCard`，1/1 Passed。
- 全量 EditMode：706/706 Passed。
- PlayMode：1/1 Passed。
- 运行时结构审计：`cards=8`、`whiteValueFrames=0`、`horizontalBands=0`、`emptyEnabled=0`、`clipped=0`。
- 运行时几何审计：图标 `(6,0)/(64,64)`、快捷键 `(2,-2)/(24,24)`、名称 `(74,-18)/(122,40)`、资源块 `(202,-6)/(60,64)`、行动点 `(204,-8)/(56,28)`、个人魔力 `(204,-40)/(56,28)`。
- Console：0 error；进入与退出 Play Mode 后 dirty scene 均为 0；未保存场景或存档。
- `occ_art_contract_v1.json` 已同步图标、快捷键、名称、资源块、费用行与状态章合同，并明确禁止费用数值白色背景；JSON 语法解析通过。

## 双分辨率与状态接触

- 正常／1920×1080：`UnityProject/Library/FunplayMcp/Screenshots/UI-POLISH-86-sketch-layout-1920x1080.png`
- 正常／960×540：`UnityProject/Library/FunplayMcp/Screenshots/UI-POLISH-86-sketch-layout-960x540.png`
- 个人魔力不足／1920×1080：`UnityProject/Library/FunplayMcp/Screenshots/UI-POLISH-86-sketch-shortage-1920x1080.png`

两档分辨率均可一眼分辨左侧图标、叠压快捷键、中部名称和右侧上下双资源；旧白色费用框、底部费用带、名称／资源重叠及可见文字裁切均未再出现。个人魔力不足时对应数值转为危险色，行动点数值仍保持正常高对比。
