using System;
using System.IO;
using System.Linq;
using System.Text;
using OCC.Combat.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace OCC.Combat.EditorTools
{
    /// <summary>Repeatable, direct-to-screen visual review from the two real scene entries.</summary>
    [InitializeOnLoad]
    public static class UiScreenshotCapture
    {
        private const string StateKey = "OCC.UiCapture.EditorState";
        private const string FrontScene = "Assets/Scenes/CombatPrototype.unity";
        private const string ArenaScene = "Assets/Scenes/CombatTestArena.unity";
        private const string ArenaScenarioKey = "OCC.CombatUiQuickTest.PendingScenario";
        private const string ArenaScenario = "arena_n01_flank";

        [Serializable]
        private sealed class Session
        {
            public string output;
            public string originalScene;
            public string[] targets;
            public int index;
            public string startedUtc;
            public bool cancelled;
            public bool pendingLaunch;
        }

        private sealed class Target
        {
            public readonly string id, category, label, scene;
            public Target(string id, string category, string label, string scene)
            { this.id = id; this.category = category; this.label = label; this.scene = scene; }
        }

        [Serializable]
        private sealed class CaptureReceipt
        {
            public string id, category, label, status, detail, scene, utc;
            public int width, height;
        }

        [Serializable]
        private sealed class CaptureManifest
        {
            public CaptureReceipt[] captures;
        }

        private static readonly Target[] Catalog =
        {
            new Target("front_branding", "启动/品牌", "品牌开场", FrontScene),
            new Target("front_first_settings", "启动/设置", "首启设置", FrontScene),
            new Target("front_opening", "启动/世界观", "世界观开场", FrontScene),
            new Target("front_landing", "主流程/入口", "以太主界面", FrontScene),
            new Target("front_settings", "主流程/设置", "辅助设置", FrontScene),
            new Target("front_slots", "主流程/存档", "新建档案选位", FrontScene),
            new Target("front_confirm_new", "主流程/确认", "建立档案确认", FrontScene),
            new Target("front_confirm_overwrite", "主流程/确认", "覆盖档案确认", FrontScene),
            new Target("front_character", "主流程/角色", "角色选择与固定配置", FrontScene),
            new Target("front_academy_intro", "主流程/教程", "学院介绍", FrontScene),
            new Target("front_encyclopedia", "主流程/百科", "战斗百科", FrontScene),
            new Target("map_departure", "学院/出发整备", "出发整备：装备与背包", FrontScene),
            new Target("map_overview", "学院/地图", "学院地图", FrontScene),
            new Target("map_first_overview", "学院/地图", "首次教程地图", FrontScene),
            new Target("map_loadout", "学院/整备", "整备：装备与背包", FrontScene),
            new Target("map_spells", "学院/整备", "整备：术式编组", FrontScene),
            new Target("map_settings", "学院/设置", "学院辅助设置", FrontScene),
            new Target("map_archive", "学院/档案", "行程与行囊", FrontScene),
            new Target("room_origin", "学院/节点", "固定基础配置节点", FrontScene),
            new Target("room_combat", "学院/节点", "战斗节点出发准备", FrontScene),
            new Target("room_elite", "学院/节点", "精英节点出发准备", FrontScene),
            new Target("room_boss", "学院/节点", "首领节点出发准备", FrontScene),
            new Target("room_event", "学院/事件", "事件节点", FrontScene),
            new Target("room_workshop", "学院/服务", "工坊", FrontScene),
            new Target("room_medical", "学院/服务", "医务室", FrontScene),
            new Target("room_shop", "学院/服务", "商店", FrontScene),
            new Target("map_reward", "学院/战利品", "战利品清点与领取", FrontScene),
            new Target("map_reward_inventory", "学院/战利品", "待领奖励去向处理", FrontScene),
            new Target("map_reward_abandon_confirmation", "学院/确认", "放弃奖励确认", FrontScene),
            new Target("map_finale_confirmation", "学院/确认", "首领门槛确认", FrontScene),
            new Target("map_save_failure", "学院/异常", "保存失败与重试", FrontScene),
            new Target("map_version_protected", "学院/异常", "档案保护与返回", FrontScene),
            new Target("map_round_victory", "学院/结算", "单轮胜利结算", FrontScene),
            new Target("map_round_failure", "学院/结算", "单轮失败结算", FrontScene),
            new Target("arena_selector", "战斗/测试入口", "测试场选择", ArenaScene),
            new Target("arena_battle", "战斗/战场", "固定场景战斗", ArenaScene),
            new Target("arena_inventory", "战斗/整理", "战斗中整理", ArenaScene),
            new Target("arena_loot", "战斗/搜刮", "现场搜刮", ArenaScene),
            new Target("arena_confirmation", "战斗/确认", "离开战斗确认", ArenaScene),
            new Target("arena_restart_confirmation", "战斗/确认", "重开战斗确认", ArenaScene),
            new Target("arena_victory", "战斗/结果", "战斗胜利结果", ArenaScene),
            new Target("arena_defeat", "战斗/结果", "战斗失败结果", ArenaScene)
        };

        static UiScreenshotCapture()
        {
            EditorApplication.update += Poll;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        [MenuItem("OCC/UI Screenshots/Capture All")]
        public static void CaptureAll() => Begin(Catalog.Select(t => t.id).ToArray());
        [MenuItem("OCC/UI Screenshots/Stop Current Run")]
        public static void Stop()
        {
            if (!EditorPrefs.HasKey(StateKey)) return;
            var session = JsonUtility.FromJson<Session>(EditorPrefs.GetString(StateKey));
            session.cancelled = true;
            EditorPrefs.SetString(StateKey, JsonUtility.ToJson(session));
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
            else Finish(session, "Capture stopped by operator");
        }
        [MenuItem("OCC/UI Screenshots/Direct/01 Main Menu")]
        public static void MainMenu() => Begin("front_landing");
        [MenuItem("OCC/UI Screenshots/Direct/02 Settings")]
        public static void Settings() => Begin("front_settings");
        [MenuItem("OCC/UI Screenshots/Direct/03 Save Slots")]
        public static void SaveSlots() => Begin("front_slots");
        [MenuItem("OCC/UI Screenshots/Direct/04 Encyclopedia")]
        public static void Encyclopedia() => Begin("front_encyclopedia");
        [MenuItem("OCC/UI Screenshots/Direct/05 Battle")]
        public static void Battle() => Begin("arena_battle");
        [MenuItem("OCC/UI Screenshots/Direct/06 Combat Inventory")]
        public static void CombatInventory() => Begin("arena_inventory");
        [MenuItem("OCC/UI Screenshots/Direct/07 Character Selection")]
        public static void Character() => Begin("front_character");
        [MenuItem("OCC/UI Screenshots/Direct/08 Academy Map")]
        public static void AcademyMap() => Begin("map_overview");
        [MenuItem("OCC/UI Screenshots/Direct/09 Departure Equipment")]
        public static void Departure() => Begin("map_departure");
        [MenuItem("OCC/UI Screenshots/Direct/10 Spell Loadout")]
        public static void SpellLoadout() => Begin("map_spells");
        [MenuItem("OCC/UI Screenshots/Direct/11 Combat Node")]
        public static void CombatNode() => Begin("room_combat");
        [MenuItem("OCC/UI Screenshots/Direct/12 Event")]
        public static void Event() => Begin("room_event");
        [MenuItem("OCC/UI Screenshots/Direct/13 Workshop")]
        public static void Workshop() => Begin("room_workshop");
        [MenuItem("OCC/UI Screenshots/Direct/14 Medical")]
        public static void Medical() => Begin("room_medical");
        [MenuItem("OCC/UI Screenshots/Direct/15 Shop")]
        public static void Shop() => Begin("room_shop");
        [MenuItem("OCC/UI Screenshots/Direct/16 Reward")]
        public static void Reward() => Begin("map_reward");
        [MenuItem("OCC/UI Screenshots/Direct/17 Round Victory")]
        public static void RoundVictory() => Begin("map_round_victory");
        [MenuItem("OCC/UI Screenshots/Direct/18 Round Failure")]
        public static void RoundFailure() => Begin("map_round_failure");
        [MenuItem("OCC/UI Screenshots/Direct/19 Combat Victory")]
        public static void CombatVictory() => Begin("arena_victory");
        [MenuItem("OCC/UI Screenshots/Direct/20 Combat Defeat")]
        public static void CombatDefeat() => Begin("arena_defeat");

        // All other IDs remain directly callable from code via CaptureTarget, so the
        // menu stays short while an evaluator can jump to any catalog entry.
        public static void CaptureTarget(string id)
        {
            if (Catalog.All(target => target.id != id)) throw new ArgumentException("Unknown UI capture target: " + id);
            Begin(id);
        }

        private static void Begin(params string[] targets)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorPrefs.HasKey(StateKey))
            { Debug.LogWarning("UI capture is already running, or Unity is in Play Mode."); return; }
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.isDirty || string.IsNullOrEmpty(scene.path))
            { Debug.LogWarning("UI capture needs a saved, clean active scene; existing scene changes were left untouched."); return; }
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../Reports/UI/AutoCapture", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")));
            Directory.CreateDirectory(output);
            var session = new Session { output = output, originalScene = scene.path, targets = targets, index = 0 };
            EditorPrefs.SetString(StateKey, JsonUtility.ToJson(session));
            Launch(session);
        }

        private static void Launch(Session session)
        {
            Target target = Catalog.First(t => t.id == session.targets[session.index]);
            if (EditorSceneManager.GetActiveScene().isDirty)
            { Finish(session, "Scene became dirty before the next capture"); return; }
            EditorSceneManager.OpenScene(target.scene);
            if (target.id.StartsWith("arena_", StringComparison.Ordinal) && target.id != "arena_selector")
            { PlayerPrefs.SetString(ArenaScenarioKey, ArenaScenario); PlayerPrefs.Save(); }
            EditorPrefs.SetString(UiScreenshotCaptureProbe.RequestKey,
                JsonUtility.ToJson(new UiScreenshotCaptureProbe.Request
                { id = target.id, category = target.category, label = target.label, output = session.output }));
            session.startedUtc = DateTime.UtcNow.ToString("O");
            EditorPrefs.SetString(StateKey, JsonUtility.ToJson(session));
            EditorApplication.EnterPlaymode();
        }

        private static void Poll()
        {
            if (!EditorPrefs.HasKey(StateKey)) return;
            var session = JsonUtility.FromJson<Session>(EditorPrefs.GetString(StateKey));
            if (session.pendingLaunch)
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
                if (!DateTime.TryParse(session.startedUtc, out DateTime readyAt) || DateTime.UtcNow < readyAt.ToUniversalTime()) return;
                session.pendingLaunch = false;
                EditorPrefs.SetString(StateKey, JsonUtility.ToJson(session));
                Launch(session);
                return;
            }
            if (!EditorApplication.isPlaying) return;
            string id = session.targets[session.index];
            if (File.Exists(Path.Combine(session.output, id + ".json")))
            { EditorApplication.ExitPlaymode(); return; }
            if (DateTime.TryParse(session.startedUtc, out DateTime started) && (DateTime.UtcNow - started.ToUniversalTime()).TotalSeconds > 90)
            {
                WriteFailure(session.output, id, "Capture timed out after 90 seconds");
                EditorApplication.ExitPlaymode();
            }
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredEditMode || !EditorPrefs.HasKey(StateKey)) return;
            var session = JsonUtility.FromJson<Session>(EditorPrefs.GetString(StateKey));
            string id = session.targets[session.index];
            if (!File.Exists(Path.Combine(session.output, id + ".json")))
                WriteFailure(session.output, id, "Play Mode ended before the screenshot was written");
            EditorPrefs.DeleteKey(UiScreenshotCaptureProbe.RequestKey);
            PlayerPrefs.DeleteKey(ArenaScenarioKey);
            if (session.cancelled) { Finish(session, null); return; }
            session.index++;
            if (session.index >= session.targets.Length) { Finish(session, null); return; }
            session.pendingLaunch = true;
            session.startedUtc = DateTime.UtcNow.AddSeconds(1).ToString("O");
            EditorPrefs.SetString(StateKey, JsonUtility.ToJson(session));
        }

        private static void WriteFailure(string output, string id, string detail)
        {
            File.WriteAllText(Path.Combine(output, id + ".json"),
                "{\"id\":\"" + id + "\",\"status\":\"failed\",\"detail\":\"" + detail + "\"}");
        }

        private static void Finish(Session session, string failure)
        {
            if (failure != null && session.index < session.targets.Length)
                WriteFailure(session.output, session.targets[session.index], failure);
            EditorPrefs.DeleteKey(StateKey);
            EditorPrefs.DeleteKey(UiScreenshotCaptureProbe.RequestKey);
            if (File.Exists(Path.GetFullPath(Path.Combine(Application.dataPath, "..", session.originalScene))) &&
                !EditorSceneManager.GetActiveScene().isDirty)
                EditorSceneManager.OpenScene(session.originalScene);
            BuildIndex(session);
            Debug.Log("UI capture report: " + Path.Combine(session.output, "index.html"));
        }

        private static void BuildIndex(Session session)
        {
            var html = new StringBuilder("<!doctype html><html lang='zh'><meta charset='utf-8'><title>OCC UI 截图</title><style>body{background:#111821;color:#e5e8ed;font:16px sans-serif;margin:32px}section{display:grid;grid-template-columns:repeat(auto-fit,minmax(400px,1fr));gap:20px}article{background:#202b37;padding:16px;border-radius:8px}img{width:100%;height:auto}small{color:#aab8c8}textarea{display:block;width:98%;height:72px}select,textarea,button{font:inherit;margin:8px 0}nav{display:flex;flex-wrap:wrap;gap:8px;margin:20px 0}nav a{color:#d9e8f8;background:#314458;padding:7px 10px;border-radius:5px;text-decoration:none}nav a:hover{background:#42627f}article{scroll-margin-top:18px}</style><h1>OCC UI 截图评估</h1><p>截图为运行时 Game View 原图。点击下方页面名直达截图，点击截图查看原图。每个 PNG 旁的 JSON 记录分类、场景、状态和实际分辨率。评估记录保存在本浏览器，可导出 JSON。</p><button onclick='exportReview()'>导出评估 JSON</button><nav>");
            foreach (string id in session.targets)
            {
                Target target = Catalog.First(t => t.id == id);
                html.Append("<a href='#").Append(id).Append("'>").Append(target.label).Append("</a>");
            }
            html.Append("</nav><section>");
            var captures = new CaptureReceipt[session.targets.Length];
            int captureIndex = 0;
            foreach (string id in session.targets)
            {
                Target t = Catalog.First(x => x.id == id);
                string png = Path.Combine(session.output, id + ".png");
                string receipt = Path.Combine(session.output, id + ".json");
                string status = File.Exists(receipt) ? File.ReadAllText(receipt) : "missing receipt";
                CaptureReceipt record = File.Exists(receipt) ? JsonUtility.FromJson<CaptureReceipt>(status) : new CaptureReceipt { id = id, status = "missing" };
                record.category = t.category;
                record.label = t.label;
                captures[captureIndex++] = record;
                html.Append("<article id='").Append(id).Append("'><h2>").Append(t.label).Append("</h2><small>").Append(t.category).Append(" · ").Append(id).Append("</small><p>");
                if (File.Exists(png)) html.Append("<a href='").Append(id).Append(".png'><img src='").Append(id).Append(".png'></a>");
                else html.Append("截图失败");
                html.Append("</p><label>改进优先级 <select data-id='").Append(id).Append("' data-field='priority'><option value=''>未评估</option><option>P0 阻断</option><option>P1 错误或严重遮挡</option><option>P2 可读性或布局</option><option>P3 打磨</option><option>通过</option></select></label><textarea data-id='").Append(id).Append("' data-field='note' placeholder='记录具体位置、问题和预期改进'></textarea><details><summary>采集记录</summary><pre>").Append(status.Replace("&", "&amp;").Replace("<", "&lt;")).Append("</pre></details></article>");
            }
            html.Append("</section><script>const key='occ-ui-review:'+location.pathname;document.querySelectorAll('[data-id]').forEach(e=>{let k=e.dataset.id+':'+e.dataset.field;e.value=localStorage.getItem(key+k)||'';e.addEventListener('input',()=>localStorage.setItem(key+k,e.value))});function exportReview(){let rows={};document.querySelectorAll('[data-id]').forEach(e=>{let id=e.dataset.id;(rows[id]??={})[e.dataset.field]=e.value});let a=document.createElement('a');a.href=URL.createObjectURL(new Blob([JSON.stringify(rows,null,2)],{type:'application/json'}));a.download='review.json';a.click();setTimeout(()=>URL.revokeObjectURL(a.href),1000)}</script></html>");
            File.WriteAllText(Path.Combine(session.output, "index.html"), html.ToString(), Encoding.UTF8);
            File.WriteAllText(Path.Combine(session.output, "manifest.json"),
                JsonUtility.ToJson(new CaptureManifest { captures = captures }, true), Encoding.UTF8);
        }
    }
}
