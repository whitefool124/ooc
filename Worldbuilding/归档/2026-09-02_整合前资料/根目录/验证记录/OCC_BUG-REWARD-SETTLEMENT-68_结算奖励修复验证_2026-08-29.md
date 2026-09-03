# BUG-REWARD-SETTLEMENT-68 结算奖励修复验证

## 范围

- 修复肉鸽结算界面的新版／旧版火术式提交路由。
- 修复学院装备奖励的 6×10 背包容量门禁、合法落位与 DTO 回写。
- 修复同一局连续迁移重选时卡片不刷新、输入保持锁定的问题。
- 修复长术式说明越过奖励卡覆盖相邻候选的问题。
- 不修改奖励池内容、术式／装备数值、地图、战斗、经济、正式资产或存档版本。

## 根因与修复

1. `RogueliteSettlementPresentation.TryClaim` 原先只按奖励 ID 是否存在于 `FireSpellCatalog` 决定调用 `ClaimMapFireSpell`。`Rogue11` 的正式火术式同样使用这些 ID，但候选属于 `CurrentRewards` 而非旧 `CurrentFireSpellChoices`，因此点击必然被旧接口拒绝。现仅当运行不是 `Rogue11` 且 ID 确实来自 `CurrentFireSpellChoices` 时调用旧接口，其余候选统一走 `ClaimMapReward`。
2. `RefreshNow` 虽能通过 `SettlementPresentationModel.RewardKey` 发现候选变化，却只在面板为空或种子变化时重建。现只要模型实际变化且奖励仍可见就重建面板，`Show` 同时复位 `claimPending`；编辑态销毁使用 `DestroyImmediate`，运行态继续使用 `Destroy`。
3. 学院装备原先直接追加坐标为 `-1,-1` 的 `EquipmentInstanceDto`，满背包也不拒绝。现领取与 UI 可用性都通过 `RogueEquipmentRuntime` 的同一 first-fit 合同；成功后写回合法坐标，失败时不增加确定性计数器、不写 DTO。
4. 结算标签原先强制水平／垂直 `Overflow`。现使用水平 `Wrap` 与垂直 `Truncate`，在统一 Canvas 缩放下保持卡片边界，避免 1920×1080 与 960×540 中跨卡覆盖。

## 自动化验证

- 聚焦 EditMode：5/5 PASS。
  - `Rogue11FireSpellCard_UsesUnifiedRewardClaimInsteadOfLegacyFireClaim`
  - `ConsecutiveLegacyReselections_RebuildCardsAndRestoreInput`
  - `SettlementLabels_WrapAndTruncateInsideTheirAssignedCards`
  - `M6FormalMapSettlement_OffersAcademyEquipmentAndPersistsItAsRogue11Instance`
  - `M6FullRogueBackpack_BlocksEquipmentRewardWithoutAppendingUnplacedDto`
- 全量 `OCC.Combat.EditModeTests`：665/665 PASS，失败 0、跳过 0。
- Unity 编译：错误 0、警告 0。
- Unity Console：error 0。
- `CombatPrototype.unity`：`isDirty=false`；dirty scene 0。
- `git diff --check`：通过，仅保留工作树既有换行提示。

## 场景与操作记录

- 身份门禁：`Application.dataPath=E:/数据库/OCC_Codex/UnityProject/Assets`。
- 活动场景：`Assets/Scenes/CombatPrototype.unity`。
- 为导入外部脚本改动退出了用户原有 Play Mode；修复后未重新进入 Play Mode。
- 未保存场景，未修改正式资产或用户无关改动。
