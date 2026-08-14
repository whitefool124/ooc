# OCC 技术决策记录

## ADR-001：表现组件通过职责接口访问组装入口

- **状态：** ACCEPTED
- **日期：** 2026-08-14
- **归属：** 剧情模式与肉鸽模式共用
- **问题：** 正式 HUD、肉鸽页面、结算、背包、交互层和反馈组件直接持有 `CombatPrototypeBootstrap`，使任何页面都能访问并扩展完整战斗/肉鸽状态，Bootstrap 因而持续成为服务定位器与万能类。
- **约束：** 不改变玩法、存档、场景布局或运行时生成 UI 的生命周期；必须兼容当前由 Bootstrap 在 `OnEnable` 中组装组件的方式。
- **决定：** 每个表现组件只依赖一个按其实际用途定义的宿主接口；Bootstrap 作为 composition root 实现这些接口。表现组件不得声明 `CombatPrototypeBootstrap` 字段。场景入口可定位具体组装组件，但不得将其保存为跨层状态依赖。
- **备选：** 一次性把 Bootstrap 拆成完整 MVC/DI 容器。未采用，因为当前工作区跨地图、战斗、UI 与美术，整体重写会扩大回归面并使用户现有改动难以追溯。
- **影响文件/系统：** `PresentationHostContracts`、`CombatPrototypeBootstrap`、正式 HUD、肉鸽 UI、结算、背包、交互层、战斗反馈、开发控制台与场景 HUD 绑定器。
- **验证：** `CombatBoundaryTests.PresentationComponents_DependOnNarrowHostsInsteadOfBootstrap` 反射检查字段类型；`Bootstrap_ImplementsEveryPresentationHostContract` 检查组装接线；全量 EditMode 与 Funplay 编译/Console 验证。
- **回滚方式：** 恢复各组件的具体 Bootstrap 参数和字段；不涉及序列化数据或场景变更。
- **复核触发器：** Bootstrap 中的战斗会话、肉鸽流程和战场输入均提取为独立控制器后，评估是否由控制器直接实现接口并移除 Bootstrap 转发。
- **替代记录：** 无。

## ADR-002：战场表现迁移到正式 UGUI

- **状态：** ACCEPTED
- **日期：** 2026-08-14
- **归属：** 剧情模式与肉鸽模式共用
- **问题：** 战斗 HUD 已使用 UGUI，但战场仍由 Bootstrap.OnGUI 绘制和接收输入，造成两套 UI 技术栈、排序/裁切/事件系统分离，并把大量绘制职责留在组装入口。
- **约束：** 保持 1920×1080、左侧战场 75% 和右侧 HUD 25% 基准；不改变格子、战斗命令、地图坐标、单位数值或正式资产；迁移期间不保存场景。
- **决定：** 直接将战场迁移为运行时生成的正式 UGUI 视图。完成替代后关闭旧 IMGUI 战场，不长期维护双实现。用户明确允许进入 Play Mode 做交互和视觉回归。
- **备选：** 保留 IMGUI 并先提取 renderer/session。未采用，因为用户选择尽早统一 UI 技术栈并接受更大的验证范围。
- **影响文件/系统：** CombatPrototypeBootstrap、表现宿主合同与组装注册表、新 UGUI 战场视图、战场输入、CombatVisualFeedback、FormalCombatHud 和相关测试。
- **验证：** EditMode `409/409 passed`、PlayMode `1/1 passed`；Play Mode 现场确认 108 格 UGUI 战场、左右键格子交互、中键平移与 1920×1080/960×540 显示；编译 0 error/0 warning、Console 无项目错误、0 dirty scenes。
- **回滚方式：** 通过版本控制恢复迁移前的旧 OnGUI 实现并移除 UGUI 注册；旧实现不再留在当前源码中，避免双实现继续漂移。不涉及存档或场景变更。
- **复核触发器：** UGUI 视图无法在现有资源和事件合同下保持等价交互，或性能证据显示运行时节点方案不可接受。
- **替代记录：** 无。
