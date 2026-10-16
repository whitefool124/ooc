# OCC 音乐与音效来源

2026-10-01 下载并核对以下作品页面。以下外部新增作品页面均标示 CC0 1.0，保留作者署名便于追溯。许可文本：https://creativecommons.org/publicdomain/zero/1.0/

| 用途 | 作品与作者 | 来源 |
| --- | --- | --- |
| 主菜单、档案、战斗结算 | JRPG Piano — Joth | https://opengameart.org/content/jrpg-piano |
| 学院地图探索 | Town Theme RPG — cynicmusic | https://opengameart.org/content/town-theme-rpg |
| 学院介绍与整备 | Magic Town — controllerhead | https://opengameart.org/content/magic-town |
| 普通战斗 | JRPG Trailer / Theme — Juhani Junkala | https://opengameart.org/content/jrpg-trailer-theme |
| 首领战 | Boss Battle Theme — Cleyton Kauffman | https://opengameart.org/content/boss-battle-theme |
| 档案翻页、装置、以太与火焰 | 80 CC0 RPG SFX — rubberduck | https://opengameart.org/content/80-cc0-rpg-sfx |
| 施法变体 | Magic Spell SFX — JaggedStone | https://opengameart.org/content/magic-spell-sfx |

完整 80 个 RPG 音效保存在 ArtSource/Audio/FreeLibrary/rubberduck；只把选用的短音效导入 Unity。下载 URL、SHA-256 与文件大小见 free_audio_sources.json。现有 Kenney 与 OpenGameArt 纸张、战斗音效继续沿用工程内各自的许可记录。

28 个选用外部短音效转换为 16-bit PCM WAV，校准到目标 RMS -22 dBFS，峰值最多 0.8；峰值约束优先。原始下载保存在 FreeLibrary/ImportedOriginals，游戏使用 FreeProcessed 下的衍生 WAV。处理系数与衍生文件 SHA-256 见 free_audio_processing.json。配乐按实测 RMS 设置各自播放增益，让档案/探索响度接近、战斗略高。

重建顺序：运行 fetch_free_audio.py；Unity 导入源音效并以 DecompressOnLoad/PCM 读取；通过 Funplay execute_code 运行 Tools/Audio/NormalizeFreeAudio.cs.txt，将返回 JSON 保存到临时目录；运行 normalize_free_audio.py <返回JSON路径>。正式包只使用 FreeProcessed；完成后将临时 Free 原始文件移回 ArtSource，避免重复打包。

OCC 原创短提示音：选择、档案确认、拒绝、以太准备、奖励、胜利与重试。由 Tools/Audio/compose_occ_score.py 合成，不使用外部采样；源码与生成参数随仓库提供。OriginalCandidates 内三首原创配乐是候选素材，本版使用上表的免费配乐，候选不进入游戏包。

主音量控制所有新增声音。背景音乐采用两路淡入淡出、2D 播放与流式加载。开场视频阶段淡出配乐，保留视频自身音轨。

cynicmusic 作者网站： https://cynicmusic.com / https://pixelsphere.org 。仅使用上表单曲页授权的 Town Theme RPG。

2026-10-01 战斗增补：来自 rubberduck 的挥击、金属、木材、石块、链条和燃烧音效，以及已有 Kenney Impact Sounds 的 impactGeneric_light_000（CC0，工程中附 License.txt）。按钮选择恢复为 Kenney click_001，确认使用 bookPlace1，拒绝使用轻量 bookClose，替换原音符提示。


2026-10-03 材质与服务拟音增补（CC0）：Kenney RPG Audio（https://kenney.nl/assets/rpg-audio）、Kenney Impact Sounds（https://kenney.nl/assets/impact-sounds）；水面脚步使用 Peludo 的 Water splash and sand footsteps（https://opengameart.org/content/water-splash-and-sand-footsteps），保留作者项目 https://rnan.itch.io/ 。原始包、许可证、下载哈希保存在 FoleyExpansion20261003；游戏仅导入25条选用的单声道PCM短音效。processing.json 记录每条来源、裁静音、增益、峰值与输出哈希，目标 RMS -22 dBFS、峰值上限0.78。重建：fetch_foley_expansion.py → prepare_foley_expansion.py → Unity 菜单 OCC/Audio/Import Foley。金属扣件原料同时用于抓取与锁扣两个语义资源，不计为两条独立原料。


## 六十项小细节首批音频（2026-10-03）

- 原有 Kenney RPG Audio / Impact Sounds CC0 原料另选14个短片：房间进入3个、商店退出1个、工坊页签2个、器皿3个、取消归位1个、推拉冲击3个、音量试听1个。音频来源和原包许可沿用上一节；处理映射见 `Tools/Audio/prepare_foley_expansion.py`，39项处理记录见 `ArtSource/Audio/FoleyExpansion20261003/processing.json`。资源存在不等于全部业务入口已验证。
- 室外循环：Luke.RUSTLTD，[Wind1](https://opengameart.org/content/wind1)，CC0。独立原料 `ArtSource/Audio/Details60/wind.wav`，使用对应公开附件风声；不使用网站其他文件的许可。
- 室内低底噪：LEGIT Audio，[The Shop 免费音效包](https://opengameart.org/content/the-shop)，CC0。仅本次免费包的便利店饮料冷柜底噪片段，原包 `ArtSource/Audio/Details60/rooms.zip`；运行时降低电气高频与音量，只用于市集/工坊等器物空间，不作为医务室环境声。
- 下载URL、作者、许可与SHA256：`ArtSource/Audio/Details60/sources.json`。两个循环的交叉接缝、RMS与峰值记录：`ArtSource/Audio/Details60/processing.json`；可复现工具 `Tools/Audio/prepare_detail_ambience.py`。风声/室内循环进入 Resources 的是处理后的单声道WAV，原料不覆盖。
