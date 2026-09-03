# OCC 当前时代特色敌人扩展包实现矩阵 v0.1

## 1. 时代修订

现行依据为《以太：世界观与魔法体系圣经》0.2：主角 14 岁入学期接近第一次工业革命前，常规战争以冷兵器与弓弩为主；圣经第 1 节以后统历 742 年、铁路、枪炮和成熟工业军队属于失效粗稿。本矩阵只登记当前正史允许的敌人闭环。

| 旧草案 | 最终运行时 | 结果 |
| --- | --- | --- |
| `aether_sapper` / 破甲工兵 / 投射破障弹 | `sigil_mauler` / 刻印锤手 / 邻接碎甲锤印 | 旧 ID、名称、投射与爆破语义不可达；保留近战破甲职责 |
| `barrier_engineer` / 屏障工程兵 | `barrier_mender` / 屏障修补师 | 删除现代工程兵称谓和工具架语义；保留确定性护障支援 |
| `relay_hound` / 中继猎兽 | `tether_hound` / 缚环猎兽 | 删除机械中继语义；保留驯化原生魔法生物与接触束缚 |
| 活跃遭遇 `rifleman` / `sniper` | 盾卫、术师、突袭者、束缚术士和本包三敌人 | 九个活跃首区遭遇与默认回退均无现代枪械角色 |

## 2. 完整运行时闭环

| 敌人 | 数据合同 | AI 与反制窗口 | 技能/状态结算 | 活跃遭遇 | 资源与表现 | 自动化证据 |
| --- | --- | --- | --- | --- | --- | --- |
| `sigil_mauler` 刻印锤手 | HP14 / 甲1 / 盾0 / 速8；战锤 | 仅邻接且目标未破甲时施放；已有破甲则普攻/接近 | `enemy_sundering_sigil`；1 格/1 魔力/CD2；2 物伤+破甲2回合 | `depot_wreck`、`elite_foundry` | `FormalUnits64/sigil_mauler`；接触技能使用 Attack 动作；伤害+破甲 VFX/图标 | 合同、旧 ID 退役、AI 首次施放/重复回退、真实 Resolver、资源/Importer |
| `barrier_mender` 屏障修补师 | HP12 / 甲0 / 盾4 / 速7；手杖 | 选 4 格内最大护盾缺口；同缺口按单位 ID；无缺口则普攻/接近 | `enemy_ward_mend`；4 格/2 魔力/CD2；恢复4护盾，两次施放资源上限 | `signal_hub`、`elite_foundry` | `FormalUnits64/barrier_mender`；Cast 动作；目标 Recover 回弹+护盾恢复 VFX | 最大缺口、稳定 ID、真实恢复结算、资源/Importer |
| `tether_hound` 缚环猎兽 | HP10 / 甲0 / 盾0 / 速11；撕咬4 | 仅邻接且目标未束缚时扑咬；已有束缚则普通撕咬/接近 | `enemy_tether_pounce`；1 格/1 魔力/CD1；2 物伤+束缚1回合 | `depot_wreck`、`relay_raid` | `FormalUnits64/tether_hound`；扑咬使用 Attack 冲刺；伤害+束缚 VFX/图标 | 首次束缚、重复回退、真实 Resolver、资源/Importer |
| `shieldguard` 铭盾卫 | HP12 / 甲2 / 盾2 / 格挡2 / 速7 | 邻接且未迟缓时冲撞；已有迟缓则盾击/接近 | `enemy_shield_ram`；1 格/1 魔力/CD2；2 物伤+迟缓1 | `rail_patrol`、`signal_hub`、`gatehouse` | `shieldguard`；Attack + Slow | 状态窗口、真实 Resolver、资源/Importer |
| `pyromancer` 火矢术师 | HP12 / 甲0 / 盾1 / 速9 | 5 格内且未燃烧时施放；已燃烧则手杖/移动 | `fire_bolt`；5 格/2 魔力/CD1；5 火伤+燃烧2 | `rail_patrol`、`transmission_tower` | `pyromancer`；Cast + FireProjectile/Burning | 状态窗口、真实 Resolver、资源/Importer |
| `raider` 钩刃突袭者 | HP12 / 甲0 / 盾0 / 格挡1 / 速11 | 邻接且未束缚时牵制；已束缚则普攻/接近 | `enemy_hooking_strike`；1 格/1 魔力/CD2；3 物伤+束缚1 | `rail_patrol`、`relay_raid` | `raider`；Attack + Bound | 状态窗口、真实 Resolver、资源/Importer |
| `elite_vanguard` 刻阵先锋 | HP12 / 甲2 / 盾4 / 格挡2 / 速10 | 邻接且未破甲时压阵；已有破甲则重锤/接近 | `enemy_vanguard_crush`；1 格/2 魔力/CD2；4 物伤+破甲2 | `elite_foundry`、`core_approach`、`core_finale` | `elite`；Attack + ArmorBreak | 状态窗口、真实 Resolver、资源/Importer |
| `stone_snare` 石索缚师 | HP11 / 甲0 / 盾1 / 速8 | 3 格内且未束缚时甩索；已有束缚则手杖/移动 | `enemy_stone_snare`；3 格/2 魔力/CD2；1 物伤+束缚2 | `depot_wreck`、`transmission_tower`、`core_approach` | `stone_snare`；Cast + Bound | 状态窗口、真实 Resolver、母图/规范化/Importer |
| `lantern_revealer` 显影灯使 | HP11 / 甲0 / 盾2 / 速9 | 3 格内且未破甲时灯照；已有破甲则手杖/移动 | `enemy_revealing_lantern`；3 格/1 魔力/CD2；1 奥术伤+破甲2 | `signal_hub`、`transmission_tower`、`core_finale` | `lantern_revealer`；Cast + ArmorBreak | 状态窗口、真实 Resolver、母图/规范化/Importer |
| `rune_arbalist` 重弩手 | HP13 / 甲1 / 盾0 / 速6；重弩3/4格 | 5 格重矢优先；冷却时 4 格弩矢或移动 | `enemy_windlass_bolt`；5 格/1 魔力/CD2；5 物伤 | `relay_raid`、`gatehouse`、`core_approach` | `rune_arbalist`；Cast + HeavyHit | 冷却回退、真实 Resolver、母图/规范化/Importer |

## 3. 遭遇与正式资源

- 九个活跃首区遭遇共 28 个声明槽位；全部 ID 可由 `EnemyArchetypes.Get` 严格解析，不存在未知 ID 静默回退。
- `BuildCombatFromSceneStageTwo` 在正式遭遇中只生成声明数量；备用出生标记不会重复填充最后一个模板。
- `FormalArtRegistry` 只登记三项最终 ID；旧三项 Unity 草案副本已移出正式目录，母图与审计报告仍保留。
- 十张成品均为 64×64、硬 Alpha、18–24 色、中心 X=31–32、接地点 Y=57；Importer 为 Sprite / Point / Clamp / PPU32 / 无 mipmap。

## 4. 验收状态

`COMPLETE`。10/10 敌人、10 个独立能力与 10 个独立 ArtId 全闭环；全量 EditMode 233/233、PlayMode 1/1；编译错误/警告 0；正常 Play Mode Console warning/error 0；1920×1080 与 960×540 实际战斗回归通过；`CombatPrototype.unity` clean，未保存场景。
