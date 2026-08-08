# OCC 全量正式美术生产与 Funplay 实装：新对话提示词

复制下面整段到位于 `E:\数据库\OCC_Codex` 工作区的新 Codex 对话：

```text
请执行 OCC 的 M-A3“全量正式美术生产与 Funplay 实装”，不要只做计划或样品。持续工作，直到当前已确定系统的全部正式美术、Unity/Funplay 实装、全流程视觉回归和最终审查均满足终局完成条件；不要因为预计工期、生成批次数、上下文长度或额度估算而自行停在阶段中间。允许按依赖分批执行和自动续接，但阶段不是交付终点。只有真正需要新的产品决定时才询问我。

工作区：E:\数据库\OCC_Codex
Unity 工程：UnityProject/
默认场景：Assets/Scenes/CombatPrototype.unity

开始前必须完整读取并遵守：
1. AGENTS.md
2. Worldbuilding/03_开发管理/OCC_当前待办.md
3. Worldbuilding/03_开发管理/OCC_全量正式美术生产与Funplay实装计划_v0.1.md
4. Worldbuilding/05_美术与音频/OCC_正式美术资产需求表_v0.1.md
5. Worldbuilding/05_美术与音频/OCC_美术规范_v0.1.md
6. Worldbuilding/05_美术与音频/OCC_单位像素美术规范_v0.1.md
7. Worldbuilding/05_美术与音频/OCC_角色三层视觉规范_v0.1.md
8. Worldbuilding/05_美术与音频/OCC_像素资产_QA流程_v0.1.md
9. Worldbuilding/05_美术与音频/正式美术基准/ART-BASE/OCC_ART_BASE_审查记录_v0.1.md
10. Worldbuilding/01_游戏策划/OCC_火元素个人术式池_v0.1.md

必须使用并严格遵守这些技能：
- funplay-unity-dev：所有 Unity 状态检查、导入读回、重编译、Console、Play Mode、场景/Prefab/ScriptableObject 修改和截图审查。
- pixel-asset-pipeline：32×32、64×64、独立帧、strip/GIF、锚点、调色板、硬 alpha 和 QA。
- imagegen：需要新位图原料时使用内建 ImageGen，一项资产一次独立生成；AI 输出只能作原料，项目资产必须复制到工作区、规范化并 QA。

产品已正式批准且不可回退：
- ART-BASE-01 32 色正式主色表。
- ART-BASE-02 v0.2：1920×1080，左战场 1440、右 HUD 480，另验 960×540。
- 32×32 地块/图标；64×64 单位画布，X=32、Y=58。
- 标准单位主体 32–38px，精英/首领 38–44px，相对旧版约 70% 有效轮廓；禁止 Unity 运行时 0.7 非整数缩放。
- 正交、Point、Clamp、无 mipmap、硬 alpha、整数最近邻。
- 黑白/灰阶主导的极简工业细线 HUD；禁止厚金属板、铆钉、浮雕和斜切装甲框。

完成范围必须覆盖：
- ART-BASE-03 模板、正式 asset manifest/registry、自动覆盖和导入检查。
- 完整中继站地面/轨道/轻重掩体/中继器/箱体三段状态与连接变体。
- 八类环境地格和全部战术叠加。
- 主角模板 + 11 种敌人，共 12 个唯一战棋单位；现有 6 张按新体量重做，缺失 6 种独立制作。
- 12 单位左右方向及实际行为所需 idle/move/attack-or-cast/hit/defeat，盾/护盾/首领补专用动作。正式人形动画必须经过 Aseprite/PixelOver 或等效人工清理，禁止脚本矩形部件拼装。
- 24 个基础战斗 VFX、六状态反馈、物件破坏和 5 类火系 VFX 模板。
- 6 张现有指令图标复核；新制 33 张基础语义图标、8 类节点图标、F-P01–F-P50 共 50 张火系术式图标，以及当前运行时实际可达武器/盾具/导具/消耗品/奖励图标。
- 成熟统一的正式入口、20 节点地图、节点详情、商店、工坊、休整、事件、宝藏、权限门、简报、战斗 HUD、三选一、普通/精英/首领/失败结算、档案、存档/继续、设置；开发控制台独立且默认隐藏。
- 把全部通过 QA 的资产通过 Funplay 实装进实际游戏流程，消除可见 prototype、兵种复用冒充、临时字符、无语义矩形和缺失映射。

范围边界：
- 不改变玩法数值、AI、节点规则、奖励概率、存档语义或剧情正史。
- 不擅自制作未批准双元素、未冻结的其余七元素技能池、未定卷轴/法宝/装备目录、未定剧情主角/NPC/剧情首领/地区、宣传图或音频；这些保持 BLOCKED_CONTENT，不得用临时原创冒充完成。
- 当前工作树已有大量用户/既有改动。先记录 git status 基线，保留所有无关改动，不 reset、不覆盖、不格式化无关文件，不修改 Library/、Logs/ 或生成工程文件。

执行要求：
1. 先审计运行时代码，生成“每个可达 screen/archetype/skill/item/node/status/environment/feedback → 正式 asset ID → 路径 → 状态 → 使用点”的完整矩阵。
2. 建立模板、规范化/QA、Unity Importer 和自动覆盖测试，再连续生产全部静态、动画、VFX 和 UI 资产；每项独立原料、版本化保存，失败只返工失败项。
3. 通过 Funplay 检查活动场景和 dirty state。所有场景/Prefab/ScriptableObject 编辑使用 Funplay/Unity API，不手改 YAML；优先代码/运行时绑定，只有确需持久结构时才保存明确目标并读回验证。
4. 每次外部修改 Unity 资产或脚本后：退出 Play Mode（如需要）→ request_recompile → wait_for_compilation → get_compilation_errors → Console 检查。
5. 新增并运行正式资产覆盖、导入参数、唯一兵种映射、状态/VFX、50 火系图标、节点图标、无 fallback、动画锚点和场景引用测试；同时运行现有全部 EditMode 测试。
6. 用 Funplay 实际走通入口、新局/继续、全部节点类型、简报、战斗、奖励、结算、存档恢复、两个固定首领种子、胜利/失败、档案和设置；覆盖六状态、八环境、三段物件、护盾破裂、治疗、魔力恢复、移动/攻击/施术/互动/搜刮及五类火系 VFX。
7. 在 1920×1080 和 960×540 保存所有关键页面/状态截图；增加高密度遮挡场景验证人物 70% 轮廓、掩体、相邻单位、VFX、意图、血条和目标格同时可读。生成灰阶、红绿色觉风险、动画 GIF、接触表和最终 QA 索引，证据保存到项目目录。
8. 自动检查失败就修复并重跑。Funplay reload 的 502/timeout 先等待恢复并读回状态，不要误判失败；任何超时先确认是否部分成功。
9. 持续更新 OCC_当前待办、正式资产清单、需求表、QA、manifest 和验证记录，不要等到最后才补文档。

最终只能在以下全部成立时宣布 COMPLETE：
- 当前全部可达系统/稳定内容 ID 正式美术覆盖率 100%。
- 12 单位唯一、完整、正确体量/锚点/朝向/动作。
- 环境、三段物件、八环境、战术叠加、24 基础 VFX、5 火系模板全部完成并实装。
- 6 指令 + 33 基础 + 8 节点 + 50 火系 + 当前实际物品图标全部完成、唯一、QA 通过且实际使用。
- 所有正式页面达到统一成熟像素游戏品质，无可见 prototype/fallback/复用冒充/临时字符/无语义占位。
- Point/Clamp/无 mipmap/正确 pivot/PPU/整数缩放；无 Missing Reference、粉色材质或未审资产。
- 全部资产 QA、现有及新增测试、两首领完整流程、双分辨率视觉回归全部通过。
- Unity 编译错误/警告 0，Console 项目错误 0；退出 Play Mode；场景无非预期 dirty/save。
- 最终报告含资产覆盖矩阵、路径、QA、测试、Console、截图/GIF 索引、git 状态和 BLOCKED_CONTENT 排除项。

不要在最终条件全部通过前用“下一步建议”结束任务。开始执行。
```

