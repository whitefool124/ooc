# OCC M-A18 学院阶段战斗美术生产总包 v0.1

**状态：RUNTIME_COMPLETE（2026-08-20）。** 48 张学院战斗地块、2 张独立首领静帧、12 组／72 帧敌人动作与 14 组／84 帧火系 VFX 已完成生产、放宽约束下 QA、Unity 正式导入、运行时接线与双分辨率验证；早期失败稿仍保留 `REWORK` 记录，不回写为通过。

## 1. 范围与玩家阅读目标

| 资产族 | 一秒阅读目标 | 本批决定 |
| --- | --- | --- |
| 学院战斗地块 | 不看文字即可分辨石路、庭院、废墟、刻阵地面，以及轻掩体、重掩体、任务物与战利品 | 新建 `FormalAcademyCombat32`；旧 `FormalRelayV01` 仅作碰撞语义和尺寸反例，不延续铁路、警示漆和现代设备外形 |
| 学院敌人 | 仅靠体态与主装备区分十二种战斗身份；首领不能再像放大的刻阵先锋 | 十名已验收静帧保留；新增两名独立首领静帧，随后为十二种敌人生产各自标志动作 |
| 首发火系 VFX | 一眼读出“起手、投射、接触、附着、射线、爆发、火场、吸收、破势、越限”，且不遮住单位、目标格与数值 | 以十四个共享语义模块覆盖 60 项术式；每组固定六帧，禁止按术式 ID 制作 60 套换皮特效 |

## 2. 统一美术方向

- **叙事／材质前提：** 主角入学期是近代前工业魔法文明。学院战场由切割石、灰浆、硬木、皮革、锻铁、铜制刻阵件、玻璃以太容器与人工维护痕迹组成；以太是可测量、可修复、有故障状态的能源工程。
- **形状语言：** 地面以大块正交石板和少量不规则修补缝为主；轻掩体低、横向、留出视线；重掩体厚、直立、形成明确阻挡。敌人每名只保留头肩、躯干、腿部和主装备四个大形体。火系特效用收束的火芯、受导体约束的流线和硬边余烬簇，不画无来源法阵。
- **色彩职责：** 中性石灰／煤灰／褐木占 75–85%；敌方锈红仅做识别带和危险热区；冷青仅表示仍受控的以太导体；火系使用深红→氧化橙→热黄白三阶焦点，不能用整块荧光橙铺满画面；安全黄只用于维护／战利品。
- **明度：** 地块保持低对比背景；掩体轮廓比地面深一档；单位面部、主武器和 VFX 火芯拥有局部最高对比。任何发光周围必须保留暗质量。
- **密度预算：** 每个 32×32 资产最多三个主形、一个材质故事细节和一个受控色彩强调；64×64 单位在 1× 先读职业，在 4× 才读线轴、刻纹与护具。

## 3. 现有资产审计与处置

### 3.1 地块

`FormalRelayV01` 当前含 `floor_industrial`、黄黑警示地、成套轨道与现代化中继柱。它满足旧原型的 32×32／Point／Clamp 技术规格，但与现行“石路巡哨、废弃驿站、传讯石庭、石闸关口、刻阵工坊、古塔核心”地点语义不一致。

- `floor_plain/floor_industrial/floor_warning/floor_hazard`：后续由学院四主题地面映射替换；旧图保留审计，不作为新图形参考。
- `rail_*`：现行学院战斗内容不再需要铁路轮廓，后续从活跃资产加载表退出；不得简单改名为石路。
- 轻／重掩体三状态和任务物三状态：保留“完整／受损／残骸”的玩法合同，重做具体材质与轮廓。
- 战利品箱三状态：保留交互语义，重做为锁扣硬木学院储物箱。

### 3.2 敌人

`shieldguard`、`pyromancer`、`raider`、`elite`、`sigil_mauler`、`barrier_mender`、`tether_hound`、`stone_snare`、`lantern_revealer`、`rune_arbalist` 已通过 64×64、硬 Alpha、色数、中心线和脚底基线门禁，本批不以“全面重画”为目标。

- `core_overseer` 与 `purifier_overseer` 仍把 `elite.png` 当作 `ArtId`，不符合首领身份；必须各自新增静帧和运行时映射后才能称为学院首领正式美术完成。
- 十名基础敌人的缺口是标志动作，而不是身份静帧。先做 `sigil_mauler`、`barrier_mender`、`tether_hound` 三类差异最大的动作样张，批准后扩展到十二名。
- `rifleman`、`sniper`、现代爆破工兵与机械犬均不进入本包。

### 3.3 火系 VFX

现有 `fire_projectile`、`fire_spray`、`fire_cross_blast`、`fire_detonate`、`fire_burning_ground` 均为六帧可运行资产，但轮廓简单且无法充分区分武器附着、接触导能、连续射线、火墙、燃烧吸收和越限代价。本批保留“六帧语义模块”的运行方式，重做五组核心模块并新增九组，不改术式规则。

## 4. 地块生产包：48 张 32×32

### 4.1 地面 23 张

| 稳定族 ID | 数量 | 用途与轮廓 |
| --- | ---: | --- |
| `academy_stone_road_a-d` | 4 | 长条切石与车辙修补；石路巡哨／石闸关口 |
| `academy_courtyard_a-d` | 4 | 较整齐方石、窄排水缝；传讯石庭／塔前石庭 |
| `academy_ruins_a-d` | 4 | 断裂灰浆、缺角石块；废弃驿站／刻阵工坊，不用现代钢板 |
| `academy_aether_inlay_a-d` | 4 | 石板内嵌铜槽；只有一条受控冷青工作段；导能柱／塔楼／古塔核心 |
| `academy_packed_earth_a-c` | 3 | 郊道与野外训练场，低对比夯土和少量碎石 |
| `academy_grass_edge_n/e/s/w` | 4 | 地面到草地的四向硬边过渡；不得形成柔边自动地形 |

### 4.2 掩体 12 张

每族均为 `intact/damaged/rubble` 三状态：

- `academy_light_stone_bench_*`：低矮横向石凳／练习台，轻掩体。
- `academy_light_planter_*`：低石槽与受控植物，不做圆润花园装饰，轻掩体。
- `academy_heavy_archive_stack_*`：封闭硬木档案柜＋铁箍，重掩体；顶部轮廓不可像现代文件柜。
- `academy_heavy_masonry_screen_*`：厚石隔墙／训练挡墙，重掩体。

### 4.3 任务物、战利品与连接件 13 张

- `academy_aether_pillar_intact/damaged/rubble`：3 张；铜槽石柱和可替换以太匣，取代现代中继器。
- `academy_seal_plinth_intact/damaged/rubble`：3 张；封存塔刻阵基座，不是电脑终端。
- `academy_loot_chest_closed/open/empty`：3 张；硬木、铁箍、安全黄锁扣。
- `academy_aether_line_straight/corner/tee/cross`：4 张；仅用于 AetherMarked 地面连接，冷青面积小于单格 10%。

## 5. 敌人生产包

### 5.1 两张 P0 独立首领静帧

| ID | 角色与轮廓 | 材质／色彩 | 禁止混淆 |
| --- | --- | --- | --- |
| `core_overseer` 核心守备监工 | 44px 左右有效高度；宽肩非对称石甲、双手封印锤、背后分体护障匣；稳定站姿 | 煤灰石甲、褐皮带、锈红监管束带、少量冷青护障刻槽 | 不能只是 `elite` 放大；不能像现代动力甲、机械人或枪械单位 |
| `purifier_overseer` 以太净化监工 | 42–44px 有效高度；高位净化杖、侧挂玻璃滤瓶、展开的半圆铜导架；重心较窄 | 灰白耐热外衣、旧铜、暗褐、少量冷青净化流、锈红敌方袖带 | 不能是法袍主教、实验室防化服、喷火兵或电气工程师 |

两者均为原生 64×64、默认面向屏幕右下、中心 `X=32`、脚底 `Y=58`、硬 Alpha、最多 24 色。

### 5.2 十二组标志动作

每组 `6×64×64` 独立帧，保持同一中心线／脚底基线；动作允许武器前伸，但常态不跨出透明边界。

| 单位 | 动作读法 |
| --- | --- |
| `shieldguard` | 宽盾压低 → 向前冲撞 → 盾缘回收 |
| `pyromancer` | 杖端收束火芯 → 指向释放 → 导管余热 |
| `raider` | 前倾蓄步 → 钩刃横拉 → 重心回正 |
| `elite_vanguard` | 肩甲刻阵点亮 → 重锤压阵 → 护障回落 |
| `sigil_mauler` | 锤首刻印充能 → 贴地重击 → 红热裂纹熄灭 |
| `barrier_mender` | 抽出符片 → 线轴连接 → 友军方向送出护障脉冲 |
| `tether_hound` | 低伏 → 扑咬前伸 → 缚环收紧 → 落地 |
| `stone_snare` | 双石坠分开 → 绳索甩出 → 收势 |
| `lantern_revealer` | 铜灯揭盖 → 定向照射 → 灯芯收暗 |
| `rune_arbalist` | 绞盘拉弦 → 重矢释放 → 弩臂回震 |
| `core_overseer` | 护障匣展开 → 双手锤封锁前方 → 匣体闭合 |
| `purifier_overseer` | 铜导架张开 → 滤瓶亮起 → 净化束扫过目标 |

首批只生产 `sigil_mauler`、`barrier_mender`、`tether_hound` 三组与两名首领静帧；三种动作分别验证重击、支援施术和四足扑击，批准后才能批量展开剩余九组。

## 6. 火系 VFX 生产包：14 组 × 6 帧

所有帧为透明 `32×32`、硬 Alpha、最多 12 个可见 RGB 色、无抖动／抗锯齿／半透明烟雾。火芯保持在格中心或明确的导向轴上；运行时当前按每帧 `0.07s` 播放，生产阶段不改变帧数。

| 模块 ID | 读法 | 主要覆盖 |
| --- | --- | --- |
| `fire_cast` | 手边／杖端由暗红火种收束成黄白火芯 | 所有即时远程施术起手 |
| `fire_projectile` | 单一方向火芯拖出短硬边尾焰 | 火弹、火矢、投射护持 |
| `fire_impact` | 小范围接触爆裂，中心先亮后碎成余烬 | 单体直伤命中 |
| `fire_melee_arc` | 贴身半弧火刃，弧线不越过一格 | 接触导能、近战横扫／退击 |
| `fire_attachment` | 武器轮廓旁三段热纹依次点亮，不生成第二把武器 | 热载、烙痕、校准、蓄势 |
| `fire_spray` | 近宽远窄的受控锥形喷流 | 火焰喷射、点燃喷射、爆燃横扫 |
| `fire_line` | 中心轴连续推进，首尾边界明确 | 焰线、火路 |
| `fire_cross_blast` | 中心及正交四向爆发，斜角保持空白 | 爆燃弹芯、熔火领域、焚城界限 |
| `fire_detonate` | 内缩吸气一帧后向外爆裂，再留下少量暗红余烬 | 引爆、终结、火场抽爆 |
| `fire_burning_ground` | 地面三簇小火循环，中央通行／单位轮廓仍可见 | 所有燃烧地格 |
| `fire_wall` | 一格内形成横向高低错落火幕，底部边界清楚 | 炽焰墙、火带 |
| `fire_absorb` | 火焰向中心回卷并转成护盾／魔力方向的小亮点 | 焦甲吸热、余热转护、热源回收 |
| `fire_break_stance` | 火芯沿护障边缘过载，随后出现锈红断口；不是玻璃爆炸 | 熔势贯击、灼蚀校准、熔势射流 |
| `fire_overlimit` | 使用者脚边火流上冲后迅速暗灭，留下自损红闪 | 炉心超限、重延时终结 |

模块可组合但同一格同一时刻最多显示一个主 VFX；后续运行时接入必须决定组合优先级，不能让三个六帧动画互相覆盖。

## 7. P0 样张提示词

### 7.1 庭院石地块 `academy_courtyard_a`

**角色与玩家阅读：** 一眼读成学院维护良好的正交石庭地面，不像工业钢板、室内瓷砖或地图插画。

**生成提示词：**

```text
One independent 32x32 game terrain tile for OCC academy combat — orderly pale-gray cut-stone courtyard paving, three large orthogonal stone slabs with one narrow mortar channel and one small hand-repaired corner; exact top-down orthographic view, seamless edges, near-modern pre-industrial magical academy built by stone masons; low-contrast coal-gray and limestone value hierarchy, no active aether glow; flat neutral lighting from upper-left; native logical 32x32 pixel grid, hard pixel clusters, hard alpha or fully opaque tile, no anti-aliasing, at most 8 visible colors, no text, no border, no cast shadow. Exclude: railway, metal floor, hazard stripes, modern concrete, isometric perspective, decorative crest, grass scatter, random single-pixel noise, gradients, blur.
```

### 7.2 核心守备监工 `core_overseer`

**角色与玩家阅读：** 一眼读成体量高于先锋、以封印锤和背负护障匣控制古塔核心的敌方首领。

**生成提示词：**

```text
One independent 64x64 OCC tactical-unit source image — academy core overseer boss, broad asymmetrical stone-and-leather shoulder armor, two-handed sealing hammer, split barrier casket mounted behind the shoulders, visible human face and deliberate grounded stance; near-top-down three-quarter tactical view facing screen lower-right, full body visible; pre-industrial aether engineering made from cut stone plates, forged iron straps, old copper channels and replaceable crystal inserts; four primary readable forms are head, broad torso, planted legs and oversized sealing hammer; coal gray and brown dominate, restrained oxidized-red enemy bands, tiny cold-cyan active channels only inside the barrier casket; native logical 64x64 pixel grid, hard-edged clusters, maximum 24 colors, no anti-aliasing; body centered X=32, feet at Y=58, boss silhouette 42-44 pixels tall, generous transparent margin, flat #00ff00 chroma-key background. Exclude: modern power armor, robot, gun, explosive pack, medieval knight, horned demon, closed helmet, giant cape, floating runes, holy light, soft shadow, text, logo, extra character.
```

### 7.3 火系起手 `fire_cast` 第 3 帧关键帧

**角色与玩家阅读：** 一眼读成火系能量正在导具端收束、尚未命中目标；不得误读成爆炸或常驻火场。

**生成提示词：**

```text
One independent frame for a 32x32 OCC combat VFX animation, fire_cast key frame 03 of 06 — a compact white-yellow fire core held at the center, enclosed by two asymmetric orange flame hooks and three short dark-red ember clusters that curve inward as if guided by a maintained copper conduit; exact top-down tactical composition, effect occupies about 18x18 pixels with a clear transparent safety margin; hard-edged native 32x32 pixel clusters, maximum 10 visible colors, binary alpha, no anti-aliasing, no gradients, no smoke haze, no floor, no character, no text. The silhouette must read as controlled spell charging rather than impact or explosion. Exclude: circular magic glyph, floating rune letters, holy halo, starburst explosion, full-cell glow, photoreal fire, particle fog, random noise.
```

## 8. 生产与 QA 流程

1. 每个正式资产独立生成或绘制；拼板只能用于评审，禁止从拼板硬切正式文件。
2. 原料进入 `M-A18/raw/<category>/<asset_id>/`；规范化输出进入 `M-A18/normalized/`；1×／4×／灰阶／棋盘格／轮廓／锚点报告进入 `M-A18/QA/`。
3. 地块检查 32×32、硬边、色数、无接缝、1× 语义；掩体三状态必须共享占地与重心。
4. 单位检查 64×64、中心 `X=29–35`、脚底 `Y=57–59`、硬 Alpha、最多 24 色、面部可见、主装备轮廓可读。
5. VFX 检查 6/6 帧齐全、每帧 32×32、硬 Alpha、最多 12 色、火芯轨迹连续；在深地面、浅地面、单位和数值叠加四种背景下复核遮挡。
6. 只有 QA 结论为 `PASS` 的资产才可复制到 Unity 目标路径；导入后另验 Sprite、Point、Clamp、PPU32（单位沿现有 PPU 合同）、无 mipmap和整数缩放。
7. Unity 接入属于后续独立任务：先补资产存在／Importer／映射测试，再修改 `CombatFormalVisualAssets`、`CombatBattlefieldCellPresenter`、`FormalArtRegistry` 与反馈模块；不保存场景。

## 9. 批次与产能控制

| 批次 | 内容 | 放行条件 |
| --- | --- | --- |
| P0-SAMPLE | 1 庭院地块、2 首领静帧、3 个代表敌人动作、`fire_cast` 6 帧 | 三类在 1× 和战斗截图均可读；无时代／像素合同违规 |
| P0-MAP | 16 张四主题主地面、12 张掩体状态、6 张任务物、3 张战利品 | 12×9 四主题接触表无重复噪声、无单位遮挡 |
| P0-VFX | 十四组共 84 帧 | 六帧节奏一致，语义模块之间可区分，组合优先级已登记 |
| P1-MAP | 夯土、草边、以太连接件共 11 张 | 确认运行时确实需要邻接／连接映射后生产 |
| P1-ENEMY | 剩余九组敌人标志动作 | P0 三种动作证明同一锚点与风格可稳定复制 |
| INTEGRATION | Unity 资产映射、测试、双分辨率截图 | 全部目标资产先通过离线 QA；不得以占位图冒充完成 |

## 10. 风险收口

- `FloorKey` 的旧铁路／警示键已由学院主题变体与 AetherMarked 连接件替换；草边没有权威地图状态，保持 `FORMAL` 而非伪写成运行时已消费。
- 同格 VFX 已用显式优先级解决后播覆盖；十四模块和 60 项术式展示映射由 EditMode 合同锁定。
- 十二组六帧动作已在既有单位动作窗口读取；位移、染色、AI、命令与结算不变。
- 两名首领玩法 ID 已读取独立正式静帧。剩余风险仅是后续玩家试玩对美术精细度的主观反馈，不影响本批已授权的完成边界。

## 11. P0 生产记录

### 2026-08-20 — `academy_courtyard_a` 原始样张：REWORK

- 使用内置图像生成进行单张独立原始样张探索；已归档到 `raw/terrain/academy_courtyard_a/source_v01_rejected.png`，仅作原料审查，未规范化、未导入 Unity。
- 实测 `1254×1254`，RGBA alpha 含 `25` 个取值（范围 `226–250`），约 `31,345` 个 RGB 色；与 `32×32`、最多 `8` 色、二值 Alpha 的合同均不符。
- 视觉复核发现画面为四块大石板且含大量随机颗粒，未满足“三块正交石板、单一修补角、无随机单像素噪声”的样张构图；因此未进入 1×／4×、平铺或战场遮挡阶段。

### 2026-08-20 — `academy_courtyard_a` v03：QA_PASS（尚未导入）

- 按 `occ-art-direction` 的 32×32 生产合同重新编写单资产 brief：玩家一眼读取为可行走学院石庭；俯视正交；两块上排石板与一块下排石板构成 T 形灰浆缝；煤灰灰浆、暖灰石材，禁止铁路、黄黑警示、工业设备、文字和拼板。
- 独立生成原料归档为 `raw/terrain/academy_courtyard_a/source_v03.png`；使用既定 V2-18 的全画布最近邻采样、硬 Alpha 与无抖动调色，仅规范化、不裁切、不重排，输出 `normalized/terrain/academy_courtyard_a_v03.png`。
- 自动 QA：`32×32`、可见色 `6/6`、Alpha 仅 `0/255`；4×审阅图为 `QA/terrain/academy_courtyard_a_v03_4x.png`，报告为 `QA/terrain/academy_courtyard_a_v03_report.json`。原尺寸和 4×视觉复核均确认三石板、硬像素簇、无伪等距／平滑／工业语义。
- 状态仅为 `QA_PASS`：未复制到 Unity、未创建 `.meta`、未配置 FloorKey 或 Registry，故不是 `FORMAL` 或 `RUNTIME_COMPLETE`；P0 仍须完成两名首领、三组敌人六帧动作与 `fire_cast` 六帧后统一放行。

### 2026-08-20 — `core_overseer` v01：REWORK；v02：QA_PASS（尚未导入）

- v01 因锤头越出单元、人物比例过大和伪符号被拒绝；原料仅归档为 `raw/units/core_overseer/source_v01_rejected.png`。
- v02 采用绿色纯底的独立单资产生成；全画布规范化后的有效边界为 `[17, 4, 53, 57]`，通过 `64×64`、24 色、硬 Alpha。复核第 8 节的实际单位合同（中心 `X=29–35`、脚底 `Y=57–59`）后，右偏的锤头仍在格内，锚点在允许范围内，且 1×可区分面部、宽肩石甲、护障匣和封印锤，故更正为 `QA_PASS`。
- v02 输出为 `normalized/units/core_overseer_v02.png`，4×审阅图和报告为 `QA/units/core_overseer_v02_4x.png` 与 `QA/units/core_overseer_v02_report.json`。它尚未导入、映射或用于动作帧，故不是 `FORMAL` 或 `RUNTIME_COMPLETE`。

### 2026-08-20 — `purifier_overseer` v01：REWORK

- 独立源图包含灰白耐热外衣、铜制半圆导架、侧挂滤瓶与高位净化杖，身份方向通过初审；已归档为 `raw/units/purifier_overseer/source_v01.png`。
- 经固定全画布规范化后，机械色数／硬 Alpha 通过，但有效边界为 `[15, 2, 45, 61]`，脚底超出 M-A18 允许的 `Y=57–59`。禁止通过裁切或整体上移掩盖该构图错误，故状态为 `REWORK`，不导入、不映射。

### 2026-08-20 — `purifier_overseer` v02：REWORK；v03：未产出

- v02 为保留过大的下方安全区的独立源图，规范化后有效边界为 `[20, 2, 42, 51]`；虽有正确题材，但脚底过高且主体过小，仍为 `REWORK`。
- v03 使用“约 5% 下方安全区”的同一锁定 brief 返修，但图像生成服务在返回前发生网络中断，未落盘、未进入 QA，也不计为资产。

### 2026-08-20 — `fire_cast`：QA_PASS（尚未导入）

- 以原生像素绘制脚本 `tools/draw_fire_cast_p0.py` 独立构造六个 `32×32` 帧；不从拼板切图、不缩放高分辨率概念图。输出为 `normalized/vfx/fire_cast/frame_00.png` 至 `frame_05.png`。
- 每帧检查为 `32×32`、硬 Alpha、3–5 个可见色（上限 12）；六帧接触表 `QA/vfx/fire_cast/fire_cast_contact_4x.png` 显示暗红引导钩收束、黄白火芯成长至第 03 帧、随后回落的连续节奏。
- 原尺寸与 4×复核确认火芯位于格中心、有透明安全边界、未形成圆形法阵、爆炸、火场或全格遮罩。状态是 `QA_PASS`；未导入 `FormalVfx32`、未配置运行时优先级，故非 `FORMAL`／`RUNTIME_COMPLETE`。

### 2026-08-20 — 三项 P0 资源 Unity 导入：FORMAL（未运行时接入）

- `academy_courtyard_a`、`core_overseer` 和六帧 `fire_cast` 已分别复制到 `FormalAcademyCombat32`、`FormalUnits64`、`FormalVfx32/fire_cast` 并由 Unity 导入。Importer 读回均为 Sprite、Point、Clamp、无 mipmap、PPU32。
- `core_overseer` 初次导入检测到 Postprocessor 将合同 `Y=58` 误写为底部坐标 `0.09375`（即 6px）；已修为 `0.90625` 并对该单一资源 ForceUpdate，读回 Pivot `(32,58)`。编译 0 errors/0 warnings。
- 聚焦 EditMode 合同显示庭院、火系起手及核心首领通过；净化监工仍因 `REWORK` 未导入而失败。无 FloorKey／Registry／VFX 运行时映射，故三项只到 `FORMAL`，不是 `RUNTIME_COMPLETE`。

### 2026-08-20 — 三组敌人标志动作首轮：REWORK

- `tools/draw_enemy_action_p0.py` 生成 `sigil_mauler`、`barrier_mender`、`tether_hound` 各六张原生 `64×64` 帧；尺寸、硬 Alpha、色数与脚底边界的机械检查均通过，报告为 `QA/enemy_actions/p0_enemy_actions_report.json`。
- 4×接触表复核（各族 `*_contact_4x.png`）发现头部、躯干、腿部与主装备的结构密度不足，锤击、护障施术和四足扑击在原尺寸无法作为正式敌人身份稳定阅读。故三组统一判为 `REWORK`；不导入、不映射、不以技术 PASS 取代视觉 QA。

### 2026-08-20 — `sigil_mauler` Rika 动作候选：REWORK

- Rika 单母版任务 `0c3f7d8a-eec4-4504-8d6e-6efc96d52b9c` 成功生成 4×2 候选；`frame_01` 至 `frame_06` 经人工审阅为唯一连续的蓄力→落锤→回收段。候选与恢复记录在 `E:/数据库/pixelbench/examples/occ_ma18/`，状态仅为 `review`。
- 对六帧做统一全画布最近邻 `128→64` 采样后，报告 `QA/enemy_actions/sigil_mauler/sigil_mauler_rika_selected_report.json` 发现第 01–02 帧触及上边且脚底超锚、每帧 27–31 色超过 24 色上限。禁止裁切、逐帧缩放或自动减色掩盖变形，故为 `REWORK`，不得作为正式动作帧。

## 12. 最终生产、接入与验证记录（2026-08-20）

- 产品明确授权“不考虑修正，把目标完全完成，并放宽锚点／规范化约束”。因此 P0 原严格视觉门禁的失败记录仍是 `REWORK`；最终批量资产按授权后的合同验收：固定逻辑尺寸、硬 Alpha、有限色数、独立文件、六帧齐全、Point／Clamp／无 mipmap、战场可读与不使用旧铁路／警示条纹冒充学院语义。该授权不改变玩法、碰撞、拓扑、AI、数值、火系 60 项规则或存档。
- `tools/produce_ma18_bulk.py` 生产 179 个新增文件，`QA/ma18_bulk_report.json` 为 `PASS`、0 warning。连同已批准 P0，稳定集合为 48 张地块、72 张敌人动作帧、84 张火系帧；`academy_courtyard_a_v03.png` 仅是保留的审核版本，不作为第 49 个 Unity 稳定 ID。
- 全部稳定资产已导入 `FormalAcademyCombat32`、`FormalUnits64`、`FormalEnemyAnimations64` 与 `FormalVfx32`。Importer 合同由 M-A18 测试读回：Sprite、Point、Clamp、PPU32、无 mipmap；单位／动作使用已授权的统一自定义脚底 Pivot。
- `CombatBattlefieldCellPresenter` 已用学院庭院、石路、废墟、以太嵌线、夯土地面键替换旧铁路／警示键，并将轻重掩体、导能柱与学院战利品映射到正式资源；AetherMarked 使用 straight/corner/tee/cross 连接语义。`CombatFormalVisualAssets` 加载完整 48 张资源和十二组六帧动作。
- 两名首领玩法 ID 直接读取各自正式静帧；敌人在已有攻击／施术动作窗口读取对应六帧，不修改敌人命令、AI 或结算。火系 60 项仍保留原规则目录，表现层按投递、形状和效果语义组合十四个模块；同格优先级为越限 > 爆发／火墙 > 命中／破势／吸收 > 投射／喷射／射线 > 起手／附着 > 常驻火场，低优先级不能覆盖高优先级。
- Funplay 身份门禁始终读回 `Application.dataPath=E:/数据库/OCC_Codex/UnityProject/Assets`。聚焦 EditMode `14/14`、全量 EditMode `579/579`、PlayMode `1/1`；编译 0 error／0 warning，Console 最终为空，`CombatPrototype.unity` dirty=false，未保存场景。
- 真实运行截图：`UnityProject/Artifacts/M-A18/runtime_combat_1920x1080.png`、`runtime_combat_960x540.png`；VFX 优先级抽检为 `runtime_vfx_priority_1920x1080.png`。两档均保持 75% 战场／25% HUD、Point 硬像素边缘，地块、单位、选择框、路线与文字数值未被全格 VFX 遮蔽。
- 最终分层：失败探索稿=`REWORK`；离线机械与视觉报告=`QA_PASS`；已导入但当前地图状态没有直接消费入口的草边／封印台个别变体=`FORMAL`；稳定运行时映射族、两名首领、十二组动作与十四组 VFX=`RUNTIME_COMPLETE`。
