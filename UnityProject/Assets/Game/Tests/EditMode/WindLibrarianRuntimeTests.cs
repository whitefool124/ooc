using System.Linq;
using NUnit.Framework;

namespace OCC.Combat.Tests
{
    /// <summary>小铃（风系精英图书管理员）的场地夺取与公开条件反应。</summary>
    public sealed class WindLibrarianRuntimeTests
    {
        private static CombatState State(GridPosition librarianCell, GridPosition heroCell, out UnitState librarian)
        {
            UnitState hero = new UnitState("hero", true, heroCell) { DisplayName = "维克多·维恩", Speed = 11 };
            hero.Equip(CombatCatalog.Hammer, CombatCatalog.Shield, CombatCatalog.FireBolt, CombatCatalog.FrostBind);
            librarian = new UnitState("enemy_0", false, librarianCell) { DisplayName = "小铃" };
            EnemyArchetypes.Get("wind_librarian").Apply(librarian);
            CombatState state = new CombatState(new GridMap(9, 7), new[] { hero, librarian });
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            state.AttachAcademyFieldEnemy(new AcademyFieldEnemyRuntime());
            state.AttachRogueSpellRuntime(new OCC.Combat.Roguelite.RogueSpellCombatRuntime(state,
                OCC.Combat.Roguelite.RogueSpellLoadout.Restore(new[] { "BASE-FIRE-MELEE" }, new[] { "BASE-FIRE-MELEE", "", "", "", "", "", "", "" }, true)));
            return state;
        }

        private static void Set(CombatState state, int x, int y, System.Action<TileState> edit)
        {
            GridPosition position = new GridPosition(x, y);
            TileState tile = state.Map.GetTile(position).Clone();
            edit(tile);
            state.Map.SetTile(position, tile);
        }

        private static TileState Tile(CombatState state, int x, int y) => state.Map.GetTile(new GridPosition(x, y));

        [Test]
        public void Librarian_IsAnEliteWithOnlyItsDeclaredAbility()
        {
            EnemyArchetype archetype = EnemyArchetypes.Get("wind_librarian");

            Assert.That(archetype.DisplayName, Is.EqualTo("小铃"));
            Assert.That(archetype.IsElite, Is.True);
            Assert.That(archetype.MaxHealth, Is.EqualTo(16));
            Assert.That(archetype.Weapon.DisplayName, Is.EqualTo("引风短杖"));
            Assert.That(archetype.PrimarySkill.Id, Is.EqualTo("enemy_wind_scroll_edge"));
            Assert.That(archetype.HasSecondarySkill, Is.False, "其余手段由运行时结算，不显示默认第二技能。");
            UnitState unit = new UnitState("enemy_x", false, new GridPosition(0, 0));
            archetype.Apply(unit);
            Assert.That(unit.SkillTwo, Is.Null);
        }

        [Test]
        public void Librarian_ChangesWindTowardTheHeroAndOnlyThreeTimes()
        {
            CombatState state = State(new GridPosition(1, 5), new GridPosition(6, 5), out UnitState librarian);
            CombatResolver.BeginTurn(state, "enemy_0");

            Assert.That(state.Environment.Wind.Level, Is.EqualTo(1));
            Assert.That(state.Environment.Wind.Direction, Is.EqualTo(FieldWindState.East));
            Assert.That(state.AcademyFieldEnemy.WindChangesUsed, Is.EqualTo(1));

            CombatResolver.BeginTurn(state, "enemy_0");
            CombatResolver.BeginTurn(state, "enemy_0");
            Assert.That(state.AcademyFieldEnemy.WindChangesUsed, Is.EqualTo(3));
            Assert.That(state.Environment.Wind.Level, Is.EqualTo(3));

            CombatResolver.BeginTurn(state, "enemy_0");
            Assert.That(state.AcademyFieldEnemy.WindChangesUsed, Is.EqualTo(3), "换风次数用尽后不再改变风向。");
            Assert.That(state.EventLog.Any(line => line.Contains("小铃换风")), Is.True);
            Assert.That(librarian.Position, Is.EqualTo(new GridPosition(1, 5)));
        }

        [Test]
        public void AcademyLibrarian_ChangesWindAsItsOneAction()
        {
            CombatState state = State(new GridPosition(1, 5), new GridPosition(6, 5), out UnitState librarian);
            state.AttachAcademyEnemyArea(new AcademyEnemyAreaRuntime());
            CombatResolver.BeginTurn(state, librarian.Id);
            Assert.That(state.Environment.Wind.Level, Is.Zero, "学院层回合开始不自动换风。");
            CombatCommand command = state.AcademyFieldEnemy.ChooseEnemyCommand(state, librarian, state.GetUnit("hero"));
            Assert.That(command.SlotIndex, Is.EqualTo(AcademyFieldEnemyRuntime.WindChangeSkillIndex));
            CombatResolver.Resolve(state, command);
            Assert.That(state.Environment.Wind.Level, Is.EqualTo(1));
            Assert.That(state.Environment.Wind.Direction, Is.EqualTo(FieldWindState.East));
            Assert.That(librarian.ActionPoints, Is.Zero);
        }

        [Test]
        public void AcademyLibrarian_PrioritizesChangingWindWhenFireIsPresent()
        {
            CombatState state = State(new GridPosition(1, 5), new GridPosition(6, 5), out UnitState librarian);
            state.AttachAcademyEnemyArea(new AcademyEnemyAreaRuntime());
            state.RogueSpells.FireBattle.CreateOrRefreshFireground(new GridPosition(3, 5), 2, 2, "test-fire");
            state.Environment.Wind.TryChange(FieldWindState.West, 1);
            CombatResolver.BeginTurn(state, librarian.Id);
            var plans = new EnemyTurnPlanBook();
            CombatCommand command = plans.GetExecutionCommand(state, librarian, state.GetUnit("hero"));
            Assert.That(command.SlotIndex, Is.EqualTo(AcademyFieldEnemyRuntime.WindChangeSkillIndex));
            Assert.That(plans.GetPublicIntent(state, librarian, state.GetUnit("hero")).ActionName, Is.EqualTo("换风"));

            CombatResolver.Resolve(state, command);
            Assert.That(state.Environment.Wind.Direction, Is.EqualTo(FieldWindState.East));
            Assert.That(librarian.ActionPoints, Is.Zero);
        }

        [Test]
        public void AcademyLibrarian_RaisesPublicPaperScreenAsOneAction()
        {
            CombatState state = State(new GridPosition(1, 3), new GridPosition(6, 3), out UnitState librarian);
            state.AttachAcademyEnemyArea(new AcademyEnemyAreaRuntime());
            state.Environment.Wind.TryChange(FieldWindState.East, 1);
            Set(state, 0, 0, tile => tile.IsLoosePaper = true);
            Set(state, 0, 1, tile => tile.IsLoosePaper = true);
            Set(state, 0, 2, tile => tile.IsLoosePaper = true);
            CombatResolver.BeginTurn(state, librarian.Id);
            var plans = new EnemyTurnPlanBook();
            CombatCommand command = plans.GetExecutionCommand(state, librarian, state.GetUnit("hero"));
            Assert.That(command.SlotIndex, Is.EqualTo(AcademyFieldEnemyRuntime.WindScreenSkillIndex));
            Assert.That(plans.GetPublicIntent(state, librarian, state.GetUnit("hero")).ActionName, Is.EqualTo("扬页"));
            Assert.That(Tile(state, 2, 3).HasPaperScreen, Is.False);

            CombatResolver.Resolve(state, command);
            Assert.That(Tile(state, 2, 3).HasPaperScreen, Is.True);
            Assert.That(librarian.ActionPoints, Is.Zero);
        }

        [Test]
        public void AcademyLibrarian_UsesScrollOrPushAsPublicAction()
        {
            CombatState scroll = State(new GridPosition(1, 3), new GridPosition(3, 3), out UnitState librarian);
            scroll.AttachAcademyEnemyArea(new AcademyEnemyAreaRuntime());
            scroll.Environment.Wind.TryChange(FieldWindState.East, 1);
            Set(scroll, 0, 0, tile => tile.IsLoosePaper = true);
            CombatResolver.BeginTurn(scroll, librarian.Id);
            var plans = new EnemyTurnPlanBook();
            CombatCommand command = plans.GetExecutionCommand(scroll, librarian, scroll.GetUnit("hero"));
            Assert.That(command.SlotIndex, Is.EqualTo(AcademyFieldEnemyRuntime.WindScrollSkillIndex));
            Assert.That(plans.GetPublicIntent(scroll, librarian, scroll.GetUnit("hero")).ActionName, Is.EqualTo("卷页"));
            int health = scroll.GetUnit("hero").Health;
            CombatResolver.Resolve(scroll, command);
            Assert.That(scroll.GetUnit("hero").Health, Is.LessThan(health));

            CombatState push = State(new GridPosition(1, 3), new GridPosition(3, 3), out UnitState pusher);
            push.AttachAcademyEnemyArea(new AcademyEnemyAreaRuntime());
            push.Environment.Wind.TryChange(FieldWindState.East, 1);
            CombatResolver.BeginTurn(push, pusher.Id);
            plans = new EnemyTurnPlanBook();
            command = plans.GetExecutionCommand(push, pusher, push.GetUnit("hero"));
            Assert.That(command.SlotIndex, Is.EqualTo(AcademyFieldEnemyRuntime.WindPushSkillIndex));
            Assert.That(plans.GetPublicIntent(push, pusher, push.GetUnit("hero")).ActionName, Is.EqualTo("推风"));
            CombatResolver.Resolve(push, command);
            Assert.That(push.GetUnit("hero").Position, Is.EqualTo(new GridPosition(4, 3)));
        }

        [Test]
        public void Librarian_SpendsLoosePaperOnTheWindEdge()
        {
            CombatState state = State(new GridPosition(3, 3), new GridPosition(5, 3), out _);
            Set(state, 4, 3, tile => tile.IsLoosePaper = true);
            UnitState hero = state.GetUnit("hero");
            int healthBefore = hero.Health;

            CombatResolver.BeginTurn(state, "enemy_0");

            Assert.That(Tile(state, 4, 3).IsLoosePaper, Is.False, "风刃消耗一格散页。");
            Assert.That(hero.Health + hero.Shield, Is.LessThan(healthBefore + hero.Shield + 1));
            Assert.That(state.EventLog.Any(line => line.Contains("卷页")), Is.True, string.Join(" | ", state.EventLog));
        }

        [Test]
        public void Librarian_WithoutLoosePaperOnlyChangesWind()
        {
            // 主角距离 6 格，超出推风范围；场上没有散页与火场，只剩换风可用。
            CombatState state = State(new GridPosition(3, 3), new GridPosition(8, 6), out _);

            CombatResolver.BeginTurn(state, "enemy_0");

            Assert.That(state.AcademyFieldEnemy.WindChangesUsed, Is.EqualTo(1));
            Assert.That(state.EventLog.Any(line => line.Contains("卷页")), Is.False);
            Assert.That(state.EventLog.Any(line => line.Contains("扬页")), Is.False);
            Assert.That(state.EventLog.Any(line => line.Contains("换风")), Is.True);
        }

        [Test]
        public void Librarian_RaisesAPaperScreenFromThreeSheets()
        {
            CombatState state = State(new GridPosition(1, 3), new GridPosition(6, 3), out _);
            Set(state, 2, 5, tile => tile.IsLoosePaper = true);
            Set(state, 3, 5, tile => tile.IsLoosePaper = true);
            Set(state, 4, 5, tile => tile.IsLoosePaper = true);
            Set(state, 0, 0, tile => tile.IsLoosePaper = true);

            CombatResolver.BeginTurn(state, "enemy_0");

            Assert.That(Tile(state, 2, 3).HasPaperScreen, Is.True);
            Assert.That(Tile(state, 3, 3).HasPaperScreen, Is.True);
            Assert.That(Tile(state, 4, 3).HasPaperScreen, Is.True);
            Assert.That(Tile(state, 2, 5).IsLoosePaper || Tile(state, 3, 5).IsLoosePaper || Tile(state, 4, 5).IsLoosePaper,
                Is.False, "立页幕消耗三格散页。");
            Assert.That(state.HasLineOfSight(new GridPosition(1, 3), new GridPosition(6, 3)), Is.False, "页幕截断攻击线。");
        }

        [Test]
        public void Librarian_PaperScreenClearsAtHerNextTurn()
        {
            CombatState state = State(new GridPosition(1, 3), new GridPosition(6, 3), out _);
            Set(state, 2, 5, tile => tile.IsLoosePaper = true);
            Set(state, 3, 5, tile => tile.IsLoosePaper = true);
            Set(state, 4, 5, tile => tile.IsLoosePaper = true);
            CombatResolver.BeginTurn(state, "enemy_0");
            Assert.That(Tile(state, 2, 3).HasPaperScreen, Is.True);

            CombatResolver.BeginTurn(state, "enemy_0");

            Assert.That(Tile(state, 2, 3).HasPaperScreen, Is.False, "页幕只持续一回合。");
        }

        [Test]
        public void Librarian_PushesAnAdjacentTargetAway()
        {
            CombatState state = State(new GridPosition(3, 3), new GridPosition(4, 3), out _);

            CombatResolver.BeginTurn(state, "enemy_0");

            Assert.That(state.GetUnit("hero").Position, Is.EqualTo(new GridPosition(5, 3)), "主角被沿主轴推开 1 格。");
            Assert.That(state.EventLog.Any(line => line.Contains("推风")), Is.True);
        }

        [Test]
        public void Librarian_KindlesExistingFireTowardTheHero()
        {
            CombatState state = State(new GridPosition(1, 1), new GridPosition(6, 3), out _);
            state.RogueSpells.FireBattle.CreateOrRefreshFireground(new GridPosition(2, 3), 3, 2, "test-fire");

            CombatResolver.BeginTurn(state, "enemy_0");

            Assert.That(state.RogueSpells.FireBattle.HasFireground(new GridPosition(2, 3)), Is.False, "火场被吹离原格。");
            Assert.That(state.EventLog.Any(line => line.Contains("引火")), Is.True, string.Join(" | ", state.EventLog));
        }

        [Test]
        public void LibrarianIntent_PublishesItsWholeFieldKit()
        {
            CombatState state = State(new GridPosition(1, 5), new GridPosition(6, 5), out UnitState librarian);
            CombatResolver.BeginTurn(state, "enemy_0");
            CombatCommand command = state.AcademyFieldEnemy.ChooseEnemyCommand(state, librarian, state.GetUnit("hero"));

            EnemyIntentPresentation intent = state.AcademyFieldEnemy.PresentIntent(state, librarian, command);

            Assert.That(intent.DetailedText, Does.Contain("卷页"));
            Assert.That(intent.DetailedText, Does.Contain("扬页"));
            Assert.That(intent.DetailedText, Does.Contain("换风"));
            Assert.That(intent.DetailedText, Does.Contain("反应"));
        }

        [Test]
        public void LibrarianClone_KeepsWindChangeCount()
        {
            CombatState state = State(new GridPosition(1, 5), new GridPosition(6, 5), out _);
            CombatResolver.BeginTurn(state, "enemy_0");

            CombatState clone = state.Clone();

            Assert.That(clone.AcademyFieldEnemy.WindChangesUsed, Is.EqualTo(1));
            Assert.That(clone.Environment.Wind.Direction, Is.EqualTo(state.Environment.Wind.Direction));
            Assert.That(clone.Environment.Wind.Level, Is.EqualTo(state.Environment.Wind.Level));
        }
    }
}
