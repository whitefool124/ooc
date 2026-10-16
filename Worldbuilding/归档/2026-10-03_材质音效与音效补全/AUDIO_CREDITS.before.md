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
