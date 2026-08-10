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

- 本工作树 Unity 6000.5.2f1 CLI 完成全量导入和脚本编译，正常退出；随后全量 EditMode **309/309 passed**、0 failed、0 skipped。首轮 308/309 只暴露资产审计冻结数仍为 184；将 5 个已批准的意图图标纳入正式注册后更新为 189，复跑全绿。
- CLI 测试未进入 Play Mode，`CombatPrototype.unity` 没有 Git diff。日志仅记录 Unity 公共 CDN 配置请求超时，未出现项目 C# error 或测试异常。
- Funplay 旧连接仍可报告其已打开副本 0 error / 0 warning、场景 `isDirty=false`，但本记录不再将其作为本工作树测试证据。

## 剩余视觉门槛

未获授权，不进行 Play Mode；1920×1080 / 960×540 的人工视觉验收仍需用户明确授权。该门槛不影响本次 EditMode、资源导入、编译与场景文件完整性证据。
