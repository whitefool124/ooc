import { mkdir, readFile, readdir, writeFile } from "node:fs/promises";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const toolRoot = fileURLToPath(new URL(".", import.meta.url));
export const repositoryRoot = resolve(toolRoot, "../..");
export const mapDirectory = join(repositoryRoot, "Worldbuilding", "地图配置", "战斗地图");
export const mapTablePath = join(repositoryRoot, "Worldbuilding", "数据表", "OCC_战斗地图配置表_v1.0.csv");
export const unityCatalogPath = join(repositoryRoot, "UnityProject", "Assets", "Game", "Runtime", "Campaign", "GeneratedBattleMapCatalog.cs");

export const firstRunIds = ["rain_lantern_court", "first_battle_greenhouse_collection_room", "first_b3_rain_prism_court", "first_elite_three_material_pressure"];
export const coreIds = ["rail_patrol", "depot_wreck", "relay_raid", "signal_hub", "gatehouse", "transmission_tower", "elite_foundry", "core_approach", "core_finale"];
export const eliteIds = ["calibration_lockdown", "cliff_relay_survey", "sealed_vault_certification", "library_discipline", "outer_ring_clearance"];
export const weakLayoutIds = ["weak_layout_w1", "weak_layout_w2", "weak_layout_w3", "weak_layout_w4"];
export const formalIds = [...firstRunIds, ...coreIds, ...eliteIds, ...weakLayoutIds];
export const editorVisibleIds = [
  "rain_lantern_court", "first_battle_greenhouse_collection_room", "first_b3_rain_prism_court",
  "rail_patrol", "depot_wreck", "relay_raid", "signal_hub", "gatehouse", "transmission_tower",
  "weak_layout_w1", "weak_layout_w2", "weak_layout_w3",
  "first_elite_three_material_pressure", "elite_foundry", "core_approach", "calibration_lockdown",
  "core_finale"
];

const csvCell = value => `"${String(value ?? "").replaceAll('"', '""')}"`;
const q = value => `"${String(value ?? "").replaceAll("\\", "\\\\").replaceAll('"', '\\"')}"`;
const pos = value => `new GridPosition(${value.x}, ${value.y})`;
const battleTypeLabel = map => map?.isBoss ? "Boss战斗" : map?.isElite ? "精英战斗" : "普通战斗";

export async function writeFileResilient(path, content, encoding = "utf8") {
  const retryableCodes = new Set(["EBUSY", "EPERM", "EACCES"]);
  let lastError;
  for (let attempt = 0; attempt < 5; attempt++) {
    try {
      await writeFile(path, content, encoding);
      return;
    } catch (error) {
      lastError = error;
      if (!retryableCodes.has(error?.code) || attempt === 4) throw error;
      await new Promise(resolveDelay => setTimeout(resolveDelay, 120 * (attempt + 1)));
    }
  }
  throw lastError;
}

export function validateMap(map, { formal = false } = {}) {
  const errors = [];
  const area = Number(map?.width) * Number(map?.height);
  if (!map || map.schemaVersion !== "occ-battle-map-v1") errors.push("schemaVersion 必须为 occ-battle-map-v1");
  if (!/^[a-z0-9_$-]+$/.test(map?.id || "")) errors.push("地图 ID 非法");
  if (map?.isBoss && map?.isElite) errors.push("地图类型不能同时为精英与 Boss");
  if (formal && !new RegExp(`^${battleTypeLabel(map)} \\d{2} · `).test(String(map?.displayName || ""))) errors.push(`地图名称必须使用“${battleTypeLabel(map)} 01 · 地图名”格式`);
  if (!Number.isInteger(map?.width) || !Number.isInteger(map?.height) || map.width < 4 || map.width > 12 || map.height < 4 || map.height > 10) errors.push("宽高超出允许范围");
  if (!Number.isInteger(area) || area < 25 || area > 50) errors.push("地图总格数必须为 25–50");
  if (!Array.isArray(map?.enemies) || !Array.isArray(map?.terrain) || !Array.isArray(map?.blockedPositions)) errors.push("地图集合字段缺失");
  const inside = item => item && item.x >= 0 && item.y >= 0 && item.x < map.width && item.y < map.height;
  const occupied = new Set();
  const add = (item, label) => {
    if (!inside(item)) errors.push(`${label} 越界 (${item?.x},${item?.y})`);
    const key = `${item?.x},${item?.y}`;
    if (occupied.has(key)) errors.push(`${label} 与其他阻挡/单位重叠 (${key})`);
    occupied.add(key);
  };
  if (!inside(map?.heroSpawn)) errors.push("主角出生点缺失或越界"); else add(map.heroSpawn, "主角");
  for (const enemy of map?.enemies || []) add(enemy, `敌人 ${enemy.archetypeId}`);
  const blocking = new Set(["HeavyCover", "PermanentWall", "AetherObjective", "AetherCrystal", "PressureCrystal", "LootChest", "TowerMechanism", "OverloadDevice", "WardGenerator"]);
  for (const terrain of map?.terrain || []) {
    if (!inside(terrain)) errors.push(`${terrain.kind} 越界 (${terrain.x},${terrain.y})`);
    if (blocking.has(terrain.kind)) add(terrain, terrain.kind);
    if (["Fireground", "Smoke", "Trace", "BindingMark"].includes(terrain.kind) && (!Number.isInteger(terrain.duration) || terrain.duration <= 0))
      errors.push(`${terrain.kind} 必须配置正整数持续时间 (${terrain.x},${terrain.y})`);
    if (["OverloadDevice", "WardGenerator"].includes(terrain.kind) && (!Number.isInteger(terrain.durability) || terrain.durability <= 0))
      errors.push(`${terrain.kind} 必须配置正整数耐久 (${terrain.x},${terrain.y})`);
  }
  for (const blocked of map?.blockedPositions || []) add(blocked, "禁用格");
  if ((map?.terrain || []).filter(item => item.kind === "LootChest").length > 1) errors.push("每张地图最多放置一个宝箱");
  const changeIds = new Set();
  for (const change of map?.changeAnnotations || []) {
    if (!change?.id || !/^[a-z0-9_-]+$/.test(change.id)) errors.push("动态变化标注 ID 非法");
    else if (changeIds.has(change.id)) errors.push(`动态变化标注 ID 重复：${change.id}`);
    else changeIds.add(change.id);
    if (!["phase", "temporary"].includes(change?.type)) errors.push(`动态变化标注类型非法：${change?.id || "未命名"}`);
    for (const field of ["label", "timing", "trigger", "result", "duration", "source"])
      if (!String(change?.[field] || "").trim()) errors.push(`动态变化标注 ${change?.id || "未命名"} 缺少 ${field}`);
    for (const position of change?.positions || []) if (!inside(position)) errors.push(`动态变化标注 ${change?.id || "未命名"} 越界 (${position.x},${position.y})`);
  }
  const enemyIds = new Set((map?.enemies || []).map(item => item.archetypeId));
  const hasChange = type => (map?.changeAnnotations || []).some(item => item.type === type);
  if (enemyIds.has("core_overseer") && !hasChange("phase")) errors.push("塔之守卫地图缺少阶段变化标注");
  if (enemyIds.has("core_overseer") && !hasChange("temporary")) errors.push("塔之守卫地图缺少临时重掩体标注");
  if ((enemyIds.has("elite_vanguard") || enemyIds.has("prototype_hand")) && !hasChange("temporary")) errors.push("会生成临时结构的单位缺少临时结构标注");
  if (formal && area <= 36 && !map.terrain.some(item => item.kind === "PermanentWall")) errors.push("正式普通地图应使用永久重物块塑形");
  if (formal && area >= 40 && !map.complexMechanics) errors.push("40 格以上正式地图必须声明 complexMechanics");
  if (map?.objectiveType === "Destruction" && !map.terrain?.some(item => item.kind === "AetherObjective")) errors.push("破坏目标地图缺少任务目标");
  if (map?.isBoss && map.terrain?.filter(item => item.kind === "TowerMechanism").length !== 3) errors.push("首领地图必须有 3 个塔内机关");
  return errors;
}

export async function readMaps() {
  await mkdir(mapDirectory, { recursive: true });
  const names = (await readdir(mapDirectory)).filter(name => name.endsWith(".occ-map.json")).sort();
  const maps = [];
  for (const name of names) {
    try { maps.push(JSON.parse(await readFile(join(mapDirectory, name), "utf8"))); }
    catch { /* Invalid drafts remain on disk but are not indexed. */ }
  }
  return maps;
}

export async function rebuildMapTable(maps) {
  const header = ["地图ID","名称","配置文件","宽","高","总格数","复杂机制","地板主题","目标类型","等级","精英","首领","主角出生","敌人编成","地形数量","永久重物块","动态变化数量","动态变化摘要","空间语法","公开风险","反制窗口","启用","版本"];
  const rows = [...maps].sort((a, b) => a.id.localeCompare(b.id)).map(map => [
    map.id, map.displayName, `Worldbuilding/地图配置/战斗地图/${map.id}.occ-map.json`, map.width, map.height, map.width * map.height,
    map.complexMechanics ? "是" : "否", map.floorTheme, map.objectiveType, map.tier, map.isElite ? "是" : "否", map.isBoss ? "是" : "否",
    map.heroSpawn ? `${map.heroSpawn.x}:${map.heroSpawn.y}` : "", map.enemies.map(item => `${item.archetypeId}@${item.x}:${item.y}`).join("|"), map.terrain.length,
    map.terrain.filter(item => item.kind === "PermanentWall").length, (map.changeAnnotations || []).length,
    (map.changeAnnotations || []).map(item => `${item.label}@${item.timing}`).join("|"), map.spaceContract?.grammar || "", map.spaceContract?.publicRisk || "", map.spaceContract?.counterplayWindow || "",
    formalIds.includes(map.id) ? "是" : "草稿", "2"
  ]);
  const csv = `\ufeff${[header, ...rows].map(row => row.map(csvCell).join(",")).join("\r\n")}\r\n`;
  await writeFileResilient(mapTablePath, csv, "utf8");
}

const terrainToCSharp = item => `new LevelTerrainPlacement(${item.x}, ${item.y}, LevelTerrainKind.${item.kind}, ${item.mechanismKind || 0}${item.duration || item.durability ? `, ${item.duration || 0}` : ""}${item.durability ? `, ${item.durability}` : ""})`;

function mapToCSharp(map) {
  const prerequisites = map.prerequisites.length ? `new[] { ${map.prerequisites.map(q).join(", ")} }` : "Array.Empty<string>()";
  const enemies = map.enemies.map(item => `new LevelEnemyPlacement(${q(item.archetypeId)}, ${item.x}, ${item.y})`).join(",\n                    ");
  const terrain = map.terrain.map(terrainToCSharp).join(",\n                    ");
  const blocked = map.blockedPositions.length ? `new[] { ${map.blockedPositions.map(pos).join(", ")} }` : "Array.Empty<GridPosition>()";
  return `            new FirstRegionLevelDefinition(${q(map.id)}, ${q(map.displayName)}, ${q(map.objectiveSummary)}, CombatObjectiveType.${map.objectiveType}, ${map.tier},\n                ${pos(map.heroSpawn)}, FirstRegionFloorTheme.${map.floorTheme}, ${String(map.isElite).toLowerCase()}, ${String(map.isBoss).toLowerCase()}, ${prerequisites},\n                new[]\n                {\n                    ${enemies}\n                },\n                new[]\n                {\n                    ${terrain}\n                },\n                new LevelSpaceContract(${q(map.spaceContract.grammar)},\n                    ${q(map.spaceContract.publicRisk)}, ${q(map.spaceContract.counterplayWindow)}),\n                width: ${map.width}, height: ${map.height}, blockedPositions: ${blocked})`;
}

export async function rebuildUnityCatalog(maps) {
  const byId = new Map(maps.map(map => [map.id, map]));
  const missing = formalIds.filter(id => !byId.has(id));
  if (missing.length) throw new Error(`缺少正式地图：${missing.join(", ")}`);
  for (const id of formalIds) {
    const errors = validateMap(byId.get(id), { formal: true });
    if (errors.length) throw new Error(`${id}: ${errors.join("；")}`);
  }
  const list = ids => ids.map(id => mapToCSharp(byId.get(id))).join(",\n");
  const weakLayout = id => {
    const map = byId.get(id); const signature = id.slice(-2).toUpperCase();
    return `            [${q(signature)}] = new RogueliteEncounterLayout(${q(signature)}, ${pos(map.heroSpawn)},\n                new[] { ${map.enemies.map(pos).join(", ")} },\n                new[] { ${map.terrain.map(terrainToCSharp).join(", ")} },\n                width: ${map.width}, height: ${map.height}, blockedPositions: Array.Empty<GridPosition>())`;
  };
  const source = `// <auto-generated />\n// Generated from Worldbuilding/地图配置/战斗地图/*.occ-map.json by Tools/BattleMapEditor/map-build.mjs.\nusing System;\nusing System.Collections.Generic;\nusing System.Linq;\n\nnamespace OCC.Combat\n{\n    public static class GeneratedBattleMapCatalog\n    {\n        public static readonly IReadOnlyList<FirstRegionLevelDefinition> FirstRunLevels = new[]\n        {\n${list(firstRunIds)}\n        };\n\n        public static readonly IReadOnlyList<FirstRegionLevelDefinition> CoreLevels = new[]\n        {\n${list(coreIds)}\n        };\n\n        public static readonly IReadOnlyList<FirstRegionLevelDefinition> EliteLevels = new[]\n        {\n${list(eliteIds)}\n        };\n\n        private static readonly IReadOnlyDictionary<string, FirstRegionLevelDefinition> ById =\n            FirstRunLevels.Concat(CoreLevels).Concat(EliteLevels).ToDictionary(level => level.Id, StringComparer.Ordinal);\n\n        private static readonly IReadOnlyDictionary<string, RogueliteEncounterLayout> WeakLayouts =\n            new Dictionary<string, RogueliteEncounterLayout>(StringComparer.Ordinal)\n            {\n${weakLayoutIds.map(weakLayout).join(",\n")}\n            };\n\n        public static FirstRegionLevelDefinition For(string id) => ById[id];\n        public static RogueliteEncounterLayout WeakLayout(string signature) => WeakLayouts[signature];\n    }\n}\n`;
  await mkdir(dirname(unityCatalogPath), { recursive: true });
  await writeFileResilient(unityCatalogPath, source, "utf8");
}

export async function rebuildAllDerivedData() {
  const maps = await readMaps();
  await rebuildMapTable(maps);
  await rebuildUnityCatalog(maps);
  return maps;
}
