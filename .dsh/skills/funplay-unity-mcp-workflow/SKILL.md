---
name: funplay-unity-mcp-workflow
description: Efficient workflow for using Unity MCP to edit, import, compile, inspect, and test Unity projects, including screenshot and Game View recording verification.
---
<!-- Funplay Unity MCP managed project skills -->
<!-- Funplay Unity MCP skill version: unity-mcp-workflow@1.0.4 -->

# Unity MCP Workflow

Use this skill when Codex or another AI agent is working in a Unity project and needs to verify code, prefabs, UI, Play Mode behavior, screenshots, Game View recordings, scene hierarchy, console logs, domain reloads, or MCP connection issues.

## Operating Loop

1. Establish context.
   - Confirm the Unity project root and active scene.
   - Check that Unity MCP is reachable before assuming Editor state.
   - Inspect hierarchy, prefab paths, selected objects, and relevant component references through MCP.
   - If the user names an object, treat the name as a hint and verify the real Unity object path before editing.
2. Choose the edit surface.
   - Edit source files with normal repo tools, then trigger Unity recompilation.
   - Edit scene objects through Unity APIs, mark the scene dirty, and save the scene.
   - Edit prefab fields with `set_prefab_property(ies)` when available. Use `PrefabUtility.LoadPrefabContents`, `SaveAsPrefabAsset`, and `UnloadPrefabContents` for structural changes.
   - Unless the user explicitly requests a full rebuild, preserve the existing hierarchy when editing UI or GameObject prefabs and modify only the required objects, components, and serialized fields; do not recreate the entire prefab.
   - Edit ScriptableObject assets through `SerializedObject`, `EditorUtility.SetDirty`, and `AssetDatabase.SaveAssetIfDirty` / `SaveAssets`.
   - Never patch `.unity`, `.prefab`, or `.asset` YAML with shell text tools.
   - If the user is looking at an open scene instance, update the visible scene instance as well as the prefab asset when appropriate.
3. Execute changes.
   - Prefer one well-guarded `execute_code` batch over many fragile UI clicks.
   - Use null guards for every object, component, asset, and path lookup.
   - Return explicit missing-path/object/component messages that include the expected path and the scene or prefab searched.
   - Return concise before/after values from snippets.
   - Save only the assets or scenes intentionally modified.
   - Do not run self-healing fallback loops; if a reference, path, package, or tool is missing, report it once and stop or skip that item.
4. Validate.
   - Read back the changed objects through MCP.
   - For code or resource edits, exit Play Mode if needed, call `request_recompile`, call `wait_for_compilation`, then inspect compilation errors and console errors.
   - For runtime behavior, enter Play Mode or inspect live objects when needed.
   - If MCP is unreachable, do not claim scene, prefab, asset, or runtime verification.
   - Report exactly what was verified and what still requires device, store, network, or manual validation.

## Unity Serialized Asset Safety

- Do not use shell text tools, scripts, or patches to modify `.unity`, `.prefab`, or `.asset` files. These are Unity-owned serialized assets; changing them outside Unity can corrupt file IDs, prefab overrides, references, import state, or scene dirtiness.
- Shell tools may inspect or locate serialized Unity assets, but scene, prefab, and ScriptableObject modifications must go through Unity MCP tools or Editor APIs.
- For scenes, modify live objects through Unity APIs, mark only the touched scene dirty, and save that scene.
- For prefabs, use Prefab Mode tools or `PrefabUtility.LoadPrefabContents` / `SaveAsPrefabAsset` / `UnloadPrefabContents`.
- For ScriptableObjects or other `.asset` files, load the asset with `AssetDatabase`, modify serialized properties through `SerializedObject` when possible, mark that asset dirty, and save only that asset.
- If Unity readback and raw file text disagree, trust Unity readback and investigate the asset path instead of hand-editing YAML.

## Tool Exposure

- With the default `core` profile, rely on the focused workflow tools: `execute_code`, recompilation, Play Mode control, hierarchy, console logs, screenshots, Game View recording, input simulation, and performance inspection.
- With the default `full` profile, prefer specific MCP tools for simple scene, asset, GameObject, component, prefab, camera, UI, package, animation, file, or visual-feedback operations.
- If Tool Exposure is customized and a named tool is unavailable, adapt to the exposed tool list and report which expected tool is missing.

## MCP Call Pattern

If native MCP tools are not directly available, probe the local HTTP endpoint. The port is
per project, so read it from the Funplay MCP Server window (it is also the port in the
configured client entry) instead of assuming a fixed one:

```bash
PORT=24312 # replace with the port shown in the Funplay MCP Server window
curl -sS -m 1 -X POST http://127.0.0.1:$PORT/mcp \
  -H 'Content-Type: application/json' \
  -d '{"jsonrpc":"2.0","id":1,"method":"tools/list"}'
```

For multi-line `execute_code` calls over curl, generate JSON with a real encoder instead of hand-escaping C#:

```bash
node - <<'NODE'
const code = String.raw`
using UnityEngine;

public class InspectSomething
{
    public static string Run()
    {
        var obj = GameObject.Find("PracticeInGameUiRoot");
        return obj != null ? obj.name : "not found";
    }
}
`;
const payload = {
  jsonrpc: "2.0",
  id: 1,
  method: "tools/call",
  params: { name: "execute_code", arguments: { code } }
};
process.stdout.write(JSON.stringify(payload));
NODE
```

## Recommended `execute_code` Template

For non-trivial snippets, prefer `IFunplayCommand` over the legacy `public static string Run()` template. `execute_code` auto-adds `using Funplay.Editor.Tools.Scripting;` when `IFunplayCommand` is used, but include it explicitly in generated snippets for readability:

```csharp
using Funplay.Editor.Tools.Scripting;
using UnityEngine;

public class CommandScript : IFunplayCommand
{
    public void Execute(ExecutionContext ctx)
    {
        var root = GameObject.Find("PracticeInGameUiRoot");
        if (root == null)
        {
            ctx.LogWarning("PracticeInGameUiRoot not found");
            ctx.ReturnValue = "missing root";
            return;
        }

        ctx.RegisterObjectModification(root);
        ctx.Log("Found {0}, active={1}", root.name, root.activeInHierarchy);
        ctx.ReturnValue = new
        {
            name = root.name,
            active = root.activeInHierarchy
        };
    }
}
```

Use `ctx.RegisterObjectCreation(obj)`, `ctx.RegisterObjectModification(obj)`, and `ctx.DestroyObject(obj)` instead of direct Undo calls when possible. Use `ctx.Log`, `ctx.LogWarning`, and `ctx.LogError` for output returned in the MCP response without polluting the Unity Console.

## Unity C# Patterns

Add explicit `using` directives or use fully qualified types for project code. `execute_code` does not auto-inject project namespaces by default:

```csharp
var root = UnityEngine.GameObject.Find("PracticeInGameUiRoot");
var rect = root.GetComponent<UnityEngine.RectTransform>();
```

Use Unity null semantics for `UnityEngine.Object` references:

```csharp
if (image == null)
{
    return "Image missing";
}
```

Do not use `??=` to lazily resolve or rebind `UnityEngine.Object` references. Unity's destroyed or unbound serialized references can be fake-null: `field == null` returns true through Unity's overloaded operator, while C# `??=` can still treat the managed wrapper as non-null and skip the fallback assignment. Use an explicit Unity-null check instead:

```csharp
if (_hud == null)
{
    _hud = GetComponentInChildren<MyHud>(true);
}
```

For prefab edits:

```csharp
var path = "Assets/MyGame/UI/Prefabs/PF_PracticeInGameUiRoot.prefab";
var prefab = UnityEditor.PrefabUtility.LoadPrefabContents(path);
try
{
    var target = prefab.transform.Find("SafeArea/SwingCancelZone");
    if (target == null)
    {
        return "SwingCancelZone not found in prefab";
    }

    var rect = target.GetComponent<UnityEngine.RectTransform>();
    var before = rect.anchoredPosition;
    rect.anchoredPosition = new UnityEngine.Vector2(-76f, 448f);

    UnityEditor.EditorUtility.SetDirty(rect);
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(prefab, path);
    UnityEditor.AssetDatabase.SaveAssets();
    return "Prefab saved: pos " + before + " -> " + rect.anchoredPosition;
}
finally
{
    UnityEditor.PrefabUtility.UnloadPrefabContents(prefab);
}
```

For scene edits:

```csharp
var obj = UnityEngine.GameObject.Find("PracticeInGameUiRoot/SafeArea/SwingCancelZone");
if (obj == null)
{
    return "Scene object not found";
}

var rect = obj.GetComponent<UnityEngine.RectTransform>();
var before = rect.sizeDelta;
UnityEditor.Undo.RecordObject(rect, "Update cancel zone");
rect.sizeDelta = new UnityEngine.Vector2(220f, 116f);
UnityEditor.EditorUtility.SetDirty(rect);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(obj.scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(obj.scene);
return "Scene saved: size " + before + " -> " + rect.sizeDelta;
```

## Recompile And Reload

After external C# or asset file edits:

1. If Unity is in Play Mode, call `exit_play_mode` first — `request_recompile` is rejected during play because Unity does not run script compilation or domain reloads while playing.
2. Call `request_recompile`.
3. Call `wait_for_compilation`.
4. Read `get_compilation_errors` and `get_console_logs` errors before continuing.
5. If a domain reload drops or interrupts the request, call `get_reload_recovery_status` when available, re-scan the MCP endpoint if needed, then continue from `wait_for_compilation`.

Do not treat a disconnected, interrupted, or domain-reload-recovered request as a successful compile or edit. It only means the state is unknown until compilation checks and MCP readback confirm the final values.

After `enter_play_mode`, the HTTP server is briefly unreachable while Unity reloads the domain. Before issuing the next tool call, poll a cheap endpoint such as `tools/list` (or `get_reload_recovery_status` if exposed) until you get a response — do not assume the connection survives the Play Mode transition.

## Verification Checklist

Use readback snippets that print exact values, not only `success`:

```csharp
var all = UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.Transform>();
UnityEngine.Transform target = null;
for (int i = 0; i < all.Length; i++)
{
    if (all[i].name == "SwingCancelZone")
    {
        target = all[i];
        break;
    }
}

if (target == null)
{
    return "SwingCancelZone not found";
}

var rect = target.GetComponent<UnityEngine.RectTransform>();
return "path=" + target.name + "; pos=" + rect.anchoredPosition + "; size=" + rect.sizeDelta;
```

For UI work, verify prefab or scene hierarchy, sprite references, anchors, sorting order, active state, text fit, and button listeners. A populated `Content` hierarchy does not prove the user can see the UI.

For gameplay or network work, verify object identity, ownership, live instance existence, transform values, animation state, visibility, and whether client-side filters are discarding valid data.

## Failure Handling

- If MCP is unreachable, say so and fall back only to safe filesystem inspection or code edits. Do not claim scene, prefab, or runtime verification without Unity readback.
- If an object lookup fails, inspect hierarchy and prefab contents instead of inventing a path.
- If multiple matching objects exist, print their paths and choose the one matching the user-visible UI or current scene.
- If a reference, package, tool, or path is missing, return one clear error and stop or skip that item. Do not loop through guessed fallback paths, create replacement objects silently, or report success after a best-effort fallback.
- If compile errors appear after a change, fix them before Play Mode validation.
- When Unity and text files disagree for serialized scene or prefab state, trust Unity readback and inspect the asset path.

## Game View Recording

Use `capture_game_view` for static layout or a single visual state. Use `record_game_view` when the task needs evidence over time, such as animation, transitions, or a reproducible interaction sequence; do not record every routine UI edit.

1. Prepare. Finish compilation, enter Play Mode when needed, and wait for MCP reload recovery. Recording requires a graphics-enabled macOS or Windows Unity Editor with a visible, rendering Game tab. Keep that tab visible and its resolution unchanged throughout capture; hiding it or resizing the source can fail the recording. The MP4 includes overlay UI but no audio.
2. Start a short, bounded clip before performing the relevant actions. For example, call `record_game_view` with:

   ```json
   {"action":"start","duration_seconds":10,"fps":15,"max_dimension":1280}
   ```

   Save `data.recording_id` from the response, then perform the interaction. Start returns immediately; recording stops automatically at the duration limit. These are the default settings; accepted ranges are 1-120 seconds, 1-60 fps, and a 128-1920 pixel maximum edge. Aspect ratio is preserved without upscaling. Prefer a shorter clip or lower sampling rate/resolution if capture overhead is disruptive.
3. Poll `record_game_view` with `{"action":"status","recording_id":"<returned id>"}`. To finish early, use `{"action":"stop","recording_id":"<returned id>"}`, then poll status until finalization. Always pass the saved ID so a stale request cannot inspect or stop a newer recording. If another recording is already active, report it rather than stopping someone else's capture.
4. Check the receipt, not just `success`. While `data.status` is `recording` or `stopping`, the file is not ready. Read the MP4 only when `data.ready=true`; a `success=true` status query can still describe a failed recording. Stop polling on terminal `completed`, `interrupted`, or `failed` status and inspect `error`, `stop_reason`, and the actual captured extent (`frame_count`, `elapsed_seconds`, `last_frame_seconds`). Leaving Play Mode or reloading scripts finalizes early; recover the receipt after reload and treat any usable partial clip as partial evidence, not a complete test.
5. Review the actual file at `data.path`, under `<UnityProject>/Library/FunplayMcp/Recordings/`. MCP returns a local-file receipt, not video bytes or base64; the client must have access to that filesystem and a video viewer. A remote MCP connection alone does not provide file access. If video viewing is unavailable, inspect extracted frames when supported and state their limits, or report that the clip was saved but not reviewed. Do not claim to have watched an inaccessible clip or upload project footage without authorization.

- Report the reproduction steps, clip path, observed result, and any interruption or unverified portion. Combine visual evidence with Unity state readback and console checks.
- Capture is best-effort with real elapsed timestamps, not guaranteed target-fps sampling. Use it for visual behavior, not frame-accurate performance measurement; use Profiler and device tests for performance.
- If the tool, platform, or rendering prerequisites are unavailable, report the limitation and use screenshots or state checks only for what they can establish. Do not loop on terminal failures or install recording dependencies merely to bypass the limitation.


## Metadata

- Original skill id: `unity-mcp-workflow`
- Skill version: `1.0.4`
- Platform: `dsh`
- Source repository: `https://github.com/FunplayAI/funplay-unity-mcp`
