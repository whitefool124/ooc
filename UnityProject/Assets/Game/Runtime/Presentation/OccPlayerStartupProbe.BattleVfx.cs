using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    public sealed partial class OccPlayerStartupProbe
    {
        private IEnumerator RenderBattleVfxSample(CombatPrototypeBootstrap host)
        {
            string folder = Path.GetDirectoryName(output);
            bool captureFrames = Environment.GetCommandLineArgs().Contains("-aether-vfx-captures");
            var opening = host.GetComponent<FirstExperiencePrototypeController>();
            opening.ShowLanding(); yield return new WaitForSecondsRealtime(.3f);
            opening.StartGameFromDesk(); yield return null;
            if (opening.CurrentStage != FirstExperiencePrototypeController.FlowStage.Configuration) throw new InvalidOperationException("New-game configuration missing");
            opening.ConfirmConfiguration(); opening.EnterAcademy(); opening.ChooseTutorial(false);
            yield return new WaitForSecondsRealtime(1f);
            var run = host.CurrentMapRun;
            if (run == null) throw new InvalidOperationException("Normal new-game entry did not create a run");
            if (run.OpeningGiftPending)
            {
                string gift = run.OpeningGift.CandidateIds.FirstOrDefault(id => id != "LK-05" && id != "LK-06");
                if (gift != null) host.ChooseOpeningGift(gift);
            }
            if (run.DeparturePending && !host.ConfirmAcademyDeparture()) throw new InvalidOperationException("Departure rejected");
            host.StartMapNodeCombat("dorm_drill");
            float deadline = Time.realtimeSinceStartup + 20;
            while ((host.CurrentState == null || !host.IsDeveloperCombatActive || host.IsCombatActionPlaying) && Time.realtimeSinceStartup < deadline) yield return null;
            yield return new WaitForSecondsRealtime(1f);
            var battle = FindAnyObjectByType<Hd2DBattlefieldSample>();
            if (battle == null || host.CurrentState == null) throw new InvalidOperationException("Normal battlefield did not initialize");
            var frames = new List<object>(); var commands = new List<object>();
            int peak = 0, captures = 0, legalActions = 0, damageBefore = host.CurrentState.Units.Values.Where(u => !u.IsHero).Sum(u => u.Health);
            bool actualDamage = false;
            float start = Time.realtimeSinceStartup; deadline = start + 45;
            while (Time.realtimeSinceStartup < deadline && !host.CurrentState.IsVictory && !host.CurrentState.IsDefeat)
            {
                var state = host.CurrentState; var vfx = battle.WorldParticles;
                int particles = vfx == null ? 0 : vfx.ActiveParticles; peak = Mathf.Max(peak, particles);
                frames.Add(new { time = Time.realtimeSinceStartup - start, actor = state.ActiveUnitId, particles,
                    slots = vfx == null ? 0 : vfx.ActiveSlots, lights = vfx == null ? 0 : vfx.ActiveLights,
                    enemyHealth = state.Units.Values.Where(u => !u.IsHero).Sum(u => u.Health), frameMs = Time.unscaledDeltaTime * 1000f });
                actualDamage |= state.Units.Values.Where(u => !u.IsHero).Sum(u => u.Health) < damageBefore;
                if (captureFrames && particles > 0 && captures < 3)
                {
                    OccPlayerRenderCapture.Capture(battle, 1920, 1080, Path.Combine(folder, "battle-vfx-" + captures + ".png"));
                    OccPlayerRenderCapture.Capture(battle, 960, 540, Path.Combine(folder, "battle-vfx-" + captures + "-960.png"));
                    captures++;
                }
                if (actualDamage && peak > 0 && !host.IsCombatActionPlaying) break;
                if (state.ActiveUnitId == "hero" && !host.IsCombatActionPlaying && !host.IsInteractionModalOpen)
                {
                    bool submitted = false;
                    foreach (string action in new[] { "技能2", "技能3", "技能4", "攻击", "移动" })
                    {
                        host.SelectHudAction(action);
                        var enemy = state.Units.Values.Where(u => !u.IsHero && u.IsAlive).OrderBy(u => Math.Abs(u.Position.X - state.GetUnit("hero").Position.X) + Math.Abs(u.Position.Y - state.GetUnit("hero").Position.Y)).FirstOrDefault();
                        var cells = new List<GridPosition>();
                        for (int y = 0; y < state.Map.Height; y++) for (int x = 0; x < state.Map.Width; x++) cells.Add(new GridPosition(x, y));
                        foreach (var cell in cells.OrderBy(p => enemy == null ? 0 : Math.Abs(p.X - enemy.Position.X) + Math.Abs(p.Y - enemy.Position.Y)))
                        {
                            if (cell == state.GetUnit("hero").Position) continue;
                            host.SelectHudAction(action); host.PreviewBattlefieldCell(cell);
                            if (!host.CurrentActionPreview.CanSubmit || host.CurrentActionPreview.ValidCellCount == 0) continue;
                            if (action == "移动" && !host.CanQuickMoveTo(cell)) continue;
                            var model = host.PresentBattlefieldCell(cell);
                            bool valid = action == "移动" ? model.MoveMarker != BattlefieldCellMarker.None : action == "攻击" ? model.AttackMarker != BattlefieldCellMarker.None : model.SkillMarker != BattlefieldCellMarker.None;
                            if (!valid) continue;
                            host.SubmitBattlefieldCell(cell, false);
                            commands.Add(new { action, cell = cell.ToString(), at = Time.realtimeSinceStartup - start });
                            legalActions++; submitted = true; break;
                        }
                        if (submitted) break;
                    }
                    if (!submitted) host.EndHeroTurn();
                }
                yield return null;
            }
            var system = battle.WorldParticles;
            if (system == null || peak == 0 || !actualDamage) throw new InvalidOperationException("Real combat particles or actual damage not observed");
            system.Clear();
            if (system.ActiveParticles != 0 || system.ActiveLights != 0) throw new InvalidOperationException("Clear left particles or lights");
            File.WriteAllText(Path.Combine(folder, "battle-vfx-runtime.json"), Newtonsoft.Json.JsonConvert.SerializeObject(new {
                status = "PASS", entry = "Normal new-game configuration, academy departure, dorm_drill; public UI preview/submit boundary", isolatedSaveScope = true,
                noLoadoutOrCombatStateInjection = true, peak, legalActions, actualDamage, captures, commands, frames,
                humanAestheticReview = "PENDING", captureFrames,
                performanceBudgetVerdict = "NOT_MEASURED: integration probe, not a paired performance benchmark"
            }, Newtonsoft.Json.Formatting.Indented));
        }
    }
}
