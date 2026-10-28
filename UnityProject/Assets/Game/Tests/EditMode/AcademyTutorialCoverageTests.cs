using System;
using System.Linq;
using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class AcademyTutorialCoverageTests
    {
        [Test]
        public void ViewingSkippingAndPartialOperationsNeverCompleteALesson()
        {
            var lesson = new AcademyTutorialLesson { id = "example", enabled = true, evidence = new[] { "preview", "operation", "result" } };
            var progress = new AcademyTutorialProgress();
            progress.Get(lesson.id).shown = true; progress.Get(lesson.id).previewed = true;
            Assert.That(progress.IsComplete(lesson), Is.False);
            progress.Observe(lesson.id, "preview"); progress.Observe(lesson.id, "operation");
            progress.enabled = false;
            Assert.That(progress.IsComplete(lesson), Is.False);
            progress.Observe(lesson.id, "result");
            Assert.That(progress.IsComplete(lesson), Is.True);
        }
        [Test]
        public void TeachingProgressResumesPerSlotAndVerificationNeverTouchesUserKeys()
        {
            using (FirstExperienceSaveRouting.BeginVerificationScope("tutorial-progress-" + Guid.NewGuid().ToString("N")))
            {
                FirstExperienceSaveRouting.SelectSlot(0);
                var progress = AcademyTutorialStore.Load(); Assert.That(progress.enabled, Is.True);
                progress.Observe("C04", "move-accepted"); progress.enabled = false; progress.highestPhase = 9;
                Assert.That(AcademyTutorialStore.Save(progress), Is.True);
                FirstExperienceSaveRouting.SelectSlot(1);
                Assert.That(AcademyTutorialStore.Load().Find("C04"), Is.Null);
                FirstExperienceSaveRouting.SelectSlot(0);
                var resumed = AcademyTutorialStore.Load();
                Assert.That(resumed.Find("C04").evidence, Does.Contain("move-accepted"));
                Assert.That(resumed.enabled, Is.False);
                Assert.That(resumed.highestPhase, Is.EqualTo(9));
            }
        }
        [Test]
        public void ActiveCatalogHasExplicitEvidenceAndUnreleasedSystemsAreExcluded()
        {
            var asset = Resources.Load<TextAsset>("Config/AcademyTutorialLessons");
            Assert.That(asset, Is.Not.Null);
            var catalog = JsonUtility.FromJson<AcademyTutorialCatalog>(asset.text);
            Assert.That(catalog.lessons.Select(l => l.id).Distinct().Count(), Is.EqualTo(catalog.lessons.Length));
            Assert.That(catalog.lessons.Count(l => l.enabled && l.required), Is.EqualTo(103));
            Assert.That(catalog.lessons.Where(l => l.enabled).All(l => l.evidence.Length > 0), Is.True);
            Assert.That(catalog.lessons.Single(l => l.id == "F06").enabled, Is.False);
            Assert.That(catalog.lessons.Single(l => l.id == "P06").enabled, Is.False);
        }
        [Test]
        public void EveryActiveLessonHasACompletePracticeBindingOrANativeControlPath()
        {
            var catalog = JsonUtility.FromJson<AcademyTutorialCatalog>(Resources.Load<TextAsset>("Config/AcademyTutorialLessons").text);
            var native = new[] { "A01", "A02", "A03", "A04", "A05", "A06", "M02", "C01", "C10", "C11" };
            foreach (var lesson in catalog.lessons.Where(l => l.enabled && l.required))
            {
                var proofs = new System.Collections.Generic.List<string>();
                if (AcademyTutorialModelExercise.CanPrepare(lesson.id))
                {
                    int count = Math.Max(1, AcademyTutorialModelExercise.Variants(lesson.id).Count);
                    for (int i = 0; i < count; i++) proofs.AddRange(AcademyTutorialModelExercise.Prepare(lesson.id, proofs).Proofs);
                }
                else if (AcademyTutorialExercise.CanPrepare(lesson.id)) proofs.AddRange(AcademyTutorialExercise.Prepare(lesson.id).Proofs);
                else { Assert.That(native, Does.Contain(lesson.id), lesson.id + " has no practice or native control path"); continue; }
                Assert.That(lesson.evidence.All(proofs.Contains), Is.True, lesson.id + " missing " + string.Join(",", lesson.evidence.Where(p => !proofs.Contains(p))));
            }
        }
        [Test]
        public void VerifiedDeviceSettingsCanCarryToAnotherNewSlotWithoutCompletingOtherLessons()
        {
            using (FirstExperienceSaveRouting.BeginVerificationScope("teaching-device-" + Guid.NewGuid().ToString("N")))
            {
                FirstExperienceSaveRouting.SelectSlot(0); AcademyTutorialStore.RecordDeviceSettingReadback();
                FirstExperienceSaveRouting.SelectSlot(1); Assert.That(AcademyTutorialStore.Load().Find("A01"), Is.Null);
                AcademyTutorialStore.CopyDeviceSettingEvidence(); var progress = AcademyTutorialStore.Load();
                Assert.That(progress.Find("A01").evidence, Is.EquivalentTo(new[] { "settings-saved", "settings-readback" }));
                Assert.That(progress.Find("C03"), Is.Null);
            }
        }
        [TestCase("X01")][TestCase("X02")][TestCase("X03")][TestCase("X04")][TestCase("X05")]
        [TestCase("X06")][TestCase("X07")][TestCase("X09")][TestCase("X10")]
        [TestCase("X11")][TestCase("X12")][TestCase("X13")][TestCase("X14")][TestCase("X15")]
        public void PracticePreviewMatchesRealRulesAndCannotMutateAnExistingCombat(string id)
        {
            var real = TrainingRangeScenarioFactory.CreateStandard();
            var realHero = real.GetUnit("hero"); var position = realHero.Position; int mana = realHero.Mana;
            var exercise = AcademyTutorialExercise.Prepare(id);
            Assert.That(exercise.Preview, Is.Not.Empty, id);
            Assert.That(exercise.Executed, Is.False);
            exercise.Execute();
            Assert.That(exercise.MatchedPreview, Is.True, id);
            Assert.That(exercise.Result, Is.EqualTo(exercise.Preview), id);
            Assert.That(realHero.Position, Is.EqualTo(position)); Assert.That(realHero.Mana, Is.EqualTo(mana));
            Assert.Throws<InvalidOperationException>(() => exercise.Execute());
        }
        [TestCase("S01")][TestCase("S02")][TestCase("S03")][TestCase("S04")][TestCase("S05")][TestCase("S06")]
        [TestCase("I01")][TestCase("I02")][TestCase("I03")][TestCase("I04")][TestCase("I05")]
        [TestCase("I06")][TestCase("I07")][TestCase("I08")][TestCase("I09")][TestCase("I10")][TestCase("G02")]
        [TestCase("X08")][TestCase("X16")][TestCase("X17")][TestCase("X18")][TestCase("X19")][TestCase("X20")]
        [TestCase("A08")][TestCase("A09")][TestCase("G01")][TestCase("M01")][TestCase("M03")]
        [TestCase("M04")][TestCase("M05")][TestCase("M06")][TestCase("M07")][TestCase("R01")]
        [TestCase("R02")][TestCase("F02")][TestCase("F03")][TestCase("F04")][TestCase("F05")]
        [TestCase("A07")][TestCase("R04")][TestCase("R06")][TestCase("F01")]
        [TestCase("E01")][TestCase("E02")][TestCase("E05")][TestCase("E07")][TestCase("E08")][TestCase("E11")]
        [TestCase("C03")][TestCase("C04")][TestCase("C05")][TestCase("C06")][TestCase("C07")][TestCase("C12")][TestCase("C13")][TestCase("C14")]
        [TestCase("E09")]
        [TestCase("C02")][TestCase("P04")]
        [TestCase("P01")][TestCase("P02")][TestCase("P05")]
        [TestCase("U05")]
        [TestCase("C08")]
        [TestCase("C15")][TestCase("P03")]
        [TestCase("E04")]
        [TestCase("E06")]
        [TestCase("C09")]
        public void IsolatedTransactionsUseRealCostsAndRoundTripResults(string id)
        {
            var exercise = AcademyTutorialModelExercise.Prepare(id);
            int steps = 0;
            while (!exercise.Executed)
            {
                string preview = exercise.Preview;
                exercise.Execute(); steps++;
                Assert.That(exercise.MatchedPreview, Is.True, id);
                Assert.That(exercise.Result, Is.EqualTo(preview), id);
                if (!exercise.Executed) exercise.PrepareNext();
                Assert.That(steps, Is.LessThan(12));
            }
            Assert.Throws<InvalidOperationException>(() => exercise.Execute());
        }
        [Test]
        public void FailureLesson_RequiresRegularRetryEvidenceAndRestoresThePracticeCombat()
        {
            var catalog = JsonUtility.FromJson<AcademyTutorialCatalog>(Resources.Load<TextAsset>("Config/AcademyTutorialLessons").text);
            var lesson = catalog.lessons.Single(value => value.id == "F02");
            var oldProgress = new AcademyTutorialProgress();
            foreach (string proof in new[] { "fixed-retry", "normal-defeat", "normal-abandon", "pause-resume" }) oldProgress.Observe("F02", proof);
            Assert.That(oldProgress.IsComplete(lesson), Is.False, "旧版自动结束本轮演练不能代替新重试规则。");
            var exercise = AcademyTutorialModelExercise.Prepare("F02");
            Assert.That(exercise.Combat.IsDefeat, Is.True);
            exercise.Execute(); exercise.PrepareNext(); exercise.Execute();
            Assert.That(exercise.Combat.IsDefeat, Is.False);
            Assert.That(exercise.Combat.GetUnit("hero").Health, Is.GreaterThan(0));
            while (!exercise.Executed) { exercise.PrepareNext(); exercise.Execute(); }
            Assert.That(exercise.MatchedPreview, Is.True);
            foreach (string proof in exercise.Proofs) oldProgress.Observe("F02", proof);
            Assert.That(oldProgress.IsComplete(lesson), Is.True);
        }
        [Test]
        public void TransientMovementCostAndCounterStatesUseTheirDeclaredConsumptionWindows()
        {
            var hero = new UnitState("hero", true, new GridPosition(1, 1));
            var enemy = new UnitState("enemy", false, new GridPosition(5, 4)); enemy.ConfigureVitality(99);
            var state = new CombatState(new GridMap(8, 6), new[] { hero, enemy }); state.ConfigureRuleset(CombatRuleset.Roguelite);
            CombatResolver.BeginTurn(state, "hero"); hero.ApplyStatus(StatusType.MoveBonus, 1, 2, "teaching");
            Assert.That(CombatMovementQuery.Budget(state, hero), Is.EqualTo(5));
            CombatResolver.Resolve(state, CombatCommand.Move("hero", new GridPosition(2, 1)));
            Assert.That(hero.HasStatus(StatusType.MoveBonus), Is.False);
            var loadout = OCC.Combat.Roguelite.RogueSpellLoadout.CreateStarter();
            var spells = new OCC.Combat.Roguelite.RogueSpellCombatRuntime(state, loadout.CreateCombatSnapshot()); state.AttachRogueSpellRuntime(spells);
            hero.ApplyStatus(StatusType.SpellCost, 1, -2, "teaching");
            var definition = spells.DefinitionAtSlot(2); int beforeMana = hero.Mana;
            Assert.That(spells.EffectiveManaCost(definition), Is.EqualTo(Math.Max(0, definition.ManaCost - 2)));
            Assert.That(hero.HasStatus(StatusType.SpellCost), Is.True, "Preview cannot consume the state");
            spells.ExecuteSlot(2, CombatCommand.UseSkill("hero", 2, "hero"));
            Assert.That(hero.HasStatus(StatusType.SpellCost), Is.False);
            Assert.That(hero.Mana, Is.EqualTo(beforeMana - Math.Max(0, definition.ManaCost - 2)));
            hero.Equip(hero.MainHand, CombatCatalog.FrostBind, hero.SkillTwo);
            hero.AdjustCooldown(hero.SkillOne.Id, 2); Assert.That(hero.IsSkillReady(hero.SkillOne), Is.False);
            hero.AdjustCooldown(hero.SkillOne.Id, -99); Assert.That(hero.IsSkillReady(hero.SkillOne), Is.True);
        }
        [Test]
        public void AdjacentCounterTriggersOnceAndExpiresAtTheNextOwnTurn()
        {
            var hero = new UnitState("hero", true, new GridPosition(1, 1)); hero.Equip(CombatCatalog.Hammer, hero.SkillOne, hero.SkillTwo);
            var enemy = new UnitState("enemy", false, new GridPosition(2, 1)); enemy.ConfigureVitality(99);
            var state = new CombatState(new GridMap(5, 5), new[] { hero, enemy }); state.ConfigureRuleset(CombatRuleset.Roguelite);
            CombatResolver.BeginTurn(state, "hero");
            CombatEffectExecutor.Execute(state, "hero", CombatEffect.AbsorbShield("hero", 99), CombatEffect.AbsorbShield("enemy", 99));
            enemy.ApplyStatus(StatusType.Counter, 1, 4, "teaching");
            int health = hero.Health; CombatResolver.ResolveWeaponAttack(state, "hero", "enemy");
            Assert.That(hero.Health, Is.EqualTo(health - 4)); Assert.That(enemy.HasStatus(StatusType.Counter), Is.False);
            CombatResolver.ResolveWeaponAttack(state, "hero", "enemy"); Assert.That(hero.Health, Is.EqualTo(health - 4));
            enemy.ApplyStatus(StatusType.Counter, 1, 4, "teaching"); CombatStatusLifecycle.ResolveTurnStart(state, enemy);
            Assert.That(enemy.HasStatus(StatusType.Counter), Is.False);
        }
        [TestCase("U01")][TestCase("U02")][TestCase("U03")][TestCase("U04")][TestCase("R03")][TestCase("R05")][TestCase("E12")][TestCase("E10")][TestCase("E03")][TestCase("E13")][TestCase("E08")][TestCase("C14")]
        public void EveryStateInstanceHasADeterministicProductionRuleExercise(string id)
        {
            var completed = new System.Collections.Generic.List<string>();
            int cases = Math.Max(1, AcademyTutorialModelExercise.Variants(id).Count);
            for (int i = 0; i < cases; i++)
            {
                var exercise = AcademyTutorialModelExercise.Prepare(id, completed);
                int steps = 0;
                while (!exercise.Executed)
                {
                    exercise.Execute(); Assert.That(exercise.MatchedPreview, Is.True, id);
                    if (!exercise.Executed) exercise.PrepareNext();
                    Assert.That(++steps, Is.LessThan(8), id);
                }
                completed.AddRange(exercise.Proofs);
            }
            if (cases > 1) Assert.That(AcademyTutorialModelExercise.Variants(id).All(key => completed.Contains(key)), Is.True);
        }
    }
}
