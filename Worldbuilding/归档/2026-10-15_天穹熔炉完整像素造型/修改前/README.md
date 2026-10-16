# OCC 特效生产样板

当前样板为2026-10-15天穹熔炉像素疏化版。用户明确授权本特效采用体素建模、像素材质和必要粒子，并要求收细模型、加强像素风格；这是独立演出的专项做法，不自动改变其他场景的纸板优先规则。状态保持候选，不代表正式战斗技能、人工风格批准或品质基线。较厚的首版与2026-10-14光影v2均保留为历史对照。

## 实际入口

- `UnityProject/Assets/Game/Runtime/Presentation/VfxVoxelMesh.cs`：统一1/32m建模格，基础体积组合、内部面剔除、外露共面面片合并，生成真实厚度的像素模型；不是逐体素GameObject。
- `UnityProject/Assets/Game/Runtime/Presentation/SkyFurnaceVoxelStage.cs`：三层单像素断弧、两条单像素空间火流、双层炉芯、竖直束流及四条疏开卷曲冲击笔画；压缩和扩张形态预先缓存，播放时不重建网格、不缩放源颗粒。
- `UnityProject/Assets/Game/Runtime/Presentation/Shaders/Hd2DVfxVoxelSurface.shader`：原生32texel/m面UV、登记贴图的明确源窗口、真实法线照明、深度／投影和沿周向／高度有序显隐。独立几何与贴图分别溯源。

- `UnityProject/Assets/Game/Runtime/Presentation/Hd2DFireLightRecipe.cs`：独立灯光配方，蓄力、压缩、接触峰值、余热、环境主光恢复均可配置。亮度和照射范围不改变原生像素尺寸。
- `UnityProject/Assets/Game/Resources/Art/SkyFurnace/sky_furnace_fire_light.asset`：本样板配方；参数只供该场景使用，不能直接套入普通火术的结算窗口。
- `UnityProject/Assets/Game/Runtime/Presentation/Shaders/Hd2DVfxLitCard.shader`：双面像素纸板受光、薄片背光散射、硬Alpha轮廓和真实ShadowCaster。角色使用独立直立投影代理，避免倾斜纸板拉歪影子。
- `UnityProject/Assets/Game/Runtime/Presentation/Shaders/SkyFurnacePixelLayer.shader`：真实面法线、场景光与投影、可选深度交界淡出。烟尘原料保留方形网格，连续透明度只用于空间消散。
- `UnityProject/Assets/Game/Editor/SkyFurnaceAuthoring.cs`：后台光源开关／角色投影开关对比和原生分辨率截图。
- `UnityProject/Assets/Game/Editor/SkyFurnaceVerification.cs`：专属演出正常入口及重播、暂停、跳过、复位、禁用恢复、资源增长检查。不能代替正式战斗接入验证。

## 后续生产顺序

1. 确定一个主形、一个空间问题及真实播放窗口。来源于已授权内容；不自行增加结算等待、破坏或相机动作。
2. 选择场景像素间距并核对源主体网格和世界尺寸。需要新原料时独立内置生图，失败按原门槛重试；不可把着色器光效当成新美术来源。
3. 分别接入自发光、环境照明、受光表面和投影。Bloom不能替代灯；有代理不能证明材质有ShadowCaster。
4. 调整蓄力／压缩／冲击／余波曲线。保持纸板原生尺寸，靠位移、材质光学与密度变化控制动势；减少重复主形。
5. 同时点做开灯／关灯、开影／关影对比；看接触面、角色可读性、遮挡和峰值过曝，再看连续运动。固定镜头交付不引入旋转。
6. 验证生命周期和打包后的材质变体。显式序列化材质与Shader引用，检查实际独立程序图像，不能只看Editor。
7. 保存本轮原料、拒绝记录、manifest、密度清单和当前代码指纹。机器PASS、实机功能PASS与人工风格认可分开记录。全方位与正式游戏接入未做时明确标注。

## 已核实的技术约束

当前样板检查50个网格（47个缓存形态与3个地面符号网格），共72468顶点；这是全部缓存形态合计，不是单帧成本或性能成绩。格点误差、UV密度误差、无效三角形与空网格均为0。独立静态观察相机检查四个方位，主演出镜头保持固定。炉环和火流为1/32m单元的单格笔画；地面法阵采用8段短线、4个短刻度和中心角标，移除密集同心环、符文与长辐条。火流减为两条，冲击不再形成连续火墙。新独立内置生图材质采用4色大色簇，完整交付32×32，明确采样Unity坐标(8,1,16,15)不透明窗口，Point、无Mip、无压缩；UV保持32texel/m。局部Bloom为threshold2、intensity0.06、scatter0.30。单格笔画是本次用户反馈的造型选择，不是所有特效通用规格。

Unity6000.5.2f1、URP17.6.0的本地包源码已核对：Point投影使用`_CASTING_PUNCTUAL_LIGHT_SHADOW`、`ApplyShadowBias`及`ApplyShadowClamping`；URP阴影分辨率通过`UniversalAdditionalLightData`档位和拥有的Pipeline副本配置，`Light.shadowResolution`仅适用于Built-In，不使用。专用像素纸板材质无条件硬Alpha裁切，避免仅运行时启用关键字造成打包剥离；其Forward与DepthOnly使用相同足点深度规则，角色ShadowCaster使用直立代理。体素表面直接使用实际几何深度与投影，并在各Pass应用一致的有序显隐。

当前光源峰值95、范围9m等是这次样板参数，不是全项目标准。烟尘和疏化造型仍需要当前实机人工审核；四方位已有静态模型技术检查，其他视角的连续运动、硬件档位及正式战斗时序尚未验证。
