using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    /// <summary>Per-battle counters clone with previews; command-journal replay recreates the same counters.</summary>
    public sealed class LampkeeperBattleRuntime
    {
        public string GiftId { get; }
        public int TotalManaGained { get; private set; }
        public int TotalShieldGained { get; private set; }
        private int fireRound = -1, wallRound = -1, movementTurn = -1, movementSteps;
        private bool movementGranted, openingShieldPending;
        private int survivingOpeningShield;
        public LampkeeperBattleRuntime(string id) { GiftId = id; LampkeeperGiftCatalog.Get(id); }
        private LampkeeperBattleRuntime(LampkeeperBattleRuntime source)
        {
            GiftId = source.GiftId; fireRound = source.fireRound; wallRound = source.wallRound;
            movementTurn = source.movementTurn; movementSteps = source.movementSteps;
            movementGranted = source.movementGranted; openingShieldPending = source.openingShieldPending;
            survivingOpeningShield = source.survivingOpeningShield;
            TotalManaGained = source.TotalManaGained; TotalShieldGained = source.TotalShieldGained;
        }
        public LampkeeperBattleRuntime Clone() => new LampkeeperBattleRuntime(this);
        public void Initialize(CombatState state)
        {
            if (GiftId != "LK-04") return;
            openingShieldPending = true;
            int before = state.GetUnit("hero").Shield;
            state.TryGrantRogueliteShield("hero", "lampkeeper:opening", 6);
            TotalShieldGained += state.GetUnit("hero").Shield - before;
        }
        public void BeforeTurn(UnitState unit)
        {
            // Initial shield is not awarded twice when the first turn clears ordinary shield.
            if (unit.IsHero && openingShieldPending) survivingOpeningShield = Math.Min(6, unit.Shield);
        }
        public void AfterTurnStart(CombatState state, UnitState unit)
        {
            if (!unit.IsHero || !openingShieldPending) return;
            openingShieldPending = false;
            if (survivingOpeningShield > 0)
                state.TryGrantRogueliteShield(unit.Id, "lampkeeper:opening-carry", survivingOpeningShield);
        }
        public void AfterActiveMove(CombatState state, string unitId, int steps)
        {
            if (GiftId != "LK-02" || unitId != "hero" || steps <= 0) return;
            int turn = state.OwnTurnCount(unitId);
            if (movementTurn != turn) { movementTurn = turn; movementSteps = 0; movementGranted = false; }
            movementSteps += steps;
            if (movementSteps < 3 || movementGranted) return;
            movementGranted = true;
            int before = state.GetUnit(unitId).Shield;
            state.TryGrantRogueliteShield(unitId, "lampkeeper:walking", 3);
            int applied = state.GetUnit(unitId).Shield - before; TotalShieldGained += applied;
            state.AddLog("行路铜钉：主动移动累计3格，获得" + applied + "普通护盾。");
        }
        public void AfterNewFireground(CombatState state, string sourceId)
        {
            if (GiftId != "LK-01" || sourceId != "hero" || fireRound == state.OwnTurnCount("hero")) return;
            fireRound = state.OwnTurnCount("hero"); RestoreMana(state, 1);
        }
        public void AfterDirectObjectDamage(CombatState state, string sourceId, int before, int after)
        {
            if (GiftId != "LK-03" || sourceId != "hero" || before <= 0 || after > 0 ||
                wallRound == state.OwnTurnCount("hero")) return;
            wallRound = state.OwnTurnCount("hero"); RestoreMana(state, 2);
        }
        private void RestoreMana(CombatState state, int amount)
        {
            var hero = state.GetUnit("hero");
            if (hero == null || !hero.IsAlive) return;
            int before = hero.Mana; hero.RestoreMana(amount);
            TotalManaGained += hero.Mana - before;
            state.AddLog(LampkeeperGiftCatalog.Get(GiftId).Name + "：恢复" + (hero.Mana - before) + "魔力。");
        }
    }
}
