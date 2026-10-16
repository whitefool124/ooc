# OCC Asset Brief Contract

Use the fields needed for the requested brief. Numeric specifications come from the current OCC master plan, its art/interface CSV and machine mirror; archived values do not override them.

## 1. Art-direction brief

- **Role and player reading:** what must be understood in one second at game scale.
- **Narrative/material premise:** select the visual theme and materials from the current life stage. The academy stage is Japanese fantasy magic isekai: clear adventurous magical-academy spaces, fantasy nature and light readable forms. Aether remains measurable, maintainable and costly energy engineering.
- **Silhouette and shape language:** height-to-width, dominant diagonal/vertical/horizontal, identifying tool or device.
- **Palette roles:** neutral material base; faction/role colour; aether cyan only for active aether; rusty red for hostile/elite threat; safety yellow only for service/treasure or warning semantics.
- **Value and light:** dominant light source, focal contrast, shadow mass, glow budget.
- **Perspective/composition:** exact orthographic/top-down/side view; pose or empty space needed for gameplay.
- **Density budget:** three primary readable forms, one material-story detail, optional one controlled accent. Omit incidental decoration.

## 2. Prompt grammar

`[asset type + exact use] — [subject and player-readable silhouette]; [view/framing]; [current-stage visual and material story]; [shape and value hierarchy]; [palette roles]; [lighting]; [pixel-art rendering constraints]; [background/cell/layout constraints]. Exclude: [style and composition failures].`

### Production addenda

- 16x16 semantic icon: one functional symbol only; dark contour, one semantic colour and optional neutral fill; no complete object or material treatment.
- 24x24 material pickup: one item only; thick near-black contour, connected shadow/highlight masses, 5–8 discrete colours and one material cue; centre the 24px source with 4px padding in a 32px delivery canvas; do not rescale it.
- Fixed 32x32 role: one object, tile or icon only; hard clusters; no anti-aliasing; state function before material; no tiny text or insignia.
- 32 PPU battlefield prop: logical occupancy is declared separately from visual canvas. Decode without resizing, then use the smallest fitting 32-multiple canvas; ordinary single-cell props normally stop at 64x64 and remain bottom-centred over the owning cell.
- 32 PPU combat unit: strict side-view body; typical humanoid canvas `32×64`, feet at `Y=62`, centre near `X=16`; larger bodies expand by 32-pixel canvas multiples instead of shrinking detail. Do not use portrait proportions.
- Strict battlefield/icon generated source (not the noncombat HD2D library): the complete subject uses one uniform coarse square macro-pixel lattice. Infer the lattice from the subject, require confidence at least `1.2`, and retry independently rather than relaxing the threshold.
- Battle-map concept: concept only, not importable art; approved battlefield camera with readable logical grid, foot contacts and occlusion; identify cover classes and one industrial-military narrative anchor.
- HUD concept: 1920x1080, full-width battlefield above bottom commands; upper-left unit status, upper-right turn order and hover action-value drawer; image text is disposable; use it only for composition, material, and icon hierarchy.

## 3. Review questions

1. At final size, is the role readable from silhouette rather than colour alone?
2. Does every glow have an aether/system meaning, with enough dark mass around it?
3. With the glow removed, does it still belong to the current life stage through architecture, material and silhouette rather than generic fantasy shorthand?
4. Are there no medieval fantasy, decorative steampunk, generic anime, or faux-AAA realism defaults?
5. For the academy stage, does it support a clear Japanese-fantasy magical-academy atmosphere while preserving OCC's functional aether engineering and semantic colours?

For noncombat HD2D text carriers and props, use A.2.2's adaptive library exception; preserve coarse clusters and provenance without forcing a fixed lattice, palette or hard-alpha decode. For migrated scene lighting/output, read A.2.4 and the project occ-hd2d-quality skill before drafting.
