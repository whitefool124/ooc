# OCC M-A18 蓝宝石法杖与宿舍床稳定性测试 v0.1

**状态：FORMAL_CANDIDATE / NOT IMPORTED（2026-08-23）。** 本批只验证直接图像渠道对细长装备与多格地图物件的稳定性；产品已确认视觉结果并通过唯一机器合同，但尚未新增或覆盖 Unity 正式资源。

## 应用合同

### 木质蓝宝石法杖图标

- 用途：装备／背包界面的法杖图标，进入现有 32×32 正式装备槽。
- 交付：原生 32×32 透明 PNG；左下至右上的对角主轮廓，有效长度约 25–27px，四周至少 2px 安全边。
- 一秒阅读：旧木长杖、分叉杖首、以皮绳固定的一枚天然蓝宝石；木杆承担绝大部分面积，宝石是小而清楚的识别点。
- 材质／色彩：近黑轮廓、深胡桃木、磨亮旧木棕、皮绳褐、深蓝与一簇浅蓝宝石高光；最多 8 色。蓝宝石不是正在工作的以太灯，不允许大面积冷青光。

### 学院宿舍单人床地图组件

- 用途：学院宿舍／医务休息区中的横向多格场景物；位于 12×9 正交地图。
- 交付：原生 64×32 透明 PNG，逻辑占格严格为横向 2×1；床头在左、床尾在右，有效轮廓约 58×26px，四周至少 2px 安全边。默认每格 128px 时整图为 4×，最小每格 64px 时为 2×。
- 一秒阅读：朴素的学院单人木床，左侧略高床头板、右侧较低床尾板、薄羊毛床垫、一个枕头与一条折叠毯；不得像柜台、箱子或祭坛。
- 材质／色彩：深木框、旧橡木边、燕麦色床垫、低饱和灰蓝毯、骨白枕头；8–10 色，无魔法发光或金属机械结构。

## 共用风格与排除

- 质朴、偏西幻魔法侧、近代学院日用品；反复维护和手工修补优先于华丽装饰。
- 硬像素簇、硬 Alpha、无抗锯齿、渐变、柔焦、环境投影、文字、徽记或水印。
- 排除高科技发光棒、漂浮宝石、金属能量导轨、机械床、医疗舱、霓虹、管线、齿轮、蒸汽朋克装饰、王室雕花、透视地板、额外物件和错误占格。

## 生成提示词

### 木质蓝宝石法杖

`Use case: stylized-concept; Asset type: one independently generated OCC in-game equipment icon designed for a native 32x32 pixel grid and an existing 32x32 UI equipment slot; Primary request: one humble wooden sapphire staff, instantly readable from a long bottom-left to top-right diagonal wooden shaft, a small forked wooden head and one modest rough-cut deep-blue sapphire tied securely into the fork with brown leather cord; wood must dominate the object, the sapphire is a small natural material accent with one connected pale-blue highlight cluster, not a light source; rustic western-fantasy magic-side academy equipment, repeatedly handled and repaired; near-black stair-step outline, walnut and worn oak browns, leather brown, deep sapphire blue, at most 8 discrete flat colours, large connected pixel clusters; single centred object, genuinely transparent background, at least 2px safety margin. Exclude: character, hand, second staff, text, label, slot frame, floor, cast shadow, floating gem, cyan aura, bloom, neon, metal energy rail, gun silhouette, machinery, gears, pipes, sci-fi, steampunk clutter, ornate royal carving, anti-aliasing, gradients, blur, watermark.`

### 学院宿舍床

`Use case: stylized-concept; Asset type: one independently generated OCC in-game horizontal 2x1-cell map prop authored for a native 64x32 transparent pixel canvas, each logical cell 32x32; Primary request: one plain academy dormitory single bed, exact orthographic top-down tactical view, headboard on the left and footboard on the right, a long honest two-cell footprint; visibly wooden side rails, slightly taller simple left headboard, lower right footboard, thin oat-coloured wool mattress, one bone-white pillow near the head and one folded desaturated grey-blue blanket; rustic western-fantasy magic-side everyday furniture, repeatedly maintained, no active magic; near-black stair-step contour, walnut and old-oak browns, 8 to 10 discrete flat colours, large connected pixel clusters, no texture smaller than it can survive at native size; genuinely transparent background, effective silhouette about 58x26px with at least 2px safety margin. Exclude: perspective room, isometric view, floor, rug, bedside table, person, canopy, royal carving, hospital machinery, capsule, metal frame, glow, cyan energy, gears, pipes, sci-fi, steampunk clutter, cast shadow, anti-aliasing, gradients, blur, text, watermark.`

## 生产与规范化记录

- 渠道：Codex 内建直接图像生成，两项分别生成；未使用本地生图工作台、localhost 服务或私有 relay。
- 原始来源：`raw/asset_stability_test_v02/wooden_sapphire_staff_source.png`（SHA-256 `04b0eca0...dcc514`）与 `academy_dormitory_bed_source.png`（SHA-256 `09a0b3dd...9844f0`）。
- 法杖：整轮廓缩至 `32×32` 画布 bounds `[3,2]-[28,29]`，8 色、硬 Alpha、安全边通过；对角长度和方向未被拉伸。成品 SHA-256 `5a5bf714...24b0ff`。
- 床：整轮廓缩至 `64×32` 画布 bounds `[3,2]-[60,29]`，10 色、硬 Alpha、安全边通过；逻辑占格 `[2,1]`，未按单格压扁。成品 SHA-256 `7dfcdc74...472385`。
- 规范化只进行 Alpha 隔离、整轮廓 BOX 缩小、硬 Alpha、调色板限制和居中；没有补画结构。所有 QA 缩放为 Nearest／整数倍。

## 返修记录

- 法杖首轮机械报告虽满足8色，但全局等权限色把小面积蓝宝石吞成木色，审美门禁判为失败。
- 修正为材质分区限色：在总色数不变的情况下，为原图中已经存在的蓝色像素预留2个颜色，其余6色承担木杆、皮绳和轮廓；没有新画宝石，也没有扩大蓝色面积。
- 同一规则用于床的低饱和灰蓝毯：总色数10，其中2色保留原图已有蓝灰材质。该规则只服务于“语义材质不能被大面积基材吞没”，不得用于凭空添加元素色。

## QA 结论

- 木质蓝宝石法杖：`PASS`。1×凭左下至右上的长木杆和分叉杖首读作法杖，深蓝宝石保持小面积天然材质点；进入现有32px装备槽后不贴边、不像枪械或科技发光棒。
- 学院宿舍床：`PASS`。1×仍可区分左侧床头、枕头、薄垫、灰蓝毯、右侧床尾与木框；在现有学院地砖上横跨相邻两格时，格中线不切断床体阅读，2×／4×均保持诚实占地。
- 稳定性判断：直接生成对“大轮廓多格家具”较稳定；细长装备的轮廓也稳定，但小面积语义材质需要显式色额门禁，不能只检查总色数。
- 证据：`QA/asset_stability_test_v02/wooden_sapphire_staff/`、`QA/asset_stability_test_v02/academy_dormitory_bed/`、两份 `.occ-art.json` 与 `QA/asset_stability_test_v02/application_contact.png`。本批没有写入 Unity Assets，暂无 GUID／Importer；必须另行完成导入评审后才能从 `FORMAL_CANDIDATE` 升为 `FORMAL`。
