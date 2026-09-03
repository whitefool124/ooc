# OCC UI-POLISH-81 战斗菜单、术式栏与资源反馈验证

- 日期：2026-09-02
- 场景：`Assets/Scenes/CombatPrototype.unity`
- 工程：`E:/数据库/OCC_Codex/UnityProject`
- 归属：剧情／肉鸽共用战斗 UI 表现层

## 截图问题定位

1. 右键菜单使用 76px 按钮，却把动作名和资源说明分别放入两个 40px 文字槽；第二行字形底边只剩 4px，进入 6px 深色底框。
2. 138×60 术式槽同时堆叠 48px 图标、键位数字和两枚 56×36 费用章，键位、图标、费用符号与数字互相遮挡；16px 宽费用数字槽在 Play Mode 实际生成 0 个字形顶点。
3. 生命／护盾／个人魔力轨道仅 14px 高，九宫格边框占用后内部色带过细；只有数值，没有明确百分比、四分位刻度或变化落点反馈。

## 实装结果

- 右键菜单资源说明由 Y=-40 上提至 Y=-32，标题提示由 Y=-42 上提至 Y=-40。
- 每个菜单行动行实测：动作名字形顶部安全距 12px、两行字形间距至少 4px、资源说明字形底部安全距 12px。
- 术式按钮改为 138×52，第二排从 Y=-110 调整为 Y=-104，末排不再接触术式组底框。
- 术式图标固定 32px；键位进入 28px 深色角标；行动点／个人魔力费用分别使用 40×28 左图标右数字费用章。
- 费用数字保留 24px 字号，使用 24×40 生成槽和浅色反差底；自动回归与 Play Mode 均确认字形顶点非零。
- 三条资源轨道增厚至 20px，增加 25%／50%／75% 刻度；生命和魔力显示“当前 / 上限 · 百分比”，肉鸽护盾显示“当前 · 无上限”。
- 资源变化继续使用宽度缓动，并在新比例位置显示红色损失／绿色恢复落点闪标。

## 验证

- 定向 EditMode：4/4 通过。
- 全量 EditMode：704/704 通过。
- PlayMode：1/1 通过。
- 编译：0 error。
- Console：0 error。
- 场景：`CombatPrototype.unity`，dirty=false；未保存场景。
- 1920×1080：`UnityProject/Library/FunplayMcp/Screenshots/UI-POLISH-81-layout-and-resource-feedback-1920x1080-verified.png`
- 960×540：`UnityProject/Library/FunplayMcp/Screenshots/UI-POLISH-81-layout-and-resource-feedback-960x540-verified.png`
- 双分辨率图中的三项右键菜单与八槽术式栏为 Play Mode 布局接触注入，只改变当次运行时显示内容；资源损失反馈通过当次 Play Mode 主角生命 18→14 触发，未写入场景或存档。
- 临时 `OCC 960x540` GameView 规格已删除，编辑器恢复内建 `Full HD (1920x1080)`。

## 涉及文件

- `UnityProject/Assets/Game/Runtime/Presentation/FormalBattlefieldView.cs`
- `UnityProject/Assets/Game/Runtime/Presentation/FormalCombatHud.cs`
- `UnityProject/Assets/Game/Tests/EditMode/FormalUiThemeTests.cs`
- `Tools/OCCArt/occ_art_contract_v1.json`
- `Worldbuilding/05_美术与音频/OCC_UI学院档案视觉规范_v0.1.md`
