# OCC 雨后灯庭方格适配 v3 QA

日期：2026-09-04

## 结论

- 水面 v3：机器门禁、用户审美、Unity Importer 与 B1 运行时接触均 `PASS`，状态 `FORMAL`。
- 灯藤 v3：机器门禁、用户审美、Unity Importer 与角色站入验证均 `PASS`，状态 `FORMAL`。
- 用户于 2026-09-04 暂时批准导入；两项已使用独立稳定 GUID 接入正式资源注册表。

## 水面 v3

- 32×32，6 色，全不透明硬 Alpha。
- 单块方形石板上覆盖无框薄水膜，断裂反光不形成居中图标。
- 已完成 1×、4×、灰阶、棋盘格与五格水沟／正式主角接触图。

## 灯藤 v3

- 32×32，8 色，硬 Alpha；可见边界 `(2, 2, 29, 30)`，满足至少 2px 安全边。
- 绿色语义像素 315，暖黄语义像素 83，机器合同通过。
- 北侧藤冠较高、左右侧臂围合、南侧前缘低矮；中心为真实透明方形站位空腔。
- 已使用正式 64px 主角完成站入接触图，并完成 2×5 重复铺设检查。

## Unity 导入与运行时证据

- 水面：`Assets/Game/Resources/Art/FormalFirstBattle32/rain_court_water.png`，GUID `1adf78f541ccd9c4685f219d5e6e5058`。
- 灯藤：`Assets/Game/Resources/Art/FormalFirstBattle32/lamp_vine.png`，GUID `161cf1ac42503eb43abf6432c2e0905c`。
- 两项 Importer：Sprite / Single、32 PPU、Point、Clamp、Uncompressed、关闭 Mipmap、Alpha From Input。
- 全量 EditMode：735/735 通过。
- Play Mode 流程：启动页 → 新游戏确认 → 固定出身 → 学院地图 → B1“雨后灯庭”。
- 角色以真实战场双击由 `(1,7)` 经 `(3,5)` 进入 `(4,4)`，行动点 3→1，底层确认 `IsLampVine=True`；画面同时显示 C7-G7 连续方形浅水与 E1-F5 中空灯藤。
- 运行时截图：`Worldbuilding/归档/2026-09-04_首次战斗雨后灯庭/美术候选/v3/evidence/rain_lantern_court_v3_unity_runtime.png`。
- 验收后已退出 Play Mode，恢复测试前存档；未保存场景。
