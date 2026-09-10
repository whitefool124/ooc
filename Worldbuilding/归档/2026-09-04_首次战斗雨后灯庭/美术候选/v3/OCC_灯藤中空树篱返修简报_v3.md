# OCC 灯藤中空树篱返修简报 v3

日期：2026-09-04

## 任务合同

- 目标：将灯藤从“可钻过的藤屏”改为“一格内可站人的中空树篱”，准确表达单位与物块同格。
- 范围：生成一份新的独立原料，规范化为 `single_cell_prop_32`；水面 v2 不变，不导入 Unity，不修改玩法规则。
- 验收：中心必须保留足以承托单位脚底的透明／露地空腔；树篱四周环绕但不封死中心；32×32、硬 Alpha、最多 10 色、四边至少 2px 透明安全边；完成单位站入接触图。
- 解锁：用户审美批准后，再判断是否需要把前缘拆为单独遮挡层，并补 Unity Importer 与真实运行时复核。

## 美术方向

- **角色与一秒读法：** 单格可进入物块；必须先读成“人物站在树篱中央”，再读出遮视、减速、可燃。
- **应用环境：** 每格灯藤覆盖一张石板；单位占位与物块并存。运行时最低要求是地表 → 灯藤 → 单位的叠放仍能读出单位被植物四周包围。
- **轮廓：** 俯视 3/4 的不规则圆角方环；外轮廓约 27×24px，中心空腔约 10×10px。北侧／左右藤冠较高且密，南侧前缘较低、较薄，让中心空腔和脚底不被封住。
- **结构分区：** 三块主形体：后侧厚藤冠、左右包围臂、低矮前缘；一项材质故事是藏在叶团里的旧铜修枝箍；两枚小型暖黄灯荚是受控身份点。
- **材质：** 灰绿密叶、暗褐粗藤、少量旧铁／氧化铜夹箍；没有高大支架、门柱或建筑结构。
- **调色板：** 深炭轮廓、暗褐藤干、三档灰绿叶团、两档旧铜、两档暖黄灯荚，最多 10 色；无冷青、无危险红。
- **明度：** 外环藤叶为连续中暗质量块；中心空腔保持透明或显示底材；内缘使用窄暗边说明“凹进去”，灯荚面积明显小于叶团。
- **视角：** 战棋俯视 3/4，无消失点、无环境投影；不能画成正面篱笆墙。
- **密度：** 大叶团与粗藤为主，不画逐片叶脉、细枝毛边或随机像素噪点。

## 锁定不变量

1. 中心是真正空的，不是叶片上的暗色圆斑。
2. 单位脚底放在中心时，四周仍能看见植物环绕。
3. 后侧厚、前侧低；不是平面花环、圆徽章或甜甜圈图标。
4. 不出现入口门洞、拱门、花架、盆器、树桩或不可进入的实心灌木。
5. 灯荚是生长在藤上的小型维护照明，不是路灯、炸弹或宝箱提示。

## 生成提示词

```text
Use case: stylized-concept
Asset type: one independently generated OCC native-32x32 enterable battlefield prop, genuine transparent background
Primary request: a single HOLLOW LAMP-VINE HEDGE built to occupy one tactical cell while a character stands inside its center; the player must instantly read a person-sized empty cavity surrounded by dense sight-blocking foliage
Scene/backdrop: true transparent background only, no checkerboard, no floor, no shadow
Subject: one irregular rounded-square hedge ring seen from tactical top-down three-quarter view; a thick high north/back crown and two dense side arms curve around a clearly empty 10x10-pixel-equivalent central cavity; a low thin south/front lip completes the hedge without covering the cavity; coarse gray-green leaves grow from dark brown vines; two tiny asymmetrical warm-yellow lamp pods are attached to the back and one side with small aged-copper pruning clamps
Style/medium: deliberately coarse native-32-pixel game art, large connected square pixel clusters, hard one-pixel near-black stair-step outline, at most ten discrete flat colors, no anti-aliasing, no gradients
Composition/framing: outside silhouette about 27x24 pixels inside a 32x32 transparent cell; at least two transparent pixels on every canvas edge; the center hole is actual transparent negative space, not a dark painted spot; back foliage visibly taller than the low front rim
Lighting/mood: neutral diffuse rainy-courtyard light; lamp pods are two restrained warm accents with no bloom
Color palette: charcoal, dark brown, three muted gray-greens, aged copper, two warm yellows; no cyan, no red
Materials/textures: living combustible hedge maintained by hand-repaired academy garden clamps, not machinery
Constraints: one object only; center must fit a standing unit's feet and remain visibly hollow at 32x32; readable as a three-dimensional waist-to-shoulder-high hedge enclosure
Avoid: solid bush, hedge wall, screen, gate, doorway, archway, tall trellis, fence panel, flower pot, planter, wreath, emblem, badge, flat donut icon, perfect circle, symmetrical crest, stone basin, nest, crown, individual tiny leaves, painterly foliage, soft transparency, glow cloud, flame, text, watermark
```

## 目标尺寸复核

- 1×：不放单位时先读“中空树篱”，中心空腔不得合并成噪点。
- 1× 单位接触：单位脚底位于空腔内，植物仍从后、左、右和低前缘包围。
- 4×：中心为硬透明区域；内外轮廓均是连续阶梯簇；无抗锯齿与伪透明。
- 灰阶：空腔与前后高度关系不依赖绿色和黄色。
- 重复接触：E1–F5 成片时，每格仍有独立中空站位，不读成连续不可进入墙体。
