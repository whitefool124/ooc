---
name: lark-slides
description: "读取、创建或编辑飞书幻灯片；按 /slides/ 路径识别，普通本地 PPTX 使用对应文件技能。"
metadata:
  version: 1.0.0
  requires:
    bins: ["lark-cli"]
  cliHelp: "lark-cli slides --help"
---

# 飞书幻灯片

只处理飞书 Slides；普通 PPTX 使用对应文件技能。先确定本次交付是读取、局部编辑还是新建，按下表读取对应参考，已读且未变化的文档不重复加载。

## 路由

| 任务 | 命令与参考 |
|---|---|
| 读取 XML | [`+xml-get`](references/cli/lark-slides-xml-presentations-get.md) |
| 新建 | [`+create`](references/cli/lark-slides-create.md)，需要逐页添加时用 [`+add-slide`](references/cli/lark-slides-add-slide.md) |
| 已有页面编辑 | [`编辑流程`](references/workflow/slides-editing.md)：局部用 [`+replace-slide`](references/cli/lark-slides-replace-slide.md)，整页重排用 [`+update-slide`](references/cli/lark-slides-update-slide.md) |
| 使用模板/PPTX | [`模板编辑`](references/workflow/template-editing.md) |
| 删除页 | [`+delete-slide`](references/cli/lark-slides-delete-slide.md)；核对 slide_id 和已有删除授权 |
| 回滚 | [`历史版本`](references/cli/lark-slides-history.md)；使用 history_version_id，不能用 revision_id 代替 |
| 图片与截图 | [`图片上传`](references/cli/lark-slides-media-upload.md)、[`截图`](references/cli/lark-slides-screenshot.md) |
| 图表或图标 | [`原生图表示例`](references/xml/slides_chart_demo.xml)、[`IconPark`](references/xml/iconpark.md) |
| 失败或部分成功 | [`排障`](references/workflow/error-handling.md)；先回读状态再重试，防止重复创建 |

## 身份、目标和写入范围

- 用户资源默认显式使用 `--as user`，后续沿用取得 token 的身份。仅在任务选择应用身份时用 bot；遇到权限错误不能自动切换身份。
- 仅认证、scope 或身份问题时读取 [`lark-shared`](../lark-shared/SKILL.md)，不在每次操作前重新登录。
- `/slides/` token 是 presentation ID。带 `--presentation` 的 shortcut 能解析 Wiki URL；原生 API 需先在相同身份下解析 Wiki 节点，确认 obj_type=slides 后取 obj_token。
- 记录 `xml_presentation_id`、`slide_id`、`revision_id`。已有稿继续原链接编辑；`+update-slide` 会删除未写回的元素，局部修改使用 `+replace-slide`。
- 命令参数以当前 `--help` / `schema` 为准。文件参数使用 CWD 内相对路径，避免 shell 拼接大 XML。

## 制作与验证

- 按用户目的、模板和受众安排内容密度与视觉；不强制图片、字数、卡片数量或固定审美。图片、图表和留白应帮助理解，不为了填满版面扩写。
- 新建先确定内容结构；复杂或多页大改可使用 [`规划层`](references/planning-layer.md) 保存可复用计划。简单任务不必创建独立 JSON 计划。
- 有视觉设计需求时参考 [`视觉规划`](references/visual-planning.md)；素材选择参考 [`资产规划`](references/asset-planning.md)。数据、Logo、截图及论文图必须真实可追溯；没有数据时不能凭“需要图表”自动造数。仅示例或模板可用明确标注的占位数据。
- 生成 XML 前读 [`XML 速查`](references/xml/xml-schema-quick-ref.md)；完整协议以 [`schema`](references/xml/slides_xml_schema_definition.xml) 为准。
- 默认画布 960×540，已有稿以实际尺寸为准。文字显式设置 fontSize、color，长文本合理分段并设置 wrap/autoFit；lineSpacing 用 `multiple:xx` 或 `fixed:xx`。
- `<slide>` 的直接子元素是 style/data/note；内容放 data。文字使用 `<content><p>…</p></content>`。rect 是形状，不是容器；叠放元素是平级节点。
- 表格使用 `<table>` 或适合编辑的平级元素；table 设置 width/height，需固定时指定 col width/tr height。td 不可嵌套 shape/img/icon。
- 支持的标准数据图表用原生 chart；不需要图例时省略 chartLegend。渐变使用带百分比停靠点的 rgba。图片标签是 img，src 需 file_token 或支持自动上传的 `@./path` 占位符，不能直接塞外链，单图最大 20 MB。
- 完整 slide XML 写入 create/add/update 前保存本地并运行 [`xml_lint.py`](scripts/xml_lint.py)，`summary.error_count` 必须为 0。警告结合实际用途判断，不通过增加无关内容消除稀疏警告。
- 写后按 [`验证流程`](references/workflow/validation-xml.md) 回读受影响页面，核对页数、内容及关键元素；新建或布局变化使用截图检查可读性、溢出和素材显示。没有新变更或失败不重复扩大检查。
- 最终交付真实链接及验证结果；截图/验证无法完成时说明限制，不宣称已通过。
