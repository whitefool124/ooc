# 技能分工与技术来源

核对日期：2026-10-09。社区教程只作技术参考，工程锁定版本和OCC总案优先。

## 已安装技能

| 技能 | 安装位置／来源 | 用途 |
|---|---|---|
| unity-lighting-vfx | `C:/Users/FNHF/.codex/skills/unity-lighting-vfx`；[Nice-Wolf-Studio](https://github.com/Nice-Wolf-Studio/unity-claude-skills/tree/main/skills/unity-lighting-vfx) | 光照、探针、Particle System、VFX Graph概念与模块 |
| unity-graphics | `C:/Users/FNHF/.codex/skills/unity-graphics`；[同仓库](https://github.com/Nice-Wolf-Studio/unity-claude-skills/tree/main/skills/unity-graphics) | URP、材质、Shader Graph及渲染资源概念 |
| unity:urp-postprocessing | 当前Unity插件提供，通过技能目录发现路径 | 实际Volume、HDR、相机及后处理配置 |
| shader-programming | `C:/Users/FNHF/.codex/skills/shader-programming` | 空间、UV、轨迹、溶解等Shader基础；具体API查URP17.6 |
| occ-pixel-vfx | 项目 `.codex/skills/occ-pixel-vfx` | 真实结算、存储提交、能力与反应分层、清理及既有否决 |
| occ-hd2d-vfx | 本技能 | HD2D光影、华丽空间特效、配方与实战验收 |

社区技能基于Unity6.3，不能视为6000.5／URP17.6代码保证。已保留上游备份、许可证，精简本机入口，并标明旧Render Graph调用与反射探针错误；引用手册按任务读取，不全量加载。没有安装付费资源、修改引擎包或更换模型。

同时找到 [poorvith-mp/skills-gamedev](https://github.com/poorvith-mp/skills-gamedev) 的unity-shaders，以及 [Unity-Open-MCP](https://github.com/AlexeyPerov/Unity-Open-MCP) 的粒子／VFX技能；前者与已安装Shader技能重叠，后者依赖另一MCP工作流，未装入OCC，避免工具栈冲突。

## 核对过的官方资料

- [Unity6000.5 Bloom](https://docs.unity3d.com/6000.5/Documentation/Manual/urp/post-processing-bloom.html)：用HDR发光与阈值集中亮度峰值，场景与UI边界仍需实际验收。
- [Unity6000.5 Particles Unlit](https://docs.unity3d.com/6000.5/Documentation/Manual/urp/particles-unlit-shader.html)：Blend、Soft Particles和Distortion输入要求；Opaque Texture的捕获时点决定可折射对象。
- [Unity6000.5 Render Graph Pass](https://docs.unity.com/en-us/engine/6000.5/manual/render-pipelines/universal-render-pipeline/customizing-urp/render-graph/write-render-pass)：官方检索命中当前RecordRenderGraph／AddRasterRenderPass模式，页面全文抓取超时；实现前再次查官方页或已安装包API，不能把本次检索当作代码编译验证。
- [VFX Graph17.6 Requirements](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@17.6/manual/System-Requirements.html)：兼容URP／HDRP；URP需Linear，GPU需Compute Shader和SSBO支持，平台支持有限，不能泛化为所有URP平台都可用。
- [ReflectionProbe.RenderProbe](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/ReflectionProbe.RenderProbe.html)：返回渲染ID；只查询ID完成状态，不在查询里重新发起渲染。

## 本次证据边界

本次完成联网技能筛选、安装、内容适配、源码／序列化配置检查与技术体系准备。未启动Unity、未验证活动Renderer或运行时覆盖值，未创建正式美术或实装新粒子，因此没有新增特效的画面、性能或审美通过结论。历史UI特效证据不能升级为HD2D空间特效证据。

## 2026-10-15 像素动画与美术补课

查阅SLYNYRD基础、动画入门、Wind Effects、Shmup31爆炸及3D Pixel Art Animation的公开正文；下载两份作者公开教程GIF并拆8个时间样本检查形态，仅作本地参考。结合VFX Apprentice公开Timing文章、Riot Art Education的VFX概览、Unity6000.5 Texture Sheet Animation与Custom Data手册整理[专项方法](pixel-animation-art.md)。研究记录含逐项来源与阅读边界，位于Worldbuilding/归档/2026-10-15_像素动画与特效学习。

Kevin Leroy的[Unity特效学习工程说明](https://realtimevfx.com/t/releasing-my-league-vfxs-fan-arts-for-study-purposes/1435)可用于研究分层；帖子指明Unity5.5、学习用途和代码局限，不能直接导入当前工程。VFX Apprentice付费课程仅阅读公开大纲，未购买、未观看付费课；Riot概览页已读，嵌入视频未观看；Riot旧PDF链接重定向、GDC大PDF未成功读取，均不计作已学完。
