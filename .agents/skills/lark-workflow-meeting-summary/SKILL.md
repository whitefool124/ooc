---
name: lark-workflow-meeting-summary
description: "会议纪要整理工作流：汇总指定时间范围内的会议纪要并生成结构化报告。当用户需要整理会议纪要、生成会议周报、回顾一段时间内的会议内容时使用。"
metadata:
  version: 1.0.0
  requires:
    bins: ["lark-cli"]
---

# 会议纪要汇总工作流

认证、身份、scope 或配置问题时读取 [`../lark-shared/SKILL.md`](../lark-shared/SKILL.md)；常规业务沿用既定身份并显式传 `--as`，不预先重登。高风险确认按完整会话中已有的具体授权处理；真正的权限或审批拒绝不得绕过。

会议产品边界或 token 路由不明时读取 [`../lark-vc/references/vc-domain-boundaries.md`](../lark-vc/references/vc-domain-boundaries.md)；已有明确资源类型和 ID 时直接使用对应命令。

## 适用场景

- "帮我整理这周的会议纪要" / "总结最近的会议" / "生成会议周报"
- "看看今天开了哪些会" / "回顾过去一周开了哪些会"

## 前置条件

仅支持 **user 身份**，所有命令显式传 `--as user` 并沿用身份。仅当前认证或所需 scope 缺失时执行以下授权命令：

```bash
lark-cli auth login --domain vc        # 基础（查询+纪要）
lark-cli auth login --domain vc,drive   # 含读取纪要文档正文、生成文档
```

## 工作流

```
{时间范围} ─► vc +search ──► 会议列表 (meeting_ids)
                   │
                   ▼
               vc +detail ──► 获取 note_id 
                   │
                   ▼
               note +detail ──► 纪要文档 tokens
                   │
                   ▼
               drive metas batch_query 纪要元数据
                   │
                   ▼
               docs +fetch / note +transcript 读取实际内容
                   │
                   ▼
               结构化报告
```

### Step 1: 确定时间范围

默认**过去 7 天**。推断规则："今天"→当天，"这周"→本周一~now，"上周"→上周一~上周日，"这个月"→1日~now。

> **注意**：日期转换必须调用系统命令（如 `date`），不要心算。时间范围参数需根据 CLI 实际要求格式化（通常为 `YYYY-MM-DD` 或 ISO 8601）。

### Step 2: 查询会议记录

```bash
# page-size 最大为 30
lark-cli vc +search --start "<YYYY-MM-DD>" --end "<YYYY-MM-DD>" --format json --page-size 30
```

- 时间范围拆分：搜索的时间范围最大为 1 个月。搜索更长时间范围的会议，需要拆分为多次时间范围为一个月查询。
- `--end` 为**包含当天**的日期（即查"今天"时 start 和 end 都填今天）
- `--format json` 输出 JSON 格式，你更佳擅长解析 JSON 数据。
- `--page-size 30` 每页最多 30 条。
- 有 `page_token` 时必须继续翻页，收集所有 `id` 字段（meeting-id）

### Step 3: 获取纪要元数据

1. 查询会议关联的纪要信息
```bash
# 首先获取 note_id 和 minute_token
lark-cli vc +detail --meeting-ids "id1,id2,...,idN"

# 然后用 note_id 获取文档 tokens（如有多个需分别获取）
lark-cli note +detail --note-id "note_id"
```
- 根据上一步搜集到的 `meeting-id` 查询。
- 单次最多查询 50 个，超过 50 个需分批调用。
- 部分会议没有 `note_id` 或报错 `no notes available`，在最终输出中标注"无纪要"。
- 记录每个纪要的 `note_id`（纪要 ID）、`note_display_type`（展示类型：`unknown` / `normal` / `unified`）、`note_doc_token`（纪要文档 Token）和 `verbatim_doc_token`（逐字稿文档 Token）。

> **逐字稿路由按 `note_display_type` 决定**（详见 [vc-domain-boundaries.md](../lark-vc/references/vc-domain-boundaries.md) 的 Note 域）：
> - `normal`：逐字稿是独立文档，链接/正文走 `verbatim_doc_token`。
> - `unified`：逐字稿**不是独立文档**，没有可分享的逐字稿文档链接；需要逐字稿内容时用 `note +transcript --note-id <note_id>`（[lark-note](../lark-note/SKILL.md)）拉取到本地，报告中标注"unified 纪要"即可。

2. 获取纪要文档和逐字稿文档链接
```bash
# 学习命令使用方式
lark-cli schema drive.metas.batch_query

# 批量获取纪要文档与逐字稿链接: 一次最多查询 10 个文档
# 仅对 note_doc_token 与 normal 纪要的 verbatim_doc_token 查询链接
lark-cli drive metas batch_query --data '{"request_docs": [{"doc_type": "docx", "doc_token": "<doc_token>"}], "with_url": true}'
```

### Step 4: 读取正文并整理纪要报告

内容总结必须先读取实际纪要正文：note_doc_token 经 `docs +fetch` 获取；normal 逐字稿读取 verbatim_doc_token，unified 逐字稿用 `note +transcript`。按需读取 lark-doc/lark-note 参考并始终沿用 user 身份。链接和元数据只能支持会议清单，不能据此编造结论、决定或待办。正文不可见时标明未读取，只报告已知信息。


根据时间跨度选择输出格式：

- **单日汇总**（"今天"/"昨天"）：用"今日会议概览"标题，逐会议列出会议时间、主题、纪要链接、逐字稿链接（`unified` 纪要无逐字稿链接，标注"unified 纪要，逐字稿需 `note +transcript` 拉取"）。
- **多日/周报**（"这周"/"过去 7 天"等）：用"会议纪要周报"标题，含概览统计、逐会议详情。

### Step 5: 生成文档（可选，用户要求时）

阅读 [`../lark-doc/SKILL.md`](../lark-doc/SKILL.md) 学习云文档技能。

使用当前 docs 创建/更新参考中的参数与格式。不要把 XML 标题标签混进 Markdown，也不要为修改已有报告另建副本。

## 参考

- [lark-shared](../lark-shared/SKILL.md) — 认证、权限问题时读取
- [lark-vc](../lark-vc/SKILL.md) — `+search`、`+detail` 详细用法
- [lark-note](../lark-note/SKILL.md) — `note +detail`、`note +transcript`（unified 纪要逐字稿）
- [lark-doc](../lark-doc/SKILL.md) — `+fetch`、`+create`、`+update` 详细用法
