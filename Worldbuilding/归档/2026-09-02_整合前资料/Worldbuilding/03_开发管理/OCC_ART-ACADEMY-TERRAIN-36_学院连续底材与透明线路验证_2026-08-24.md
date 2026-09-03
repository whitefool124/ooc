# ART-ACADEMY-TERRAIN-36 学院连续底材与透明线路验证

日期：2026-08-24  
结论：PRODUCT REJECTED / SUPERSEDED（同日产品复核）

> 本文保留机械与运行时证据，但不再证明美术方向通过。产品复核确认四张连续底材是低清材质场，不具备成熟像素战棋的 tileset 语法和人工构图质量；原人工 `PASS` 与 `FORMAL` 结论撤销，任务已重新打开。

## 结果

- 战场地面从 108 张逐格底图实例改为一张 12×9、384×288 连续底材；格子对象仅保留命中、范围、物件和单位层，不再常驻绘制逐格地板。
- 新增庭院、石路、废墟、夯土四套独立连续底材。旧庭院十字网、石路砖墙行、废墟四象限和泥地折线符号不再由运行时消费。
- 旧以太连接件拆为透明直线和十字覆盖层，只保留暗铜槽与有效青色；运行时继续使用 90° 离散旋转，横纵连接无方形底色补丁。
- 四套底材均为 8 色硬 Alpha，线路为 4 色硬 Alpha；六份 `occ-art-manifest-v1` 状态 `FORMAL`，独立来源、1×／4×／灰阶／棋盘格／应用接触和人工审美全部通过。

## 实机审美复核

- 1920×1080：地面结构跨越玩法格，没有闭合格框、双线、L 角、逐格重复或缩放拉伸；单位、掩体、战利品、青色移动范围和 HUD 均保持更高视觉优先级。
- 960×540：同构缩放下人物轮廓、范围边界和线路节点仍清楚，底材没有重新聚合成粗网格。
- 四主题运行时轮换接触：庭院为暖灰石灰岩，石路为深冷旧石，废墟为石土侵蚀，夯土为大块压实走势；主题差异来自连续材料结构，不再来自格内符号。
- 线路接触：横纵主线及中心十字连续；透明区不污染任何底材，没有 32×32 方形补丁。

## 证据

- 四主题实机接触：`UnityProject/Artifacts/Terrain36/runtime_four_themes_contact.png`
- 庭院／线路 1920×1080：`UnityProject/Artifacts/Terrain36/runtime_courtyard_1920x1080.png`
- 庭院／线路 960×540：`UnityProject/Artifacts/Terrain36/runtime_courtyard_960x540.png`
- 规范化候选：`UnityProject/Artifacts/Terrain36/floorfields_contact_2x.png`
- 透明线路 4×：`UnityProject/Artifacts/Terrain36/aether_overlays_4x.png`

## 门禁

- 美术合同审计：PASS。
- 六份正式 manifest：6/6 PASS。
- Unity 导入：四张 384×288、两张 32×32；PPU32、Point、Clamp、Uncompressed、无 mipmap；六项稳定 GUID 已回填。
- 聚焦 EditMode：24/24 PASS。
- 全量 EditMode：630/630 PASS。
- PlayMode：1/1 PASS。
- 编译：0 error / 0 warning。
- Console：无 error。
- 场景：`Assets/Scenes/CombatPrototype.unity`，dirty=false；未保存场景。

## 下一步

恢复学院第一层固定种子三路线实机平衡采样。环境主题后续只通过结构物、局部透明覆盖和光色差异扩展，不再恢复逐格底材拼贴。
