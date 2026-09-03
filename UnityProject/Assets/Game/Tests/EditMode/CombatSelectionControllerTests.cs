using System.Linq;
using System.Reflection;
using NUnit.Framework;
using OCC.Combat.Roguelite;
using OCC.Combat.Presentation;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class CombatSelectionControllerTests
    {
        [Test]
        public void SelectActionAndReset_ClearTargetAndKeyboardCursor()
        {
            CombatState state = State(out UnitState hero, out UnitState enemy);
            CombatResolver.BeginTurn(state, hero.Id);
            CombatSelectionController selection = new CombatSelectionController();
            selection.SetTarget(state, enemy.Id);
            selection.BeginKeyboardTargeting(state);

            selection.SelectAction("攻击");

            Assert.That(selection.Action, Is.EqualTo("攻击"));
            Assert.That(selection.TargetId, Is.Null);
            Assert.That(selection.IsKeyboardTargeting, Is.False);
            selection.SetKnownTarget(enemy.Id);
            selection.Reset();
            Assert.That(selection.Action, Is.EqualTo("移动"));
            Assert.That(selection.TargetId, Is.Null);
        }

        [Test]
        public void KeyboardTargeting_StartsAtSelectionMovesAndTracksEnemyOnly()
        {
            CombatState state = State(out UnitState hero, out UnitState enemy);
            CombatResolver.BeginTurn(state, hero.Id);
            CombatSelectionController selection = new CombatSelectionController();
            selection.SetTarget(state, enemy.Id);

            Assert.That(selection.BeginKeyboardTargeting(state), Is.True);
            Assert.That(selection.KeyboardPosition, Is.EqualTo(enemy.Position));
            Assert.That(selection.MoveKeyboardTarget(state, -1, 0), Is.True);
            Assert.That(selection.TargetId, Is.Null);
            Assert.That(selection.MoveKeyboardTarget(state, 1, 0), Is.True);
            Assert.That(selection.TargetId, Is.EqualTo(enemy.Id));
        }

        [Test]
        public void CommitAndCancel_EndCursorWithExistingTargetSemantics()
        {
            CombatState state = State(out UnitState hero, out UnitState enemy);
            CombatResolver.BeginTurn(state, hero.Id);
            CombatSelectionController selection = new CombatSelectionController();
            selection.SetTarget(state, enemy.Id);
            selection.BeginKeyboardTargeting(state);

            Assert.That(selection.TryCommitKeyboardTarget(out GridPosition committed), Is.True);
            Assert.That(committed, Is.EqualTo(enemy.Position));
            Assert.That(selection.TargetId, Is.EqualTo(enemy.Id), "commit retains the inspected target until command handling decides otherwise");

            selection.BeginKeyboardTargeting(state);
            Assert.That(selection.CancelKeyboardTargeting(), Is.True);
            Assert.That(selection.TargetId, Is.Null);
            Assert.That(selection.IsKeyboardTargeting, Is.False);
        }

        [Test]
        public void TargetValidationRejectsUnknownUnitsAndInactiveHeroCannotStartCursor()
        {
            CombatState state = State(out _, out _);
            CombatSelectionController selection = new CombatSelectionController();

            Assert.That(selection.SetTarget(state, "missing"), Is.False);
            Assert.That(selection.TargetId, Is.Null);
            Assert.That(selection.BeginKeyboardTargeting(state), Is.False);
        }

        [Test]
        public void SpellShortcut_RejectsMissingManaAndArmsAffordableSlotWithoutCasting()
        {
            CombatState state = State(out UnitState hero, out _);
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            hero.ConfigureMana(12, 0);
            state.AttachRogueSpellRuntime(new RogueSpellCombatRuntime(state,
                RogueSpellLoadout.CreateStarter().CreateCombatSnapshot()));
            CombatResolver.BeginTurn(state, hero.Id);
            GameObject root = new GameObject("shortcut-host");
            try
            {
                CombatPrototypeBootstrap bootstrap = root.AddComponent<CombatPrototypeBootstrap>();
                SetState(bootstrap, state);

                Assert.That(bootstrap.TrySelectSpellShortcut(0), Is.False);
                Assert.That(bootstrap.SelectedAction, Is.EqualTo("移动"));
                Assert.That(bootstrap.TrySelectSpellShortcut(3), Is.True);
                Assert.That(bootstrap.SelectedAction, Is.EqualTo("技能4"));
                Assert.That(hero.ActionPoints, Is.EqualTo(CombatResolver.HeroActionPointsPerTurn),
                    "arming a shortcut must not spend resources before a target is submitted");
                Assert.That(hero.Mana, Is.Zero,
                    "arming an affordable zero-mana shortcut must not change personal mana");
                Assert.That(bootstrap.TrySelectSpellShortcut(7), Is.False);
                Assert.That(bootstrap.SelectedAction, Is.EqualTo("技能4"),
                    "rejecting an empty slot must preserve the last valid mode");
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void ContextActions_ExposeOnlyLegalPositionActionsAndQuickMoveDeferralIsModeAware()
        {
            CombatState state = State(out UnitState hero, out UnitState enemy);
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            hero.ConfigureMana(12, 12);
            state.AttachRogueSpellRuntime(new RogueSpellCombatRuntime(state,
                RogueSpellLoadout.CreateStarter().CreateCombatSnapshot()));
            CombatResolver.BeginTurn(state, hero.Id);
            GameObject root = new GameObject("context-host");
            try
            {
                CombatPrototypeBootstrap bootstrap = root.AddComponent<CombatPrototypeBootstrap>();
                SetState(bootstrap, state);

                string[] enemyActions = bootstrap.ContextActionsAt(enemy.Position).Select(value => value.Id).ToArray();
                Assert.That(enemyActions, Does.Contain("attack"));
                Assert.That(enemyActions, Does.Not.Contain("spell:0"),
                    "a spell whose targeting contract rejects this enemy must stay out of the context menu");
                Assert.That(enemyActions, Does.Contain("spell:1"));
                Assert.That(enemyActions, Does.Not.Contain("move"));

                GridPosition ground = new GridPosition(0, 1);
                Assert.That(bootstrap.CanQuickMoveTo(ground), Is.True);
                Assert.That(bootstrap.ShouldDeferPrimaryClickForQuickMove(ground), Is.False);
                bootstrap.SelectHudAction("攻击");
                Assert.That(bootstrap.ShouldDeferPrimaryClickForQuickMove(ground), Is.True);
                Assert.That(bootstrap.ContextActionsAt(ground).Select(value => value.Id), Does.Contain("move"));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void QuickMoveConfirmation_RequiresTwoClicksOnTheSameLegalCell()
        {
            GridPosition first = new GridPosition(2, 3);
            GridPosition other = new GridPosition(3, 3);

            Assert.That(FormalBattlefieldView.IsConfirmedQuickMove(first, first, false, true), Is.False,
                "the first click must only arm the quick move");
            Assert.That(FormalBattlefieldView.IsConfirmedQuickMove(first, first, true, true), Is.True,
                "the second click on the same legal cell confirms the quick move");
            Assert.That(FormalBattlefieldView.IsConfirmedQuickMove(other, first, true, true), Is.False,
                "a fast click on another cell must not inherit the first cell's confirmation");
            Assert.That(FormalBattlefieldView.IsConfirmedQuickMove(first, first, true, false), Is.False,
                "a destination that became illegal must never submit a quick move");
        }

        [Test]
        public void QuickMoveSubmission_UsesAuthoritativeMoveCostAndRejectsOccupiedDestination()
        {
            CombatState state = State(out UnitState hero, out UnitState enemy);
            CombatResolver.BeginTurn(state, hero.Id);
            GameObject root = new GameObject("quick-move-submit-host");
            try
            {
                CombatPrototypeBootstrap bootstrap = root.AddComponent<CombatPrototypeBootstrap>();
                SetState(bootstrap, state);
                GridPosition destination = new GridPosition(0, 1);
                int actionPoints = hero.ActionPoints;

                bootstrap.SubmitBattlefieldQuickMove(destination);

                Assert.That(hero.Position, Is.EqualTo(destination));
                Assert.That(hero.ActionPoints, Is.EqualTo(actionPoints - CombatResolver.BasicActionPointCost));
                Assert.That(bootstrap.SelectedAction, Is.EqualTo("移动"));

                int remaining = hero.ActionPoints;
                bootstrap.SubmitBattlefieldQuickMove(enemy.Position);
                Assert.That(hero.Position, Is.EqualTo(destination));
                Assert.That(hero.ActionPoints, Is.EqualTo(remaining),
                    "a rejected quick move must not spend action points");
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void ContextAttackSubmission_ExecutesAttackAndClosesMenu()
        {
            CombatState state = State(out UnitState hero, out UnitState enemy);
            enemy.Armor = 0;
            enemy.Block = 0;
            CombatResolver.BeginTurn(state, hero.Id);
            GameObject root = new GameObject("context-attack-submit-host");
            try
            {
                CombatPrototypeBootstrap bootstrap = root.AddComponent<CombatPrototypeBootstrap>();
                SetState(bootstrap, state);
                int actionPoints = hero.ActionPoints;
                int effectiveVitality = enemy.Health + enemy.Shield;
                CombatResolver.AttackPreview preview = CombatResolver.PreviewAttack(state, hero.Id, enemy.Id, false);
                Assert.That(preview.FinalDamage, Is.GreaterThan(0));
                Assert.That(bootstrap.ContextActionsAt(enemy.Position).Select(value => value.Id), Does.Contain("attack"));
                bootstrap.SetBattlefieldContextMenuOpen(true);

                bootstrap.SubmitBattlefieldContextAction(enemy.Position, "attack");

                Assert.That(bootstrap.IsInteractionModalOpen, Is.False);
                Assert.That(bootstrap.SelectedAction, Is.EqualTo("攻击"));
                Assert.That(hero.ActionPoints, Is.EqualTo(actionPoints - CombatResolver.BasicActionPointCost));
                Assert.That(enemy.Health + enemy.Shield,
                    Is.EqualTo(effectiveVitality - preview.ShieldAbsorption - preview.FinalDamage),
                    "the context attack must apply the same shield-plus-health damage predicted by the resolver");
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void ContextSpellSubmission_UsesMatchingSlotAndSpendsOnlyItsAuthoritativeCosts()
        {
            CombatState state = State(out UnitState hero, out UnitState enemy);
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            hero.ConfigureMana(12, 12);
            var runtime = new RogueSpellCombatRuntime(state,
                RogueSpellLoadout.CreateStarter().CreateCombatSnapshot());
            state.AttachRogueSpellRuntime(runtime);
            CombatResolver.BeginTurn(state, hero.Id);
            var spell = runtime.DefinitionAtSlot(1);
            GameObject root = new GameObject("context-spell-submit-host");
            try
            {
                CombatPrototypeBootstrap bootstrap = root.AddComponent<CombatPrototypeBootstrap>();
                SetState(bootstrap, state);
                int actionPoints = hero.ActionPoints;
                int mana = hero.Mana;
                int health = enemy.Health;
                Assert.That(bootstrap.ContextActionsAt(enemy.Position).Select(value => value.Id), Does.Contain("spell:1"));

                bootstrap.SubmitBattlefieldContextAction(enemy.Position, "spell:1");

                Assert.That(bootstrap.SelectedAction, Is.EqualTo("技能2"));
                Assert.That(hero.ActionPoints, Is.EqualTo(actionPoints - spell.ActionPointCost));
                Assert.That(hero.Mana, Is.EqualTo(mana - spell.ManaCost));
                Assert.That(enemy.Health, Is.LessThan(health));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void ContextActions_HideAllSpellsWhenPersonalManaIsEmpty()
        {
            CombatState state = State(out UnitState hero, out UnitState enemy);
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            hero.ConfigureMana(12, 0);
            state.AttachRogueSpellRuntime(new RogueSpellCombatRuntime(state,
                RogueSpellLoadout.CreateStarter().CreateCombatSnapshot()));
            CombatResolver.BeginTurn(state, hero.Id);
            GameObject root = new GameObject("context-resource-gate-host");
            try
            {
                CombatPrototypeBootstrap bootstrap = root.AddComponent<CombatPrototypeBootstrap>();
                SetState(bootstrap, state);

                string[] actions = bootstrap.ContextActionsAt(enemy.Position).Select(value => value.Id).ToArray();

                Assert.That(actions, Does.Contain("attack"));
                Assert.That(actions.Any(value => value.StartsWith("spell:")), Is.False);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [TestCase("移动", "双击空地：快捷移动")]
        [TestCase("攻击", "左键敌人：攻击")]
        [TestCase("技能8", "左键合法目标：施放术式")]
        [TestCase("互动", "左键相邻目标：互动")]
        public void ModeInstruction_StatesWhatPrimaryClickWillDo(string action, string expected)
        {
            Assert.That(FormalCombatHud.PrimaryClickInstruction(action), Is.EqualTo(expected));
        }

        private static void SetState(CombatPrototypeBootstrap bootstrap, CombatState state)
        {
            typeof(CombatPrototypeBootstrap).GetField("state", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(bootstrap, state);
        }

        private static CombatState State(out UnitState hero, out UnitState enemy)
        {
            hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            enemy = new UnitState("enemy", false, new GridPosition(2, 0), Facing.West);
            return new CombatState(new GridMap(4, 2), new[] { hero, enemy });
        }
    }
}
