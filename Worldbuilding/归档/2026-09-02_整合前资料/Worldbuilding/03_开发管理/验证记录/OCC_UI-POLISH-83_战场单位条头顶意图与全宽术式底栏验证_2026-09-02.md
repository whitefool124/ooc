# UI-POLISH-83 战场单位条、头顶意图与全宽术式底栏验证

日期：2026-09-02  
归属：剧情／肉鸽共用战斗 UI 表现层

## 结果

- 战场单位生命／护盾条不再复用会在小尺寸下压住填充的 `bar_segment_health/shield`。正式结果为浅中性轨道、锈红／灰绿纯色填充、2px 内缩、25%／50%／75% 刻度、比例宽度过渡、变化闪烁和落点闪标。
- 128px 参考格下生命条为 `120×22px`，护盾条为 `120×14px`；64／96／128／160px 战斗格继续按比例缩放并保持在格内。
- 敌人意图徽章相对逻辑格顶部上移 `22px`，16px 原图继续严格以整数 `2×` 显示为 32px，徽章下沿位于头像透明净空内，不再遮挡头部。
- `combat.commands` 从 1408px 扩为 1888px，横跨 1920 参考画布的底部操作带；底栏以上仍保持 1440px 战场／480px HUD。
- 术式组为 1208px，八个槽始终以 `4×2` 呈现，单卡 `288×70px`；已装备卡显示键位、32px 术式图标、最长八字名称、行动点图标／数值和个人魔力图标／数值，空槽保持同一网格节奏。
- “背包／搜索 [B]”从右下删除，入口迁为顶部抬头的“背包 [B]”；搜索功能保留在展开后的背包页。

## 实机纠错

第一次 Play Mode 接触发现单位条对子节点调用固定 `SetTopLeft` 内缩，父条后续缩放时造成红／绿填充被拉成大块。最终实现保留 Stretch 锚点，只设置 `offsetMin/offsetMax = ±2px`，最终截图确认填充严格留在轨道内。

## 自动验证

- Funplay 主工程身份：`Application.dataPath = E:/数据库/OCC_Codex/UnityProject/Assets`。
- 编译：0 error，0 warning。
- 聚焦 EditMode：7/7 Passed。
- 全量 EditMode：706/706 Passed。
- PlayMode：1/1 Passed。
- Console：0 error。
- Dirty scenes：0。
- 新增合同回归：单位条轨道使用 `ResourceTrack`，填充不存在旧正式皮肤覆盖，2px 内缩、三条刻度和变化落点对象全部存在。

## 双分辨率接触

- `UnityProject/Library/FunplayMcp/Screenshots/UI-POLISH-83-final2-1920x1080.png`
- `UnityProject/Library/FunplayMcp/Screenshots/UI-POLISH-83-final-960x540.png`

两档画面均确认单位条颜色／比例、头顶意图净空、八槽术式卡、全宽底栏和右侧 HUD 的主要信息可辨。Play 审核只改运行态与截图，不保存场景或存档。
