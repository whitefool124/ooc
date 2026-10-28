import test from 'node:test';
import assert from 'node:assert/strict';
import { ROOM, SPAWN, FURNITURE, WALK_BOUNDS, isWalkable, movePlayer, nearestTarget, findPath } from '../src/exploration-world.mjs';

const obstacles = FURNITURE.map(item => item.solid);

test('spawn and all public interaction approaches are walkable', () => {
  assert.equal(isWalkable(SPAWN, obstacles), true);
  for (const item of FURNITURE.filter(item => item.action)) assert.equal(isWalkable({ x: item.x, y: item.y + 36 }, obstacles), true, item.id);
});

test('held movement uses the same speed on diagonals and straight lines', () => {
  const straight = movePlayer(SPAWN, { x: 1, y: 0 }, .04);
  const diagonal = movePlayer(SPAWN, { x: 1, y: 1 }, .04);
  assert.ok(Math.abs(Math.hypot(straight.x - SPAWN.x, straight.y - SPAWN.y) - Math.hypot(diagonal.x - SPAWN.x, diagonal.y - SPAWN.y)) < .00001);
});

test('room bounds stop the feet and furniture blocks continuous movement', () => {
  const wallStart = { x: WALK_BOUNDS.x + ROOM.radius, y: 300 };
  assert.deepEqual(movePlayer(wallStart, { x: -1, y: 0 }, .05, obstacles), wallStart);
  const tableStart = { x: 400, y: 278 };
  let point = tableStart;
  for (let frame = 0; frame < 80; frame++) point = movePlayer(point, { x: 1, y: 0 }, .016, obstacles);
  assert.ok(point.x <= 408);
  assert.equal(isWalkable(point, obstacles), true);
});

test('large frame gaps do not teleport and invalid elapsed time does not move', () => {
  const next = movePlayer(SPAWN, { x: 1, y: 0 }, 10);
  assert.ok(next.x - SPAWN.x <= ROOM.speed * .05 + .001);
  assert.deepEqual(movePlayer(SPAWN, { x: 1, y: 0 }, Number.NaN), SPAWN);
});

test('interactions are available only near an actual target', () => {
  const targets = [{ id: 'lise', x: 352, y: 352 }];
  assert.equal(nearestTarget(SPAWN, targets), null);
  assert.equal(nearestTarget({ x: 352, y: 390 }, targets).id, 'lise');
});

test('click path goes around the shared table and every waypoint is safe', () => {
  const path = findPath({ x: 384, y: 278 }, { x: 592, y: 278 }, obstacles);
  assert.ok(path.length > 2);
  assert.ok(path.every(point => isWalkable(point, obstacles)));
  assert.ok(path.some(point => point.y <= 256 || point.y >= 304));
});

test('navigation can start off-grid next to furniture and rejects blocked destinations', () => {
  const path = findPath({ x: 406, y: 278 }, { x: 592, y: 278 }, obstacles);
  assert.ok(path.length > 0);
  assert.deepEqual(findPath(SPAWN, { x: 448, y: 278 }, obstacles), []);
  assert.deepEqual(findPath(SPAWN, { x: -100, y: -100 }, obstacles), []);
});
