---
name: lark-workflow-standup-report
description: "日程待办摘要：编排 calendar +agenda 和 task +get-my-tasks，生成指定日期的日程与未完成任务摘要。适用于了解今天/明天/本周的安排。"
metadata:
  version: 1.0.0
  requires:
    bins: ["lark-cli"]
---

# 日程待办摘要工作流

认证、身份、scope 或配置问题时读取 [`../lark-shared/SKILL.md`](../lark-shared/SKILL.md)；常规业务沿用既定身份并显式传 `--as`，不预先重登。高风险确认按完整会话中已有的具体授权处理；真正的权限或审批拒绝不得绕过。

## 适用场景

- "今天有什么安排" / "今天的日程和待办"
- "明天有什么会" / "明日日程与未完成任务"
- "帮我看看今天要做什么" / "早报摘要"
- "开工摘要" / "standup report"
- "这周还有哪些安排"

## 前置条件

仅支持 **user 身份**，所有命令显式传 `--as user` 并沿用身份。仅当前认证或所需 scope 缺失时执行以下授权命令：

```bash
lark-cli auth login --domain calendar,task
```

## 工作流

```
{date} ─┬─► calendar +agenda [--start/--end]              ──► 日程列表（会议/事件）
        └─► task +get-my-tasks --complete=false [--due-end] ──► 未完成待办列表
                    │
                    ▼
              AI 汇总（时间转换 + 冲突检测 + 排序）──► 摘要
```

### Step 1: 获取日程

```bash
# 今天（默认，无需额外参数）
lark-cli calendar +agenda

# 指定日期范围（必须使用 ISO 8601 格式，不支持 "tomorrow" 等自然语言）
lark-cli calendar +agenda --start "2026-03-26T00:00:00+08:00" --end "2026-03-26T23:59:59+08:00"
```

> **注意**：`--start` / `--end` 仅支持 ISO 8601 格式（如 `2026-01-01` 或 `2026-01-01T15:04:05+08:00`）和 Unix timestamp，**不支持** `"tomorrow"`、`"next monday"` 等自然语言。需要 AI 根据当前日期自行计算目标日期。

输出包含：event\_id、summary、start\_time（含 timestamp + timezone）、end\_time、free\_busy\_status、self\_rsvp\_status。

### Step 2: 获取未完成待办

```bash
# 完整 pending 摘要：显式过滤未完成任务并取全页
lark-cli task +get-my-tasks --complete=false --page-all

# 只看指定日期前到期的未完成任务（推荐用于摘要场景，减少数据量）
lark-cli task +get-my-tasks --complete=false --due-end "2026-03-27T23:59:59+08:00"

# 获取全部未完成任务（超过 20 条时）
lark-cli task +get-my-tasks --complete=false --page-all
```

> **注意**：`+get-my-tasks` 不带 `--complete` 时会**同时返回已完成和未完成任务**，会把已完成任务当成"待办"展示进摘要里。站会/日报这种 pending 汇总场景**必须**显式带上 `--complete=false`，不要省略。
>
> 数据量层面也建议加过滤：
> - 用 `--due-end` 过滤出目标日期前到期的任务
> - 如果也需要无截止日期的任务，可不加 `--due-end`，完整读取后按截止日期和相关性组织；可以折叠较早任务并报真实数量，不能静默丢弃

### Step 3: AI 汇总

整合两类结果，按用户需求和内容量输出；以下结构为较复杂摘要示例：

```
## {日期}摘要（{YYYY-MM-DD 星期X}）

### 日程安排
| 时间 | 事件 | 组织者 | 状态 |
|------|------|--------|------|
| 09:00-10:00 | 产品需求评审 | 张三 | 已接受 |
| 14:00-15:00 | 技术方案讨论 | 李四 | 待确认 |

### 待办事项
- [ ] {task_summary}（截止：{due_date}）
- [ ] {task_summary}

### 小结
- 共 {n} 场会议，{m} 项待办
- 冲突提醒：{列出时间重叠的日程}
- 空闲时段：{free_slots}（根据日程推算）
```

**数据处理规则：**

1. **时间转换**：API 返回 Unix timestamp，需根据 `timezone` 字段（通常为 `Asia/Shanghai`）转换为 `HH:mm` 格式
2. **RSVP 状态映射**：
   | API 值 | 显示文案 |
   |--------|---------|
   | `accept` | 已接受 |
   | `decline` | 已拒绝 |
   | `needs_action` | 待确认 |
   | `tentative` | 暂定 |
3. **日程排序**：按开始时间升序排列
4. **冲突检测**：按开始时间排序，维护尚未结束的忙碌日程集合，与每个新日程检查重叠；不能只比较相邻项，否则会漏掉跨越多个短会的长会。标为空闲及已拒绝的事件不计为忙碌冲突。
5. **已拒绝日程**：标注"已拒绝"但不计入忙碌时段和冲突检测
6. **待办排序**：按截止时间升序，已过期的标注"已过期"，无截止时间的排在最后

## 权限表

| 命令 | 所需 scope |
|------|-----------|
| `calendar +agenda` | `calendar:calendar.event:read` |
| `task +get-my-tasks` | `task:task:read` |

## 参考

- [lark-shared](../lark-shared/SKILL.md) — 认证、权限问题时读取
- [lark-calendar](../lark-calendar/SKILL.md) — `+agenda` 详细用法
- [lark-task](../lark-task/SKILL.md) — `+get-my-tasks` 详细用法
