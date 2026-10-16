# OCC 特效生产样板

当前可复用实现为天穹熔炉光影迭代v2，状态是运行验证候选；不代表正式战斗技能、全方位风格审核或批准品质基线。

## 实际入口

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

Unity6000.5.2f1、URP17.6.0的本地包源码已核对：Point投影使用`_CASTING_PUNCTUAL_LIGHT_SHADOW`、`ApplyShadowBias`及`ApplyShadowClamping`；URP阴影分辨率通过`UniversalAdditionalLightData`档位和拥有的Pipeline副本配置，`Light.shadowResolution`仅适用于Built-In，不使用。新增材质无条件硬Alpha裁切，避免仅运行时启用关键字造成打包剥离。Forward与DepthOnly使用相同足点深度规则，ShadowCaster使用直立代理的真实几何。

当前光源峰值95、范围9m等是这次样板参数，不是全项目标准。烟尘和主火冠仍需要当前实机人工审核，其他视角、硬件档位及正式战斗时序尚未验证。
