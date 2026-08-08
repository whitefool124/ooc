# OCC 通用法宝内容包技术方案 v0.1

## 1. 范围与结论

`ARTIFACT-PACK-20` 属于剧情/肉鸽共用战斗内容。正式目录固定为 `F-T01` 与 `G-T01–G-T19`：19 件通用、1 件火倾向。超凡来源只有以太素；剧情探索不增加倒计时或时间压力。

实现采用“目录数据 + 通用执行器 + 显式反应钩子”，不为 20 件复制 20 个施放类。`ArtifactDefinition` 保存稳定 ID、物品规格、目标、公开字段、动作/VFX 语义、内容来源与效果序列；`ArtifactEngine` 统一完成 Preview/Execute。

## 2. 运行时结构

- `ArtifactCatalog`：20 件单一数据源；`ItemCatalog` 从目录派生物品定义与正式图标路径。
- `ArtifactBattleState`：保存标记格、定锚、远程折返、火场、诱导物与延迟资源；构造时挂接当前 `CombatState`。
- `ArtifactEngine.Preview`：统一检查边界、范围、视线、目标阵营/占用、AP、次数和效果前置条件；返回固定目标格、单位、友军风险和签名。
- `ArtifactEngine.Execute`：按目录效果顺序结算 AP、生命/护盾/魔力、状态、位移、行动条、地形/物件、部署物与公开反噬，生成有序 `ArtifactStep` 与确定性签名。
- `ExecuteInventory`：只在 Preview 合法且 Execute 成功后扣实例次数；耗尽由 `CombatState.ConsumeInventoryItem` 移出背包并清快捷栏。
- `CombatResolver`：敌人进入标记格时触发截击铃；远程武器/技能伤害进入护盾结算前触发棱返调节器；强制位移查询定锚支架，只有实际抵消时扣次。

## 3. 背包、获取与存档

- 法宝实例沿用 `InventoryContainerState`：独立实例 ID、位置、旋转、剩余次数、来源顺序与快捷槽；同名不堆叠。
- 普通/精英/宝藏/首领节点由 `ArtifactRewardPool` 按来源和稀有度确定性抽取；商店、事件、战利品使用声明过对应来源的条目。
- 抽取使用混合运行种子与法宝 ID 的 FNV-1a 稳定键，避免等长 ID 在所有种子下保持同一相对顺序。
- `map9` 保存完整背包与快捷槽；`map8` 显式迁移；未知定义/坏档先备份并禁止静默覆盖。

## 4. 正式表现与 UI

- 战斗快捷栏武装主动法宝后复用棋盘目标预览；被动 `G-T13` 装备即待机，不出现点击耗次路径。
- `CombatVisualFeedback.NotifyArtifact` 把通用结算步骤映射到伤害、恢复、护盾、状态、移动、物件与环境反馈，并播放施放动作。
- 战斗背包与奖励卡显示名称、稀有度、尺寸/重量、剩余次数、来源、代价、目标、效果、风险/反制；隐藏稳定 ID、slug 与枚举。
- 行动档案可循环查看本局拥有法宝，并显示名称、剩余次数、来源、代价、目标、效果与风险。
- 每件法宝使用 `Art/FormalArtifactIcons32/<slug>` 独立正式图标；Importer 固定 Sprite/Single、Point、Clamp、PPU32、无 mipmap。

## 5. 安全边界

- 不修改场景 YAML，不保存场景；脚本/资源变更后由 Funplay 重编译、跑测试、检查 Console 与场景 dirty 状态。
- 不修改 `Library/`、`Logs/`、缓存、生成工程文件或现有人物资产。
- 玩法数字以 `OCC_通用法宝内容包_v0.1.md` 为源；技术层不得通过隐藏概率、随机故障或未公开资源改变条目身份。

