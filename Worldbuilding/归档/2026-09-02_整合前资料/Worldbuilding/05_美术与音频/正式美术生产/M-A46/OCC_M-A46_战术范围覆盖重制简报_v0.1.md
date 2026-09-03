# OCC M-A46 战术范围覆盖重制简报 v0.1

## 应用环境与屏幕占位

- 运行时：剧情／肉鸽共用 `12×9` 正交战场，覆盖层绘制在每个 `32×32` 逻辑格上方，随战场严格最近邻显示为 `64／96／128／160px`。
- 玩家必须在一秒内同时读出“这是移动范围／这是攻击或施法范围”与下方地材、单位和物件；覆盖层不能伪装成地砖常驻边缘，也不能遮住单位脚底、血条或选中框。
- 只重制 `move_range` 与 `attack_range` 两项独立资产。生命、护盾与行动点使用既有正式 `FormalUISkin16` 资源条皮肤重排和动效，不新造脚本矩形美术。

## 美术方向

- **移动范围：** 四个冷青阶梯角扣与四条很短的内向测距刻线，中心和边中段留空；像学院现场测绘用的可执行以太定位标记，而不是霓虹方框。
- **攻击范围：** 四个封存红阶梯角扣，角内各有一枚向心威胁齿，边中段留空；比移动框更重、更尖，但不铺红底。
- **材质／叙事：** 近代学院以太测绘与危险封存标记；冷青只表示可执行移动，封存红只表示伤害／威胁。
- **形状层级：** 四个角块为第一读数，内向短刻线／威胁齿为第二读数；不增加第三层装饰。
- **像素合同：** 每项独立来源，原生 `32×32` 透明交付，硬 Alpha，最多 6 个可见色；无抗锯齿、渐变、柔光、投影、文字、数字、徽章、完整地板或随机单像素噪点。

## 生产提示词

### move_range_v2

`32×32 tactical movement-range overlay for OCC — four chunky stair-step corner brackets and four very short inward survey ticks, clearly open centre and open edge midpoints; exact orthographic overlay, academy field-measurement language, near-black support pixels plus restrained active-aether cyan and one pale cyan highlight; native low-resolution pixel grid, hard alpha, discrete square clusters, transparent background. Exclude: full continuous square border, floor tile, filled tint, neon glow, gradient, anti-aliasing, thin vector line, text, number, rune, logo, watermark, shadow, random isolated pixels.`

### attack_range_v2

`32×32 tactical attack-range overlay for OCC — four heavy stair-step corner brackets, each with one compact inward-pointing threat tooth, clearly open centre and open edge midpoints; exact orthographic overlay, academy danger-seal language, near-black support pixels plus oxidized seal red and one pale red highlight; native low-resolution pixel grid, hard alpha, discrete square clusters, transparent background. Exclude: full continuous square border, floor tile, filled tint, neon glow, gradient, anti-aliasing, thin vector line, text, number, rune, logo, watermark, shadow, random isolated pixels.`

## 验收

1. 1× 下仅凭角块／内向齿即可区分移动与攻击，不依赖文字。
2. 4× 下无半透明边、平滑线、孤立噪点或伪文字；灰阶仍能读出攻击框更重、更尖。
3. 棋盘格下透明中心完整；实际战场下不遮单位、地材、物件、血条与选择框。
4. Unity 使用 Texture/Sprite Point、Clamp、无 Mipmap、无压缩、PPU32；稳定 GUID、Resources 加载、动画 alpha/整数像素位移和双分辨率 Play Mode 接触通过后才可为 `FORMAL`。
