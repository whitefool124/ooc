# ART-ACADEMY-TILES-41 学院九图独立方块地板与有向阴影纠正验证

日期：2026-08-25  
归属：剧情／肉鸽共用学院战场表现层

## 结论

通过。学院九张 `12×9` 战场已从历史 `3×3 / 96×96` 连续地面宏块迁移为 16 张原生 `32×32` 自包含方块：庭院、道路、遗迹、夯土各 A–D。运行时每格只绘制一张完整素材、使用完整 UV、旋转角恒为 0；相邻格不共享砖缝、裂纹、车辙、阴影或结构端点。

## 实机迭代结果

1. v14 独立地砖首次接入后，道路每格只有两块巨型板、夯土主缝过粗，判定不转正。
2. v20 第二稿用每格 `3×3` 小砖解决巨型板，但庭院和遗迹在大面积铺设时形成高频墙纸，判定回炉。
3. 最终稿把每个战棋格收敛成一个完整材料块；内部仅保留不接边的石质斑驳、凿痕、短裂纹或压实土斑。遗迹裂纹再次降低对比，九图总览中不再压过单位轮廓。
4. 九图的道路主轴、地材分区、掩体、单位、选中框和 HUD 在 `1920×1080` 均可快速读取；首领图另以实际 `960×540` 输出复核，无关键裁切或重叠；没有半块结构、跨格接线或软融合边缘。

最终九图接触表：`UnityProject/Artifacts/ArtTile41/NineMaps/academy_nine_maps_contact.png`  
原始实机图：`UnityProject/Artifacts/ArtTile41/NineMaps/*_1920x1080.png`
低分辨率复核：`UnityProject/Artifacts/ArtTile41/NineMaps/core_finale_capture_960x540.png`

## 有向阴影与运行时规则

- `AcademyBattlefieldLayoutCatalog.FloorAsset` 只返回 `academy_block_{family}_{a-d}`，`quarterTurns=0`。
- 地板 UV 固定为完整 `(0,0,1,1)`，不再采样多格宏块子区。
- 旧压边覆盖层退出当前独立地板运行时，避免旋转带固定光照的 PNG。
- 北侧墙与台阶只使用固定朝向的 `academy_wall_straight`／`academy_stairs_2x1`，全部 `QuarterTurns=0`。

## 资产与机器门禁

- 批次：`terrain_independent_tiles_v20`
- 数量：16/16 `FORMAL`
- 规格：`32×32`、最多 5 色、硬 Alpha、完整单格边缘
- Importer：Sprite、PPU32、Point、Clamp、Uncompressed、无 mipmap
- QA：1×、4×、灰阶、棋盘格、九图实机应用接触齐全
- `validate_occ_art_asset.py`：16/16 PASS
- `validate_occ_art_asset.py --audit-contract`：PASS

## Unity 验收

- 编译：0 error / 0 warning
- 专项 EditMode：17/17 PASS
- 全量 EditMode：644/644 PASS
- PlayMode：1/1 PASS
- Console：无 error
- Dirty scenes：0
- `CombatPrototype.unity`：未保存

## 解锁下一步

恢复固定种子 `240824` 的低风险、战斗偏好、资源偏好三条真人完整路线，按真实游玩记录调整学院第一层风险／奖励数值；地图地板不再作为平衡任务的默认改动范围。
