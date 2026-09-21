# OCC 手动下载清单

日期：2026-09-17　用途：脚本抓不到、需要你用浏览器下载后放进暂存区的素材

**建议落盘位置**：`E:\OCC_素材库\_manual\`（建好各来源子目录，见每节标明的 `→ 放这里`）

> 配套：`README.md` 是完整候选清单与许可核实记录；`fetch_cc0_bulk.ps1` 是能自动跑的部分（已跑完 DENZI / DCSS / Tiddybub）。
> 现在暂存区已有 **70,337 个 PNG，其中命中 OCC 目标规格 37,040 个**。

---

## 为什么需要手动

脚本能自动抓的已经抓完了。剩下的卡在四类障碍上：

| 障碍 | 涉及来源 | 说明 |
| --- | --- | --- |
| **页面 JS 渲染** | Kenney、Godot store | 纯 HTTP 抓到的 HTML 里既没有条目链接也没有下载链接 |
| **反爬 / 需登录** | itch.io 列表页、CraftPix、GameDev Market | 列表页 403；下载需浏览器会话 |
| **浏览器交互** | LPC 生成器 | 需要在网页上勾选部件后导出 |
| **连接不稳** | GitHub 个别仓库 | 已重试，见 §4 |

---

## 1. Kenney —— 单笔收益最高（54 包 / 18,810 文件 / 100% CC0）

**为什么手动**：`kenney.nl` 的列表页与包页在纯 HTTP 抓取下都是 JS 空壳，实测 `https://kenney.nl/assets/tag:pixel`、`?q=pixel`、`/assets/tiny-factory` 三者原始 HTML 均无 slug 也无 `.zip` 链接（长度仅 15–23 KB）。

**三条路线，选一**：

- [ ] **路线 A（推荐，省事）**：买全量包 **$19.95** — https://kenney.itch.io/kenney-game-assets
      60,000+ 资产一次到手，注明 "unlimited commercial projects (no attribution required)"，CC0。
- [ ] **路线 B（免费）**：GitHub CC0 镜像 — `git clone --depth 1 https://github.com/iwenzhou/kenney`（21 MB）
- [ ] **路线 C（免费，最全）**：逐包下载 https://kenney.nl/assets?q=pixel（14 页）
      以及 https://kenney.nl/assets/series:Tiny 、 https://kenney.nl/assets/tag:interface

**尺寸最对口、优先下的包**（16×16 为主，可整数 2× 进 OCC 网格）：

| 包 | 文件数 | 尺寸 | 为什么优先 |
| --- | --- | --- | --- |
| Roguelike/RPG pack | 1700 | 16×16 | 体量最大，含家具/门/面板 |
| 1-Bit Pack | 1078 | 16×16 | 极简双色，最贴硬边限色契约 |
| Roguelike Caves & Dungeons | 520 | 16×16 | 石墙/砖/矿 |
| Roguelike Indoors | 480 | 16×16 | 室内陈设 |
| RPG Urban Pack | 480 | 16×16 | 近代城市街景 |
| Roguelike Characters | 450 | 16×16 | 角色 |
| Tiny Factory | 130 | 16×16 | 工厂/传送带/**管道与阀件** |
| Scribble Dungeons | 256 | **64×64** | 64 档 |
| Rune Pack | 640 | — | 符文石 |
| Tower Defense (Top-Down) | 300 | — | 战棋向 |
| Top-down Shooter | 580 | — | 俯视向 |
| Pixel Platformer Industrial Expansion | 110 | 18×18 | 工业金属件 |
| Input Prompts Pixel / Pixel 1-Bit | 800 / 800 | 16×16 | 快捷键位 |
| Minimap Pack | 150 | 8×8 | 6× 放大即 48×48 |

**→ 放这里**：`E:\OCC_素材库\_manual\Kenney\`

> ⚠ Kenney Fonts 只有 11 个**纯拉丁**字体，无中文。中文走 §5。

---

## 2. itch.io —— 需要浏览器会话（列表页 403，商品页可读）

**为什么手动**：`web_fetch` 全线失败；pwsh 抓 browse/tag 列表页返回 403（`tag-cc0` 无法枚举），但**单个商品页可读**——所以下面的许可都是逐包核实过的。

### 2.1 CC0（免署名、可商用）—— 优先

- [ ] **Ninja Adventure Asset Pack**（Pixel-Boy）— https://pixel-boy.itch.io/ninja-adventure-asset-pack
      **CC0，89 MB**，16×16，含 UI / 字体 / 音效。页面原文 "even commercial ones. Attribution is not required"
- [ ] **0x72 全部自有包** — https://0x72.itch.io/
      **全部 CC0**：DungeonTileset II（16×16，业界标杆）、16×16 Dungeon Tileset、**16×16 Industrial Tileset**、DungeonUI、µFantasy、Pirates、Robot、2Bit Micro Metro、8×8 F24
- [ ] **Ansimuz 的 15 个 CC0 包** — https://ansimuz.itch.io/
      最对口：Tiny RPG Fantasy（2.6 MB）、Tiny RPG Adventure（7.7 MB）、Legend of Faune（3 MB）、SunnyLand Forest、GothicVania Town、Magic Cliffs
- [ ] **Matt Walkden 3 包** — https://mattwalkden.itch.io/
      **CC0**；Robot Warfare 的 tags 含 **Tactical / Roguelike / Tileset**
- [ ] **Not Jam Font Pack** — **28 个 CC0 像素字体**（拉丁）
- [ ] **Karsiori** — 齿轮 / 工业 / Mechs / 蒸汽工控件面板，**CC0**
- [ ] **GrafxKid** — City Mega Pack 等，**CC0**
- [ ] **isaiah658 Pixel Pack #1 / #2** — **CC0**；#2 含"科学实验室 / 洁净工业风瓷砖"与 64×64 怪物
- [ ] **Superpowers Asset Packs** — https://github.com/SparklinLabs/superpowers-asset-packs（`LICENSE.txt` = CC0-1.0）
- [ ] **surt** 全部包 / **Buch**（Outdoor 32×32、Sci-fi Interior、RPG portraits）/ **Jetrel**（RPG item set、16×16 RPG items）— 均 **CC0**

### 2.2 自定义许可（可商用、免署名、禁再分发）—— 次优先

- [ ] **Szadi art 的 22 个免费包** — https://szadiart.itch.io/
      许可原文："Public domain and free to use, personal or commercial. Credit is not required… You can edit, but not resell the asset pack"
      ⚠ 法律上 "public domain" 与 "not resell" 矛盾，**不是真正 CC0**；建议署名 Szadi art 以求稳妥
- [ ] **Cainos 3 个免费包** — https://cainos.itch.io/pixel-art-top-down-basic 等
      **原生 32×32**，48 props + 256×256 grass/stone + 512×512 wall；`pixel-art-icon-pack-rpg` 有 **107 个 32×32 图标**

### 2.3 ❌ 免费版禁商用（**别下免费版当可用素材**）

| 包 | 说明 |
| --- | --- |
| **Mystic Woods**（Game Endeavor） | 免费版禁商用，**且被叠了 noise 水印**，作者原话 "without allowing use of the assets" → **实际不可用** |
| **Sprout Lands**（Cup Nooble） | 免费版仅非商用；付费 ≥$3.99 才可商用 |
| **Kenmi Cute Fantasy RPG** | 免费版仅非商用（免费 53 kB vs 付费 3.4 MB） |

### 2.4 🔴 "免费"其实是 demo / 子集（下了也用不了多少）

**LimeZu Modern Interiors**（免费 1 MB vs 完整 **149 MB** = 0.67%）· Mana Seed Character Base / Farmer / 四季森林 · Szadi Craftland / Fantasy Lands Houses / PostApo Lands Demo · Kenmi Tiny Metroidvania（5 kB）· Ansimuz Cold Corridors · Sunset City LITE
**「免费」名不副实**：**LimeZu Modern Exteriors 纯付费 $5，无免费版**

**→ 放这里**：`E:\OCC_素材库\_manual\itch\<作者名>\`

---

## 3. CraftPix 与 Godot —— 需注册 / 浏览器

- [ ] **CraftPix 免费区** — https://craftpix.net/freebies/
      **实测 30 页 = 471 个去重免费项**。许可（https://craftpix.net/file-licenses/ 第 2 节）：**可商用、无限项目、免署名**；禁转售源文件
      ⛔ **但条款 3.1.1 明确禁止用于 AI/ML 训练与生成式 AI** —— 见下方 §6 的红线
      主题对口项：**Free Factory Pixel Art 32×32 Tileset**（工厂/管道/箱柜 + 动画陷阱）、**Free Top-Down Guild Hall**（最接近"学院大厅"）、**Free Glassblower's Workshop Top-Down**（熔炉带火焰动画）、**Free Pixel Dungeon Props**（页面写明适合 alchemist's labs）、**Free Basic Pixel Art UI for RPG**（全套窗口/菜单/HUD，PNG+PSD）、**Free Steampunk Cityscape Pixel Backgrounds**
- [ ] **CraftPix 的 itch 账号** — https://free-game-assets.itch.io/ 挂 **1,121 个免费包**
- [ ] **Godot Asset Store** — https://store.godotengine.org/search/?query=%23tileset（**62 条**，可按 CC0/MIT/Apache 筛）
      例：**Little Factory**（工厂 + 传送带动画，标签含 Top-down）
      ✅ **Godot 素材是引擎无关 PNG，Unity 可直接用**
      ⛔ 注意 `godotengine.org/asset-library`（旧站）**没有美术分类**，`category=assets` = 0 items —— 别走错

**→ 放这里**：`E:\OCC_素材库\_manual\CraftPix\` 、 `E:\OCC_素材库\_manual\GodotStore\`

---

## 4. 脚本没抓下来的 GitHub 仓库（我已重试，失败可手动）

- [ ] `https://github.com/Papyszoo/CC0-Public-Domain-Sprites` — **172 MB**，CC0
- [ ] `https://github.com/iwenzhou/kenney` — 21 MB，CC0-1.0（Kenney 镜像）
      下载方式：仓库页 → Code → Download ZIP（比 `git clone` 抗断）

**→ 放这里**：`E:\OCC_素材库\_manual\GitHub\`

---

## 5. 中文字体（GitHub Releases，可直接下）

| 字体 | 许可 | 覆盖率 | 建议 |
| --- | --- | --- | --- |
| **Fusion Pixel Font**（现用） | OFL 1.1 | 12px：规范汉字 7269/7445（97.64%）、GB2312 100% | 保留为主用 |
| ★ **ZLabs Pixel 12px** | OFL-1.1 | **GB/T 2312 6763/6763 + 通用规范汉字表 8105/8105 双 100%** | **建议 A/B**——唯一能补 Fusion 那 145 个二级汉字缺口 |
| Ark Pixel | OFL-1.1 | 12px 6930/7445；**10px 不可用、16px 已废弃** | 不如现有 |
| Cubic 11 | OFL-1.1 | 简体仅 GB2312 一级 | 不如现有 |
| GNU Unifont | GPLv2+嵌入例外 或 OFL 1.1 | BMP 全覆盖 | 仅缺字 fallback |
| ⛔ Zpix / DinkieBitmap / 文泉驿 | 付费或 GPL | — | **排除** |

- [ ] https://github.com/TakWolf/fusion-pixel-font/releases
- [ ] https://github.com/Astro-2539/ZLabs-Pixel-12px （锁 **Build_20260519**，含 `ZLabsPixel_12px_M_CN.ttf` 3.35 MB）
- [ ] https://github.com/TakWolf/ark-pixel-font/releases
- [ ] https://github.com/ACh-K/Cubic-11

**→ 放这里**：`E:\OCC_素材库\_manual\Fonts\`

---

## 6. ⛔ 下载前必读：三条红线

1. **AI 训练禁令（影响最大）**
   **CraftPix（条款 3.1.1）、Unity Asset Store EULA、UnDots（日）、ぴぽや（日）** 都**明文禁止**把素材用于训练/微调/改进生成式 AI。
   OCC 的美术生产是 `image_gen` 驱动的 → 这些素材**连当生图参考都不行**，只能人工目视或手工重绘。
   ✅ 反之 **CC0 来源（Kenney / OGA / Tiddybub / DCSS / DENZI）没有此限制**。

2. **Mana Seed 的 No-GenAI 条款**
   https://selieltheshaper.weebly.com/user-license.html 原文禁止 "used in a project alongside AI generated imagery, writing, code, or anything else"。
   → 若 OCC 成品保留任何 AI 生成内容或 AI 代码，**Mana Seed 全部（含免费）不可用**。而它恰是唯一有蒸汽朋克飞艇/庄园/大图书馆顶视素材的作者。

3. **传染性许可（CC-BY-SA / GPL）不要下**
   下了也用不了。清单里已逐条标注，典型：`denzis-32x32-orthogonal-tilesets`、`32x32-fantasy-tileset`、`roguelike-dungeonworld-tiles`、LPC Base Assets、`golden-ui-bigger-than-ever-edition`、Sithjester（许可要求 "Must own RMXP"）。

---

## 7. 建议的下载顺序

1. **Kenney**（§1）—— 单笔收益最高，54 包 / 18,810 文件
2. **itch.io CC0 那批**（§2.1）—— 0x72 全站 + Ninja Adventure + Ansimuz 15 包 + Cainos
3. **GitHub 两个仓库**（§4）—— 手动 Download ZIP
4. **中文字体 ZLabs**（§5）—— 为 A/B 准备
5. **CraftPix + Godot store**（§3）—— 量大但要先过 §6 红线判断

下载完把目录丢进 `E:\OCC_素材库\_manual\`，我可以用同一套脚本按像素规格（尺寸分布、命中 OCC 规格的数量）盘点一遍，并入总账。
