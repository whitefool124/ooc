using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using OCC.Combat.Presentation;

namespace OCC.Combat.EditorTools
{
    public static class CombatParticleSystemAudit
    {
        private static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
        private static string Output => Path.Combine(Root, "Worldbuilding/归档/2026-10-23_战斗粒子系统");
        [MenuItem("OCC/Verification/Battle Particles/Build Windows")]
        public static void BuildWindows()
        {
            Preflight();
            OccWindowsPlayerBuild.BuildTo(Path.Combine(Root, "Builds/Aether_BattleParticles_20261023_v2/以太.exe"));
        }
        [MenuItem("OCC/Verification/Battle Particles/Preflight")]
        public static void Preflight()
        {
            Directory.CreateDirectory(Output);
            var scenes = Enumerable.Range(0, SceneManager.sceneCount).Select(i => SceneManager.GetSceneAt(i)).ToArray();
            File.WriteAllText(Path.Combine(Output, "preflight.txt"), Application.dataPath + "\n" +
                string.Join("\n", scenes.Select(s => s.path + " dirty=" + s.isDirty)));
            if (Application.dataPath.Replace('\\', '/') != "E:/数据库/OCC_Codex/UnityProject/Assets") throw new InvalidOperationException("Wrong project");
            if (scenes.Any(s => s.isDirty)) throw new InvalidOperationException("Dirty scene");
        }

        [MenuItem("OCC/Verification/Battle Particles/Local Checks")]
        public static void LocalChecks()
        {
            Preflight();
            var root = new GameObject("Particle audit transient world") { hideFlags = HideFlags.HideAndDontSave, layer = 15 };
            var records = new System.Collections.Generic.List<object>();
            try
            {
                var vfx = CombatWorldParticles.Create(root.transform);
                string[] effects = { "hit", "heavy_hit", "fire_cast", "fire_projectile", "ether_projectile", "metal_projectile", "fire_melee_arc", "fire_spray", "fire_line", "fire_impact", "fire_cross_blast", "fire_detonate", "fire_overlimit", "fire_burning_ground", "fire_wall", "burning", "health_repair", "mana_restore", "fire_absorb", "cleanse", "bound", "slow", "armor_break", "fire_break_stance", "object_damage", "object_break", "path", "fire_attachment", "ether_attribute", "ether_utility" };
                foreach (string effect in effects)
                {
                    vfx.Clear(); var cell = new GridPosition(2, 2);
                    var cue = new CombatVfxCue(cell, effect, 0, .42f, new GridPosition(0, 2), effect.EndsWith("projectile"));
                    var key = new CombatVfxSlot(cell, CombatVfxLayer.Ability); int peak = 0; Bounds bounds = default;
                    for (int frame = 0; frame < 12; frame++)
                    {
                        vfx.Sample(new[] { new CombatVfxSample(key, cue, frame / 12f, 10f) }, 1f);
                        peak = Mathf.Max(peak, vfx.ActiveParticles);
                        if (frame == 0) bounds = vfx.MotionBounds; else bounds.Encapsulate(vfx.MotionBounds);
                    }
                    if (peak == 0) throw new InvalidOperationException("No particles: " + effect);
                    // Same named effect must restart after replacement, including before its old end.
                    vfx.Sample(new[] { new CombatVfxSample(key, cue, 0f, 11f) }, 1f);
                    if (vfx.ActiveSlots != 1) throw new InvalidOperationException("Slot replacement leaked");
                    vfx.Sample(Array.Empty<CombatVfxSample>(), 1f);
                    if (vfx.ActiveParticles != 0 || vfx.ActiveLights != 0) throw new InvalidOperationException("Expired effect leaked");
                    records.Add(new { effect, peak, observedBounds = bounds.ToString(), pixelMetres = 1f / 32f });
                }
                foreach (CombatFeedbackKind kind in Enum.GetValues(typeof(CombatFeedbackKind)))
                {
                    string effect = CombatPixelVfxPolicy.Feedback(kind);
                    bool mapped = CombatParticleRecipe.TryGet(effect, out _);
                    if (mapped == CombatPixelVfxPolicy.IsShieldResource(kind)) throw new InvalidOperationException("Feedback coverage: " + kind);
                }
                var fields = new[] { new GridPosition(2, 2), new GridPosition(3, 2) };
                vfx.SampleFields(fields, 0f, 1f); vfx.SampleFields(fields, .2f, 1f);
                if (vfx.FieldSlots != 2 || vfx.ActiveParticles == 0) throw new InvalidOperationException("Field particles missing");
                vfx.SampleFields(Array.Empty<GridPosition>(), .3f, 1f);
                if (vfx.FieldSlots != 0 || vfx.ActiveParticles != 0) throw new InvalidOperationException("Removed fireground leaked");
                var many = Enumerable.Range(0, 100).Select(i => new CombatVfxSample(new CombatVfxSlot(new GridPosition(i, 0), CombatVfxLayer.Ability), new CombatVfxCue(new GridPosition(i, 0), "fire_impact", 0, .18f), .2f, 30f)).ToArray();
                vfx.Sample(many, 1f);
                if (vfx.ActiveSlots != CombatWorldParticles.MaxTransientSlots || vfx.ActiveLights > CombatWorldParticles.MaxLights) throw new InvalidOperationException("Budget not enforced");
                vfx.Sample(many, 0f);
                if (vfx.ActiveSlots != 0 || vfx.ActiveParticles != 0 || vfx.ActiveLights != 0) throw new InvalidOperationException("Animation-off leaked");
                File.WriteAllText(Path.Combine(Output, "local-checks.json"), Newtonsoft.Json.JsonConvert.SerializeObject(new { status = "PASS", scope = "Native ParticleSystem local checks; not normal-entry visual approval", effects = records, feedbackKinds = Enum.GetValues(typeof(CombatFeedbackKind)).Length, maxTransientSlots = CombatWorldParticles.MaxTransientSlots, maxLights = CombatWorldParticles.MaxLights, peakParticles = vfx.PeakParticles }, Newtonsoft.Json.Formatting.Indented));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}
