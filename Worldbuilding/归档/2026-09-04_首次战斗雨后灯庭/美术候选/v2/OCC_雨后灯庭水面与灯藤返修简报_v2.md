# OCC 雨后灯庭水面与灯藤返修简报 v2

日期：2026-09-04

## 任务合同

- 目标：替换已被产品审美否决的首版水面与灯藤候选，使地块在 1× 下先读出玩法性质，再读出材质与世界观。
- 范围：各生成一份独立原料，规范化为 `floor_tile_32` 与 `single_cell_prop_32`；不导入 Unity，不改变规则与关卡数据。
- 验收：32×32、硬 Alpha、色数与透明边界满足机器合同；完成 1×、4×、灰阶、棋盘格和应用接触；人工审美保持待确认。
- 解锁：用户批准后，才可补稳定 GUID、Importer 与真实运行时复核并晋级 `FORMAL`。

## 水面：浅水石板

- 角色与一秒读法：地表瓦片；应读成“雨后积在学院石板上的浅水”，可进入、减速、灭火，不造成伤害、不遮挡视线。
- 应用环境：`rain_lantern_court` 的 C7–G7，一格一张完整 32×32 地板；与石板相邻，单位与移动／攻击覆盖层会叠在其上。
- 轮廓与形状：完整方形石板仍可见；一块不闭合的浅水面覆盖大部中心，水缘为低对比不规则阶梯线；不使用独立金属框、圆角面板或跨格连续纹理。
- 材质叙事：冷灰学院石板的凹洼积雨，透过水能看到石材；两段断开的浅灰蓝天光反射和一处细小水缘波纹确认“液体”。
- 调色板：最多 6 色。石灰／煤灰为底，低饱和灰蓝为水，浅灰蓝为反光；不使用主动以太的高饱和冷青，不使用安全黄与危险红。
- 明度与密度：水面与干石板有清楚但低于战术覆盖层的明度差；三块信息预算为石板底、浅水主体、断裂反光；禁止高频涟漪、泡沫、雨滴与碎裂噪点。
- 视角：严格正交俯视，无立面、无透视、无投影。
- 锁定不变量：看得到底材；没有容器边框；没有危险感；单格自足，主要痕迹不碰四边。

### 水面生成提示词

```text
Use case: stylized-concept
Asset type: independently generated raw material for one OCC 32x32 orthographic battlefield floor tile
Primary request: one complete square academy courtyard stone tile holding a shallow rain puddle; the player must instantly read wet shallow enterable water that slows movement and extinguishes fire, never a hazard
Scene/backdrop: the tile fills the whole image, no surrounding scene
Subject: pale weathered limestone slab remains visibly present beneath and around a broad irregular shallow puddle; one low-contrast stepped water edge, two short broken sky-reflection streaks, one tiny restrained ripple; no separate container or frame
Style/medium: deliberately coarse native-32-pixel game art, large connected square pixel clusters, hard edges, six discrete flat colors, no anti-aliasing, no gradients
Composition/framing: strict orthographic top-down, one self-contained square gameplay tile, internal surface marks stop before the tile perimeter, neutral light with no cast shadow
Color palette: desaturated stone gray, charcoal seam, muted blue-gray water, pale gray-blue reflections; no saturated cyan
Materials/textures: shallow transparent-looking rainwater over worn academy stone, readable stone below the water
Constraints: one tile only; physical stone perimeter is restrained and must not resemble a UI button; no cross-tile pattern; readable at 32x32
Avoid: brass or metal border, framed panel, glass screen, pool basin, sewer grate, icon, emblem, isometric cube, perspective, deep ocean, waterfall, hazard stripes, glow, text, watermark, soft blur, random single-pixel noise
```

目标尺寸预审发现初稿把一格拆成多块铺石，因此对独立水面原料执行一次定向编辑，锁定最终原料所对应的修正：

```text
Edit the supplied water-tile image only. Preserve the broad irregular shallow rain puddle, muted blue-gray water, visible submerged stone, broken reflection streaks, strict orthographic top-down pixel-art rendering, and square framing. Make one targeted structural correction: replace the many small surrounding paving stones and every internal masonry seam with ONE single complete pale academy limestone slab for this gameplay cell. The slab may have one restrained physical perimeter seam and at most two tiny low-contrast wear marks that stop before the edge. Keep the puddle shallow, enterable, non-hazardous, and borderless. Use large connected pixel clusters and a six-color flat palette. Exclude nested masonry, multiple slabs, tile subdivisions, metal/brass border, glass panel, pool basin, UI-button bevel, perspective, glow, text, gradients, blur, and random noise.
```

## 灯藤：可穿行的遮视藤屏

- 角色与一秒读法：单格可进入物块；应读成“人能挤过、视线穿不过的密藤屏”，入格消耗 2，藤内仅能攻击相邻目标，可被合法火焰烧毁且不爆炸。
- 应用环境：E1–F5 成片重复布置，每格独立销毁；覆盖石板地表，单位会站入同格，因此中心下部必须留出可见通行洞与脚底空间。
- 轮廓与形状：不对称的窄底宽冠藤屏；两根细旧铁／铜导架提供可维护结构，三至四股藤茎在中上部交织成密实横向遮视带，下部中央形成开放拱口。禁止圆环、花环、徽章、盆栽或完整墙板轮廓。
- 材质叙事：学院旧庭院的灯藤被夹扣固定在检修支架上；两到三枚暖黄灯荚通过短铜箍供能，亮点很小且由植物和锻铁暗部承托；干细枝与叶团表达可燃性，但资产本身不带火焰。
- 调色板：最多 10 色。深炭轮廓、两级旧铁／铜、三档灰绿藤叶、暗褐枝条、两档暖黄灯荚；不使用冷青能量，不使用危险红。
- 明度与密度：中上部形成连续暗质量块以表达挡视线；灯荚仅为 2–3 个小焦点；底部开放区保持轻，避免误读成不可进入重物块。
- 视角：战棋俯视 3/4；顶面可见，前立面压短，无消失点与环境投影。
- 锁定不变量：2px 透明安全边；硬 Alpha；可穿行洞明确；遮视冠部明确；灯荚非爆炸装置；重复成片时不形成圆徽章墙纸。

### 灯藤生成提示词

```text
Use case: stylized-concept
Asset type: independently generated raw material for one OCC 32x32 enterable battlefield prop with genuine transparent background
Primary request: one asymmetrical lamp-vine screen that clearly reads as a dense shoulder-high plant mass blocking sight while a person can push through its open lower-center passage; it is combustible but not currently burning
Scene/backdrop: genuinely transparent background, no floor and no shadow
Subject: two slim repaired wrought-iron and aged-copper trellis posts, three or four thick gray-green vine stems woven densely across the upper and middle silhouette, a clear dark arch-shaped passage in the lower center, sparse dry tendril tips, two or three small warm yellow lamp pods held by visible copper clamps
Style/medium: deliberately coarse native-32-pixel game prop, hard one-pixel near-black stair-step outline, large connected pixel clusters, at most ten discrete flat colors, no anti-aliasing, no gradients
Composition/framing: tactical top-down three-quarter view without vanishing point, narrow base and broad irregular crown, subject occupies about 24x27 pixels inside a 32x32 cell, at least two transparent pixels on every edge
Lighting/mood: neutral diffuse courtyard light; lamp pods are small controlled warm accents, no bloom
Color palette: charcoal, dull iron, aged copper, dark brown stems, three muted gray-greens, two warm lamp yellows; no cyan and no red
Materials/textures: maintained academy garden infrastructure, handmade clamps and repaired trellis rather than fantasy ornament or steampunk machinery
Constraints: single object only; upper mass must be optically dense, lower center visibly passable; preserve true transparency and hard alpha
Avoid: circle, wreath, halo, medallion, emblem, badge, icon, pot, barrel, bush ball, solid wall, rectangular plaque, symmetrical crest, explosion, flame, electric arc, lantern post, text, watermark, soft shadow, glow cloud, painterly foliage, random one-pixel noise
```

初次规范化后，首轮返修仍在 1× 下读成高大花架门，因此未作为最终原料；保留为 `raw/lamp_vine_source_v2_attempt1.png` 追溯。最终原料来自以下定向编辑：

```text
Edit the supplied lamp-vine prop only. Preserve its transparent background, muted gray-green living vines, old iron/copper maintenance supports, two warm lamp pods, combustible plant identity, and hard pixel-art treatment. Make one decisive gameplay-readability correction for a native 32x32 tactical cell: turn the tall architectural gateway into a squat, broad, shoulder-high VINE SCREEN. Compress the crown downward and simplify all foliage into exactly three large connected leaf masses with only a few thick stems. The upper and middle 70 percent should form one dense irregular sight-blocking band. Keep only a narrow 4-to-6-pixel-equivalent lower-center dark opening so a person can visibly push through; this is a passage in foliage, not a doorway or arch building. Reduce the trellis to two thin repaired stakes mostly hidden by the plant. Keep exactly two small asymmetrical warm-yellow lamp pods secured by copper clips. Use a dominant 24x24 silhouette inside the 32x32 cell, at least two transparent pixels on every edge, at most ten flat colors, large square pixel clusters, hard alpha, no antialiasing or gradients. Exclude archway, gate, doorway, building, circle, wreath, emblem, symmetrical crest, pot, wall slab, individual tiny leaves, dangling detail noise, fantasy ornament, steampunk machinery, floor, shadow, bloom, flame, text, and watermark.
```

## 目标尺寸复核清单

- 水面 1×：先读“浅水”，仍看到石板；绝不读成金属／玻璃框板或深水危险格。
- 水面 4×：水缘、反光均为连续像素簇；无抗锯齿、渐变、随机噪点和高对比按钮边。
- 灯藤 1×：冠部密、底部通，非圆环／徽章；暖灯荚只作身份点。
- 灯藤 4×：两像素安全边、硬 Alpha、藤茎与检修夹扣可读；没有悬空碎像素。
- 灰阶：两者的玩法轮廓不依赖颜色；灯藤上密下疏仍成立。
- 应用接触：水面不抢单位和战术覆盖；灯藤重复成片不形成徽章墙纸，单位站入同格时脚底仍可读。
