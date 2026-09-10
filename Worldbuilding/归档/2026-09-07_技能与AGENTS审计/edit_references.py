from audit_support import *
import re

def change(p, fn, why):
    old=read(p); new=fn(old)
    if old!=new: edit(p,new,why)

for root in [ROOTS['global_lark'],ROOTS['project_lark']]:
    p=root/'lark-slides/references/visual-planning.md'
    change(p,lambda s:s.replace('新建演示文稿或大幅改写页面时，在 `slide_plan.json` 完成后、生成 XML 前读取本文件。','需要设计布局时按需读取本文件；使用 slide_plan.json 时将计划映射为 XML。').replace('For 4 or more pages, use at least 4 different layout structures when the content allows.','Choose layout variation for content relationships, with no minimum number of layouts.').replace('and fill the content area densely with a card grid rather than leaving large empty space.','and use spacing appropriate to the content; no dense-card-grid requirement.').replace('Do not use `<shape>` to build pictorial visuals like mock photos or fake objects. Use the image generation tool instead.','Use native shapes for diagrams and image generation for requested illustrations; neither may impersonate real screenshots or evidence.').replace('- Do not place a `rect` or `line` for dividing or decorative purposes directly under a `headline` or `title`.\n- Do not use section bands, horizontal bars, vertical bars, or page-edge strips.','- Use separators and section bands only when they help grouping or follow the user’s template; avoid decoration that competes with content.'),'视觉参考取消版式数量与高密度配额')
    p=root/'lark-doc/references/lark-doc-create-workflow.md'
    change(p,lambda s:s.replace('无论创建成功、失败或被阻塞，只要 Step 4 已返回 `work_dir`，就先离开该目录，再使用当前运行时的文件删除能力精确删除整个 `work_dir`；不要使用通配符，也不要删除目录外的用户原始文件。','创建成功且回读验证完成后，可清理本任务创建且无需保留的临时草稿；失败、部分成功或仍需恢复时保留 work_dir 及请求/返回证据。清理前核对绝对路径位于本任务临时范围内，只删除 agent 自建内容，保留用户源文件和所需交付物。'),'失败时保留恢复证据，不无条件删除草稿目录')
    p=root/'lark-base/SKILL.md'
    change(p,lambda s:s.replace('## 身份与权限降级','## 身份与权限恢复').replace('user 身份报资源级无访问且无授权恢复提示时，才可用 `--as bot` 重试一次；bot 仍失败就停止重试并按权限错误处理。','user 身份报资源级无访问时，保持原身份并说明需要资源所有者授权；不自动切换 bot 重试。'),'删除与身份延续规则冲突的ACL失败自动换bot')
    p=root/'lark-whiteboard/SKILL.md'
    change(p,lambda s:s.replace('> - 运行 `lark-cli --version`，确认可用，无需询问用户。\n> - 运行 `npx -y @larksuite/whiteboard-cli@^0.2.13 -v`，确认可用，无需询问用户。','> 复用已核实的 CLI 环境；版本或兼容性不明时查询。仅所选编辑/DSL流程依赖 whiteboard-cli 时检查该工具，普通读取和导出不预先运行 npx。'),'只读导出不触发无关npx依赖启动')
    p=root/'lark-openapi-explorer/SKILL.md'
    change(p,lambda s:s.replace('严格按以下步骤逐层检索，**不要跳步或猜测 API**：','按需检索官方资料，**不要猜测 API**。已持有准确官方 API 页面或当前已核实 schema 时可直接使用；不知道入口时再从索引逐层定位：'),'复用已核实的官方页面，无需每次重走完整索引')
    p=root/'lark-wiki/SKILL.md'
    change(p,lambda s:s.replace('**一旦任一页累计到至少 1 条精确匹配就停止翻页**','**继续分页至覆盖当前可见范围，防止遗漏后续同名空间**').replace('**无论命中 1 条还是多条，发起删除前都必须把候选（`name` + `space_id` + `description` + `space_type`）列给用户，由用户明确选定一个 `space_id` 再执行**。不要因为"只命中一条"就自动执行删除。','核对候选（`name` + `space_id` + `description` + `space_type`）、下属节点影响与完整会话中的删除授权。目标已唯一明确且影响已被授权时执行；存在同名歧义、模糊匹配或未授权影响时才展示候选询问。'),'完整分页消歧，已指定目标与影响无需重新选ID')
    p=root/'lark-wiki/references/lark-wiki-delete-space.md'
    change(p,lambda s:s.replace('执行前必须反复确认','执行前核对空间 ID、下属节点范围与不可逆删除的具体授权；已有有效授权不重复询问'),'保留不可逆删除授权但移除反复确认')
    p=root/'lark-sheets/references/lark-sheets-write-cells.md'
    change(p,lambda s:re.sub(r'^6\. \*\*REGEX 模式覆盖率验证\*\*.*$', '6. **REGEX 覆盖与语义验证**：批处理前核对源列的实际格式类型，完整检查需要转换的有效记录；缺失、无效和本来不应匹配的数据分别处理并报告数量。不要为了达到 100% 命中率扩大规则、填造值或吞掉错误。',s,flags=re.M),'正则验证覆盖真实有效范围，不强凑100%命中')
    for name in ['lark-note','lark-vc','lark-minutes','lark-workflow-meeting-summary']:
        p=root/name/'SKILL.md'
        def boundary(s):
            s=s.replace('，使用前必读。','；身份选择或权限问题时按需读取。')
            return re.sub(r'\*\*CRITICAL — 开始前 MUST 先用 Read 工具读取 (\[[^\n]+vc-domain-boundaries.md\))\*\*[^\n]*\n(?:>[^\n]*\n)+',r'会议产品边界或 token 路由不明时读取 \1；已有明确资源类型和 ID 时直接使用对应命令。\n',s)
        change(p,boundary,'产品边界明确时不预读整个跨域说明')
    for p in (root/'lark-mail/references').glob('*.md'):
        change(p,lambda s:s.replace('(references/lark-mail-html.md)','(lark-mail-html.md)'),'修正邮件参考同目录链接')
    for p in (root/'lark-drive').rglob('*.md'):
        change(p,lambda s:re.sub(r'\]\(../../lark-wiki/references/lark-wiki-(?:node-get|node-list|node-create|node-delete|move|move-to-drive).md\)','](../../lark-wiki/SKILL.md)',s),'缺失Wiki独立参考改为实际Wiki入口及当前help')
    mappings={
        'lark-mail/references/lark-mail-watch.md':('../../lark-event/references/lark-event-subscribe.md','../../lark-event/SKILL.md'),
        'lark-minutes/references/lark-minutes-speaker-replace.md':('../../lark-vc/references/lark-vc-notes.md','../../lark-note/SKILL.md'),
        'lark-wiki/references/lark-wiki-member-add.md':('(lark-wiki-member-remove.md)','(../SKILL.md)'),
    }
    for rel,(a,b) in mappings.items():
        if (root/rel).exists(): change(root/rel,lambda s:s.replace(a,b),'修复指向不存在参考文件的入口')
    p=root/'lark-event/references/lark-event-im.md'
    change(p,lambda s:re.sub(r'\[([^\]]+)\]\(../../../events/im/message_receive.go\)',r'`events/im/message_receive.go`（CLI 上游源码位置，本技能包不含该源码）',s),'上游源码引用不冒充本地文件')

p=ROOTS['personal']/'remotion/rules/text-animations.md'
change(p,lambda s:re.sub(r'\[([^\]]+)\]\(assets/(text-animations[^)]+)\)',r'\1（示例源码 `assets/\2` 未随当前技能包分发）',s),'缺失示例不再提供失效链接，保留示例名称')

# Discard only incidental EOF normalization from this audit, guarded against later edits.
changes=json.loads(read(OUT/'changes.json'))
kept=[]; incidental=[]
for item in changes:
    p=Path(item['path']); backup=Path(item['backup']) if item['backup'] else None
    if backup and read(p).rstrip()==read(backup).rstrip() and sha(p.read_bytes())==item['after_sha256']:
        p.write_bytes(backup.read_bytes()); incidental.append(item['path'])
    else: kept.append(item)
save_json('changes.json',kept)
save_json('incidental-formatting-restored.json',incidental)
print(json.dumps({'meaningful_changed_files':len(kept),'formatting_only_restored':len(incidental)}))
