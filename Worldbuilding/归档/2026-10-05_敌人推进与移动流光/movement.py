from pathlib import Path
p=Path('UnityProject/Assets/Game/Runtime/Combat/EnemyArchetypes.cs');s=p.read_text(encoding='utf-8-sig');s=s.replace('.OrderBy(path => path.Count)\n                .ThenBy(path => path[path.Count - 1].Y)', '.OrderBy(path => path.Skip(1).Sum(position => CombatMovementQuery.EntryCost(state, enemy, position)))\n                .ThenBy(path => path.Count)\n                .ThenBy(path => path[path.Count - 1].Y)',1);s=s.replace('return route == null ? CombatCommand.EndTurn(enemy.Id) : CombatCommand.Move(enemy.Id, route[1]);','return MoveWithinBudget(state, enemy, route);')
s=s.replace('state.Map.FindShortestPath(enemy.Position, position, 12, cell => state.IsOccupied(cell, enemy.Id))','state.Map.FindLowestCostPath(enemy.Position, position, state.Map.Width * state.Map.Height * 4,\n                    cell => CombatMovementQuery.EntryCost(state, enemy, cell), cell => state.IsOccupied(cell, enemy.Id))')
s=s.replace('.OrderBy(candidate => candidate.Count).ThenBy(candidate => candidate[candidate.Count - 1].Y)', '.OrderBy(candidate => candidate.Skip(1).Sum(cell => CombatMovementQuery.EntryCost(state, enemy, cell)))\n                .ThenBy(candidate => candidate.Count).ThenBy(candidate => candidate[candidate.Count - 1].Y)')
s=s.replace('            command = CombatCommand.Move(enemy.Id, path[1]); return true;', '            command = MoveWithinBudget(state, enemy, path); return true;')
a=s.index('        private static bool CanCast(');s=s[:a]+'''        private static CombatCommand MoveWithinBudget(CombatState state, UnitState enemy, IReadOnlyList<GridPosition> route)
        {
            if (route == null || enemy.HasStatus(StatusType.Bound)) return CombatCommand.EndTurn(enemy.Id);
            int spent = 0, budget = CombatMovementQuery.Budget(state, enemy);
            GridPosition destination = enemy.Position;
            for (int step = 1; step < route.Count; step++)
            {
                int cost = CombatMovementQuery.EntryCost(state, enemy, route[step]);
                if (spent + cost > budget) break;
                spent += cost; destination = route[step];
                if (state.Map.GetTile(destination).IsBindingMark) break;
            }
            return destination == enemy.Position ? CombatCommand.EndTurn(enemy.Id) : CombatCommand.Move(enemy.Id, destination);
        }

''' +s[a:];p.write_text(s,encoding='utf-8')
