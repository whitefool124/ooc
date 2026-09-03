# OCC UI-UX-POLISH-66 交互反馈精修验证

日期：2026-08-29  
范围：剧情／肉鸽共用表现层；不改玩法、数值、地图、存档、按钮业务条件或执行顺序。

## 玩家阅读目标

玩家从输入后应立即分辨“命中并执行”“输入被忽略”“操作被拒绝及原因”“结果已确认”，提示层不得遮挡或截获底层可点击目标。视觉使用学院档案的暖纸、旧金、确认绿与拒绝红；蓝晶与冷青能量不作为通用反馈模板。

## 实现结果

- 鼠标反馈仅响应左键，右键／中键不会误播点击动画。
- 键盘／手柄 Submit 增加短按压脉冲，并保留正式焦点框与六帧局部确认反馈。
- 禁用点击同时播放按钮内 `rejected` 六帧标记并上报明确原因；禁用按钮不再发生按压位移。
- 短时提示条关闭 `CanvasGroup.blocksRaycasts`、`interactable` 和全部 `Graphic.raycastTarget`，不拦截下层交互。
- 消息停留由 1.35 秒起步，超过 18 个可见字符后按字符延长，最长 3.25 秒。
- 连续反馈替换及布局变化会清理提示根、Graphic 与 RectTransform Tween，避免残留动画。
- `RuntimeUiEventSystem` 仅在 Play Mode 使用 `DontDestroyOnLoad`，Funplay 编辑态接触不再抛异常。

## 自动验证

- 新增聚焦回归：3/3 PASS。
- `FormalUiThemeTests`：38/38 PASS。
- 全量 EditMode：658/658 PASS，0 failed，0 skipped。
- 编译：0 error，0 warning。
- Console：0 error。
- Unity 身份：`E:/数据库/OCC_Codex/UnityProject/Assets`。
- 活动场景：`Assets/Scenes/CombatPrototype.unity`；Play Mode false；dirty scene 0。
- 未保存场景。

## Funplay 接触

- `UnityProject/Artifacts/UiUxPolish66/contacts/ui_interaction_polish_1920x1080.png`
- `UnityProject/Artifacts/UiUxPolish66/contacts/ui_interaction_polish_960x540.png`

人工检查：五类输入状态均处于安全区；960×540 无裁切；焦点黄、拒绝红、确认绿可在灰阶之外通过边框／叉号／对勾结构继续区分；短、长提示不遮挡卡片，文字与局部反馈未抢占战场信息层级。

## 后续建议

不继续叠加通用反馈装饰。下一轮应依据真实页面中的具体误触、焦点丢失或文案截断定点修复；如无证据，转入明确功能或内容任务。
