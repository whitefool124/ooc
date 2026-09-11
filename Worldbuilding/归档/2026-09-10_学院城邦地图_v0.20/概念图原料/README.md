# 学院城邦概念图原料

## 学院城邦_全城鸟瞰概念_v1.png

- 状态：`CONCEPT`
- 用途：依据学院城邦 v0.20 地图结构生成的全城环境概念图，用于审阅城市空间、建筑语言与气氛。
- 生成方式：Codex 内置图像生成。
- 生成日期：2026-09-09
- 尺寸：1672×941 px
- SHA-256：`E00D077B2BBE43CBD763A3A851CA55458FCA24B1A7BED21355054642E0B5DAE6`
- 结构参考：`../学院全域地图_建筑与占地草案_v0.1.md` 与 `../../../../Tools/AcademyMapPrototype/data/academy-campus-v0.20.geojson`
- 资产边界：概念原料，不是地图几何母版，不得反向覆盖 v0.20 道路、建筑或分区数据；未建立 `occ-art-manifest-v1`，不得进入 Unity 或标记为正式资产。

### 玩家阅读目标

一眼读出中央河湖分隔的学院／城市双核心、两座功能不同的桥、学院钟楼、城市公共核心、东南工坊和南部课程场。

### 已锁定内容

- 左岸学院，右岸市政、市集与工坊；
- 南北连续河湖；
- 普通公共桥与巨蚓专用桥分离；
- 巨蚓是固定导轨上的公共交通，背部承载实用车厢；
- 近代魔法工业文明，使用石材、旧木、锻铁、旧铜和可维护以太设施；
- 冷青光只表现正在工作的以太装置；
- 无文字、标牌、界面或水印。

### 审阅结论

当前图已表达地图骨架和主要功能区，适合作为第一张全城气氛概念。建筑语言仍偏整洁的大陆学院／市政传统，需要人工确认是否继续沿此方向；它不构成建筑立面或逐区细节定稿。

### 最终生成提示词

```text
Use case: stylized-concept
Asset type: OCC game environment concept art, city establishing image, visual exploration only
Input image: Image 1 is the structural map reference. Use it only for city layout, district placement, river shape, bridges, density, and landmark relationships; do not reproduce its UI, labels, text, colors, or flat-map rendering.
Primary request: Generate a polished wide landscape aerial concept painting of the complete OCC Academy City-State based on the supplied map.
Scene/backdrop: A self-contained near-modern magical-industrial city spanning both banks of a north-south river that widens into a large central lake. The west bank is the academy core and student living districts; the east bank is the civic core, covered market, workshops, freight, and controlled archive district. The north edge becomes sparse green outer land; the south edge contains a stadium, nonlethal ward-combat grounds, squad tactical grounds, and water-management facilities.
Required structural invariants: Preserve the map's clear west-academy / east-civic dual core. Keep the continuous river-lake as the central spine. Show a broad ordinary civic bridge linking the two cores and a visibly separate fixed-guideway bridge for the tamed giant earthworm transit. Include the academy clock tower as the west-bank landmark, the civic hall and covered market as east-bank landmarks, denser low-to-mid-rise blocks around both cores, regular but lived-in streets, riverside public walkways, five small parks, southeast industrial workshops, a restricted archive compound, and the large southern course fields.
Narrative/material premise: Aether is measurable, maintained energy engineering in a near-modern magical industrial civilization. Buildings use practical pale stone, lime plaster, old dark wood, wrought iron, weathered copper, ceramic roof or practical flat-roof service additions. Aether infrastructure appears as maintainable conduits, gauges, insulated pylons, ward frames, service access panels, and restrained cold-cyan active light. Show one tamed colossal earthworm-shaped transit creature moving on its fixed track with several practical passenger carriages secured along its back; it is public infrastructure, not a monster attack.
Style/medium: grounded painterly environment concept art with readable architecture and believable urban planning, detailed but not photorealistic, original design, no imitation of any named artist or game.
Composition/framing: wide 16:9 oblique bird's-eye establishing view from the southwest looking north across the city; the lake occupies the center without swallowing the city; academy clock tower anchors the left third, civic hall and market anchor the right third; both bridges remain separately readable; southern training fields are visible in the foreground.
Lighting/mood: calm clear morning after rain, wet roofs and streets catching soft light, thin mist above the lake, inhabited and maintained rather than grandiose or ruined.
Color palette: restrained warm stone, charcoal iron, muted wood brown, weathered copper and natural greens; water uses subdued blue-green. Cold cyan only for active aether devices, oxidized brass for maintenance and transit details, no saturated rainbow magic.
Text: no text, no labels, no signage, no logo, no watermark.
Avoid: medieval fantasy castle or walled feudal city, Gothic cathedral dominance, generic wizard school, pure steampunk gears and smokestack clutter, Victorian London imitation, floating islands, giant crystal spires, neon cyberpunk, excessive magical glow, military fortress composition, symmetrical palace plan, empty decorative plazas, skyscrapers, modern cars, rail locomotive replacing the earthworm transit, UI panels or map graphics.
```
