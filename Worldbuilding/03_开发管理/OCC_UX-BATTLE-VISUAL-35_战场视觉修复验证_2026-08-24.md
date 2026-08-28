# UX-BATTLE-VISUAL-35 战场视觉修复验证（2026-08-24）

## 结论

本轮三个问题均确认是运行时应用合同错误，不是正式 PNG 本体损坏，因此没有重画、替换或重新晋级任何美术资产。

- 共享详情页把“距父容器左上角”的坐标传给左中锚点，导致资源芯片、术式／物品详情与指标图标出现系统性纵向偏移。
- 单位 RawImage 原先作为单格子级存在；每个格子的地面、覆盖、物件和单位按整格交错排序。64px 单位整画布及动作位移越出本格后，会被后创建的相邻格地板覆盖，看起来像人物贴图超过范围后消失。
- `AetherMarked` 地面把三向 T 形刻阵当作整条横线重复铺设，同时把四张局部刻阵铺满所有普通格，形成整排下岔和重复 U 形电路毯。

## 修改

- `FormalUiKit` 新增明确的 `TopLeftIconSlot`；顶部定位调用全部迁移到该入口，按钮内图标继续使用原左中锚点。
- `FormalBattlefieldView` 将地面改为格内独立子层，保留透明根点击面；所有单位进入棋盘级“单位独立层”，该层始终位于全部格子地板之后，单位自身按屏幕行排序。单位仍使用完整 64×64 UV、整数倍和脚底锚定，仍由战场总视口裁切。
- `BattlefieldCellPresentation` 增加离散地面旋转字段。以太标记地图仅在 `x=6`／`y=4` 绘制一横一竖主线路，横线使用直线资产 90° 旋转，中心只用一张十字；其余格恢复低密度学院庭院石地。

## 自动验证

- Unity 编译：`0 error / 0 warning`。
- 聚焦 EditMode：`55/55 passed`。
- 全量 EditMode：`629/629 passed`。
- PlayMode：`1/1 passed`。
- Console：无 error。
- 场景：`CombatPrototype.unity`，`dirty=false`，dirty scenes `0`；未保存场景。

新增／更新的回归合同覆盖：顶部图标锚点、单位独立层位于所有格子之后、地面仍无 Unity Outline、以太地图单一十字／旋转直线／安静普通格，以及既有完整单位画布与整数缩放。

## 实机视觉证据

- `UnityProject/Artifacts/VisualAudit35/after_combat_1920x1080.png`
- `UnityProject/Artifacts/VisualAudit35/after_combat_960x540.png`
- `UnityProject/Artifacts/VisualAudit35/final_aether_1920x1080.png`
- `UnityProject/Artifacts/VisualAudit35/final_aether_960x540.png`

双分辨率确认：战斗指令／资源图标没有越出按钮或槽位；单位完整轮廓可以跨格显示且不再被相邻地面切断；以太标记地图在 960×540 可清楚读取为单一十字主回路，普通区域保持安静，不再出现整屏重复刻阵。

## 后续

恢复学院第一层固定种子 `240824` 三路线实机平衡采样。若后续只在战场视口最外边缘看到裁切，应按镜头安全区另立任务；视口裁切与本次“相邻地板覆盖单位”不是同一缺陷。
