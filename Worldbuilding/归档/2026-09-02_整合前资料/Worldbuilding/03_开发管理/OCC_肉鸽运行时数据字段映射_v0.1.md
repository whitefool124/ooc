# OCC 肉鸽运行时数据字段映射 v0.1

> 状态：已按 `rogue11` 实装并通过 M0–M7 验收，2026-08-16。策划语义以 `OCC_肉鸽模式策划总纲_v0.2.md` 及其 L2 源文件为准；本表只决定运行时如何承载和迁移。

## 1. 字段命名原则

- 稳定内容使用 `snake_case` ID；运行时 C# 属性可用 PascalCase，但序列化名不得随类型重命名漂移。
- 定义数据描述“这类物品／术式是什么”；实例数据只保存会因本局生成、强化、使用或选择而变化的内容。
- 派生值不入存档：当前装备盾合计、有效射程、最终伤害、背包占用格和可用奖励候选均由定义与实例重算。
- 被删除的旧字段只能出现在 `LegacyMap10Dto` 和迁移报告，不能进入新战斗状态。

## 2. 战斗与单位字段

| 旧字段／入口 | 新字段／结构 | 迁移处理 |
| --- | --- | --- |
| `UnitState.Armor`、`EffectiveArmor` | 无 | 肉鸽删除；旧值不转换为盾或减伤 |
| `UnitState.Block` | 无 | 删除；不转换为随机格挡或固定减伤 |
| `UnitState.MaxShield` | 无全局上限 | 普通盾只保存战斗中 `current_shield`；定义可自行写单次效果上限，不能复活全局帽子 |
| `UnitState.Shield` | `combatant.current_shield` | 仅战斗内存在；进入／离开战斗为 0，存档不保存 |
| `WeaponDefinition.ArmorPierce`、`SkillModifierType.ArmorPierce` | 明确的物件破坏效果，或 `BreakStance` 效果 ID | 不做数值穿甲换算；逐条人工映射，无合法语义则删除 |
| `StatusType.ArmorBreak` | `StatusType.BreakStance` | 新状态持续到目标下一次自己回合结束；施加时清盾，持续期间禁盾；不易伤、不加伤 |
| `flatIncomingDamageReduction`、普通固定减伤规则 | `percentage_reduction_effects[]` | 只有获批条目可存在；必须写类别、比例、来源、适用伤害、开始／结束时点 |
| 掩体 `DamageReduction` | `cover_kind`、射线／路径职责、`cover_shield_amount` | 轻／重合法触发分别给 2／4 盾；不进入伤害公式 |
| `AttackPreview.CoverReduction/ArmorReduction/BlockReduction` | `raw_damage`、`percentage_reduced`、`shield_absorbed`、`health_damage`、`hit_segments[]` | 预览与实际调用同一个纯结算函数 |

### 2.1 伤害包

`DamagePacket` 至少包含：

| 字段 | 含义 |
| --- | --- |
| `packet_id` | 单次结算稳定标识；同一次命中的组成伤害共享一个包 |
| `source_unit_id`、`target_unit_id` | 来源与目标；环境来源允许来源单位为空但必须有来源效果 ID |
| `source_effect_id` | 武器动作、术式、状态或环境的稳定 ID |
| `components[]` | 物理／元素等组成及原始值，用于日志和条件，不各自独立扣盾 |
| `tags[]` | 近战、远程、爆炸、地面、多段序号等显式标签 |
| `reduction_effects[]` | 当前合法的少数百分比减伤来源快照 |
| `segment_index`、`segment_count` | 真正多段攻击的逐包顺序 |

`DamageResolution` 至少返回 `raw_total`、`reduction_rate`、`after_reduction`、`shield_before`、`shield_absorbed`、`health_before`、`health_damage`、`target_defeated`。所有 UI 预览和战斗日志复用该结果结构。

## 3. 护盾与状态生命周期字段

| 新字段 | 规则 |
| --- | --- |
| `current_shield` | 当前战斗普通盾总值，不设全局上限，不跨战 |
| `shield_source_id` | 装备实例、术式、掩体或状态的稳定来源 |
| `shield_trigger_turn` | 判定同一来源本回合最多触发一次 |
| `shield_event_kind` | `Granted`、`PreventedByBreakStance`、`Absorbed`、`ClearedAtTurnStart`、`Wasted` |
| `break_stance_expires_after_turn_owner_id` | 目标完成下一次自己回合后解除；重复施加只刷新该截止点 |
| `cover_shield_claimed_turn` | 轻／重掩体共享的每个自己回合一次额度 |

回合开始顺序固定为：破势有效性检查 → 清遗留普通盾 → 按装备稳定槽位顺序结算装备盾 → 其他回合开始盾 → 记录被阻止来源。回合结束才处理本回合到期的破势。

## 4. 术式字段

| 旧结构 | 新结构 | 迁移处理 |
| --- | --- | --- |
| `SkillOne`、`SkillTwo` | `equipped_spell_ids[8]` | 新局前四槽装四项基础术式，后四槽为空 |
| `equippedFireSpells[2]` | 同上 | 旧有效奖励术式按旧槽顺序放入第 5、6 槽；基础四项不得被覆盖 |
| 武器相容性决定“能否拥有” | `compatibility_tags[]` 只决定具体术式合法装备条件 | 四基础术式完全忽略武器相容性；奖励生成先过滤合法候选 |
| 旧法术冷却临时状态 | `spell_cooldowns[spell_id]` | 只在战斗状态保存，战后清空，不进肉鸽跨节点存档 |

术式定义至少保存：`spell_id`、`display_name`、`element`、`role`、`rarity`、`ap_cost`、`mana_cost`、`cooldown_own_turns`、`targeting`、`range`、`line_of_sight_rule`、`rules[]`、`compatibility_tags[]`、`reward_sources[]`、`reward_eligible`、`equivalence_group_id`、`catalog_version`。

奖励规则明确为：四基础术式 `reward_eligible=false`；火系正式 60 项全部 `reward_eligible=true`，不设置固定总量裁剪字段。

## 5. 装备定义、实例与槽位

`EquipmentSlot` 固定为：`MainHand`、`OffHand`、`Head`、`Chest`、`Hands`、`Legs`、`Backpack`、`AetherCore`、`Conduit`、`Accessory1`、`Accessory2`。

### 5.1 定义数据

每个装备定义至少保存：

`definition_id`、`display_name`、`slot`、`handedness`、`allowed_rarities`、`width`、`height`、`rotatable`、`base_weight`、`base_aether_load`、`base_action_ids`、`fixed_effect_ids`、`affix_pool_ids`、`upgrade_node_definitions[]`、`source_stage`、`source_types[]`、`unique_group_id`、`content_version`。

### 5.2 实例数据

每个装备实例至少保存：

`instance_id`、`definition_id`、`equipped_slot`、`rarity`、`power_band`、`mutable_affix_ids[]`、`upgrade_branch_ids[]`、`reforge_count`、`resolved_weight`、`resolved_aether_load`、`source_stage`、`source_type`、`acquired_order`、`backpack_x`、`backpack_y`、`rotated`。

说明：固定效果和可选强化定义来自装备定义；实例只保存已经选择的分支。若为了快照审计冗余保存 `fixed_effect_ids`，加载时必须与定义核对，不允许实例偷偷改变固定效果。

严禁装备实例保存：`durability`、`max_durability`、`armor`、`block_chance`、跨战 `shield_balance`。现有 `EquipmentState.Durability` 仅保留在旧格式读取器，不能复制进新实例。

## 6. 背包、快捷栏与战术道具

6×10 网格、矩形占格、90° 旋转和独立实例继续复用现有实现。字段边界如下：

| 类型 | 保存内容 | 禁止内容 |
| --- | --- | --- |
| 养成装备 | 装备实例字段；未装备时保存网格位置，已装备时保存槽位 | 次数、耐久、维修状态 |
| 战术消耗道具 | `instance_id`、`definition_id`、网格位置、`charges_current`、`charges_max`、`source_stage`、`source_type` | 装备词条、强化分支 |
| 快捷栏 | `item_quickbar_instance_ids[4]` | 装备实例、术式、超过 4 格的隐藏引用 |

旧 8 格快捷栏迁移时按槽位 0→7 扫描，把前 4 个仍存在且属于战术消耗道具的实例放入新快捷栏；其余实例仍留在背包，不销毁、不自动使用。

## 7. 肉鸽局状态与资源

| 旧字段 | 新字段 | 迁移处理 |
| --- | --- | --- |
| `Parts`、`Aether`、`Supplies`、`ScoutingBeacons`、`AccessCards` | `gold`、`stage_contribution`；核心许可另作进度标志 | 旧数值无一一语义，不静默折算；记录迁移警告。许可只映射明确的任务／节点完成事实 |
| `CurrentHealth` | `current_health` | 保留并截断到新上限；若小于等于 0，不生成可继续局 |
| `CurrentMana` | `current_mana` | 保留并截断到 0–12 |
| `CurrentShield` | 无跨节点字段 | 丢弃并记录“普通盾归零”；新战斗从 0 开始 |
| `ItemQuickbar[8]` | `item_quickbar_instance_ids[4]` | 按第 6 节确定性压缩 |
| `EquippedWeaponId` | `equipment_slot_instance_ids` | 有正式定义映射才迁；枪械等不合学院时代／目录的旧 ID 转装备重选凭证 |
| `IsAetherCalibrated` | 明确的强化节点或一次迁移重选凭证 | 不再隐式增加护甲 |
| `HasCombatSnapshot` | 开发专用验证快照 | 正式局死亡后不得用其继续；普通盾不写快照外层状态 |

新局状态还必须明确保存：`save_version`、`run_id`、`seed`、`stage_id`、`stage_time`、地图访问／完成状态、`gold`、`stage_contribution`、`current_health`、`current_mana`、已掌握术式、8 槽配置、库存实例、11 槽装备引用、4 格快捷栏、固定种子计数器、待领取奖励／重选凭证和迁移报告 ID。

金币／阶段贡献的推荐迁移默认值为新局基线 `8／0`，因为旧原型资源不存在可靠汇率。旧资源不转换为金币，防止一次迁移制造不可验证的经济优势；这是开发期存档迁移规则，不是正式发行后的玩家补偿政策。

## 8. `map10` → `rogue11` 一次性迁移

1. 读取旧字符串但不原地覆盖，先生成带时间戳的备份和迁移报告。
2. 校验种子、节点、生命、魔力、库存和内容版本；解析失败则保留旧档并拒绝部分写入。
3. 添加四项基础术式到已掌握列表和前四槽；把最多两个仍合法的旧已装备奖励术式放入第 5、6 槽。
4. 对旧武器、法术和物品逐项查稳定映射表。无映射内容不猜测替代，转为对应重选凭证；背包实例只有在占格与类型仍合法时保留。
5. 普通盾归零；旧货币不折算；金币／阶段贡献采用 `8／0` 开发期基线；生命和魔力按新上限截断。
6. 8 格快捷栏确定性压缩为 4 格；未入栏实例保留在背包。
7. 校验新装备槽、背包重叠、双手占用、术式重复、词条互斥和内容版本。任何硬错误都不写 `rogue11`。
8. 成功后写新档并保存迁移报告；之后只读 `rogue11`。旧文件保留到 M7 验证完成，不自动删除。

迁移报告至少列出：保留字段、截断字段、丢弃的旧资源、归零护盾、映射内容、重选凭证、快捷栏变化、警告和新档校验摘要。

## 9. 校验门禁

- 任何肉鸽单位定义出现 `armor > 0`、`block > 0` 或默认掩体减伤即失败。
- 任何养成装备出现耐久、护甲、格挡率或跨战盾余额即失败。
- 术式装备数组长度不为 8、快捷栏长度不为 4、持久装备槽集合不完整即失败。
- 四基础术式缺失、进入奖励池或被武器相容性拒绝即失败。
- 火系正式奖励资格数量不为 60，或同次候选存在重复等价组即失败。
- 伤害预览与实际结算对同一 `DamagePacket` 结果不同即失败。
- 存档读取后出现第三货币、跨战普通盾、非法装备占手或背包重叠即失败。

## 10. 实装核对（2026-08-16）

- `RogueRunDto` 固定保存 8 个术式槽、11 个装备槽引用、4 格战术快捷栏、金币、阶段贡献、跨战生命／个人魔力与道具次数；不保存普通盾、装备耐久或旧五资源。
- 未装备养成装备以 `backpack_x`、`backpack_y`、`rotated` 往返 6×10 背包；已装备实例只以稳定实例 ID 绑定槽位。
- `RogueliteSaveGateway` 是正式存档写入口，只产生 `rogue11`；`LegacyMap10Migrator` 先保存原文备份和迁移报告再生成新 DTO。生产代码不存在 `map10` 写入器。
- 固定种子往返、迁移、装备／次数持久化和普通盾归零均由 EditMode 回归覆盖；最终证据见 `OCC_肉鸽运行时M7固定种子验证记录_2026-08-16.md`。
