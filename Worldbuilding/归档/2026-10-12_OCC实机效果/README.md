# 雨后灯庭实机截图

`rain_lantern_court_live.png` 来自 Unity Editor Play Mode 的真实 B1 战斗相机输出，使用现有正常场景构建与渲染代码、实际人物、地表、水面、灯光、阴影和后处理；不是离屏单材质测试板，也不是Player试玩包截图。

只通过已存在的存档写入隔离预览入口进入这一战斗，没有执行全旅程、移动、战斗结算或写入玩家进度。图片是实际战场RenderTexture（1888×768），不含HUD；GPU转为sRGB后原样导出，没有缩放、重着色或修图。该场景不保证同时包含上一轮的全部木架、宝箱和诱导灯，启用数量以最新capture.json为准。

进入Play前读取已有PlayModeView，将其临时设为PlayUnfocused，并临时关闭进入Play时创建Game窗口；不创建、打开或聚焦EditorWindow。退出后恢复原值，保留原主场景，未保存场景。实际清理记录见cleanup.json。

首轮r1图像有效，但材质数量查找误用UI宿主子树，因此r1的两个材质计数字段不构成证据；最新版本改从战斗相机所属世界根遍历并重新截图。保留r1追溯。本次没有性能、正常入口全流程、完整密度或人工审美批准，不晋升APPROVED_BASELINE。

后台Play控制参考Unity公开源码中的PlayUnfocused和openWindowOnEnteringPlayMode，并由本机实际API复核：[Unity PlayModeView源码](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/PlayModeView/PlayModeView.cs)。
