# UI-POLISH-85 术式槽内部信息分区与状态辨识验证

日期：2026-09-02  
归属：剧情／肉鸽共用战斗 UI 表现层

## 结果

- 八个术式槽继续保持 `4×2` 和 268px 卡宽，卡高由 70px 增至 76px；术式组调整为 `1100×196px` 并完整留在 `1888×200px` 底部指令托盘内。
- 左侧新增 `40×64px` 术式识别轨：`24×24px` 键位徽章位于上方，32px 正式术式图标位于下方，二者不再争抢同一基线。
- 右上名称改为 `208×40px` 左对齐槽；右下新增 `208×28px` 独立费用带，行动点／个人魔力费用章均为 `56×28px`，其中数值底与文字生成槽宽度为 40px。
- 冷却／风险徽章改为 `48×28px` 右上状态章。出现时名称宽度由 208px 自动缩至 156px，超过六字时省略，状态章与名称不再覆盖。
- 正常可用卡使用冷青识别轨；选中时外框／卡体和识别轨同时强调；行动点或个人魔力不足时对应费用数字与识别轨使用危险色；冷却使用琥珀状态；空槽置灰、隐藏费用并禁止操作。
- 修复空槽在后续装备术式后，行动点费用章仍保持隐藏的复用缺陷。

## 自动验证

- Funplay 身份门禁：`Application.dataPath = E:/数据库/OCC_Codex/UnityProject/Assets`；活动场景为 `Assets/Scenes/CombatPrototype.unity`，修改前场景未脏。
- 编译：0 error，0 warning。
- 聚焦 EditMode：`FormalUiThemeTests.SpellSlots_PresentKeyIconNameAndTwoResourceCostsAsOneReadableCard`，1/1 Passed；最终空槽复用修复后再次通过。
- 全量 EditMode：706/706 Passed。
- PlayMode：1/1 Passed。
- 运行时结构：8 张卡均为 `(268,76)`；名称 `(52,-4)/(208,40)`、识别轨 `(6,-6)/(40,64)`、费用带 `(52,-42)/(208,28)`。
- 运行时状态审计：空槽仍可操作数量 `emptyEnabled=0`；可见文字高度越界 `clipped=0`。
- Console：0 error；退出 Play Mode 后 dirty scene 0；未保存场景或存档。
- `occ_art_contract_v1.json` 已同步卡高、识别轨、名称槽、费用带、费用章和状态章尺寸并通过 JSON 语法解析。

## 双分辨率与状态接触

- 正常／1920×1080：`UnityProject/Library/FunplayMcp/Screenshots/UI-POLISH-85-spell-card-normal-clean-1920x1080.png`
- 正常／960×540：`UnityProject/Library/FunplayMcp/Screenshots/UI-POLISH-85-spell-card-normal-clean-960x540.png`
- 个人魔力不足：`UnityProject/Library/FunplayMcp/Screenshots/UI-POLISH-85-spell-card-shortage-1920x1080.png`
- 当前选中术式：`UnityProject/Library/FunplayMcp/Screenshots/UI-POLISH-85-spell-card-selected-1920x1080.png`

正常状态可同时辨认键位、图标、名称与双费用；魔力归零时只有个人魔力费用数字和识别轨转为危险色；选中术式同时强化卡体与识别轨；空槽保持八卡节奏但不再伪装成可执行操作。
