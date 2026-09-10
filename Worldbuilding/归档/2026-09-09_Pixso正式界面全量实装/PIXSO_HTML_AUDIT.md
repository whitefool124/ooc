# Pixso 全界面 HTML 桥接审计

来源：Pixso 当前页 `0:1｜OCC｜正式界面总览`，批次 `1788936783136`。该 HTML 仅作为 Unity 实装的布局与视觉核对证据，不作为游戏运行时页面。

## 资源本地化

- 临时 URL：发现 0，保留 0。
- 远程 HTTP(S) URL：发现 0，保留 0。
- 本地化资源：35 个图片与字体文件，均位于 `pixso-html/assets` 或 `pixso-html/fonts`。

## Phase A：机械清理

- Pixso 节点式 `id`：发现 2496 个，移除 2053 个，保留 443 个。
- 保留原因：保留项均由导出页面的显示切换脚本通过 `$('<node_id>')` 或 `getElementById(...)` 引用；删除会破坏页面切换。
- 无引用节点 `id` 已移除。

## Phase B：语义重命名

- 首轮生成辅助类：327 个，全部一对一替换。
- `Pixso-frame/vector/group/rectangle/text/paragraph-*` 类：2807 个，全部一对一替换。
- 使用的语义族：`panel-*`、`layout-panel-*`、`content-panel-*`、`surface-*`、`icon-*`、`module-*`、`label-*`、`copy-*`。
- DOM 类与 CSS 选择器在同一次映射中成对替换；每个元素的类 token 数保持不变。
- 未保留上述 Pixso 节点式类；未新增依赖、状态类或组件体系，CSS 声明与布局值未修改。

## 交付文件

- `pixso-html/0_0.html`
- `pixso-html/assets/`
- `pixso-html/fonts/`
