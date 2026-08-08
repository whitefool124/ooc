# OCC 火元素个人术式实现矩阵 v0.1

## 0. 定位

- 日期：2026-08-08。
- 玩法源：`Worldbuilding/01_游戏策划/OCC_火元素个人术式池_v0.1.md`；本文件不重新决定数值。
- 唯一运行时机械目录：`UnityProject/Assets/Game/Runtime/Combat/FireSpellCatalog.cs`。
- 通用预览/执行：`FireSpellRuntime.cs`；获取/存档：`FireSpellProgression.cs`、`RogueliteMapRun.cs`；HUD/结算/VFX：`FormalCombatHud.cs`、`RogueliteSettlementPresentation.cs`、`CombatVisualFeedback.cs`。
- 图标根：`Art/FormalSkillIcons32/Fire/f-pNN`；50/50 独立 PNG、50 个独立 SHA-256、32×32、硬 alpha、Point/Clamp/无 mipmap。
- VFX 根：`Art/FormalVfx32/<module>/frame_00..05`；火系五套 30/30 帧 QA 通过，并与已通过的通用命中、破甲、护盾、解控、位移、魔力和物件反馈模块组合复用。

## 1. 50 项覆盖

| ID | 名称 | 组 | AP/魔力/CD/延时 | 目标/形状 | 核心规则 | 表现序列 |
| --- | --- | --- | --- | --- | --- | --- |
| F-P01 | 火弹 | A | 1/2/0/0 | 敌方/单体3 | 12伤害 | projectile→hit |
| F-P02 | 火带 | B | 2/5/2/0 | 空地/连续3 | 8@6燃烧地格 | projectile→ground |
| F-P03 | 烙印 | A | 1/3/0/0 | 敌方/单体3 | 4伤害+燃烧8×1 | projectile→burning |
| F-P04 | 引爆 | C | 2/5/2/0 | 单位/单体3 | 20伤害；燃烧优先于地格消费 | detonate→hit |
| F-P05 | 火矢 | A | 1/2/0/0 | 敌方/单体5 | 8伤害 | projectile→hit |
| F-P06 | 火种 | A | 1/2/1/0 | 敌方/单体4 | 燃烧8×2 | projectile→burning |
| F-P07 | 焰线 | A | 2/4/2/0 | 单位/直线4 | 全单位8；重掩体截断 | projectile→cross→hit |
| F-P08 | 余烬火弹 | A | 2/4/1/0 | 敌方/单体4 | 16伤害；既有燃烧至少2回合 | projectile→burning |
| F-P09 | 焰击术 | A | 2/3/1/+4 | 敌方/单体3 | 20伤害 | projectile→heavy_hit |
| F-P10 | 火焰喷射 | A | 1/3/0/0 | 单位/锥形3 | 全单位8 | spray→hit |
| F-P11 | 点燃喷射 | A | 2/4/2/0 | 单位/锥形3 | 全单位4；仅敌方燃烧8×1 | spray→burning |
| F-P12 | 追火术 | A | 1/3/1/0 | 敌方/单体4 | 8；目标燃烧则16 | projectile→burning→hit |
| F-P13 | 火路 | B | 1/3/1/0 | 空地/连续4 | 8@4燃烧地格；墙截断 | path→ground |
| F-P14 | 热浪弧 | B | 2/4/2/0 | 空地/锥形3 | 8@6燃烧地格 | spray→ground |
| F-P15 | 灼域火钉 | B | 1/3/1/0 | 空地/单格3 | 12@8燃烧地格 | projectile→ground |
| F-P16 | 围炉火环 | B | 2/4/2/0 | 单位/正交环3 | 周围4格8@6 | cross→ground |
| F-P17 | 炉口喷涌 | B | 2/4/2/0 | 空地/中心+正交4 | 8@4 | cross→ground |
| F-P18 | 炽焰墙 | B | 3/5/3/+4 | 空地/连续5 | 8@8 | cross→ground |
| F-P19 | 灰烬复燃 | B | 1/2/1/0 | 燃烧格/单格4 | 消失标记+4；最低8 | ground→burning |
| F-P20 | 焦土十字 | B | 2/5/3/0 | 空地/十字4 | 12@4 | cross→ground |
| F-P21 | 熔火领域 | B | 3/5/4/+8 | 空地/3×3 | 未被重掩体覆盖空地8@6 | cross→ground |
| F-P22 | 烬爆指令 | C | 1/3/1/0 | 燃烧单位/单体4 | 12；消费燃烧 | detonate→hit |
| F-P23 | 地火抽爆 | C | 1/3/1/0 | 火场单位/单体4 | 12；消费所在地格 | ground→detonate |
| F-P24 | 爆燃横扫 | C | 2/4/2/0 | 单位/锥形3 | 范围燃烧单位12并消费 | spray→detonate |
| F-P25 | 焚心爆点 | C | 2/4/2/0 | 燃烧单位/单体4 | 16并消费；邻格4 | detonate→cross |
| F-P26 | 焦土回响 | C | 2/4/2/0 | 燃烧格/十字4 | 火场格单位8；仅中心消费 | ground→cross→detonate |
| F-P27 | 余烬追爆 | C | 1/3/1/0 | 燃烧单位/单体4 | 16；燃烧固定1回合 | projectile→detonate→burning |
| F-P28 | 火场聚爆 | C | 3/5/3/+4 | 燃烧格/十字4 | 单位16；仅中心消费 | cross→detonate |
| F-P29 | 焚烬穿刺 | C | 2/4/2/0 | 单位/直线4 | 燃烧单位16并消费；重掩体截断 | projectile→detonate |
| F-P30 | 终焰裁决 | C | 3/5/4/+8 | 双前置单位/单体3 | 28；状态与地格全消费 | detonate→heavy_hit |
| F-P31 | 熔甲火钉 | D | 1/3/1/0 | 敌方/单体3 | 8+破甲-4×1 | projectile→armor_break |
| F-P32 | 赤炼爆点 | D | 2/4/2/0 | 敌方/单体4 | 16+破甲-4×1 | detonate→armor_break |
| F-P33 | 熔切束 | D | 2/4/2/0 | 物件/单体4 | 耐久20 | projectile→object_damage |
| F-P34 | 炉压破门 | D | 2/5/3/0 | 物件/单体2 | 轻掩体摧毁；重掩体20 | detonate→object_break |
| F-P35 | 焦甲横扫 | D | 2/4/2/0 | 单位/锥形3 | 全单位8；仅敌方破甲-4×1 | spray→armor_break |
| F-P36 | 热裂震波 | D | 2/4/2/0 | 物件/中心+正交3 | 轻掩体/设备耐久12 | cross→object_damage |
| F-P37 | 熔障炮 | D | 3/5/3/+4 | 物件/单体5 | 耐久28；轻掩体摧毁后8@4 | projectile→object_break→ground |
| F-P38 | 炉芯过热 | D | 2/4/2/0 | 设备/单体3 | 耐久20；归零过载 | projectile→object_damage→detonate |
| F-P39 | 焚甲贯矢 | D | 2/5/3/0 | 敌方/单体5 | 16+破甲-8×1 | projectile→armor_break→heavy_hit |
| F-P40 | 高炉断层 | D | 3/5/4/+8 | 单位/直线4 | 轻28/重20耐久；单位12 | cross→object_damage→heavy_hit |
| F-P41 | 灼热疾行 | E | 1/2/1/0 | 空地/直线2 | 移动；起点8@4 | path→ground |
| F-P42 | 炽热障壁 | E | 1/3/2/0 | 自身 | 护盾12 | cross→shield_restore |
| F-P43 | 余烬护甲 | E | 2/4/2/0 | 自身 | 清自身燃烧；护盾20 | cleanse→shield_restore |
| F-P44 | 灼缚解离 | E | 1/3/2/0 | 相邻敌方 | 需束缚；清束缚+敌8 | cleanse→projectile→hit |
| F-P45 | 温血苏醒 | E | 1/2/1/0 | 自身 | 需迟缓；清迟缓，移动力恢复 | cleanse→path |
| F-P46 | 炉温护持 | E | 1/3/2/0 | 自身/友方3 | 护盾12 | projectile→shield_restore |
| F-P47 | 焰压退击 | E | 1/3/1/0 | 相邻敌方 | 8+推1；非法终点只伤害 | spray→hit→path |
| F-P48 | 焦痕突进 | E | 2/4/2/0 | 空地/路径3 | 邻敌4；移动；终点8@4 | path→projectile→ground |
| F-P49 | 热源回收 | E | 1/2/2/0 | 燃烧格3 | 清地格；魔力+2（上限12） | ground→cleanse→mana_restore |
| F-P50 | 炉火应急 | E | 2/4/3/0 | 相邻空地 | 需自身燃烧；清除+护盾20+8@4 | cleanse→shield_restore→ground |

## 2. 自动门禁

- 目录：连续 ID、唯一 ID、五组各 10、字段非空、成本范围、规则与表现序列非空。
- 预览/执行：50/50 均构造合法命令并执行两份相同状态，结果序列签名完全一致；所有来源消费术式在无燃烧来源时拒绝且不扣成本。
- 内容池/存档：普通与稀有候选按固定种子可覆盖 50/50；同局个人术式不重复；`map6` 精确恢复持有与两个装备槽，兼容读取 `map1–map5`。
- 资产：50/50 图标可加载且无 fallback；每项表现模块的 `frame_00` 可加载；五套火系 VFX 共 30/30 独立帧 QA 通过。
- 排除：人物造型、人物逐帧动画与角色立绘继续暂停。
