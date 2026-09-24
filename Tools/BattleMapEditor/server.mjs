import http from "node:http";
import { mkdir, readFile, stat } from "node:fs/promises";
import { extname, join, normalize, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { editorVisibleIds, mapDirectory, readMaps, rebuildAllDerivedData, validateMap, writeFileResilient } from "./map-build.mjs";

const root = fileURLToPath(new URL(".", import.meta.url));
const repositoryRoot = resolve(root, "../..");
const dataDirectory = join(repositoryRoot, "Worldbuilding", "数据表");
const port = Number(process.argv[2] || 4179);
const mime = {
  ".html": "text/html; charset=utf-8",
  ".js": "text/javascript; charset=utf-8",
  ".css": "text/css; charset=utf-8",
  ".json": "application/json; charset=utf-8",
  ".csv": "text/csv; charset=utf-8",
  ".svg": "image/svg+xml"
};

const summary = map => ({
  id: map.id, displayName: map.displayName, width: map.width, height: map.height,
  isElite: Boolean(map.isElite), isBoss: Boolean(map.isBoss), hidden: !editorVisibleIds.includes(map.id),
  totalCells: map.width * map.height, complexMechanics: Boolean(map.complexMechanics),
  changeCount: (map.changeAnnotations || []).length, updatedAt: map.updatedAt || ""
});

async function readBody(request) {
  const chunks = []; let size = 0;
  for await (const chunk of request) {
    size += chunk.length; if (size > 1024 * 1024) throw new Error("Request too large"); chunks.push(chunk);
  }
  return JSON.parse(Buffer.concat(chunks).toString("utf8"));
}

function parseCsv(text) {
  const rows = []; let row = [], cell = "", quoted = false;
  for (let index = 0; index < text.length; index++) {
    const char = text[index];
    if (quoted && char === '"' && text[index + 1] === '"') { cell += '"'; index++; }
    else if (char === '"') quoted = !quoted;
    else if (!quoted && char === ",") { row.push(cell); cell = ""; }
    else if (!quoted && (char === "\n" || char === "\r")) {
      if (char === "\r" && text[index + 1] === "\n") index++;
      row.push(cell); cell = ""; if (row.some(value => value !== "")) rows.push(row); row = [];
    } else cell += char;
  }
  if (cell || row.length) { row.push(cell); rows.push(row); }
  const headers = (rows.shift() || []).map(value => value.replace(/^\ufeff/, ""));
  return rows.map(values => Object.fromEntries(headers.map((header, index) => [header, values[index] || ""])));
}

async function readCsv(name) { return parseCsv(await readFile(join(dataDirectory, name), "utf8")); }

async function readCatalog() {
  const [elements, units, skills, enemySkills, baseSkills, passives, statuses] = await Promise.all([
    readCsv("OCC_地图元素配置表_v1.0.csv"), readCsv("OCC_单位配置表_v1.0.csv"), readCsv("OCC_技能配置表_v1.0.csv"),
    readCsv("OCC_敌人技能数据表_v1.0.csv"), readCsv("OCC_基础术式数据表_v1.0.csv"), readCsv("OCC_被动配置表_v1.0.csv"),
    readCsv("OCC_状态配置表_v1.0.csv")
  ]);
  const normalizedSkills = [
    ...skills.map(row => ({ id:row["技能ID"], name:row["名称"], type:row["类别"], cost:`AP ${row["AP"] || 0} / 魔力 ${row["魔力"] || 0}`, cooldown:row["冷却"], range:row["目标与范围"], effect:row["效果链"], rule:row["结算规则"] })),
    ...enemySkills.map(row => ({ id:row["技能ID"], name:row["技能名"], type:row["类型"], cost:row["花费"], cooldown:row["冷却"], range:row["射程"], effect:row["效果要点"], rule:[row["反应触发"],row["反应结果"]].filter(Boolean).join(" → ") })),
    ...baseSkills.map(row => ({ id:row["ID"], name:row["名称"], type:row["类别"], cost:`AP ${row["AP"] || 0} / 魔力 ${row["魔力"] || 0}`, cooldown:row["冷却_自身回合"], range:`${row["目标"]}；${row["射程"]}`, effect:row["效果"], rule:"基础固定术式" })),
    ...passives.map(row => ({ id:row["被动ID"], name:row["名称"], type:"被动", cost:"常驻", cooldown:row["上限与重置"], range:row["条件"], effect:row["效果"], rule:[row["触发"], row["备注"]].filter(Boolean).join("；") }))
  ].filter(item => item.id);
  return {
    elements: elements.map(row => ({ id:row["元素ID"], name:row["名称"], type:row["类型"], tags:row["标签"], moveCost:row["移动消耗"], blocksMovement:row["阻挡移动"], blocksLineOfSight:row["阻挡攻击线"], durability:row["耐久"], duration:row["持续与计时"], trigger:row["触发"], effect:row["效果"], after:row["变化后元素"], rule:row["边与覆盖规则"] })),
    units: units.map(row => ({ id:row["单位ID"], name:row["名称"], category:row["类别"], health:row["生命"], initialShield:row["初始护盾"], turnShield:row["回合护盾"], mana:row["魔力"], speed:row["速度"], movement:row["移动"], ap:row["AP"], footprint:row["占格"], skillIds:(row["技能ID"] || "").split("|").filter(Boolean), passiveIds:(row["被动ID"] || "").split("|").filter(id => id && id !== "无"), aiCounterplay:row["AI与反制"] })),
    skills: normalizedSkills,
    statuses: statuses.map(row => ({ id:row["状态ID"], name:row["名称"], category:row["类别"], duration:row["持续与计时"], periodic:row["周期效果"], modifier:row["属性修正"], restriction:row["动作限制"], stacking:row["叠加与移除"] }))
  };
}

async function handleApi(request, response, url) {
  if (request.method === "GET" && url.pathname === "/api/maps") {
    const maps = await readMaps();
    response.writeHead(200, { "Content-Type": "application/json; charset=utf-8", "Cache-Control": "no-store" });
    response.end(JSON.stringify(maps.map(summary))); return true;
  }
  if (request.method === "GET" && url.pathname === "/api/catalog") {
    response.writeHead(200, { "Content-Type": "application/json; charset=utf-8", "Cache-Control": "no-store" });
    response.end(JSON.stringify(await readCatalog())); return true;
  }
  const match = url.pathname.match(/^\/api\/maps\/([a-z0-9_$-]+)$/);
  if (request.method === "GET" && match) {
    const path = join(mapDirectory, `${match[1]}.occ-map.json`);
    response.writeHead(200, { "Content-Type": "application/json; charset=utf-8", "Cache-Control": "no-store" });
    response.end(await readFile(path)); return true;
  }
  if (request.method === "POST" && url.pathname === "/api/maps") {
    const map = await readBody(request); const errors = validateMap(map, { formal: false });
    if (errors.length) {
      response.writeHead(400, { "Content-Type": "application/json; charset=utf-8" });
      response.end(JSON.stringify({ ok: false, saved: false, error: errors.join("；") })); return true;
    }
    map.updatedAt = new Date().toISOString();
    await mkdir(mapDirectory, { recursive: true });
    await writeFileResilient(join(mapDirectory, `${map.id}.occ-map.json`), JSON.stringify(map, null, 2) + "\n", "utf8");
    let maps;
    try {
      maps = await rebuildAllDerivedData();
    } catch (error) {
      console.error(`[地图已保存，派生配置同步失败] ${map.id}:`, error);
      response.writeHead(202, { "Content-Type": "application/json; charset=utf-8" });
      response.end(JSON.stringify({ ok: true, saved: true, derivedUpdated: false, map: summary(map), error: error?.message || String(error) })); return true;
    }
    response.writeHead(200, { "Content-Type": "application/json; charset=utf-8" });
    response.end(JSON.stringify({ ok: true, saved: true, derivedUpdated: true, map: summary(map), tableRows: maps.length })); return true;
  }
  return false;
}

const server = http.createServer(async (request, response) => {
  try {
    const url = new URL(request.url, `http://${request.headers.host || "127.0.0.1"}`);
    if (await handleApi(request, response, url)) return;
    const relative = decodeURIComponent(url.pathname === "/" ? "/index.html" : url.pathname).replace(/^[/\\]+/, "");
    const path = normalize(join(root, relative));
    if (!path.startsWith(normalize(root))) throw new Error("Invalid path");
    const info = await stat(path);
    if (!info.isFile()) throw new Error("Not a file");
    response.writeHead(200, { "Content-Type": mime[extname(path)] || "application/octet-stream", "Cache-Control": "no-store" });
    response.end(await readFile(path));
  } catch (error) {
    const isApi = String(request.url || "").startsWith("/api/");
    if (isApi) console.error(`[地图编辑器 API 失败] ${request.method} ${request.url}:`, error);
    response.writeHead(isApi ? 500 : 404, { "Content-Type": isApi ? "application/json; charset=utf-8" : "text/plain; charset=utf-8" });
    response.end(isApi ? JSON.stringify({ ok: false, saved: false, error: error?.message || String(error) }) : "Not found");
  }
});

server.listen(port, "127.0.0.1", () => console.log(`OCC Battle Map Editor: http://127.0.0.1:${port}`));
