# Reference — 外部参考资料库（只读）

本目录存放入库的**第三方开源参考资料**，供后续按需检索学习。它们不是 OCC 的规则来源，也不构成策划案、美术规范或数据口径的依据。

## 规则

- 本目录内容**只读**：不修改、不提交进 OCC 仓库（`.gitignore` 已忽略 `/Reference/*`，仅本 README 入库）。
- 与 OCC 设定/玩法/美术口径冲突时，一律以 `Worldbuilding/策划案/OCC_项目总策划案_v1.0.md` 与飞书母版为准，本目录不得反向覆盖。
- 引用时注明来源仓库与文件路径；通用工程经验可用，涉及具体产品决定必须回到 OCC 自身文档确认。
- 两份资料都是 `git clone --depth 1` 快照，可 `git pull` 更新；在克隆内部检索优先用关键词搜索，不要整篇通读。

## 目录清单

| 子目录 | 来源 | 定位 | 入库日期 | 版本 | 体积 |
| --- | --- | --- | --- | --- | --- |
| `GameDevMind/` | https://github.com/gonglei007/GameDevMind | 最全面的游戏开发技术图谱：基础能力、技术能力、研发能力、生产能力、管理能力、运营能力 | 2026-09-17 | `626c187`（2026-08-11） | 工作树 ≈153 MB + `.git` 127 MB |
| `anything_about_game/` | https://github.com/killop/anything_about_game | 游戏开发资源链接清单：引擎、图形、CG、硬件、Unity 路线、语言与博客聚合 | 2026-09-17 | `7744ff4`（2026-09-14） | ≈1 MB |

## GameDevMind 入口

- `README.md`：总技术图谱说明
- `INDEX.md`：**全文档索引**（按能力模块分类，182 行），找章节从这里进
- `KEYWORDS.md`：关键词索引，按关键词反查章节
- `mds/`：124 篇章节正文，顶层为 `1.基础能力`、`2.技术能力`、`3.研发能力`、`4.生产能力`、`5.管理能力`、`6.运营能力`，另有 `topics/`、`一站式手游创业/`
- `code/`：151 个配套示例代码；`xminds/`：112 个思维导图源文件；`exports/`：导出物（81.5 MB）；`nav/`：站点导航
- `docs/知识结构分层规范.md`、`docs/文档命名规范.md`：本仓库自身的组织规范

## anything_about_game 入口

- `README.md`：主清单（约 504 KB / 6564 行），含 Awesome-Game、Awesome-General、News（Game/Graphic/CG/HardWare）、Person/Social/Blogs 等分区
- `AI.md`、`UnityTips.md`、`FamousGame.md`、`Houdini.md`、`Quantification`、`Resource.drawio`

## 网络注意事项

本机直连 `github.com` 的 git 协议会被重置（`Recv failure: Connection was reset`），必须走本地代理 `127.0.0.1:7897`。两个克隆已各自写入本地配置 `http.proxy=http://127.0.0.1:7897`，代理客户端在运行时可正常 `git pull`；若代理未启动，需先启动代理，或临时 `$env:https_proxy='http://127.0.0.1:7897'` 后再操作。
