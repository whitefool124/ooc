# OCC BUG-COMBAT-SPAWN-67 弱遭遇出生格修复验证

日期：2026-08-29

## 现象与根因

进入分配到 `weak_arbalist_calibration` 的地图节点时，`CombatState` 抛出 `Unit hero has an invalid starting position`。W2 灰盒按策划将英雄放在 `(4,7)`；该遭遇绑定到 `gatehouse` 后，运行时虽然替换了 W2 的出生与地形，却错误保留了基础关卡的永久地台阻挡，而基础阻挡恰好包含 `(4,7)`。

策划源 `OCC_首区遭遇池与空间灰盒_v0.1.md` 已将 W1–W4 定义为完整 12×9 空间语法，因此修复完整布局的几何所有权，不移动英雄、不删除运行时单位，也不修改敌群、数值、节点或存档。

## 修复

- `RogueliteEncounterLayout` 增加独立 `BlockedPositions`，默认空集合。
- `CombatSceneSessionBuilder.BindEncounterToLevel` 在有完整布局时采用布局自己的 `Width`、`Height`、`Terrain` 与 `BlockedPositions`。
- 无独立布局的强战、精英与首领继续保留基础关卡尺寸及永久阻挡。

## 回归覆盖

- `EveryFormalEncounterPackage_BindsAndBuildsWithValidUniqueUnitSpawns`：遍历全部正式遭遇包，断言构造成功、单位在图内、出生不阻挡且不重叠。
- `WeakW2Layout_DoesNotInheritGatehouseDaisBlockersAcrossLayoutBoundary`：锁定本次 `(4,7)` 回归。
- `EncounterWithoutLayout_PreservesBaseMapDimensionsAndPermanentBlockers`：防止修复误删强战基础地图几何。

## 验证结果

- 精确 Funplay 构建：`weak_arbalist_calibration`、`gatehouse`、12×9、`hero=(4,7)`、`heroBlocked=False`、`baseBlockerInherited=False`、`units=3`。
- 新增聚焦回归：3/3 PASS。
- `RogueliteEncounterPoolTests`：13/13 PASS。
- 全量 EditMode：661/661 PASS。
- Unity 编译：0 error，0 warning。
- 修复后最近 180 秒 Console：0 error。
- `CombatPrototype.unity`：dirty false；Play Mode false；未保存场景。
