# OCC UI-POLISH-82 资源语义色、敌人意图与术式卡验证

## 目标

- 修复英雄生命／护盾／个人魔力轨道大面积读成黑条、具体比例不直观的问题。
- 修复敌人头顶移动意图像“!!”且原生贴图偏在徽章左上角的问题。
- 重做术式快捷栏，使键位、术式图标、术式名称、行动点消耗和个人魔力消耗同时清楚可读。

## 根因与实现

- `bar_segment_health/shield/mana` 的旧彩色九宫格厚边在 20px 轨道上占比过大，并且旧生命皮肤实际为绿色；即使运行时填充色正确，最终画面仍被深色／旧彩边覆盖。现将轨道增厚到 24px，正式 `bar_track` 皮肤置于底层，语义填充四边内缩 4px 后置于上层；生命为锈红、护盾为灰绿、个人魔力为冷青。
- `FormalBattlefieldView` 原先把 16px 意图贴图以 1× 放在 40px 徽章左上角。现固定以整数 2× 显示为 32px，四向 4px 居中；伤害徽章宽度扩为 68px，图标与数字互不覆盖。移动意图改用已批准的 16px 双箭头移动符号，避免旧足迹像素被误读为“!!”。
- 战术栏中心术式组扩为 `800×184`；八槽由无名称窄槽改为 `4×2` 的 `192×70` 横向卡。单卡左侧为 32px 图标和 24×24 键位徽章，右上为 140×40 名称槽，右下以 2px 分隔线承接两枚 40×28 费用章。
- `game-interface-planning` 的信息预算用于确定卡片只保留“快捷键／是什么／消耗什么”三层决策信息；完整效果仍留在悬浮详情，不向卡片继续塞说明文本。

## 自动验证

- 聚焦 EditMode：4/4。
- 全量 EditMode：705/705。
- PlayMode：1/1。
- JSON 机器合同可解析，相关文件 `git diff --check` 通过。

## Play Mode 接触

- 自然战斗状态：`UnityProject/Library/FunplayMcp/Screenshots/UI-POLISH-82-final-natural-1920x1080.png`
- 八槽完整布局：`UnityProject/Library/FunplayMcp/Screenshots/UI-POLISH-82-final-eight-spells-1920x1080.png`
- 半分辨率接触：`UnityProject/Library/FunplayMcp/Screenshots/UI-POLISH-82-final-eight-spells-960x540.png`

八槽截图中的额外六张卡仅通过 Play Mode 反射填入正式术式图标、名称和费用，以验证最密集状态；该审核数据没有写入场景、配置、存档或正式玩法数据。资源比例和敌人意图来自真实运行时状态。

## 场景安全

- 未保存 `Assets/Scenes/CombatPrototype.unity`。
- Play Mode 审核数据随退出 Play Mode 销毁。
- 最终需保持编译错误 0、Console Error 0、场景 `isDirty=false`。
