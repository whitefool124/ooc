# OCC 免费像素素材候选清单

日期：2026-09-17　性质：**外部素材调研记录，不是活动美术规范**

> 本文件只记录"外面有什么、许可能不能用、尺寸风格像不像"。
> 活动口径仍是 `Worldbuilding/策划案/OCC_项目总策划案_v1.0.md` 附录 A、`Worldbuilding/数据表/OCC_美术与界面规格表_v1.0.csv`
> 与机器镜像 `Tools/OCCArt/occ_art_contract_v1.json`。本文件不改变其中任何一条。

---

## 0. 先读这一节：外部素材能用到什么程度

OCC 现行机器合同写死了来源渠道：

```jsonc
// Tools/OCCArt/occ_art_contract_v1.json
"allowed_source_channels": ["codex_builtin_imagegen"]
```

`Tools/OCCArt/validate_occ_art_asset.py` 第 198–199 行按此字段硬拒：

```python
channel = provenance.get("source_channel")
if channel not in contract.get("allowed_source_channels", []):
    # 直接判失败
```

**结论：本文件里任何素材都无法通过 `validate_occ_art_asset.py`，因此不能成为 `FORMAL` 正式资产。** 这不是风格问题，是来源渠道门禁。

项目内已有的两条先例说明实际可行的用法边界：

| 先例 | 位置 | 允许的用法 |
| --- | --- | --- |
| Epic Toon FX v1.8 | `UnityProject/Assets/Game/Resources/Art/PrototypeToonParticles/source_manifest.json` | 标记 `PROTOTYPE_ONLY`，登记来源、归档 SHA256、文件清单；`"does not replace OCC formal VFX frames"` |
| Fusion Pixel Font | `UnityProject/Assets/Game/Resources/Fonts/Licenses/FusionPixelFont-OFL-1.1.txt` | 字体不在像素资产门禁内，随包附许可原文即可正式使用 |

所以外部素材当前有两条合法通道：

1. **参考/风格研究**——与 `ArtSource/sephiria_style_study_2026-09-16/` 同等地位，只作观察对象，不进 Unity。
2. **PROTOTYPE_ONLY 占位**——照 `source_manifest.json` 的格式登记，用于打通界面流程与交互，正式替换时仍走 `image_gen`。

要让它成为正式资产，必须先由用户裁决修改总策划案 A.2.2、规格表 `ART-PIXEL-PIPELINE` 与机器合同 `allowed_source_channels` 三处，再谈入库。

**另有一条反向约束**：CraftPix（条款 3.1.1）与 Unity Asset Store EULA 都**禁止把素材用于训练/微调/改进 AI 系统**。OCC 的美术生产本身是 `image_gen` 驱动的，所以这类素材**连当生图参考喂给生成器都不行**，只能人工目视参考。

---

## 1. 抓取可用性（本轮实测，决定哪些来源还能继续挖）

| 站点 | web_fetch | PowerShell | 说明 |
| --- | --- | --- | --- |
| opengameart.org | ✅ | ✅ | 首选；搜索 URL 可用 |
| kenney.nl | ✅ | ✅ | 全站 CC0，最好核 |
| craftpix.net | ✅ | ✅ | 许可页可读 |
| lospec.com | ✅ | ✅ | 只有调色板，无资产 |
| creativecommons.org / unity.com | ✅ | ✅ | 许可原文 |
| **itch.io（含子域）** | ❌ 全部 fetch failed | ✅ **200，可抓许可字段** | 本轮靠这条通道解锁 |
| github.com | ❌ | ✅ 200 | 字体仓库要用这条 |
| huggingface.co | ❌ | ✅ 200 | 批量数据集要用这条 |
| 0x72.dev | ❌ | ❌ | 主站确实不可达（itch 上的同物可达） |
| gamedevmarket.net | ❌ | — | Cloudflare 403 |
| summerengine.com / freepixel.art | ✅ 但 JS 空壳 | — | 见 §7 不建议来源 |

**itch.io 的可行抓法**（本轮已验证，供后续复用）：

```powershell
$h = Invoke-WebRequest -Uri $u -UseBasicParsing -Headers @{'User-Agent'='Mozilla/5.0 (Windows NT 10.0; Win64; x64)'}
$txt = ([regex]::Replace($h.Content,'<[^>]+>',' ') -replace '\s+',' ').Trim()
# itch 的许可字段锚点依次为：'Asset license' / 'LICENCE' / 'License for Everyone'
```

---

## 2. 许可已核实 · 地图瓦片 / 墙体 / 结构

按"最补 OCC 真实缺口"排序。**★ = 优先试做样板。**

### ★ lukems-br 系（CC0，32px 正交俯视，唯一做出"顶面 + 前立面"的免费墙体）
- **31 RPG/Roguelike 2d Topdown Dungeon Wall Tilesets (32px)** — https://opengameart.org/content/31-rpgroguelike-2d-topdown-dungeon-wall-tilesets-32px
  - 许可：CC0（页面同时给 CC-BY 3.0/4.0、OGA-BY 3.0，取 CC0 即可）
  - 规格：31 套 32px 墙块 PNG + Blender 源文件
  - 主题：作者明确用 Blender 给墙加了顶面；含 `iron0 / lab-metal0 / lab-stone0 / lab-rock0 / metal_wall / brick_brown / sandstone_wall / marble_wall / catacombs / church`
  - 匹配度：**高（结构）**——正对 OCC"方形顶面 + 朝屏幕下方前立面"契约，`iron / lab-*` 直接对应锻铁与实验室内墙
  - 风险：贴图源自 Dungeon Crawl 纹理，部分块面偏软、高频
- **2d Dungeon Wall (32px, DB32 palette): brick brown** — https://opengameart.org/content/2d-dungeon-wall-32px-db32-palette-brick-brown
  - 许可：CC0　规格：32px 墙块 + `.xcf` 源文件
  - 匹配度：**高**——DB32 限色后色数与块状感更贴 OCC 的"4–6 色 / 主色 ≥45%"；有源文件便于按 OCC 调色板重映射

### ★ DENZI's public domain art（CC0，32×32 真斜俯视材质库）
- https://opengameart.org/content/denzis-public-domain-art
- 许可：CC0　规格：32×32 为主（另有 32×48 怪物表），整张 tileset + 分类散图
- 主题：3/4 overhead orthogonal——砖/石/鹅卵石/大理石墙、木、铁、金属、地面、火把、道具、纸娃娃、图标
- 匹配度：**中高**——CC0 里体量最大且确实看得到顶面的 32×32 正交俯视材质库；材质故事（石材/旧木/锻铁）与 OCC 高度重合；硬边无抗锯齿
- 风险：单块细节略密、阴影偏重，需重上色

### 其他已核实瓦片/结构
| 素材包 | 许可 | 尺寸/规模 | 主题 | 匹配度 | 用法 |
| --- | --- | --- | --- | --- | --- |
| [Dungeon Crawl 32x32 tiles](https://opengameart.org/content/dungeon-crawl-32x32-tiles) | CC0（页面注明无需署名） | **3000+ 张** 32×32 | 地形/墙/装饰/怪物/法术/道具/GUI/头像 | 中（透视）/ 低（一致性） | **仅参考**——透视精确同轴，是顶面比例的标尺；但多画师拼盘、大量密集裂纹苔痕，命中淘汰项 |
| [Slates 32x32 orthogonal tileset](https://opengameart.org/content/slates-32x32px-orthogonal-tileset-by-ivan-voirol) | CC-BY 4.0 | 32×32 正交，PNG + 调色板 | 树/栅栏/阶梯/栈桥/立柱/招牌/建筑 | 中高 | 需署名 Ivan Voirol；161 收藏、7300+ 下载的成熟套 |
| [Underworld Load comprehensive top view RPG tileset](https://opengameart.org/content/underworld-load-comprehensive-top-view-rpg-tileset-32x32-some-16x24-16x16) | CC-BY 3.0 / OGA-BY 3.0 | 32×32 + **16×24 / 16×16 子模块** + Tiled .tmx | 无缝墙/地面/楼梯/门/陷阱/机关/箱柜 | 中高 | 需署名 poikilos；**尺寸契约命中率最高**（含 16×24"格+上探"档） |
| [Stone Terrain](https://opengameart.org/content/stone-terrain) | CC-BY 3.0 / OGA-BY 3.0；`stone_ground` 系列为 **CC0** | 32×32 indexed PNG + GIMP 源 | 石墙/石地/楼梯，为 Stendhal 制作 | 中 | 取 CC0 的 `stone_ground` 部分 |
| [DawnLike 16×16 Universal Rogue-like tileset](https://opengameart.org/content/dawnlike-16x16-universal-rogue-like-tileset-v181) | CC-BY 4.0 | 16×16，1057+ 瓦片，2 帧动画 | 地牢/矿/城镇/冥界 + 怪物/武器/道具 | 中 | 需署名 DawnBringer；DB16 限色、体量极大 |
| [Sci-Fi RogueLike Pixel Art](https://opengameart.org/content/sci-fi-roguelike-pixel-art) | CC0 | 16×16 | 地板墙体/机器人/力场装置，作者注明"100/100 斜俯视" | 中高 | 装置类小道具与 16×16 子网格补充 |
| [Colony Sim Extended Version](https://opengameart.org/content/colony-sim-extended-version) | CC0 | 16×16，DB32 | 地面/可延展建筑/墙/城门/塔/桥 | 中高 | 建筑外壳与地面；白蓝配色需整体重映射 |
| ★ [Dirt and Grass - Forager Inspired](https://opengameart.org/content/dirt-and-grass-forager-inspired) | CC0（页面"No attribution necessary"） | 16×16，土/草各 47 格 autotile + 20 墙块 | 平色泥地/草地 | **高** | 正面命中"地面近乎平色、靠色相区分"；文件仅 561 B → 色数极少、无高频抖色 |
| [Tiny Zelder Clone Topdown Pack](https://opengameart.org/content/tiny-zelder-clone-topdown-pack) | CC0 | 16×16 四向正交，含 **cliff 悬崖** | 草地/高差/角色/洞口 | 中 | 唯一的 16×16 悬崖模块；图形极简，需重绘细节 |
| [Chain Railing Sprite Strip](https://opengameart.org/content/chain-railing-sprite-strip) | CC0 | 单张 PNG 条带 | 悬崖/木桥链式护栏 | 中 | 补"护栏/掩体" |
| [Tiny 16: Basic](https://opengameart.org/content/tiny-16-basic) | CC-BY 4.0 / 3.0 / OGA-BY 3.0 | 16×16，DawnBringer 16 色 | 草地/沙/水/砖地/木地板/红毯/墙/宝箱/招牌/桥 | 中高 | 需署名 Lanea Zimmerman；像素语言很贴，但内容是中世纪 JRPG 村落 |

---

## 2b. ★ 学院 / 工坊 / 实验室 / 机械内饰（本轮 OGA 系统扫描新增，主题最对口）

这一组是"近代魔法工业学院"题材命中率最高的，全部来自 OGA 系统扫描。

| 素材包 | 许可 | 尺寸 | 内容 | 匹配度 |
| --- | --- | --- | --- | --- |
| ★★ [Laboratory tileset PixelArt 16px](https://opengameart.org/content/laboratory-tileset-pixelart-16px) | CC-BY 4.0 | 原生 16px，**ZIP 内同时给 32px 与 48px 版** | 地板/墙/门/液体/楼梯/**电梯**/屏幕/数字 0–9/**能量球与管道特效**/培养管中人体动画 + 3 个四向行走 NPC | **高**——全站最接近"实验室/学院"的成套正交瓦片，且自带 32px 版可直接接 OCC 32 PPU |
| ★★ [Observatory Tileset](https://opengameart.org/content/observatory-tileset) | CC-BY 4.0 | 32×32 | 石造建筑墙与屋顶、**铜制穹顶**、**黄铜望远镜** | **高**——石材 + 铜 + 黄铜机械，正是学院/以太观测的场景件 |
| ★★ [Copper pipe tiles](https://opengameart.org/content/copper-pipe-tiles) | CC-BY 4.0 | 32×32 | 铜/金属管道：直管、转角、三通、管口（3 张 PNG，含 RPG Maker XP autotile 版） | **高（主题）**——工业以太管线的关键件；作者说明不含配套环境，需配其他地面墙 |
| ★ [Old Wooden Props 32x32](https://opengameart.org/content/old-wooden-props-32x32) | CC-BY 4.0 | 32×32 | **旧木道具/家具**：箱子、柜台、木桶，可循环拼成模块化长条；含 dirty / clean 两版 | **高**——旧木是加分主题，模块化正好铺工坊/仓库/教室 |
| [Minimal Industrial Tiles](https://opengameart.org/content/minimal-industrial-tiles) | **CC0** | — | 工业/机械瓦片 | 高（主题） |
| [Bulkhead Walls Hangar](https://opengameart.org/content/bulkhead-walls-hangar) | **CC0** | — | 铁/舱壁墙体，工业金属 | 中高 |
| [Hospital TopDown perspective](https://opengameart.org/content/hospital-topdown-perspective-2d-pixel-art) | CC-BY 4.0 | — | 俯视房间：**实验室、机房**、厨房、食堂、卫浴、卧室、走廊、门、**电梯**、墙体、机器 + 主角动画 | 中高（主题极高，风格偏现代医院需重着色） |
| [Tileable combination locks](https://opengameart.org/content/tileable-combination-locks) | CC-BY 4.0 | 32×32 | 密码锁/机械 | 中高 |
| [Pixel Art - Mechanic's Tool Set](https://opengameart.org/content/pixel-art-mechanics-tool-set) | CC-BY 4.0 | 16×16 | 8 方向工具 | 中 |
| [[LPC] Woodshop](https://opengameart.org/content/lpc-woodshop) | CC-BY 4.0/3.0 + OGA-BY 3.0（可选宽松项） | 32×32 | 木工台/工具 | 中高 |
| [[LPC] Ore and Forge](https://opengameart.org/content/lpc-ore-and-forge) | CC-BY 4.0/3.0 + OGA-BY 3.0 | LPC | 矿石/熔炉动画 | 中高——锻造/工坊 |
| [[LPC] Siege Weapons](https://opengameart.org/content/lpc-siege-weapons) | CC-BY 4.0/3.0 + OGA-BY 3.0 | LPC | 弩炮/投石/火炮 | 中——以太装置参考 |
| [Evil Dungeon Asset Pack](https://opengameart.org/content/evil-dungeon-asset-pack) | CC-BY 4.0 | 32×32（RPG Maker XP autotile 版式） | 墙/地面/门/装饰/排水口/喷泉/陷阱/地面按钮/**四向巨鼠**/**四向牛头人守卫** | 中高——暗色正交地牢，**一次补齐瓦片与敌人** |
| [Mage City Arcanos](https://opengameart.org/content/mage-city-arcanos) | **CC0** | 32×32 | 城镇套件：墙、屋顶、铺装、建筑、道具 | 中高——"魔法城市"主题契合学院城 |
| [Oh my dungeon!](https://opengameart.org/content/oh-my-dungeon) | CC-BY 4.0 | 16×16，**DB32** | 自洽地牢套装：瓦片/门/宝箱/角色/怪物/物品/金币 | 中高——有限调色板硬边像素，不依赖外部素材 |
| [Zelda-like tilesets and sprites](https://opengameart.org/content/zelda-like-tilesets-and-sprites) | **CC0** | 16×16 | 地表/洞窟/室内/UI | 中 |
| [tinySLATES 16×16](https://opengameart.org/content/tinyslates-16x16px-orthogonal-tileset-by-ivan-voirol) | CC-BY 4.0 | 16×16 + Tiled tsx | Slates 的 16px 版，层数更少、调色更亮 | 中高（若战场降到 16 格基线） |

> 另有 **GUI dialogue box 系列**（`/content/gui-dialogue-box`、`/content/sword-dialog-box`、`/content/dialog-box`、`/content/gui-information-kiosk`）为 CC-BY 4.0——正对总案第 9 节欠的"**局内对话字幕与对话框表现**"。

---

## 2c. ★★ 管道 / 阀门 / 机械装置（对应"可维护以太装置"这一核心缺口）

这是全项目最难找的主题，本轮找到了可用的成套解。

| 素材包 | 许可 | 尺寸 | 内容 | 匹配度 |
| --- | --- | --- | --- | --- |
| ★★★ [Pipes and Tanks](https://opengameart.org/content/pipes-and-tanks) | **CC0 1.0**（含 **Aseprite 源文件**） | **32×32**，34 KB | **铜 / 铂 / 钢三种材质**管道地块 + 储罐装饰件；标签明确 **Top-down + orthographic** | **最高**——三材质可重着色，是"可维护以太管道"首选底材；CC0 + 源文件 = 可自由改造成标准件 |
| ★★ [Copper pipe tiles](https://opengameart.org/content/copper-pipe-tiles) | CC-BY 4.0 | **32×32** | 顶视铜管地块 + autotile 版 + 单独端点表 | **最高**——端点齐全，可直接铺满整张"可维护管道网"，首战场景的骨架 |
| ★ [6 Colored Pipes](https://opengameart.org/content/6-colored-pipes) | CC-BY 4.0 | **32×32 与 16×16 双网格** | 6 色管道/导管；调色板 Endesga 64 | **最高**——按颜色区分管线，天然适合"不同功能以太管路"的颜色编码 |
| [pipes](https://opengameart.org/content/pipes-0) | **CC0** | 32×32 | 铜/黑/蓝三色分表 | 高 |
| ★ [2D Pipe parts](https://opengameart.org/content/2d-pipe-parts) | **CC0** | — | 4 种管件（直管/弯头/三通/十字）× **6 级损坏状态** × **通电/断电两态** | **高（玩法层面）**——"6 级损坏 + 通断"与"可维护、有代价的能量工程"契合度极高；⚠ 交付为 Blender `.blend` 源，需自行渲染成像素 |
| [factory tileset](https://opengameart.org/content/factory-tileset) | **CC0** | — | 工厂地块：**传送带、管道、电脑/屏幕**；作者注明可与 Kenney roguelike 套件混用 | 高 |
| [Core Reactor Machines](https://opengameart.org/content/core-reactor-machines) | **CC0** | — | 机器/反应堆/发电机 + **可叠加装甲护罩组件** + 2 组动画帧表 | 高——"可叠加护罩 + 动画"直接对应"可维护、有代价"的装置状态表现 |
| [Mysterious Contraption](https://opengameart.org/content/mysterious-contraption) | **CC0** | — | 带曲柄、按钮、闪灯的机械装置（动画 GIF + 逐帧 PNG） | 高——带可动部件与指示灯，可做场景焦点 |
| [Rough Industrial Combat Props](https://opengameart.org/content/rough-industrial-combat-props) | **CC0** | — | 金属/玻璃桶、燃料危险物，**含玻璃碎裂动画帧** | 高——可直接当战术场景互动物与掩体 |
| ★ [Cogwheels and Gears](https://opengameart.org/content/cogwheels-and-gears) | **CC0** | **14×14 到 32×32** | 像素齿轮组，材质为**钢（灰）与黄铜**，可组合 | **最高**——尺寸落在目标区间 + 明确含黄铜 + CC0 |
| [16x16 Pipe Tileset](https://opengameart.org/content/16x16-pipe-tileset) | **CC0** | 16×16 | 金属管道 | 中高 |
| [construction box to assemble pipes grids](https://opengameart.org/content/construction-box-to-assemble-pipes-grids-or-to-decorate-backgrounds) | **CC0** | — | 乐高式可组合管道构件库，可拼"极其纠缠的回路" | 中高 |
| [Industrial tiles](https://opengameart.org/content/industrial-tiles) | **CC0** | 16×16 | 标签明确含 **top down** | 中高 |
| [Industrial TilePack](https://opengameart.org/content/industrial-tilepack) | **CC0** | **64×64** 无 margin 无 spacing | 混凝土地面 + 工业箱体 | 中高（尺寸命中 64 档） |
| ⚠ [Steampunk Inspired Tiles (32x32)](https://opengameart.org/content/steampunk-inspired-tiles-32x32) | CC0 | 32×32 | **更正：实为侧视（sidescroll），不是俯视** → 排除 |
| [Steampunk cyber dungeon tiles](https://opengameart.org/content/steampunk-cyber-dungeon-tiles) | **CC0** | — | "steampunk pipes, cogs, servers and some basic walls and floors" | 中高 |
| [Metal tileset](https://opengameart.org/content/metal-tileset-0) | CC-BY 4.0 | — | 126 块金属地块 | 中 |
| [Steampunk Level Tileset Mega Pack 16x16](https://opengameart.org/content/steampunk-level-tileset-mega-pack-level-tileset-16x16) | CC-BY 3.0 | 16×16 连接式 | 灰调砖墙 + 管道 + 梁柱 | 中高 |
| [Steampunk Brick NEW / OLD 16x16](https://opengameart.org/content/steampunk-brick-new-connecting-tileset-16x16) | CC-BY 3.0 | 16×16 连接式 | 铜色调蒸汽朋克砖墙；OLD 版可做旧墙/湿墙变化 | 中 |
| [Industrial Zone Tileset](https://opengameart.org/content/industrial-zone-tileset) | OGA-BY 3.0 | — | 工业区地块；⚠ 标签含 cyberpunk，需筛掉霓虹部分 | 中 |
| [2D Metal Fence](https://opengameart.org/content/2d-metal-fence) | OGA-BY 3.0 | 可平铺 | **wrought iron 铁艺栏杆**——极小单件 | 中（锻铁语汇） |
| [2D Pixel Robot](https://opengameart.org/content/2d-pixel-robot) | **CC0** | 拼合画布 36×93（需重排） | 模块化**可拆装**机器人，含"大脑"与"内脏"图层 + idle/step/scan/duck/jet 动画 | 中高 |
| [Stylish Top-Hat Robot 16x16](https://opengameart.org/content/stylish-top-hat-robot-16x16-animated-spritesheet) | CC-BY 4.0/3.0 | 16×16 | 机械单位（作者用于其 steampunk android 游戏） | 中 |
| [Communication terminal, 32x32](https://opengameart.org/content/communication-terminal-32x32) | **CC0** | **32×32** | 机械终端/控制台道具 | 高——以太终端直接用 |
| [Indoor office appliances](https://opengameart.org/content/indoor-office-appliances) | **CC0** | **32×32** | 室内办公/工厂装饰，单张 tilemap | 高——尺寸与题材双命中 |
| ⚠ [transparent pipe tileset](https://opengameart.org/content/transparent-pipe-tileset) | **CC-BY-SA 3.0** | — | 透明管道可表现以太流体 | 传染性，排除 |
| ⚠ [Golden Pipes](https://opengameart.org/content/golden-pipes) | **CC-BY-SA 3.0** | — | 黄铜/金管线观感 | 传染性，排除 |
| ⚠ [Violet Industrial 16x16](https://opengameart.org/content/violet-industrial-16x16-tileset) | **CC-BY-SA 4.0** | 16×16 | 5 色 + 透明，含 pipes 分表 | 传染性，排除 |
| ⚠ [[LPC] Alchemy](https://opengameart.org/content/lpc-alchemy) | **CC-BY-SA 3.0/4.0** | 16×16 | 烧瓶、冷凝器、熔炉、本生灯 + **玻璃与铜管** + 大量齿轮（最贴"可维护以太装置"的一条） | 传染性，排除 |

### 规模最大的 32×32 补充
- [Top Down Dungeon Pack](https://opengameart.org/content/top-down-dungeon-pack) — **CC0**，**全部 64×64，2,256 个 tile**，含 Tiled `.tsx` autotile；墙 1,316（Brick/Stone/**Metal**/Concrete…28 变体）+ 地板 940（Stone/**Metal**/**Wood**…14 变体）→ 明确含 Metal 与 Wood 地板
- [32x32 floor tiles](https://opengameart.org/content/32x32-floor-tiles) — **CC0**
- [Floor Tiles](https://opengameart.org/content/floor-tiles)（AntumDeluge）— CC-BY 4.0/3.0 + **OGA-BY 3.0** 三选；页面明写 `orientation: orthogonal / tile dimensions: 32x32`；**含 `.xcf` 源文件**
- [Stone floor tiles](https://opengameart.org/content/stone-floor-tiles) — **CC0**，32×32 破败石板

---

## 2d. 主题标签扫描新增（⚠ 本批**未做许可筛选**，入库前必须逐条核对）

23 个标签页 + 9 组关键词。各标签全站总量：`top-down` 354 | `magic` 274 | `building` 222 | `brick` 91 | `orthogonal` 86 | `furniture` 67 | `town` 57 | `book` 57 | `gear` 29 | `factory` 29 | `machine` 28 | `engine` 23 | `iron` 22 | `laboratory` 16 | `indoor` 12 | `school` 12 | `steam` 8 | `machinery` 6 | `library` 6 | `workshop` 2。

**重要结构发现**：OGA **没有"学院/讲台"专用标签**（`university`、`lectern` 均 0 结果），学院题材只能靠 `school` / `classroom` / `lab` / `library` / `book` / `furniture` / `building` 几条路径拼装。

### 🎯 学院题材命中（本批最大收获）
- [Cool School tileset](https://opengameart.org/content/cool-school-tileset) — 学校室内瓦片集
- ★ [low-pixel school tileset and objects etc. (16x16px)](https://opengameart.org/content/low-pixel-school-tileset-and-objects-etc-16x16px) — **16×16 学院瓦片 + 物件，尺寸与题材双命中**
- [Sprite City Series 1 "School"](https://opengameart.org/content/sprite-city-series-1-school)
- [Pixel City - Municipal buildings](https://opengameart.org/content/pixel-city-municipal-buildings) — 市政/公共建筑
- [Classroom 002](https://opengameart.org/content/classroom-002)（已核 CC0）

### 🎯 工坊 / 机械 / 工业（续）
[LPC Revised] Workshop Tilesets · [LPC] Blacksmith · [LPC] Ore and Forge · Steam powered engine · Motor · Turret Gun, Bogie, Rail · [Mega Zero Style - Foundry Tileset](https://opengameart.org/content/mega-zero-style-foundry-tileset) · [Symphonium, Sci-Fi Industrial Plant](https://opengameart.org/content/symphonium-sci-fi-industrial-plant) · [Metal Blocks and misc elements](https://opengameart.org/content/metal-blocks-and-misc-elements) · girder (platform) · Iron grate over well · cauldrons · Background Wire Tile Kit · Industrial Traps 2D · [Industrial Mine Tileset](https://opengameart.org/content/industrial-mine-tileset)
传送带（4 个版本）：Conveyor Belts SpriteSheet (Anims) · Conveyor Belt Spriteset · 16x Animated Conveyor Belt (16×16) · Isometric conveyor belt animation
资源图标：Various stones and ore/gem veins 16x16 · Resource icons · Resource Pack 1

### 🎯 实验室（续，与已核对的 Laboratory PixelArt 16px 不同）
[Laboratory tileset](https://opengameart.org/content/laboratory-tileset) · Pixel Art Lab/Office Tiles · Lab Decors Tileset · Pixel Art Laboratory Props · 2D Laboratory pack · Sci-Fi Facility Asset Pack · Morgue (science) · test tube chemical pixel art · Still, Chemical Device · Ceramic Melting Pot

### 🎯 大包（体量优先）
- ★ [Wyrmsun CC0 - over 900 items](https://opengameart.org/content/wyrmsun-cc0-over-900-items) — **CC0，900+ 件**
- ⚠ **更正**：`denzis-32x32-orthogonal-tilesets`（32×32，含地牢/怪物/物品/技能图标/武器/头像/护甲）与 `denzis-16x16-oblique-tilesets` **是 CC-BY-SA 3.0 only（传染性，无宽松选项）** —— 此前误记为"DENZI 家族 CC0"。**必须区分**：`denzis-public-domain-art`（见 §2）确实是 **CC0 ✓**，但那个 32×32 正交大集实际**不可宽松商用**。
- [Public Domain Pack](https://opengameart.org/content/public-domain-pack) · [NES CC0 Graphics #2](https://opengameart.org/content/nes-cc0-graphics-2) · [MetallicOrange 16px orthogonal tileset](https://opengameart.org/content/metallicorange-16px-orthogonal-tileset)
- ✅ [Basic 32x32 sci-fi tiles for roguelike](https://opengameart.org/content/basic-32x32-sci-fi-tiles-for-roguelike) — CC-BY 3.0 + OGA-BY 3.0，**32×32 正交俯视**；基础地板 + 墙，金属/锈色，扁平有限原型调色板
- ✅ [Top Down 32x32 2D tileset](https://opengameart.org/content/top-down-32x32-2d-tileset) — CC-BY 3.0，**32×32 正交俯视**；草/土/水基础地形，含窄角与宽角过渡

### ⭐ 图标 / UI（魔法 + 机械主题）
- ⭐ [Magic and mechanic in-game interface icons](https://opengameart.org/content/magic-and-mechanic-in-game-interface-icons) — **魔法 + 机械 UI 图标，主题极对口**
- [250+ HUD and interface icons (Unknown Horizons)](https://opengameart.org/content/250-hud-and-interface-icons-unknown-horizons) · [Resource and building icons (Unknown Horizons)](https://opengameart.org/content/resource-and-building-icons-unknown-horizons)（32×32）· Attack Icons Wesnoth · 98 Pixel Art RPG Icons
- 药水/宝箱/书/文件：RPG potions 16x16 · Shiny RPG potions 16x16 · Shiny treasure icons 16x16 · **CC0 Book Icons** · CC0 Document Icons · RPG Book Icons Pack · 32px RPG Clutter/Icons（+64px 书堆）
- UI 皮肤：**Parchment GUI** · **FANTASY-parchment-set** · Magic Buttons · GUI Buttons · Inventory UI · Arcade GUI · Upgrade Screen
- 法术/特效：**Runes** · **4 summoning circles** · Extended LPC Magic pack · 16x18 Magic Cast Animations Sheet 1 + Template · Spell icon collection part 2
- 装备：Loyalty Lies Equipment 系列（匕首/弓/头盔/盾/法器/护甲/护手/项链）· Drawn RPG Inventory Icons (Ardentryst)

### 室内 / 家具 / 书架（图书馆主题）
书架类：★ [LPC] Shelves Rework · Antique Bookshelf · Bookshelf · Wooden cupboard shelf pcdesk · Magic Books · Pixel Books · Flying book sprite sheet · Pixel Art Documents · Tables and Misc Props (16x16)
室内：Interior Tileset 16x16 · Indoor Tileset 1 · Office Space Tileset · [LPC Revised] The Office · Rustic Indoor Remake · TDS Tilesets: buildings and furniture · Pixel Furniture · Table and Chair set · Beds · Cupboard · Chair · Kitchen countertop · Microwave/Fridge/Oven · Lamps Lights n Torches · [LPC] Simple Modern Furniture · Futuristic-ish furniture set · **warm eclectic / cottage core bedroom furniture set**（暖色调，加分）· [LPC] Wooden Furniture · [LPC] House interior and decorations

### 砖石铁 / 城镇
砖石：Tileset brick wall 16x16 · Brick Walls · Pixel Art Brick Tiles · 32 x 32 Bricks · Castle Brick [Connecting 16×16] · Sandstone Brick [Connecting 16×16] · 16x16 pixel art dungeon wall and cobblestone floor tiles · Classic Dungeon Walls · BMR's 20×20 Wall Tiles v3.0.5 · **Wall+ Cracked Asphalt** · Old stone buildings · Ancient ruins pixel art
城镇：Exterior 32x32 Town tileset · Town tiles · RPG Town Tileset · JS SaGa Style tileset 2 "Ascent" · Bountiful Bits 10x10 Top-Down RPG Tiles · Pico-8 City · Sketch Town (+Expansion) · Medieval town · House Sets · Village Buildings · **Watermill**（动画水车）

### 未取完的页（如需继续扩量）
`top-down`（354 项 / 仅取 48）· `magic`（274 / 48）· `building`（222 / 48）· `brick`（91 / 48）· `orthogonal`（86 / 48）· `furniture`（67 / 48）· `town`（57 / 48）· `book`（57 / 48）· keyword `library`（112 / 24）

---

## 2e. ★★ 收尾新增（含两处对前面结论的更正）

### 更正 1：实验室内景**不是空集**（修正 §10 旧判断）
存在 4 项 32×32 实验室内景，其中 **2 项是 CC0**：

| 素材包 | 许可 | 尺寸 | 内容 |
| --- | --- | --- | --- |
| ★★★ [Warped Top-Down Tech Lab 2](https://opengameart.org/content/warped-top-down-tech-lab-2) | **CC0**（署名 Ansimuz 可选） | **32×32**（页面原文 "Size of the Tiles are 32x32 pixels."） | 俯视像素 tile set，风格仿 Phantasy Star，专为搭 SciFi 实验室俯视布局而做 |
| ★★ [Warped Top-Down Tech Lab Extension](https://opengameart.org/content/warped-top-down-tech-lab-extension) | **CC0** | — | **动画门、竖门（动画）、板条箱与桶、信标（动画）、动画显示屏、墙灯、反向/双面墙砖、孵化器（动画）**、血/甲条 |
| [Warped Top-Down Tech Lab](https://opengameart.org/content/warped-top-down-tech-lab) | **CC0** | 页内未标 | 同系列 |
| [Pixel Art Lab/Office Tiles](https://opengameart.org/content/pixel-art-laboffice-tiles) | CC-BY 3.0 | **32×32** | 作者称 "Tiles should be able to work for a top-down style game" |
| [isaiah658's Pixel Pack #2](https://opengameart.org/content/isaiah658s-pixel-pack-2) | **CC0** | 16×16 图块 + 怪物 64×64 | 含 **科学实验室 / 洁净工业风瓷砖**、便利店/售货机/街机、家具/地毯/画、UI；部分 4 帧动画 |

**结论改为**：实验室内景**有来源**，但全是"现代/科幻实验室"调性，需重调色去科幻感；**且仍无"以太/魔法"语义**。

### 更正 2：仪表盘**并非完全缺失**（修正 §10 旧判断）
- ★★ [Lab rack equipment](https://opengameart.org/content/lab-rack-equipment) — **CC0**（放弃权利、无需署名）— 实验室机架设备像素图，可用作 radio / **oscilloscope** / **UV meter** / **power supply** / I/O adapter；**每件零件独立文件**（`lre_d1.png`–`lre_d9.png`）+ 合成图 → **"仪表/电源/接口"零件层，直接补齐仪表盘缺口**
- **Warped Tech Lab Extension（CC0）** 另提供动画显示屏与墙灯
- **但阀门仍确认缺失**（OGA `valve` 全站 3 条且全部无关）——此结论不变

### ★ 其他高价值新增

| 素材包 | 许可 | 尺寸 | 内容 |
| --- | --- | --- | --- |
| ★★ [[LPC Revised] Workshop Tilesets](https://opengameart.org/content/lpc-revised-workshop-tilesets) | **OGA-BY 3.0 + OGA-BY 4.0（非传染）**；上游更宽：Blacksmith = OGA-BY 3.0 / CC-BY 3.0+ / GPL 2.0+ | Tiled 可直用 tileset | bluecarrot16 编译的三套：**Blacksmith 铁匠铺（圆炉/方炉、炉内部分离成独立瓦片、梯子）、Woodshop 木工坊、Tailor 裁缝铺**；每套自带 Tiled tileset 与动画。**关键词 `workshop tileset` 全站仅此 1 条** |
| ★ [Background Wire Tile Kit](https://opengameart.org/content/background-wire-tile-kit) | **CC0** | **16×16** | 像素**双线束瓦片**，可贴墙/贴地作附加层；透明背景 + 整张 sheet + **去饱和/压暗版** → **以太走线/管路** |
| ★ [Industrial Mine Tileset](https://opengameart.org/content/industrial-mine-tileset) | **CC0**（署名 Spring） | **32×32** | 灰调脏污金属工业瓦片（含熔岩/矿井元素需挑选） |
| ★ [Cool School tileset](https://opengameart.org/content/cool-school-tileset) | **CC0** | **48×48** | 黑板/课桌/椅/柜/书/电脑/电视/窗 + autotile |
| ★ [Complete 32x32 RPG Combat Pack](https://opengameart.org/content/complete-32x32-rpg-combat-pack) | **OGA-BY 4.0（非传染）** | **32×32** | 玩家角色（Idle/Walk/Sword Attack/Damaged/Death）、地精敌人、道具（动画壁炬亮/灭、带破坏动画木箱）、VFX、**UI（血条/面板/剑/金币/药水图标）**、**Dirt 地面 tileset、Stone 墙面 tileset** —— **角色/道具/VFX/UI/地面/墙面全齐的完整战斗原型包** |
| ★ [12 Blade Fuses](https://opengameart.org/content/12-blade-fuses) | **CC0** | **10×10** | 12 色插片式保险丝按现实绘制 —— **极贴"魔法=可测量、可维护、有代价的能量工程"**；尺寸低于下限需重制到 16/32 |
| ★ [Vintage Office Interiors](https://opengameart.org/content/vintage-office-interiors) | **OGA-BY 3.0（非传染）** | 归入 Sidescroll | 长椅/衣帽架/橱柜/抽屉/**文件柜**/桌/**灯开关（开/关）**/纸堆/**大型保险柜**/**木板墙地（可平铺）**/**灯罩**/电话/马克杯（**含独立蒸汽 sprite**）；家具另有 clean/noisy 两版 → **氛围最接近"近代魔法工业日常"的一套**，但为横版侧视排列，需改造 |
| [Various Containers](https://opengameart.org/content/various-containers) | **CC0**（Attribution Notice: "None."） | **16×16**，调色板 **Arne16** | 约 11 物件：**坩埚**、陶罐、**木桶 + 5 种箱子** |
| [Tables & Stools](https://opengameart.org/content/tables-stools) | CC-BY 4.0/3.0 + **OGA-BY 3.0** | **32×32**（含 PNG 索引色 + `.xcf` 源） | 正交俯视桌椅 |
| [[LPC] House interior and decorations](https://opengameart.org/content/lpc-house-interior-and-decorations) | CC-BY-SA 3.0 / GPL 3.0 / 2.0 | **32×64 橱柜、32×96 床** | 上下分体橱柜、**厨房设备（金属炉/灶含火焰动画）**、**书架**、瓶罐、书籍、蛛网、**台灯** |
| [[LPC Revised] Base Object Kit!](https://opengameart.org/content/lpc-revised-base-object-kit) | CC-BY-SA 3.0 | **32×32** | 含 **lighting（灯光）与 clutter（杂物）**分类、四方向转面，统一 **128 色原创调色板** |
| [Colored Metal tiles 1b](https://opengameart.org/content/colored-metal-tiles-1b) | **CC0** | — | **6 种金属地板瓦片 × 8 套 NES 调色板**，原始 + 4X 两版 |
| [Metal Blocks and misc elements](https://opengameart.org/content/metal-blocks-and-misc-elements) | **CC0** | — | Blocks、fan、**lamps**、grille、gem + **多种损毁变体** |
| [Factory01](https://opengameart.org/content/factory01) | **CC0** | 10 个 PNG | 环境、**地面熔炉、大红/大黄工业灯**、两种墙、小灯、**两台电机** |
| [modern city extension](https://opengameart.org/content/modern-city-extension) | **CC0** | 图集 896×736 | 为 Kenney roguelike modern city pack 做的扩展（**须配合原包**）：更多建筑（**含多种工业/工厂建筑**）、暗变体、亮灯窗、工厂路牌 |
| [Bookshelf](https://opengameart.org/content/bookshelf-3) | **CC0** | 实测 **64×64** RGBA | 标签含 office/**school** |
| [Magic Books](https://opengameart.org/content/magic-books) | **CC0** | 实测图集 400×400 | 魔法书道具 |
| [Framed Artwork](https://opengameart.org/content/framed-artwork) | **CC0** | **明确给 16×16 / 24×24 / 32×32 / 48×48 + Raw** | 室内墙面挂画 |
| [Zelda-like tilesets and sprites](https://opengameart.org/content/zelda-like-tilesets-and-sprites) | **CC0** | 16×16 | Overworld / Cave / **Indoors** tileset + 角色 + UI，CC0 无署名负担的成套俯视室内外 |
| [two dungeon wall tilesets](https://opengameart.org/content/two-dungeon-wall-tilesets) · [16x16 Tiles](https://opengameart.org/content/16x16-tiles) · [Grey Roguelike Tileset](https://opengameart.org/content/grey-roguelike-tileset) · [Battle for Wesnoth - Metal factory floor](https://opengameart.org/content/battle-for-wesnoth-metal-factory-floor) · [Gear 32x32](https://opengameart.org/content/gear-pixel-art-32x32) | **CC0** | 16×16 / 32×32 | 砖墙多变体、含管道与木地板、无缝金属工厂地面、齿轮 |
| [Idylwild's Alchemical Codex](https://opengameart.org/content/idylwilds-alchemical-codex) | **CC0** | 16×16 | **75 符号 × 3 风格 = 225 图标，明确含 Aether 替代符** —— 符文/以太主题最强单项 |
| [[LPC Revised] The Office](https://opengameart.org/content/lpc-revised-the-office) · [Office Space Tileset](https://opengameart.org/content/office-space-tileset) · [Rustic Indoor Remake](https://opengameart.org/content/rustic-indoor-remake) | 待核 | — | 室内补充 |
| [Stone Home Interior Tileset](https://opengameart.org/content/stone-home-interior-tileset) | CC-BY-SA 3.0 | **32×32** | 石墙（支持非矩形结构）、石地板、**木地板**、地毯、柱子、楼梯、木半墙、栏杆 |

### 关键词层面的零结果（已确证，避免重复投入）
- **`academy`**：4 条全部无关 → **OGA 没有 academy 主题素材**
- **`workshop tileset`**：全站**仅 1 条**（= [LPC Revised] Workshop Tilesets），**等价于无冗余供给**
- **`classroom`**：全站**仅 1 条**（Classroom 002）
- **`plank`**：8 条，除 2 项地板外全是单图标 → 无可用套件
- **`crate barrel`**：7 条，仅 Various Containers 与 [LPC] Containers → **无专门的正交箱桶套件**
- **`bookshelf`**：13 条**全部是单个精灵**，无完整套件
- **`machinery`**：8 条，除 16x Animated Conveyor Belt 与 Misc. Sci-Fi Tiles（等距、128px）外无像素地块/道具
- **OGA CJK 检索无效**（见 §6b）

### 尺寸现实的两条硬结论
1. **全部候选中没有任何一项页面标注 32×40 / 32×48** —— OCC 规范的 **32×40 外沿地块**与 **32×48** 两档**无现成来源，必须自绘**。最接近的是 4 Glowing Rune Stones（32×49）与 DENZI（32×32 / 32×48）。
2. **页面明确 32×64 大画布的仅一处**：[LPC] House interior and decorations（32×64 橱柜、32×96 床）；另有 isaiah658 与 Top Down Dungeon Pack 的 64×64。

### 新增许可陷阱（累计 8 条，全部标着可用但实际有雷）
| 素材包 | 陷阱 |
| --- | --- |
| [[LPC] Bricks](https://opengameart.org/content/lpc-bricks) | 顶部许可块写 CC-BY-SA，**正文却写 "License changes to CC-BY-4.0 due to inclusion of tiles from n2liquid"** → 自相矛盾 |
| [Antique Bookshelf](https://opengameart.org/content/antique-bookshelf) | `License(s)` 图标为 CC-BY-SA 4.0，**Attribution Notice 文字却写 CC 4.0 Attribution** → 文字与链接不一致 |
| [2d Lost Garden Zelda style tiles resized to 32x32](https://opengameart.org/content/2d-lost-garden-zelda-style-tiles-resized-to-32x32-with-additions) | 评论区记录 **CC-BY 与 GPL 不可兼容、曾被 flag**；bart 称 Jetrel 部分为 CC0，Bertram 称部分新增件可能是 GPLv2 → 来源存疑 |
| [Steampunk Level Tileset Mega Pack](https://opengameart.org/content/steampunk-level-tileset-mega-pack-level-tileset-16x16) | 页面 `License(s)` 为 CC-BY 3.0，作者描述却称"无需署名只要不声称原创" → 字面冲突；**建议按 CC-BY 3.0 署名 KnoblePersona 以稳妥** |
| [32x32 Stone Brick Sprite](https://opengameart.org/content/32x32-stone-brick-sprite) | 标 CC0，描述却请求 "please credit me as : Luckius" → 与 CC0 无强制署名矛盾 |
| [Tileset brick wall 16x16](https://opengameart.org/content/tileset-brick-wall-16x16) | CC0 与 Attribution Notice "Panda Feroz" 并存 |
| [Mage City Arcanos](https://opengameart.org/content/mage-city-arcanos) | 标 CC0，树灯疑似 RPG Maker RTP，有版权争议历史 |
| [32X32 Dungeon Tileset](https://opengameart.org/content/32x32-dungeon-tileset) | 标 CC0，作者附加"不得再分发"条款，与 CC0 直接冲突 |

---

## 2f. 最终追加：新来源、实测尺寸修正与再更正

### 三个新来源（本轮此前完全没覆盖）

| 来源 | 许可（已实抓原文） | 规格 | 价值 |
| --- | --- | --- | --- |
| ★ [UnDots](https://freepixelarts-undots.com/)（日） | [许可页](https://freepixelarts-undots.com/licence/) 已核实：商用/非商用、个人/法人全免费，**无需署名、无需联络**；禁止再配布、禁止素材本体售卖；**禁止无授权用于生成式 AI 训练** | **逐件 PNG 单图，非 tileset** | 工业/科技题材件多。已核实：[ロボットアーム 工厂机械臂](https://freepixelarts-undots.com/roboarm/)（自述"工場や機械が出てくるような場面にどうぞ"）、[コンテナ 集装箱](https://freepixelarts-undots.com/container/) |
| ★ [ぴぽや倉庫 Pipoya](https://pipoya.net/sozai/)（日） | [规约页](https://pipoya.net/sozai/terms-of-use/) 已核实：个人/法人、营利/非营利均可；**无需利用报告、无需著作权表记**；禁止素材本体转售；**改変自由（色/张数/尺寸变更）**；允许附条件无偿再配布；**限制生成式 AI 训练** | **地图芯片 16×16** 与 **32×32～** 两档；另有战斗/事件/全屏特效、图标、背景、**雾效图** | **本批许可条款最干净的日文来源**。⚠「ぴぽや魔法陣エフェクトアニメ素材集」为 DLsite **付费**，不属免费 |
| ★ [indienova 开发素材库](https://ldt.indienova.com/resource/2d)（中） | 每件标注作者、体积、协议、**原站链接**：[CC0 58 件](https://ldt.indienova.com/resource/license/CC0)、[CC-BY 52 件](https://ldt.indienova.com/resource/license/CC-BY) | — | **中文侧唯一带许可标注的可依赖免费素材索引**。已核实：[2D 蒸汽朋克素材包](https://ldt.indienova.com/resource/r/2d-zqpkscbsteampunk-pack) = CC-BY 3.0，KnoblePersona，16×16，29K（与 OGA 的 Steampunk Level Tileset 同源，已交叉核实） |
| [ゲームまてりあるず](https://game-materials.com/)（日） | ⚠ **未完全核实**（规约页存在但未实抓）→ 需人工确认商用条款 | **JPG 背景插画，非可拼 tileset** | 已实抓：[小さな書斎 书房/书斋室内](https://game-materials.com/a-small-study-in-pixel-art-style/) 对学院图书馆/研究室有构图参考价值 |

### ⚠ 更正：Godot Asset Store 有素材（此前判断需限定）
此前"Godot 没有美术素材"应**限定为官方 `godotengine.org/asset-library` 检索为 0**。新的独立站 **`store.godotengine.org` 有素材**：
- 检索入口 https://store.godotengine.org/search/?query=%23tileset — **62 条结果**，可按 CC0 / MIT / Apache 过滤
- ★ [Little Factory](https://store.godotengine.org/asset/turturi/little-factory/)（Turturi）— **自定义许可，需人工读条款**；工厂主题，含**传送带动画 tileset**、地板与墙体 tile；标签 2D / Pixel Art / Top-down → **Godot 素材为引擎无关 PNG，可直接用于 Unity**
- [Free 2D Topdown Shooter Pack](https://store.godotengine.org/asset/twodpixx/free-2d-topdown-shooter-pack/)（2DPIXX）— **CC-BY 4.0**；City Tileset 121 tiles（264×264 sheet）；**现代城市**而非中世纪，后末日调性偏离
- [Verdant 00: Free Tileset Sample (16x16)](https://store.godotengine.org/asset/csaf/verdant-00-sample/)（csaf）— 自定义许可，需确认；16×16，64 tiles，附现成 Godot 4 TileSet 资源

### 新增高价值条目
| 素材包 | 许可 | 规格 | 内容 |
| --- | --- | --- | --- |
| ★ [Industrial Robot Factory Free Edition](https://opengameart.org/content/industrial-robot-factory-free-edition) | **CC-BY 4.0，必须署名 "2D Art by CookieEfedu (https://cookieefedu.itch.io)"** | **32×32 网格**，单张 PNG sheet，22.6 KB | 地面 tiles（**16-tile autotile 兼容**）+ 液体/背景墙、门、电锯、**拨杆/开关(Toggle)**、**传送带(Conveyor)**、箱体、模块化桁架、火花与子弹 VFX、2 个动画敌人（炮塔、飞行无人机） |
| ★★ [Idylwild's Arcanum](https://opengameart.org/content/idylwilds-arcanum) | **CC0，可商用、署名非强制**（经作者 OGA 镜像页确认） | **50 个 32×32 图标 + Aseprite `.ase` 源文件** | 法杖、药水炼金件（研钵研杵/骨/蟾蜍菌/曼德拉草根）、水晶宝石、**符文石（火/水/风/土 + 空白符文石）**、卷轴与法术书、羽毛笔、奥术墨水、水晶球、骷髅蜡烛 → **空白符文石可叠加自绘符号，服务"可维护以太设备"的图标语言**。这是 Idylwild 三件套之外的第 4 件 |
| ★ [Pixel Art Dungeon Props and Objects](https://craftpix.net/freebies/free-pixel-dungeon-props-and-objects-asset-pack/)（CraftPix） | CraftPix Freebie（可商用、无限项目、无需署名；禁再分发；**禁 AI 训练**） | PNG + PSD，含动画 sprite | 木桌、长凳、**货架**、木箱、床（页面明确写适合填充酒馆、守卫室或 **alchemist's labs**）、陷阱、断头台、瓶中生物、石骷髅、祭坛、书、箱、**工作台**、桶、招牌 |
| [Free Path and Road Top-Down](https://craftpix.net/freebies/free-path-and-road-top-down-pixel-tileset/)（CraftPix） | 同上 | 模块化 tile（grass base 272×496；Road 组 240×416） | 5 种地表/道路（**青灰大石"类似中世纪鹅卵石"**、棋盘格砖、泥土路、暖灰大石），含直/角/交叉/尽头模块。⚠ **模块化 tile 而非固定 32×32 格** |
| [Free Top-Down Ruins](https://craftpix.net/freebies/free-top-down-ruins-pixel-art/) · Free Rocks and Stones / Rocky Area Objects / Cave Objects | 同上 | — | 废墟/石构件，可作雨后石砌外景 |
| [Pipes Tileset](https://stealthix.itch.io/pipes-tileset)（Stealthix） | ⚠ **未核实**（itch 直抓失败） | 未核实 | **管道/阀门直接对应以太设备** → **建议优先人工核实与试看** |

### ⚠ 更正：DENZI 的价值此前被低估
其页面原始标签包含 **`puddle`（水洼）、`metal`、`rust`、`iron`、`stone`、`brick`、`cobblestone`、`wood`、`torch`、`magic`、`spell`、`3/4 view`、`orthogonal`、`overhead`**。
- 此前判定"OGA 上积水/湿地面几乎是空集，唯一可用只有 Puddle Corpses" → **更正为：DENZI（CC0，32×32 为主，含 32×48）自带 puddle 标签件，是第二个可用来源**
- 且它同时覆盖 **iron / metal / rust**（锻铁与锈蚀语汇），是「石材旧木锻铁」三项全中的 CC0 单项

### ⚠ 更正：Core Reactor Machines 尺寸极不统一
实测包内 **81 个文件尺寸混乱**：16×16×4、24×24×7、32×32×1、36×36×12、35×35×9、48×48×2、96×96×6，另有 14×24、27×27、40×40、88×115、92×139、637×659 等。包内 Info 明确 **24 pixels per meter/tile 基准**、建议 32 PPU + point filter。
→ 匹配度由"高"**下调为"中"**：题材最贴合（X 型反应堆核心 96×96、发光以太绿），但**绝大多数精灵尺寸不在目标清单内，须按 32 PPU 重制或重裁**。

### 实测尺寸修正（把"未标注"变成硬数据）
| 素材包 | 实测结果 | 结论 |
| --- | --- | --- |
| ★ Kenney **Tiny Factory** | **132 个 PNG 全为 16×16**；`tilemap_packed.png` = 192×176（12×11 = 132 格）。含灰金属地板与铆接板、黄橙机械件、带方向箭头的传送带、**管道与阀件**、木箱/托盘、危险条纹板、齿轮/机器人面、工具 | **尺寸/主题/授权三项全通过的最强 16×16 候选** |
| ★ **factory tileset (rubberduck)** | `factory_tileset_0.png` = 832×512（可被 16/32/64 整除），地面大格约 64px。画面为灰度金属地板/格栅、走道、通风口、管线（**含红色阀门轮**）、仪表屏、传送带、危险条纹、梯子 | **目前唯一目视确认含"阀门轮"的免费像素件** |
| ★ **indoor office appliances** | `office-tilemap.png` = **160×160（= 5×5 个 32×32 格）**。约 25 格：办公桌、文件柜、办公椅、显示器与电脑、**服务器机柜**、成摞书本纸张、绿植、咖啡杯、白板，以及**桌面上的一只机械臂** | **高**——32×32 完全合规、CC0、硬边像素、灰绿棕低饱和 |
| **Industrial tiles (adythewolf)** | 80×80（= 5×5 个 16×16 格） | 深棕/暗红木地板、灰金属墙板、红门、红色 X 形危险块、橙/蓝花纹地砖、黑色板条箱、电脑/终端格 |
| **Factory Tiles (TrueCynder)** | `Upload WIP.PNG` = 336×240（= 21×15 个 16×16 格），CC-BY 3.0 | 十余种金属墙/地板块；**属未完成稿** |
| **16x Animated Conveyor Belt** | `texture.png` = 16×320（**20 帧 16×16** 纵向条带） | 作者明说 "made in Blender and rendered at a lower scale" → 3D 渲染降采样，非手绘像素，需调色板量化 |
| **The Great Machine** | 200×144（**不合规**），且为**正/侧视剖面**（非俯视） | 匹配度由"中"**下调为"低"** |
| **Technology and Machinery Icons** | 17 文件 = 4 图标 ×（SVG + 512/256/64 PNG） | **名不符实**：实为 Boat / Camera / Car / Phone 黑白矢量剪影 → 与"机械/魔法工业"无关 |
| **Vending Machine Redux** | 73×117 | **确认为等距投影**（可见顶面与两侧面） |
| 已确认低匹配（实测不合规或非像素） | — | 2d Washing Machine（500×700 矢量）、top down machine PUNK（809×1795 矢量插画）、Factory tileset (ColdCoffee)（1500×1350 = 10×9 个 150×150 数字绘画）、Factory Robot Arm（180×180 平涂卡通）、Conveyor Belts SpriteSheet（147×84，含内嵌文字/logo，明确 sidescroll） |

### 新增许可陷阱（累计 11 条）
- [Conveyor Belt Spriteset](https://opengameart.org/content/conveyor-belt-spriteset) — **CC-BY-SA 4.0（传染）**；32×32，**12 个方向连接件**完整 + 演示 gif → 匹配度高但 SA
- **UnDots（日）** 与 **ぴぽや（日）** 均**明文禁止或限制生成式 AI 训练用途** —— 与 CraftPix 同类风险，**不得进入 OCC 内置生图流程**
- 反之 **Pipes and Tanks / factory tileset / Tiny Factory 等 CC0 包无此限制** —— 这是它们相对专有许可来源的关键优势

### 新增盲区确认
- **中文站许可普遍不可靠**：爱给网跨域重定向抓取失败；yswgame 与 rpg.blue 为聚合/论坛性质，作者与授权无法追溯 → **中文侧唯一可依赖的是 indienova 素材库 + 作者自述开发日志**
- **GitHub 未找到成规模的 CC0 像素 tileset 仓库**：唯一 CC0 命中的 CC0Tree 是**低多边形 3D**，其余为工具或零散 tile 集
- **日文站未找到「工房 / 機械」专门 tileset**：唯一直接命中工业机械的 UnDots 是**逐件单图、无网格**
- **`gauge / valve / dial` 这一层仍是明确空缺**（最接近：Laboratory tileset 的屏幕与能量球、Copper pipe tiles 的管道端点、**factory tileset 的阀门轮**、Godot Little Factory 的传送带）
- **学院/教室/讲堂俯视像素 tileset：0 命中**（日文侧只有动画风「アニメ学園背景」，中文侧只有立绘）；**图书馆/书架无可拼 tileset**

---

## 2g. 战棋专属素材 — 我亲自核实的三项（含一条确认与一条排除）

OGA 扫描线留下的两个"名字直接写着 turn-based strategy"的候选，加上一条投影存疑项，我逐页开页核实：

| 素材 | 许可 | 朝向 | 结论 |
| --- | --- | --- | --- |
| ✅ [**Turn-Based Strategy Buildings Set**](https://opengameart.org/content/turn-based-strategy-buildings-set) | **CC-BY 3.0**（需署名） | **正交俯视** —— 页面原文 "Set of top-down buildings for strategy games. Originally created for a turn-based sci-fi strategy taking place on Mars."；已被收入 `2D top-down`、`Top Down view` 两个收藏夹 | **可用**。标签 building / structure / **Strategy** / top / tower / wall / power / water / plant —— **战棋建筑套件，13 人收藏**。火星科幻调性需重着色调到石材/锻铁 |
| ❌ [**Dungeon TBS**](https://opengameart.org/content/dungeon-tbs) | **CC-BY-SA 3.0（传染性，无宽松选项）** | — | **排除**。虽然标签含 `dungeon tbs`（本轮扫描线里唯一的 TBS 地牢套件），但许可落在隔离区，与 OCC 闭源商业不兼容 |
| ❌ [**Classic Dungeon Walls**](https://opengameart.org/content/classic-dungeon-walls) | CC-BY 3.0（许可本身可以） | **等距（Isometric）** | **排除——投影不符**。标签明确含 `Isometric`，且被收入 `00 Isometric Stuff`、`2D::Tile::Isometric`、`Isometric collection`、`Iso DnD` 等 9 个等距收藏夹。**扫描线的警告得到确认：标题没有 isometric 但实际是等距，入库前必须看图。** |

> **教训沉淀**：`classic-dungeon-walls` 是"标题不写等距但实际等距"的实例。**OGA 上凡标题含 dungeons/walls/castle 且预览文件名带 `iso_` 的，一律先看图再排期。** 同理，`turn-based-strategy-buildings-set` 虽未标投影，但被收进 `Top Down view` 收藏夹 —— 收藏夹归属可作为投影的**辅助证据**，但**不能替代看图**。

---

## 3. 许可已核实 · 场景陈设 / 工业机械

### ★ Kenney（全站 CC0 1.0，16×16 可整数 2× 进 OCC 子网格）
| 素材包 | 文件数 | 主题 | 匹配度 |
| --- | --- | --- | --- |
| ★ [Tiny Factory](https://kenney.nl/assets/tiny-factory) | 130 | 工厂/传送带/仓库/机器 | **高**——唯一主题正对"可维护以太装置、导管、机械"的 CC0 包 |
| [Tiny Town](https://kenney.nl/assets/tiny-town) | 130 | 草地/土路/水/可见屋顶建筑/树/围栏 | 中高 |
| [Tiny Dungeon](https://kenney.nl/assets/tiny-dungeon) | 130 | 地下城/下水道墙体地面道具 | 中（石材偏冷蓝） |
| ★ [Roguelike/RPG pack](https://kenney.nl/assets/roguelike-rpg-pack) | **1700** | 瓦片/墙/地板/**家具**/门/面板 | 中高——室内陈设覆盖最全 |
| [RPG Urban Pack](https://kenney.nl/assets/rpg-urban-pack) | 480 | 路面/人行道/建筑/车辆 | 中——补"近代"底色的唯一 CC0 来源 |
| [Roguelike Indoors](https://kenney.nl/assets/roguelike-indoors) | 480 | 厨房件/桌椅/沙发/柜体 | 中 |
| [Roguelike Caves & Dungeons](https://kenney.nl/assets/roguelike-caves-dungeons) | 520 | 石墙/砖/矿 | 中 |
| [Pixel Platformer Industrial Expansion](https://kenney.nl/assets/pixel-platformer-industrial-expansion) | 110 | 工业/金属/建造/工厂（18×18） | 中高（金属件贴锻铁） |
| [Particle Pack](https://kenney.nl/assets/particle-pack) | — | 512×512 粒子贴图 | 低（非像素网格，仅做雨/烟） |

> 代价明确：Kenney 调色偏亮偏饱和，**必须整体重映射到 OCC 石材/旧木/锻铁调色板**，并做减法去掉竞争性细节。

### 其他
- [Roguelike Indoor pack](https://opengameart.org/content/roguelike-indoor-pack)（Kenney 的 OGA 镜像）— CC0，480 个 16×16 室内陈设
- **pH64 Pixel Pack** — https://opengameart.org/content/ph64-pack-100s-of-sideview-assets （正确页：https://opengameart.org/content/ph64-pixel-pack-100s-of-sideview-assets）
  - 许可：CC-BY 3.0/4.0、OGA-BY 3.0、GPL（取 CC-BY 分支）
  - 署名要求：作者名 **Stacy Kendra Love** + 品牌 **Narcissist Interactive**；发布在画集/博客时附资源链接
  - 规格：60+ 门与窗、90+ 家具、40+ 灯、70+ 电器与机械、130+ 小型植物 + PSD
  - 调色板：AAP-64 / Splendor128
  - 匹配度：中高——"电器与机械"这一层正面命中近代工业语气；缺点是侧视 2D 内部件，不是斜俯视

---

## 4. 许可已核实 · 角色 / 单位

OCC 单位契约极窄：**32×64 画布、主体 46–58 px、纯 90° 侧视、硬 Alpha、脚底 Y62 中心 X16**。免费素材里没有开箱即用的。

| 素材包 | 许可 | 规格 | 角度 | 匹配度 | 用法 |
| --- | --- | --- | --- | --- | --- |
| ★ [Side-On Starter Kit](https://opengameart.org/content/side-on-starter-kit)（steamgirl） | **CC0** | 32×32 格，页面注明 "The character is 32 x 64" | **纯侧视** | 中高 | **画布/锚点模板**——只有 idle + walk，无攻击/受击/死亡；64px 身高略超 32–58 窗口 |
| [Agent Character](https://opengameart.org/content/agent-character)（Chasersgaming） | CC0 | 32×32 | 纯侧视 | 中 | 西装特工，idle + walk + 拳脚；**主题上最接近"学院制服/守卫"** |
| [Character Animations (Caveman)](https://opengameart.org/content/character-animations-caveman)（Chasersgaming） | CC0 | 32×32 | 纯侧视 | 中 | **动画集最全**：15 状态含 Idle/Walk/Run/Swing(攻击)/Hurt/Death——最接近 OCC 要求的动作集 |
| [Pixel Character 01/02/03](https://opengameart.org/content/pixel-character-01-onur)（ImogiaGames） | CC0 | 16×16 | — | 中 | idle+walk 前后侧+attack+hit+death，用 24 色 archererer24 调色板（命中 ~20–24 色规则）；16px = OCC 最小值一半，只能 2× 放大做一格 chibi 或当动画参考 |
| [Pixel Chibi Character Pack](https://opengameart.org/content/pixel-chibi-character-pack-%E2%80%94-knight-archer-gunner-spearman-48%C3%9748-animated)（UnitForge） | CC0 | 48×48 | — | 中 | 显式 idle/move/attack/damaged/death + 左右朝向 + 部件换装构建器 + 22 怪物；**最接近"能做出一批学院学生/教师/守卫"的系统**，但 chibi 比例 ≠ Sephiria |

### 敌人 / 构装体
| 素材包 | 许可 | 规格 | 匹配度 |
| --- | --- | --- | --- |
| [Space Bot Rework](https://opengameart.org/content/space-bot-rework)（AntumDeluge） | CC-BY 3.0 | **48×64 画布**，正交 N/E/S/W，idle + 12 帧漂浮，5 配色 | 画布尺寸最贴"漂浮装置" |
| [Industrial Robot Factory Free Edition](https://opengameart.org/content/industrial-robot-factory-free-edition)（CookieEfedu） | CC-BY 4.0 | 32×32，Turret + Flying Drone，idle/damage/dead/attack | **工业锻铁语气命中**，是"以太多构体"的合理基座 |
| [Gum Bot sprites](https://opengameart.org/content/gum-bot-sprites)（GrafxKid） | CC0 | 机器人 walk/blink/powered-down，4 配色 | ⚠ 归在 OGA "3/4 Directional Sprite Sets" → **身体角度是 3/4，需改造** |
| [Animal Enemies 10 Pack](https://opengameart.org/content/animal-enemies-10-pack)（OnixGames） | CC0 | 含 Dog / Fox，侧视剖面 | "机械犬"最合理的基座 |

### 单位动画集的系统性结论

角色调研的最终判断：**没有任何现成素材包能直接满足 OCC 契约。许可不是瓶颈——CC0/CC-BY 像素角色很多；瓶颈是四条硬性要求的交集：32–58px 主体 × 纯 90° 侧视 × 粗颗粒剪影 × idle+walk+attack+hit+death。** 免费素材集中在 16×16（太小）或 3/4 斜俯视（角度错）。"纯侧视 + 32–58px + 硬边 + 锻铁/以太题材"四项同时成立，在可达来源中**客观上不存在**。

### ★ CraftPix 免费件（页面亲验，主题命中率最高）

| 素材包 | 页面亲验要点 | 匹配度 |
| --- | --- | --- |
| ★★ [Free City Enemies Pixel Art Sprite Sheets](https://craftpix.net/freebies/free-city-enemies-pixel-art-sprite-sheets/) | 原文 "All graphics consist of sprite sheets (**48 pixels in height**)"、6 个角色（**含 robotic drones**）、"Each has a unique animation (**Attack, Death, Hurt, Idle, Walk**)"、**Vector: No** | **高**——全调查中唯一同时满足：页面亲验 48px 原生（正落 32–58 带中央）+ 五态齐备 + 纯侧视 + Vector: No（降低矢量软边风险）。**robotic drones 就是现成的以太多构体** |
| ★ [Free Factory Boss Enemies](https://craftpix.net/freebies/free-factory-boss-enemies-asset-pack-for-cyberpunk/) | 侧视已亲验（"designed for platformers, beat 'em ups... 2D side-scrolling games"）；3 个 Boss 含**机械履带四足机器人**与火炮装甲车 | 主题最贴（近代工业 + 机械四足）；⚠ 但 Technical Details 标 **Vector: Yes** ⇒ 很可能抗锯齿/软边，**大概率淘汰**；尺寸未标 |
| [Free Tiny Schoolgirl](https://craftpix.net/freebies/free-tiny-schoolgirl-pixel-art-sprite-pack/) | 3 个"distinctive school uniforms"的学生，完整状态集（Idle/Walking/Attack/Pain/Death），侧视 | **字面最贴 OCC"学院学生"阵容**；⚠ 页面写 "soft color palette" ⇒ 有大色块规则风险；尺寸未标 |

> ⚠ **CraftPix 条款 3.1.1 可能整体否决该站**：禁止把素材用于"训练、微调、开发、测试、验证或改进任何 AI/ML 系统"。OCC 的美术生产正是 `image_gen` 驱动的——
> - 作为**成品 PNG 直接使用或人工重绘** = 可以；
> - 作为 `native32_generation_profiles.json` / `image_gen` 的**输入或参考** = **合同性地被禁止**。

### 其他新增（许可已核实）
| 素材包 | 许可 | 规格 | 匹配度 |
| --- | --- | --- | --- |
| [2D Soldier Guy Character](https://opengameart.org/content/2d-soldier-guy-character)（Segel） | CC-BY 3.0 + OGA-BY 3.0 | 需下载确认 | **唯一"纯侧视 + 完整五态（Gun/Sword 两套 Idle/Run/Attack/Jump/Hurt + Die）+ 白名单许可"的免费人形**；持枪士兵比中世纪骑士更贴近代。⚠ **单帧原生高度页面未标注，是成败未知量** |
| [Toen's Medieval Strategy Sprite Pack v1.0](https://opengameart.org/content/toens-medieval-strategy-sprite-pack-v10-16x16) | CC-BY 3.0 | 16×16 | **战棋单位**，主题对口 |
| [24x32 Pepper&Carrot Characters](https://opengameart.org/content/24x32-peppercarrot-characters) | CC-BY 3.0/4.0 | 24×32 | 四向行走 + **蒸汽朋克女巫** |
| [Zombie and Skeleton 32×48](https://opengameart.org/content/zombie-and-skeleton-32x48) | **CC0** | **32×48** | 尺寸直接命中 OCC 目标档 |
| [superpowers-asset-packs](https://github.com/sparklinlabs/superpowers-asset-packs)（sparklinlabs） | **CC0 1.0，零署名**（调查中纸质最干净） | 16px 级 | 含 ninja-adventure（`characters/1.png…25.png` + `dog.png` + `faceset/`），**仓库内未标注任何尺寸**；3/4–俯视手绘软渐变，**大概率风格淘汰** |

---

## 5. 许可已核实 · UI / 九宫格 / 图标

### ★★ Golden UI（Buch）— 本轮最值得先做样板的一套
- https://opengameart.org/content/golden-ui　许可：**CC0**
- 内容：面板、按钮、滚动条、滑块、指针、菜单、banner、message box、填充条、inventory 槽、minimap；作者建议 **2× 放大**（= OCC 2px 网格的整数倍）
- 调色：DawnBringer **暖金/棕/青铜**
- 匹配度：**高**——暖金正是"氧化黄铜 `#A86F22`"的天然近亲，配旧木/锻铁语汇；改四档语义色即可落地

### 其余 UI
| 素材包 | 许可 | 规格 | 匹配度 | 用法 |
| --- | --- | --- | --- | --- |
| ★ [UI pieces](https://opengameart.org/content/ui-pieces)（Buch） | CC0 | DB16 暖棕金；血/蓝条（填充可裁剪）、minimap、portrait、icon slot、action slot + 2× 版 | 高 | 槽位语法直接对应 OCC 术式卡/选卡/状态条 |
| [RPG status icons 16x16 and 8x8](https://opengameart.org/content/rpg-status-icons-16x16-and-8x8)（Buch） | CC-BY 3.0（署名 Buch + OGA 链接） | 8 个状态图标（中毒/流血/防御/燃烧/治疗/攻击/溺水/回蓝），16×16 与 8×8 | 中 | **免费池里唯一语义命中且许可合格的状态图标**；四档语义色需自绘补齐 |
| ★ [GUI Windows constructor](https://opengameart.org/content/gui-windows-constructor)（yd） | CC0 | **32×32 模块化窗口构造器** | 中 | 少数能满足"厚边九宫格内边 ≥12px 文字安全区"的自由素材 |
| [Pointers: part 5](https://opengameart.org/content/pointers-part-5)（yd） | CC0 | **锻铁手套光标 72×72**，可缩到 48×48 | 中高 | 锻铁语汇；48×48 正好是地图节点图标档 |
| [Journal](https://opengameart.org/content/journal)（yd） | CC0 | 日志/旧纸页面 XCF | 中高 | 暖纸档案参考 |
| [Notice sheet](https://opengameart.org/content/notice-sheet)（R3tr0BoiDX） | CC0 | 像素便签纸纹理 | 中高 | 纸面底纹 |
| [PixelArt Skills Icons](https://opengameart.org/content/pixelart-skills-icons)（DeadKir） | CC0 | 10 个 **32×32** 技能图标 | 中 | 尺寸正好在 OCC 图标档 |
| [Vintage UI Pack](https://opengameart.org/content/vintage-ui-pack)（Eliza Wyatt） | CC-BY 3.0 | 36.7 KB 极简复古组件 | 中 | 体量小，改造成本低 |
| [Pixel Parchment UI Kit](https://opengameart.org/content/pixel-parchment-ui-kit)（Sasquatchii） | CC-BY 4.0 | 整屏 VN 菜单，**480×321** | 高（主题） | **不是 9-slice 组件**；当"暖纸档案"氛围与配色校准样本（480×321 贴 OCC 480×270） |

### Kenney UI（CC0，但风格不对口）
- [Pixel UI Pack](https://kenney.nl/assets/pixel-ui-pack) — 750 文件，含 **30 个独立 9-slice**；但正是 OCC 明令排除的"厚框灰底白边"挤压型 → 仅取结构语法
- [Fantasy UI Borders](https://kenney.nl/assets/fantasy-ui-borders) — 140 文件 → 仅参考
- [UI Pack - Pixel Adventure](https://kenney.nl/assets/ui-pack-pixel-adventure) — 500 文件，高饱和卡通 → 匹配度低

---

## 5b. ★ VFX / 图标（逐像素实测，含一条关键否定结论）

> 本节的尺寸与色数**全部由下载文件后用脚本逐像素统计**，不是页面文字；硬 Alpha 用"是否存在 1–255 的部分透明像素"判定。

### 关键否定结论
**没有任何一个素材包原生同时满足 32×32 + ≤12 色 + 4–10 帧 + 硬 Alpha。**

真实存在且质量很高的是**一条降级可用路径**：**原生 16×16 + 每帧 2–6 色 + 硬 Alpha + 4–8 帧**，整数 2× 放大即 32×32，恰好形成 OCC 规则本来就允许的**均匀 2×2 宏像素格**。这条路径足以覆盖 OCC **完全没有**的八元素语义，且许可统一为 CC0。**这是本轮唯一"能直接进管线"的收获。**

### ★★★ Top：八元素特效（补 OCC 最真实的缺口）
[**Pixelart Spells（DevWizard）**](https://opengameart.org/content/pixel-art-spells) — **CC0 1.0**
- **实测**：单帧 **16×16**；每表 96×16（6 帧）/ 64×16（4 帧）/ 128×16（8 帧）。**每张表全表仅 2–6 色**（Arcane Bolt 3、Fireball 3、Ice Lance 2、Magic Ray 5、Shield 2）。**零部分透明像素 = 纯硬 Alpha**。含 PNG + 21 个 Aseprite 源
- **内容**：**八元素全覆盖** —— fireball / firebomb / ice lance / water bolt / water blast / water orb / splash / wind bolt / rock sling / plant missile / light bolt / bolt of purity / darkness bolt / darkness orb / arcane bolt / magic orb / **magic shield** / **pixelart shield** / magic ray / magic sparks
- **为什么最重要**：OCC 现有 39 组特效**完全集中在火焰系列 + 通用命中/状态/资源，火水风土雷冰光暗八元素基本是空的**。这是唯一免费且覆盖八元素 + 护盾的包
- **代价**：配色需重映射到 OCC 四语义色 + 八元素色；16×16 放大后线条比原生 32 略粗，需一轮人工审美

### ★★ Top：图标（补 OCC 的尺寸缺口）
[**RPG UI Icons（OwlishMedia）**](https://opengameart.org/content/rpg-ui-icons) — **CC0 1.0**
- **实测**：**108 个图标全部原生硬边** —— **22 个 16×16 + 86 个 32×32**；色数 3–11，**78/108 在 4 色以内**；**全部零部分透明像素**。8× 放大目视确认无抗锯齿、无渐变
- **内容**：**16×16 状态语义** —— alert / berserk / blind / brave / confuse / eagle_eye / frozen / nausea / poison / regen / silence / sleep / slow / smoulder / stone / stunned / swift / weaken / shield（**正好覆盖 OCC 的 burning/bound/slow/dazzled/cleanse/lock 语义族**）。**32×32 物品/元素** —— sword / axe / bow / **gun（左轮）** / hammer / spear / staff / **八元素齐全**（fire/water/wind/earth/lightning/ice/light/darkness）
- **代价**：**16×16 状态图标中 19 个超过 OCC 的 4 色上限**（strengthen 11 色、nausea/weaken 10、swift 9、shield/regen/slow/berserk 8）→ **必须降色**

### ★ Top：爆炸/冲击
[**Explosion Set 2**](https://opengameart.org/content/explosion-set-2-m484-games) + [**Explosion Set 1**](https://opengameart.org/content/explosion-set-1-m484-games)（M484 Games / Master484）— **CC0 / Public Domain，图内自带 "LICENSE: PUBLIC DOMAIN / CC0" 自证**
- **实测**：Set 2 整表 850×850，**含原生 16×16 与 30×30 两种尺寸**，3 造型 × 4 配色主题；分区统计 16×16 列 **13 色**、圆形弹族 17 色、30×30 环形族 21 色（**一条动画实际只用一条 2–4 色色阶**）。Set 1 整表 790×440 全表 **16 色**，**内含 16×16 版本（8 色）**
- **零部分透明像素 = 硬 Alpha**；"多配色主题"结构正好对应 OCC 八元素换色需求

### 其余可核实条目
| 素材包 | 许可 | 实测 | 结论 |
| --- | --- | --- | --- |
| ★ [Splash Effect 32×32](https://opengameart.org/content/splash-effect-32x32)（Jesse McCarthy） | CC-BY 3.0 | 128×32 = **4 帧 × 32×32 原生**，全表 **4 色**，硬 Alpha | **本轮仅有的两个原生 32×32 合格素材之一**（水花） |
| ★ [Firebal 32x32](https://opengameart.org/content/firebal-32x32)（MSavioti） | **CC0** | 128×32 = **4 帧 × 32×32 原生**，全表 **9 色**，硬 Alpha | **另一个原生 32×32 合格素材**（火球投射物） |
| [16×16 Explosion](https://opengameart.org/content/16x16-explosion)（BitingChaos） | **CC0**（包内 ReadMe 明写无需署名） | 80×16 = **5 帧 × 16×16**；单帧色数 3/3/6/4/1，硬 Alpha | 5 帧正好落在 4–10 窗口，但只补一个效果位 |
| [496 pixel art icons](https://opengameart.org/content/496-pixel-art-icons-for-medievalfantasy-rpg)（7Soul1） | **CC0** | **496 个文件全部 34×34**；色数 3–40，**中位 8 色**，435/496 ≤12 色 | 物品图标扩量；需 34→32 尺寸规整 |
| [CC0 Light Icons](https://opengameart.org/content/cc0-light-icons) | **CC0** | Jetrel 子集 6 文件：32×32 ×2、**24×24 ×2**、64×64、**48×48**；色数 9–13 | 证明 48×48 与 24×24 的 CC0 像素图标确实存在 |
| [Controller Input Icons](https://opengameart.org/content/controller-input-icons)（ElDuderino） | **CC0** | 声明含 128×128、**48×48、32×32** + **源 SVG**（可任意尺寸再导出） | 少数"48 档不需放大"的选择 |
| [PixelArt Skills Icons](https://opengameart.org/content/pixelart-skills-icons)（DeadKir） | **CC0** | 仅 10 个图标（尺寸按标签 32×32，**页面正文未复述，待复核**） | 数量太少 |
| [Electricity Overlay Effect](https://opengameart.org/content/electricity-overlay-effect)（AntumDeluge） | CC-BY 3.0 / OGA-BY 3.0 | 1584×64，全图 **2 色**，硬 Alpha（索引色，仅 371 字节）；另有 blue/red/violet 换色 | **像素语言最干净**，直接对应"以太放电"；但是叠加层、64 高非 32 倍数 → 需重排 |
| [Minimap Pack](https://kenney.nl/assets/minimap-pack)（Kenney） | **CC0** | **实测 164 个 PNG 中 150 个确为 8×8**，162/164 ≤12 色 | 6× 整数放大即 48×48 且仍是均匀宏像素；但语义是房间标记 |
| [Input Prompts Pixel](https://kenney.nl/assets/input-prompts-pixel) / [… 1-Bit](https://kenney.nl/assets/input-prompts-pixel-1-bit)（Kenney） | **CC0** | 实测 **816/819 与 1632/1637 个确为 16×16**，≤12 色；1-Bit 版严格 2 色 | OCC 规格要求"快捷键 24×24"→ 本包是 **16×16 原生**，3× 放大即 48 或 1× 用 16 |
| [UI pieces](https://opengameart.org/content/ui-pieces)（Buch） | **CC0** | `ui_2.png` 184×80，**恰好 16 色**（DawnBringer）+ 2× 版 | **最接近 OCC 12 色规则的调色板**；作节点/图标外框与槽位底座 |

### 明确排除（含关键理由）
| 素材 | 理由 |
| --- | --- |
| **Kenney 全 VFX 系列** | **完整否定结论：Kenney VFX 系列仅 5 个包（Light Masks / Splat Pack / Foliage Sprites / Particle Pack / Smoke Particles），不存在像素 32×32 VFX 包**；Particle Pack 512×512 软 Alpha |
| CodeManu Free Pixel Effects Pack / Pixel FX Pack | 帧 100×100，**多数表单帧 79–1901 色**，是渲染渐变而非 12 色像素画 |
| Clint Bellanger Sparks / Heal / Shield / Rune | Blender 渲染 + 作者自述 motion blur，软边渐变 |
| Mikodrak 2D Spell Effects | AfterFX 噪声/扭曲合成，无像素网格 |
| ansimuz Animated Explosions | 色数（3–6）与硬 Alpha 合格，但**帧尺寸 40×40 起、最大 192×192**，且用了**有序抖色**直接违反"无抖色" |
| JROB774 Pixel Explosion (12 Frames) | 96×96 且 **12 帧超上限**（但 8 色很克制，可作重绘母版） |
| Viktor Hahn Attack/Hit | 64×64，降到 32 是破坏性重采样 |
| Sinestesia Hit Animation | 单帧 1024×1024、16 帧，超三个数量级 |
| Calinou Lightning | Art Type 为 Texture 的 HD 贴图序列 |
| ElricJohan Booms Explosions | 含**爱心**爆炸，OCC 明确排除 |
| tiopalada Spring VFX Pack | 春季/复活节题材与"近代魔法工业"直接冲突 |
| Kelvin Shadewing Elemental Charge | **GPL 3.0 + CC-BY-SA 4.0**，不在白名单 |
| ⚠ **Jetrel/Frogatto explosion animations** | **许可自相矛盾**：OGA 页面标 CC0，但源仓库 `LICENSE` 实为 CC-BY 3.0 且列出 `images/characters` 等为 **CC-BY-NC-SA 豁免目录**；OGA 评论区也有 "License is incorrect!" 指正 → **无法证实，建议直接放弃** |
| ⚠ **DENZI CC0 Shield Icons（knik1985 子集）** | 48/64 档实测 **161–24239 色** —— 是插值放大产物，**不是原生像素画**（汇编者本人声明非 32 尺寸用 GIMP 三次插值 + Scale2x 生成） |
| **game-icons.net** | CC-BY 3.0 但**是矢量剪影非像素画**，且 4000+ 图标**每个作者不同、需逐个署名** |
| Lorc 700+ RPG Icons | 400px 黑白位图，需完全重绘 |
| Ardentryst Cursors/Arrows/Map Markers | 带明暗渐变的 GUI 美术 |
| CraftPix / Unity Asset Store | 自定义许可非 CC；CraftPix §3.1.1 禁 AI 训练；Unity EULA 需账号且禁再分发 |

### VFX/图标层的新增盲区
1. **地图节点图标（48×48）没有合格解** —— OGA 按 `48x48` 标签**仅 40 个结果**（全是人脸/角色/飞船，无图标包）、`map marker` 标签**仅 2 个结果**。**"开始/普通战/精英战/Boss/工坊/医务室/商店/事件"这 8 类语义不存在现成的宽松许可素材包** → 只能自绘字形 + 用 CC0 外框/标记拼装。
2. **64×64 术式图标（元素轮廓）在 CC0 像素画里未找到** —— 最接近的是 game-icons.net（CC-BY 3.0 矢量，非像素）与 496 图标包（34×34，尺寸不对）。
3. **没有任何素材符合"工程化能量"主题** —— 没有以太放电、符文回路、锻铁过载、蒸汽烟尘的现成品。**这是排期影响最大的发现：即使许可最好的素材也需要完整的美术方向改写，主题层面可能根本不存在现货解。**
4. **帧数多有缺口** —— 仅 BitingChaos（5 帧）、DevWizard（4–8 帧）、M484（按造型推断）能确认落在 4–10；多数候选的精确帧数未能从文件层面确认。

---

## 6. 许可已核实 · 天气 / 雨景 / 特效 / 字体

### 雨（首战"雨后学院"的关键，已确认）
| 素材包 | 许可 | 规格 | 匹配度 |
| --- | --- | --- | --- |
| ★ [LPC] Rain animation | CC-BY 3.0 / GPL 3.0 / OGA-BY 3.0（可取 CC-BY） | **64×64，10 帧**，LPC 调色板，可平铺 | **高**——尺寸正落在 OCC 的 64 档，雨后场景直接叠 https://opengameart.org/content/lpc-rain-animation |
| [RAIN (raidensan)](https://opengameart.org/content/rain) | CC-BY 3.0 | 雨滴 sprite / 背景动画 | 中 |
| [10 Pixel Art Atmospheric VFX Pack](https://alenia-studios.itch.io/10-pixel-art-atmospheric-vfx-pack)（Alenia Studios） | **CC0** | 雨/雪/风/神之光/萤火虫/樱花瓣/落叶/龙卷风/流星/火花；320×180、48 帧无缝循环 | **高**——唯美雨景正好是"雨后学院"的天气层。⚠ 许可经由作者中文发布页交叉确认：https://ld0.indienova.com/u/aleniastudios/blogread/38281 |

### 中文像素字体（均已核实汉字覆盖原文）
| 字体 | 许可 | 汉字覆盖 | 结论 |
| --- | --- | --- | --- |
| [Fusion Pixel Font](https://github.com/TakWolf/fusion-pixel-font)（**现用**） | OFL 1.1，**未声明保留字体名** | GB2312 7269/7445（97.64%）；一级 3755 满、**二级缺 145**；CJK 统一 19214/20992；**8px 比例模式汉字 6763/6763 全覆盖** | **保留为主用**——8/10/12px 三档、等宽+比例双模式，24/48/72px 正好 2×/4×/6× |
| ★ [Z 工坊像素黑体 12px](https://github.com/Astro-2539/ZLabs-Pixel-12px) | OFL 1.1（**声明保留字体名 "Z工坊/ZLabs"**，衍生须改名） | GB/T 2312 **6763/6763**、通用规范汉字表 **8105/8105** 双 100%；CN 变体合计 12994 汉字 | **强烈建议做一次 A/B**——唯一能在 12px 档补上 Fusion 那 145 个二级汉字缺口的合格候选。新项目（2025 起），须锁定并冻结 Build（建议 Build_20260519） |
| [Ark Pixel 方舟像素 12px](https://github.com/TakWolf/ark-pixel-font) | OFL 1.1 | GB2312 6930/7445（93.08%），一级缺 172、二级缺 312；16px 已废弃 | 不如现有（官方 README 自己让 10/12px 用户改用 Fusion） |
| [Cubic 11 俐方體](https://github.com/ACh-K/Cubic-11) | OFL 1.1 | 常用國字 4808、Big5 5401、**GB2312 仅一级** | 不如现有（简体无二级） |
| [BoutiqueBitmap9x9](https://github.com/scott0107000/BoutiqueBitmap9x9) | OFL 1.1 | 12,858 字符，**简体覆盖未量化** | 不作主力，9px 备选 |
| [GNU Unifont](https://unifoundry.com/unifont/) | **双许可**：GPLv2+嵌入例外 或 OFL 1.1 | BMP 全覆盖（CJK 20992） | **仅作缺字 fallback**；只有 8×16/16×16 单档，混排割裂 |

---

## 6b. ★ 学院室内 / 符文以太 / 雨后湿街（主题定向新增）

### 学院 / 教室 / 室内
| 素材包 | 许可 | 尺寸 | 内容 |
| --- | --- | --- | --- |
| ★★ [Classroom 002](https://opengameart.org/content/classroom-002) | **CC0** | — | **教室室内场景——本轮全站扫描到的唯一一个"教室"**，学院题材直接可用 |
| ★★ [16x16 indoor rpg tileset: the baseline](https://opengameart.org/content/16x16-indoor-rpg-tileset-the-baseline) | CC-BY 3.0 + OGA-BY 3.0（OGA 官方委托） | 16×16 正交 | **旧木地板、木家具、书架**——学院室内基底；另有 [Expansion 1](https://opengameart.org/content/rpg-indoor-tileset-expansion-1) |
| [Modern Houses Tileset TopDown](https://opengameart.org/content/modern-houses-tileset-topdown) | **CC0** | — | 近代住宅室内俯视 |
| [Sci-fi Interior Tiles (47 autotile)](https://opengameart.org/content/sci-fi-interior-tiles-47-autotile-edition) | **CC0** | — | 科幻室内 47 自动地形 |
| [Framed Artwork](https://opengameart.org/content/framed-artwork) | **CC0** | — | 墙面装饰（挂画/挂旗） |
| [Granny's House](https://opengameart.org/content/grannys-house) · [Chipsets from NeoWolf](https://opengameart.org/content/chipsets-from-neowolf) · [Interior spritesheet 16x16](https://opengameart.org/content/interior-spritesheet-16x16-tiles) | **CC0** | 16×16 | 室内补充 |
| [The Lost Library](https://opengameart.org/content/the-lost-library) | CC-BY 3.0 | — | 维多利亚-蒸汽朋克图书馆建筑，5 个尺寸版本；⚠ 矢量渲染而非像素 |
| [[LPC] House Insides](https://opengameart.org/content/lpc-house-insides) | CC-BY-SA 3.0 + GPL 3.0 | 32×32 | 橱柜/桌椅/壁炉/火把/床/门/墙/地板/花瓶/厨房水槽——⚠ 传染性 |
| [[LPC] City inside](https://opengameart.org/content/lpc-city-inside) | CC-BY-SA 3.0 / GPL 3.0 / 2.0 | 32×32 | 地毯/桌/橱柜/**火盆(brazier)**/王座/门/马厩门——火盆可改造为以太灯具/锅炉；⚠ 传染性 |
| ★★ [Free Glassblower's Workshop Top-Down](https://craftpix.net/freebies/free-glassblowers-workshop-top-down-pixel-art-asset/)（CraftPix） | CraftPix Freebie（可商用、免署名；**禁 AI 训练**） | 未标 | **熔炉（带火焰动画）、石板/木墙、烟囱、煤仓、大小罐子、花瓶、玻璃马赛克、桌子、水晶箱、地毯**；门有开合动画——罕见的**工坊室内成套** |
| [Free Urban Shooter Top-Down Mini Game Kit](https://craftpix.net/freebies/free-urban-shooter-top-down-pixel-art-mini-game-kit/)（CraftPix） | CraftPix Freebie | — | **砖墙店面、屋顶、消防梯、沥青路面、金属梯、发光招牌、自动售货机** + 40+ UI 图标（含**齿轮与铃**） |
| [Free Ruined Temple Top-Down](https://craftpix.net/freebies/free-ruined-temple-top-down-location-pixel-art/)（CraftPix） | CraftPix Freebie | — | 石造废墟 |
| [DOTOWN](https://dotown.maeda-design-room.net/)（日，前田デザイン室） | 免费商用（**须核对** https://dotown.maeda-design-room.net/term-of-use/） | 约 700 点，粗 16px | 含 **学校(148)**、文房具、インテリア(室内)、天気、梅雨、PC、日用品；风格偏可爱，需人工筛 |
| [House Interior Tileset 32x32 LITE](https://assetstore.unity.com/packages/2d/environments/house-interior-tileset-32x32-lite-307715)（Unity） | Standard Unity EULA | **32×32** | Unity 免费区里最对口的 32×32 正交室内家具；⚠ 非 CC 系，且禁 AI 训练 |

### 符文 / 以太回路（"以太"是最难找的主题，靠这几支补上）
| 素材包 | 许可 | 尺寸 | 内容 |
| --- | --- | --- | --- |
| ★ [4 Glowing Rune Stones](https://opengameart.org/content/4-glowing-rune-stones-32w-x-49h) | **CC0** | **32×49**，**6 帧发光动画** | 宽度正好贴合原生 32 格基线，高度 49 接近契约的 32×48；发光帧可直接当以太节点 |
| ★ [32x32 Line Runes](https://opengameart.org/content/32x32-line-runes) | **CC0** | 32×32 | **连线式符文地块——形态接近"以太回路 / 导线图"，是本轮最接近"符文回路"的现成件** |
| ★ [Idylwild 三件套](https://opengameart.org/content/idylwilds-alchemical-codex) | **CC0** | Alchemical Codex 16×16 / Arcanum 32×32 / Runic Codex 16×16 | **Alchemical Codex 直接含 Aether 的炼金替代符号** |
| [Universal Fantasy Roguelike Tileset 16x16](https://opengameart.org/content/universal-fantasy-roguelike-tileset-16x16) | CC-BY 4.0 | 16×16 正交 | Obj 含 **Rune / Inscription（墙地铭刻）/ Switch On-Off / Light Source / Crafting Table**；Tiles 含石地板、木地板、石墙、木墙 |
| [Gas gauge](https://opengameart.org/content/gas-gauge) | CC-BY 3.0 | — | 油量表盘（无指针），可做以太压力表 |
| [Industrial Simulation Icons](https://opengameart.org/content/industrial-simulation-icons) | CC-BY 4.0（含 SVG） | — | 工业设施图标，含阀门/储罐造型 |
| [Newton's Octant](https://opengameart.org/content/newtons-octant) | CC-BY 3.0 | 16–256px PNG + SVG | **黄铜精密仪器**图标 |
| [The Great Machine](https://opengameart.org/content/the-great-machine) | CC-BY 3.0 | DB16 调色板 | 像素机械，含拉杆 |
| ⚠ [Gauge Bar Double](https://opengameart.org/content/gauge-bar-double) | **CC-BY-SA 3.0** | — | 双值仪表条（gauge/energy/fuel/magic）——传染性，排除 |
| ⚠ [[LPC] Interface Items by Tempest in the Aether](https://opengameart.org/content/lpc-interface-items-contribution-by-tempest-in-the-aether) | **CC-BY-SA 3.0**（**内嵌图标注 CC-NC-SA，有冲突记录**） | 2,151 文件 / 24 文件夹 | **唯一的 brass 黄铜主题 + steampunk 标签免费大包**：黄铜面板、建筑状态图标、职业/任务图标、头盔护甲图标、坐骑、公会徽记；被收录进 "TDS: Steampunk/Victorian"。→ 传染性排除；若要冒险采用，**必须自行在包内复核许可** |

> ⚠ **OGA 的 CJK 检索完全无效**：`魔法` 被站点直接拒绝（"You must include at least one positive keyword with 3 characters or more"，2 个汉字判定长度不足）；`魔法学院`、`タイルセット` 均 0 结果；`sigil` 连 tag 都不存在。→ 中日文只能转向 DOT ILLUST / Pixnote / DOTOWN / indienova 等本土站。

### 雨后 / 潮湿 / 水面（首战"雨后学院"）
| 素材包 | 许可 | 尺寸 | 内容 |
| --- | --- | --- | --- |
| ★ [[LPC] Rain animation](https://opengameart.org/content/lpc-rain-animation) | CC-BY 3.0 + GPL 3.0 + OGA-BY 3.0 三选 | **64×64，10 帧**，LPC 调色板，可平铺 | 尺寸精确落在 64×64 |
| ★ [Rain Animation](https://opengameart.org/content/rain-animation)（Stendhal / AntumDeluge） | CC-BY 4.0/3.0 + OGA-BY | **64×64 正交可平铺，3 档雨强 × 16 帧** | 三档雨强可做"雨势变化"动态天气 |
| [RAIN (raidensan)](https://opengameart.org/content/rain) | CC-BY 3.0 | — | 雨滴 sprite / 背景动画 |
| [Swapshot Rain animations](https://opengameart.org/content/swapshot-rain-animations) · [Rain Particle Animated](https://opengameart.org/content/rain-particle-animated) | — | — | 雨动画 / 雨粒子补充 |
| ★ [LPC Modern Streets](https://opengameart.org/content/lpc-modern-streets) | **CC0** | **32×32** | 含**雨水沟、排水格栅、井盖**——同时命中"近代工业 + 雨后"两个关键词 |
| [Puddle Corpses](https://opengameart.org/content/puddle-corpses) | CC-BY 3.0 | 32×32 / 64×64 | **OGA 上唯一正俯视可用的积水件** |
| ★ [10 Pixel Art Atmospheric VFX Pack](https://alenia-studios.itch.io/10-pixel-art-atmospheric-vfx-pack)（Alenia） | **CC0** | **320×180**，48 帧无缝循环 | 唯美雨景、雪、疾风、神之光、萤火虫、樱花瓣、落叶、龙卷风、流星雨、火花；SpriteSheet + 单帧 PNG |
| [16x16 Puny Dungeon Tileset](https://opengameart.org/content/16x16-puny-dungeon-tileset) | **CC0**（正文 LICENSE 段亦写明） | 全部 16×16 | 2-edge Wang 自动墙、地面变体、动画水/火把、**陷阱（落石/地刺/机械刀片/墙式喷火器/陷坑/捕兽夹）**、可交互件（**压力板**/木箱/传送门/钥匙）+ Tiled 示例——"压力板 + 机械刀片"几乎是"维护指令"谜题件的现成原型 |

### 单位补充（CC0）
[32-pixel human sprites](https://opengameart.org/content/32-pixel-human-sprites)（**32px 人形，战棋基线**）· [32x32 RPG character sprites](https://opengameart.org/content/32x32-rpg-character-sprites) · [puny characters](https://opengameart.org/content/puny-characters) · [24x32 bases](https://opengameart.org/content/24x32-bases) · [tiny characters set](https://opengameart.org/content/tiny-characters-set) · [assorted 32x32 creatures](https://opengameart.org/content/assorted-32x32-creatures) · [animated birds 32x32](https://opengameart.org/content/animated-birds-32x32) · [bat 32x32](https://opengameart.org/content/bat-32x32) · [animated top down zombie](https://opengameart.org/content/animated-top-down-zombie)

### ⚠ 许可陷阱（标着 CC0 但实际有雷，务必人工复核）
| 素材包 | 陷阱 |
| --- | --- |
| [Mage City Arcanos](https://opengameart.org/content/mage-city-arcanos) | 标 CC0，但其树灯疑似源自 RPG Maker RTP，**有版权争议历史** → 不用，或逐 tile 核对 |
| [32X32 Dungeon Tileset](https://opengameart.org/content/32x32-dungeon-tileset) | 标 CC0，但作者附加"不得再分发"条款，**与 CC0 直接冲突** |
| [[LPC] Interface Items by Tempest in the Aether](https://opengameart.org/content/lpc-interface-items-contribution-by-tempest-in-the-aether) | 页面标 CC-BY-SA 3.0，**包内嵌图标注 CC-NC-SA**；作者称已按更宽松 LPC 发布，站点管理员确认"文件内标注已过期，站内标注正确" → 按 CC-BY-SA 3.0 处理 |
| `2D Water animations Tiles`（opengameasset.net） | **聚合站，许可未核实** → 不用 |
| CraftPix 全部素材 | file-licenses 第 3 节**明确禁止用于 AI/ML 训练、微调、生成式 AI**——与 AGENTS.md 规定的内置生图链路直接冲突，**任何 CraftPix 素材都不得进入生图流程** |
| DOT ILLUST（日） | 免费商用但**每个作品上限 30 件素材**，禁再分发/NFT/模板化（https://dot-illust.net/terms/）→ 只能点缀 |

---

## 6c. ★ 灯具 / 光源（第 8 个实质主题类别，此前被低估）

**结论更正**：此前把"灯具"当零散点缀。实际 **CC0 就有 6 项**，且 `torch`（56 条 / 3 页）与 `lamp`（40 条 / 2 页）是 OGA 上**信噪比最高的两个关键词**。**灯具是"可维护以太装置"最容易落地的一层。**

| 素材包 | 许可 | 尺寸 | 内容 |
| --- | --- | --- | --- |
| ★ [CC0 Light Icons](https://opengameart.org/content/cc0-light-icons) | **CC0** | **32×32 主套 + 24×24 + 48×48** | 火把/蜡烛/手电/灯泡等光源图标合集（OpenClipart / DCSS / Jetrel / thekingphoenix）——**24/32/48 三档全落在目标尺寸表内** |
| ★ [Lantern 32px spritesheet](https://opengameart.org/content/lantern-32px-spritesheet) | CC-BY 3.0 | **32px** | 提灯精灵表；可改造为可维护以太光源装置。符文/特效轨道称之为"全局最佳命中" |
| ★ [Lamps Lights n Torches](https://opengameart.org/content/lamps-lights-n-torches) | **CC0** | — | 作者明确分 **mounted（壁挂）** 与 **portable（便携）** 两类——正对应学院"壁挂管道灯 + 手持灯"两套需求 |
| ★ [candles, oil lamp and eye with varied stands](https://opengameart.org/content/candles-oil-lamp-and-eye-with-varied-stands) | **CC0** | — | **8 种底座 × 8 种顶部 = 64 种变体**——"灯座与灯头可自由组合"的结构本身就是**可维护装置的视觉语言** |
| ★ [[LPC] Animated Torch](https://opengameart.org/content/lpc-animated-torch) | CC-BY 3.0/4.0 + GPL + **OGA-BY 3.0** | **32×32，9 帧火焰动画** | 火焰帧可直接改造为"以太能量辉光"循环 |
| ★ [[LPC] Lamp Posts Rework](https://opengameart.org/content/lpc-lamp-posts-rework) | CC-BY 3.0 + GPL + **OGA-BY 3.0**（**作者自改部分声明按 CC0**） | **为 32×32 平铺地图设计**，含 `.xcf` | 小号灯、U 形横杆、双灯变体 + Sharm 原版灯柱 → 直击"石材 + 锻铁 + 可维护灯" |
| [[LPC] Misc](https://opengameart.org/content/lpc-misc) | CC-BY 3.0 + GPL + **OGA-BY 3.0** | 32×32 | 石轮、石门/金属装饰板、花、**街灯** |
| ★ [Iron Torches](https://opengameart.org/content/iron-torches) | **CC0** | 附 **13.4 MB SVG 源**（可无损改尺寸）+ 铁门/石门模板 | **"锻铁"主题直击要点**；作者建议"远处看 + 顶部加火焰动画"，正对应以太灯加辉光 |
| [2D Platformer Side Scroller Stone Fence Street Lamp](https://opengameart.org/content/2d-platformer-side-scroller-stone-fence-street-lamp) | **CC0** | 石栅栏 **32×32**；灯柱 16×64 / 32×128 / 160×640 | 32×32 石墙正对"朴素石材" |
| [16x16 Torch](https://opengameart.org/content/16x16-torch) · [Torch 32px sprite](https://opengameart.org/content/torch-32px-sprite)（CC-BY 3.0，6 帧） | CC0 / CC-BY 3.0 | 16×16 / 32px | 动画火把 |
| [Wall Mounted Lamp](https://opengameart.org/content/wall-mounted-lamp) | CC-BY 3.0 | 1000×760 非像素，需降采样 | **含 On / On with glow / Off 三态**——极适合可开关的以太壁灯 |
| ⚠ [[LPC] Castle Lights Repack](https://opengameart.org/content/lpc-castle-lights-repack) | **CC-BY-SA 3.0** + GPL | 明确重排为 **32×32 tile** | 内容最省事，但传染性排除 |

## 6d. ★ 符文 / 以太回路（补齐此前最薄的一环）

| 素材包 | 许可 | 尺寸 | 内容 |
| --- | --- | --- | --- |
| ★★ [Idylwild's Runic Codex](https://opengameart.org/content/idylwilds-runic-codex) | **CC0** | 16×16 | **672 枚 = 224 符文 × 2 风格 × 3 配色**，附参考总表 / spritesheet / `.ase` —— 数量极大的 16×16 符文库 |
| ★ [Character Sheet Runic Alphabet](https://opengameart.org/content/character-sheet-runic-alphabet) | **CC0** | **单字符 64×64**（8×8 字符网格） | 古弗萨克 + 年轻弗萨克两套符文表，clean / rough / flat 三版 → 直接做 64×64 符文贴片/机关面板字符集 |
| ★ [CC0 Rune Icons](https://opengameart.org/content/cc0-rune-icons) | **CC0** | **32×32** + scalable | 取自 OpenClipart Futhark + DCSS |
| ★ [4 summoning circles](https://opengameart.org/content/4-summoning-circles) | **CC0** | **矢量 `.ai`/`.svg` 可无限缩放** | 4 个召唤阵，标签含 **Top-down / Strategy** → **矢量可精确输出任意 32 倍数尺寸** |
| [Encircled Runes](https://opengameart.org/content/encircled-runes) | **CC0** | **83×83，34 张** | 圆环包裹符文、粗青底可换色，作者称"旋转时好看" |
| [Rune Pack](https://opengameart.org/content/rune-pack) | **CC0**（Kenney） | 35 块符文石 × 6 风格 × 3 色 | 含描边/无描边版；640px 源需降采样 |
| [Tiny Magic Glyphs](https://opengameart.org/content/tiny-magic-glyphs) | **OGA-BY 3.0 + CC0 双列** | 5×5 单体 | 官方定位"用于 16×16–32×32 角色上的附魔标记/粒子" |
| [teleport circle sprite sheet](https://opengameart.org/content/teleport-circle-sprite-sheet) · [fire trap rune animation](https://opengameart.org/content/fire-trap-rune-animation-sprite-sheet) | **CC0** | — | 传送法阵逐帧；符文 + 地面陷阱 → 可作**回路过载/危险区** |
| [Stone Runes](https://opengameart.org/content/stone-runes) | CC-BY 3.0 + OGA-BY 3.0 | 256×256，16 张 | 12 黄道 + 3 元素 + 1 空白；石质符文牌 → 可作**可维护装置插槽** |

## 6e. ★ 能量辉光 / 特效（"以太"视觉语言）

| 素材包 | 许可 | 尺寸 | 内容 |
| --- | --- | --- | --- |
| ★★ [glow circle](https://opengameart.org/content/glow-circle) | **CC0** | 单个发光圆/球体，可调色 | **可直接作以太灯、仪表辉光、设备运行光效的叠加层** |
| ★★ [2D Spell Effects](https://opengameart.org/content/2d-spell-effects) | **CC0** | 10 组透明 PNG 序列 + 11 个 gif | 闪电球、蓝色顶光、剑火、火球、火焰印记、**能量球**、**`fx9_rainOnGround`（雨落地）**、黑色爆炸 —— 同一次拿到"雨打地面"落点特效与能量球 |
| ★ [Seamless Lava Animated Tile](https://opengameart.org/content/seamless-lava-animated-tile) | CC-BY 3.0 + GPL 2.0 | **32×32，5 帧**，含无缝平铺版 | **改色后可直接作"以太管道内流动的发光能量 / 炉膛"** —— 发光流体最现成的一项 |
| [UI Orbs](https://opengameart.org/content/ui-orbs) | **CC0** | — | 可任意着色的发光圆球 → HUD 以太储量球 |
| [Magic spell icons](https://opengameart.org/content/magic-spell-icons) | **CC0** | 矢量 `.svg` + PNG 导出 | 6 个魔法图标 + 背景/特效变体 |
| [Arcane Magic Effect](https://opengameart.org/content/arcane-magic-effect) · [Animated fireball / candle light](https://opengameart.org/content/animated-fireball-candle-light) · [Simple spell effect](https://opengameart.org/content/simple-spell-effect) · [Cinema projector light](https://opengameart.org/content/cinema-projector-light) | **CC0** | — | 奥术弹道 / 灯焰叠加帧 / 极小像素序列 / 辉光叠加层 |

## 6f. 雨天 / 水面 / 气候（补齐）

| 素材包 | 许可 | 尺寸 | 内容 |
| --- | --- | --- | --- |
| ★★ [Rain Animation](https://opengameart.org/content/rain-animation)（Stendhal/AntumDeluge） | CC-BY 4.0/3.0 + **OGA-BY 3.0** | **64×64 正交；3 个变体（light/medium/heavy），各 16 帧** | 标签明确含 rain / rainy / weather / water / **wet** / climate —— **三个降雨强度档 + 64×64 正交 + 显式 "wet"，本轮最贴合"雨后学院维护"氛围的一套** |
| ★ [Puddle Corpses](https://opengameart.org/content/puddle-corpses) | CC-BY 3.0 + **OGA-BY 3.0** | **32×32 与 64×64**（索引色），5 个文件 | 5 种颜色水洼贴片（含 2×2 大块）—— **`puddle` 关键词唯一真正可用的正俯视水洼贴片**，与 32 PPU 完全对齐 |
| ★ [Swapshot Rain animations](https://opengameart.org/content/swapshot-rain-animations) | **CC0** | 普通雨 + **酸雨(acid rain)** | 酸雨变体可改作**以太泄漏/腐蚀性降雨** |
| [32x32px static water tileset](https://opengameart.org/content/32x32px-static-water-tileset) | CC-BY 3.0 | **32×32，4 色** | 积水/排水渠水面底图 |
| [Animated Ocean Water Tile](https://opengameart.org/content/animated-ocean-water-tile) | **CC0** | **32×32** | 微流动水面 |
| ★ [Backgrounds & Effects sprite pack](https://opengameart.org/content/backgrounds-effects-sprite-pack) | **CC0** 无署名要求 | 13 张 | 标签含 rain / weather / smoke / **steam** —— **含 steam，正好服务"以太管道/阀门泄压"视觉** |
| [Weather Icons](https://opengameart.org/content/weather-icons) | **CC0** | 100×100（作者注明项目内缩到约 30×30） | 8 个天气组合图标 + PSD |
| [Rain Particle Animated](https://opengameart.org/content/rain-particle-animated) | **CC0** | **10×10，5 帧** | 尺寸过小不能当地表，适合作引擎粒子发射器的雨滴 sprite |
| [Fog Animation](https://opengameart.org/content/fog-animation) | **CC0** | 640×480；40 帧 @150ms，可平铺 | "雨后薄雾"氛围层，尺寸偏大需降采样 |
| ⚠ [Ice patch effect](https://opengameart.org/content/ice-patch-effect) · [water splash](https://opengameart.org/content/water-splash) | CC-BY-SA 3.0 | — | 传染性排除 |

## 6g. OGA 关键词命中数硬证据（可引用为"确认性缺口"）

| 关键词 | OGA 2D Art 原始命中 | 可用 | 结论 |
| --- | --- | --- | --- |
| **ether** | **0** | 0 | **完全盲区** |
| **wrought iron** | **0** | 0 | **完全盲区**——锻铁栏杆/格栅只能从 LPC 钢栏杆、31 套墙包(`iron0-0`)、Metal Blocks 拼 |
| **reflection** | 7 | **0** | **确认性缺口**——OGA 没有正俯视的"湿地反光/倒影"像素贴片 |
| **valve** | 3 | 0 | 3 条全无关 → **"管道+阀门+仪表"三角里最缺的一角** |
| **sigil** | 2 | 0 | 且 OGA **没有 `sigil` 这个 tag**（`magic circle` tag 同样不存在） |
| **academy** | 4 | 0 | 4 项全无关 → **确认零可用** |
| **runic** | 4 | 0 | 全是符文字母表/字体，**不含任何地块或道具** |
| **puddle** | 3 | 1 | 唯一可用 Puddle Corpses |
| **weather effects** | 3 | 3 | Weather Icons / Backgrounds & Effects / Rain Animation |
| **gauge** | 5 | 1 | 仅 Gas gauge 是真仪表 |
| **brass** | 14 | 2 | Cogwheels and Gears、Observatory Tileset |
| **workshop tileset** | 1 | 1 | 仅 [LPC Revised] Workshop Tilesets |

**CJK 检索三个全部归零（含技术原因）**：`keys=魔法` **被站点直接拒绝**（"You must include at least one keyword with 3 characters or more"，OGA 按**字符数**校验，2 个汉字判定长度不足 —— **技术性不可检索，非内容不存在**）；`魔法学院` 通过长度校验但 **0 结果**（真实索引空集）；`タイルセット` 0 结果（**OGA 基本未索引日文标题**）。

**信噪比**：`lamp`（40/2 页）与 `torch`（56/3 页）最高；反之 `tileset`（2,291 条）、`water`（506 条/21 页）、`light`（288 条/12 页，多为 "light weapon/light magic" 歧义）**无收敛价值，必须叠加材质词**。

> ⚠ **两条接入警告**：① **不要按标题里的 "16 Bit" 或所属合集名推断尺寸**——大量页面不写像素尺寸，接入前必须**下载实测**；② [[LPC] City outside](https://opengameart.org/content/lpc-city-outside) 的路灯因源自 RPG Maker RTP **已被维护者移除并替换**（2018），下载须确认取最新 zip。

---

## 7. 大批量来源（"找得越多越好"的主引擎）

### ★ OpenGameArt 全站镜像（体量最大的一条）
- 数据集集：**https://huggingface.co/collections/nyuuzyou/opengameart-680b5aa2e8ab3ed893af03f0**
- 按许可分拆：`OpenGameArt-CC0`、`OpenGameArt-CC-BY-3.0`、`-CC-BY-4.0`、`-OGA-BY-3.0`、`-OGA-BY-4.0`、`-CC-BY-SA-3.0`、`OpenGameArt-Mixed-Licenses`
- 每条附 JSON/Excel 署名字段：`url / title / author / author_url / post_date / art_type / tags / licenses / collections / files`——**使许可逐条可核**
- OGA 站方论坛确认帖：https://opengameart.org/comment/110139
- 抓取：`web_fetch` 不通，**用 PowerShell `Invoke-WebRequest` 可通**（HF 返回 200）

**OGA 站内 2D Art 各许可条目数（本轮从高级搜索表单实测）**：

| 许可 | tid | 2D Art 条数 | OCC 可用 |
| --- | --- | --- | --- |
| **CC0** | **4** | **8,067** | ✅ 首选 |
| CC-BY 3.0 | 2 | 4,142 | ✅ 需署名 |
| CC-BY 4.0 | 17981 | 1,911 | ✅ 需署名 |
| OGA-BY 3.0 | 10310 | 1,171 | ✅ |
| OGA-BY 4.0 | 31772 | 99 | ✅ |
| CC-BY-SA 3.0 | 3 | 2,134 | ❌ 传染性 |
| CC-BY-SA 4.0 | 17982 | 742 | ❌ 传染性 |
| GPL 3.0 | 6 | 1,184 | ❌ |

> ⚠ **修正一个常见错误**：网上与直觉常把 `field_art_licenses_tid[]=17981` 当成 CC0，实测它是 **CC-BY 4.0**；**CC0 是 `4`**。用错会白翻几十页。

**可复用搜索 URL**（翻页加 `&page=N`）：
```
https://opengameart.org/art-search-advanced?keys=<关键词>&field_art_type_tid%5B%5D=9&field_art_licenses_tid%5B%5D=4&sort_by=count&sort_order=DESC
```

### ★★ Dungeon Crawl Stone Soup 32×32 瓦片集（CC0，实测尺寸，体量最大的一份 32×32）
本轮**实测下载 PNG 并解析 IHDR** 拿到真实尺寸，不是估计：

| 素材包 | 许可 | 实测 | 瓦片数（32×32） |
| --- | --- | --- | --- |
| [Dungeon Crawl 32x32 tiles](https://opengameart.org/content/dungeon-crawl-32x32-tiles) | **CC0** | `ProjectUtumno_full.png` = **2048×3040 px**（4.27 MB） | 64×95 = **6,080** |
| [Dungeon Crawl 32x32 tiles supplemental](https://opengameart.org/content/dungeon-crawl-32x32-tiles-supplemental) | **CC0** | `ProjectUtumno_supplemental.png` = **2048×1536 px** | 64×48 = **3,072** |
| | | | **合计 ≈ 9,152** |

tags：`overhead / 3/4 view / orthogonal / dungeon crawl / 32x32 / roguelike / town / weapon / armor / magic / item / monster / gui`
匹配度：**高**——正交俯视 + 32×32 + 地牢/城镇/物品/武器/护甲/魔法/怪物/GUI，正是本作战场与图标所需。唯一代价是 3/4 视角混排、画师拼盘风格不统一，**需人工逐块筛选**。

### ★ Kenney 全量（脚本逐页抓字段，非摘要）
**2D / Pixel / UI / Tiny 相关共 54 个包、合计 18,810 个文件，全部 CC0 1.0**（每页 License 字段原文均为 "Creative Commons CC0"）。

尺寸命中目标网格的有 30 个包：16×16（Tiny 系列、1-Bit Pack、Roguelike/RPG pack、RPG Urban Pack、Cursor Pixel Pack、Input Prompts Pixel…）、8×8（Pico-8 系列、Micro Roguelike、Minimap Pack）、18×18（Pixel Platformer 系列）、21×21、64×64（Input Prompts、Crosshair Pack）。

体量最大的几包：**Roguelike/RPG pack 1700 文件**（16×16，rpg/tile/town/furniture/button/panel）、Input Prompts 1500（64×64）、1-Bit Pack 1078（16×16）、Roguelike Modern City 1036、Platformer Art Pixel 900（21×21）、Mobile Controls 900、Input Prompts Pixel / Pixel 1-Bit 各 800、Pixel UI Pack 750、Roguelike Caves & Dungeons 520、Roguelike Indoors 480、RPG Urban Pack 480、Roguelike Characters 450、UI Pack 430。

入口：`https://kenney.nl/assets/tag:pixel`（3 页）、`https://kenney.nl/assets/series:Tiny`、`https://kenney.nl/assets/tag:interface`

⚠ **Kenney Fonts 只有 11 个纯拉丁字体，无中文**——中文字体走 §6。
付费聚合包 `https://kenney.itch.io/kenney-game-assets` = $19.95、60,000+ 资产、注明 "unlimited commercial projects (no attribution required)"；免费逐包下载已足够，此包只是省事。

### ★ LPC（Liberated Pixel Cup）——许可有重大风险，必须按文件分流
- 基础包 [LPC Base Assets](https://opengameart.org/content/liberated-pixel-cup-lpc-base-assets-sprites-map-tiles)：License 字段 = **CC-BY-SA 3.0 + GPL 3.0**，即**传染性 share-alike**，对闭源商业项目有实际风险。
- **例外**：页面明确写 Leana "Sharm" Zimmerman 与 Stephen "Redshrike" Challener 的资产**同时可用 OGA-BY 3.0**；bluecarrot16 的评论进一步确认这两位部分资产可改用 CC-BY。**只有这两位作者的部分可安全用。**
- Universal LPC Spritesheet Generator 已迁移：`https://liberatedpixelcup.github.io/Universal-LPC-Spritesheet-Character-Generator/`（实测 200）；旧地址 `sanderfrenken.github.io` 超时。仓库 `https://github.com/LiberatedPixelCup/Universal-LPC-Spritesheet-Character-Generator`，**LICENSE = GPL-3.0，体积 1.5 GB**。
- **关键量化**：仓库 `CREDITS.csv` 共 **13,917 条**素材记录，逐行许可统计（一行可多许可）：

  | 许可 | 行数 | OCC 可用 |
  | --- | --- | --- |
  | **OGA-BY 3.0** | **10,503** | ✅ 署名即可，可商用闭源 |
  | GPL 3.0 | 7,720 | ❌ |
  | CC-BY-SA 3.0 | 5,382 | ❌ 传染 |
  | CC-BY 3.0 | 3,084 | ✅ 需署名 |
  | CC0 | 1,011 | ✅ |
  | CC-BY 4.0 | 487 | ✅ 需署名 |

- **结论：LPC 生态不是 CC0。主体是 OGA-BY 3.0（安全）+ CC-BY-SA 3.0（需规避）。必须按 `CREDITS.csv` 逐文件筛许可，不能整包引入。**

### 其他已证伪的"知名免费包"（避免踩坑）
| 素材包 | 实测结论 |
| --- | --- |
| Mystic Woods（免费版） | **禁商用**，且被叠了噪点水印 |
| Sprout Lands（免费版） | **禁商用**（与 §8 我这边独立核实一致） |
| Tiny Swords | **只有旧版是 CC0**，当前免费包不可按 CC0 处理 |
| Ninja Adventure | **CC0 已证实**，89 MB（与 §8 一致） |

### 其他批量来源
| 来源 | 许可 | 说明 |
| --- | --- | --- |
| [CraftPix 免费区](https://craftpix.net/freebies/) | 免费件可商用、**免署名**、可随游戏销售；禁转售源文件；**禁 AI 训练**（条款 3.1.1） | **301 页**免费资源。许可原文：https://craftpix.net/file-licenses/ 第 2 节。⚠ 当期索引里未筛出符合 OCC 投影+像素语言的包，建议按 `tilesets` / `2d-game-objects` 细分类再扫 |
| [Pixnote Pixels](https://pixnote.net/pixels/genre/tile/) | **CC0**，无需署名 | 单张无缝瓦片：石瓦/木瓦/砖瓦/水瓦/土瓦/道路瓦 |
| [DOT ILLUST](https://dot-illust.net/tag/gauge/) | ⚠ **非标准开源**：免费商用但**每个作品上限 30 件素材**，禁再分发/NFT/模板化（https://dot-illust.net/terms/） | 仪表 19 件、实验器皿 16 件——主题极贴"可维护以太装置"，但 30 件上限是硬约束，只能点缀 |
| [Cainos – Pixel Art Top Down Basic](https://cainos.itch.io/pixel-art-top-down-basic) | **免费商用、免署名、禁再分发**（itch 页面 LICENCE 字段，已核实）；Unity Asset Store 版为 Standard Unity EULA | 32×32 正交俯视废墟 |
| [LimeZu Modern Interiors](https://limezu.itch.io/moderninteriors) | **完整版（付费 ≥$1.50）**：可商用/非商用、需署名（附链接）；免费版条款未取到，按同类作者惯例**大概率仅非商用，须人工确认** | 像素室内标杆 |
| [Kenney 全站](https://kenney.nl/assets?q=pixel) | **全部 CC0** | 14 页 |

---

## 8. itch.io 通道专项成果（本轮 PowerShell 解锁，agents 未能核实）

这是本轮独有的增量：agents 全部报告 itch.io 不可达，实际可用 PowerShell 读到许可字段。

### ✅ 已核实可用
| 素材包 | 许可（页面原文要点） |
| --- | --- |
| [0x72 DungeonTileset II](https://0x72.itch.io/dungeontileset-ii) | **Asset license: Creative Commons Zero v1.0 Universal** — CC0，16×16 地牢瓦片 + 角色，业界标杆 |
| [0x72 16×16 Dungeon Tileset](https://0x72.itch.io/16x16-dungeon-tileset) | **Creative Commons Zero v1.0 Universal** — CC0 |
| [Pixel-boy Ninja Adventure Asset Pack](https://pixel-boy.itch.io/ninja-adventure-asset-pack) | **Creative Commons Zero (CC0)**；页面明写 "even commercial ones. Attribution is not required"；89 MB，含 UI |
| [ansimuz Magic Cliffs Environment](https://ansimuz.itch.io/magic-cliffs-environment) | CC0 ——⚠ 侧视平台跳跃投影，与 OCC 斜俯视不同轴，**仅参考** |
| [ansimuz SunnyLand Forest](https://ansimuz.itch.io/sunnyland-forest) | CC0 —— 同为侧视，仅参考 |
| [ansimuz GothicVania Town](https://ansimuz.itch.io/gothicvania-town) | CC0 —— 同上 |
| [Szadi Art Rogue Fantasy Catacombs](https://szadiart.itch.io/rogue-fantasy-catacombs) | **"Public domain and free to use, personal or commercial. Credit is not required but appreciated. You can edit, but not resell."** |
| [Szadi Art Rogue Fantasy Castle](https://szadiart.itch.io/rogue-fantasy-castle) | 同上（同一许可声明） |
| [Cainos Pixel Art Top Down Basic](https://cainos.itch.io/pixel-art-top-down-basic) | "can be used in both free and commercial projects. Credit is not needed. You may not redistribute it or resell it." |

### ❌ 已核实不可用（免费版仅限非商用——容易踩的坑）
| 素材包 | 页面原文 |
| --- | --- |
| [Cup Nooble Sprout Lands](https://cupnooble.itch.io/sprout-lands-asset-pack) | "**Free version license**: This asset pack can be used in any **non-commercial** project… can't be used in any commercial project"；付费 ≥$3.99 才可商用 |
| [Kenmi Cute Fantasy RPG](https://kenmi-art.itch.io/cute-fantasy-rpg) | "**Free Version License**: …used in any **non-commerical** project"；付费 ≥$3.99 才可商用 |

> 这两个是像素圈里口碑很好的包，**免费版不能商用**。如果之前有人推荐过它们，要按付费版处理。

### 尚未核实（需继续跑 itch 通道）
0x72 其余包、Szadi 其余包、LimeZu Modern Exteriors / Modern Office、pimen、PiiiXL、Seliel (Mana Seed)、GrafxKid 的 itch 侧、Sithjester、Kenney 的 itch 镜像。

---

## 8b. 来源可信度与方法学警告（后续继续挖之前必读）

1. **Lospec 没有授权条款。** ToS 是通用 Shopify 样板、无调色板/美术条款；`/legal` 404；调色板页无 License 字段；投稿规则只讲策展、无 CC0 选项；17,883 件 Gallery 无逐件许可元数据。"Lospec 调色板随便用"是**社区假设，没有书面授权**。
   → **不要在任何 manifest 里把 Lospec 当作 OCC 20–24 色调色板的许可来源。** 要么自建色阶，要么从 CC0 包派生（例：Pixel Character 01 用的 archererer24，其宿主 OGA 页是 CC0）。

2. **OGA 的许可是上传者自述的**，站方论坛自己就有 "licensing paradox" 与 "Licenses Contradictory in different websites" 两个讨论帖。实测撞到的具体案例：`Platformer Armor` 页面标 CC-BY 3.0，作者在评论区改口 "released under CC-BY-SA 3.0 (or later)" → **按最严口径已排除**。
   → 每条都要**打开明细页 + 读评论区**，再把结论快照进 manifest。另有 `classic-rpg-tileset` 站内标 CC-BY/CC-BY-SA、压缩包内 text 自称 CC0 —— **以站内元数据为准**。

3. **`sort_by=count` 分页不稳定**：跨页会重复（实测第 8 页重复第 7 页 6 项、第 17 页重复第 16 页 19 项、第 21/24 页大量重叠）。**必须按 slug 去重**，否则会白翻。

4. **列表不显示像素尺寸**，标题里常不写（LPC 系实际 32×32/64×64 但标题不标）。**尺寸必须下载确认。**

5. **等距 ≠ 正交。** OGA 大量 tile 集是等距菱形，而且 `orthogonal` 与 `isometric` 标签会同时出现在同一素材上（如 Dark Mountain Tileset）。**入库前必须看图**，不能信标签。

6. **多许可条目要挑宽松项。** 同一条目常并列 6 种许可，例如 `[LPC] Interior Castle Tiles` = CC-BY 4.0 + 3.0 + CC-BY-SA 4.0 + 3.0 + GPL 3.0 + OGA-BY 3.0 —— 可取 CC-BY 分支。反之 `LPC Tile Atlas` 是 CC-BY-SA 3.0 + GPL 3.0，**无宽松选项**。

7. **本轮没有任何人打开过 PNG。** 五条硬性淘汰项（硬 Alpha / 禁抗锯齿 / 禁渐变 / 禁抖色 / 粗颗粒）对**每一个候选都未经验证**——所有"匹配度"判断来自页面文字、标签与作者自述，属**推断**。任何候选进 manifest 前必须过一遍 1× 人眼。

---

## 9. 明确排除名单（附原因，避免重复踩坑）

### 许可不合格
| 素材包 | 原因 |
| --- | --- |
| Zpix 最像素 | 商用 USD $1000 / RMB 7000，且明确禁止修改、反编译、转换、拆分 |
| 文泉驿点阵宋体 | GPL v2；官方页原文"在非 GPL 形式的单一性软件中使用属违反授权"，企业闭源产品须另行洽谈 |
| [Golden UI - Bigger Than Ever edition](https://opengameart.org/content/golden-ui-bigger-than-ever-edition) | **CC-BY-SA 3.0**——最可惜的一条：它是 Golden UI 的高清版，风格最贴 OCC，被 SA 挡掉 |
| [[LPC] Interface Items by Tempest in the Aether](https://opengameart.org/content/lpc-interface-items-contribution-by-tempest-in-the-aether) | **CC-BY-SA 3.0**——内含 **brass theme 黄铜面板 + 铁手套光标 + 2151 文件**，极贴"近代魔法工业/黄铜"，被 SA 挡掉 |
| [LPC] Wooden Furniture / Interior Tileset 16x16 (Bonsaiheldin) / Top-Down: Basic Furniture (Omnihunter, Tatermand) / Overworld Objects (Kelvin Shadewing) | CC-BY-SA / GPL，传染性 |
| Roguelike/RPG Icons (Joe Williamson)、Variety of cursors (Kemono) | CC-BY-SA 3.0 |
| Cup Nooble Sprout Lands / Kenmi Cute Fantasy RPG（免费版） | 免费版仅非商用（见 §8） |
| 丁卯点阵体 DinkieBitmap、全小素 QuanPixel | 只有下载站二手"免费商用"说法，**找不到作者原始许可原文** |
| Unity Asset Store 素材（含 Cainos 的 UAS 版） | Standard Unity EULA，非 CC 系；且禁 AI 训练，不能与 OCC 的 CC0 管线混装 |

### 投影不符（侧视平台跳跃，与 OCC 斜俯视网格不同轴）
[Magic Cliffs Environment](https://opengameart.org/content/magic-cliffs-environment)（ansimuz，CC0）、[4-Color Dungeon Bricks Extended](https://opengameart.org/content/4-color-dungeon-bricks-extended)（MoikMellah，CC0）——后者只有 4 色、像素语言极贴，可惜是 side-scroll。

### 像素语言违反契约
| 素材包 | 原因 |
| --- | --- |
| [Simple Orange Pixel Art UI](https://opengameart.org/content/simple-orange-pixel-art-ui) | 作者自述用了噪点/抖色 → 违反"无抗锯齿/渐变/抖色" |
| Kenney [Pixel UI Pack](https://kenney.nl/assets/pixel-ui-pack)、[UI Pack - Pixel Adventure](https://kenney.nl/assets/ui-pack-pixel-adventure) | 前者正是契约禁止的"厚框灰底白边"挤压型；后者高饱和卡通 |
| Dungeon Crawl 32×32 中的密集裂纹/苔痕/铆钉块 | 直接命中淘汰项（密集裂纹、苔痕、孤立单像素散点、高频抖色） |
| [Fantasy UI Elements](https://opengameart.org/content/fantasy-ui-elements-by-ravenmore) / [Fantasy Icon Pack](https://opengameart.org/content/fantasy-icon-pack-by-ravenmore-0) | 手绘非像素、为暗背景设计 |
| [700+ RPG Icons](https://opengameart.org/content/700-rpg-icons)（Lorc） | 789 个黑白**非像素**线条画 |
| [Furniture Kit](https://opengameart.org/content/furniture-kit)（Kenney） | 3D 低模 + 3D 渲染俯视图，非原生像素 |

### 不建议来源（许可不明 / 二次转载 / AI 生成provenance不清）
- **summerengine.com** — 无代码 AI 游戏平台的附带 asset store，来源与许可不明，搜索结果里高频出现，**不要用**
- **freepixel.art** — 页面标题称 "Free Pixel Art for Commercial Use, No Attribution"，但正文 JS 渲染读不到条款全文，且以"weekly drops"形式发布、疑为 AI 生成，provenance 不清 → 需人工核 https://freepixel.art/license
- Godot Asset Store — 实测搜索 "pixel art 2d" 只返回 3 项且全是 shader/插件，**不是美术素材库**，此路不通
- Lospec — 只有调色板数据库，无瓦片资产；价值在于提供 480×270 ≤24 色的调色约束（DB16/DB32 正是 Golden UI 采用的暖棕金体系）

---

## 10. 调研盲区（这些方向确实没有免费合规素材）

1. **"魔法学院 / 图书馆"专用像素 tileset 极稀缺**——OGA 上 `pixel academy` 关键词 **0 条**；能用的只有泛用 Roguelike 室内包，没有真正意义的"学院"包。
2. **"可维护以太装置（管道 + 阀门 + 仪表 + 符文回路）"没有成套免费素材**——最贴的只有零散件：DOT ILLUST 的仪表/实验器皿（有 30 件上限）、Kenney Tiny Factory、pH64 的电器机械。**这个题材空白基本可确认，需自制。**
3. **精确模块尺寸无人发布**——`32×40` 外沿地块、`32×8` 前立面、`32×48`"格+上探"这类 OCC 契约尺寸，除 Underworld Load 提供 16×24/16×16 子模块外没有第二家；只能在 32×32 墙块基础上自绘。
4. **32px 正交俯视的悬崖/高差模块为零**——唯一命中是 16×16 的 Tiny Zelder cliff。
5. **湿地面 / 积水反射瓦片集不存在**——只有雨动画图层，没有专门的湿地砖/反射瓦片。
6. **"机械犬 / 四足机械"不存在任何现成解**——所有可达来源都没有四足机械狗素材，也没有蜘蛛机，也没有任何"时钟轮/秘法机械"敌人包（OGA `clockwork` 只返回 2 条且都不是敌人）。**寻迹兽必须完全手搓**；可借 Benalene 的 50×25 犬类跑动做四足占地与比例参考、借 Fox 包的 6/8/5/4 状态拆分做动画表版式，然后整体重绘成锻铁 + 以太。
7. **学院 / 学者 / 守卫 / 维护员阵容在免费素材里近乎空白**——最接近的三个：CraftPix Tiny Schoolgirl（学院制服，但有"soft palette"风险）、Agent Character（CC0 西装特工）、Gun Girl + Riflemen（16px 俯视且带 share-alike 标签）。**OCC 的核心人类阵容没有可用的现成来源。**
8. **战棋单位集稀缺**——唯一明确对口的免费战棋单位是 Toen's Medieval Strategy Sprite Pack（16×16，CC-BY 3.0）。
9. **游戏状态类 16×16 图标的四档语义色族严重不足**——Buch 的 8 个状态图标是唯一语义命中且许可合格的一套；冷青/封存红/氧化黄铜/医务绿四档完整图标族需自绘。
10. **没有任何免费包同时提供"暖纸 + 旧木 + 锻铁 + 冷青"四档语义色**——不存在开箱即用方案，所有候选都必须经自建调色板改色。
11. **所有素材只核了许可页与文字规格，没有目视像素**——本轮未下载预览图（唯一例外是 DCSS 的 IHDR 尺寸实测），"色调/匹配度"判断来自作者自述与页面文字，属推断。**最高风格失败风险的几个按序为**：CraftPix City Enemies（真实颗粒度）、CraftPix Factory Boss（`Vector: Yes`）、Kenney New Platformer Pack（`oopi` 圆润风）、Superpowers/Pixel-Boy。
12. **两个决定性未知量**：`2D Soldier Guy Character` 与 UnitForge chibi 部件的**原生像素高度**，两页都没标。这决定候选是"重新排版"还是"从零重绘"——也就是决定 Top 3 是否成立。
13. **Fusion Pixel 各尺寸的"缺失汉字清单"拿不到**——仓库只给统计（8px 缺 168、10px 缺 1031、12px 缺 176 个字符，其中汉字 145）。要确认 OCC 文案是否踩缺字，必须拿到字体后跑覆盖率脚本。
14. **itch.io 只核了一小批**——见 §8 末尾的待核列表。Szadi art 由我这边核实为公有领域；但 Mana Seed/Seliel、piiixl、Bevouliin、RobertBrooks、penzilla、aamatniekss、jesse-m 仍**零核实**。
15. **LPC 的 1,011 行 CC0 + 10,503 行 OGA-BY 3.0 尚未展开**——这是还没挖的大矿，但必须按 `CREDITS.csv` 逐文件筛。

---

## 11. ★★ 批量获取（不限题材，按像素规格收；"有多少来多少"）

> 本节不考虑题材匹配，只看**像素规格**与**许可**。目标是把合规素材**尽可能多地拿到本地**。
> 已实测的尺寸/数量标注为「**实测**」，其余为来源声明。

### Tier 1 — 一次拿到上万个资产（全部 CC0，免署名）

| 来源 | 规模 | 许可 | 获取方式 |
| --- | --- | --- | --- |
| ★★★ [**Tiddybub/2d-assets**](https://github.com/Tiddybub/2d-assets) | **1,101 个包 / 约 1.09 GB** | **100% CC0**（README 原文："Every asset here is CC0 … free for commercial use, no attribution required, no license tracking needed. Nothing in this repo has usage restrictions."） | `git clone --depth 1 https://github.com/Tiddybub/2d-assets.git` |
| ★★★ **Kenney.nl 全站像素包** | **54 包 / 18,810 文件** | **100% CC0** | 逐包下载，或 `https://kenney.itch.io/kenney-game-assets`（$19.95 / 60,000+ 资产，省事） |
| ★★★ [**Dungeon Crawl Stone Soup 32×32**](https://opengameart.org/content/dungeon-crawl-32x32-tiles) + [补充包](https://opengameart.org/content/dungeon-crawl-32x32-tiles-supplemental) | **实测 6,029 个 32×32 PNG**（图集 2048×3040 + 2048×1536） | **CC0** | OGA 页面的 zip |
| ★★ [**DENZI's public domain art**](https://opengameart.org/content/denzis-public-domain-art) | **实测 1,361 PNG = 1,248×32×32 + 113×32×48**（另有 paperdoll 纸娃娃 182） | **CC0**（zip 内 `LICENSE.TXT` 逐字 CC0） | `DENZI_CC0_individual_organized_tiles_sprites.zip` |
| ★★ **OpenGameArt CC0 2D Art** | **8,067 条** | 混合，需筛 | 搜索 URL 见 §7（`tid=4`） |

**Tiddybub 的题材分布**（可按需只取子目录）：`sci-fi` 529 · `fantasy` 248 · `misc` 96 · `ui` 93 · `tiles-terrain` 34 · `characters` 29 · `effects` 21 · `nature` 19 · `modern-urban` 18 · `vehicles` 14。
> ⚠ **注意**：俯视向内容主要在 `fantasy/` 与 `ui/`，**不在 `tiles-terrain/`**（那个是平台跳跃/等距/六边形居多）。每包内附 `SOURCE.md` 记录作者、许可与原页面 —— **可直接用于 manifest 溯源**。

### Tier 2 — 大体量 CC0 仓库（GitHub）

| 仓库 | 规模 | 许可 | 备注 |
| --- | --- | --- | --- |
| [Papyszoo/CC0-Public-Domain-Sprites](https://github.com/Papyszoo/CC0-Public-Domain-Sprites) | 172 MB | CC0-1.0 | |
| [SpriteCook/spritecook-free-game-assets](https://github.com/SpriteCook/spritecook-free-game-assets) | 44 MB | CC0-1.0 | |
| [iwenzhou/kenney](https://github.com/iwenzhou/kenney) | 21 MB | **CC0-1.0** | Kenney 镜像（要比逐包下载省事） |
| [doficia/project-cordon-sprites](https://github.com/doficia/project-cordon-sprites) | ★75 | CC0（`LICENSE.md` 全文） | |
| **rakkarage** `PixelLevel` / `PixelInterface` / `PixelFood` / `PixelItem` / `PixelMob` / `PixelEffect` | 多仓库 | **代码 MIT + 美术 CC0** | 分散但成套 |
| [crawl/tiles](https://github.com/crawl/tiles) | 7,396 PNG | CC0 | ⛔ **只用 `releases/` 或 OGA 的 zip** —— master 含约 1,230 个归属不清文件（见仓库内 `TILES_UNDER_UNKNOWN_LICENSE.md`） |
| ⛔ [AscensionGameDev/Intersect-Assets](https://github.com/AscensionGameDev/Intersect-Assets) | 124 MB | **混合，道具部分 CC-BY-SA 3.0** | **不可整包引入** |

### Tier 3 — 逐作者/逐站批量

| 来源 | 规模 | 许可 |
| --- | --- | --- |
| **itch.io 已核实作者** | **101 个包** | 见 §8（CC0 / 自定义 / 3 家禁商用） |
| — [**Szadi art**](https://szadiart.itch.io/) | 41 项中 **22 个免费** | "Public domain, free for commercial. Credit not required. You can edit, but not resell"（⚠ 法律上 PD 与 not-resell 矛盾，**非真正 CC0**） |
| — **Ansimuz** | **44 个免费包**（15 个 CC0） | CC0 / 自定义 |
| — [**0x72**](https://0x72.itch.io/) | 自有包**全部 CC0** | DungeonTileset II / DungeonUI / µFantasy / 16×16 Industrial Tileset… |
| — **Matt Walkden** | 3 包 | **CC0**（Robot Warfare tags 含 Tactical/Roguelike/Tileset） |
| — **Not Jam Font Pack** | **28 个 CC0 像素字体** | CC0 |
| — **Karsiori** | 齿轮/工业/Mechs/蒸汽工控件面板 | CC0 |
| — **GrafxKid** / **isaiah658** / **surt** / **Buch** / **Jetrel** | 多包 | CC0 |
| [**CraftPix 免费区**](https://craftpix.net/freebies/) | **实测 30 页 = 471 个去重免费项**；其 itch 账号 `free-game-assets.itch.io` 挂 **1,121 个免费包** | 可商用、免署名、禁再分发；**⛔ 禁 AI 训练** |
| [**store.godotengine.org**](https://store.godotengine.org/search/?query=%23tileset) | **62 条 tileset**（可按 CC0/MIT/Apache 筛） | 引擎无关 PNG，**Unity 可直接用** |
| **freegamesprites.com** | 声称 **20,054 sprites / 100+ 包** | ⛔ **排除** —— 浏览器端程序化生成的 256×256+ 贴图，**不是原生像素画** |

### 按像素规格的实测供给（这是"能不能用"的硬账）

| OCC 规格 | 已确认供给 | 结论 |
| --- | --- | --- |
| **32×32** | DCSS **6,029** + DENZI **1,248** + Kenney/Tiddybub 若干 | **充足** |
| **32×48** | DENZI **113** | **有供给**（此前"必须自绘"的判断已作废） |
| **16×16** | Kenney Tiny 系 + DCSS 补充 + 大量 OGA 包 | **充足** |
| **16×16 图标（≤4 色）** | RPG UI Icons **22 个** | 够用，但半数需降色 |
| **48×48** | 仅 `1-bit Resources and Gems 48x48`（CC0）+ CC0 Light Icons 1 个 | **稀缺** |
| **64×64** | Top Down Dungeon Pack **2,256 个**（含 Metal/Wood） | **充足** |
| **32×40** | **0** | **必须自绘** |
| **32×64**（单位画布） | **0**（页面标注层面） | **必须自绘** |

### 取用顺序建议（按"每单位工作量换到的资产数"排）

1. `git clone` **Tiddybub/2d-assets** → 一次拿 1,101 个 CC0 包
2. **Kenney 全站 54 包** → 18,810 文件，零许可负担
3. **DCSS + DENZI** 两个 zip → 7,390 个 32×32/32×48 PNG
4. **OGA `tid=4`** 按标签继续翻（`orthogonal` 已全部翻完；`topdown` 缺 185、`top-down` 缺 283、`stone` 缺 168）
5. **itch.io / CraftPix / Godot store** 逐作者补

---

### ★ 实测结果（2026-09-17，脚本已实际跑通）

暂存区 `E:\OCC_素材库`（**仓库之外**，不污染工程）。已自动拉取成功：

| 来源 | PNG 数 | 体积 | 许可 |
| --- | --- | --- | --- |
| **Tiddybub/2d-assets** | **50,637** | 1,494 MB | 100% CC0 |
| **Papyszoo/CC0-Public-Domain-Sprites** | **19,530**（含非 PNG） | 172 MB | CC0 |
| **DCSS 系列**（4 个 zip） | 18,113 | 12 MB | CC0 |
| **DENZI_individual + sheets** | 1,376 | 2 MB | CC0 |
| **doficia/project-cordon-sprites** | 211 | 1 MB | CC0 |
| **合计** | **≈ 89,655** | — | 全 CC0 |

**按 OCC 目标规格统计（全量 89,655 PNG，用 `inventory_by_spec.ps1` 实测）**：

| 规格 | 数量 | | 规格 | 数量 |
| --- | --- | --- | --- | --- |
| **32×32** | **23,996** | | 48×48 | **601** |
| **64×64** | **10,673** | | 24×24 | **487** |
| **16×16** | **10,471** | | 32×48 | **173** |
| 128×128 | 8,144 | | 32×64 | **39** |
| 8×8（6×→48×48） | 1,953 | | 16×24 | **16** |
| 18×18 / 21×21 | 1,082 / 913 | | 32×40 | **7** |
| 96×128 | 563 | | **命中合计** | **46,463** |

> 连同 32×64（39 个）与 32×40（7 个）都有货了 —— 此前"这两档无供给、必须自绘"的判断**已作废**。
> 另有 5,616 个 `0×0` 与 1,421 个 `0×96` —— 是扩展名为 `.png` 但头部不是有效 IHDR 的文件（模板/占位/损坏），挑素材时跳过。

**已知冗余**：DCSS 的 `Full` 包在原始页与补充页各出现一次，解压出两份 6,029 文件 → 去重可省约 6,000 个。

### 工具（同目录）

| 文件 | 用途 |
| --- | --- |
| `fetch_cc0_bulk.ps1` | 批量获取脚本。`-Target` 指定落盘、`-Only denzi,dcss` 选来源、`-IncludeClone` 加成批 git clone |
| `inventory_by_spec.ps1` | 按像素规格盘点：直接读 PNG 的 IHDR（不解码、不依赖图像库），输出各档命中数与 Top 20 尺寸分布，可选导出 CSV |
| `manual_download_worklist.md` | **需要你用浏览器手动下载的清单**（Kenney / itch.io / CraftPix / Godot / 字体），含准确 URL、许可、体积、优先级与落盘位置 |

> 脚本均为**纯 ASCII**：Windows PowerShell 5.1 按 GBK 读取无 BOM 的 UTF-8 文件，中文注释会把语法拆坏（已实测踩过）。

### ⚠ Kenney 脚本化下载不可行（实测）

**实测 2026-09-17**：`kenney.nl` 的**列表页与包页在纯 HTTP 抓取下都是 JS 渲染的空壳** —— `https://kenney.nl/assets/tag:pixel`、`https://kenney.nl/assets?q=pixel`、`https://kenney.nl/assets/tiny-factory` 三者的原始 HTML 里**既没有包 slug 也没有 `.zip` 链接**（长度仅 15–23 KB）。`web_fetch` 能看到内容是因为它做了渲染，但拿不到可枚举的 URL。

**替代路线（择一）**：
1. **GitHub CC0 镜像** `git clone --depth 1 https://github.com/iwenzhou/kenney`（21 MB，CC0-1.0）
2. **付费全量包** `https://kenney.itch.io/kenney-game-assets`（$19.95 / 60,000+ 资产，一次拿全）
3. 人工逐包从 `https://kenney.nl/assets?q=pixel` 下载

---

## 12. 批量获取脚本

`fetch_cc0_bulk.ps1`（同目录）是可重复执行的批量获取脚本，默认下载到仓库外的暂存目录，不污染工程。

---

## 13. 结论与下一步

### 收录总量

**共收录约 265 条**（含同系列展开 **290+ 个可选包**），覆盖 **8 个实质主题类别**（工坊机械 / 石材铺地 / 旧木室内 / 学院 / 符文以太 / 雨天水面 / 灯具光源 / 泛用）。7 条并行调研轨道合计核验 **OGA 页面 136+、非 OGA 页面 60+、逐项开页核对 75 项**，主动排除（等距/侧视/非像素/许可无法核实/实测不合规）约 45 项。VFX/图标层另有**逐像素实测**（下载文件后脚本统计色数与硬 Alpha）。

| 许可 | OGA 全站 2D Art | 本轮已收录 |
| --- | --- | --- |
| **CC0** | 8,067 | **≈ 380 项** |
| CC-BY 4.0 | 1,911 | ≈ 1,180 项 |
| CC-BY 3.0 | 4,142 | 120 项（CORE 100） |
| OGA-BY 3.0 | 1,171 | 77 项 CORE |
| CC-BY-SA 4.0（隔离区） | 742 | 116 项 |
| 主题标签扫描（全许可，**待核**） | — | ≈ 400 项 |

按主题的最终可用度：

| 主题 | 条数 | 可用度 |
| --- | --- | --- |
| 工坊 / 实验室 / 机械 / 管道 | ~90 | **很丰富**——CC0 密集；含 4 项 32×32 实验室内景、仪表/机柜/拨杆/传送带/金属地面 |
| 石材 / 砖墙 / 铺地 | ~35 | **很丰富**——DCSS 9,152 张 CC0 + DENZI + Top Down Dungeon Pack 2,256 张 64×64 |
| 旧木 / 家具 / 室内 | ~40 | **丰富**——LPC 系为主；CC0 有 Roguelike Indoors、Tables & Stools(OGA-BY)、Pixnote 木瓦 |
| **灯具 / 光源** | **~20** | **丰富（新识别类别）**——CC0 有 6 项；`lamp`/`torch` 是 OGA 信噪比最高的两个关键词 |
| **符文 / 以太** | **~26** | **中等偏薄**——Idylwild 四件套（全 CC0，含 Aether 符号 + 672 枚符文）；仍无成套"以太装置" |
| 学院 / 学校 / 图书馆 | ~22 | **中等**——有 Cool School(48×48 CC0，离格)、isaiah658(CC0)、Bookshelf(64×64 CC0)，**无"学院"成套** |
| 雨天 / 潮湿 / 水面 | ~14 | **中等**——雨动画三档雨强；**湿地反光贴片确认缺失**（`reflection` 命中 7、可用 0） |
| VFX / 图标 | ~25 | **中等**——八元素与状态图标有 CC0 解；**原生 32×32 合格特效仅 2 个**；地图节点 48×48 无现货 |
| 其他 / 泛用 | ~18 | 基底、UI、粒子、原型包 |

**许可分布**：CC0 约 65 条；CC-BY 3.0/4.0 约 35 条；CC-BY-SA / GPL（**传染**）约 20 条；OGA-BY 3.0/4.0（**非传染**）约 10 条；Unity EULA 4 条；CraftPix Freebie 约 8 条；日文站自定义许可 3 条；自定义/未核实约 25 条（主要为 itch.io）。

另：**Kenney 54 个包 / 18,810 文件全 CC0**；**DCSS 9,152 张 32×32 CC0 瓦片**；**LPC 生态 13,917 条记录**（OGA-BY 3.0 10,503 / CC0 1,011 / CC-BY 3,084+487 安全，其余传染性）。翻页总量约 **130+ 个列表页 / 3,500+ 条列表记录**。

### 能立刻做的（不触碰任何规范）
1. 把候选按 `ArtSource/<专题>/` 建观察目录，做与 Sephiria 研究同等的"仅观察"比对。
2. **优先试做这几套样板**（按"许可最干净 × 尺寸最对齐 × 主题最对口"排序）：
   - **Warped Top-Down Tech Lab 2**（CC0，32×32）— 许可最干净 + 尺寸完全对齐 + 实验室主题
   - **DCSS 32×32 双包**（CC0，9,152 张）— 一次性解决战场地面/墙体/图标面积问题
   - **lukems-br 31 种墙体集**（CC0）— 唯一做出"方形顶面 + 前立面"的 32px 正交墙
   - **Pipes and Tanks + Copper pipe tiles**（CC0 / CC-BY 4.0，32×32）— "可维护以太管道"骨架
   - **[LPC Revised] Workshop Tilesets**（OGA-BY 非传染）— 锻炉/铁砧/木工坊
   - **Golden UI**（CC0，暖金）— 暖纸档案 UI 面板
   - **Lab rack equipment**（CC0）— 仪表/电源/接口零件层
3. 跑一次 **Z Labs Pixel 12px vs FusionPixel12** 的中文字形 A/B，顺带做 OCC 现有文案的覆盖率检查。

### 确认必须自绘的部分（免费范围内确证为空）
成套"以太/能量装置"· **阀门本体** · 符文回路地面贴花 · **32×40 外沿地块与 32×48 两档尺寸** · 32px 正交俯视悬崖/高差模块 · 正交湿地砖/积水反射瓦片 · 机械犬 · 学院/学者/守卫/维护员阵容 · 四档语义色完整状态图标族。

### 需要用户裁决的一件事

外部素材当前**进不了正式资产**（`allowed_source_channels` 只允许 `codex_builtin_imagegen`）。可选路径：

| 路径 | 需要改什么 | 代价 |
| --- | --- | --- |
| A. 只作参考 + PROTOTYPE_ONLY 占位 | 什么都不用改，照 `PrototypeToonParticles/source_manifest.json` 登记即可 | 正式资产仍要 `image_gen` 生产一遍 |
| B. 允许外部素材成为正式资产 | 改总策划案 A.2.2 + 规格表 `ART-PIXEL-PIPELINE` + 机器合同 `allowed_source_channels`（按"先总案→规格表→机器镜像"顺序） | 引入风格一致性风险与逐包许可管理成本；仍需逐项过 manifest/证据/人工审美，未必比生图省事。**且 CraftPix 与 Unity Asset Store 明确禁止用于 AI 训练，这两家在 B 路径下只能成品直用、不能进生图链路** |

**建议**：先走 A，用免费素材把"局内对话条 / 事件界面 / 服务节点界面"这些**尚未制作的界面**做成可玩占位（正是总案第 9 节【美术表现线】欠的 14 场战斗界面、8 个事件界面、4 个服务节点界面、局内对话字幕），等流程跑通后再决定是否升级到 B。
