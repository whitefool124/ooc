# UI-FLOW-BRIEFING-69 行动档案合并验证

- 日期：2026-08-29
- 范围：肉鸽地图战斗节点详情、地图战斗进入编排、既有正式学院档案装饰接触。
- 工作树：`E:/数据库/OCC_Codex`
- Unity：`Application.dataPath=E:/数据库/OCC_Codex/UnityProject/Assets`

## 实现结果

- 战斗节点详情与战前简报合并为“学院行动档案”，同页公开行动目标、敌方编成、空间风险、失败后果、风险、时序、预计生命／魔力恢复、阈值与成功奖励。
- 流程由“节点详情 → 战前简报 → 开始战斗”缩短为“行动档案 → 开始行动”；一次确认同步选择节点、保存推进、构建战斗并进入 `CombatFlowPhase.Active`。
- 返回学院地图不会选择节点，不消耗时序、金币、贡献或节点内容。
- 普通战斗接入已验收 `teaching_record` 与 `teaching_chalk_clip`；精英／首领接入 `sealed_dossier` 与 `sealed_red_clip`；奖励区接入 `reward_brass_tag`。全部来自现有 FORMAL 资源，未生成新图片、未绕过 manifest／Importer 合同。
- 非地图开发／剧情简报仍保留原有 `DrawBriefing`，非战斗节点交互未改。

## 自动验证

- `AcademyNodeRoomArtCoverageTests`：3/3 passed；覆盖节点图标、导航图标、合并页章节横幅／角标及直接进入合同。
- 全量 EditMode：666/666 passed，0 failed，0 skipped。
- Funplay 编译：0 errors，0 warnings。
- Unity Console：0 error entries。
- `Assets/Scenes/CombatPrototype.unity`：`isDirty=false`。
- 最终状态：`Application.isPlaying=false`；本任务未进入 Play Mode、未保存场景。
