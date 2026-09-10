"""Apply the personally maintained instruction changes reviewed in this audit."""
from audit_support import edit, read, ROOT
from pathlib import Path

PERSONAL = Path('C:/Users/FNHF/.codex/skills')

edit('C:/Users/FNHF/.codex/AGENTS.md', '''# 全局个人协作规则

## 执行与授权

- 根据完整会话执行用户目标，完成已授权的实现、必要验证和交付；中途补充是对当前任务的调整，不默认替换原目标。
- 已有具体授权跨轮有效。常规可逆选择自行处理；只有缺失信息会实质改变结果且无法从上下文确定时才提问，并继续独立工作。
- 必须确认时，先完成已授权的准备，给出可审阅的目标、范围与影响，再询问缺失决定。泛化授权不扩张为任意删除、权限变更或发布；向他人发送消息或邮件须有明确授权。

## 技能与项目规则

- 遵守系统和开发者指令；用户明确要求优先于 skill 指导。保留正确项目、用户数据、真实权限和审批边界，不换工具绕过拒绝。
- 按当前任务读取相关源文件与规则，复用仍有效的上下文。小修不自动触发整库阅读、全套技能或无关专项审计。
- 区分技术核对和向用户确认。示例、推荐流程、历史经验不能自行升级为审批门槛；失败后的写入结果未知时先查状态再重试。
- 若 skill 导致暂停、确认或偏离目标，给出实际读取的 SKILL.md 及相关引用路径，引用触发条款，说明是明确要求还是自己的解释。

## 输出与验证

- 默认简洁中文，先说结果，再给必要依据；技术细节用于解释结果。
- 验证与改动相称。完成必要检查后，没有新改动、失败或未解决问题就不反复扩大测试；区分本次证据、历史结果和未验证部分。
- 子代理仅在当前环境允许且授权、任务确有收益时使用；遵守委派、模型和资源限制。

## 模型升级与技能维护

- 用户要求升级模型时，同步审查本文件、相关个人技能及其实际引用；修正已证实的冲突和过时约束，符合要求的内容保留。
- 使用 openai-docs 重新核对用户指定模型的[官方指南](https://developers.openai.com/api/docs/guides/latest-model)。模型细节会更新，不把历史参数当作当前事实。
- 技能维护只保留会影响决策的项目知识、非显然技术约束和准确触发条件；按需路由，不把固定篇幅、工具偏好或重复确认推广为所有任务的硬要求。
- 修改前备份，修改后检查差异、元数据和引用。仅在涉及 API 接入时核对参数与工具兼容性；提示词维护不自动更换模型、升级插件或改写无关配置。

维护依据：2026-09-07 重新核对的 GPT-6 Astra 官方指南与[技能和提示词审计文章](https://x.com/pvncher/status/2095991462416490862)。以上为个人协作约定，不代替每次实际核对的产品文档。
''', '压缩重复协作指令，保留跨轮授权、真实边界及模型升级约定；补充按证据维护技能')

p = ROOT / 'AGENTS.md'
s = read(p)
s = s.replace('每个任务必须写明目标、涉及文件/系统、验收标准和完成后解锁的下一步；不能只写模糊的“做 UI”或“完善战斗”。同一时间只保留一个主任务。',
              '任务需明确目标、涉及文件/系统、验收标准和完成后解锁的下一步；可在任务回复中说明，不为例行小修另建任务文档或改写总案。同一时间只保留一个主任务。')
s = s.replace('先读 `Worldbuilding/策划案/OCC_项目总策划案_v1.0.md` 的“界面布局”与“美术方向”，以及',
              '涉及美术或界面改动时，按需读 `Worldbuilding/策划案/OCC_项目总策划案_v1.0.md` 第7节“界面”和第12节“美术方向与视觉实施合同”的相关内容，以及')
s += '''
## 技能与工具维护

- 文档、AGENTS.md 或技能维护只检查差异、元数据和引用，无需启动 Unity、重新编译或进入 Play Mode。
- Funplay 插件自带的通用工作流须结合本项目解释：保存场景、Play Mode 和正式资产替换仍遵守上面的明确授权要求；通用移动端 UI 建议不改变 OCC 的 PC 像素布局和既有运行时 UI 架构。
- 个人技能按实际任务触发；飞书业务仅加载对应领域。旧 README、归档与工具中的失效路径不构成活动规则，遇到相关引用时核实当前入口。
'''
edit(p, s, '明确任务记录可在回复完成、美术章节实际入口及Funplay通用规则的项目适用边界')

p = PERSONAL / 'funplay-unity-dev/SKILL.md'
s = read(p).replace('完成已授权的 Unity 改动、必要的保存与相称验证。',
                    '完成已授权的 Unity 改动和相称验证；保存与 Play Mode 的权限按当前项目及完整会话判断。')
s = s.replace('后续使用返回的 `instanceId`；', '后续使用返回的 `instanceId` 和显式 `find_method=by_id`，避免失效 ID 回退匹配同名对象；')
s = s.replace('保存交付所需的目标场景或资产并回读。先检查已有脏修改；只有保存会夹带无法隔离的无关用户修改时才需要询问。',
              '已授权持久化时，定向保存目标场景或资产并回读。先检查已有脏修改；项目若要求明确保存授权，核对已有会话是否覆盖。保存会夹带无法隔离的无关用户修改时，先完成可隔离的工作再询问。')
s = s.replace('## 按需读取', '''## 插件边界

插件包版本从当前工程 `Packages/manifest.json`、`packages-lock.json` 与解析后的包信息核对。工具是否可调用以当前连接的 schema 为准；配置中出现端口不等于连接已验证。无需为普通任务导出整个工具表或重新安装插件技能。

插件生成的 `unity-mcp-workflow` / `unity-ui-composition` 是通用指导。保留项目既有 UI 构建方式、目标平台与画布规则，不为满足移动端模板重构现有 PC 界面。源码可以只读检查 `Library/PackageCache`，持久化技能修订放在用户维护的技能目录，不修改包缓存。

## 按需读取''')
edit(p, s, '区分项目授权与通用保存流程，补充显式ID和插件模板适用边界')

p = PERSONAL / 'funplay-unity-dev/references/compile-and-recovery.md'
s = read(p).replace('本参考的行为依据为已检查的 Funplay 0.6.5 源码。升级后检查当前工具 schema 和实现，不把版本快照当作永久约定。',
                    '2026-09-07 已核对 OCC 解析到的 Funplay 0.6.4（`com.gamebooom.unity.mcp`），包括 `CompilationFunctions`、`ScriptExecutionFunctions`、`TestRunnerFunctions` 与服务器指令。其它工程先核对自己的包版本；以下是该版本的行为证据，不是所有版本的永久合同。')
s = s.replace('`request_recompile` 导入外部变更，可能在编译/导入启动后立即返回；启动成功不等于编译完成。',
              '`request_recompile` 导入外部变更；区分“已请求”与明确返回“编译完成”，不要仅凭 HTTP 成功认定新程序集已可用。')
s = s.replace('0.6.5 的 `execute_code`', '已核对的 0.6.4 `execute_code`')
edit(p, s, '以本项目实际Funplay 0.6.4源码替代其它项目版本快照')

p = PERSONAL / 'game-interface-planning/SKILL.md'
s = read(p)
s = s[:s.index('## Working sequence')] + '''## Completion by requested scope

For a text plan, deliver the relevant rules, screen/state relationships and open decisions. For canvas editing, update and inspect the affected design states. Export annotations and synchronize a document only when those outputs are part of the request. Reuse current rules and already-inspected artifacts; a local label correction does not require a new screen inventory or a full document workflow.

Complete the requested canvas or document changes and their relevant verification before returning for review. Do not add a separate approval checkpoint for routine, reversible design choices.
'''
edit(p, s, '移除每次设计都必须导出并同步文档的固定七步，按交付范围选择流程')

p = PERSONAL / 'smbx-145-level-design/SKILL.md'
s = read(p).replace('2. Read or search these local references before changing files:',
                    '2. Read the relevant manual section and a working example for the mechanic being changed; the following are lookup locations, not a required reading stack:')
s = s.replace('3. Read any user-authored design notes in the episode folder before editing. If a note is mojibake, repair the note as UTF-8 text, but keep `.lvl` data ASCII unless the file already proves otherwise.',
              '3. Read the affected episode design notes. If a note is mojibake, identify its encoding before relying on it; repair it only when that is within the task, preserving a backup. Preserve the existing `.lvl` encoding and serialization.')
edit(p, s, '将旧版格式保护与全套手册预读、无关笔记修复区分开')

p = PERSONAL / 'remotion/rules/voiceover.md'
s = read(p).replace('](./calculate-metadata)', '](./calculate-metadata.md)')
edit(p, s, '修复已核实的缺少扩展名参考链接')

print('Personal instruction edits saved; originals and hashes are in changes.json.')
