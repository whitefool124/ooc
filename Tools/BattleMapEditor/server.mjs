import http from "node:http";
import { mkdir, readFile, readdir, stat, writeFile } from "node:fs/promises";
import { extname, join, normalize, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const root = fileURLToPath(new URL(".", import.meta.url));
const repositoryRoot = resolve(root, "../..");
const mapDirectory = join(repositoryRoot, "Worldbuilding", "地图配置", "战斗地图");
const mapTablePath = join(repositoryRoot, "Worldbuilding", "数据表", "OCC_战斗地图配置表_v1.0.csv");
const port = Number(process.argv[2] || 4178);
const mime = {
  ".html": "text/html; charset=utf-8",
  ".js": "text/javascript; charset=utf-8",
  ".css": "text/css; charset=utf-8",
  ".json": "application/json; charset=utf-8",
  ".csv": "text/csv; charset=utf-8",
  ".svg": "image/svg+xml"
};

const csvCell = value => `"${String(value ?? "").replaceAll('"', '""')}"`;
const summary = map => ({ id: map.id, displayName: map.displayName, width: map.width, height: map.height, updatedAt: map.updatedAt || "" });

async function readMaps() {
  await mkdir(mapDirectory, { recursive: true });
  const names = (await readdir(mapDirectory)).filter(name => name.endsWith(".occ-map.json")).sort();
  const maps = [];
  for (const name of names) {
    try { maps.push(JSON.parse(await readFile(join(mapDirectory, name), "utf8"))); }
    catch { /* An invalid file remains visible to source control but is omitted from the editor library. */ }
  }
  return maps;
}

async function readBody(request) {
  const chunks = []; let size = 0;
  for await (const chunk of request) {
    size += chunk.length; if (size > 1024 * 1024) throw new Error("Request too large"); chunks.push(chunk);
  }
  return JSON.parse(Buffer.concat(chunks).toString("utf8"));
}

function validateMap(map) {
  if (!map || map.schemaVersion !== "occ-battle-map-v1") throw new Error("Invalid schemaVersion");
  if (!/^[a-z0-9_$-]+$/.test(map.id || "")) throw new Error("Invalid map id");
  if (!Number.isInteger(map.width) || !Number.isInteger(map.height) || map.width < 4 || map.width > 32 || map.height < 4 || map.height > 24) throw new Error("Invalid map size");
  if (!Array.isArray(map.enemies) || !Array.isArray(map.terrain) || !Array.isArray(map.blockedPositions)) throw new Error("Invalid map collections");
}

async function rebuildMapTable(maps) {
  const header = ["地图ID","名称","配置文件","宽","高","地板主题","目标类型","等级","精英","首领","主角出生","敌人编成","地形数量","空间语法","公开风险","反制窗口","启用","版本"];
  const rows = maps.sort((a, b) => a.id.localeCompare(b.id)).map(map => [
    map.id, map.displayName, `Worldbuilding/地图配置/战斗地图/${map.id}.occ-map.json`, map.width, map.height,
    map.floorTheme, map.objectiveType, map.tier, map.isElite ? "是" : "否", map.isBoss ? "是" : "否",
    map.heroSpawn ? `${map.heroSpawn.x}:${map.heroSpawn.y}` : "",
    map.enemies.map(item => `${item.archetypeId}@${item.x}:${item.y}`).join("|"), map.terrain.length,
    map.spaceContract?.grammar || "", map.spaceContract?.publicRisk || "", map.spaceContract?.counterplayWindow || "", "是", "1"
  ]);
  const csv = `\ufeff${[header, ...rows].map(row => row.map(csvCell).join(",")).join("\r\n")}\r\n`;
  await writeFile(mapTablePath, csv, "utf8");
}

async function handleApi(request, response, url) {
  if (request.method === "GET" && url.pathname === "/api/maps") {
    const maps = await readMaps();
    response.writeHead(200, { "Content-Type": "application/json; charset=utf-8", "Cache-Control": "no-store" });
    response.end(JSON.stringify(maps.map(summary))); return true;
  }
  const match = url.pathname.match(/^\/api\/maps\/([a-z0-9_$-]+)$/);
  if (request.method === "GET" && match) {
    const path = join(mapDirectory, `${match[1]}.occ-map.json`);
    response.writeHead(200, { "Content-Type": "application/json; charset=utf-8", "Cache-Control": "no-store" });
    response.end(await readFile(path)); return true;
  }
  if (request.method === "POST" && url.pathname === "/api/maps") {
    const map = await readBody(request); validateMap(map); map.updatedAt = new Date().toISOString();
    await mkdir(mapDirectory, { recursive: true });
    await writeFile(join(mapDirectory, `${map.id}.occ-map.json`), JSON.stringify(map, null, 2) + "\n", "utf8");
    const maps = await readMaps(); await rebuildMapTable(maps);
    response.writeHead(200, { "Content-Type": "application/json; charset=utf-8" });
    response.end(JSON.stringify({ ok: true, map: summary(map), tableRows: maps.length })); return true;
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
  } catch {
    response.writeHead(404, { "Content-Type": "text/plain; charset=utf-8" });
    response.end("Not found");
  }
});

server.listen(port, "127.0.0.1", () => console.log(`OCC Battle Map Editor: http://127.0.0.1:${port}`));
