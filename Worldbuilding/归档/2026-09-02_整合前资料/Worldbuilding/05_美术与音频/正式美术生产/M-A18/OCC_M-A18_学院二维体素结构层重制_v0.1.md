# OCC M-A18 学院二维体素结构层重制 v0.1

## 1. 美术方向简报

- **角色与玩家阅读：** 1 秒内先看懂可走区域、硬阻挡、门洞、楼梯与路线转折；结构轮廓必须独立于地面颜色成立。
- **叙事／材质前提：** 本轮不以学院设定细节为首要目标。使用干净、耐看的浅暖石顶，深褐灰立面与近黑硬阴影；少量冷青只保留给真正的主动设施。
- **轮廓与形状语言：** 墙顶为宽而连续的浅色带，立面为明确的中深色带，投影为最深色；转角、端头、门洞和台阶必须只靠轮廓即可区分。单格压边应留出大部分可走地面，不读成重掩体。
- **色板角色：** 浅灰米色墙顶；灰褐立面；炭黑轮廓／投影；不使用高饱和装饰色。常规件控制在 5–8 色。
- **明度与光照：** 左上方统一硬光；顶面最高明度，立面至少降低两个明度级，投影为连续硬块。禁止柔光、渐变和环境噪点。
- **透视／构图：** 严格正交俯视像素战棋；逻辑北边朝画布上方。透明覆盖件必须与 32px 格边精确对齐，并允许 90° 离散旋转。
- **密度预算：** 每件最多三个主形：顶面、立面、投影；只允许一个受控材质线索。不要裂纹、苔藓、徽章、文字和散点装饰。

## 2. 首批资产与玩法语义

| 资产 | 交付画布 | 语义 | 旋转复用 |
| --- | ---: | --- | --- |
| `academy_curb_edge` | 32×32 | 单侧低地台压边，可走 | 4 向 |
| `academy_curb_corner` | 32×32 | 两个相邻方向压边，可走 | 4 向 |
| `academy_curb_opposite` | 32×32 | 两个相对方向压边，可走 | 2 向 |
| `academy_curb_three` | 32×32 | 三侧包边的窄路／端部，可走 | 4 向 |
| `academy_curb_enclosed` | 32×32 | 四侧包边的小岛，可走 | 无需旋转 |
| `academy_wall_straight` | 32×32 | 一格连续墙段，阻挡 | 2 向 |
| `academy_wall_corner` | 32×32 | 一格墙转角，阻挡 | 4 向 |
| `academy_wall_end` | 32×32 | 一格墙端头，阻挡 | 4 向 |
| `academy_gate_3x2` | 96×64 | 两侧墙墩阻挡、中央门洞可走 | 固定朝向，必要时另做竖向版 |
| `academy_stairs_2x1` | 64×32 | 两格宽楼梯，可走 | 2 向 |

压边是区域高度／材质交界的透明覆盖，不改变逻辑可走性；高墙只允许放在 `BlockedPositions`。门洞和台阶只覆盖真实可走格，禁止视觉碰撞谎言。

## 3. 生成提示词骨架

`native 32x32 pixel-game transparent terrain overlay for an orthographic tactical map — [exact adjacency silhouette and gameplay read]; strict top-down view, north is canvas top; broad light warm-stone cap, dark brown-gray vertical face, continuous near-black hard cast shadow; three connected value masses, 5 to 8 discrete flat colors, hard square pixel clusters, one-pixel stair-step contour, transparent safety area and exact edge connection points; no text, no symbols, no scenery, no units, no floor fill, no anti-aliasing, no gradients, no soft shadow, no random cracks, no isolated noise.`

多格门洞／楼梯按真实画布改写尺寸，并明确中央可走区和两侧阻挡占格。

## 4. 审核问题

1. 1× 尺寸下，不看颜色能否区分直边、转角、端头和包围关系？
2. 相同连接端逐像素相等，旋转后是否无缝且没有双线？
3. 顶面、立面、投影是否建立明确高度，又没有遮住单位脚底和范围覆盖？
4. 墙、门、楼梯的视觉占格是否与战斗阻挡／可走数据一致？
5. 九张地图是否由同一套件重组，而不是依赖任何关卡专属整图？
