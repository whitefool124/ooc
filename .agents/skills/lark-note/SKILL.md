---
name: lark-note
description: "持有 note_id 时读取飞书会议纪要详情和 unified 原始逐字记录；会议定位使用 lark-vc。"
metadata:
  version: 1.0.0
  requires:
    bins: ["lark-cli"]
  cliHelp: "lark-cli note --help"
---

# note (v1)

身份：`+detail` 支持 `--as user` / `--as bot`；`+transcript` 仅支持 `--as user`。`note_id` 若由某个身份取得（例如 `vc +detail --as bot`），`+detail` 必须显式沿用同一个 `--as`——不要依赖 profile 默认身份。完整身份延续规则见 [`../lark-shared/SKILL.md`](../lark-shared/SKILL.md)；身份选择或权限问题时按需读取。

`+detail` 返回的 `note_doc_token` / `verbatim_doc_token` / `shared_doc_tokens` 交给 [lark-doc](../lark-doc/SKILL.md) 读正文时，仍要显式带上同一个 `--as`。lark-doc 对普通文档推荐 `--as user`，**不覆盖这些纪要文档 token 的来源身份**。

会议产品边界或 token 路由不明时读取 [`../lark-vc/references/vc-domain-boundaries.md`](../lark-vc/references/vc-domain-boundaries.md)；已有明确资源类型和 ID 时直接使用对应命令。

Note 域只接受显式 `note_id`：用户直接提供，或 `docs +fetch` 返回的 `<vc-transcribe-tab vc-node-id="...">` 中的 `vc-node-id`。不要从 `doc_token`、标题、正文或 backlink 反推 `note_id`。

## 命令路由

| 用户表达 / 上下文 | 路由 |
|---------|------|
| 已知 `note_id`，查纪要类型 / 文档 token | `note +detail --note-id NOTE_ID` |
| `docs +fetch` 返回 `<vc-transcribe-tab vc-node-id="...">` | 取 `vc-node-id` 作为 `NOTE_ID`，先 `note +detail --note-id NOTE_ID` |
| 只持有 `meeting_id` | 先 `vc +detail --meeting-ids <id>` 拿 `note_id`，再 `note +detail --note-id NOTE_ID` |
| 只持有 `minute_token`（妙记 URL） | 先 `minutes +detail --minute-tokens <token>` 顶层取 `note_id`，再 `note +detail --note-id NOTE_ID`（不要把 `minute_token` 当 `note_id`） |
| 只持有日程 `event_id` | 先 `calendar +meeting --event-ids <id>` 拿 `meeting_id`，再按上一行继续 |
| 已知 `note_id`，读纪要正文 | `note +detail` → `docs +fetch --doc <note_doc_token>` |
| 已知 `note_id`，查 unified 原始记录 / 逐字稿 | `note +transcript --note-id NOTE_ID` |
| 只有自然语言纪要标题，用户要逐字稿 / 原始记录 / 谁说了什么 | 不进本 skill；先走文档搜索与 `docs +fetch`，拿到 `vc-node-id` 后再回来 |

## `note_display_type` 路由

| `note +detail` 结果 | 用户要逐字稿 / 原始记录时 |
|------|---------------|
| `normal` + `verbatim_doc_token` 非空 | `docs +fetch --doc <verbatim_doc_token>`（沿用 `+detail` 用的身份） |
| `unknown` + `verbatim_doc_token` 非空 | 先按独立文档处理；不要猜成 unified |
| `unknown` + 无逐字稿 token | 停止重试并说明无法确定逐字稿入口 |
| `unified` | `note +transcript --note-id <note_id>`（仅支持 `--as user`） |

判别键是 `note_display_type`，不是 `verbatim_doc_token` 是否为空：unified 纪要也可能返回非空 `verbatim_doc_token`。

> **bot + unified 的边界**：`+transcript` 目前仅支持 `--as user`。如果 `+detail --as bot` 返回 `unified`，不要静默切到 `--as user` 继续——先停下来向用户说明"该纪要逐字稿只能以 user 身份读取"，只有用户明确同意才切换身份重试。

## 关键字段

- `note_id`：Note 域唯一入口。
- `note_display_type`：`unknown` / `normal` / `unified`。
- `note_doc_token`：纪要正文文档，正文读取交给 [lark-doc](../lark-doc/SKILL.md)。
- `verbatim_doc_token`：普通纪要逐字稿文档；unified 逐字稿不按这个 token 路由。

## 不在本 Skill 范围

- 通过 `meeting_id` 定位纪要（`note_id`）→ [lark-vc](../lark-vc/SKILL.md)（`vc +detail`）。
- 通过 `minute_token` 定位纪要（`note_id`）→ [lark-minutes](../lark-minutes/SKILL.md)（`minutes +detail` 顶层返回 `note_id`）。
- 通过日程 `event_id` 定位会议(`meeting_id`) / 用户绑定纪要(`meeting_note`) → [lark-calendar](../lark-calendar/SKILL.md)（`calendar +meeting`）。
- 自然语言纪要标题搜索 → [lark-drive](../lark-drive/SKILL.md) / [lark-doc](../lark-doc/SKILL.md)。
- Docx 正文读取 → [lark-doc](../lark-doc/SKILL.md)。
- 妙记基础信息与媒体文件 → [lark-minutes](../lark-minutes/SKILL.md)。

## Shortcuts

| Shortcut | 何时读 reference |
|----------|------|
| [`+detail`](references/lark-note-detail.md) | 需要解释输出字段或根据展示类型继续路由 |
| [`+transcript`](references/lark-note-transcript.md) | 需要拉取 unified 原始记录或处理本地输出文件 |

## 核心概念

- **会议纪要（Note）**：视频会议结束后生成的结构化文档，通过 `note_id` 标识。一个 Note 包含 AI 智能纪要文档、逐字稿文档和会中共享文档。
- **note_id**：纪要的唯一标识符，可通过 `vc +detail --meeting-ids` 获取。
- **AI 智能纪要（MainDoc）**：AI 生成的会议总结与待办，对应 `note_doc_token`。
- **逐字稿（VerbatimDoc）**：会议的逐句发言记录，含说话人和时间戳，对应 `verbatim_doc_token`。
- **共享文档（SharedDoc）**：会中投屏共享的文档，对应 `shared_doc_tokens`。

## 核心场景

### 1. 通过 note_id 获取纪要文档 Token

1. 当用户已有 `note_id`，需要获取对应的 `note_doc_token`、`verbatim_doc_token` 或 `shared_doc_tokens` 时，使用 `note +detail`。
2. `note_id` 通常来自 `vc +detail` 的返回结果。
3. 获取到文档 Token 后，可使用 `docs +fetch` 读取文档内容，或使用 `drive metas batch_query` 获取文档元信息。

```bash
# 1. 从会议获取 note_id（这里以 bot 身份为例）
lark-cli vc +detail --meeting-ids <meeting_id> --as bot

# 2. 用 note_id 拿文档 Token；沿用第 1 步的身份，不要省略 --as
lark-cli note +detail --note-id <note_id> --as bot

# 3. 读取纪要文档内容；同样沿用第 1 步的身份
lark-cli docs +fetch --doc <note_doc_token> --doc-format markdown --as bot
```
