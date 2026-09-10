from audit_support import *
import re

roots = [ROOTS['global_lark'], ROOTS['project_lark']]

def change(p, fn, why):
    old = read(p)
    new = fn(old)
    if new != old:
        edit(p, new, why)

def front(body):
    return body.split('---', 2)[1]

def metadata_version(s):
    head, body = s.split('---', 2)[1:]
    m = re.search(r'^version: (.+)\n', head, re.M)
    if not m:
        return s
    head = head[:m.start()] + head[m.end():]
    version = m.group(1)
    if re.search(r'^metadata:\s*$', head, re.M):
        head = re.sub(r'^metadata:\s*$', 'metadata:\n  version: ' + version, head, count=1, flags=re.M)
    else:
        head += 'metadata:\n  version: ' + version + '\n'
    return '---' + head + '---' + body

auth = '认证、身份、scope 或配置问题时读取 {link}；常规业务沿用既定身份并显式传 `--as`，不预先重登。高风险确认按完整会话中已有的具体授权处理；真正的权限或审批拒绝不得绕过。'
for root in roots:
    for p in root.rglob('*.md'):
        def common(s):
            if p.name == 'SKILL.md':
                s = metadata_version(s)
            lines = s.splitlines()
            for i, line in enumerate(lines):
                if ('CRITICAL — 开始前 MUST' in line or line.startswith('开始前先读 ') or line.startswith('> **前置条件：** 先阅读 ')) and 'lark-shared/SKILL.md' in line:
                    link = re.search(r'\[[^\]]+\]\([^)]*lark-shared/SKILL.md\)', line)
                    if link:
                        lines[i] = auth.format(link=link.group())
            return '\n'.join(lines) + '\n'
        change(p, common, '飞书入口按需加载认证规则；将版本移入兼容的 metadata 并保留原值')

qr = '**授权链接**：原样展示 CLI 返回的 URL，不编解码或重拼 query。用户要求二维码或移动端扫码更方便时，使用 `lark-cli auth qrcode` 生成 PNG 并同时展示链接；二维码不是每次授权的必经步骤。'
split = '''#### Agent 代理发起认证

仅在任务需要且当前认证/权限确实缺失时发起授权，使用所需的最小 `--scope` 或 `--domain`：

```bash
lark-cli auth login --scope "<required_scope>" --no-wait --json
```

立即向用户展示返回的 `verification_url`；`device_code` 仅用于当前认证流程，不写入报告或长期保存。若客户端只能在最终回复展示链接，先交还控制权；能展示中间结果时可继续独立工作，不因授权流程强制结束整项任务。

用户完成授权后，由 agent 执行 `lark-cli auth login --device-code <device_code>` 并核对成功结果。不要在用户尚未看到链接时阻塞轮询。链接或 code 过期后，保留原 scope/domain/exclude 选择重新发起；不复用过期凭据。

'''
for root in roots:
    for p in (root/'lark-shared').rglob('*.md'):
        def shared(s):
            s = re.sub(r'^3\. \*\*授权 / 配置类 URL 必须配二维码\*\*.*$', '3. ' + qr, s, flags=re.M)
            s = re.sub(r'^\*\*URL 转发规则\*\*.*$', qr, s, flags=re.M)
            s = s.replace('首次使用需运行 `lark-cli config init` 完成应用配置。', '仅在尚未配置或用户要求重新配置时运行 `lark-cli config init`；已有配置直接复用。')
            start = s.find('#### Agent 代理发起认证')
            if start >= 0:
                m = re.search(r'^## (?!#)', s[start:], re.M)
                end = start + m.start() if m else len(s)
                s = s[:start] + split + s[end:]
            s = s.replace('2. **向用户确认**：把 `error.action`、`error.risk` 和关键参数展示给用户，明确告知"这是高风险操作"，等待用户显式同意', '2. **核对授权**：检查完整会话中对动作、目标、范围与影响的具体同意。已有授权且未实质变化时继续；缺少授权时，先完成只读准备并展示具体影响，再询问。exit 10 本身不构成授权。')
            s = s.replace('3. **用户同意** →', '3. **已有有效授权或取得新同意** →')
            return s
        change(p, shared, '认证按实际缺失触发，二维码可选，跨轮具体授权有效')

slides_body = '''
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
'''
for root in roots:
    p=root/'lark-slides/SKILL.md'
    edit(p, '---'+front(read(p))+'---\n'+slides_body, '删除强制二读、图片字数配额和固定审美，保留 XML 与读改写合同')
    p=root/'lark-slides/references/planning-layer.md'
    change(p, lambda s: s.replace('新建演示文稿或大幅改写页面时，必须先写', '复杂新建、多页重排或需要后续复用计划时，建议先写').replace('只要任务会重排多页、生成新 deck、替换整页结构，仍然需要规划层。','是否保存规划层取决于复杂度；简单新建也可直接组织内容并生成 XML。').replace('## Required Flow','## 使用规划文件时的流程').replace('Create a close-enough image with the image generation tool instead of a real logo.', 'Use the brand name as plain text; do not generate an imitation logo.'), '规划文件按复杂度选择，禁止生成仿真 Logo 替代真实品牌资产')
    p=root/'lark-slides/references/asset-planning.md'
    change(p, lambda s: s.replace('For a 6-page technical or business deck, plan assets on at least 3 pages when the content allows.', 'There is no minimum asset count; text-led pages are valid.').replace('but do not execute the search unless the user separately requests real assets.', 'and use available search when real assets would help fulfill the authorized task; no separate approval is needed for ordinary read-only research.').replace('`mock_required_by_intent`: when the user does not provide concrete values but asks for data expression, charts, trends, comparisons, or distributions, use mock data in a native `<chart>`.', '`data_missing`: when an analytical request lacks concrete values, retrieve authorized source data or ask for the missing input; do not invent values merely to fill a chart.').replace('2. If no asset exists, immediately render `fallback_if_missing` with the planned generated close-enough image. Supported standard data visuals still use native `<chart>`; other fallbacks may use the image generation tool to create an approximate image.', '2. If no asset exists, use a truthful fallback: text identity for a missing logo, a clearly labeled original schematic for an explanation, or report the missing evidence. Never generate a substitute screenshot, paper figure, logo or measurement and present it as real. Image generation is suitable for requested illustrations.').replace('6. If the image generation tool is unavailable or fails, degrade to an XML-native fallback instead of leaving a blank: native `<chart>` for data, otherwise a simple in-card shape/text placeholder sized to fill `visual_focus`.', '6. If image generation fails, use a meaningful native visual or concise text where it fulfills the request; report any required visual that remains unavailable. Data charts still require real data unless this is explicitly a placeholder/template.'), '素材与数据来源真实，移除配额和自动仿造素材回退')

for root in roots:
    p=root/'lark-doc/SKILL.md'
    change(p, lambda s: s.replace('先完整执行创建工作流，**简单任务不是跳过的理由**；', '复杂文档按创建工作流组织；简单文档可直接使用 [`+create`](references/lark-doc-create.md) 的受支持格式，保留内容核验与写后回读；'), '简单文档允许直接创建，复杂流程渐进加载')
    p=root/'lark-doc/references/lark-doc-create-workflow.md'
    change(p, lambda s: s.replace('**CRITICAL：从零创作文档时按下述步骤依次执行，不可跳步。**', '以下是复杂文档的制作流程。简单文档可直接读 `lark-doc-create.md`、按受支持格式创建并回读；只有使用草稿脚本时才必须满足其输入契约，不能把脚本契约扩展成所有文档的前置审批。'), '限定完整创建工作流的适用范围')

print('Lark first pass saved')
