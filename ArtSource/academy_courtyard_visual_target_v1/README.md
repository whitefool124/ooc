# Warm academy courtyard visual target v1

Status: `VISUAL_TARGET_ONLY`; not a Unity asset and not a modular tile source.

This source establishes the acceptance bar for the current battle-map art pass:

- warm honey limestone, terracotta shadow masses, muted olive planting;
- fixed screen-aligned shallow map view, with one light direction and shared top/front faces;
- readable stone-joint tactical grid, never a black overlay or checkerboard;
- quiet open centre, sparse practice screens, and a rear cloister that creates a place rather than a board;
- no cold sci-fi equipment, rain, crates, rope, or chest decoration.

Its generated pixel lattice failed native32 decoding (`confidence 1.0987`), so it must not be sliced, scaled, or imported. Future ground, directional-edge, background, and prop candidates are reviewed against this composition and material hierarchy before assembly.
