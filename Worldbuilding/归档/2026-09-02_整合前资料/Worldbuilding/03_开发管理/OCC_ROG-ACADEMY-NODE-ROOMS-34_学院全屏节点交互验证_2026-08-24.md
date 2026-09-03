# ROG-ACADEMY-NODE-ROOMS-34 学院全屏节点交互验证

日期：2026-08-24  
范围：仅肉鸽模式学院第一层；未保存场景，未改变节点数值与结算。

## 结果

学院地图节点已从右侧小详情升级为统一的独立全屏节点房间。所有已识别节点先在地图选择，再打开全屏档案；页面统一提供身份、状态、风险、时序、恢复、奖励、进入与返回。抵达后的非战斗节点显示公开选项，并提供“暂不结算，退出节点”；战斗、精英和首领进入原有全屏简报，简报仍可返回地图；完成节点进入安全回访页且不会重复产出。

## 交互闭环

1. 地图选择节点，不立即结算。
2. 点击“打开”进入全屏节点档案。
3. 未抵达节点核对风险后选择“进入节点”或“返回学院地图”。
4. 抵达非战斗节点后选择公开选项，或“暂不结算，退出节点”。
5. 战斗节点进入全屏战前简报；开始战斗前仍可返回地图。
6. 已完成节点只显示安全回访；工坊回访可继续进入学院整备。

## 节点类型与美术

九类节点均使用独立正式类型图标和语义主题：起点、普通战、精英、事件、工坊、商店、医务、宝藏、首领。页面复用已通过门禁的 `FormalNodeIcons32/types`、风险／时序／生命资源图标，以及 continue／confirm／back／close 导航图标；没有将未审批 AI 原料导入 Unity。原生 32px 类型图在参考画布显示为严格 4×，960×540 为严格 2×。

专项美术应用合同见 `Worldbuilding/05_美术与音频/OCC_学院节点全屏房间美术简报_v0.1.md`。

## 实机证据

- 战斗当前节点：全屏档案显示“进入战前简报”和“返回学院地图”；进入后简报正常显示“开始战斗／返回地图”。
- 未抵达工坊：全屏档案显示“进入节点／返回学院地图”。
- 抵达工坊：显示付费与免费两项公开选择，以及“暂不结算，退出节点”。
- 退出工坊后权威状态为 `node=field_workshop|gold=3|contribution=0|time=0|completed=supply_checkpoint`，证明退出没有结算或消耗。
- 已完成商店：显示安全回访且明确不会重复奖励、战斗或时序结算。
- 双分辨率截图：`UnityProject/Temp/ROG-NODE-ROOMS-34/final_node_room_1920.png`、`final_node_room_960.png`、`combat_room_1920.png`、`combat_room_960.png`、`shop_revisit_960.png`。

## 验证门禁

- 身份：`Application.dataPath=E:/数据库/OCC_Codex/UnityProject/Assets`；场景 `Assets/Scenes/CombatPrototype.unity`。
- 编译：0 error。
- 聚焦 EditMode：25/25 passed。
- 全量 EditMode：626/626 passed。
- PlayMode：1/1 passed；Console 无 error。
- 最终状态：`dirty=false`、`playing=false`，未保存场景。
