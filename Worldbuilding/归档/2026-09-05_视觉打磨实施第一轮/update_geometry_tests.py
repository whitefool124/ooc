from pathlib import Path
root=Path('E:/数据库/OCC_Codex')
task=root/'Worldbuilding/归档/2026-09-05_视觉打磨实施第一轮'
def patch(name, replacements):
 p=root/'UnityProject/Assets/Game/Tests/EditMode'/name
 s=p.read_text(encoding='utf-8-sig')
 backup=task/'before/tests'/name;backup.parent.mkdir(parents=True,exist_ok=True)
 assert not backup.exists();backup.write_bytes(p.read_bytes())
 for old,new in replacements:
  assert old in s,old[:80];s=s.replace(old,new)
 p.write_text(s,encoding='utf-8')
patch('BattlefieldPresentationAdapterTests.cs',[
 ('Is.EqualTo(1536f)','Is.EqualTo(768f)'),('Is.EqualTo(1152f)','Is.EqualTo(576f)'),
 ('Assert.That(board.X, Is.LessThanOrEqualTo(0f));','Assert.That(board.X, Is.GreaterThanOrEqualTo(adapter.ViewportRect.X));'),
 ('Assert.That(board.Y, Is.LessThanOrEqualTo(BattlefieldPresentationAdapter.BoardTop));','Assert.That(board.Y, Is.GreaterThanOrEqualTo(BattlefieldPresentationAdapter.BoardTop));\n            Assert.That(board.XMax, Is.LessThanOrEqualTo(adapter.ViewportRect.XMax));\n            Assert.That(board.YMax, Is.LessThanOrEqualTo(adapter.ViewportRect.YMax));'),
 ('Assert.That(viewport.CellSize, Is.EqualTo(128f));\n            viewport.ZoomAt(720f, 450f, -1);\n            Assert.That(viewport.CellSize, Is.EqualTo(96f));','Assert.That(viewport.CellSize, Is.EqualTo(64f));\n            Assert.That(viewport.ZoomAt(720f, 450f, -1), Is.False);\n            Assert.That(viewport.CellSize, Is.EqualTo(64f));'),
 ('Is.EqualTo(160f)','Is.EqualTo(128f)'),('after.X + 40f','after.X + 64f'),('after.Y + 60f','after.Y + 96f'),
 ('            viewport.Focus(new GridPosition(6, 4));\n            Assert.That(viewport.IsNearSafeEdge','            Assert.That(viewport.IsNearSafeEdge(new GridPosition(0, 8)), Is.False);\n            viewport.ZoomAt(720f, 450f, 1);\n            viewport.Focus(new GridPosition(6, 4));\n            Assert.That(viewport.IsNearSafeEdge'),
 ('            viewport.Focus(new GridPosition(5, 8));','            viewport.ZoomAt(720f, 450f, 1);\n            viewport.Focus(new GridPosition(5, 8));'),
 ('            var input = new BattlefieldViewportInputController();\n            input.HandleGuiEvent(new Event { type = EventType.KeyDown, keyCode = KeyCode.Space }','            viewport.ZoomAt(720f, 450f, 1);\n            var input = new BattlefieldViewportInputController();\n            input.HandleGuiEvent(new Event { type = EventType.KeyDown, keyCode = KeyCode.Space }'),
 ('        [Test]\n        public void TryResolveCell_AccountsForInvertedVisualYAndCellGap()', '''        [TestCase(1f)]
        [TestCase(.5f)]
        public void Overview_AllCellsUnitsAndIntentBadgesFitAndKeepNativePixels(float screenScale)
        {
            BattlefieldViewport viewport = new BattlefieldPresentationAdapter().CreateViewport();
            BattlefieldRect safe = viewport.ViewportRect;
            for (int y = 0; y < 9; y++) for (int x = 0; x < 12; x++)
            {
                BattlefieldRect cell = viewport.CellRect(new GridPosition(x, y));
                Rect unit = CombatUnitHudLayout.UnitPresentationRect(cell);
                Rect badge = CombatUnitHudLayout.EnemyIntentBadgeRect(cell, 5);
                Rect shield = CombatUnitHudLayout.UnitShieldBarRect(cell);
                Assert.That(unit.xMin, Is.GreaterThanOrEqualTo(safe.X));
                Assert.That(unit.xMax, Is.LessThanOrEqualTo(safe.XMax));
                Assert.That(badge.yMin, Is.GreaterThanOrEqualTo(safe.Y));
                Assert.That(shield.yMax, Is.LessThanOrEqualTo(safe.YMax));
                Assert.That(unit.width * screenScale / 64f % 1f, Is.Zero);
                Assert.That(cell.Width * screenScale / 32f % 1f, Is.Zero);
                Assert.That(unit.x * screenScale % 1f, Is.Zero);
                Assert.That(unit.y * screenScale % 1f, Is.Zero);
            }
            viewport.ZoomAt(720f, 400f, 1);
            viewport.Pan(17.3f, 27.4f);
            viewport.ResetOverview();
            Assert.That(viewport.BoardRect.XMax, Is.LessThanOrEqualTo(safe.XMax));
            Assert.That(viewport.BoardRect.YMax, Is.LessThanOrEqualTo(safe.YMax));
        }

        [Test]
        public void TryResolveCell_AccountsForInvertedVisualYAndCellGap()''')])
patch('CombatUnitHudPresentationTests.cs',[
 ('visible.width, Is.EqualTo(128f)','visible.width, Is.EqualTo(256f)'),
 ('[TestCase(64f, 64f)]','[TestCase(64f, 128f)]'),('[TestCase(80f, 128f)]','[TestCase(80f, 192f)]'),
 ('[TestCase(96f, 128f)]','[TestCase(96f, 192f)]'),('[TestCase(128f, 128f)]','[TestCase(128f, 256f)]'),
 ('[TestCase(160f, 192f)]','[TestCase(160f, 320f)]')])
patch('CombatHoverInformationTests.cs',[
 ('EnlargedUnitAndVitalBars_StayInsideTheirCell','EnlargedUnit_OverhangsWithoutChangingCellAndVitalsRemainBelowFeet'),
 ('Assert.That(unit.xMin, Is.GreaterThanOrEqualTo(cell.X));','Assert.That(unit.center.x, Is.EqualTo(cell.X + cell.Width / 2f));'),
 ('Assert.That(unit.yMin, Is.GreaterThanOrEqualTo(cell.Y));','Assert.That(unit.yMin, Is.LessThan(cell.Y));'),
 ('Assert.That(unit.xMax, Is.LessThanOrEqualTo(cell.X + cell.Width));','Assert.That(unit.width, Is.EqualTo(cell.Width * 2f));'),
 ('Assert.That(health.yMax, Is.LessThanOrEqualTo(cell.Y + cell.Height));','Assert.That(health.yMin, Is.GreaterThanOrEqualTo(unit.yMin + unit.height * 58f / 64f));'),
 ('Assert.That(shield.yMax, Is.LessThanOrEqualTo(cell.Y + cell.Height));','Assert.That(shield.yMin, Is.GreaterThan(health.yMax));'),
 ('Is.EqualTo(120f)','Is.EqualTo(112f)'),('120f / 128f','112f / 128f'),('22f / 128f','16f / 128f'),('14f / 128f','8f / 128f'),
 ('Mathf.Max(visible.width, visible.height), Is.EqualTo(128f)','Mathf.Max(visible.width, visible.height), Is.EqualTo(256f)'),
 ('Assert.That(visible.yMin, Is.GreaterThanOrEqualTo(cell.Y));','Assert.That(visible.yMin, Is.EqualTo(cell.Y - cell.Height));'),
 ('Assert.That(visible.xMin, Is.EqualTo(cell.X).Within(.0001f));','Assert.That(visible.xMin, Is.EqualTo(cell.X - cell.Width / 2f).Within(.0001f));'),
 ('Assert.That(visible.xMax, Is.EqualTo(cell.X + cell.Width).Within(.0001f));','Assert.That(visible.xMax, Is.EqualTo(cell.X + cell.Width * 1.5f).Within(.0001f));')])
print('Updated geometry regression expectations and added dual-resolution full-board coverage.')
