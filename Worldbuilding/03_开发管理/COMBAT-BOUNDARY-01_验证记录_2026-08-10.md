# COMBAT-BOUNDARY-01 验证记录（2026-08-10）

## 范围

- 共用战斗信息、敌方计划与开发入口边界；未修改 AI 数值、战斗规则、场景、存档或正式像素资产。

## 自动化覆盖

- `CombatBoundaryTests`：行动可用性查询委托、意图与执行同一计划、失效、默认开发入口关闭、展示模型不公开命令。
- 既有 `CombatInformationPresentationTests` 与 `CombatHoverInformationTests` 改为通过 `EnemyTurnPlanBook` 获得公开意图，不允许展示层调用 AI。
- `FormalArtRegistryTests` 现覆盖 5 个已批准 `FormalIntentIcons32` 映射；目标模块按公开计划签名显示攻击、施放、移动、据守或交互图标。所有图标均复用已有 Point/Clamp、硬 Alpha、PPU32 的正式资源，未引入未 QA 的位图。

## 入口隔离

- 训练靶场的启动、选择、预览、执行与审计 API 也要求 `DeveloperBuildGate.IsEnabled`；即使外部组件仍持有 Bootstrap，也无法在默认/Release 触发开发流程。

## Unity 健康检查

- Funplay `request_recompile → wait_for_compilation → get_compilation_errors`：0 error / 0 warning。
- 活动 `CombatPrototype.unity`：`isDirty=false`，编辑器非 Play Mode。
- Console：仅发现既有 UnityConnect Token Exchange 网络错误，与本改动无关。

## 环境门槛

Funplay 当前连接的工程副本未包含本工作树新增的 `CombatBoundaryQueries` 与 `DeveloperBuildGate`，不能将其 EditMode 结果计入本工作树。待 Unity Editor 绑定本工作树后，先运行新增聚焦测试与全量 EditMode；随后在获得用户授权时进行双分辨率 Play Mode 视觉验收。
