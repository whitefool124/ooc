# ENEMY-PACK-01 实施计划 v0.3

## v0.3 扩展执行（立即实施）

1. 保留 v0.2 已验收的刻印锤手、屏障修补师、缚环猎兽；将盾卫、火术师、突袭者、精英先锋升级为同等完整的特色敌人。
2. 新增石索缚师、显影灯使、重弩手，锁定稳定 ID、当前数值、能力、状态结算、AI 反制窗口、遭遇位置和时代相容视觉语义。
3. 10 个敌人全部使用互不复用的 64×64 正式单位图；新 3 个执行母图→规范化→有限色板/基线/轮廓 QA→Unity Importer 门禁，保留母图和报告。
4. 重编九个首区活跃遭遇，使 10 个角色均可在真实运行时到达；继续禁止 `rifleman/sniper`、现代枪械和现代爆破语义。
5. 扩展数据合同、能力目录、AI、Resolver 测试、资源注册、动作/VFX 映射和时代禁词回归；经 Funplay 重编译、全量 EditMode/必要 PlayMode、Console、1920×1080/960×540 与场景 clean 验收后收口。

## 任务合同

- **目标：** 按主角入学期“第一次工业革命前”技术基线，完整交付 10 名职责互补敌人，并清除首区活跃遭遇中的步枪、狙击和现代爆破语义。
- **涉及文件/系统：** 敌人玩法源与本计划；`EnemyArchetypes`、`EnemyAbilityCatalog`、能力型 AI、`CombatResolver`、`RogueliteEncounterCatalog`、`CombatPrototypeBootstrap`、`CombatVisualFeedback`、`FormalArtRegistry`、`FormalUnits64`、母图/规范化/Importer QA、EditMode/PlayMode 与双分辨率回归。
- **验收标准：** 10 敌人数据、AI、技能/状态结算、遭遇、资源与动作/VFX 形成真实运行时闭环；所有活跃首区遭遇无现代枪械/爆破语义；10 张最终图通过像素及 Importer QA；全量测试、编译、Console、1920×1080/960×540 和场景 clean 通过。
- **完成后解锁：** 首区编成平衡、更多原生魔法生物和第二地区敌群；首领重做与四倍正式数值迁移另立任务。

## 执行阶段

1. 冻结时代证据与旧草案→最终方案修订表；同步玩法源和当前待办。
2. 将旧 `aether_sapper/barrier_engineer/relay_hound` 与三个旧技能 ID 迁移为 `sigil_mauler/barrier_mender/tether_hound` 及时代正确能力；保持确定性目标和稳定回退。
3. 重编全部首区活跃遭遇，移除 `rifleman/sniper`，验证只生成声明数量且所有 archetype 可解析。
4. 登记十张最终 64×64 资源；保留旧母图审计证据，新三张按最终身份独立规范化并生成 1×/4×/灰阶/基线/调色板报告；通过 Unity Importer 门禁后才视为正式。
5. 把碎甲锤印、护障续接、缚环扑咬接入正确近战/施术/扑咬动作和破甲/护盾恢复/束缚 VFX；验证普通攻击、专属施放、受击、恢复和待机均可见。
6. 增加时代禁词、模板、AI、结算、遭遇、资源、像素与 Importer 回归；经 Funplay 重新编译，跑全量 EditMode 与必要 PlayMode，检查 Console。
7. 以 1920×1080 与 960×540 实际战斗检查同屏轮廓、技能/VFX/动作读法和 HUD 占比；确认 `CombatPrototype.unity` 与相关场景 clean，更新验证、矩阵与待办收口。
