from pathlib import Path
import json,hashlib,importlib.util,re,subprocess

ROOT=Path('E:/数据库/OCC_Codex')
OUT=Path(__file__).resolve().parent
AUDIT=OUT.parent
changes=[]
def read(p): return Path(p).read_text(encoding='utf-8-sig')
def digest(p): return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def edit(p,body,reason):
    p=Path(p); old=p.read_bytes(); backup=OUT/'before'/p.drive.replace(':','')/Path(*p.parts[1:])
    backup=backup.with_name(backup.name+'.bak'); backup.parent.mkdir(parents=True,exist_ok=True)
    if not backup.exists():backup.write_bytes(old)
    p.write_text(body,encoding='utf-8',newline='\n')
    changes.append({'path':p.as_posix(),'backup':backup.as_posix(),'before_sha256':digest(backup),'after_sha256':digest(p),'reason':reason})

root_agents=ROOT/'AGENTS.md'
route='- Unity/Funplay 任务按需读取 [UnityProject/AGENTS.md](UnityProject/AGENTS.md) 及其中列出的项目技能；本仓库根目录启动的任务也使用该入口，避免遗漏 Unity 子目录下的技能。\n'
body=read(root_agents)
if route not in body:edit(root_agents,body.rstrip()+'\n'+route,'明确嵌套Unity工程新技能的读取入口')

unity_agents=ROOT/'UnityProject/AGENTS.md'
adapter='''

## OCC 项目适配（用户维护，位于 Funplay 托管区之外）

本工程沿用 [仓库工作约定](../AGENTS.md) 和完整会话中的具体授权。以下条款限定上方通用工作流及项目技能在 OCC 中的适用范围，不改变游戏产品决定。

- Unity 编辑、编译、连接或场景对象任务按需读取 [Unity MCP Workflow](.codex/skills/funplay-unity-mcp-workflow/SKILL.md)；实际 uGUI 制作或审查再读取 [Unity UI Composition](.codex/skills/funplay-unity-ui-composition/SKILL.md)。简单脚本或技能维护不预读整套 UI 流程。
- 操作前核对主工程 `Application.dataPath`。技能例子中的对象名、Prefab 路径和数值仅是示例，不能当作 OCC 中已存在的事实。
- “修改并保存场景”“进入 Play Mode 验证”等通用步骤仍以仓库明确授权要求为准。已有具体授权跨轮有效；只保存获准且本次涉及的场景或资产，避免用全局 SaveAssets 夹带无关修改。
- 普通源码/资源修改执行必要导入编译与 Console 检查；纯 Markdown/技能变更不重新编译。身份、版本和当前状态只读核验可用 skip_refresh=true。明确通过后不重复刷新或扩大测试。
- UI 保持 OCC 当前 PC 像素合同：1920×1080、左地图75%/右HUD25%，资产尺寸按总案与机器合同分层。移动端720×1559/1559×720、刘海安全区、全套手机/平板设备验证只在实际任务涉及对应平台时适用。
- 保留现有运行时 UI 构建架构、文本组件和绑定；不因通用技能偏好 Prefab 而自行重构现有屏幕。新建可复用 UI 按当前工程架构选择实现，必要时再做与改动相称的验证。
- 采用新版 TMP 规则：设计确需文本效果时控制局部材质作用范围，不影响无关标签。生图原料及正式资产仍遵守根目录的来源、manifest、人工审美和入库要求。
- Funplay 托管块和生成技能由插件同步。持久项目修订写在本节或个人 funplay-unity-dev 技能；不要直接修改 Library/PackageCache 或手改托管文件来伪装升级。
'''
body=read(unity_agents)
if '## OCC 项目适配' not in body:edit(unity_agents,body.rstrip()+adapter+'\n','明确保存/播放授权、PC规格和现有架构，保留官方托管区')

p=Path('C:/Users/FNHF/.codex/skills/funplay-unity-dev/references/compile-and-recovery.md')
body=read(p)
paragraph='版本升级时先核对官方已发布版本与当前安装状态，再验证实际工具和编译状态；不要为迁就旧安装而回退技能目标。2026-09-07 已核对 OCC manifest、解析包及运行中的 MCP 服务均为 0.6.5，主工程路径正确，当前编译检查无错误/警告；Codex 的 unity-mcp-workflow 与 unity-ui-composition 已同步到 1.0.3，插件回读无缺失/待更新。此次只验证编辑器连接、当前编译状态与技能同步，没有执行游戏 Play Mode 验证。升级证据保存在项目审计归档；其他工程和后续版本仍以当次实际核对为准。'
parts=body.split('\n\n',2)
new=parts[0]+'\n\n'+paragraph+'\n\n'+parts[2]
if new!=body:edit(p,new,'用实际升级与同步验收替换待升级描述')

generated=list((ROOT/'UnityProject/.codex/skills').glob('*/SKILL.md'))
module_spec=importlib.util.spec_from_file_location('quick',Path('C:/Users/FNHF/.codex/skills/.system/skill-creator/scripts/quick_validate.py'))
module=importlib.util.module_from_spec(module_spec);module_spec.loader.exec_module(module)
validation=[]
for p in generated:
    ok,message=module.validate_skill(p.parent)
    validation.append({'path':p.as_posix(),'sha256':digest(p),'valid':ok,'message':message})
links=[]
for p in [root_agents,unity_agents,*generated]:
    body=re.sub(r'(?ms)^```.*?^```[^\n]*','',read(p))
    for target in re.findall(r'\]\(([^)\n]+)\)',body):
        if re.match(r'^[a-zA-Z]+:|^#',target):continue
        if not (p.parent/target.split('#')[0]).exists():links.append({'source':str(p),'target':target})
git=subprocess.run(['git','diff','--check','--','AGENTS.md','UnityProject/AGENTS.md','UnityProject/.codex/skills'],cwd=ROOT,capture_output=True,text=True,encoding='utf-8')
result={'generated_skills':validation,'unresolved_links':links,'git_diff_check':git.returncode,'git_output':git.stdout,'followup_changes':changes}
(OUT/'validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
(OUT/'changes.json').write_text(json.dumps(changes,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
report=AUDIT/'审计报告.md'
body=read(report)
body=body.replace('## Funplay 升级状态','## Funplay 升级状态（已于后续轮完成，见下方追加验收）')
body=body.replace('**待用户完成**：在 OCC Unity','**初次审计时的待办，现已完成**：在 OCC Unity')
body=body.replace('此项仍待完成，不计为运行验证通过。','以上为初次审计状态。后续已完成升级、连接/当前编译状态核验与技能同步，结果如下；游戏行为仍未进行 Play Mode 验证。')
append='''

## 升级后追加验收 — 2026-09-07

用户完成升级后，manifest、packages-lock、解析包及运行中的 MCP 服务均确认是 **Funplay 0.6.5**。通过既有工程端点的标准 MCP JSON-RPC 接口核验 Application.dataPath 为 `E:/数据库/OCC_Codex/UnityProject/Assets`，Unity 6000.5.2f1，活动场景 `Assets/Scenes/CombatPrototype.unity`；场景未脏、非 Play Mode、未在编译/导入。

`get_compilation_errors(include_warnings=true)` 返回无编译错误或警告；Console 错误读取为空。未触发重新编译、保存场景或进入 Play Mode，当前编辑器状态检查不等于游戏行为测试。

初次回读发现 `.funplay/skills/manifest.json` 的 platforms 为空，Codex 技能文件未生成。已在用户添加 Codex 技能的既定范围内，通过插件 `ProjectSkillsManager` 官方同步接口补齐：先读取冲突清单（空），保留现有平台/可选项，再启用 codex。没有手写替代托管技能。

- `funplay-unity-mcp-workflow` 1.0.3
- `funplay-unity-ui-composition` 1.0.3

插件 `GetUpgradeStatus` 对 UnityProject/AGENTS.md 与两个技能均返回 Missing=false、Unmanaged=false、RequiresUpgrade=false。两个生成技能通过 quick_validate；新增/修改的真实本地引用有效。新增 **2 个技能入口和1个 UnityProject/AGENTS.md**，与初次审计基线合计为124个技能入口、3个AGENTS。

已在根 AGENTS 增加 Unity 子目录技能入口，在 UnityProject/AGENTS 的托管标记外加入 OCC 适配：按需读技能、保存和Play授权、PC像素规格、现有运行时UI架构、TMP局部材质、验证规模与例子不能当作真实对象。官方生成技能保持原样，后续同步不会覆盖这些项目适配条款。

一次含路径字面量的预检被 Funplay strict filesystem guard 拒绝；保持 safety_checks=true 后，改用插件自身获取当前工程的接口完成检查与同步，未关闭安全设置。同步前清单与标准 MCP 回读保存在 [追加验收目录](./funplay-upgrade-verification)，具体见 [同步结果](./funplay-upgrade-verification/sync-result.json)、[验证结果和文件哈希](./funplay-upgrade-verification/validation.json) 及 [本轮备份记录](./funplay-upgrade-verification/changes.json)。

初次审计的 inventory/changes/validation 文件是该阶段快照；后续改动以追加验收目录中的记录为准。当前对话的动态技能目录未自动出现新名称，已通过仓库入口明确读取实际生成文件；没有宣称客户端已热重载工具或技能目录。
'''
if '## 升级后追加验收' not in body:edit(report,body.rstrip()+append,'补记实际升级、MCP核验和官方技能同步完成')
# Include the report backup in the follow-up manifest.
(OUT/'changes.json').write_text(json.dumps(changes,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'skills':validation,'unresolved_links':links,'git_diff_check':git.returncode,'files_changed':len(changes)},ensure_ascii=False))
