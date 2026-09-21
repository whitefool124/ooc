# OCC 战斗地图编辑器

用于可视化制作 `FirstRegionLevelDefinition` 对应的战斗格图。编辑器在浏览器中运行，但地图文件保存在本机，不依赖云端服务。

## 启动

双击 `启动地图编辑器.ps1`。如果 Windows 阻止直接运行，可在 PowerShell 中执行：

```powershell
powershell -ExecutionPolicy Bypass -File "E:\数据库\OCC_Codex\Tools\BattleMapEditor\启动地图编辑器.ps1"
```

默认地址为 `http://127.0.0.1:4178`。

## 主要能力

- 图层绘制：效果层、物件层、单位层、路线与禁用格标记。
- 拖动连续绘制、右键逐层擦除、`Ctrl+点击` 清空格子。
- 格子属性编辑、地图规则编辑、尺寸调整、缩放和图层显隐。
- 撤销／重做、本机草稿库、JSON 打开与保存。
- 导出当前地图的配置表 CSV，或复制 `FirstRegionLevelDefinition` C# 片段。
- 保存前持续校验地图 ID、出生点、重叠、边界、目标和路线锚点。

## 数据格式

- Schema：`schema/occ-battle-map-v1.schema.json`
- 活动地图目录：`../../Worldbuilding/地图配置/战斗地图/`
- 当前地图总表：`Worldbuilding/数据表/OCC_战斗地图配置表_v1.0.csv`

顶部“保存”会把 JSON 直接写入活动地图目录，并自动重建 CSV 总表；“另存为”用于导出独立文件。JSON 是逐格配置源，CSV 是地图级索引与查阅表。正式接入 Unity 前仍应通过项目测试验证配置与运行时一致。
