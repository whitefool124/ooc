# OCC M-A18 学院战场环境美术重置 v0.1

**状态：PRODUCT_REJECTED / PROTOTYPE（2026-08-23）。** 产品实机复核否决本批 v08：庭院主地面错误覆盖已确认的 v07，脚本几何道具虽通过机械规格但像素美术质量不足。该批只保留为拒收审计证据，不得作为正式资产来源或再次覆盖 Unity。

## 1. 玩家阅读与风格合同

- 32×32 原生逻辑像素；地面全不透明，道具与掩体使用二值 Alpha 并保留至少 1px 透明安全边。
- 学院首区以暖灰切石、灰浆、夯土、旧木、锻铁、羊皮纸、苔草和少量旧铜构成；冷青只出现在仍工作的以太槽、导能柱和封印节点。
- 禁止现代钢板、LED、警示条、整块霓虹、枪械／机械装置轮廓、临时色块和用红色斜杠冒充破损。
- 每个地块最多 8 个可见色；以太连接件最多 9 色；透明道具／掩体最多 12 色。所有边缘为硬像素，不使用半透明、抗锯齿、渐变或运行时绘制。
- 轻掩体保持低矮横向轮廓；重掩体保持厚重直立轮廓。`intact/damaged/rubble` 三态继承同一材质和占地，损坏通过缺角、断裂、散落和重心下降表达。
- 地面严格正交俯视；有高度的掩体、设施和容器必须使用战棋俯视 3/4：顶面可见、前立面压短、脚点位于格内下半部。正面立视或建筑立面贴图即使单图清晰，也不得通过实机门禁。

## 2. 生产与来源

| 项目 | 路径／结论 |
| --- | --- |
| 方向参考 | `raw/battlefield_reset_v08/reference/academy_battlefield_rustic_reference_v01.png`；仅用于材质、轮廓和色彩方向，不切片为正式资产 |
| 拒收像素源／导出器 | `tools/produce_academy_battlefield_v08.py`；脚本几何部件稿，仅保留审计，不得参与正式生产或运行时覆盖 |
| 规范化输出 | `normalized/battlefield_reset_v08/`，48 张独立 PNG |
| 机械报告 | `QA/battlefield_reset_v08/battlefield_reset_v08_report.json`，48/48 PASS |
| 人工接触表 | `QA/battlefield_reset_v08/candidate_48_contact_4x.png`；确认地块主次、道具轮廓和三态连续性 |
| 12×9 混合场景 | `QA/battlefield_reset_v08/battlefield_mixed_12x9_2x.png`；确认相邻地块、连接件和道具叠加阅读 |
| 覆盖前证据 | `rejected/formal_academy_combat_before_v08/`；旧 PNG 来自上一批 `normalized/terrain/`，旧 `.meta` 与 GUID 快照保留 |

方向参考由 Codex 内建直接图像生成能力产生；未使用本地生图工作台、localhost 服务或私有 relay。v08 的 48 张 PNG 为脚本绘制产物，不满足正式像素资产“独立美术来源”门禁，原 `FORMAL / RUNTIME_COMPLETE` 结论撤销。

## 3. 数量与规格

- 地面与连接件 27 张：石路 4、庭院 4、废墟 4、刻阵地 4、夯土 3、草边 4、以太连接件 4；全部 32×32、Alpha=255、4–9 色。
- 掩体／任务物 18 张：石凳、种植槽、档案架、砌体屏、导能柱、封印台各 3 态；全部 32×32、Alpha 仅 0/255、3–8 色、安全边 PASS。
- 战利品箱 3 张：关闭、开启、空置；全部 32×32、Alpha 仅 0/255、3–7 色、安全边 PASS。
- Unity 正式路径保持 `Assets/Game/Resources/Art/FormalAcademyCombat32/`；只替换同名 PNG 内容，48 个 `.meta` 未重建。

## 4. 实机视角返工

首轮接触表通过后，真实战斗截图发现档案架、砌体屏和导能柱仍偏正面立视，与严格俯视地面不在同一投影体系，因此未直接放行。返工将档案架改为腰高档案束，砌体屏改为有顶盖的低墙，并为石凳、花槽、导能柱、封印台和箱体统一增加可读顶面、压短前沿与下半格接地点。新版重新通过 48/48 机械 QA，并在以下实机截图复核：

- `UnityProject/Artifacts/M-A18/battlefield_reset_v08_perspective_1920x1080.png`
- `UnityProject/Artifacts/M-A18/battlefield_reset_v08_perspective_960x540.png`

## 5. 撤销放行

原机械测试与 Unity 读回只能证明尺寸、导入设置和代码接线，不能证明美术质量。2026-08-23 起：`academy_courtyard_a-d` 恢复 v07；v08 道具／掩体必须由独立正式来源重制并重新经过 1×／4×／灰阶／棋盘格、三态接触表和真实战场遮挡门禁。不得回到本批脚本像素源修正，不以运行时滤镜、裁切或 fallback 掩盖。
