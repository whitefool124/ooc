# UI-POLISH-88 术式细边框与资源底色修正验证

日期：2026-09-02  
归属：剧情／肉鸽共用战斗 UI 表现层

## 结果

- 依据玩家澄清，灰色问题定位为右侧资源区底色；上一轮完全移除卡片边界的处理已修正。
- 单卡四边新增独立炭墨直线框：上／下为 `268×2px`，左／右为 `2×76px`；不重新启用标准按钮九宫格，因此不存在旧皮肤的灰色承重下沿。
- 在 1920×1080 下边框为 2px；在 960×540 下准确落为 1 个物理像素，卡片边界清楚但不再压迫内部文字与图标。
- 右侧 `60×64px` 资源块改为不透明深冷炭墨，正常实机样本为 `#2B3734FF`；空槽使用更暗冷调炭墨，不使用 `FormalUiTheme.Muted` 中性灰。
- 资源不足和冷却继续在同一冷调基底上分别混入危险色／琥珀色；行动点与个人魔力 32px 图标及数值坐标未改变。

## 自动验证

- Funplay 身份门禁：`Application.dataPath = E:/数据库/OCC_Codex/UnityProject/Assets`；活动场景为 `Assets/Scenes/CombatPrototype.unity`，修改前场景未脏且未处于 Play Mode。
- 编译：0 error，0 warning。
- 聚焦 EditMode：`FormalUiThemeTests.SpellSlots_PresentKeyIconNameAndTwoResourceCostsAsOneReadableCard`，1/1 Passed。
- 全量 EditMode：706/706 Passed。
- PlayMode：1/1 Passed。
- 运行时八卡审计：`cards=8`、`activeStandardSkins=0`、`wrongFrameCount=0`、`wrongFrameThickness=0`、`grayResourceBlocks=0`、`translucentResourceBlocks=0`、`wrongIcons=0`。
- 选中／禁用运行时探针未重新激活旧标准皮肤；双资源图标继续为 32×32px。
- Console：0 error；进入与退出 Play Mode 后 dirty scene 均为 0；未保存场景或存档。
- `occ_art_contract_v1.json` 已登记 2px 细框、`deep_cool_charcoal` 资源块和禁止空槽中性灰底合同，并通过 JSON 语法解析。

## 双分辨率接触

- 1920×1080：`UnityProject/Library/FunplayMcp/Screenshots/UI-POLISH-88-thin-frame-cool-resource-1920x1080.png`
- 960×540：`UnityProject/Library/FunplayMcp/Screenshots/UI-POLISH-88-thin-frame-cool-resource-960x540.png`

两档分辨率下，八张术式卡均恢复清楚细边界；资源区不再呈现突兀中性灰，空槽与已装备卡仍能通过内容和整体禁用色区分。
