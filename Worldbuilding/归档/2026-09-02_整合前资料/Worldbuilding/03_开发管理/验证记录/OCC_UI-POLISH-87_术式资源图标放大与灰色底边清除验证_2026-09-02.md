# UI-POLISH-87 术式资源图标放大与灰色底边清除验证

日期：2026-09-02  
归属：剧情／肉鸽共用战斗 UI 表现层

## 结果

- 单卡继续保持 `268×76px`；左侧 64px 术式图标、24px 快捷键块、中部 122×40px 名称和右侧 60×64px 资源块均未改位。
- 行动点／个人魔力两行由 `56×28px` 调整为 `56×32px`，位置分别为 `(204,-6)` 与 `(204,-38)`，完整填满右侧资源块的上下两半。
- 两枚原生 16px 正式语义图严格整数 2× 显示为 `32×32px`；24px 消耗数字位于图标右侧 `(32,4)/(24,40)` 独立文字槽，图标与数值不重叠。
- 术式卡底面改为无 Sprite 的 `Image.Type.Simple` 平面底色；标准按钮 `正式皮肤` 保留节点但关闭渲染，因此原九宫格皮肤产生的灰色承重下沿已消失。
- 悬停／按下／选中／禁用继续通过平面底色、像素焦点框、资源块和文字语义反馈；运行时主动切换选中与禁用后，旧皮肤仍未被重新激活。

## 自动验证

- Funplay 身份门禁：`Application.dataPath = E:/数据库/OCC_Codex/UnityProject/Assets`；活动场景为 `Assets/Scenes/CombatPrototype.unity`，修改前场景未脏且未处于 Play Mode。
- 编译：0 error，0 warning。
- 聚焦 EditMode：`FormalUiThemeTests.SpellSlots_PresentKeyIconNameAndTwoResourceCostsAsOneReadableCard`，1/1 Passed。
- 全量 EditMode：706/706 Passed。
- PlayMode：1/1 Passed。
- 运行时八卡审计：`cards=8`、`activeStandardSkins=0`、`nonFlatSurfaces=0`、`wrongRows=0`、`wrongIcons=0`、`overlappingValues=0`；当前两张已装备卡共生成四个资源行，均符合尺寸合同。
- Console：0 error；进入与退出 Play Mode 后 dirty scene 均为 0；未保存场景或存档。
- `occ_art_contract_v1.json` 已同步 56×32 资源行、32px 资源图标和禁用术式标准按钮皮肤合同，并通过 JSON 语法解析。

## 双分辨率接触

- 1920×1080：`UnityProject/Library/FunplayMcp/Screenshots/UI-POLISH-87-resource-icons-flat-card-1920x1080.png`
- 960×540：`UnityProject/Library/FunplayMcp/Screenshots/UI-POLISH-87-resource-icons-flat-card-960x540.png`

两档分辨率下均能更清楚地区分行动点与个人魔力图标，消耗数字未被图标挤压；八张术式卡底部不再出现标准按钮皮肤的灰色横条。
