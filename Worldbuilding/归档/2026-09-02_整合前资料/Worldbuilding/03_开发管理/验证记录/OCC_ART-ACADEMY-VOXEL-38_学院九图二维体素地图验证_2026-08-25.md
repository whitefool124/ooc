# ART-ACADEMY-VOXEL-38 学院九图二维体素地图验证

日期：2026-08-25  
范围：剧情／肉鸽共用学院战场表现层；不改 12×9 逻辑格、战斗数值、节点内容或存档。

## 结论

通过。学院九张战斗地图已由“连续底纹分区”升级为可跨地图复用的二维体素式结构语法：地面只承担低权重材质，透明压边和模块化墙／端头／转角／台阶承担边界、高度与入口。审美取舍以单位、路线和阻挡的一秒可读性优先，不以学院设定装饰密度为目标。

九图实机接触证据：`UnityProject/Artifacts/Terrain38/NineMaps/terrain38_nine_maps_contact.png`。

## 资产与运行时

- 5 件 `32×32` 四邻接压边：直边、转角、对边、三向、包围；由邻接掩码选择并以 90° 旋转复用。
- 4 件模块化结构：直墙、端头、转角与 `2×1` 台阶；九图仅保存资产、坐标、旋转，不烘焙关卡整图。
- 返工 4 件 `3×3 / 96×96` 底材：夯土 A/B 收窄到 4 色，遗迹 A/B 收窄到 5 色；实机不再出现贯穿地图的斜向车辙和重复大黑洞。
- `academy_gate_3x2` 因门柱视觉占格与永久阻挡不一致被拒绝部署，状态为 `PROTOTYPE`；Unity 正式路径副本已移出，源稿仍可追溯。
- 新增机器合同角色 `terrain_adjacency_overlay_32`、`modular_structure_32`，同步写入唯一美术规范并通过合同审计。

## 实机视觉与交互复核

- 九图缩览均能先读出石路／庭院／夯土／遗迹分区、主通路与边界，再读材质细节；单位、选中框、范围框和战利品不被结构遮挡。
- 0.75 格镜头安全外沿使顶行单位和墙体可被查看，不改变战斗坐标或命中。
- Funplay RenderTexture 读回在本轮长会话中返回旧黑帧并产生工具侧释放警告；已改用 Windows Graphics Capture 对 Unity 最大化 Game View 留存四张返工地图，再裁出纯 Game View 证据。实机窗口显示正常，黑帧不属于游戏渲染。
- 热切换新 `CombatState` 暴露一帧 `ActiveUnitId == null`；`CombatState.GetUnit` 现对空 ID 返回 null，敌方回合 Update 同时跳过空行动者。复验在 `active=<null>` 条件下 Console 无 error。

## 美术与工程门禁

- `occ-art-manifest-v1`：13 项 `FORMAL`；尺寸、硬 Alpha、色数、1×／4×／灰阶／棋盘格、来源哈希、稳定 GUID、应用接触全部登记。
- 机器验证：13/13 PASS；`occ-art-contract-audit-v1` PASS。
- Importer：13/13 为 Sprite、Point、Clamp、PPU32、Uncompressed、无 mipmap。
- Unity 编译：0 error / 0 warning。
- EditMode：643/643 PASS。
- PlayMode：1/1 PASS。
- Console：清空缓存后热切换空行动者冒烟，无 error。
- 场景：`CombatPrototype.unity` dirty=false；dirty scenes=0；未保存场景。

## 下一步

地图视觉任务完成并冻结。下一主任务为固定种子 `240824` 的学院第一层三路线真人完整游玩与平衡采样；只根据真实游玩数据调整节点／经济／战斗数值，不默认返工地图结构或底材。
