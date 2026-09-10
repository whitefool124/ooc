from audit_support import *
import subprocess
from collections import Counter

changes=json.loads(read(OUT/'changes.json'))
summary=json.loads(read(OUT/'validation-summary.json'))
validation=json.loads(read(OUT/'skill-validation.json'))
entries=json.loads(read(OUT/'inventory-after.json'))
before=json.loads(read(OUT/'inventory-before.json'))
lookup={x['path']:x for x in changes}
checks={x['path']:x for x in validation}
before_map={x['path']:x for x in before}
modified_checks=[x for x in validation if x['path'] in lookup]
project_paths=[str(Path(x['path']).relative_to(ROOT)) for x in changes if Path(x['path']).is_relative_to(ROOT)]
git=subprocess.run(['git','diff','--check','--',*project_paths],cwd=ROOT,capture_output=True,text=True,encoding='utf-8',errors='replace')
save_json('git-diff-check.json',{'exit_code':git.returncode,'stdout':git.stdout,'stderr':git.stderr})
unchanged_managed=all(x['sha256']==before_map[x['path']]['sha256'] for x in entries if x['group'] in ('system','plugin_cache'))
groups=Counter('全局AGENTS' if x['path']=='C:/Users/FNHF/.codex/AGENTS.md' else '项目AGENTS' if x['path']==(ROOT/'AGENTS.md').as_posix() else '项目飞书' if '/OCC_Codex/.agents/skills/' in x['path'] else '全局飞书' if '/.agents/skills/' in x['path'] else '个人技能' for x in changes)
report=f'''# 技能与 AGENTS 审计 — 2026-09-07

目标：依据用户指定文章复查全局与 OCC 项目内的技能、AGENTS 和 Funplay 指导，修正已证实的冗余流程、授权冲突、陈旧规格及引用错误。

涉及：全局/项目 AGENTS，12 个个人技能、两份各 27 个飞书技能，6 个系统技能与 50 个插件缓存技能入口；另行检查 Funplay 已安装源码与官方 0.6.5 发布源码。验收：入口可解析、修改符合具体任务边界、原有技术/授权保护保留、引用有效、备份可追溯。下一步：用户升级 Funplay 后，核对 Editor 身份、版本和实际工具，再做必要的运行验证。

## 结果与范围

- 盘点 **122 个 SKILL.md + 2 个 AGENTS.md**。50 个插件缓存入口包含旧版本或模板，不等于当前全部启用。
- 修改 **{len(changes)} 个文件**，其中 **{len(modified_checks)} 个技能入口、2 个 AGENTS.md**；其余为对应参考规则。分布：{'、'.join(f'{k} {v}' for k,v in groups.items())}。
- 主仓库始终是 `E:/数据库/OCC_Codex`。保留任务开始前已有的工作树改动；本次没有创建工作树、改动游戏代码或发布外部内容。
- 系统与供应商插件缓存保持原样：**{unchanged_managed}**。全量完成入口静态审查；引用检查覆盖 Markdown 链接，行为审查深入受影响参考。未把静态扫描描述为对所有业务 API 的实测。

## 已修正

| 范围 | 修订 |
|---|---|
| 全局 AGENTS | 保留跨轮具体授权与必要工程限制；按实际任务读取技能，区分技术检查与用户审批；测试规模与改动相称。 |
| OCC AGENTS | 任务目标可写在回复中；按真实章节定位界面/美术规则；技能维护无需 Unity 编译；Funplay 通用 UI 流程不能覆盖 OCC 架构与 PC 规格。 |
| Funplay 个人技能 | 按已授权任务完成编辑与验证；显式 by_id、对象回读、结果未知先核对；升级目标取官方版本，不为旧安装回退；补充新版 TMP 局部材质作用域与技能托管区规则。 |
| 飞书共同规则 | 认证按实际缺失触发，普通任务沿用显式身份；确认 flag 复用具体授权；保留真正权限/审批拒绝；二维码按需要提供；版本值保留到 metadata.version。 |
| 飞书文档/幻灯片 | 简单文档不强制完整草稿流程；复杂流程按需加载。取消二读全文、图片/卡片/字数配额和固定审美；保留 XML 规范、lint、写后回读和布局检查。失败草稿保留用于恢复。 |
| 飞书数据真实性 | 缺少数据不自动造数；缺 Logo/截图/论文图不生成近似图冒充证据；会议内容总结必须读取正文，只有元数据时只交付会议清单。 |
| 飞书表格 | 尊重公式联动或静态快照要求；原生透视表约束限定于请求的对象；按实际范围验证，不硬编码假设条数；禁止用 IFERROR 掩盖错误或为了 100% 正则命中填造值。 |
| 飞书身份/操作 | 删除 Base ACL 失败自动换 bot；邮件与妙搭具体授权跨轮有效；Wiki 分页不能首个同名即停；画板只读不启动额外 npx 工具。 |
| 日程待办汇总 | 全量分页；不只读取前 20 条或静默丢弃旧待办；用活动时间区间检测重叠，避免遗漏跨多个短会的长会。 |
| 像素/图片技能 | 项目合同决定尺寸、帧数和调色板；历史 64px/8帧或 Rika128px 不作为通用规格；明确现有归一化脚本逐帧拟合限制；本地工作台仅在明确选用且项目允许时调用，原料不自动导入 Unity。 |
| 其他个人技能 | 界面规划按请求交付，避免小改强制额外导出与文档同步；SMBX 按受影响内容查手册；修复 Remotion 失效引用。符合要求的 gpt-image、OCC 平衡/关卡等规则保留。 |

全局与项目飞书目录有版本和内容差异，本次按各文件定点修改，没有将整份全局目录覆盖到项目。未来 CLI/插件更新可能覆盖维护过的技能，应以本次差异复核，而不是盲目覆盖新版文件。

## Funplay 升级状态

审计开始时 OCC manifest 及解析包为 0.6.4。用户纠正后，以官方 **0.6.5** 为升级目标；未修改 Packages/manifest.json 或 Library。已直接查询 OpenUPM 注册表，并静态核对发布 revision `b9291dab745ef690b457244af616bc29933327e2` 的 package、Changelog、ProjectSkillsManager 与服务器指令，来源副本位于 [funplay-0.6.5](./funplay-0.6.5)。

0.6.5 发布于 2026-09-03；内置 `unity-mcp-workflow` 为 1.0.3，`unity-ui-composition` 为 1.0.3。新增/更新的指导包含跨项目离线连接解释、Project Skills 更新提示与 TMP 设计效果实现。自定义指导放在个人技能或托管标记之外，不编辑会再生的包缓存。

**待用户完成**：在 OCC Unity 的 Package Manager 中选择 Funplay MCP for Unity → Version History → 0.6.5 → Update，等待导入编译完成并启动 Funplay MCP Server。当前会话无可调用 Funplay 工具，因此未确认当前 Editor 的 Application.dataPath、升级完成状态或运行行为。升级后核对主工程 Assets 路径与实际 schema；按需要同步 Project Skills，再核验托管文件和 Console。此项仍待完成，不计为运行验证通过。

## 验证

- 本次修改的 {len(modified_checks)} 个技能入口：**{sum(x['valid'] for x in modified_checks)} 通过** quick_validate。
- 全部 122 个入口：**115 通过，7 个原有校验器不兼容项**。gpt-image 的 compatibility 字段不在该简单校验器白名单；供应商 Presentations/Spreadsheets 名称含大写；4 个 Pixso 技能包含 disable-model-invocation/references 扩展字段。它们在当前会话中可见，但未据此宣称所有扩展字段生效。保留原文件，未为通过通用校验器删除供应商语义。
- **61 个 agents/openai.yaml 均可解析**，没有改动其调用策略。
- 有效本地 Markdown 引用：**0 未解决断链**。示例 `url`、图片 token 等占位符已与真实引用区分；缺失源码示例改为诚实标明未分发。
- 备份和修改后哈希：**0 不一致**；新增行尾空白：**0**；项目受影响文件 `git diff --check` 返回 **{git.returncode}**。
- 没有发送飞书消息/邮件、创建线上测试文件、生图、进入 Play Mode 或修改场景。验证范围为技能文本、元数据、引用、官方发布源码与备份完整性；未调用飞书业务接口或测试 Unity 运行结果。

## 依据与可审阅资料

- [用户指定文章：Rethinking skills and prompts for GPT-6 Astra](https://x.com/pvncher/status/2095991462416490862)。X 页面访问受限，实际通过 FXTwitter 的该推文 article 字段读取全文。采纳的是缩窄触发、按需加载、目标导向、相称验证和精确授权边界，未把文章升级为高于用户/系统的规则。
- [OpenAI 当前模型指南](https://developers.openai.com/api/docs/guides/latest-model)、[Skills 文档](https://developers.openai.com/codex/skills)、[AGENTS 文档](https://developers.openai.com/codex/guides/agents-md)。
- [Funplay 官方仓库](https://github.com/FunplayAI/funplay-unity-mcp)、[OpenUPM 包入口](https://openupm.com/packages/com.gamebooom.unity.mcp/)。
- [完整变更及原因](./changes.json)、[逐文件差异](./review.diff)、[修改前备份](./before)、[验证摘要](./validation-summary.json)、[入口逐项验证](./skill-validation.json)、[Git 检查](./git-diff-check.json)。备份以 .bak 保存，不作为活动技能入口。

## 全量入口清单

“保留”表示本次没有修改入口；其参考文件可能有定点修订。供应商/系统入口只作静态检查，不表示其全部远程功能已实测。

| 分类 | 入口 | 处理 | 静态格式结果 |
|---|---|---|---|
'''
for x in entries:
    p=x['path']; v=checks.get(p)
    label='通过' if v and v['valid'] else '校验器兼容项，见上文' if v else '差异与引用检查'
    status='已修订' if p in lookup else '保留'
    report+=f"| {x['group']} | [{Path(p).parent.name + '/' + Path(p).name}](<{p}>) | {status} | {label} |\n"
(OUT/'审计报告.md').write_text(report,encoding='utf-8')
print(json.dumps({'report':(OUT/'审计报告.md').as_posix(),'changed_file_groups':groups,'modified_entry_checks':Counter(x['valid'] for x in modified_checks),'git_diff_check':git.returncode,'managed_unchanged':unchanged_managed},ensure_ascii=False))
