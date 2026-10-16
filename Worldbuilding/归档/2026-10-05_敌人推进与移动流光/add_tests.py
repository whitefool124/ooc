from pathlib import Path
p=Path('UnityProject/Assets/Game/Tests/EditMode/EnemyPackTests.cs');s=p.read_text(encoding='utf-8-sig');idx=s.rfind('\n}');s=s[:idx]+'''
    public sealed class EnemyMovementBudgetTests
    {
        private static CombatState Lane(out UnitState enemy)
        {
            var hero = new UnitState("hero", true, new GridPosition(11, 0));
            enemy = new UnitState("enemy", false, new GridPosition(0, 0));
            EnemyArchetypes.Get("shieldguard").Apply(enemy);
            var state = new CombatState(new GridMap(12, 1), new[] { hero, enemy });
            state.ConfigureRuleset(CombatRuleset.Roguelite); CombatResolver.BeginTurn(state, enemy.Id);
            return state;
        }
        [Test] public void PursuitSpendsFiveStepsAndEndsWithoutAttacking()
        {
            var state = Lane(out var enemy); var hero = state.GetUnit("hero");
            var command = EnemyTactics.Choose(state, enemy, hero);
            Assert.That(command.Type, Is.EqualTo(CombatCommandType.Move));
            Assert.That(command.Destination, Is.EqualTo(new GridPosition(5, 0)));
            var intent = CombatInformationPresenter.BuildEnemyIntent(state, enemy, command).WithMovementPreview(state, enemy, command);
            Assert.That(intent.Route.Count, Is.EqualTo(6));
            int health = hero.Health; CombatResolver.Resolve(state, command);
            Assert.That(enemy.Position, Is.EqualTo(intent.Destination));
            Assert.That(enemy.ActionPoints, Is.Zero); Assert.That(hero.Health, Is.EqualTo(health));
        }
        [Test] public void WaterConsumesBudgetRatherThanCountingOnlyPathCells()
        {
            var state = Lane(out var enemy);
            state.Map.SetTile(new GridPosition(1, 0), new TileState { IsWater = true });
            state.Map.SetTile(new GridPosition(2, 0), new TileState { IsWater = true });
            var command = EnemyTactics.Choose(state, enemy, state.GetUnit("hero"));
            Assert.That(command.Destination, Is.EqualTo(new GridPosition(3, 0)));
            Assert.DoesNotThrow(() => CombatResolver.Resolve(state, command));
        }
        [Test] public void BindingMarkStopsThePublishedDestination()
        {
            var state = Lane(out var enemy);
            state.Map.SetTile(new GridPosition(2, 0), new TileState { IsBindingMark = true });
            var command = EnemyTactics.Choose(state, enemy, state.GetUnit("hero"));
            Assert.That(command.Destination, Is.EqualTo(new GridPosition(2, 0)));
            CombatResolver.Resolve(state, command); Assert.That(enemy.Position, Is.EqualTo(command.Destination));
        }
        [Test] public void SlowCapsPursuitAtThreeAndBindingPreventsMovement()
        {
            var state = Lane(out var enemy); enemy.ApplyStatus(StatusType.Slow, 2); CombatResolver.BeginTurn(state, enemy.Id);
            Assert.That(EnemyTactics.Choose(state, enemy, state.GetUnit("hero")).Destination, Is.EqualTo(new GridPosition(3, 0)));
            enemy.ApplyStatus(StatusType.Bound, 2);
            Assert.That(EnemyTactics.Choose(state, enemy, state.GetUnit("hero")).Type, Is.EqualTo(CombatCommandType.EndTurn));
        }
    }

    public sealed class EnemyMoveRouteGraphicTests
    {
        [Test] public void BentRouteUsesPathLengthAndClearsWhenDisabled()
        {
            var root = new GameObject("route mesh", typeof(RectTransform), typeof(CanvasRenderer));
            try
            {
                var graphic = root.AddComponent<CombatEnemyMoveRouteGraphic>();
                var path = new[] { Vector2.zero, new Vector2(32, 0), new Vector2(32, -32) };
                Assert.That(CombatEnemyMoveRouteGraphic.Length(path), Is.EqualTo(64));
                graphic.Refresh(new[] { path }, 32, 0);
                Assert.That(graphic.raycastTarget, Is.False);
                using (var vh = new UnityEngine.UI.VertexHelper())
                {
                    var populate = typeof(CombatEnemyMoveRouteGraphic).GetMethod("OnPopulateMesh", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly);
                    populate.Invoke(graphic, new object[] { vh }); Assert.That(vh.currentVertCount, Is.GreaterThan(0));
                    root.SetActive(false); root.SetActive(true);
                    populate.Invoke(graphic, new object[] { vh }); Assert.That(vh.currentVertCount, Is.Zero);
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
    }
''' +s[idx:];p.write_text(s,encoding='utf-8')
