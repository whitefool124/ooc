# OCC 中文像素字体来源与集成记录 v0.1

## 采用字体

- 名称：Fusion Pixel Font / 缝合像素字体
- 变体：12px、Proportional、`zh_hans`
- 版本：2026.07.20
- 作者/维护者：TakWolf / Pixel Font Studio
- 官方仓库：<https://github.com/TakWolf/fusion-pixel-font>
- 官方发布：<https://github.com/TakWolf/fusion-pixel-font/releases/tag/2026.07.20>
- 发布包：`fusion-pixel-font-12px-proportional-ttf-v2026.07.20.zip`
- 发布包 SHA-256：`A6B32FE3E663BC3575DC8A71E1F5F1C17B5951558B0FBA9E5E75A33AFC2AB2DA`
- 导入 TTF SHA-256：`FFA464AAE492ED7A8526367DEBCCE62603CEC8157F59548CC50CEBF1ED81A53F`

## 许可

字体使用 SIL Open Font License 1.1。许可证副本随项目保存在：

`UnityProject/Assets/Game/Resources/Fonts/Licenses/FusionPixelFont-OFL-1.1.txt`

字体可嵌入游戏并随游戏分发；不得单独出售字体文件。字体及上游字形的许可说明以随包许可证和官方仓库为准。

## 选型结论

- 12px 比 8px/10px 更适合 OCC 当前的简体中文长说明与 960×540 紧凑画面。
- 比例版拥有自然字距和行高，官方建议无特殊排版要求时优先采用；比等宽版更适合当前按钮、节点标题和混合数字信息。
- `zh_hans` 使用简体中文语言字形，避免引入不需要的繁体/日文/韩文字形版本。
- 方舟像素字体官方仍提示缺少大量汉字，不作为当前正式 UI 字体。
- 三二像素体的游戏嵌入商业授权需另行购买，不纳入本项目。

## Unity 集成

- Runtime 路径：`Fonts/FusionPixel12ProportionalZhHans`
- 文件：`UnityProject/Assets/Game/Resources/Fonts/FusionPixel12ProportionalZhHans.ttf`
- Importer：12px、`HintedRaster`、嵌入字体数据、字符 padding 1。
- `FormalUiKit`、战斗反馈和旧场景 UI 入口统一使用 `FormalUiKit.Font`，不再直接加载 `SimHei`。
- `SimHei.ttf` 暂时保留为未绑定的历史文件，避免对用户已有资源执行未授权删除；正式运行时无引用。
