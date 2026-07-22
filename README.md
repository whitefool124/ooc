# OCC Codex Project

OCC（魔法战争人生战棋肉鸽）的统一工作目录。

## 目录

- `UnityProject/`：Unity 工程源文件。打开 `UnityProject/My project.slnx` 或将 `UnityProject/` 作为 Unity 项目打开。
- `Worldbuilding/`：OCC 游戏策划、技术方案、开发计划、数据设计、美术规范与构建发布资料。
- `Art/GeneratedPreviews/`：OCC 专用 AI 生图预览与 UI/地图风格参考；这些文件不是可直接导入的正式游戏资产。

## 当前基线

- Unity 场景：`UnityProject/Assets/Scenes/CombatPrototype.unity`
- 当前完成阶段：阶段 2，确定性战斗、状态、装备、技能、背包、快捷栏、工坊、敌人原型与精英敌人。
- 当前美术基准：`Worldbuilding/05_美术与音频/OCC_美术规范_v0.1.md`
- 当前 UI 构图参考：`Art/GeneratedPreviews/OCC_UI_1920x1080_map75_v02.png`

## 维护原则

1. 先阅读 `Worldbuilding/README.md` 与对应策划/技术文档。
2. Unity 只使用 `UnityProject/` 内的项目源文件；不要把生成预览直接当作正式资产。
3. 生成图需经过像素化、固定尺寸、透明背景和人工 QA 后，才能复制到 Unity `Assets/`。
4. 原始位置仍保留作为备份；本目录是 Codex 的统一工作副本。
