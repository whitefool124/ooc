# UI-POLISH-84 底部指令栏比例再平衡验证

日期：2026-09-02  
归属：剧情／肉鸽共用战斗 UI 表现层

## 结果

- 底部指令托盘继续使用 `1888×200px` 全宽合同，不增加屏幕占高，也不改变底栏以上 `1440:480` 的战场／HUD 分区。
- 五组参考宽度改为移动／武器 `200px`、个人术式 `1100px`、交互 `152px`、战术栏 `176px`、结束回合 `204px`，组间保持 8px 间距。
- 移动／攻击按钮由强制两行竖排恢复为横排中文，按钮尺寸为 `88×116px`；交互按钮为 `136×54px`，在两档分辨率下均可直接读出动作。
- 八个术式槽仍为 `4×2`，单卡 `268×70px`；32px 图标、24×24px 键位徽章、最长八字名称、行动点与个人魔力费用继续同时存在。名称槽为 `216×40px`，两枚费用章的左上参考位置为 `(178,-36)`／`(222,-36)`，注意徽章左移至 `(230,-2)`。
- 战术栏两列槽位扩为 76px，结束回合按钮扩为 204px，整体主次由“术式独占”调整为“基础操作—术式—情境操作—回合确认”的稳定层级。

## 自动验证

- Funplay 身份门禁：`Application.dataPath = E:/数据库/OCC_Codex/UnityProject/Assets`；活动场景为 `Assets/Scenes/CombatPrototype.unity`，修改前场景未脏。
- 编译：0 error，0 warning。
- 聚焦 EditMode：`FormalUiThemeTests.SpellSlots_PresentKeyIconNameAndTwoResourceCostsAsOneReadableCard`，1/1 Passed。
- 全量 EditMode：706/706 Passed。
- PlayMode：1/1 Passed。
- 运行时合同：`战术指令=(1888,200)`；五组位置／尺寸分别为 `(8,-14)/(200,172)`、`(216,-8)/(1100,184)`、`(1324,-14)/(152,172)`、`(1484,-14)/(176,172)`、`(1668,-24)/(204,140)`。
- 运行时底栏可见文字边界审计：clipped 0。
- Console：0 error；退出 Play Mode 后 dirty scene 0；未保存场景或存档。
- `occ_art_contract_v1.json` 已同步五组宽度与术式卡尺寸并通过 JSON 语法解析。

## 双分辨率接触

- `UnityProject/Library/FunplayMcp/Screenshots/UI-POLISH-84-proportion-final-1920x1080.png`
- `UnityProject/Library/FunplayMcp/Screenshots/UI-POLISH-84-proportion-final-960x540.png`

两档画面均确认移动／攻击不再竖排，交互与结束回合获得足够权重，八张术式卡仍保留名称和双费用，底栏无裁切、重叠或跨组覆盖。
