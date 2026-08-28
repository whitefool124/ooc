# OCC M-A18 木桌与调料瓶稳定性测试 v0.1

**状态：FORMAL_CANDIDATE / NOT IMPORTED（2026-08-23）。** 本批用于验证直接图像渠道在两种应用规格上的稳定性；产品已确认视觉结果并通过唯一机器合同，但尚未新增或覆盖 Unity 正式资源。

## 应用合同

### 木桌地图组件

- 用途：学院食堂／厨房／公共服务空间的一格低矮场景物；位于 12×9 正交地图，默认 128px 格显示 4×，最小 64px 格显示 2×。
- 交付：原生 32×32 透明 PNG；有效轮廓约 26×20px，四周至少 2px 安全边；正交俯视，不承担掩体数值。
- 一秒阅读：结实、反复维修的木制公共长桌；宽横向桌面为主形，木板分缝与两组桌腿／横撑为材质线索。
- 材质／色彩：深胡桃木、旧橡木、少量暗锻铁钉件；5–8 个离散颜色；无以太发光、无精密机械、无装甲包边。

### 调料瓶图标

- 用途：背包／拾取界面的材料或消耗品图标；32×32 UI 槽位。
- 交付：先成立为原生 24×24 材质物品，再不缩放居中到 32×32 透明交付画布；主体约 16–20px 高，保留 UI 呼吸边。
- 一秒阅读：一只装有暖赭色干香料的矮玻璃调料瓶；软木塞、收腰瓶颈和瓶内色块构成识别。
- 材质／色彩：近黑轮廓、旧玻璃灰褐、软木棕、暖赭香料；5–8 个离散颜色；一个连通高光簇、一个阴影块。

## 共用风格与排除

- 质朴、偏西幻魔法侧、近代学院日用品；物品本身不需要以太结构。
- 硬像素簇、硬 Alpha、无抗锯齿、渐变、柔焦、环境投影、文字、徽记或水印。
- 排除高科技瓶罐、实验室玻璃器械、金属胶囊、霓虹、管线、齿轮、蒸汽朋克装饰、华丽中世纪雕花、伪等距视角和额外物件。

## 生成提示词

### 木桌

`Use case: stylized-concept; Asset type: one independently generated OCC in-game 32x32 single-cell scene prop; Primary request: a sturdy repeatedly repaired communal wooden table for an academy dining hall, instantly readable as a low horizontal table; exact top-down orthographic view, isolated single object, broad plank tabletop with two or three large board divisions, two simple trestle supports visible beneath, a few dark forged-iron fasteners; rustic western-fantasy magic-side everyday object, no active magic; walnut and old-oak browns, near-black outline, 5 to 8 discrete flat colours, large connected pixel clusters; genuinely transparent background, generous safety margin. Exclude: perspective, isometric view, chairs, dishes, food, cloth, text, shadow, floor, glow, cyan, machinery, armor plating, gears, pipes, sci-fi, steampunk clutter, ornate carving, anti-aliasing, gradients, blur, watermark.`

### 调料瓶

`Use case: stylized-concept; Asset type: one independently generated OCC in-game material/consumable icon authored to read at native 24x24 and later centred without scaling in a 32x32 UI slot; Primary request: one squat glass seasoning bottle containing warm ochre dried spice, unmistakable silhouette from cork stopper, narrow neck, rounded-square body and visible spice fill; front-facing item icon, isolated single object; rustic academy kitchen everyday object, no active magic; near-black stair-step outline, old glass grey-brown, cork brown, warm ochre spice, 5 to 8 discrete flat colours, one connected highlight cluster and one shadow mass; genuinely transparent background, wide safety margin. Exclude: label, text, logo, extra bottle, table, floor, cast shadow, potion glow, cyan, laboratory flask, metal capsule, machinery, gears, pipes, sci-fi, steampunk clutter, ornate jewel, anti-aliasing, gradients, blur, watermark.`

## 生产与规范化记录

- 渠道：Codex 内建直接图像生成，两项资产分别生成；未使用本地生图工作台、localhost 服务或私有 relay。
- 原始来源：`raw/asset_stability_test_v01/academy_communal_wood_table_source.png`（SHA-256 `8c3978db...75a1d`）与 `seasoning_bottle_source.png`（SHA-256 `705a2a2d...5a1b75ad`）。
- 木桌：整轮廓缩至最大 `26×20px`，基线 `Y=28`，落在 `32×32` 画布的 `[3,13]-[29,28]`；硬 Alpha、8 色、安全边通过。规范化成品 SHA-256 `35358925...3683b5`。
- 调料瓶：原生 `24×24` 轮廓 `[5,2]-[19,22]`，7 色、硬 Alpha；再以 `(4,4)` 偏移不缩放嵌入 `32×32` UI 画布，交付轮廓 `[9,6]-[23,26]`。24px／UI32 成品 SHA-256 分别为 `d1d8d1e5...208d8` 与 `323b7e71...ea676`。
- 所有缩放检查均使用 Nearest／整数倍；规范化只做 Alpha 隔离、整轮廓缩小、调色板限制与居中，不补画结构。

## QA 结论

- 木桌：`PASS`。1×仍可辨认宽桌面、板缝、横撑和两组桌腿；放入当前学院庭院地砖后，默认 4×与低分辨率 2×均不越格、不产生透明边毛刺，地砖接缝不被误认为桌体轮廓。
- 调料瓶：`PASS`。1×仍可凭软木塞、细瓶颈、玻璃肩与暖赭香料色块辨认；24px 原生规格进入 32px 正式槽位后留有足够呼吸边，没有为了填满槽位而错误放大。
- 稳定性判断：本次两项“一次生成＋纯规范化”均通过，但稳定并不来自让两项共用画布或提示词，而来自先分别锁定地图格与 UI 槽的应用合同。后续桌椅类须继续限制为大轮廓、低纹理密度；小图标须先在原生 24px 通过，再进入 32px 槽位。
- 证据：`QA/asset_stability_test_v01/table/`、`QA/asset_stability_test_v01/seasoning_bottle/`、两份 `.occ-art.json` 与 `QA/asset_stability_test_v01/application_contact.png`。本批没有写入 Unity Assets，暂无 GUID／Importer；必须另行完成导入评审后才能从 `FORMAL_CANDIDATE` 升为 `FORMAL`。
