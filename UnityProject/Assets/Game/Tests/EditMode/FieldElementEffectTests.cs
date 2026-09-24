using System;
using System.Linq;
using NUnit.Framework;
using OCC.Combat.Presentation;

namespace OCC.Combat.Tests
{
    /// <summary>通用场地库的逐元素效果：风、痕迹、约束纹、护罩发生器、过载装置。</summary>
    public sealed class FieldElementEffectTests
    {
        private static CombatState State(GridPosition? enemyCell = null)
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(1, 3)) { DisplayName = "维克多·维恩", Speed = 11 };
            hero.Equip(CombatCatalog.Hammer, CombatCatalog.Shield, CombatCatalog.FireBolt, CombatCatalog.FrostBind);
            UnitState enemy = new UnitState("enemy_0", false, enemyCell ?? new GridPosition(7, 5)) { DisplayName = "盾术生" };
            EnemyArchetypes.Get("shieldguard").Apply(enemy);
            CombatState state = new CombatState(new GridMap(9, 7), new[] { hero, enemy });
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            return state;
        }

        private static TileState Tile(CombatState state, int x, int y) => state.Map.GetTile(new GridPosition(x, y));

        /// <summary>空格的 GetTile 返回一次性空物块；写入必须克隆后 SetTile 才会落在棋盘上。</summary>
        private static void Set(CombatState state, int x, int y, Action<TileState> edit)
        {
            GridPosition position = new GridPosition(x, y);
            TileState tile = state.Map.GetTile(position).Clone();
            edit(tile);
            state.Map.SetTile(position, tile);
        }

        private static void MakeDevice(CombatState state, int x, int y, int durability) =>
            Set(state, x, y, tile => { tile.IsDevice = true; tile.Durability = durability; });

        [Test]
        public void Wind_MovesLoosePaperShardAndSmokeOneCellDownwind()
        {
            CombatState state = State();
            Set(state, 4, 3, tile => tile.IsLoosePaper = true);
            Set(state, 2, 5, tile => tile.IsCrystalShard = true);
            Set(state, 6, 1, tile => tile.SmokeExpiresAt = 99);
            Assert.That(state.Environment.Wind.TryChange(FieldWindState.East, 2), Is.True);

            CombatResolver.BeginTurn(state, "hero");

            Assert.That(Tile(state, 4, 3).IsLoosePaper, Is.False, "散页应离开原格");
            Assert.That(Tile(state, 5, 3).IsLoosePaper, Is.True, "散页应顺风推进一格");
            Assert.That(Tile(state, 2, 5).IsCrystalShard, Is.False, "碎晶应离开原格");
            Assert.That(Tile(state, 3, 5).IsCrystalShard, Is.True, "碎晶应顺风推进一格");
            Assert.That(Tile(state, 6, 1).SmokeExpiresAt, Is.EqualTo(0), "烟尘应离开原格");
            Assert.That(Tile(state, 7, 1).SmokeExpiresAt, Is.EqualTo(99), "烟尘应顺风推进一格");
        }

        [Test]
        public void CalmWind_LeavesLooseMaterialsInPlace()
        {
            CombatState state = State();
            Set(state, 4, 3, tile => tile.IsLoosePaper = true);

            CombatResolver.BeginTurn(state, "hero");

            Assert.That(Tile(state, 4, 3).IsLoosePaper, Is.True);
            Assert.That(Tile(state, 5, 3).IsLoosePaper, Is.False);
        }

        [Test]
        public void StrongWind_RemovesTrace()
        {
            CombatState state = State();
            Set(state, 3, 3, tile => tile.HasTrace = true);
            Set(state, 5, 5, tile => tile.HasTrace = true);
            Assert.That(state.Environment.Wind.TryChange(FieldWindState.West, 3), Is.True);

            CombatResolver.BeginTurn(state, "hero");

            Assert.That(Tile(state, 3, 3).HasTrace, Is.False);
            Assert.That(Tile(state, 5, 5).HasTrace, Is.False);
        }

        [Test]
        public void MildWind_KeepsTrace()
        {
            CombatState state = State();
            Set(state, 3, 3, tile => tile.HasTrace = true);
            Assert.That(state.Environment.Wind.TryChange(FieldWindState.West, 2), Is.True);

            CombatResolver.BeginTurn(state, "hero");

            Assert.That(Tile(state, 3, 3).HasTrace, Is.True);
        }

        [Test]
        public void BindingMark_StopsTheEnteringUnit()
        {
            CombatState state = State();
            Set(state, 2, 3, tile => tile.IsBindingMark = true);
            CombatResolver.BeginTurn(state, "hero");

            CombatResolver.Resolve(state, CombatCommand.Move("hero", new GridPosition(2, 3)));

            UnitState hero = state.GetUnit("hero");
            Assert.That(hero.Position, Is.EqualTo(new GridPosition(2, 3)), "进入者留在约束纹格");
            Assert.That(hero.HasStatus(StatusType.Bound), Is.True, "进入者本回合不能主动移动");
            Assert.That(state.EventLog.Any(line => line.Contains("约束纹")), Is.True);
        }

        [Test]
        public void WardGenerator_GrantsExtraShieldToAdjacentUnit()
        {
            CombatState state = State();
            MakeDevice(state, 1, 4, TileState.StandardDurability);
            Set(state, 1, 4, tile => tile.IsWardGenerator = true);

            CombatResolver.EndTurn(state, state.GetUnit("hero"));

            // 护盾在下一个自身回合开始时清空，因此以护盾来源记录为验收依据。
            Assert.That(state.RogueShieldEvents.Any(record => record.SourceId == "ward-generator" && record.Amount == 4), Is.True);
        }

        [Test]
        public void DestroyedWardGenerator_StopsGrantingShield()
        {
            CombatState state = State();
            MakeDevice(state, 1, 4, 0);
            Set(state, 1, 4, tile => tile.IsWardGenerator = true);

            CombatResolver.EndTurn(state, state.GetUnit("hero"));

            Assert.That(state.RogueShieldEvents.Any(record => record.SourceId == "ward-generator"), Is.False);
        }

        [Test]
        public void OverloadDevice_DetonatesAndHitsBothSides()
        {
            CombatState state = State(new GridPosition(2, 4));
            UnitState hero = state.GetUnit("hero");
            UnitState enemy = state.GetUnit("enemy_0");
            GridPosition device = new GridPosition(2, 3);
            MakeDevice(state, device.X, device.Y, TileState.StandardDurability);
            Set(state, device.X, device.Y, tile => tile.IsOverloadDevice = true);
            int heroHealth = hero.Health, enemyHealth = enemy.Health;

            Set(state, device.X, device.Y, tile => tile.Durability = 0);
            Assert.That(state.ResolveAetherCrystalDamage(device, TileState.StandardDurability), Is.True);

            Assert.That(Tile(state, device.X, device.Y).IsOverloadDevice, Is.False);
            Assert.That(hero.Health, Is.LessThan(heroHealth), "过载伤害对己方一致结算");
            Assert.That(enemy.Health, Is.LessThan(enemyHealth), "过载伤害对敌方一致结算");
            Assert.That(state.EventLog.Any(line => line.Contains("过载装置引爆")), Is.True);
        }

        [Test]
        public void Hover_DescribesNewEffectLayers()
        {
            CombatState state = State();
            Set(state, 2, 2, tile => tile.IsLoosePaper = true);
            Set(state, 3, 2, tile => tile.HasTrace = true);
            Set(state, 4, 2, tile => tile.IsBindingMark = true);

            string paper = CombatBattlefieldCellPresenter.BuildTerrainEffectHover(state, null, Tile(state, 2, 2), new GridPosition(2, 2));
            string trace = CombatBattlefieldCellPresenter.BuildTerrainEffectHover(state, null, Tile(state, 3, 2), new GridPosition(3, 2));
            string mark = CombatBattlefieldCellPresenter.BuildTerrainEffectHover(state, null, Tile(state, 4, 2), new GridPosition(4, 2));

            Assert.That(paper, Does.Contain("散页"), paper);
            Assert.That(paper, Does.Contain("2 移动距离"), paper);
            Assert.That(trace, Does.Contain("痕迹"), trace);
            Assert.That(mark, Does.Contain("约束纹"), mark);
            Assert.That(mark, Does.Contain("留在原格"), mark);
        }

        [Test]
        public void Hover_DescribesNewDevices()
        {
            CombatState state = State();
            MakeDevice(state, 2, 2, TileState.StandardDurability);
            Set(state, 2, 2, tile => tile.IsOverloadDevice = true);
            MakeDevice(state, 4, 2, TileState.StandardDurability);
            Set(state, 4, 2, tile => tile.IsWardGenerator = true);
            MakeDevice(state, 5, 2, TileState.HeavyDurability);
            Set(state, 5, 2, tile => tile.IsTowerMechanism = true);
            MakeDevice(state, 6, 2, TileState.TemporaryHeavyCoverDurability);
            Set(state, 6, 2, tile => tile.Cover = CoverType.Heavy);

            Assert.That(Hover(state, 2, 2), Does.Contain("8 点以太伤害"));
            Assert.That(Hover(state, 4, 2), Does.Contain("结构护盾"));
            Assert.That(Hover(state, 5, 2), Does.Contain("维护链"));
            Assert.That(Hover(state, 6, 2), Does.Contain("重掩体"));
        }

        private static string Hover(CombatState state, int x, int y) =>
            CombatBattlefieldCellPresenter.BuildObjectHover(state, Tile(state, x, y), new GridPosition(x, y));

        [Test]
        public void IntactDevice_DoesNotDetonate()
        {
            CombatState state = State();
            GridPosition device = new GridPosition(5, 5);
            MakeDevice(state, device.X, device.Y, TileState.StandardDurability);
            Set(state, device.X, device.Y, tile => tile.IsOverloadDevice = true);
            int heroHealth = state.GetUnit("hero").Health;

            Assert.That(state.ResolveAetherCrystalDamage(device, TileState.StandardDurability), Is.False);
            Assert.That(Tile(state, device.X, device.Y).IsOverloadDevice, Is.True);
            Assert.That(state.GetUnit("hero").Health, Is.EqualTo(heroHealth));
        }
    }
}
