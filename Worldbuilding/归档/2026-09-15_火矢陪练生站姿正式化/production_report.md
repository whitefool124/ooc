# 火矢陪练生站姿资产生产记录

- 第 1 轮：拒绝。主体网格置信度 2.3406，但无损解码后为 40×89，超过固定 32×64 单位合同。
- 第 2 轮：通过。只修正宏像素粗细与主体占用；网格置信度 2.3304，主体 27×51，32 色，硬 Alpha，无缩放、插值、抖色或新增颜色。
- 视觉审查：严格右侧视站姿；学院制服、紧凑手杖和单一暖色火矢特征在 1× 下清晰，相比旧正式图主体更饱满，但双脚仍落在单格所有者位置。
- Unity 导入：保留 GUID `acc330d5d9d87c7479c701774999cdc3`；Sprite、Point、Clamp、32 PPU、Uncompressed、关闭 mipmap。
- 运行时：`FormalArtRegistry.UnitPath("pyromancer")` 与 `CombatFormalVisualAssets` 缓存均指向新 32×64 贴图，缓存一致性断言通过。
- 编译与 Console：0 error、0 warning，Console 无错误。

本批未更改玩法、数值或画布合同，因此不需要同步总策划案或数据表。原正式贴图保存为 `pyromancer_previous_formal.png`供追溯。
