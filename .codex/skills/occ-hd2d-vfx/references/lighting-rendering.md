# 光影与渲染

适用于主菜单、战斗外空间页面与战场。以下参数为制作建议，先在代表场景对比，再按实测调整；不是新的产品固定规格。

## 当前实现证据（2026-10-09源码静态检查）

| 项目 | 证据与意义 |
|---|---|
| Unity／URP | `UnityProject/ProjectSettings/ProjectVersion.txt` 为6000.5.2f1；`Packages/manifest.json` 为URP17.6.0；未直接依赖VFX Graph |
| Renderer | `Assets/Settings/UniversalRP.asset` 默认索引0为Renderer2D；HD2D相机指定索引1，对应 `Assets/Game/Resources/Art/MainMenuE/Hd2D/HD2DRenderer.asset` 的UniversalRendererData、Forward路径；不能只看默认索引断言HD2D在2D Renderer里 |
| 颜色与采样 | PlayerSettings为Linear；管线HDR开启、renderScale=0.5、每物件附加光限制4；这是序列化配置，不代表运行时克隆／质量覆盖后的最终配置 |
| 战场 | `Hd2DBattlefieldSample.cs` 使用ARGBHalf RT、24位深度、HDR、ACES、Bloom与Gaussian景深，独立图层照明；已有暖主光和冷补光 |
| 主菜单 | `MainMenuHd2DStage.cs` 使用HDR RT、独立Volume、真实灯光及相机；应与战场分别调校 |
| 深度／颜色副本 | 管线资产 `m_RequireDepthTexture=0`、`m_RequireOpaqueTexture=0`；相机或Renderer可覆盖。软粒子、折射前需实时核对，不声称“有深度缓冲就有可采样Depth Texture” |

历史 `MainMenuPixelScene.shader`、`MainMenuStagePresentation.shader` 中320×180演示不是当前全项目输出合同。现有场景运行时可能克隆材质或配置，静态文件不能证明实机效果正常。

## 光照分层

| 层 | 作用 | 制作方法 |
|---|---|---|
| 环境基底 | 清楚辨认地形、物件与像素主体 | 克制环境填充；稳定曝光与色调，保留暗部色相，不用黑场掩盖模型接触问题 |
| 主光 | 形成统一方向、长短投影与空间体积 | 战场以现有日光为基础；桌面使用窗光／晶灯。观察脚底接触、墙边偏移及面片代理投影 |
| 材质响应 | 区分石材、纸、黄铜、水与晶体 | 石材低光泽；纸与布以粗糙漫反射为主；黄铜控制窄高光；水面运动与倒影另行测量 |
| 术式瞬态光 | 爆发照亮地面、墙面、人物 | 少量池化Point／Spot Light、短包络与明确作用半径，默认无阴影；有阴影的英雄光仅在收益明确时启用 |
| 空气层 | 建立前中后景和体积感 | 稀疏受光尘点、局部烟片、薄雾和遮挡感；真正体积积分另算能力，不把透明锥体称为真实体积光 |
| 后期 | 集中亮度峰值、保持画面色相 | HDR Emission→局部Bloom→稳定Tonemapping；UI文字在受控合成层保持清晰 |

爆发光需照亮真实表面。验证时禁用发光粒子只留灯，再禁用灯只留发光粒子，分别证明受光与自发光。光源半径随效果空间尺度，不随RT分辨率改变；图层和Rendering Layer Mask必须与实际相机／材质支持一致。

## 面片和阴影

保持现有脚底锚定、45°战场面片与直立投影代理。新粒子不能重新定义角色法线、拉长投影或把颜色通道与深度通道的修正拆开。检测窄墙前、墙后、两单位交错和边界位置；粒子是否穿墙、软粒子是否吃掉角色边缘，需要实机运动确认。

用Alpha Clip主体保持轮廓及深度，透明烟尘通常ZWrite Off并读取场景深度；可见面片若使用自定义受光Shader，检查Forward、ShadowCaster、DepthOnly，以及所选功能需要的DepthNormals一致。若不是法线材质合同，不自动从像素图生成“正式法线美术”；可先评估共享几何法线／技术代理的局限。

## Bloom、景深与HDR

- Bloom从Emissive峰值调起，避免把全局阈值降低到纸面、文字和地面一起发白。战场已有threshold1.25、intensity0.18、scatter0.45作为现状比较点，不是永久上限。
- 单次命中允许很亮的紧凑核心、细亮边、宽而弱的光晕；主体形状主要由不透明／Alpha主层读取，Bloom只是外层。
- 战场景深保护所有可交互格与范围；远景可虚化。桌面焦点放在当前操作物件，正文与按钮仍需清楚。使用实际投影检查，不能只调焦距数值。
- 不默认给战场加Motion Blur、全局Chromatic Aberration或持续镜头震动。冲击主要来自局部形变、轨迹宽度、命中顿挫及亮度包络。
- Pixel纹理Point采样与高清RT可以并存。检查renderScale、显示窗口与RT尺寸三者；配置0.5不能当成已输出原生1920×1080。

## 折射、软粒子与自定义Pass

URP粒子Distortion／Scene Color读取Opaque Texture，通常不包含随后绘制的透明对象；不会自然折射UI。启用采样之前确认HD2D相机实际生成颜色副本，热浪贴局部空间，控制屏幕边缘Clamp与深度遮挡。若必须折射透明像素主体，先确定主体是Alpha Clip不透明通道还是透明通道，再设计明确的颜色捕获时点；不以调渲染队列保证一个未生成的输入存在。

URP17.6自定义功能先查当前Render Graph模式和已安装API。现代图路径用 `RecordRenderGraph`、`AddRasterRenderPass`、`UseTexture`、`SetRenderAttachment` 等与实际包匹配的调用；旧 `AddRenderPass/ReadTexture/UseColorBuffer` 范例不能直接照搬。不为让旧教程运行而关闭全项目Render Graph。

深度纹理与深度附件不同，Gaussian景深、软粒子、投影环各自核对依赖。场景颜色源与目标不能在同一Pass无声明地读写同一纹理；使用受支持的中间资源／Blit，过滤相机、图层及SceneView，避免主菜单Pass影响战场或UI。

## 进阶技术的启用条件

| 技术 | 什么时候值得做 | 先验证什么 |
|---|---|---|
| Light Cookie | 晶灯、窗框、旋转能量纹在真实表面投光 | 对应光类型、Forward限制、Cookie图集及纹理来源 |
| Reflection Probe | 桌面黄铜／稳定室内反射 | 使用按需更新；保存RenderProbe返回ID，查询完成不能再次发起RenderProbe |
| 烘焙GI／APV | 可复用静态空间需要稳定间接光 | 场景保存授权、静态几何与烘焙数据生命周期；运行时生成庭院不能直接借用静态烘焙结论 |
| 真实体积光 | 单一代表场景中烟雾受遮挡光束收益明显 | 官方能力／自定义方案、深度输入、成本和目标GPU；未实施时明确为研究项 |
| Forward+ | 真实测到Forward每物件灯光限制成为问题 | 项目版本及相机兼容、性能与光照一致性；不预先切管线或写质量资产 |

官方链接与社区技能适用边界见 [sources.md](sources.md)。
