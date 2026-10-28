export const ROOM = Object.freeze({ width: 960, height: 640, speed: 126, radius: 8, interactionRadius: 48 });
export const WALK_BOUNDS = Object.freeze({ x: 48, y: 112, width: 864, height: 480 });
export const SPAWN = Object.freeze({ x: 480, y: 520 });

export const FURNITURE = [
  { id: 'journal', image: 'cabinet', x: 192, y: 208, label: '交谈记录', action: 'journal', solid: { x: 160, y: 184, width: 64, height: 26 } },
  { id: 'route', image: 'map-table', x: 752, y: 208, label: '查看学院路线', action: 'depart', solid: { x: 720, y: 184, width: 64, height: 26 } },
  { id: 'tools', image: 'tools', x: 752, y: 448, label: '整理行囊', action: 'loadout', solid: { x: 720, y: 424, width: 64, height: 26 } },
  { id: 'rest', image: 'books', x: 192, y: 448, label: '休息角闲谈', action: 'optional', solid: { x: 160, y: 424, width: 64, height: 26 } },
  { id: 'desk-a', image: 'desk', x: 448, y: 288, solid: { x: 416, y: 264, width: 64, height: 26 } },
  { id: 'desk-b', image: 'desk', x: 512, y: 288, solid: { x: 480, y: 264, width: 64, height: 26 } },
  { id: 'books-a', image: 'books', x: 384, y: 176, solid: { x: 352, y: 152, width: 64, height: 26 } },
  { id: 'books-b', image: 'books', x: 576, y: 176, solid: { x: 544, y: 152, width: 64, height: 26 } },
  { id: 'plant-a', image: 'planter', x: 96, y: 208, solid: { x: 84, y: 194, width: 24, height: 16 } },
  { id: 'plant-b', image: 'planter', x: 864, y: 208, solid: { x: 852, y: 194, width: 24, height: 16 } },
  { id: 'crate', image: 'crate', x: 832, y: 448, solid: { x: 820, y: 428, width: 24, height: 22 } },
];

export function isWalkable(point, obstacles = [], radius = ROOM.radius, bounds = WALK_BOUNDS) {
  if (!Number.isFinite(point.x) || !Number.isFinite(point.y)) return false;
  if (point.x - radius < bounds.x || point.x + radius > bounds.x + bounds.width || point.y - radius < bounds.y || point.y + radius > bounds.y + bounds.height) return false;
  return !obstacles.some(rect => point.x + radius > rect.x && point.x - radius < rect.x + rect.width && point.y + radius > rect.y && point.y - radius < rect.y + rect.height);
}

export function movePlayer(position, axis, seconds, obstacles = []) {
  const magnitude = Math.hypot(axis.x, axis.y);
  if (!magnitude || !Number.isFinite(seconds) || seconds <= 0) return { ...position };
  const distance = ROOM.speed * Math.min(seconds, .05);
  const dx = axis.x / Math.max(1, magnitude) * distance;
  const dy = axis.y / Math.max(1, magnitude) * distance;
  const steps = Math.max(1, Math.ceil(Math.max(Math.abs(dx), Math.abs(dy)) / 3));
  let next = { ...position };
  for (let i = 0; i < steps; i++) {
    const horizontal = { x: next.x + dx / steps, y: next.y };
    if (isWalkable(horizontal, obstacles)) next = horizontal;
    const vertical = { x: next.x, y: next.y + dy / steps };
    if (isWalkable(vertical, obstacles)) next = vertical;
  }
  return next;
}

export function nearestTarget(position, targets, range = ROOM.interactionRadius) {
  let nearest = null; let distance = range;
  for (const target of targets) {
    const candidate = Math.hypot(position.x - target.x, position.y - target.y);
    if (candidate <= distance) { nearest = target; distance = candidate; }
  }
  return nearest;
}

// Four-way A* on a 16px navigation grid. Collision is still checked continuously
// while following its waypoints, so a click never teleports through furniture.
export function findPath(start, destination, obstacles = []) {
  if (!isWalkable(start, obstacles) || !isWalkable(destination, obstacles)) return [];
  const cell = 16;
  const key = point => `${Math.round(point.x / cell)},${Math.round(point.y / cell)}`;
  const decode = value => { const [x, y] = value.split(',').map(Number); return { x: x * cell, y: y * cell }; };
  const nearestGridPoint = point => {
    const rounded = decode(key(point)); const options = [];
    for (let dx = -1; dx <= 1; dx++) for (let dy = -1; dy <= 1; dy++) {
      const candidate = { x: rounded.x + dx * cell, y: rounded.y + dy * cell };
      const steps = Math.max(1, Math.ceil(Math.hypot(candidate.x - point.x, candidate.y - point.y) / 3));
      if (Array.from({ length: steps + 1 }, (_, index) => ({ x: point.x + (candidate.x - point.x) * index / steps, y: point.y + (candidate.y - point.y) * index / steps })).every(sample => isWalkable(sample, obstacles))) options.push(candidate);
    }
    return options.sort((a, b) => Math.hypot(a.x - point.x, a.y - point.y) - Math.hypot(b.x - point.x, b.y - point.y))[0];
  };
  const snappedStart = nearestGridPoint(start); const snappedGoal = nearestGridPoint(destination);
  if (!snappedStart || !snappedGoal) return [];
  const startKey = key(snappedStart); const goalKey = key(snappedGoal);
  const open = new Set([startKey]); const cameFrom = new Map(); const cost = new Map([[startKey, 0]]);
  const heuristic = value => { const point = decode(value); const goal = decode(goalKey); return Math.abs(point.x - goal.x) + Math.abs(point.y - goal.y); };
  for (let iteration = 0; open.size && iteration < 2600; iteration++) {
    let current = null; let best = Infinity;
    for (const candidate of open) { const score = cost.get(candidate) + heuristic(candidate); if (score < best) { current = candidate; best = score; } }
    if (current === goalKey) {
      const route = []; let cursor = current;
      while (cursor !== startKey) { route.unshift(decode(cursor)); cursor = cameFrom.get(cursor); }
      if (Math.hypot(start.x - snappedStart.x, start.y - snappedStart.y) > 1) route.unshift(snappedStart);
      route.push({ ...destination });
      return route;
    }
    open.delete(current);
    const point = decode(current);
    for (const delta of [{ x: cell, y: 0 }, { x: -cell, y: 0 }, { x: 0, y: cell }, { x: 0, y: -cell }]) {
      const next = { x: point.x + delta.x, y: point.y + delta.y };
      if (!isWalkable(next, obstacles)) continue;
      const nextKey = key(next); const nextCost = cost.get(current) + cell;
      if (nextCost >= (cost.get(nextKey) ?? Infinity)) continue;
      cameFrom.set(nextKey, current); cost.set(nextKey, nextCost); open.add(nextKey);
    }
  }
  return [];
}
