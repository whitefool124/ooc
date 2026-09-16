# OCC Codex Project

OCC（魔法战争人生战棋肉鸽）的统一工作目录。仓库根目录固定为 `E:/数据库/OCC_Codex`。

## 目录

- `UnityProject/`：Unity 工程源文件。打开 `UnityProject/My project.slnx`，或把 `UnityProject/` 作为 Unity 项目打开；默认场景 `Assets/Scenes/CombatPrototype.unity`。
- `Worldbuilding/OC世界观/`：OC 世界观唯一活动参考；OCC 的设定必须遵守该目录。入口 `Worldbuilding/OC世界观/README.md`，版本与路由见 `00_项目总览/世界观索引.md`。未成文的编号目录表示当前确有缺口，不用归档材料补位。
- `Worldbuilding/策划案/`：OCC 总策划案（飞书总策划案的本地同步镜像）与 `临时工作稿/`。
- `Worldbuilding/数据表/`：OCC 玩法与内容数据基线（单位、装备、强化材料、状态、被动、技能、地图元素、学院敌人／装备／遭遇编成／节点内容、教程系列、经济与服务、美术与界面规格、锻造与专精、餐食增益、第一阶段全流程内容清单）。
- `Worldbuilding/开发管理/`：开发管理与维护记录。**项目待办以总案第 9 章「三线并行设计待办」为唯一标准**（已与飞书同步），本目录不再维护并列清单。
- `Worldbuilding/归档/`：历史版本、过程记录、`*_QA_PENDING` 候选与发布镜像；只用于追溯，不构成当前规则，也不得回移为活动源。
- `Worldbuilding/实验性内容/`：尚未定论的实验素材与样板（如三材质地块拼接样板）；不构成规则，也不是正式资产。结论产生后被采用的转 `ArtSource/` 走正式流程，被否决的进 `归档/`。
- `ArtSource/`：正式美术的生产源（生图原料、解码结果、`occ-art-manifest-v1`、QA 与复核记录）。
- `Art/GeneratedPreviews/`：OCC 专用 AI 生图预览与 UI／地图风格参考；这些文件不是可直接导入的正式游戏资产。
- `Tools/`：校验与生图辅助工具（如 `Tools/OCCArt/` 的美术合同与校验脚本）。
- `.agents/`、`.dsh/`：各代理在本仓库使用的项目技能目录。
- 本地产物（`Artifacts/`、`draft_*_folder/`、`Library/`、`Temp/`、`Logs/` 等）由 `.gitignore` 排除，不属于源文件。

## 母版与镜像关系

- 飞书文档 `https://jcnlgassfnsm.feishu.cn/docx/Y1KydFPjMocJjYxMmFEcTblcnLf` 是 OCC 具体产品内容的编辑母版。
- `Worldbuilding/策划案/OCC_项目总策划案_v1.0.md` 是飞书总策划案的本地同步镜像，也是离线工作的第一参考。
- 美术规范的唯一活动源是总策划案第 5 节与附录 A，以及 `Worldbuilding/数据表/OCC_美术与界面规格表_v1.0.csv` 与机器镜像 `Tools/OCCArt/occ_art_contract_v1.json`。
- 归档中的专项规范、历史简报、资产清单与旧 manifest 只能追溯，不得覆盖上述活动源。

## 当前基线

- 世界观基线：`OC-WB-2026-09-07`，入口 `Worldbuilding/OC世界观/README.md`。
- 玩法阶段：学院第一阶段。首轮固定教程段为 FIRST-B1／B2／B3／FIRST-X 四场，随后接入随机学院地图循环（普通战 N01–N09、精英战 E01–E03、事件 EV01–EV08、服务节点与首领 B01）。范围、冻结状态与验收缺口以总案第 8 章和 `Worldbuilding/数据表/OCC_学院第一阶段全流程内容清单_v1.0.csv` 为准。
- 战斗原型：`Assets/Scenes/CombatPrototype.unity`，含 13 场正式候选战斗与专用测试场样板。
- 美术基准：战场地面以原生 32×32 为逻辑格基线，多格地表使用 `(W×32)×(H×32)`，外沿地块 32×40（下方 8px 为不参与交互的外立面）；战斗 UI 为 1920×1080、左侧地图 75%／右侧 HUD 25%。
- UI 构图参考：`Art/GeneratedPreviews/OCC_UI_1920x1080_map75_v02.png`。

## 维护原则

1. 世界设定先读 `Worldbuilding/OC世界观/README.md` 与索引；玩法实现再读 OCC 总策划案和对应数据表。
2. Unity 只使用 `UnityProject/` 内的项目源文件；不要把生成预览直接当作正式资产。
3. 生成图需经过像素化、固定尺寸、透明背景和人工 QA 后，才能复制到 Unity `Assets/`；每项新美术必须先有 `occ-art-manifest-v1` 并通过 `Tools/OCCArt/validate_occ_art_asset.py`。
4. 不再维护项目外的 OC 世界观副本；历史稿和发布镜像只进入 `Worldbuilding/归档/`，不得作为活动设定。
5. 同一主题只保留一个活动源：数据源总表见总案 7.1，节点与遭遇见 `OCC_学院节点内容数据表_v1.0.csv` 与 `OCC_学院遭遇编成数据表_v1.0.csv`，技能内容见 `OCC_技能配置表_v1.0.csv`。
6. 发现文档冲突时先列出冲突位置并停止沿用冲突口径，由用户裁决后再改活动源；旧内容移入 `Worldbuilding/归档/`。
7. 项目待办只维护一处：`Worldbuilding/策划案/OCC_项目总策划案_v1.0.md` 第 9 章；新增或完成任务直接改该章并同步飞书。
