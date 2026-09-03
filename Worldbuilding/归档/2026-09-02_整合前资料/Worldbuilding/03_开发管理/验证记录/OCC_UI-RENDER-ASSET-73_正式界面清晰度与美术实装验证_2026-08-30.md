# UI-RENDER-ASSET-73 正式界面清晰度与美术实装验证

日期：2026-08-30  
范围：剧情／肉鸽共用正式 UI 表现层；不改玩法、数值、地图、存档、正式图片原料或玩家文案。

## 完成内容

- 正式正文从 12px 细像素字体切换为 `SimHei` 阅读字体；Fusion Pixel 保留为可选展示字体。正式字号下限为 16，960×540 紧凑模式下限为偶数 20；战斗资源、行动序列、指令和消耗字号同步提高。
- Canvas `referencePixelsPerUnit` 对齐 `32 PPU × 4` 逻辑像素；语义图标、战斗反馈、奖励图标、详情图标、章节横幅、角标、空状态插图和背包图标统一按原生尺寸整数倍显示。
- `FormalUISkin16` 不再只做加载审计。面板、按钮、页签、槽位、资源条和焦点框使用正式九宫格边框层；浅纸面保留阅读底色，正式皮肤关闭不透明深色中心。按钮普通、悬停、按下、禁用与选中状态切换对应 Sprite。
- `inventory` 背景用于整备页面；页面装饰按 startup／landing／map／briefing／inventory／settlement／archive／settings 分配，不再把全部页脊、尺、纸夹和折页堆到每一页。
- 六张空状态插图均有正式语义落点：档案空盘、空背包、无路线、空奖励箱、空整备架与锁住的文书袋。
- 五类章节横幅与六类章节角标按教学、工坊、医务、郊野、封存和奖励语义映射到节点与奖励界面。

## 自动验证

- Funplay Unity 身份门禁：`Application.dataPath=E:/数据库/OCC_Codex/UnityProject/Assets`；活动场景 `Assets/Scenes/CombatPrototype.unity`。
- Unity 编译：错误 0，警告 0。
- 聚焦 EditMode：79/79 通过（字体、正式皮肤、按钮状态、整数尺寸、周边资产、节点材质映射）。
- 全量 EditMode：677/677 通过。
- Console error：0。
- Dirty scene：0；`CombatPrototype` 未保存、未改脏。
- `git diff --check`：无空白错误。

## 未执行项

- 按项目约定未自行进入 Play Mode，因此没有生成新的运行态双分辨率截图。现有代码、Importer、资源绑定和 EditMode 合同已通过；下一次用户批准的真实运行接触只需检查局部信息密度与审美，不再承担基础接线验证。

