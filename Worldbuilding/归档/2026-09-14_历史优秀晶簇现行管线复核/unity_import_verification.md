# 晶簇与焦痕 Unity 实装验证

- 工程：`E:/数据库/OCC_Codex/UnityProject/Assets`。
- 活动场景：`Assets/Scenes/CombatTestArena.unity`；操作前无 dirty 场景。
- 编译：完成，0 error／0 warning；Console 无 error。
- 普通晶簇：资源 `academy_aether_crystal_intact`，96×64；Presenter 位置 `(8,2)` 与地图晶簇格一致。
- 64px 战场格下渲染矩形为 `x=-64, y=-64, width=192, height=128`，即按 32 PPU 原尺寸绘制并底部中心锚定，不改变单格逻辑占地。
- 焦痕：资源 `academy_scorched_lamp_vine`，32×32；Presenter 独立加载，`ObjectForegroundRows=0`。
- N06 运行截图：`UnityProject/Artifacts/RuntimeReview/crystal_n06_960x540.png`。
- 两项 Importer 均回读为 Sprite／Point／Clamp／32 PPU／Uncompressed／无 Mipmap。
- 针对性 EditMode 测试已写入；Unity Test Runner 被 21:46 起的旧零进度任务占用且取消接口失败，因此本轮使用同等断言的 Editor 内直接回读完成验证，没有重启编辑器破坏共享状态。
