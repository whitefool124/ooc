---
name: lark-shared
description: "Use for lark-cli setup/auth tasks: auth login/status/logout, user vs bot identity, business-domain permissions (--domain, including all/docs/drive), missing scopes, revoking authorization, or handling _notice JSON."
metadata:
  version: 1.0.0
---

# lark-cli 共享规则

本技能指导你如何通过lark-cli操作飞书资源, 以及有哪些注意事项。

## 配置初始化

仅在尚未配置或用户要求重新配置时运行 `lark-cli config init`；已有配置直接复用。

当你帮用户初始化配置时，使用background方式使用下面的命令发起配置应用流程，启动后读取输出，从中提取授权链接并发给用户。

**授权链接**：原样展示 CLI 返回的 URL，不编解码或重拼 query。用户要求二维码或移动端扫码更方便时，使用 `lark-cli auth qrcode` 生成 PNG 并同时展示链接；二维码不是每次授权的必经步骤。

```bash
# 发起配置（该命令会阻塞直到用户打开链接并完成操作或过期）
lark-cli config init --new
```

## 认证

### 认证任务速查

认证、scope、业务域、登录态、退出登录态、撤销授权问题都走本技能。

| 用户意图 | 首选命令 / 回答 |
|---|---|
| 获取全部权限 | `lark-cli auth login --domain all --no-wait --json` |
| 按业务域授权 | `lark-cli auth login --domain docs --domain drive --no-wait --json`；`--domain` 可重复，也可用逗号分隔 |
| 指定单个 scope 授权 | `lark-cli auth login --scope "<scope>" --no-wait --json` |
| 检查当前登录态、是谁登录、token 是否有效 | `lark-cli auth status --json --verify`；回答时引用 `identity`、`verified`、`identities.user.status`、`identities.user.userName`、`identities.user.openId`（用户 open id）、`identities.user.tokenStatus`、`identities.user.scope` |
| 快速查看当前身份状态 | `lark-cli whoami`；实际生效的那一个身份 |
| 退出当前机器的用户登录态 | `lark-cli auth logout --json`；`loggedOut:true` 表示注销成功 |
| bot 缺少权限 | 不要执行 `auth login`；引导用户在开发者后台开通 bot scope，优先复用错误里的 `console_url` |
| 取消用户对应用的全部服务端授权 | `auth logout` 只清本机登录态；服务端授权需用户在飞书授权管理页取消 |
| 只取消一个 scope | CLI 不支持单独撤销一个已授予 scope；可重新走最小 scope 授权，或让用户在授权管理页处理 |

机器读取 JSON 时，为减少 `_notice` 干扰，可在命令前加：

```bash
LARKSUITE_CLI_NO_UPDATE_NOTIFIER=1 LARKSUITE_CLI_NO_SKILLS_NOTIFIER=1 lark-cli auth status --json --verify
```

### 身份类型

两种身份类型，通过 `--as` 切换：

| 身份 | 标识 | 获取方式 | 适用场景 |
|------|------|---------|---------|
| user 用户身份 | `--as user` | `lark-cli auth login` 等 | 访问用户自己的资源（日历、云空间/云盘/云存储等） |
| bot 应用身份 | `--as bot` | 自动，只需 appId + appSecret | 应用级操作,访问bot自己的资源 |

### 身份选择原则

输出的 `[identity: bot/user]` 代表当前身份。bot 与 user 表现差异很大，需确认身份符合目标需求：

- **Bot 看不到用户资源**：无法访问用户的日历、云空间（云盘/云存储）文档、邮箱等个人资源。例如 `--as bot` 查日程返回 bot 自己的（空）日历
- **Bot 无法代表用户操作**：发消息以应用名义发送，创建文档归属 bot
- **Bot 权限**：只需在飞书开发者后台开通 scope，无需 `auth login`
- **User 权限**：后台开通 scope + 用户通过 `auth login` 授权，两层都要满足


### 身份延续（跨命令工作流）

身份是**整个工作流的状态**，不是单条命令的局部参数。CLI 不会在进程之间继承"上一步用的身份"——省略 `--as` 不代表"保持当前身份"，而是把身份选择交回下面这条优先级链：

```text
显式 --as > profile default-as > credential auto-detect
```

因此，只要用户显式选择了身份，或某个 ID / Token 是通过某个身份取得的（例如 `vc +detail --as bot` 返回的 `note_id`），**后续每一条消费该 ID/Token 的命令都必须显式带上相同的 `--as`**，跨 skill 传递也不例外：

- 禁止依赖 profile 默认身份让后续命令"自动"沿用同一身份。
- 禁止仅仅因为遇到权限错误就切换身份去绕过它——先如实报告，只有用户明确同意才切换。
- 下游命令根本不支持来源身份时（如 `--as bot` 拿到的 `note_id` 指向 `note_display_type=unified`，而 `note +transcript` 仅支持 `--as user`），停止并向用户说明这个边界，不要静默省略 `--as` 把身份交给默认值。
- 命令支持的精确身份以 `<command> --help` / `schema` 为准；各 skill 的身份小节只标注会影响路由决策的例外，不重复维护完整矩阵。

```bash
# GOOD — note_id 来自 bot 链路，下一步显式沿用 bot
lark-cli vc +detail --meeting-ids <meeting_id> --as bot
lark-cli note +detail --note-id <note_id> --as bot
lark-cli docs +fetch --doc <note_doc_token> --as bot

# BAD — 省略 --as，身份可能被 profile 默认值悄悄换成 user
lark-cli vc +detail --meeting-ids <meeting_id> --as bot
lark-cli note +detail --note-id <note_id>
```

### 权限不足处理

遇到权限相关错误时，**根据当前身份类型采取不同解决方案**。

错误响应中包含关键信息：
- `missing_scopes`：列出缺失的 scope (N选1)
- `console_url`：飞书开发者后台的权限配置链接
- `hint`：建议的修复命令

**missing_scope 与资源 ACL（无权访问某具体资源）是两类不同问题**，恢复方式也不同：

| 失败类型 | user | bot |
|---------|---------|---------|
| missing scope（应用/用户完全没有这个权限） | `auth login --scope ...` | 使用错误中的 `console_url` 去开发者后台开通，**禁止** `auth login` |
| 资源 ACL（有 scope，但对这一条具体资源没有访问权限） | 请求资源所有者给当前用户授权 | 请求资源所有者给当前应用/bot 授权 |
| 资源在当前身份下不可见 | 保持当前身份，如实报告不可见，不要切换身份重试 | 保持当前身份，如实报告不可见，不要切换身份重试 |

任何权限恢复完成后，都必须用**触发错误时的原身份**重试，不要在恢复过程中换成另一个身份。

#### Bot 身份（`--as bot`）

将错误中的 `console_url` 原样提供给用户，引导去后台开通 scope。**禁止**对 bot 执行 `auth login`。

#### User 身份（`--as user`）

```bash
lark-cli auth login --domain <domain> --no-wait --json          # 按业务域发起授权
lark-cli auth login --scope "<missing_scope>" --no-wait --json  # 按具体 scope 发起授权（推荐，符合最小权限原则）
```

**规则**：auth login 必须指定范围（`--domain` 或 `--scope`）。多次 login 的 scope 会累积（增量授权）。

#### Agent 代理发起认证

仅在任务需要且当前认证/权限确实缺失时发起授权，使用所需的最小 `--scope` 或 `--domain`：

```bash
lark-cli auth login --scope "<required_scope>" --no-wait --json
```

立即向用户展示返回的 `verification_url`；`device_code` 仅用于当前认证流程，不写入报告或长期保存。若客户端只能在最终回复展示链接，先交还控制权；能展示中间结果时可继续独立工作，不因授权流程强制结束整项任务。

用户完成授权后，由 agent 执行 `lark-cli auth login --device-code <device_code>` 并核对成功结果。不要在用户尚未看到链接时阻塞轮询。链接或 code 过期后，保留原 scope/domain/exclude 选择重新发起；不复用过期凭据。

## 更新检查

lark-cli 命令执行后，如果检测到新版本，JSON 输出中会包含 `_notice.update` 字段（含 `message`、`command` 等）。

除非用户正在询问更新、版本或 notice，否则不要把 `_notice` 原样复制为当前任务的主要答案，也不要为了 notice 中断当前任务去反复查 help。

需要稳定 JSON 给脚本或机器读取时，可以在命令前设置：

```bash
LARKSUITE_CLI_NO_UPDATE_NOTIFIER=1 LARKSUITE_CLI_NO_SKILLS_NOTIFIER=1 <lark-cli command>
```

当你在输出中看到 `_notice.update` 时，先完成用户当前请求；如仍相关，再简短告知可运行：

```bash
lark-cli update
```

**重要**：始终使用 `lark-cli update` 更新，它会同时更新 CLI 和 AI Skills。

## JSON 输出契约

`--format json`（默认）下，成功与错误的信封结构不同：

成功信封写入 **stdout**（退出码 0）：

```json
{ "ok": true, "identity": "user", "data": { "guid": "..." }, "meta": { "count": 1 } }
```

错误信封写入 **stderr**（退出码非 0）：

```json
{ "ok": false, "identity": "user", "error": { "type": "authorization", "subtype": "missing_scope", "code": 99991679, "message": "...", "hint": "...", "missing_scopes": ["..."] } }
```

**判断成功必须用 `ok == true`（或进程退出码 0），不要用 `code == 0`**：成功信封没有顶层 `code` / `msg` 字段，`code` 只出现在错误信封的 `error` 内，含义是上游 OpenAPI 的 numeric code。按 OpenAPI 老格式 `{"code": 0, "msg": "ok"}` 判断会把所有成功调用误判为失败；封装写入类命令（如 `task +create`）时尤其危险，误判会绕过幂等逻辑导致重复创建。

## 安全规则

- **禁止输出密钥**（appSecret、accessToken）到终端明文。
- **写入/删除前核对完整会话中的具体授权**。明确请求覆盖动作、目标和范围时继续，不重复确认；身份、目标或影响发生实质变化时再询问。
- 用 `--dry-run` 预览危险请求。
- **文件路径只接受相对路径**：`--file`、`--output`、`--output-dir`、`@file` 等路径参数只接受 cwd 下的相对路径，传绝对路径会报 `unsafe file path`。数据输入（`@file`、大 JSON）优先用 stdin 传入，避免路径和转义问题。

## 高风险操作的审批协议（exit 10）

lark-cli 对高风险写操作（`risk: "high-risk-write"`）有强制确认门禁。当你不带 `--yes` 调用这类命令时，CLI 会退出码 `10`、并在 stderr 返回如下结构化 envelope：

```json
{
  "ok": false,
  "identity": "bot",
  "error": {
    "type": "confirmation",
    "subtype": "confirmation_required",
    "message": "drive +delete requires confirmation",
    "hint": "add --yes to confirm",
    "risk": "high-risk-write",
    "action": "drive +delete"
  }
}
```

**遇到这种情况，不要当普通错误放弃。** 按以下流程处理：

1. **识别**：看到子进程 exit code = `10` 且 stderr JSON 里 `error.type == "confirmation"`、`error.subtype == "confirmation_required"`
2. **核对授权**：检查完整会话中对动作、目标、范围与影响的具体同意。已有授权且未实质变化时继续；缺少授权时，先完成只读准备并展示具体影响，再询问。exit 10 本身不构成授权。
3. **已有有效授权或取得新同意** → 在你**原始 argv 的末尾追加 `--yes`** 后重试
4. **用户拒绝** → 终止流程，不要擅自改写参数或跳过门禁

**绝对不允许**：
- 看到 exit 10 就默认加 `--yes` 静默重试（这等于禁用门禁）
- 把 `confirmation_required` 当网络错误/权限错误处理
- 在用户没明确同意的前提下追加 `--yes` 重试
- 用 `sh -c` 等 shell 方式拼接命令重试——用 `exec.Command(argv...)` 参数数组形式，避免 shell 解析把用户参数当作语法

提前预判：想先让用户 review 危险操作的具体请求，调用时加 `--dry-run`——它不触发门禁，会打印完整请求详情（URL / body / params），你可以把这个预览给用户看过再去真正执行。

### 如何识别一条命令是高风险

- shortcut：`lark-cli <service> +<cmd> --help` 顶部会显示 `Risk: high-risk-write`
- service 命令：`lark-cli schema <service>.<resource>.<method> --format json` 的返回值里 `"risk": "high-risk-write"`
