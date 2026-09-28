#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    // Test-only bridge: the Editor chooses a route, the running game renders and captures it.
    public sealed class UiScreenshotCaptureProbe : MonoBehaviour
    {
        public const string RequestKey = "OCC.UiCapture.Request";

        [Serializable]
        public sealed class Request
        {
            public string id;
            public string category;
            public string label;
            public string output;
        }

        [Serializable]
        private sealed class Receipt
        {
            public string id;
            public string category;
            public string label;
            public string status;
            public string detail;
            public string scene;
            public int width;
            public int height;
            public string utc;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Attach()
        {
            string json = EditorPrefs.GetString(RequestKey, string.Empty);
            if (string.IsNullOrEmpty(json)) return;
            var host = new GameObject("OCC UI Screenshot Capture Probe");
            DontDestroyOnLoad(host);
            host.AddComponent<UiScreenshotCaptureProbe>().request = JsonUtility.FromJson<Request>(json);
        }

        private Request request;
        private string deferredPage;
        private string deferredNode;

        private IEnumerator Start()
        {
            if (request == null || string.IsNullOrEmpty(request.output)) yield break;
            yield return null;
            yield return null;
            string error = PrepareRoute();
            if (error == null)
            {
                if (deferredPage != null)
                {
                    yield return null;
                    yield return null;
                    var previewBootstrap = FindAnyObjectByType<CombatPrototypeBootstrap>();
                    if (previewBootstrap == null) error = "Preview bootstrap missing";
                    else if (deferredPage == "reward_inventory")
                        error = previewBootstrap.PreviewUiCaptureRewardInventory();
                    else if (deferredPage == "reward_abandon_confirmation")
                    {
                        previewBootstrap.RequestAbandonMapReward();
                        if (!previewBootstrap.IsInteractionModalOpen) error = "Abandon reward confirmation did not open";
                    }
                    else if (deferredPage == "finale_confirmation")
                    {
                        var run = previewBootstrap.CurrentMapRun;
                        if (run == null || !run.IsNodeAvailable("core_finale"))
                            error = "Academy finale preview is not available: " + run?.CurrentNodeId + "/" + run?.StageTime;
                        previewBootstrap.SelectMapNode("core_finale");
                        if (error == null && !previewBootstrap.IsInteractionModalOpen) error = "Academy finale gate did not open";
                    }
                    else if (deferredPage == "save_failure" || deferredPage == "version_protected")
                    {
                        error = previewBootstrap.PreviewUiCaptureSaveFailure(deferredPage == "version_protected");
                        if (error == null && previewBootstrap.IsMapRunSaved) error = "Save failure preview did not activate";
                    }
                    else error = previewBootstrap.PreviewUiCapturePage(deferredPage, deferredNode);
                }
                // Give the production UI its normal Update, layout and transition frames.
                for (int i = 0; i < 12; i++) yield return null;
                if (request.id.StartsWith("arena_", StringComparison.Ordinal) && request.id != "arena_selector")
                {
                    var bootstrap = FindAnyObjectByType<CombatPrototypeBootstrap>();
                    if (bootstrap == null || !bootstrap.IsDeveloperCombatActive)
                        error = "Arena combat did not become active";
                    else if (request.id == "arena_inventory")
                    {
                        bootstrap.OpenCombatInventoryPanel();
                        var inventory = FindAnyObjectByType<TarkovInventoryPanel>();
                        if (inventory == null || !inventory.IsOpen)
                            error = "Combat inventory could not open from this scenario";
                    }
                    else if (request.id == "arena_loot")
                    {
                        var state = bootstrap.CurrentState;
                        var hero = state?.GetUnit("hero");
                        if (hero == null) error = "Loot preview hero missing";
                        else
                        {
                            state.SetLootSource(new LootSourceState("UI-CAPTURE-LOOT", hero.Position,
                                new[] { new ItemInstance("UI-CAPTURE-MEDKIT", "medkit", 0) }));
                            bootstrap.OpenCombatInventoryPanel();
                            var inventory = FindAnyObjectByType<TarkovInventoryPanel>();
                            if (inventory == null || !inventory.IsOpen) error = "Loot screen did not open";
                        }
                    }
                    else if (request.id == "arena_victory" || request.id == "arena_defeat")
                    {
                        bootstrap.ForceCurrentOutcome(request.id == "arena_victory");
                        CombatFlowPhase wanted = request.id == "arena_victory" ? CombatFlowPhase.Victory : CombatFlowPhase.Defeat;
                        if (bootstrap.CurrentFlowPhase != wanted) error = "Combat result page did not open";
                    }
                    else if (request.id == "arena_confirmation")
                    {
                        bootstrap.RequestLeaveCombat();
                        if (!bootstrap.IsInteractionModalOpen) error = "Leave confirmation did not open";
                    }
                    else if (request.id == "arena_restart_confirmation")
                    {
                        bootstrap.RequestTacticalRestart();
                        if (!bootstrap.IsInteractionModalOpen) error = "Restart confirmation did not open";
                    }
                    for (int i = 0; i < 12; i++) yield return null;
                }
                else
                {
                    var flow = FindAnyObjectByType<FirstExperiencePrototypeController>();
                    var bootstrap = FindAnyObjectByType<CombatPrototypeBootstrap>();
                    if (request.id == "front_encyclopedia")
                    {
                        if (bootstrap == null || !bootstrap.IsEncyclopediaOpen) error = "Encyclopedia did not open";
                    }
                    else if (request.id.StartsWith("front_", StringComparison.Ordinal) &&
                             (flow == null || flow.CurrentStage != ExpectedFrontStage(request.id)))
                        error = "Requested front-end stage did not open";
                    else if ((request.id.StartsWith("map_", StringComparison.Ordinal) || request.id.StartsWith("room_", StringComparison.Ordinal)) &&
                             (bootstrap == null || bootstrap.CurrentMapRun == null ||
                              request.id != "map_save_failure" && request.id != "map_version_protected" && !bootstrap.IsMapRunSaved))
                        error = "In-memory map preview did not become visible";
                }
            }
            yield return new WaitForEndOfFrame();
            var receipt = new Receipt
            {
                id = request.id,
                category = request.category,
                label = request.label,
                status = error == null ? "captured" : "failed",
                detail = error ?? string.Empty,
                scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,
                utc = DateTime.UtcNow.ToString("O")
            };
            try
            {
                if (error == null)
                {
                    Texture2D image = ScreenCapture.CaptureScreenshotAsTexture();
                    if (image == null) throw new InvalidOperationException("Screen capture returned no texture");
                    receipt.width = image.width;
                    receipt.height = image.height;
                    File.WriteAllBytes(Path.Combine(request.output, request.id + ".png"), image.EncodeToPNG());
                    Destroy(image);
                }
            }
            catch (Exception exception)
            {
                receipt.status = "failed";
                receipt.detail = exception.Message;
            }
            File.WriteAllText(Path.Combine(request.output, request.id + ".json"), JsonUtility.ToJson(receipt, true));
            EditorPrefs.DeleteKey(RequestKey);
        }

        private string PrepareRoute()
        {
            var bootstrap = FindAnyObjectByType<CombatPrototypeBootstrap>();
            if (bootstrap == null) return "CombatPrototypeBootstrap missing";
            if (request.id.StartsWith("arena_", StringComparison.Ordinal))
            {
                if (FindAnyObjectByType<CombatTestArenaEntry>() == null) return "Dedicated arena entry missing";
                // CombatTestArenaEntry consumes this existing quick-test key and builds a real battle.
                return null;
            }
            if (request.id.StartsWith("map_", StringComparison.Ordinal) || request.id.StartsWith("room_", StringComparison.Ordinal))
            {
                bool academy = request.id.StartsWith("map_", StringComparison.Ordinal);
                bool departure = request.id == "map_departure";
                deferredNode = request.id == "room_origin" ? "O" :
                    request.id == "room_combat" ? "B1" :
                    request.id == "room_elite" ? "X" :
                    request.id == "room_boss" ? "core_finale" :
                    request.id == "room_event" ? "EV1" :
                    request.id == "room_workshop" ? "W" :
                    request.id == "room_medical" ? "M" :
                    request.id == "room_shop" ? "S" :
                    request.id == "map_reward" || request.id == "map_reward_inventory" ||
                    request.id == "map_reward_abandon_confirmation" ? "B1" :
                    request.id == "map_round_victory" ? "core_finale" :
                    request.id == "map_round_failure" ? "dorm_drill" : null;
                if (request.id == "room_boss") academy = true;
                if (request.id == "map_reward" || request.id == "map_reward_inventory" ||
                    request.id == "map_reward_abandon_confirmation" || request.id == "map_first_overview") academy = false;
                string special = request.id == "map_reward" || request.id == "map_reward_inventory" ||
                    request.id == "map_reward_abandon_confirmation" ? "reward" :
                    request.id == "map_round_victory" ? "victory" :
                    request.id == "map_round_failure" ? "failure" :
                    request.id == "map_finale_confirmation" ? "finale_gate" : null;
                string error = bootstrap.PreviewUiCaptureRun(academy, deferredNode, departure, special);
                if (error != null) return error;
                deferredPage = request.id == "map_reward_inventory" ? "reward_inventory" :
                    request.id == "map_reward_abandon_confirmation" ? "reward_abandon_confirmation" :
                    request.id == "map_finale_confirmation" ? "finale_confirmation" :
                    request.id == "map_save_failure" ? "save_failure" :
                    request.id == "map_version_protected" ? "version_protected" :
                    request.id == "map_departure" || request.id == "map_loadout" ? "loadout" :
                    request.id == "map_spells" ? "spells" :
                    request.id == "map_settings" ? "settings" :
                    request.id == "map_archive" ? "archive" :
                    request.id.StartsWith("room_", StringComparison.Ordinal) ? "room" : "map";
                return null;
            }
            var flow = FindAnyObjectByType<FirstExperiencePrototypeController>();
            if (flow == null) return "First experience controller missing";
            switch (request.id)
            {
                case "front_branding": flow.PreviewUiCaptureStage(FirstExperiencePrototypeController.FlowStage.Branding); return null;
                case "front_first_settings": flow.PreviewUiCaptureStage(FirstExperiencePrototypeController.FlowStage.FirstSettings); return null;
                case "front_opening": flow.ReplayWorldOpening(); return null;
                case "front_landing": flow.ShowLanding(); return null;
                case "front_settings": flow.PreviewUiCaptureStage(FirstExperiencePrototypeController.FlowStage.FirstSettings, true); return null;
                case "front_slots": flow.PreviewUiCaptureStage(FirstExperiencePrototypeController.FlowStage.SaveSelection); return null;
                case "front_confirm_new": flow.PreviewUiCaptureStage(FirstExperiencePrototypeController.FlowStage.SaveConfirmation); return null;
                case "front_confirm_overwrite": flow.PreviewUiCaptureStage(FirstExperiencePrototypeController.FlowStage.SaveConfirmation, true); return null;
                case "front_character": flow.PreviewUiCaptureStage(FirstExperiencePrototypeController.FlowStage.Configuration); return null;
                case "front_academy_intro": flow.PreviewUiCaptureStage(FirstExperiencePrototypeController.FlowStage.AcademyIntro); return null;
                case "front_encyclopedia": flow.ShowLanding(); flow.OpenEncyclopedia(); return null;
                default: return "Unknown capture route: " + request.id;
            }
        }

        private static FirstExperiencePrototypeController.FlowStage ExpectedFrontStage(string id)
        {
            switch (id)
            {
                case "front_branding": return FirstExperiencePrototypeController.FlowStage.Branding;
                case "front_first_settings":
                case "front_settings": return FirstExperiencePrototypeController.FlowStage.FirstSettings;
                case "front_opening": return FirstExperiencePrototypeController.FlowStage.WorldOpening;
                case "front_slots": return FirstExperiencePrototypeController.FlowStage.SaveSelection;
                case "front_confirm_new":
                case "front_confirm_overwrite": return FirstExperiencePrototypeController.FlowStage.SaveConfirmation;
                case "front_character": return FirstExperiencePrototypeController.FlowStage.Configuration;
                case "front_academy_intro": return FirstExperiencePrototypeController.FlowStage.AcademyIntro;
                default: return FirstExperiencePrototypeController.FlowStage.Landing;
            }
        }
    }
}
#endif
