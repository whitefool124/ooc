# OCC ART-COMBAT-SEMANTIC-POLISH-57 战斗语义弱项定点打磨验证（2026-08-27）

## 结论

- 状态：`COMPLETE / FORMAL / QA_PASS`。
- 范围：只返修 `intent.attack`、`intent.cast`、`intent.interact_destroy`、`status.slow`、`status.revealed`、`feedback.bound`、`feedback.slow`、`feedback.healing`、`feedback.shield_restore`、`feedback.status_cleared`。
- 未新增语义，未改稳定 ID、资源路径、玩法、数值、AI、状态、VFX、HUD 布局或存档。

## 美术审查

- 3 张 16px 意图从相近三叉／杂乱器物轮廓，改为单一重斩楔、缺口校准环和短锤断角。
- 2 张 32px 状态使用稳定持续构图：赭黄重块压住靴跟、暖金测绘括号围住人形。
- 5 张 32px 反馈使用明确瞬时动作：两侧夹紧、拖线止动、伤口缝合、三片护片合拢、扫弧推出状态点。
- 1×、4×、灰阶、棋盘格以及旧新对照通过；青色未进入本批，水晶、眼睛、医疗瓶、医疗十字、翅膀盾和装饰花结均未复用。

## Unity 与验证

- Funplay 身份门禁：`Application.dataPath=E:/数据库/OCC_Codex/UnityProject/Assets`，活动场景 `Assets/Scenes/CombatPrototype.unity`，开始时 `dirty=False`。
- Resources／Importer 10/10，原 GUID 保持 10/10；Sprite、Point、Clamp、Uncompressed、无 mipmap、PPU16／PPU32 全部通过。
- 1920×1080 使用 2×、960×540 使用 1× 的实际 HUD 接触通过；意图、状态和反馈未遮挡单位、资源条或交互提示。
- manifest 10/10 `FORMAL`；验证器 10/10 PASS；合同测试 6/6；Unity 聚焦 EditMode 3/3；全量 EditMode 650/650。
- 编译错误／警告 0；Console error／warning 0；dirty scenes 0。未进入 Play Mode，未保存场景。

## 证据

- 简报与 manifest：`Worldbuilding/05_美术与音频/正式美术生产/M-A25/`。
- 清单：`Worldbuilding/05_美术与音频/正式美术生产/M-A25/combat_semantic_polish_10_catalog.json`。
- 旧新对照：`UnityProject/Artifacts/CombatSemanticPolish10/contacts/offline_before_after_1920x1080.png`。
- Unity 1920×1080 接触：`UnityProject/Artifacts/CombatSemanticPolish10/contacts/unity_polish_contact_1920x1080.png`。
- Unity 960×540 接触：`UnityProject/Artifacts/CombatSemanticPolish10/contacts/unity_polish_contact_960x540.png`。
- Unity 导入报告：`UnityProject/Artifacts/CombatSemanticPolish10/unity_import_report.json`。
- 正式验证报告：`UnityProject/Artifacts/CombatSemanticPolish10/validation_report_formal.json`。

## 后续

- 本批完成后停止；其余 21 张不因批量一致性而自动重做。
- 后续新增内容优先另立八元素／资源货币图标任务；真实游玩若暴露单件误读，只返修对应 manifest 与 PNG。
