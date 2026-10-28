using System;
using System.Linq;

namespace OCC.Combat
{
    public sealed partial class AcademyTutorialModelExercise
    {
        private static Model VineSearch()
        {
            var hero = new UnitState("hero", true, new GridPosition(1, 1)); hero.Equip(CombatCatalog.Rifle, CombatCatalog.Shield, CombatCatalog.FireBolt, CombatCatalog.FrostBind);
            var enemy = new UnitState("raider", false, new GridPosition(3, 2)); EnemyArchetypes.Get("raider").Apply(enemy);
            var state = new CombatState(new GridMap(7, 5), new[] { hero, enemy }); state.ConfigureRuleset(CombatRuleset.Roguelite); state.ConfigureLampVineSeed(4312);
            var entry = new GridPosition(3, 1); var deeper = new GridPosition(4, 1); state.Map.SetTile(entry, new TileState { IsLampVine = true, Durability = 6 }); state.Map.SetTile(deeper, new TileState { IsLampVine = true, Durability = 6 });
            bool hidden = false, stable = false, preserved = false, advanced = false, found = false; int hp = enemy.Health;
            return new Model
            {
                ReadCombat = () => state,
                Prompt = "先记录入藤的第一格。藤内移动不公开真实格；按原动作伤害搜查，未毁不推进。",
                Read = () => "敌方位置 " + (state.LampVines.IsHiddenFrom(state, hero, enemy) ? "隐藏，仅保留已知候选格 " + state.LampVines.PublicCell(enemy.Id) : "公开 " + enemy.Position) + "\n入口灯藤耐久 " + state.Map.GetTile(entry).Durability,
                Steps = new Action[] {
                    () => { CombatResolver.BeginTurn(state, enemy.Id); CombatResolver.Resolve(state, CombatCommand.Move(enemy.Id, entry)); hidden = state.LampVines.IsHiddenFrom(state, hero, enemy) && state.LampVines.PublicCell(enemy.Id) == entry; },
                    () => { CombatResolver.BeginTurn(state, enemy.Id); CombatResolver.Resolve(state, CombatCommand.Move(enemy.Id, deeper)); stable = state.LampVines.PublicCell(enemy.Id) == entry && CombatInformationPresenter.EnemyInspectionTargetAt(state, deeper) == null; },
                    () => { CombatResolver.BeginTurn(state, hero.Id); CombatResolver.Resolve(state, CombatCommand.AttackCell(hero.Id, entry)); preserved = state.Map.GetTile(entry).Durability == 2 && state.LampVines.PublicCell(enemy.Id) == entry && enemy.Health == hp; },
                    () => { CombatResolver.BeginTurn(state, hero.Id); CombatResolver.Resolve(state, CombatCommand.AttackCell(hero.Id, entry)); advanced = state.LampVines.PublicCell(enemy.Id) == deeper && state.Clone().LampVines.PublicCell(enemy.Id) == deeper; },
                    () => { CombatResolver.BeginTurn(state, hero.Id); CombatResolver.Resolve(state, CombatCommand.AttackCell(hero.Id, deeper)); found = !state.LampVines.IsHiddenFrom(state, hero, enemy) && enemy.Health == hp - 4 && state.Map.GetTile(deeper).Durability == 2; }
                }, Labels = new[] { "观察进入灯藤的已知格", "观察藤内移动后的信息", "以4点正常伤害搜查", "继续搜查，实际摧毁入口藤", "搜查下一候选格并观察显形" },
                Verify = () => hidden && stable && preserved && advanced && found,
                Fingerprint = () => state.Map.GetTile(entry).Durability + "|" + state.Map.GetTile(deeper).Durability + "|" + enemy.Health + "|" + state.LampVines.IsHiddenFrom(state, hero, enemy),
                Proofs = new[] { "hidden", "search", "revealed" }
            };
        }
        private static Model SearchContainer()
        {
            var state = StatusFixture(); var hero = state.GetUnit("hero");
            state.ConfigureItemInventory(new InventoryContainerState(), Array.Empty<string>());
            state.SetLootSource(new LootSourceState("teaching-loot", new GridPosition(1, 2), new[] { new ItemInstance("teaching-one", "medkit", 0), new ItemInstance("teaching-two", "shield_cell", 1) }, BattlefieldChestTier.Common));
            int ap = hero.ActionPoints; bool searched = false;
            return new Model
            {
                ReadCombat = () => state,
                Prompt = "在容器正交相邻格搜索，支付1行动点。随机容器一次揭示全部，逐件拿取不再收费。",
                Read = () => "行动点 " + hero.ActionPoints + "\n已揭示 " + state.LootSource.RevealedCount + "\n已拿取 " + state.LootSource.TakenInstanceIds.Count,
                Steps = new Action[] { () => { CombatResolver.Resolve(state, CombatCommand.SearchLoot(hero.Id)); searched = hero.ActionPoints == ap - 1 && state.LootSource.RevealedCount == 2; }, () => CombatResolver.Resolve(state, CombatCommand.TakeLoot(hero.Id, "teaching-one")), () => CombatResolver.Resolve(state, CombatCommand.TakeLoot(hero.Id, "teaching-two")) },
                Labels = new[] { "搜索这个相邻容器", "拿取已揭示的第一件", "拿取已揭示的第二件" },
                Verify = () => searched && hero.ActionPoints == ap - 1 && state.LootSource.TakenInstanceIds.Count == 2,
                Fingerprint = () => hero.ActionPoints + "|" + state.LootSource.RevealedCount + "|" + state.LootSource.TakenInstanceIds.Count,
                Proofs = new[] { "search-paid", "reveal", "take-no-charge" }
            };
        }
        private static Model ContainerVariant(string key)
        {
            var state = StatusFixture(); var tier = key == "common-container" ? BattlefieldChestTier.Common : key == "refined-container" ? BattlefieldChestTier.Refined : key == "cache-container" ? BattlefieldChestTier.Cache : BattlefieldChestTier.Fixed;
            var cell = new GridPosition(1, 2); var loot = new LootSourceState("teaching-container", cell, new[] { new ItemInstance("teaching-contained", "medkit", 0) }, tier); state.SetLootSource(loot);
            bool previewed = false;
            return new Model
            {
                ReadCombat = () => state,
                Prompt = tier == BattlefieldChestTier.Fixed ? "固定箱不属于可破坏的随机容器。" : "容器损毁会销毁尚未拿取的内容。先核对本箱耐久，再观察破坏结果。",
                Read = () => loot.DisplayName + "\n耐久 " + loot.Durability + "\n内容 " + (loot.IsDestroyed ? "已销毁，不可领取" : "仍在容器中"),
                Steps = new Action[] { () => previewed = tier == BattlefieldChestTier.Fixed ? !loot.IsDestroyed : loot.Durability == AcademyLootCatalog.Durability(tier), () => { if (tier != BattlefieldChestTier.Fixed) CombatEffectExecutor.Execute(state, "hero", CombatEffect.DamageObject(cell, loot.Durability)); } },
                Labels = new[] { "查看本类容器的耐久", tier == BattlefieldChestTier.Fixed ? "核对固定箱类别" : "结算足以摧毁的物块伤害" },
                Verify = () => previewed && (tier == BattlefieldChestTier.Fixed ? !loot.IsDestroyed : loot.IsDestroyed && loot.RevealNext() == null && loot.ClaimRevealed("teaching-contained") == null),
                Fingerprint = () => loot.Durability + "|" + loot.IsDestroyed, Proofs = new[] { key }
            };
        }
        private static Model EnvironmentVariant(string key)
        {
            var state = StatusFixture(); var fire = state.RogueSpells.FireBattle; var hero = state.GetUnit("hero"); var cell = new GridPosition(1, 2);
            string text = "尚未生成"; bool valid = false;
            Action setup = () =>
            {
                switch (key)
                {
                    case "loose-paper": fire.CreateOrRefreshLoosePaper(cell); text = "散页占效果层，移动代价2，可点燃"; break;
                    case "trace": fire.CreateOrRefreshTrace(cell); text = "痕迹供追踪机制读取，浅水覆盖会清除"; break;
                    case "binding-mark": fire.CreateOrRefreshBindingMark(cell); text = "约束纹在入格后截停，不是永久阻挡物"; break;
                    case "paper-screen": state.Map.SetTile(cell, new TileState { HasPaperScreen = true, EffectSourceId = hero.Id }); text = "页幕来源 演练者\n遮断经过它的攻击线，仍可通行"; break;
                    case "smoke": state.Map.SetTile(cell, new TileState { SmokeExpiresAt = state.CurrentTime + 10, SmokePublicRoundsRemaining = 1, EffectSourceId = hero.Id }); text = "烟尘来源 演练者\n有效期内遮断经过它的攻击线"; break;
                    case "wind": state.EnableWindMechanic(); fire.CreateOrRefreshLoosePaper(cell); state.Environment.Wind.TryChange(FieldWindState.East, 1); text = state.Environment.Wind.PreviewText(); break;
                    case "light-lane": state.Environment.AddLightLane(new FieldLightLaneState(hero.Id, cell, FieldWindState.East, 4)); text = "光带沿公开方向延伸，阻挡物后的部分不受照明"; break;
                }
            };
            Action probe = () =>
            {
                switch (key)
                {
                    case "loose-paper": valid = CombatMovementQuery.EntryCost(state, hero, cell) == 2 && fire.IgniteLoosePaper(cell, 8, 2, "teaching", hero.Id) && !state.Map.GetTile(cell).IsLoosePaper && fire.HasFireground(cell); text = "散页被点燃，原散页层移除，现为燃烧地格"; break;
                    case "trace": fire.CreateOrRefreshShallowWater(cell); valid = !state.Map.GetTile(cell).HasTrace && state.Map.GetTile(cell).IsWater; text = "浅水覆盖痕迹，旧痕迹已清除"; break;
                    case "binding-mark": CombatResolver.Resolve(state, CombatCommand.Move(hero.Id, cell)); valid = hero.Position == cell && hero.HasStatus(StatusType.Bound); text = "入格后停在约束纹上，本回合受到束缚"; break;
                    case "paper-screen": valid = !state.Map.HasLineOfSight(new GridPosition(0, 2), new GridPosition(3, 2), state.CurrentTime) && !state.Map.IsBlocked(cell); CombatResolver.Resolve(state, CombatCommand.Move(hero.Id, cell)); valid &= hero.Position == cell; text = "攻击线被截断；实际移动仍能进入页幕格"; break;
                    case "smoke": valid = !state.Map.HasLineOfSight(new GridPosition(0, 2), new GridPosition(3, 2), state.CurrentTime); state.SetCurrentTime(state.CurrentTime + 11); valid &= state.Map.HasLineOfSight(new GridPosition(0, 2), new GridPosition(3, 2), state.CurrentTime); text = "超过有效期后，原攻击线重新可通"; break;
                    case "wind": CombatResolver.BeginTurn(state, hero.Id); valid = !state.Map.GetTile(cell).IsLoosePaper && state.Map.PositionsWith(t => t.IsLoosePaper).Any(); text = "到环境结算窗口，散页按风向搬动"; break;
                    case "light-lane": var lane = state.Environment.LightLanes.Single(); int before = state.Environment.LitCells(state.Map, lane, state.CurrentTime).Count; state.Map.SetTile(new GridPosition(3, 2), new TileState { IsPermanentWall = true }); int after = state.Environment.LitCells(state.Map, lane, state.CurrentTime).Count; valid = before > after && after > 0; text = "阻挡前照明格数 " + before + "\n阻挡后照明格数 " + after; break;
                }
            };
            return new Model
            {
                ReadCombat = () => state,
                Prompt = "本次只核对一种场地规则。每次查看当前来源、作用与实际变化。", Read = () => text,
                Steps = new[] { setup, probe }, Labels = new[] { "查看这一场地规则", "执行并核对实际变化" },
                Verify = () => valid, Fingerprint = () => text + "|" + valid, Proofs = new[] { key }
            };
        }
        private static Model Passage()
        {
            var state = StatusFixture(); var hero = state.GetUnit("hero");
            var wall = new GridPosition(1, 2); var destination = new GridPosition(0, 2);
            state.Map.SetTile(wall, new TileState { IsPermanentWall = true, Durability = 24 });
            string result = "永久物块与单位占格均不能进入"; bool rejected = false, orthogonal = false;
            return new Model
            {
                ReadCombat = () => state,
                Prompt = "相邻指上下左右。永久物块不能破坏，路径也不能穿过单位。",
                Read = () => result + "\n当前位置 " + hero.Position,
                Steps = new Action[] {
                    () => { int ap = hero.ActionPoints; var position = hero.Position; try { CombatResolver.Resolve(state, CombatCommand.Move(hero.Id, wall)); } catch (InvalidOperationException) { }
                        CombatEffectExecutor.Execute(state, hero.Id, CombatEffect.DamageObject(wall, 99));
                        rejected = hero.ActionPoints == ap && hero.Position == position && state.Map.IsBlocked(wall) && state.Map.GetTile(wall).Durability == 24 && state.IsOccupied(new GridPosition(2, 1), hero.Id); result = "目标被阻挡，位置与行动点未变；永久物块不受破坏伤害"; },
                    () => { var path = CombatMovementQuery.FindPath(state, hero, destination); orthogonal = path.Count > 1 && path.Zip(path.Skip(1), (a,b) => a.ManhattanDistance(b) == 1).All(x => x); CombatResolver.Resolve(state, CombatCommand.Move(hero.Id, destination)); result = "按正交路径进入合法格"; }
                }, Labels = new[] { "尝试进入永久物块格", "沿可通行路径移动" },
                Verify = () => rejected && orthogonal && hero.Position == destination,
                Fingerprint = () => result + hero.Position + hero.ActionPoints, Proofs = new[] { "blocked-preview", "legal-path" }
            };
        }
        private static Model CoverLines()
        {
            var map = new GridMap(7, 3); var cell = new GridPosition(3, 1);
            var from = new GridPosition(1, 1); var to = new GridPosition(5, 1);
            string result = "同一条攻击线，每次只更换一种掩体"; bool light = false, heavy = false;
            return new Model
            {
                Prompt = "轻掩体减伤但不截断攻击线；重掩体会阻挡穿过它的攻击线。",
                Read = () => result,
                Steps = new Action[] {
                    () => { map.SetTile(cell, new TileState { Cover = CoverType.Light, Durability = 8 }); light = map.HasLineOfSight(from, to) && map.GetTile(cell).DamageReduction == 1; result = "轻掩体耐久8\n攻击线可通　掩体减伤1"; },
                    () => { map.SetTile(cell, new TileState { Cover = CoverType.Heavy, Durability = 24 }); heavy = !map.HasLineOfSight(from, to) && map.IsBlocked(cell) && map.GetTile(cell).DamageReduction == 2; result = "重掩体耐久24\n穿过它的攻击线被阻挡　格子不可通行"; }
                }, Labels = new[] { "查看轻掩体的实际查询", "更换重掩体并重新查询" },
                Verify = () => light && heavy, Fingerprint = () => result,
                Proofs = new[] { "visibility-view", "attack-line-view", "cover-light", "cover-heavy" }
            };
        }
        private static Model WaterCost()
        {
            var state = StatusFixture(); var hero = state.GetUnit("hero"); var water = new GridPosition(1, 2);
            state.RogueSpells.FireBattle.CreateOrRefreshShallowWater(water); int cost = 0, ap = hero.ActionPoints;
            return new Model
            {
                ReadCombat = () => state,
                Prompt = "浅水这一格消耗2步。步数决定可走多远，一次移动仍支付一次行动点。",
                Read = () => "浅水步数代价 " + CombatMovementQuery.EntryCost(state, hero, water) + "\n当前位置 " + hero.Position + "\n行动点 " + hero.ActionPoints,
                Steps = new Action[] { () => cost = CombatMovementQuery.EntryCost(state, hero, water), () => CombatResolver.Resolve(state, CombatCommand.Move(hero.Id, water)) },
                Labels = new[] { "查看路径代价", "移动进入浅水" },
                Verify = () => cost == 2 && hero.Position == water && hero.ActionPoints == ap - 1,
                Fingerprint = () => cost + "|" + hero.Position + "|" + hero.ActionPoints, Proofs = new[] { "water-cost", "water-step" }
            };
        }
        private static Model DestructiblePassage()
        {
            var state = StatusFixture(); var hero = state.GetUnit("hero"); var cell = new GridPosition(1, 2);
            state.Map.SetTile(cell, new TileState { Cover = CoverType.Heavy, Durability = 24 }); bool blocked = false;
            return new Model
            {
                ReadCombat = () => state,
                Prompt = "可破坏物显示耐久。耐久归零后，通行和攻击线会重新计算。",
                Read = () => "物块耐久 " + state.Map.GetTile(cell).Durability + "\n通行 " + (state.Map.IsBlocked(cell) ? "阻挡" : "可通行"),
                Steps = new Action[] { () => blocked = state.Map.IsBlocked(cell), () => CombatEffectExecutor.Execute(state, hero.Id, CombatEffect.DamageObject(cell, 24)) },
                Labels = new[] { "查看耐久与阻挡", "结算24点物块伤害" },
                Verify = () => blocked && state.Map.GetTile(cell).Durability == 0 && !state.Map.IsBlocked(cell) && CombatMovementQuery.FindPath(state, hero, cell).Count > 1,
                Fingerprint = () => state.Map.GetTile(cell).Durability + "|" + state.Map.IsBlocked(cell), Proofs = new[] { "durability-view", "object-destroyed", "path-updated" }
            };
        }
        private static Model CrystalBurst() => CrystalBurst(false);
        private static Model CrystalBurst(bool heavy)
        {
            var state = StatusFixture(); var cell = new GridPosition(3, 1); var hero = state.GetUnit("hero"); var target = state.GetUnit("target"); int hp = target.Health; bool preview = false;
            int durability = heavy ? 24 : 16;
            state.Map.SetTile(cell, new TileState { IsAetherCrystal = true, IsPressureCrystal = heavy, Durability = durability });
            return new Model
            {
                ReadCombat = () => state,
                Prompt = "晶簇破裂会波及邻格，留下碎晶。演练只核对范围与结果，不指定攻击对象。",
                Read = () => (heavy ? "精英稳压晶簇" : "蓄能晶簇") + "\n耐久 " + state.Map.GetTile(cell).Durability + "\n相邻演练靶生命 " + target.Health + "\n碎晶 " + (state.Map.GetTile(cell).IsCrystalShard ? "已留下" : "无"),
                Steps = new Action[] { () => preview = target.Position.ManhattanDistance(cell) == 1, () => CombatEffectExecutor.Execute(state, hero.Id, CombatEffect.DamageObject(cell, durability)) },
                Labels = new[] { "查看晶簇与相邻格", "结算晶簇破裂" },
                Verify = () => preview && state.Map.GetTile(cell).IsCrystalShard && target.Health < hp,
                Fingerprint = () => state.Map.GetTile(cell).Durability + "|" + target.Health + "|" + state.Map.GetTile(cell).IsCrystalShard,
                Proofs = new[] { "crystal-preview", "crystal-burst", "shards-view", heavy ? "pressure-crystal" : "aether-crystal" }
            };
        }
        private static Model FieldOverwrite()
        {
            var state = StatusFixture(); var fire = state.RogueSpells.FireBattle; var cell = new GridPosition(1, 2); var hero = state.GetUnit("hero"); bool overwritten = false, rejected = false;
            fire.CreateOrRefreshLoosePaper(cell);
            return new Model
            {
                ReadCombat = () => state,
                Prompt = "同格只保留一个效果层，后生成的覆盖原层。非法位置不产生效果，也不支付费用。",
                Read = () => "当前效果层 " + (fire.Firegrounds.ContainsKey(cell) ? "燃烧地格" : state.Map.GetTile(cell).EffectLayerName() ?? "无") + "\n魔力 " + hero.Mana + "　行动点 " + hero.ActionPoints,
                Steps = new Action[] {
                    () => { fire.CreateOrRefreshShallowWater(cell); overwritten = state.Map.GetTile(cell).IsWater && !state.Map.GetTile(cell).IsLoosePaper; },
                    () => { int ap = hero.ActionPoints, mana = hero.Mana; var spell = FireSpellCatalog.Get("F-P-M03"); var invalid = FireSpellTarget.At(new GridPosition(-1, -1), CardinalDirection.North); bool blocked = !FireSpellEngine.Preview(fire, hero.Id, spell, invalid).CanCommit; try { FireSpellEngine.Execute(fire, hero.Id, spell, invalid); } catch (InvalidOperationException) { } rejected = blocked && hero.ActionPoints == ap && hero.Mana == mana && fire.Firegrounds.Count == 0; }
                }, Labels = new[] { "用浅水覆盖散页", "尝试向越界目标生成火场" },
                Verify = () => overwritten && rejected, Fingerprint = () => state.Map.GetTile(cell).EffectLayerName() + hero.ActionPoints + hero.Mana,
                Proofs = new[] { "field-preview", "field-overwrite", "illegal-no-cost" }
            };
        }
    }
}
