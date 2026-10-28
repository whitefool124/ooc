# 战斗空间粒子系统

运行入口：`CombatVisualFeedback.RefreshVfx` → 既有 `CombatVfxPlayback` → `Hd2DBattlefieldSample.WorldParticles`。剧情与肉鸽共用，只消费表现样本，不改结算、存档、战斗随机数或动作时长。

`CombatParticleRecipe.TryGet` 管理30个现有标识、18类运动配方：命中、重击、起手、投射、近战弧、喷射、线形导引、爆发、火场、燃烧、恢复、净化、束缚、迟缓、破甲、物件碎屑、位移、属性/辅助。护盾资源仍无罩体特效，原有数字、银灰受击和声音保留。未知标识与非HD2D战场沿用旧表现。

全部使用共用无贴图立方网格、已有 `SkyFurnaceSolidPixel` 材质与原生 ParticleSystem；本轮新增美术图片为0。方粒子边长1/32米，投射/线形/弧形细粒子截面1/32米、长度4/32米；这是特效尺度登记，不修改单位或场景密度。实际运动范围见本轮局部回读及Player证据，不能从单颗尺寸推断整团大小。

瞬态能力/反应按格分层复用，至多48槽，每槽至多96粒；持续火场另有24槽，超出的格保留既有静态场地提示。全战场瞬态无阴影光至多4盏，每盏2.5米范围，仅起手/爆发启用。动画强度影响发射数量，零强度清空全部瞬态；隐藏战场、停用、重开、离战与同格替换清除原生粒子和灯。持续火场读取命中快照控制后的实际火场集合，移除即回收。

动作时间来自原队列；`CombatVfxSample.StartedAt` 区分相同标识的再次施放和连续阶段，不另建结算时钟。空间模拟使用战场根的Custom simulation space，跟随原战场相机遮挡与输出。技术依据：[Unity ParticleSystem 自定义模拟空间](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/particlesystem/mainmodule/customsimulationspace)，实际工程锁定6000.5.2f1。

肉鸽出口：`RogueSpellCombatRuntime.LastExecution` 只保留最近一次成功执行结果供表现读取，不参与规则、克隆或存档；`CombatPrototypeBootstrap.TryCommand` 比较本次执行前后引用，只将本次新结果交给 `NotifyRogueSpellDelivery`。基础火术使用普通技能180ms命中窗口，个人火术沿用 `FireVfxSequence` 的真实逐格结果和延迟标记。失败指令不能重播上次法术。

扩展已有结果类型时在配方表添加映射与运动参数；新玩法先更新总案。法阵/核心独特美术仍走独立生图与资产审核，不把本系统的程序粒子当作生图原料或正式主形资产。

后台局部检查：Unity菜单 `OCC/Verification/Battle Particles/Local Checks`，覆盖30个标识原生发射/运动、全部结果枚举映射、同名重启、火场移除、槽/灯上限和零强度回收。Player以 `-aether-battle-vfx-check <绝对报告路径>` 启动，在隔离存档域从普通新游戏配置进入学院战斗，使用真实界面的预览/提交边界行动；不注入角色、装备、地图状态或直接伪造命中特效。该自动化不是鼠标命中测试，也不代表每种效果均已在正式战斗实测。

本轮归档：`Worldbuilding/归档/2026-10-23_战斗粒子系统/`。技术检查、Player局部验证和人工审美分别登记；HD2D基线保持QA_PENDING，机器覆盖不能替代人工审美、完整密度审核与性能预算验收。

`v2-motion/` 为修正版不截图的连续原生样本记录；`v2-capture/` 为同一版本的双分辨率画面采集，截图会扰动帧时，不能代替连续时序记录。原目录的首版Player记录仅追溯旧接线，不证明修正版表现。
