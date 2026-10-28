using System;
using System.IO;
using System.Linq;
using System.Reflection;
using OCC.Combat.Presentation;
using Unity.Pipeline.Commands;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OCC.Combat.EditorTools
{
    // Captures the live combat panel offscreen without opening or focusing a window.
    public static class AutoCombatPanelReview
    {
        [CliCommand("occ_auto_combat_review", "Inspect and capture the live auto combat panel without focusing a window.")]
        public static object Review(string action, string output = "")
        {
            if (Application.dataPath.Replace('\\', '/') != "E:/数据库/OCC_Codex/UnityProject/Assets")
                throw new InvalidOperationException("Wrong main project");
            if (action == "inspect")
            {
                var current = UnityEngine.Object.FindAnyObjectByType<CombatPrototypeBootstrap>();
                return new
                {
                    Application.isPlaying,
                    frame = Time.frameCount,
                    scenes = Enumerable.Range(0, SceneManager.sceneCount).Select(index => new
                    { SceneManager.GetSceneAt(index).path, SceneManager.GetSceneAt(index).isDirty }).ToArray(),
                    node = current?.CurrentMapRun?.CurrentNodeId,
                    available = current?.CurrentMapRun?.AvailableNodes.Select(node => node.Id).ToArray(),
                    active = current?.IsDeveloperCombatActive,
                    entry = current?.IsCombatEntryBlocking,
                    panel = current?.IsAutoCombatPanelOpen
                };
            }
            if (!Application.isPlaying) throw new InvalidOperationException("Play Mode required");
            Application.runInBackground = true;
            var host = UnityEngine.Object.FindAnyObjectByType<CombatPrototypeBootstrap>();
            if (host == null) throw new InvalidOperationException("Combat host missing");
            if (action == "enter")
            {
                var node = host.CurrentMapRun?.AvailableNodes.FirstOrDefault(candidate => candidate.IsCombat &&
                    candidate.Type != RogueliteMapNodeType.Finale);
                if (node == null) throw new InvalidOperationException("No available combat node");
                host.StartMapNodeCombat(node.Id);
                return new { node = node.Id, active = host.IsDeveloperCombatActive };
            }
            if (action == "panel")
            {
                if (host.IsCombatEntryBlocking) host.CompleteCombatEntrySequence();
                if (!host.IsDeveloperCombatActive) throw new InvalidOperationException("Combat has not become active");
                host.OpenAutoCombatPanel();
                if (!host.IsAutoCombatPanelOpen) throw new InvalidOperationException("Panel did not open");
                return new { panel = true, frame = Time.frameCount };
            }
            if (action == "populate")
            {
                if (!host.IsDeveloperCombatActive || host.CurrentState?.RogueEquipment == null)
                    throw new InvalidOperationException("Live roguelite combat required");
                string[] definitions = { "G-T01", "G-T07", "G-T13", "G-T19" };
                var equipment = host.CurrentState.RogueEquipment;
                for (int slot = 0; slot < definitions.Length; slot++)
                {
                    var item = equipment.CreateTacticalItem("auto-review-" + slot, definitions[slot], 0, "visual-review");
                    if (!equipment.AddTacticalToBackpack(item) || !equipment.AssignQuickbar(slot, item.InstanceId))
                        throw new InvalidOperationException("Cannot populate review slot " + slot);
                }
                host.SetAutoCombatItemEnabled(0, true);
                host.SetAutoCombatReservedUses(0, 0);
                return new { equipped = definitions, frame = Time.frameCount };
            }
            if (action == "capture")
            {
                if (!host.IsAutoCombatPanelOpen || string.IsNullOrEmpty(output))
                    throw new InvalidOperationException("Panel and output path required");
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                var capture = typeof(CombatPrototypeBootstrap).Assembly
                    .GetType("OCC.Combat.Presentation.OccPlayerRenderCapture")
                    .GetMethod("Capture", BindingFlags.Static | BindingFlags.NonPublic);
                var sample = UnityEngine.Object.FindAnyObjectByType<Hd2DBattlefieldSample>();
                capture.Invoke(null, new object[] { sample, 1920, 1080, output, true });
                string half = Path.ChangeExtension(output, null) + "_960.png";
                capture.Invoke(null, new object[] { sample, 960, 540, half, true });
                return new { output, half, frame = Time.frameCount };
            }
            throw new ArgumentException("Unknown action: " + action);
        }
    }
}
