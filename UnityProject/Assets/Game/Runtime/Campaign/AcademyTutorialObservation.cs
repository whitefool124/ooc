using System;
using System.Collections.Generic;
using System.Linq;
using OCC.Combat.Roguelite;

namespace OCC.Combat
{
    // Observes committed domain values; never infers a successful operation from button text.
    public sealed class AcademyTutorialObservation
    {
        public static string Fingerprint(RogueliteMapRun run) => run == null ? "" : Rogue11Serializer.Serialize(run.ExportRogue11());
        private string runId;
        private CombatState battle;
        private string activeUnit;
        private int mana, shield, health, time;
        private string[] equippedSpells, quickbar;
        private Dictionary<string, string> equipment;
        private Dictionary<string, int> uses;
        private HashSet<string> mastered;
        private string gift;
        public void Tick(RogueliteMapRun run, CombatState state, bool inCombat, Action<string,string> evidence)
        {
            var dto = run?.RogueRunState;
            if (dto == null) return;
            bool same = runId == dto.RunId;
            if (same)
            {
                foreach (string id in dto.MasteredSpellIds.Where(id => !mastered.Contains(id)))
                { evidence("G02", "mastered"); evidence("I07", "learn"); }
                for (int i = 0; i < dto.EquippedSpellIds.Length; i++)
                {
                    string current = dto.EquippedSpellIds[i], old = equippedSpells[i];
                    if (current == old) continue;
                    if (!string.IsNullOrEmpty(current))
                    { evidence("G02", "equipped"); evidence("I07", "equip"); }
                    if (!string.IsNullOrEmpty(old) && string.IsNullOrEmpty(current)) evidence("I07", "remove");
                    if (!string.IsNullOrEmpty(old) && !string.IsNullOrEmpty(current)) evidence("I07", "replace");
                }
                foreach (var entry in dto.EquipmentSlotInstanceIds)
                {
                    string key = entry.Key.ToString();
                    if (equipment.TryGetValue(key, out string old) && old != entry.Value)
                    { if (!string.IsNullOrEmpty(old)) evidence("I01", "unequip"); if (!string.IsNullOrEmpty(entry.Value)) evidence("I01", "equip"); }
                }
                for (int i = 0; i < dto.ItemQuickbarInstanceIds.Length; i++)
                {
                    string current = dto.ItemQuickbarInstanceIds[i];
                    if (current == quickbar[i]) continue;
                    if (string.IsNullOrEmpty(current)) evidence("I04", "unlink");
                    else
                    {
                        evidence("I04", "link");
                        var item = dto.TacticalItemInstances.FirstOrDefault(t => t.InstanceId == current);
                        if (item != null && uses.TryGetValue(current, out int remaining) && remaining == item.ChargesCurrent)
                            evidence("I04", "charges-retained");
                    }
                }
                if (gift != dto.OpeningGift?.SelectedId && !string.IsNullOrEmpty(dto.OpeningGift?.SelectedId)) evidence("G01", "gift-chosen");
            }
            if (inCombat && state != null)
            {
                var hero = state.GetUnit("hero");
                if (hero != null)
                {
                    if (!ReferenceEquals(battle, state))
                    {
                        if (hero.Mana == hero.MaxMana) evidence("C07", "battle-full");
                        if (same && battle != null)
                        {
                            bool retained = dto.TacticalItemInstances.Any(t => uses.ContainsKey(t.InstanceId)) && dto.TacticalItemInstances.All(t => !uses.TryGetValue(t.InstanceId, out int count) || count == t.ChargesCurrent);
                            if (retained) { evidence("C08", "artifact-uses-retained"); evidence("U05", "artifact-retained"); }
                        }
                    }
                    else
                    {
                        if (hero.Mana > mana) { evidence("C07", "mana-restored"); if (hero.Mana <= hero.MaxMana) evidence("C07", "mana-cap"); }
                        // A value drop may be expiry, payment or refresh; it is not proof of damage.
                        if (activeUnit != state.ActiveUnitId && state.ActiveUnitId == "hero")
                        { evidence("C09", "own-turn"); evidence("C09", "public-turn"); }
                    }
                    battle = state; activeUnit = state.ActiveUnitId; mana = hero.Mana; health = hero.Health; shield = hero.Shield; time = state.CurrentTime;
                }
            }
            runId = dto.RunId;
            equippedSpells = dto.EquippedSpellIds.ToArray(); mastered = new HashSet<string>(dto.MasteredSpellIds);
            quickbar = dto.ItemQuickbarInstanceIds.ToArray(); equipment = dto.EquipmentSlotInstanceIds.ToDictionary(p => p.Key.ToString(), p => p.Value);
            uses = dto.TacticalItemInstances.ToDictionary(t => t.InstanceId, t => t.ChargesCurrent);
            gift = dto.OpeningGift?.SelectedId;
        }
    }
}
