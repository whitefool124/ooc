from audit_support import *
import re

def change(p,fn,why):
    s=read(p); t=fn(s)
    if s!=t:edit(p,t,why)

for root in [ROOTS['global_lark'],ROOTS['project_lark']]:
    p=root/'lark-slides/references/planning-layer.md'
    def plan(s):
        s=s.replace('`mock_required_by_intent`: the user did not provide concrete values but asked for data expression, charts, trends, comparisons, or distributions; use mock data in native `<chart>`.','`data_missing`: an analytical request has no concrete values; retrieve authorized real data or ask for it. Do not create mock data merely because a chart is requested.')
        s=s.replace('It does not require user-provided real-world values. When real values are unavailable but chart expression is part of the user\'s intent, write mock or placeholder values into native `<chart>` and label them clearly instead of switching to manual drawing primitives or metric blocks.','Use actual source values for analytical work. Mock values are allowed only for an explicitly requested example, placeholder or template, and must be labeled clearly.')
        s=s.replace('short future lookup hint only; do not execute it unless separately requested.','a focused query for authorized read-only asset research when it helps the requested deck.')
        s=s.replace('a plan to create a close-enough image with the image generation tool, or a native `<chart>` for data.','a truthful alternative such as brand text, a labeled original schematic, or a statement that evidence is unavailable. Native charts require real data unless the user requested a placeholder.')
        s=s.replace('otherwise render a native `<chart>` with mock placeholder values and label it as 模拟数据，仅占位，待替换真实数据.','if real data is missing, request or retrieve it; use labeled mock values only for a requested template.')
        s=s.replace('At least several pages have visibly different XML layout structures.','Layout variation reflects the content relationships and user template, without a numeric diversity quota.')
        return s
    change(p,plan,'同步规划层数据真实性规则，移除遗留自动造数回退')
    p=root/'lark-apps/references/lark-apps-role.md'
    change(p,lambda s:s.replace('普通“删除某角色”请求只说明目标，**不等于不可逆确认**。如果用户尚未明确确认删除后果，本轮只能定位角色、读取完整成员并说明影响，最后请求确认；不得在同一轮自动追加 `--yes`。用户已明确确认不可逆删除时才继续。','删除前读取角色和完整成员影响，结合完整会话核对具体授权。已明确覆盖该 app、role、成员范围和不可逆影响时继续；泛化授权、目标歧义或新增影响才需说明并询问，不要求授权必须发生在当前轮。'),'移除引用中残留的同轮审批门槛')
    p=root/'lark-apps/references/lark-apps-env.md'
    change(p,lambda s:s.replace('如果用户在同一轮已经明确说“确认/直接执行”，视为已确认','如果完整会话中已有对具体 app/env/key 及影响的有效授权，视为已确认'),'环境变量操作复用具体跨轮授权')

p=ROOTS['personal']/'funplay-unity-dev/SKILL.md'
change(p,lambda s:s.replace('无需为普通任务导出整个工具表或重新安装插件技能。','无需为普通任务导出整个工具表或重新安装插件技能。用户要求升级时先核对官方发布版本，提示或执行已授权升级，再验证新版；不要为迁就旧安装而回退技能目标。其他工程的离线 funplay 连接不代表当前工程故障，不删除无关连接。').replace('持久化技能修订放在用户维护的技能目录，不修改包缓存。','持久化技能修订放在用户维护的技能目录，不修改包缓存。更新插件后，Project Skills 的托管区会在同步时重新生成；自定义项目规则放在托管标记之外。\n\nUI 使用既有 Text 或 TMP 约定。设计明确要求 TMP 描边、阴影、发光等效果时，使用对应组件参数或作用范围明确的材质预设；简单描边可用 outlineColor/outlineWidth。不要把无关标签共用的 fontSharedMaterial 改成局部样式，也不默认添加装饰效果。'),'依据官方0.6.5源码补充新版TMP、技能托管区及跨项目连接语义')

p=ROOTS['personal']/'funplay-unity-dev/references/compile-and-recovery.md'
change(p,lambda s:s.replace('历史安装证据记录在审计归档，长期技能不绑定旧版本。','已静态核对官方 0.6.5 发布源码（2026-09-03，revision b9291dab745ef690b457244af616bc29933327e2）；其内置 unity-mcp-workflow 为 1.0.3、unity-ui-composition 为 1.0.3。静态源码核对不等于当前 Editor 已升级或运行通过。历史安装证据记录在审计归档，长期技能不绑定旧版本。'),'记录已核实新版源码与未完成的运行验证边界')
print('Final corrections saved')
