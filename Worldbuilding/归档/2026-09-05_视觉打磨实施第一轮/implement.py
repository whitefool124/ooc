from pathlib import Path
import json
import csv
import io
root=Path('E:/数据库/OCC_Codex')
task=root/'Worldbuilding/归档/2026-09-05_视觉打磨实施第一轮'
changed=[]
def edit(relative, replacements):
 p=root/relative; original=p.read_text(encoding='utf-8-sig'); result=original
 for old,new in replacements:
  assert result.count(old)==1, (relative,old[:100],result.count(old))
  result=result.replace(old,new)
 backup=task/'before/code'/relative
 backup.parent.mkdir(parents=True,exist_ok=True)
 assert not backup.exists(), f'Already applied {relative}'
 backup.write_bytes(p.read_bytes())
 p.write_text(result,encoding='utf-8');changed.append(relative)

# Mirror the now-adopted visual specification before the runtime change.
contract=root/'Tools/OCCArt/occ_art_contract_v1.json'
data=json.loads(contract.read_text(encoding='utf-8-sig'))
data['battlefield_presentation']={'reference_resolution':[1920,1080],'viewport_reference':[16,80,1408,768],'default_cell_reference':64,'zoom_cells_reference':[64,128],'unit_canvas_to_cell_ratio':2,'position_quantum_reference':2,'terrain_labels':'hover_including_occupied_cells','inactive_ranges':'hidden','vfx_frame_display':'native_integer_scale_from_current_cell'}
data['typography'].update({'battlefield_unit_health_track_reference':[112,16],'battlefield_unit_shield_track_reference':[112,8]})
contract.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
spec=root/'Worldbuilding/数据表/OCC_美术与界面规格表_v1.0.csv'
with spec.open(encoding='utf-8-sig',newline='') as stream: rows=list(csv.DictReader(stream))
fields=list(rows[0])
rows=[r for r in rows if r['ID']!='ART-VIEWPORT-OVERVIEW']
rows.append(dict(zip(fields,['ART-VIEWPORT-OVERVIEW','战场表现','首轮全场视口与像素倍率','视口16/80/1408/768；默认64px格；放大128px格','统一世界像素倍率；单位画布2倍格宽','1920与960均整数倍率；血条默认56×8护盾56×4；隐藏非当前范围；特殊地形说明含占位单位'])))
for row in rows:
 if row['ID']=='ART-PLAN-20260905':row['限制']='2026-09-05用户授权执行；首轮具体实施口径见总案第12节；完整目标仍在进行'
with spec.open('w',encoding='utf-8-sig',newline='') as stream:
 writer=csv.DictWriter(stream,fieldnames=fields,lineterminator='\n');writer.writeheader();writer.writerows(rows)

edit('UnityProject/Assets/Game/Runtime/Campaign/BattlefieldPresentationAdapter.cs',[
 ('public const float MinimumCellSize = 64f;', 'public const float OverviewCellSize = 64f;\n        public const float MinimumCellSize = 64f;'),
 ('public const float MaximumCellSize = 160f;', 'public const float MaximumCellSize = 128f;'),
 ('public const float CellSizeStep = 32f;', 'public const float CellSizeStep = 64f;'),
 ('public const float BattlefieldHeight = 900f;', 'public const float BattlefieldHeight = 848f;'),
 ('public const float BoardTop = 24f;', 'public const float BoardTop = 80f;'),
 ('new BattlefieldRect(0f, BoardTop, BattlefieldWidth, BattlefieldHeight - BoardTop)', 'new BattlefieldRect(16f, BoardTop, BattlefieldWidth - 32f, BattlefieldHeight - BoardTop)'),
 ('new BattlefieldViewport(ViewportRect, width, height, CellSize)', 'new BattlefieldViewport(ViewportRect, width, height, OverviewCellSize)'),
 ('        public void Focus(GridPosition position)\n', '        public void ResetOverview()\n        {\n            cellSize = BattlefieldPresentationAdapter.OverviewCellSize;\n            ClampToViewport();\n        }\n\n        public void Focus(GridPosition position)\n'),
 ('            BattlefieldRect cell = CellRect(position);\n            float inset', '            if (BoardWidth <= viewport.Width && BoardHeight <= viewport.Height) return false;\n            BattlefieldRect cell = CellRect(position);\n            float inset'),
 ('boardX = (float)Math.Round(ClampAxis(boardX, BoardWidth, viewport.X, viewport.Width, overscroll));','boardX = 2f * (float)Math.Round(ClampAxis(boardX, BoardWidth, viewport.X, viewport.Width, overscroll) / 2f);'),
 ('boardY = (float)Math.Round(ClampAxis(boardY, BoardHeight, viewport.Y, viewport.Height, overscroll));','boardY = 2f * (float)Math.Round(ClampAxis(boardY, BoardHeight, viewport.Y, viewport.Height, overscroll) / 2f);')])

edit('UnityProject/Assets/Game/Runtime/Presentation/CombatUnitHudLayout.cs',[
 ('commandsTop = 900f;', 'commandsTop = 864f;') if False else ('height = 242f, margin = 16f, battlefieldRight = 1440f, commandsTop = 900f;', 'height = 242f, margin = 16f, battlefieldRight = 1440f, commandsTop = 864f;'),
 ('height = 86f, margin = 16f, battlefieldRight = 1440f, commandsTop = 900f;', 'height = 86f, margin = 16f, battlefieldRight = 1440f, commandsTop = 864f;'),
 ('return new Rect(cell.X + (cell.Width - width) * .5f, cell.Y - 22f, width, 40f);', 'Rect unit = UnitPresentationRect(cell);\n            return new Rect(cell.X + (cell.Width - width) * .5f, unit.y + cell.Width * .3125f - 40f, width, 40f);'),
 ('            // Formal unit sprites own a 64x64 transparent safety canvas. Present that whole canvas\n            // at a whole-number texture scale. Intermediate camera zooms round upward and may\n            // overflow the logical cell; presentation overflow never changes the logical hit cell.\n            float size = Mathf.Max(64f, Mathf.Ceil(cell.Width / 64f) * 64f);','            // One world pixel has one scale: 64px units use twice the canvas of 32px ground.\n            // Keep the whole texture and the foot anchor; visual overhang never changes occupancy.\n            float size = Mathf.Max(128f, Mathf.Ceil(cell.Width / 32f) * 64f);'),
 ('float width = 120f * scale;\n            return new Rect(cell.X + (cell.Width - width) * .5f, cell.Y + 90f * scale, width, 22f * scale);', 'float width = 112f * scale;\n            return new Rect(cell.X + (cell.Width - width) * .5f, cell.YMax - 8f * scale, width, 16f * scale);'),
 ('float width = 120f * scale;\n            return new Rect(cell.X + (cell.Width - width) * .5f, cell.Y + 114f * scale, width, 14f * scale);','float width = 112f * scale;\n            return new Rect(cell.X + (cell.Width - width) * .5f, cell.YMax + 12f * scale, width, 8f * scale);')])

edit('UnityProject/Assets/Game/Runtime/Presentation/CombatBattlefieldCellPresenter.cs',[
 ('Texture2D move = battlefield.IsInMoveRange(state, position) ?', 'Texture2D move = selection.Action == "移动" && battlefield.IsInMoveRange(state, position) ?'),
 ('Texture2D attack = battlefield.IsInAttackRange(state, position) ?', 'Texture2D attack = selection.Action == "攻击" && battlefield.IsInAttackRange(state, position) ?'),
 ('objectLabel = "灯藤";', 'objectLabel = string.Empty;'),
 ('objectLabel = "水面";', 'objectLabel = string.Empty;'),
 ('            Texture2D floor = assets.Academy(FloorKey', '            if (unit != null)\n                hover = AppendTerrainHover(hover, BuildTerrainHover(state, fireBattle, tile, position));\n            Texture2D floor = assets.Academy(FloorKey'),
 ('selection.Action == "移动" ? .88f : .26f, attack, selection.Action == "攻击" ? .88f : .22f,', '.64f, attack, .72f,'),
 ('        public static string BuildTerrainHover(', '        public static string AppendTerrainHover(string unitHover, string terrainHover) =>\n            string.IsNullOrWhiteSpace(terrainHover) ? unitHover : unitHover + "\\n\\n脚下地形 · " + terrainHover;\n\n        public static string BuildTerrainHover(')])

edit('UnityProject/Assets/Game/Runtime/Presentation/FormalBattlefieldView.cs',[
 ('surface.color = new Color(.012f, .022f, .027f, 1f);','surface.color = new Color(.16f, .17f, .15f, 1f);'),
 ('Button home = FormalUiKit.Button("战场归中", "⌂", viewport.transform,\n                new Vector2(BattlefieldPresentationAdapter.BattlefieldWidth - 42f, -8f), new Vector2(32f, 32f),\n                FormalUiTheme.SurfaceRaised, 18);\n            home.onClick.AddListener(host.FocusBattlefieldOnHero);','Button home = FormalUiKit.Button("战场归中", "全场", viewport.transform,\n                new Vector2(host.BattlefieldViewport.ViewportRect.Width - 100f, -8f), new Vector2(88f, 40f),\n                FormalUiTheme.SurfaceRaised, FormalUiTheme.BodyFontSize);\n            home.onClick.AddListener(() => host.BattlefieldViewport.ResetOverview());'),
 ('// transparent margins determine visual footprint, preserving 2x/3x/4x/5x pixel scales.','// transparent margins determine visual footprint at approved integer pixel scales.'),
 ('localX, localY, unit.width, unit.height);','Mathf.Round(localX / 2f) * 2f, Mathf.Round(localY / 2f) * 2f, unit.width, unit.height);'),
 ('            if (model.Unit == null || model.Unit.IsHero)\n', '            TileState hoveredTile = host.CurrentState.Map.GetTile(position);\n            if (model.Unit == null || model.Unit.IsHero || hoveredTile.IsWater || hoveredTile.IsLampVine || hoveredTile.IsScorched)\n')])

edit('UnityProject/Assets/Game/Runtime/Presentation/CombatVisualFeedback.cs',[
 ('rect.anchoredPosition = CurrentGridFeedbackPosition(position); rect.sizeDelta = new Vector2(58, 58);','rect.anchoredPosition = CurrentGridFeedbackPosition(position);\n            float size = CurrentFeedbackCellSize();\n            rect.sizeDelta = new Vector2(size, size);'),
 ('        private Vector2 CurrentGridFeedbackPosition(GridPosition position) =>', '        private float CurrentFeedbackCellSize()\n        {\n            if (bootstrap == null) return BattlefieldPresentationAdapter.OverviewCellSize;\n            float cell = Mathf.Abs(bootstrap.GridToFeedbackPosition(new GridPosition(1, 0)).x -\n                bootstrap.GridToFeedbackPosition(new GridPosition(0, 0)).x);\n            return Mathf.Max(64f, Mathf.Round(cell / 64f) * 64f);\n        }\n\n        private Vector2 CurrentGridFeedbackPosition(GridPosition position) =>')])
print(json.dumps({'edited':changed},ensure_ascii=False))
