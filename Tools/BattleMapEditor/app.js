const enemyCatalog = [
  ["sigil_mauler", "替身偶"], ["barrier_mender", "补盾助教"], ["tether_hound", "寻迹兽"],
  ["shieldguard", "盾术生"], ["pyromancer", "火矢生"], ["raider", "侧锋生"],
  ["elite_vanguard", "划线教官"], ["stone_snare", "拴索助教"], ["lantern_revealer", "提灯巡查"],
  ["rune_arbalist", "背弩生"], ["core_overseer", "塔之守卫"], ["breach_ram", "楔角"],
  ["elder_tracker_hound", "老寻"], ["wind_librarian", "小铃"], ["prototype_hand", "试制员"],
  ["legacy_storekeeper", "老库管"], ["signal_keeper", "灯台值守"]
].map(([id, name]) => ({ id, name }));

const tokens = [
  { id: "hero", label: "主角出生", group: "单位与标记", layer: "units", glyph: "主", color: "#63b5d0" },
  { id: "enemy", label: "敌人", group: "单位与标记", layer: "units", glyph: "敌", color: "#db7264" },
  { id: "routeAnchor", label: "路线锚点", group: "单位与标记", layer: "markers", glyph: "路", color: "#e2b85b" },
  { id: "blocked", label: "禁用格", group: "单位与标记", layer: "markers", glyph: "禁", color: "#c9665b" },
  { id: "LightCover", label: "轻掩体", group: "物件", layer: "objects", glyph: "轻", color: "#8aa16a" },
  { id: "HeavyCover", label: "重掩体", group: "物件", layer: "objects", glyph: "重", color: "#667c89" },
  { id: "PermanentWall", label: "永久重物块", group: "物件", layer: "objects", glyph: "墙", color: "#4f5b66" },
  { id: "AetherObjective", label: "任务目标", group: "物件", layer: "objects", glyph: "目", color: "#d49844" },
  { id: "LampVine", label: "灯藤", group: "物件", layer: "objects", glyph: "藤", color: "#4a9a73" },
  { id: "AetherCrystal", label: "蓄能晶簇", group: "物件", layer: "objects", glyph: "晶", color: "#7a8fd4" },
  { id: "TowerMechanism", label: "塔内机关", group: "物件", layer: "objects", glyph: "机", color: "#b27ad1" },
  { id: "StakedStructure", label: "标定结构", group: "物件", layer: "objects", glyph: "桩", color: "#9a7457" },
  { id: "OverloadDevice", label: "过载装置", group: "物件", layer: "objects", glyph: "载", color: "#d06254" },
  { id: "CertifierStand", label: "检定台", group: "物件", layer: "objects", glyph: "检", color: "#9175b4" },
  { id: "WardGenerator", label: "护罩发生器", group: "物件", layer: "objects", glyph: "护", color: "#4e94be" },
  { id: "Water", label: "浅水", group: "效果层", layer: "effects", glyph: "水", color: "#337fa1" },
  { id: "LoosePaper", label: "散页", group: "效果层", layer: "effects", glyph: "页", color: "#c6ad72" },
  { id: "Trace", label: "痕迹", group: "效果层", layer: "effects", glyph: "迹", color: "#aa7b57" },
  { id: "BindingMark", label: "约束纹", group: "效果层", layer: "effects", glyph: "缚", color: "#ad6bc4" }
];

const tokenById = Object.fromEntries(tokens.map(token => [token.id, token]));
const enemyName = Object.fromEntries(enemyCatalog.map(enemy => [enemy.id, enemy.name]));
const objectKinds = new Set(tokens.filter(token => token.layer === "objects").map(token => token.id));
const effectKinds = new Set(tokens.filter(token => token.layer === "effects").map(token => token.id));
const storageKey = "occ-battle-map-library-v1";
const lastMapKey = "occ-battle-map-last-v1";

const initialMap = {
  schemaVersion: "occ-battle-map-v1",
  id: "core_finale",
  displayName: "古塔核心",
  objectiveSummary: "击败拦在必经之路上的塔之守卫",
  objectiveType: "Elimination",
  tier: 5,
  width: 12,
  height: 9,
  floorTheme: "AetherMarked",
  isElite: false,
  isBoss: true,
  prerequisites: ["core_approach"],
  heroSpawn: { x: 0, y: 4 },
  enemies: [{ archetypeId: "core_overseer", x: 6, y: 4 }],
  terrain: [
    [3,3,"LightCover"],[3,6,"LightCover"],[9,4,"LightCover"],[9,6,"LightCover"],
    [5,3,"HeavyCover"],[7,3,"HeavyCover"],[7,5,"HeavyCover"],[5,5,"HeavyCover"],
    [9,2,"TowerMechanism",1],[3,1,"TowerMechanism",2],[6,7,"TowerMechanism",3]
  ].map(([x, y, kind, mechanismKind = 0]) => ({ x, y, kind, mechanismKind })),
  blockedPositions: [],
  spaceContract: {
    grammar: "中心核心与三道机关门槛",
    routeAnchors: [{ x: 2, y: 2 }, { x: 2, y: 6 }],
    publicRisk: "塔之守卫居中；三组塔内机关在三个阶段内逐组放行，放行前不提供任何效果。",
    counterplayWindow: "三组机关是塔之守卫的施术介质，耐久公开、可以先拆；拆掉已放行的机关即切断对应加成。"
  },
  notes: "从当前 Unity FirstRegionLevelCatalog 同步的编辑器示例。",
  updatedAt: new Date().toISOString()
};

const blankMap = () => ({
  schemaVersion: "occ-battle-map-v1", id: "new_battle_map", displayName: "新战斗地图",
  objectiveSummary: "击倒全部敌人", objectiveType: "Elimination", tier: 1,
  width: 12, height: 9, floorTheme: "Courtyard", isElite: false, isBoss: false,
  prerequisites: [], heroSpawn: { x: 1, y: 1 }, enemies: [], terrain: [], blockedPositions: [],
  spaceContract: { grammar: "", routeAnchors: [], publicRisk: "", counterplayWindow: "" },
  notes: "", updatedAt: new Date().toISOString()
});

let mapData = loadLastMap() || structuredClone(initialMap);
let activeTool = "select";
let selectedCell = null;
let selectedEnemy = enemyCatalog[0].id;
let fileHandle = null;
let dirty = false;
let pointerPainting = false;
let strokeChanged = false;
let undoStack = [];
let redoStack = [];
let visibleLayers = { effects: true, objects: true, units: true, markers: true };
let workspaceMaps = [];

const $ = selector => document.querySelector(selector);
const $$ = selector => [...document.querySelectorAll(selector)];
const clone = value => structuredClone(value);
const posEqual = (a, b) => a && b && a.x === b.x && a.y === b.y;
const inBounds = pos => pos && pos.x >= 0 && pos.y >= 0 && pos.x < mapData.width && pos.y < mapData.height;
const escapeHtml = value => String(value ?? "").replace(/[&<>'"]/g, char => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", "'": "&#39;", '"': "&quot;" })[char]);

function normalizeMap(source) {
  const base = blankMap();
  const normalized = { ...base, ...source };
  normalized.width = Math.max(4, Math.min(32, Number(normalized.width) || 12));
  normalized.height = Math.max(4, Math.min(24, Number(normalized.height) || 9));
  normalized.tier = Math.max(1, Number(normalized.tier) || 1);
  normalized.heroSpawn = source.heroSpawn ?? null;
  normalized.enemies = Array.isArray(source.enemies) ? source.enemies : [];
  normalized.terrain = Array.isArray(source.terrain) ? source.terrain : [];
  normalized.blockedPositions = Array.isArray(source.blockedPositions) ? source.blockedPositions : [];
  normalized.prerequisites = Array.isArray(source.prerequisites) ? source.prerequisites : [];
  normalized.spaceContract = { ...base.spaceContract, ...(source.spaceContract || {}) };
  normalized.spaceContract.routeAnchors = Array.isArray(normalized.spaceContract.routeAnchors) ? normalized.spaceContract.routeAnchors : [];
  return normalized;
}

function loadLastMap() {
  try { const value = localStorage.getItem(lastMapKey); return value ? normalizeMap(JSON.parse(value)) : null; }
  catch { return null; }
}

function loadLibrary() {
  try { return JSON.parse(localStorage.getItem(storageKey) || "{}"); }
  catch { return {}; }
}

function persistDraft() {
  localStorage.setItem(lastMapKey, JSON.stringify(mapData));
}

function markDirty(message = "有未保存修改") {
  dirty = true;
  persistDraft();
  $("#document-state").textContent = message;
}

function markSaved(message = "已保存") {
  dirty = false;
  $("#document-state").textContent = message;
}

function pushHistory() {
  undoStack.push(clone(mapData));
  if (undoStack.length > 80) undoStack.shift();
  redoStack = [];
  updateHistoryButtons();
}

function undo() {
  if (!undoStack.length) return;
  redoStack.push(clone(mapData));
  mapData = undoStack.pop();
  selectedCell = selectedCell && inBounds(selectedCell) ? selectedCell : null;
  markDirty("撤销后未保存");
  renderAll();
}

function redo() {
  if (!redoStack.length) return;
  undoStack.push(clone(mapData));
  mapData = redoStack.pop();
  markDirty("重做后未保存");
  renderAll();
}

function updateHistoryButtons() {
  $("#undo").disabled = undoStack.length === 0;
  $("#redo").disabled = redoStack.length === 0;
}

function renderPalette() {
  const groups = [...new Set(tokens.map(token => token.group))];
  $("#palette-groups").innerHTML = groups.map(group => `
    <section class="palette-group"><h3>${escapeHtml(group)}</h3><div class="palette-grid">
      ${tokens.filter(token => token.group === group).map(token => `
        <button class="palette-item ${activeTool === token.id ? "active" : ""}" data-token="${token.id}" title="${escapeHtml(token.label)}">
          <span class="palette-glyph" style="--token-color:${token.color}">${token.glyph}</span><span>${escapeHtml(token.label)}</span>
        </button>`).join("")}
    </div></section>`).join("");
  $$(".palette-item").forEach(button => button.addEventListener("click", () => setTool(button.dataset.token)));
}

function setTool(tool) {
  activeTool = tool;
  $$(".tool-button").forEach(button => button.classList.toggle("active", button.dataset.tool === tool));
  $$(".palette-item").forEach(button => button.classList.toggle("active", button.dataset.token === tool));
  const token = tokenById[tool];
  $("#active-tool-name").textContent = token?.label || (tool === "erase" ? "擦除" : "选择");
  $("#active-layer-label").textContent = token ? ({ units: "单位", objects: "物件", effects: "效果", markers: "标记" }[token.layer]) : (tool === "erase" ? "擦除" : "选择");
  $("#active-tool-swatch").style.background = token?.color || (tool === "erase" ? "#ef806f" : "#e2b85b");
}

function renderGrid() {
  const grid = $("#map-grid");
  grid.style.gridTemplateColumns = `repeat(${mapData.width}, var(--cell-size))`;
  grid.style.gridTemplateRows = `repeat(${mapData.height}, var(--cell-size))`;
  const cells = [];
  for (let y = mapData.height - 1; y >= 0; y--) {
    for (let x = 0; x < mapData.width; x++) cells.push(cellMarkup(x, y));
  }
  grid.innerHTML = cells.join("");
  $$(".grid-cell").forEach(cell => {
    cell.addEventListener("pointerdown", onCellPointerDown);
    cell.addEventListener("pointerenter", onCellPointerEnter);
    cell.addEventListener("contextmenu", event => { event.preventDefault(); beginStroke(); eraseAt(+cell.dataset.x, +cell.dataset.y, event.ctrlKey); endStroke(); });
    cell.addEventListener("mouseenter", () => $("#cursor-position").textContent = `坐标 (${cell.dataset.x}, ${cell.dataset.y})`);
  });
  renderCoordinates();
}

function cellMarkup(x, y) {
  const terrain = mapData.terrain.filter(item => item.x === x && item.y === y);
  const effect = terrain.find(item => effectKinds.has(item.kind));
  const object = terrain.find(item => objectKinds.has(item.kind));
  const enemy = mapData.enemies.find(item => item.x === x && item.y === y);
  const isHero = posEqual(mapData.heroSpawn, { x, y });
  const isAnchor = mapData.spaceContract.routeAnchors.some(item => item.x === x && item.y === y);
  const isBlocked = mapData.blockedPositions.some(item => item.x === x && item.y === y);
  const selected = posEqual(selectedCell, { x, y });
  const layers = [];
  if (visibleLayers.effects && effect) layers.push(layerMarkup(effect.kind, "effect"));
  if (visibleLayers.objects && object) layers.push(layerMarkup(object.kind, "object", object.mechanismKind));
  if (visibleLayers.markers && isBlocked) layers.push(`<span class="cell-layer cell-blocked" title="禁用格"></span>`);
  if (visibleLayers.units && isHero) layers.push(layerMarkup("hero", "unit"));
  if (visibleLayers.units && enemy) layers.push(layerMarkup("enemy", "unit", 0, enemyName[enemy.archetypeId] || enemy.archetypeId));
  if (visibleLayers.markers && isAnchor) layers.push(layerMarkup("routeAnchor", "marker"));
  return `<div class="grid-cell ${selected ? "selected" : ""}" role="gridcell" aria-label="坐标 ${x},${y}" data-x="${x}" data-y="${y}">${layers.join("")}</div>`;
}

function layerMarkup(kind, type, mechanismKind = 0, overrideLabel = "") {
  const token = tokenById[kind];
  if (!token) return "";
  let glyph = token.glyph;
  if (kind === "TowerMechanism" && mechanismKind) glyph = ["", "障", "显", "冲"][mechanismKind] || "机";
  if (kind === "enemy" && overrideLabel) glyph = overrideLabel.slice(0, 1);
  return `<span class="cell-layer cell-${type}" style="--token-color:${token.color}" title="${escapeHtml(overrideLabel || token.label)}">${escapeHtml(glyph)}</span>`;
}

function renderCoordinates() {
  const stage = $("#grid-stage");
  stage.querySelectorAll(".coord-x,.coord-y").forEach(element => element.remove());
  const size = Number(getComputedStyle(document.documentElement).getPropertyValue("--cell-size").replace("px", "")) || 52;
  for (let x = 0; x < mapData.width; x++) {
    const label = document.createElement("span"); label.className = "coord-x"; label.textContent = x;
    label.style.left = `${26 + x * size}px`; label.style.width = `${size}px`; label.style.textAlign = "center"; stage.append(label);
  }
  for (let y = mapData.height - 1; y >= 0; y--) {
    const label = document.createElement("span"); label.className = "coord-y"; label.textContent = y;
    label.style.top = `${26 + (mapData.height - 1 - y) * size}px`; label.style.height = `${size}px`; label.style.display = "grid"; label.style.placeItems = "center"; stage.append(label);
  }
}

function beginStroke() {
  if (!pointerPainting) { pushHistory(); pointerPainting = true; strokeChanged = false; }
}

function endStroke() {
  if (!pointerPainting) return;
  pointerPainting = false;
  if (!strokeChanged) undoStack.pop();
  updateHistoryButtons();
}

function onCellPointerDown(event) {
  event.preventDefault();
  const x = +event.currentTarget.dataset.x, y = +event.currentTarget.dataset.y;
  selectedCell = { x, y };
  if (event.button === 2) return;
  if (activeTool === "select") { renderGrid(); renderInspector(); return; }
  beginStroke();
  if (event.ctrlKey) clearCell(x, y); else applyTool(x, y);
}

function onCellPointerEnter(event) {
  if (!pointerPainting || !(event.buttons & 1)) return;
  const x = +event.currentTarget.dataset.x, y = +event.currentTarget.dataset.y;
  selectedCell = { x, y };
  applyTool(x, y);
}

document.addEventListener("pointerup", endStroke);

function applyTool(x, y) {
  if (activeTool === "erase") return eraseAt(x, y, false);
  const token = tokenById[activeTool];
  if (!token) return;
  let changed = false;
  if (activeTool === "hero") {
    if (!posEqual(mapData.heroSpawn, { x, y })) { mapData.heroSpawn = { x, y }; changed = true; }
  } else if (activeTool === "enemy") {
    const existing = mapData.enemies.find(item => item.x === x && item.y === y);
    if (!existing || existing.archetypeId !== selectedEnemy) {
      mapData.enemies = mapData.enemies.filter(item => item.x !== x || item.y !== y);
      mapData.enemies.push({ archetypeId: selectedEnemy, x, y }); changed = true;
    }
  } else if (activeTool === "routeAnchor") {
    if (!mapData.spaceContract.routeAnchors.some(item => item.x === x && item.y === y)) {
      mapData.spaceContract.routeAnchors.push({ x, y }); changed = true;
    }
  } else if (activeTool === "blocked") {
    if (!mapData.blockedPositions.some(item => item.x === x && item.y === y)) {
      mapData.blockedPositions.push({ x, y }); changed = true;
    }
  } else {
    const sameLayer = token.layer === "objects" ? objectKinds : effectKinds;
    const existing = mapData.terrain.find(item => item.x === x && item.y === y && sameLayer.has(item.kind));
    if (!existing || existing.kind !== activeTool) {
      mapData.terrain = mapData.terrain.filter(item => item.x !== x || item.y !== y || !sameLayer.has(item.kind));
      mapData.terrain.push({ x, y, kind: activeTool, mechanismKind: activeTool === "TowerMechanism" ? 1 : 0 });
      changed = true;
    }
  }
  if (changed) afterPaint(`${token.label} · (${x}, ${y})`);
}

function eraseAt(x, y, all) {
  const before = JSON.stringify(mapData);
  if (all) return clearCell(x, y);
  const enemyIndex = mapData.enemies.findIndex(item => item.x === x && item.y === y);
  const objectIndex = mapData.terrain.findIndex(item => item.x === x && item.y === y && objectKinds.has(item.kind));
  const effectIndex = mapData.terrain.findIndex(item => item.x === x && item.y === y && effectKinds.has(item.kind));
  const anchorIndex = mapData.spaceContract.routeAnchors.findIndex(item => item.x === x && item.y === y);
  const blockedIndex = mapData.blockedPositions.findIndex(item => item.x === x && item.y === y);
  if (enemyIndex >= 0) mapData.enemies.splice(enemyIndex, 1);
  else if (posEqual(mapData.heroSpawn, { x, y })) mapData.heroSpawn = null;
  else if (objectIndex >= 0) mapData.terrain.splice(objectIndex, 1);
  else if (effectIndex >= 0) mapData.terrain.splice(effectIndex, 1);
  else if (anchorIndex >= 0) mapData.spaceContract.routeAnchors.splice(anchorIndex, 1);
  else if (blockedIndex >= 0) mapData.blockedPositions.splice(blockedIndex, 1);
  if (before !== JSON.stringify(mapData)) afterPaint(`已擦除 (${x}, ${y})`);
}

function clearCell(x, y) {
  const before = JSON.stringify(mapData);
  mapData.enemies = mapData.enemies.filter(item => item.x !== x || item.y !== y);
  mapData.terrain = mapData.terrain.filter(item => item.x !== x || item.y !== y);
  mapData.blockedPositions = mapData.blockedPositions.filter(item => item.x !== x || item.y !== y);
  mapData.spaceContract.routeAnchors = mapData.spaceContract.routeAnchors.filter(item => item.x !== x || item.y !== y);
  if (posEqual(mapData.heroSpawn, { x, y })) mapData.heroSpawn = null;
  if (before !== JSON.stringify(mapData)) afterPaint(`已清空 (${x}, ${y})`);
}

function afterPaint(message) {
  strokeChanged = true;
  markDirty();
  $("#status-message").textContent = message;
  renderGrid(); renderInspector(); renderStats();
}

function renderInspector() {
  renderCellInspector(); renderMapInspector(); renderValidation();
}

function renderCellInspector() {
  const container = $("#tab-cell");
  if (!selectedCell) {
    container.innerHTML = `<div class="empty-inspector"><div><b>选择一个格子</b><span>查看并编辑该格的单位、物件、效果和标记。</span></div></div>`;
    return;
  }
  const { x, y } = selectedCell;
  const terrain = mapData.terrain.filter(item => item.x === x && item.y === y);
  const enemy = mapData.enemies.find(item => item.x === x && item.y === y);
  const items = [];
  if (posEqual(mapData.heroSpawn, selectedCell)) items.push(propertyCard("hero", "主角出生点", "unit-hero"));
  if (enemy) items.push(propertyCard("enemy", enemyName[enemy.archetypeId] || enemy.archetypeId, "unit-enemy"));
  terrain.forEach(item => items.push(propertyCard(item.kind, tokenById[item.kind]?.label || item.kind, `terrain-${item.kind}`)));
  if (mapData.spaceContract.routeAnchors.some(item => posEqual(item, selectedCell))) items.push(propertyCard("routeAnchor", "路线锚点", "marker-route"));
  if (mapData.blockedPositions.some(item => posEqual(item, selectedCell))) items.push(propertyCard("blocked", "禁用格", "marker-blocked"));
  container.innerHTML = `
    <h2 class="inspector-title">格子 (${x}, ${y})</h2><p class="inspector-subtitle">右键格子可按最上层依次擦除</p>
    <div class="coordinate-card"><div><span>X 坐标</span><b>${x}</b></div><div><span>Y 坐标</span><b>${y}</b></div></div>
    <section class="inspector-section"><h3>格内内容</h3><div class="stack">${items.join("") || `<div class="issue ok">这是一个空格，可以从左侧选择素材绘制。</div>`}</div></section>
    ${enemy ? `<section class="inspector-section"><label class="field"><span>敌人类型</span><select id="inspector-enemy">${enemyCatalog.map(item => `<option value="${item.id}" ${item.id === enemy.archetypeId ? "selected" : ""}>${escapeHtml(item.name)} · ${item.id}</option>`).join("")}</select></label></section>` : ""}
    ${terrain.some(item => item.kind === "TowerMechanism") ? `<section class="inspector-section"><label class="field"><span>机关类型</span><select id="mechanism-kind"><option value="1">1 · 护障维护</option><option value="2">2 · 显影巡查</option><option value="3">3 · 冲压隔离</option></select></label></section>` : ""}
    <section class="inspector-section"><button id="clear-cell" class="danger-button">清空这个格子</button></section>`;
  container.querySelectorAll("[data-remove]").forEach(button => button.addEventListener("click", () => removeSelectedItem(button.dataset.remove)));
  $("#clear-cell")?.addEventListener("click", () => { pushHistory(); clearCell(x, y); });
  $("#inspector-enemy")?.addEventListener("change", event => {
    pushHistory(); enemy.archetypeId = event.target.value; markDirty(); renderAll();
  });
  const mechanism = terrain.find(item => item.kind === "TowerMechanism");
  if (mechanism && $("#mechanism-kind")) {
    $("#mechanism-kind").value = String(mechanism.mechanismKind || 1);
    $("#mechanism-kind").addEventListener("change", event => { pushHistory(); mechanism.mechanismKind = +event.target.value; markDirty(); renderAll(); });
  }
}

function propertyCard(kind, label, removeKey) {
  const token = tokenById[kind] || { glyph: "?", color: "#778899" };
  return `<article class="property-card" style="--token-color:${token.color}"><span class="symbol">${token.glyph}</span><div><strong>${escapeHtml(label)}</strong><small>${escapeHtml(kind)}</small></div><button class="delete-button" data-remove="${removeKey}" title="移除">×</button></article>`;
}

function removeSelectedItem(key) {
  if (!selectedCell) return;
  pushHistory(); const { x, y } = selectedCell;
  if (key === "unit-hero") mapData.heroSpawn = null;
  else if (key === "unit-enemy") mapData.enemies = mapData.enemies.filter(item => item.x !== x || item.y !== y);
  else if (key === "marker-route") mapData.spaceContract.routeAnchors = mapData.spaceContract.routeAnchors.filter(item => item.x !== x || item.y !== y);
  else if (key === "marker-blocked") mapData.blockedPositions = mapData.blockedPositions.filter(item => item.x !== x || item.y !== y);
  else if (key.startsWith("terrain-")) { const kind = key.slice(8); mapData.terrain = mapData.terrain.filter(item => item.x !== x || item.y !== y || item.kind !== kind); }
  markDirty(); renderAll();
}

function renderMapInspector() {
  const sc = mapData.spaceContract;
  $("#tab-map").innerHTML = `
    <h2 class="inspector-title">地图规则</h2><p class="inspector-subtitle">这些字段会随地图 JSON 一起保存</p>
    <div class="stack">
      <label class="field"><span>任务说明</span><textarea data-map-field="objectiveSummary">${escapeHtml(mapData.objectiveSummary)}</textarea></label>
      <label class="field"><span>前置地图 ID（逗号分隔）</span><input data-map-field="prerequisites" value="${escapeHtml(mapData.prerequisites.join(", "))}" /></label>
      <label class="field"><span>空间语法</span><textarea data-space-field="grammar">${escapeHtml(sc.grammar)}</textarea></label>
      <label class="field"><span>玩家可见压力</span><textarea data-space-field="publicRisk">${escapeHtml(sc.publicRisk)}</textarea></label>
      <label class="field"><span>反制窗口</span><textarea data-space-field="counterplayWindow">${escapeHtml(sc.counterplayWindow)}</textarea></label>
      <label class="field"><span>制作备注</span><textarea data-map-field="notes">${escapeHtml(mapData.notes || "")}</textarea></label>
    </div>`;
  $$("[data-map-field]").forEach(input => input.addEventListener("change", () => {
    pushHistory(); const field = input.dataset.mapField;
    mapData[field] = field === "prerequisites" ? input.value.split(",").map(value => value.trim()).filter(Boolean) : input.value;
    markDirty(); renderStats(); renderValidation();
  }));
  $$("[data-space-field]").forEach(input => input.addEventListener("change", () => {
    pushHistory(); mapData.spaceContract[input.dataset.spaceField] = input.value; markDirty(); renderStats(); renderValidation();
  }));
}

function validateMap() {
  const issues = [];
  if (!/^[a-z0-9_$-]+$/.test(mapData.id)) issues.push({ level: "error", text: "地图 ID 只能使用小写字母、数字、下划线、$ 或连字符。" });
  if (!mapData.displayName.trim()) issues.push({ level: "error", text: "地图名称不能为空。" });
  if (!mapData.heroSpawn) issues.push({ level: "error", text: "尚未放置主角出生点。" });
  else if (!inBounds(mapData.heroSpawn)) issues.push({ level: "error", text: "主角出生点超出地图边界。" });
  const positions = new Set();
  mapData.enemies.forEach(enemy => {
    const key = `${enemy.x},${enemy.y}`;
    if (!enemyCatalog.some(item => item.id === enemy.archetypeId)) issues.push({ level: "error", text: `坐标 (${key}) 使用未知敌人 ${enemy.archetypeId}。` });
    if (!inBounds(enemy)) issues.push({ level: "error", text: `敌人 ${enemy.archetypeId} 超出地图边界。` });
    if (positions.has(key)) issues.push({ level: "error", text: `坐标 (${key}) 有多个敌人重叠。` });
    positions.add(key);
    if (posEqual(enemy, mapData.heroSpawn)) issues.push({ level: "error", text: `主角和敌人在坐标 (${key}) 重叠。` });
  });
  const blockingKinds = new Set(["HeavyCover", "PermanentWall", "AetherCrystal", "TowerMechanism", "StakedStructure", "OverloadDevice", "CertifierStand", "WardGenerator"]);
  mapData.terrain.forEach(item => {
    if (!inBounds(item)) issues.push({ level: "error", text: `${item.kind} 位于地图边界外 (${item.x},${item.y})。` });
    if (blockingKinds.has(item.kind) && (posEqual(item, mapData.heroSpawn) || mapData.enemies.some(enemy => posEqual(enemy, item)))) issues.push({ level: "error", text: `阻挡物 ${item.kind} 与单位重叠于 (${item.x},${item.y})。` });
  });
  if (mapData.objectiveType === "Destruction" && !mapData.terrain.some(item => item.kind === "AetherObjective")) issues.push({ level: "error", text: "破坏目标地图必须至少放置一个任务目标。" });
  if (!mapData.enemies.length) issues.push({ level: "warning", text: "地图没有敌人；若不是纯测试场，请补充敌人。" });
  if (mapData.spaceContract.routeAnchors.length < 2) issues.push({ level: "warning", text: "建议至少设置两个路线锚点，便于核对可走路线。" });
  if (!mapData.spaceContract.grammar.trim()) issues.push({ level: "warning", text: "尚未填写空间语法。" });
  if (!mapData.spaceContract.counterplayWindow.trim()) issues.push({ level: "warning", text: "尚未填写玩家反制窗口。" });
  return issues;
}

function renderValidation() {
  const issues = validateMap();
  const errors = issues.filter(issue => issue.level === "error").length;
  const warnings = issues.length - errors;
  $("#issue-count").textContent = String(issues.length);
  $("#tab-check").innerHTML = `<h2 class="inspector-title">配置校验</h2><p class="inspector-subtitle">${errors} 个错误 · ${warnings} 个提醒</p><div class="issue-list">${issues.length ? issues.map(issue => `<div class="issue ${issue.level === "warning" ? "warning" : ""}">${escapeHtml(issue.text)}</div>`).join("") : `<div class="issue ok">地图结构完整，可以保存和导出。</div>`}</div>`;
  $("#stat-validation").textContent = errors ? `${errors} 个错误` : warnings ? `${warnings} 个提醒` : "校验通过";
  $("#stat-validation").style.color = errors ? "var(--danger)" : warnings ? "var(--gold)" : "var(--success)";
}

function syncMetaInputs() {
  const pairs = [["#map-name", mapData.displayName], ["#map-id", mapData.id], ["#map-width", mapData.width], ["#map-height", mapData.height], ["#floor-theme", mapData.floorTheme], ["#objective-type", mapData.objectiveType], ["#map-tier", mapData.tier]];
  pairs.forEach(([selector, value]) => { if (document.activeElement !== $(selector)) $(selector).value = value; });
  $("#map-elite").checked = !!mapData.isElite; $("#map-boss").checked = !!mapData.isBoss;
}

function renderStats() {
  $("#stat-size").textContent = `${mapData.width}×${mapData.height}`;
  $("#stat-terrain").textContent = `${mapData.terrain.length} 个地形`;
  $("#stat-enemies").textContent = `${mapData.enemies.length} 个敌人`;
}

function renderLibrary() {
  const library = loadLibrary();
  const select = $("#map-library");
  const workspaceOptions = workspaceMaps.map(map => `<option value="workspace:${escapeHtml(map.id)}">项目 · ${escapeHtml(map.displayName)} · ${escapeHtml(map.id)}</option>`).join("");
  const localOptions = Object.values(library).filter(map => !workspaceMaps.some(item => item.id === map.id)).sort((a, b) => a.displayName.localeCompare(b.displayName, "zh-CN")).map(map => `<option value="local:${escapeHtml(map.id)}">草稿 · ${escapeHtml(map.displayName)} · ${escapeHtml(map.id)}</option>`).join("");
  select.innerHTML = `<option value="">选择已保存地图…</option>${workspaceOptions}${localOptions}`;
}

async function refreshWorkspaceMaps() {
  try {
    const response = await fetch("/api/maps", { cache: "no-store" });
    if (!response.ok) throw new Error("API unavailable");
    workspaceMaps = await response.json(); renderLibrary();
  } catch { workspaceMaps = []; renderLibrary(); }
}

function renderAll() {
  syncMetaInputs(); renderPalette(); renderGrid(); renderInspector(); renderStats(); renderLibrary(); updateHistoryButtons();
}

function resizeMap(width, height) {
  width = Math.max(4, Math.min(32, Number(width) || mapData.width));
  height = Math.max(4, Math.min(24, Number(height) || mapData.height));
  if (width === mapData.width && height === mapData.height) return;
  pushHistory(); mapData.width = width; mapData.height = height;
  const inside = item => item.x >= 0 && item.y >= 0 && item.x < width && item.y < height;
  mapData.terrain = mapData.terrain.filter(inside); mapData.enemies = mapData.enemies.filter(inside);
  mapData.blockedPositions = mapData.blockedPositions.filter(inside); mapData.spaceContract.routeAnchors = mapData.spaceContract.routeAnchors.filter(inside);
  if (mapData.heroSpawn && !inside(mapData.heroSpawn)) mapData.heroSpawn = null;
  selectedCell = selectedCell && inside(selectedCell) ? selectedCell : null;
  markDirty("尺寸已修改"); renderAll(); toast(`地图尺寸已调整为 ${width}×${height}`, "success");
}

function saveToLibrary(showToast = true) {
  if (!mapData.id.trim()) return toast("请先填写地图 ID", "error");
  const library = loadLibrary(); mapData.updatedAt = new Date().toISOString(); library[mapData.id] = clone(mapData);
  localStorage.setItem(storageKey, JSON.stringify(library)); persistDraft(); markSaved("已存入本机库"); renderLibrary();
  if (showToast) toast("已保存到本机地图库", "success");
}

async function openMap() {
  try {
    let file;
    if (window.showOpenFilePicker) {
      [fileHandle] = await window.showOpenFilePicker({ types: [{ description: "OCC 地图 JSON", accept: { "application/json": [".json"] } }], multiple: false });
      file = await fileHandle.getFile();
    } else { $("#file-input").click(); return; }
    await importFile(file);
  } catch (error) { if (error?.name !== "AbortError") toast(`打开失败：${error.message}`, "error"); }
}

async function importFile(file) {
  const parsed = JSON.parse(await file.text());
  if (parsed.schemaVersion !== "occ-battle-map-v1") throw new Error("不是 occ-battle-map-v1 配置文件");
  mapData = normalizeMap(parsed); undoStack = []; redoStack = []; selectedCell = null; persistDraft(); markSaved(`已打开 ${file.name}`); renderAll(); toast(`已打开 ${file.name}`, "success");
}

async function saveMap(saveAs = false) {
  mapData.updatedAt = new Date().toISOString();
  const content = JSON.stringify(mapData, null, 2) + "\n";
  try {
    if (!saveAs) {
      const response = await fetch("/api/maps", { method: "POST", headers: { "Content-Type": "application/json" }, body: content });
      if (!response.ok) throw new Error(await response.text());
      const result = await response.json(); saveToLibrary(false); markSaved("已保存到项目"); await refreshWorkspaceMaps();
      toast(`已保存地图，并更新总配置表（${result.tableRows} 张）`, "success"); return;
    }
    if (window.showSaveFilePicker) {
      fileHandle = await window.showSaveFilePicker({ suggestedName: `${mapData.id}.occ-map.json`, types: [{ description: "OCC 地图 JSON", accept: { "application/json": [".json"] } }] });
      const writable = await fileHandle.createWritable(); await writable.write(content); await writable.close();
      markSaved("已另存为文件"); toast("地图文件已另存", "success");
    } else {
      downloadBlob(content, `${mapData.id}.occ-map.json`, "application/json"); markSaved("已下载 JSON"); toast("浏览器已下载地图 JSON", "success");
    }
  } catch (error) {
    if (error?.name === "AbortError") return;
    if (!saveAs) { toast("项目直存不可用，改用另存为", "error"); return saveMap(true); }
    toast(`保存失败：${error.message}`, "error");
  }
}

function downloadBlob(content, name, type) {
  const url = URL.createObjectURL(new Blob([content], { type }));
  const anchor = document.createElement("a"); anchor.href = url; anchor.download = name; anchor.click();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}

function csvCell(value) { return `"${String(value ?? "").replaceAll('"', '""')}"`; }
function exportCsv() {
  const header = ["地图ID","名称","配置文件","宽","高","地板主题","目标类型","等级","精英","首领","主角出生","敌人编成","地形数量","空间语法","公开风险","反制窗口","启用","版本"];
  const row = [mapData.id,mapData.displayName,`${mapData.id}.occ-map.json`,mapData.width,mapData.height,mapData.floorTheme,mapData.objectiveType,mapData.tier,mapData.isElite?"是":"否",mapData.isBoss?"是":"否",mapData.heroSpawn?`${mapData.heroSpawn.x}:${mapData.heroSpawn.y}`:"",mapData.enemies.map(item=>`${item.archetypeId}@${item.x}:${item.y}`).join("|"),mapData.terrain.length,mapData.spaceContract.grammar,mapData.spaceContract.publicRisk,mapData.spaceContract.counterplayWindow,"是","1"];
  downloadBlob(`\ufeff${header.map(csvCell).join(",")}\r\n${row.map(csvCell).join(",")}\r\n`, `${mapData.id}.map-config.csv`, "text/csv;charset=utf-8");
  toast("已导出当前地图配置表", "success");
}

function toCSharp() {
  const terrain = mapData.terrain.map(item => {
    const args = `${item.x}, ${item.y}`;
    if (item.kind === "LightCover") return `L(${args})`;
    if (item.kind === "HeavyCover") return `H(${args})`;
    if (item.kind === "AetherObjective") return `O(${args})`;
    if (item.kind === "Water") return `W(${args})`;
    if (item.kind === "LampVine") return `V(${args})`;
    if (item.kind === "AetherCrystal") return `C(${args})`;
    if (item.kind === "TowerMechanism") return `M(${item.mechanismKind || 1}, ${args})`;
    return `T(${args}, LevelTerrainKind.${item.kind})`;
  }).join(",\n                    ");
  const enemies = mapData.enemies.map(item => `E("${item.archetypeId}", ${item.x}, ${item.y})`).join(", ");
  const prerequisites = mapData.prerequisites.map(id => `"${id}"`).join(", ");
  const anchors = mapData.spaceContract.routeAnchors.map(item => `new GridPosition(${item.x}, ${item.y})`).join(", ");
  const blocked = mapData.blockedPositions.map(item => `new GridPosition(${item.x}, ${item.y})`).join(", ");
  const q = value => String(value).replaceAll("\\", "\\\\").replaceAll('"', '\\"');
  return `new FirstRegionLevelDefinition("${q(mapData.id)}", "${q(mapData.displayName)}", "${q(mapData.objectiveSummary)}",\n    CombatObjectiveType.${mapData.objectiveType}, ${mapData.tier}, new GridPosition(${mapData.heroSpawn?.x ?? 0}, ${mapData.heroSpawn?.y ?? 0}),\n    FirstRegionFloorTheme.${mapData.floorTheme}, ${String(mapData.isElite).toLowerCase()}, ${String(mapData.isBoss).toLowerCase()},\n    new[] { ${prerequisites} },\n    new[] { ${enemies} },\n    new[]\n    {\n        ${terrain}\n    },\n    new LevelSpaceContract("${q(mapData.spaceContract.grammar)}",\n        new[] { ${anchors} },\n        "${q(mapData.spaceContract.publicRisk)}",\n        "${q(mapData.spaceContract.counterplayWindow)}"),\n    width: ${mapData.width}, height: ${mapData.height}${blocked ? `,\n    blockedPositions: new[] { ${blocked} }` : ""});`;
}

async function copyCSharp() {
  try { await navigator.clipboard.writeText(toCSharp()); toast("Unity 配置片段已复制", "success"); }
  catch { downloadBlob(toCSharp(), `${mapData.id}.cs.txt`, "text/plain;charset=utf-8"); toast("无法访问剪贴板，已改为下载文本", "success"); }
}

function toast(message, type = "") {
  const element = document.createElement("div"); element.className = `toast ${type}`; element.textContent = message;
  $("#toast-region").append(element); setTimeout(() => element.remove(), 3200);
}

function switchTab(name) {
  $$(".tab").forEach(tab => tab.classList.toggle("active", tab.dataset.tab === name));
  $$(".tab-content").forEach(content => content.classList.toggle("active", content.id === `tab-${name}`));
}

function bindEvents() {
  $("#enemy-archetype").innerHTML = enemyCatalog.map(enemy => `<option value="${enemy.id}">${escapeHtml(enemy.name)} · ${enemy.id}</option>`).join("");
  $("#enemy-archetype").addEventListener("change", event => { selectedEnemy = event.target.value; setTool("enemy"); });
  $$(".tool-button").forEach(button => button.addEventListener("click", () => setTool(button.dataset.tool)));
  $$("[data-meta]").forEach(input => input.addEventListener("change", () => {
    pushHistory(); const field = input.dataset.meta; mapData[field] = input.type === "checkbox" ? input.checked : input.type === "number" ? +input.value : input.value;
    markDirty(); renderGrid(); renderInspector(); renderStats();
  }));
  $("#map-width").addEventListener("change", event => resizeMap(event.target.value, mapData.height));
  $("#map-height").addEventListener("change", event => resizeMap(mapData.width, event.target.value));
  $("#new-map").addEventListener("click", () => {
    if (dirty && !confirm("当前地图有未保存修改，仍要新建吗？")) return;
    mapData = blankMap(); fileHandle = null; selectedCell = null; undoStack = []; redoStack = []; markDirty("新地图未保存"); renderAll();
  });
  $("#open-map").addEventListener("click", openMap); $("#save-map").addEventListener("click", () => saveMap(false)); $("#save-as-map").addEventListener("click", () => saveMap(true));
  $("#save-library").addEventListener("click", () => saveToLibrary(true));
  $("#map-library").addEventListener("change", async event => {
    if (!event.target.value) return;
    if (dirty && !confirm("当前地图有未保存修改，仍要切换吗？")) { event.target.value = ""; return; }
    const [source, id] = event.target.value.split(":", 2);
    try {
      const found = source === "workspace" ? await fetch(`/api/maps/${encodeURIComponent(id)}`, { cache: "no-store" }).then(response => { if (!response.ok) throw new Error("读取失败"); return response.json(); }) : loadLibrary()[id];
      if (!found) throw new Error("地图不存在");
      mapData = normalizeMap(found); fileHandle = null; selectedCell = null; undoStack = []; redoStack = []; markSaved(source === "workspace" ? "已从项目打开" : "已从本机库打开"); persistDraft(); renderAll();
    } catch (error) { toast(`打开失败：${error.message}`, "error"); }
  });
  $("#file-input").addEventListener("change", async event => { if (event.target.files[0]) try { fileHandle = null; await importFile(event.target.files[0]); } catch (error) { toast(`打开失败：${error.message}`, "error"); } event.target.value = ""; });
  $("#undo").addEventListener("click", undo); $("#redo").addEventListener("click", redo);
  $$(".tab").forEach(tab => tab.addEventListener("click", () => switchTab(tab.dataset.tab)));
  $$("[data-layer-toggle]").forEach(input => input.addEventListener("change", () => { visibleLayers[input.dataset.layerToggle] = input.checked; renderGrid(); }));
  const zoom = $("#zoom"); const setZoom = value => { value = Math.max(32, Math.min(76, Number(value))); zoom.value = value; document.documentElement.style.setProperty("--cell-size", `${value}px`); $("#zoom-value").textContent = value; renderCoordinates(); };
  zoom.addEventListener("input", () => setZoom(zoom.value)); $("#zoom-out").addEventListener("click", () => setZoom(+zoom.value - 4)); $("#zoom-in").addEventListener("click", () => setZoom(+zoom.value + 4));
  $("#canvas-scroll").addEventListener("wheel", event => { if (!event.ctrlKey) return; event.preventDefault(); setZoom(+zoom.value + (event.deltaY < 0 ? 4 : -4)); }, { passive: false });
  const menuButton = $("#export-menu-button"), menu = $("#export-menu"); menuButton.addEventListener("click", () => { menu.hidden = !menu.hidden; menuButton.setAttribute("aria-expanded", String(!menu.hidden)); });
  menu.addEventListener("click", event => { const type = event.target.dataset.export; if (!type) return; menu.hidden = true; if (type === "json") downloadBlob(JSON.stringify(mapData, null, 2) + "\n", `${mapData.id}.occ-map.json`, "application/json"); if (type === "csv") exportCsv(); if (type === "csharp") copyCSharp(); });
  document.addEventListener("click", event => { if (!event.target.closest(".menu-wrap")) menu.hidden = true; });
  document.addEventListener("keydown", event => {
    if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "s") { event.preventDefault(); saveMap(false); }
    else if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "o") { event.preventDefault(); openMap(); }
    else if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "n") { event.preventDefault(); $("#new-map").click(); }
    else if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "z") { event.preventDefault(); event.shiftKey ? redo() : undo(); }
    else if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "y") { event.preventDefault(); redo(); }
    else if (!event.ctrlKey && !event.metaKey && !["INPUT","TEXTAREA","SELECT"].includes(document.activeElement.tagName)) {
      if (event.key.toLowerCase() === "v") setTool("select"); if (event.key.toLowerCase() === "e") setTool("erase");
    }
  });
  window.addEventListener("beforeunload", event => { if (dirty) { event.preventDefault(); event.returnValue = ""; } });
}

function registerWebMcp() {
  const context = document.modelContext;
  if (!context?.registerTool) return;
  Promise.resolve(context.registerTool({
    name: "read_current_battle_map", title: "读取当前战斗地图", description: "读取编辑器里当前地图的完整 OCC 配置。",
    inputSchema: { type: "object", properties: {}, additionalProperties: false }, annotations: { readOnlyHint: true, untrustedContentHint: false },
    execute: async () => clone(mapData)
  })).catch(() => {});
  Promise.resolve(context.registerTool({
    name: "place_battle_map_item", title: "放置地图内容", description: "在当前 OCC 战斗地图指定坐标放置素材。",
    inputSchema: { type: "object", properties: { tokenId: { type: "string", enum: tokens.map(token => token.id) }, x: { type: "integer" }, y: { type: "integer" }, enemyArchetypeId: { type: "string" } }, required: ["tokenId","x","y"], additionalProperties: false },
    annotations: { readOnlyHint: false, untrustedContentHint: false },
    execute: async input => { if (!inBounds(input)) throw new Error("坐标超出地图边界"); pushHistory(); activeTool = input.tokenId; if (input.enemyArchetypeId) selectedEnemy = input.enemyArchetypeId; applyTool(input.x, input.y); renderAll(); return { ok: true, tokenId: input.tokenId, x: input.x, y: input.y }; }
  })).catch(() => {});
}

bindEvents();
renderAll();
setTool("select");
markSaved(loadLastMap() ? "已恢复上次草稿" : "内置示例");
refreshWorkspaceMaps();
registerWebMcp();
