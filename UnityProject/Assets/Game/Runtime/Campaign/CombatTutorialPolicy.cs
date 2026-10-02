using System.Collections.Generic;

namespace OCC.Combat
{
    public static class CombatTutorialPolicy
    {
        public static bool IsOperation(string id) => id == "B1-04" || id == "B1-05";
        public static string Next(ISet<string> completed, bool heroTurn, string action, int validCells,
            bool enemyTurn, bool moveDeferred)
        {
            if (!completed.Contains("B1-01")) return "B1-01";
            if (!completed.Contains("B1-02")) return "B1-02";
            if (heroTurn && !completed.Contains("B1-03")) return "B1-03";
            if (heroTurn && action == "移动" && validCells > 0 && !moveDeferred && !completed.Contains("B1-04")) return "B1-04";
            if (heroTurn && (action == "攻击" || action.StartsWith("技能")) && validCells > 0 && !completed.Contains("B1-05")) return "B1-05";
            if (enemyTurn && !completed.Contains("B1-06")) return "B1-06";
            return string.Empty;
        }
        public static bool Allows(string id, bool ready, CombatCommandType type)
        {
            if (string.IsNullOrEmpty(id)) return true;
            if (!ready) return false;
            if (id == "B1-04") return type == CombatCommandType.Move;
            if (id == "B1-05") return type == CombatCommandType.Attack || type == CombatCommandType.Cast || type == CombatCommandType.UseSkill;
            return false;
        }
        public static bool Completes(string id, bool ready, CombatCommandType acceptedType)
            => IsOperation(id) && Allows(id, ready, acceptedType);
    }
}
