from pathlib import Path
root=Path('UnityProject/Assets/Game')
p=root/'Runtime/Presentation/CombatTestArenaScenarioCatalog.cs'
s=p.read_text(encoding='utf-8-sig'); a=s.index('            // Place indestructible wall tiles'); b=s.index('\n        private static int StableSeed',a)
s=s[:a]+'''            // The collision mask describes the full shape, including concave corners.
            // Every blocked cell must carry a visible wall; bounding-box edges miss
            // interior cut-outs and leave ordinary floor that cannot be entered.
            var walls = new List<LevelTerrainPlacement>();
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    if (!walkable.Contains(new GridPosition(x, y)))
                        walls.Add(new LevelTerrainPlacement(x, y, LevelTerrainKind.PermanentWall));
            return walls.ToArray();
        }
''' +s[b:];p.write_text(s,encoding='utf-8')
p=root/'Runtime/Presentation/CombatBattlefieldRangeGraphic.cs';s=p.read_text(encoding='utf-8-sig')
s=s.replace('        private int lastAnimationFrame = -1;', '''        private int lastAnimationFrame = -1;
        private const float EnterDuration = .16f, ExitDuration = .12f;
        private MarkerState departing;
        private float departingOpacity;
        private float meshOpacity = 1f;

        private struct MarkerState
        {
            public BattlefieldCellMarker Move, Attack, Skill;
            public bool EnemyRange, EnemyEffect, PlayerEffect, EnemyRoute, EnemyDestination, Focused;
            public byte EnemyEdges, PlayerEdges;
        }

        private MarkerState CurrentMarkers => new MarkerState
        {
            Move = move, Attack = attack, Skill = skill, EnemyRange = enemyRange,
            EnemyEffect = enemyEffect, PlayerEffect = playerEffect, EnemyRoute = enemyRoute,
            EnemyDestination = enemyDestination, EnemyEdges = enemyEdges, PlayerEdges = playerEdges,
            Focused = focused
        };''')
s=s.replace('            appearedAt = Time.unscaledTime;', '''            // Keep the old geometry until its exit completes. Rapid changes retain
            // only one outgoing snapshot, so cancelled previews cannot accumulate.
            departing = CurrentMarkers;
            departingOpacity = animations ? Mathf.Clamp01((Time.unscaledTime - appearedAt) / EnterDuration) : 0f;
            appearedAt = Time.unscaledTime;''')
s=s.replace('            float entrance = animations ? Mathf.Clamp01(age / .16f) : 1f;','')
s=s.replace('canvasRenderer.SetAlpha(entrance * pulse);','canvasRenderer.SetAlpha(pulse);')
s=s.replace('            if (focused || intentAge', '            if (animations && age <= EnterDuration || focused || intentAge')
s=s.replace('            base.OnDisable(); resultAt = -10f; canvasRenderer.SetAlpha(1f);', '''            base.OnDisable(); resultAt = -10f; canvasRenderer.SetAlpha(1f);
            departing = default; departingOpacity = 0f;
            move = attack = skill = BattlefieldCellMarker.None;
            enemyRange = enemyEffect = playerEffect = enemyRoute = enemyDestination = focused = false;
            enemyEdges = playerEdges = 0; lastAnimationFrame = -1;''')
s=s.replace('            vh.Clear();\n            if (enemyEffect)', '''            vh.Clear();
            float age = Time.unscaledTime - appearedAt;
            if (animations && age < ExitDuration)
                DrawMarkers(vh, departing, departingOpacity * (1f - Mathf.Clamp01(age / ExitDuration)));
            DrawMarkers(vh, CurrentMarkers, animations ? Mathf.Clamp01(age / EnterDuration) : 1f);
            meshOpacity = 1f;
            if (intentAge < CombatIntentChangeTracker.Duration)
            {
                byte alpha = animations ? (Mathf.FloorToInt(intentAge / .12f) % 2 == 0 ? (byte)255 : (byte)100) : (byte)200;
                Border(vh, 1, 1, new Color32(255, 196, 94, alpha));
            }
            float resultAge = Time.unscaledTime - resultAt;
            if (resultAge < .40f)
            {
                byte alpha = (byte)(255f * (1f - Mathf.Clamp01(resultAge / .40f)));
                Corners(vh, 1, accepted ? new Color32(198, 255, 226, alpha) : new Color32(255, 105, 86, alpha));
            }
        }

        private void DrawMarkers(VertexHelper vh, MarkerState state, float opacity)
        {
            if (opacity <= 0f) return;
            meshOpacity = opacity;
            var move = state.Move; var attack = state.Attack; var skill = state.Skill;
            bool enemyRange = state.EnemyRange, enemyEffect = state.EnemyEffect, playerEffect = state.PlayerEffect;
            bool enemyRoute = state.EnemyRoute, enemyDestination = state.EnemyDestination, focused = state.Focused;
            byte enemyEdges = state.EnemyEdges, playerEdges = state.PlayerEdges;
            if (enemyEffect)''')
a=s.index('            if (intentAge < CombatIntentChangeTracker.Duration)',s.index('        private void DrawMarkers'));b=s.index('\n        private void Corners',a)
s=s[:a]+'        }\n'+s[b:]
s=s.replace('            Rect rect = rectTransform.rect;', '            tint.a = (byte)Mathf.RoundToInt(tint.a * meshOpacity);\n            Rect rect = rectTransform.rect;')
p.write_text(s,encoding='utf-8')
p=root/'Tests/EditMode/CombatTestArenaScenarioTests.cs';s=p.read_text(encoding='utf-8-sig');idx=s.index('        [Test]');s=s[:idx]+'''        [Test]
        public void EveryBlockedArenaCellHasVisiblePermanentWallTerrain()
        {
            var assets = new CombatFormalVisualAssets();
            assets.LoadRuntime();
            var presenter = new CombatBattlefieldCellPresenter(new BattlefieldPresentationAdapter(), assets);
            foreach (var scenario in CombatTestArenaScenarioCatalog.All)
            {
                var state = CombatTestArenaScenarioCatalog.Build(scenario.Id).State;
                foreach (var position in scenario.Level.BlockedPositions)
                {
                    Assert.That(state.Map.GetTile(position).IsPermanentWall, Is.True, scenario.Id + " " + position);
                    var cell = presenter.Build(state, scenario.Level, new FireBattleState(state),
                        new CombatSelectionController(), false, null, position, _ => null, (_, __) => null, _ => null, _ => null);
                    Assert.That(cell.ObjectTexture, Is.Not.Null, scenario.Id + " " + position);
                    Assert.That(state.Map.IsBlocked(position), Is.True);
                }
            }
        }

''' +s[idx:];p.write_text(s,encoding='utf-8')
