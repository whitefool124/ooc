# AGENTS.md

<!-- Funplay Unity MCP managed project skills -->
<!-- Funplay Unity MCP project skill versions: unity-mcp-workflow@1.0.4, unity-ui-composition@1.0.4 -->

# Funplay Unity MCP Project Guidance

This section is managed by Funplay MCP for Unity. Everything between the begin and end markers is regenerated on each sync; edit outside this block.

## Installed project skills

- `funplay-unity-mcp-workflow` v1.0.4 - Efficient workflow for using Unity MCP to edit, import, compile, inspect, and test Unity projects, including screenshot and Game View recording verification.
- `funplay-unity-ui-composition` v1.0.4 - Build and revise responsive Unity uGUI mobile interfaces, including portrait and landscape layouts, safe areas, prefabs, auto layout, scrolling, text, input, animation, and performance validation.

## Agent workflow rules

- Prefer project-local Funplay skills: `.codex/skills/` for Codex, `.opencode/skills/` for OpenCode, `.dsh/skills/` for DeepSeek Harness, `.agents/skills/` for Antigravity.
- Use `execute_code` as the primary Unity automation tool. For new snippets, include `using Funplay.Editor.Tools.Scripting;`, implement `IFunplayCommand`, and use `ctx.RegisterObjectCreation` / `RegisterObjectModification` / `DestroyObject` so changes participate in Undo automatically.
- Confirm the Unity project root, active scene, and real object/prefab/asset path before edits. Treat user-provided object names as hints, not paths.
- Inspect Unity objects through MCP before changing user-named scene or prefab targets. Carry the returned `instanceId` into follow-up calls (`find_method=by_id`) instead of re-resolving by name.
- Tool returns are structured JSON (`{success, message, data}` / `{success: false, code, error, data}`). Branch on `code`, not free-form text.
- Set component fields with `set_component_property(ies)` — it picks up `[SerializeField] private` fields and accepts Object references as `{"fileID": <instanceId>}` or `{"assetPath": "Assets/..."}`.
- For field-only prefab asset edits, use `set_prefab_property(ies)` with a verified `Assets/**/*.prefab` path. If it reports duplicate paths or components, retry only with an index from that response; use Prefab Mode for structural edits.
- Read editor state through dedicated tools (`get_selection`, `get_prefab_stage`, `get_tags`, `get_layers`, `get_build_settings`); use `execute_menu_item` before falling back to ad-hoc `execute_code`.
- Never edit `.unity`, `.prefab`, or `.asset` files with shell text tools or patches; use Unity MCP / Editor APIs for scenes, prefabs, and ScriptableObject assets.
- Save only the scene or prefab assets intentionally modified, then read back exact values.
- With default `core` exposure, use the focused workflow tools. With default `full` exposure, prefer specific MCP tools for simple editor operations.
- `execute_code` refreshes the asset database and waits for compilation before running. For other tools that depend on freshly compiled code, still call `request_recompile` after external script edits.
- In `execute_code`, null-guard every lookup and return explicit missing path/object/component messages; do not run self-healing fallback loops.
- For Unity object references, do not use `??=` for lazy rebinding; use explicit `if (field == null) field = Resolve();`.
- After code or resource edits, exit Play Mode if needed, call `request_recompile`, `wait_for_compilation`, then read compilation or console errors.
- `request_recompile` is rejected while Unity is in Play Mode. Call `exit_play_mode` first, then retry.
- After `enter_play_mode`, the HTTP server briefly drops while Unity reloads the domain. Poll `tools/list` or `get_reload_recovery_status` until it responds again before issuing the next tool call.
- If recompilation triggers a domain reload or interrupts a request, treat the result as unknown until `get_reload_recovery_status`, compilation checks, and MCP readback confirm it.
- Avoid changing `Library/`, `Temp/`, `Logs/`, or `obj/`.

## Project

- Project root: `E:\数据库\OCC_Codex\UnityProject`
- Product name: `My project`

## Notes

- Re-run `Funplay > Project Skills` after changing selected skills or platforms.
<!-- /Funplay Unity MCP managed project skills -->

## OCC 项目适配（用户维护，位于 Funplay 托管区之外）

本工程沿用 [仓库工作约定](../AGENTS.md) 和完整会话中的具体授权。以下条款限定上方通用工作流及项目技能在 OCC 中的适用范围，不改变游戏产品决定。

- 游戏中的力量来源、时代、制度、组织、地点、人物背景、技术边界和叙事因果以 [OC 世界观唯一参考](../Worldbuilding/OC世界观/README.md) 及其索引为准；OCC 总案负责玩法规则。实现不得从旧策划、归档或发布镜像恢复相冲突的世界设定。
- Unity 编辑、编译、连接或场景对象任务按需读取 [Unity MCP Workflow](.codex/skills/funplay-unity-mcp-workflow/SKILL.md)；实际 uGUI 制作或审查再读取 [Unity UI Composition](.codex/skills/funplay-unity-ui-composition/SKILL.md)。简单脚本或技能维护不预读整套 UI 流程。
- 操作前核对主工程 `Application.dataPath`。技能例子中的对象名、Prefab 路径和数值仅是示例，不能当作 OCC 中已存在的事实。
- “修改并保存场景”“进入 Play Mode 验证”等通用步骤仍以仓库规则为准：进入 Play Mode 已获项目级默认授权，保存场景仍需明确授权。只保存获准且本次涉及的场景或资产，避免用全局 SaveAssets 夹带无关修改。
- 普通源码/资源修改执行必要导入编译与 Console 检查；纯 Markdown/技能变更不重新编译。身份、版本和当前状态只读核验可用 skip_refresh=true。明确通过后不重复刷新或扩大测试。
- UI 保持 OCC 当前 PC 像素合同：1920×1080、左地图75%/右HUD25%，资产尺寸按总案与机器合同分层。移动端720×1559/1559×720、刘海安全区、全套手机/平板设备验证只在实际任务涉及对应平台时适用。
- 稳定的界面结构优先落在 Prefab 或场景层级中，运行时只负责真实数据绑定、重复列表、瞬时反馈和必须按战局生成的内容；现有动态 UI 按可验收的完整样板逐步迁移，不要求一次性重构全部屏幕。文本组件和绑定方式沿用当前工程兼容口径。
- 采用新版 TMP 规则：设计确需文本效果时控制局部材质作用范围，不影响无关标签。生图原料及正式资产仍遵守根目录的来源、manifest、人工审美和入库要求。
- Funplay 托管块和生成技能由插件同步。持久项目修订写在本节或个人 funplay-unity-dev 技能；不要直接修改 Library/PackageCache 或手改托管文件来伪装升级。
