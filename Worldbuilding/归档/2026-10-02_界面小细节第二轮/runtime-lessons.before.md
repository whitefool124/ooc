# 已验证的技术知识与适用范围

2026-10-02，Unity 6000.5 / URP 17.6：

- URP 离屏摄影机目标需要深度缓冲。展示场 RT 使用 Point / Clamp / 无 mipmap，在 endCameraRendering 后处理，避免读尚未绘制的源。
- 展示场的 240×180 左视口及整数缩放是研究样板；正式战斗的格尺寸、平移和裁切由 BattlefieldPresentationAdapter 与实际 Bootstrap 提供，不能硬套演示坐标。
- 项目原 URP renderScale=0.5。展示场临时克隆 Pipeline 并还原原配置；不要为了效果静默改写全项目渲染资产。
- CLI 后台运行时 runInBackground=false 会停住演示。演示可临时开启并还原；正式游戏先检查真实前台流程。不要因此修改所有游戏运行配置。
- CLI eval 在中文路径连续调用曾出现 Illegal byte sequence / QuickInstaller reflection 异常。首选已编译 Editor 菜单验证并将工具错误与游戏错误分开报告。
- 静态截图不能验证时间。像素投射、冲击和照明必须核对阶段时间；暂停后捕获 GameView，异步截图必须绑定实际模式，不能把下一模式截图标成上一模式。
- 当前战斗光效使用 UI 加法合成、六级衰减、八段时间和整数粒子格；它是局部亮度叠加，不等于表面法线照明、遮挡阴影或背景折射。这些高级能力应独立验收，不借用展示场证据声称已接入。
- 自定义 MaskableGraphic 显式要求 CanvasRenderer，测试需 ForceUpdateCanvases，不能只检查生成了组件。当前 uGUI 自定义顶点参数方案出现整片青色加法覆盖；改为每个有限播放槽持有独立材质、通过 uniform 传时间/族类/强度，已用真实命中画面复核。材质随实例销毁，不在每帧创建。不要把未经画面核验的顶点传参方案直接推广。
- 遵守 RectMask2D 和 stencil，Graphic 禁用 raycast。Shader 放 Resources 保证构建包含，不能只凭 Editor Shader.Find 成功。实战截图同时看战场和 HUD，确认光效没有跨视口遮蔽信息。

研究参考与原始验证见 `Worldbuilding/归档/2026-10-02_像素技术美术与特效研究`、`2026-10-02_非护盾特效与界面规划`；历史护盾演示已被用户否决，不能继续当作认可方向。

战斗外界面迭代（2026-10-02）：

- 资源扣除提示与服务成功特效必须在 `SaveMapRun` 成功后发布。先发资源变化、后保存，会在保存失败时播放误导反馈。
- 服务结果刷新不应再次淡入整页；使用 `Invalidate(false)` 即时显示结果，再在重建后的目标图标上播放短效果。把效果挂在即将销毁的旧按钮或旧页面上会丢失。
- 自定义路线 Graphic 的 RectTransform 必须覆盖地图画布，不能用零尺寸矩形承载分散顶点，否则 RectMask2D 的包围盒裁切会把有效路线剔除。
- 短提示的文字槽会被通用画框安全边压小，导致整行不可见；取当前字体 `MinimumTextSlotHeight` 与实际换行 `preferredHeight` 的较大值，再设置提示高度与文字槽。只检查 text 非空或 preferredHeight 不能验证文字可见，需检查实际生成顶点与渲染画面。
- 几何采用共享 uGUI 材质、整数像素位置、有限时间步，不使用逐帧材质分配、永久闪烁或全屏辉光。瞬态实例与页面共同取消，强度零不创建实例。
