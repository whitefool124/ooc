# OCC M-A15 学院与外围大地图构图及动态节点合同 v0.1

> 状态：`TECHNICAL QA PASS / PRODUCT VISUAL REJECTED / SUPERSEDED BY M-A16`；沿海／半岛地理方向继续保留，但 v01 运行画面因局部镜头、路线蜘蛛网、控件拥挤和地标层级不足被产品否决，不得作为正式地图完成证据。
> 范围：仅 `rogue11` 学院首区大地图表现与交互；不改变 40 节点权威拓扑、节点内容生成、时序、货币、许可或首领门槛。

## 1. 玩家一秒阅读目标

玩家首先读出“自己位于一所拥有外围街区、院墙、建筑群、荒野训练区和封存高塔的学院”，其次读出当前区域、可前往方向和终点；节点类型、风险、时序与奖励只在悬浮或选中后进入详情。

## 2. 固定地理与动态内容

### 固定层

- `1600×900` 原生像素地理底图，在 Unity 逻辑画布中显示为 `3200×1800`。
- 学院外围运输站与服务街、学院正门、中庭宿舍、教学档案、实训工坊、市集医务、校园郊野、高塔外环和封存高塔。
- 院墙、道路、水渠、运输轨道、桥梁、门廊、庭院和中性节点锚点。
- 40 个空间锚点与连接的地理走向；起点和首领空间位置固定。

### 每局动态层

- 普通战、精英、事件、服务、宝藏等节点类型分配。
- 战斗编成、事件条目、奖励、风险、时序成本、恢复、许可来源和商店内容。
- 当前、可达、清理、访问、权限不足、已知与未知状态。
- 精确名称、详情文本、动态路线亮度和测绘揭示。

底图中的锚点只能表现为路口、门廊、站台、空庭、设备平台或界标，不得预先画成商店、宝箱、战斗或事件。

## 3. 地理构图

- **左下／西南：** 学院外围服务街、运输站、仓库边界与学院正门，形成地图的外部世界入口。
- **中央：** 中庭、宿舍和学院主路；拥有最大的公共留白和最多跨区路线。
- **左上／西北：** 教学楼、档案馆、旧校舍与连廊；建筑密度较高但道路清楚。
- **上中／北部：** 训练场、校准工坊、设备院和服务轨道；硬质地面与工业设施占主导。
- **左下内侧／南部：** 市集、医务站、补给棚和公共设施；安全黄只用于服务识别。
- **右下／东南：** 校园郊野、废弃练习场、石路、水渠与院墙缺口；自然覆盖工业遗迹。
- **右上／东北：** 高塔外环、权限门和封存高塔；塔身是全图唯一强垂直终点轮廓。

起点不放在地图绝对边缘，而位于学院正门进入中庭后的首个交通节点，以保持现行“中庭与宿舍”起点语义。学院外围仍有可进入节点，通过两条以上路线接回中庭、市集或郊野。

## 4. 美术方向

- **叙事与材质：** 近代魔法工业学院；石砌教学建筑、灰泥宿舍、铁制连廊、校准设备、运输轨道与可维护的以太管线并存。不是中世纪城堡、哥特修道院、蒸汽朋克机械堆或现代电子园区。
- **形体：** 学院围墙与主路形成大环；中庭是中央负空间；封存高塔形成右上垂直终点；外围轨道和郊野形成不规则边缘。
- **色彩：** 煤灰、铁黑、冷石灰和氧化棕承担地理主体；冷青只用于实际运行的以太设施与后续路线层；安全黄只用于服务设施和工程警示；锈红只用于封存威胁与危险设施。
- **明度：** 道路和公共庭院比建筑屋顶略亮；节点锚点周围必须存在可叠加图标的安静明度块；高塔区域更暗但轮廓最强。
- **视角：** 轻度斜俯视、近似正交；建筑保留可辨屋顶和立面厚度，但道路与节点位置不得被透视遮挡。
- **密度：** 每区最多三个主地标、一个材质故事细节和一个受控色彩强调。道路、锚点和区域边界优先于窗户、瓦片和装饰。

## 5. 40 节点空间预算

| 区域 | 锚点数 | 空间职责 |
| --- | ---: | --- |
| 中庭与宿舍 | 7 | 起点、公共庭院、宿舍门廊和四区交换 |
| 教学与档案 | 7 | 教学楼入口、连廊、档案馆侧门与旧校舍 |
| 实训与工坊 | 7 | 训练台、设备院、工坊门和服务轨道 |
| 市集与医务 | 6 | 补给街、医务入口、公共棚与南门路线 |
| 校园边缘与郊野 | 7 | 石路、水渠、废弃练习场、外围缺口与绕行路线 |
| 封存区与高塔 | 6 | 外环门、维护站、权限门、塔前平台和首领终点 |

每个锚点显示直径预算为原生 `24–32px`，相邻锚点中心至少间隔 `72px`；已知名称不常驻在底图上。动态连接必须沿道路、桥梁或连廊走向，不允许穿过建筑主体。

## 6. 拖动与缩放合同

- 地图视口不要求一屏展示全部地图；默认镜头聚焦当前节点所在区域。
- 左键在空白处拖动；中键可从任意位置拖动；触控板使用相同拖动合同。
- 指针移动超过参考分辨率 `10px` 后进入拖动，取消本次节点点击。
- 缩放采用离散档位，并保证最终屏幕像素尽量保持 1×／2×整数采样；不使用无级模糊缩放。
- 缩放围绕指针位置；拖动边界禁止暴露地图外空白。
- 提供“当前位置”回中、六区快速定位和折叠详情栏。
- 节点首次单击只选中并显示详情；再次确认才前往，避免拖动结束误触移动。
- 键盘方向键／WASD平移；手柄焦点切换节点时自动保持目标进入安全视口。

## 7. 构图稿生成提示词

Use case: stylized-concept
Asset type: OCC tactical roguelite academy world-map composition concept, concept only and not importable final art
Primary request: a large navigable academy campus and surrounding district map that clearly reads as a geographic place before any route UI is added
Scene/backdrop: near-modern aether-industrial academy with an outer transport stop and service street at southwest, main gate and central courtyard, dormitory blocks, teaching and archive quarter at northwest, training grounds and calibration workshops at north, market and infirmary at south, overgrown campus wilds and abandoned practice grounds at southeast, sealed tower outer ring and final tower at northeast
Style/medium: carefully clustered pixel-art concept, restrained detail, no anti-aliased painting look
Composition/framing: wide 16:9, light oblique orthographic top-down view; readable roads, walls, courtyards, bridges and building entrances; central courtyard is the main negative space; sealed tower is the strongest vertical landmark; leave forty quiet circular or square anchor clearings distributed across roads and entrances but draw no gameplay icons and no labels
Lighting/mood: overcast daylight with cool stone shadows, controlled warm service lights, active aether only at a few maintained devices
Color palette: charcoal, iron black, cool grey stone, oxidized brown; restrained cyan for operating aether, safety yellow for service infrastructure, rusty red only around sealed danger
Materials/textures: stone academic buildings, plaster dormitories, iron walkways, rail sidings, maintained conduits, worn paving, sparse vegetation reclaiming peripheral facilities
Constraints: fixed geography with multiple loops and cross-region roads; academy exterior content is traversable; space for dynamic node overlays; no baked UI, text, node types, rewards, characters or route lines
Avoid: medieval castle, gothic cathedral, fantasy parchment map, steampunk gear clutter, neon cyberpunk, modern computer screen, tabletop board, strict rectangular node grid, fake text, labels, legends, minimap inset, isometric city diorama, excessive glow, dense roof detail that hides roads

## 8. 构图验收

1. 移除所有节点和路线后，画面仍明显是一张学院及外围的地理地图。
2. 七个主要空间（外围、正门／中庭、教学、工坊、市集医务、郊野、高塔）只凭轮廓可区分。
3. 中庭与高塔形成清楚的起点区域和终点视觉张力，外围不是无意义边框。
4. 画面存在至少 40 个不被建筑遮挡的动态图标落点，并能沿实际道路组织 12 个以上环路。
5. 冷青、黄和红都有系统语义，去掉发光后仍属于近代魔法工业文明。
6. 无地图文字、伪文字、节点图标或任何可被误认为正式运行时信息的生成内容。

## 9. 构图 v01 记录（2026-08-17）

- 文件：`OCC_M-A15_学院与外围大地图构图_概念_v01.png`；仅作构图蓝本，不导入 Unity。
- 通过项：学院及外围无需节点 UI 即可读作地理地图；外围铁路／码头、正门、中庭、教学建筑、工坊设备院、郊野和东北高塔轮廓明确；超过 40 个动态图标落点拥有足够安静空间。
- 需在正式生产时移除：生成稿中的圆形落点、所有可能被误解为节点底座的铺装图案，以及过密的屋顶碎节。
- 产品决定：采用 v01 的沿海／半岛学院地理，以铁路码头和服务街作为外围入口；已同步至学院首区地图拓扑源文件，可据此重绘正式 `1600×900` 像素底图。

## 10. 正式生产与运行时接线

- 正式地图：`UnityProject/Assets/Game/Resources/Art/FormalAcademyAtlas/academy_coastal_atlas.png`，原生 `1600×900`、30 色、硬 Alpha、Point／Clamp、无 Mipmap；没有烘焙节点、路线、文字或奖励信息。
- 正式节点牌：`FormalMapNodeMarkers48` 的 Current／Available／Cleared／Visited／Locked／Known／Unknown 共 7 张 `48×48` 独立像素资产；状态由形态和色彩表达，节点类型图标由运行时叠加。
- `AcademyMapVisualLayout` 固定 40 个地理锚点，六区数量严格为 `7/7/7/6/7/6`；节点类型、名称、风险、奖励和内容仍按种子动态装配。
- `RogueMapViewportController` 已接入 `FormalRogueliteUi`：左／中键拖动、10px 拖动阈值、1×／2×离散缩放、指针中心缩放、边界夹紧、视角重建保持、当前位置回中、六区图标定位、WASD／方向键／手柄平移。
- 地图节点由旧 `154×78` 文字卡收敛为 `96×96` 图标牌；名称、类型、时序与摘要进入悬浮／右侧详情。详情栏可收起，地图视口随之扩展至 1872 逻辑像素宽。
- QA：地图与节点资产机器报告 `QA_PASS`；聚焦 EditMode `41/41`、全量 EditMode `542/542`、PlayMode `1/1`；Funplay 编译 `0 error / 0 warning`，Console 无 error，活动场景 `0 dirty` 且未保存。
- 运行截图：`OCC_M-A15_学院沿海大地图_1920x1080_v01.png`、`OCC_M-A15_学院沿海大地图_960x540_v01.png`；40 锚点与节点标记审查表同目录保存。

## 11. 产品视觉否决记录（2026-08-17）

- 否决范围：M-A15 v01 的运行界面与信息层级；沿海／半岛学院的地理愿景不撤销。
- 主要问题：首屏只显示局部建筑，玩家不能一秒读出半岛全貌；全拓扑粗实线穿过建筑并压过地理；40 个大节点牌、七状态文字图例、六区定位和详情面板同时常驻；建筑和路面明度接近，七个地标没有形成主次；右侧详情永久压缩地图。
- 后续：M-A16 改为 `1536×864` 正式全貌底图在 1872×866 地图视口中以整数 1× 完整显示，2× 才进入拖动巡查；全拓扑仅保留低对比细线，当前／可达路线局部高亮；节点牌缩至 32px，详情默认隐藏并按选择浮出；区域定位折叠，不再占据首层。
