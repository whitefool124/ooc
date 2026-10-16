# 图书馆陈设尝试与建筑法线接入

状态：QA_PENDING。没有新增可用书柜美术；本次实装是建筑法线接口和网格切线支持，不代表当前场景已经使用了完整法线/表面图。

## 原料结果

三轮均为独立Codex内置生图，原料与解码记录分别保存。没有降低1.2阈值、几何缩放、抖色或新增色彩来强行入库。

| 轮次 | 主体网格置信度 | 结果 |
|---|---:|---|
| R1 | 1.0528 | REJECT_UNSTABLE_SUBJECT_GRID |
| R2 | 1.0035 | REJECT_UNSTABLE_SUBJECT_GRID |
| R3 | 1.0405 | REJECT_UNSTABLE_SUBJECT_GRID |

原料没有复制到Unity；decoded文件仅用于失败诊断，不是交付素材。图书馆继续使用上一候选环境配置。

执行依据是实际读取的 `C:/Users/FNHF/.codex/skills/occ-art-direction/SKILL.md` 第7步：“require lattice confidence at least `1.2`, and retry independently rather than relaxing the threshold. A retry changes only the diagnosed failed dimension; stop after three independent rounds.” 这是明确停止重试要求；同场景像素大小同时受OCC总案A.2.4最高优先级约束，不因美观放宽。

## 代码实装

- `Hd2DBakedArchitecture.shader`新增标准 `_BumpMap` 属性及 `_NORMALMAP` 变体，使用URP既有法线解包与切线空间转换。默认未启用，不为缺失贴图伪造法线。
- `Hd2DAcademyEnvironment`为建筑面、坡屋顶和纸板生成切线，保持原法线、UV及32 PPU不变，使未来配套法线数据有完整转换基础。
- 增加无窗口、无Play Mode的单个Shader变体编译检查。没有运行全目录旅程，未打开、置顶或聚焦任何窗口。
- 局部检查脚本最初把HasProperty用于Shader，产生CS1061；已改为Shader.FindPropertyIndex并重新检查，失败未隐藏。

`normal_variant_check.json`只证明法线变体编译准备情况，不是渲染图、完整游戏流程或审美证明。当前没有新增法线/粗糙度贴图，没有从颜色亮暗推算表面属性。实际灯光响应、配套图对齐、两个分辨率、运动与人工审美仍待后续局部验证。
