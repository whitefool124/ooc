import { ROOM, WALK_BOUNDS, SPAWN, FURNITURE, isWalkable, movePlayer, nearestTarget, findPath } from './exploration-world.mjs';

export const EXPLORATION_BINDINGS = Object.freeze({
  left: ['KeyA', 'ArrowLeft'], right: ['KeyD', 'ArrowRight'],
  up: ['KeyW', 'ArrowUp'], down: ['KeyS', 'ArrowDown'], interact: ['KeyE', 'Enter'],
});
export const EXPLORATION_HINTS = Object.freeze({
  move: ['up', 'left', 'down', 'right'].map(action => EXPLORATION_BINDINGS[action][0].replace('Key', '')).join(''),
  interact: EXPLORATION_BINDINGS.interact[0].replace('Key', ''),
});

export function mountExploration({ canvas, presentCharacters, optionalSceneId, paused, onInteract, position = SPAWN, onPosition }) {
  const context = canvas.getContext('2d', { alpha: false });
  const prompt = document.querySelector('#explore-interact');
  const input = new Set();
  const npcPositions = [{ x: 352, y: 352 }, { x: 608, y: 352 }];
  const npcs = presentCharacters.map((person, index) => ({ ...person, ...npcPositions[index % npcPositions.length], image: `${person.id}-proxy`, action: 'scene', label: `与${person.name}交谈`, solid: { x: npcPositions[index].x - 10, y: npcPositions[index].y - 5, width: 20, height: 14 } }));
  const targets = [
    ...npcs,
    ...FURNITURE.filter(item => item.action && (item.id !== 'rest' || optionalSceneId)).map(item => ({ ...item, sceneId: item.id === 'rest' ? optionalSceneId : null })),
    { id: 'exit', x: 480, y: 580, action: 'depart', label: '从南门出发' },
  ];
  const obstacles = [...FURNITURE.map(item => item.solid), ...npcs.map(npc => npc.solid)];
  let player = isWalkable(position, obstacles) ? { ...position } : { ...SPAWN };
  let camera = { x: 0, y: 0 }; let zoom = 1; let size = { width: 1, height: 1 };
  let path = []; let pendingTarget = null; let currentTarget = null; let facing = 1;
  let previousTime = 0; let animationFrame = 0; let alive = true; let ready = false; let moving = false;
  const images = {};
  const assetNames = ['hero', 'lise-proxy', 'nata-proxy', 'veil-proxy', 'cassian-proxy', 'floor', 'desk', 'tools', 'cabinet', 'map-table', 'books', 'planter', 'crate', 'wall'];
  const loaded = Promise.all(assetNames.map(name => new Promise(resolve => {
    const image = new Image(); image.onload = () => { images[name] = image; resolve(); }; image.onerror = () => resolve(); image.src = new URL(`../assets/exploration/${name}.png`, import.meta.url).href;
  }))).then(() => { if (alive) { ready = true; canvas.dataset.ready = 'true'; } });

  function resize() {
    const bounds = canvas.getBoundingClientRect();
    size = { width: Math.max(1, Math.round(bounds.width)), height: Math.max(1, Math.round(bounds.height)) };
    canvas.width = size.width; canvas.height = size.height;
    zoom = size.width >= 1450 ? 2 : 1;
    context.imageSmoothingEnabled = false;
  }
  const observer = new ResizeObserver(resize); observer.observe(canvas); resize();

  function isHeld(action) { return EXPLORATION_BINDINGS[action].some(code => input.has(code)); }
  function clearInput() { input.clear(); }
  function interact() { if (paused() || !currentTarget || !ready) return; clearInput(); path = []; pendingTarget = null; onInteract(currentTarget); }
  function keydown(event) {
    if (paused()) return;
    const action = Object.entries(EXPLORATION_BINDINGS).find(([, codes]) => codes.includes(event.code))?.[0];
    if (!action) return;
    event.preventDefault();
    if (action === 'interact') { if (!event.repeat) interact(); return; }
    input.add(event.code); path = []; pendingTarget = null;
  }
  function keyup(event) { input.delete(event.code); }
  function pointerdown(event) {
    if (paused() || !ready || event.button !== 0) return;
    canvas.focus({ preventScroll: true });
    const rect = canvas.getBoundingClientRect();
    const destination = { x: camera.x + (event.clientX - rect.left) / zoom, y: camera.y + (event.clientY - rect.top) / zoom };
    const clicked = npcs.find(npc => Math.abs(destination.x - npc.x) <= 20 && destination.y >= npc.y - 64 && destination.y <= npc.y + 6) || nearestTarget(destination, targets, 25);
    pendingTarget = clicked;
    if (clicked) {
      if (Math.hypot(player.x - clicked.x, player.y - clicked.y) <= ROOM.interactionRadius) { currentTarget = clicked; interact(); return; }
      const approaches = [{ x: 0, y: 36 }, { x: -36, y: 0 }, { x: 36, y: 0 }, { x: 0, y: -36 }]
        .map(delta => ({ x: clicked.x + delta.x, y: clicked.y + delta.y })).filter(point => isWalkable(point, obstacles))
        .sort((a, b) => Math.hypot(player.x - a.x, player.y - a.y) - Math.hypot(player.x - b.x, player.y - b.y));
      path = [];
      for (const approach of approaches) { path = findPath(player, approach, obstacles); if (path.length) break; }
    } else path = findPath(player, destination, obstacles);
    if (!path.length) pendingTarget = null;
  }
  canvas.addEventListener('keydown', keydown); canvas.addEventListener('keyup', keyup);
  canvas.addEventListener('blur', clearInput); canvas.addEventListener('pointerdown', pointerdown);
  window.addEventListener('blur', clearInput);
  prompt.addEventListener('click', interact);

  function drawImage(name, x, y, flip = false, bob = 0) {
    const image = images[name]; if (!image) return;
    const left = Math.round(x - image.width / 2); const top = Math.round(y - image.height - bob);
    if (flip) { context.save(); context.translate(Math.round(x), 0); context.scale(-1, 1); context.drawImage(image, -image.width / 2, top); context.restore(); }
    else context.drawImage(image, left, top);
  }
  function label(text, x, y, active = false) {
    context.font = '12px "OCC Pixel", sans-serif'; context.textAlign = 'center';
    const width = context.measureText(text).width + 14;
    context.fillStyle = active ? '#235c62' : '#f5f1e7'; context.fillRect(Math.round(x - width / 2), Math.round(y - 16), Math.round(width), 20);
    context.fillStyle = active ? '#fffaf0' : '#302e29'; context.fillText(text, Math.round(x), Math.round(y - 1));
  }
  function draw(time) {
    context.setTransform(1, 0, 0, 1, 0, 0); context.fillStyle = '#26352e'; context.fillRect(0, 0, size.width, size.height);
    camera = {
      x: Math.round(Math.max(0, Math.min(ROOM.width - size.width / zoom, player.x - size.width / zoom / 2))),
      y: Math.round(Math.max(0, Math.min(ROOM.height - size.height / zoom, player.y - size.height / zoom * .72))),
    };
    if (size.width / zoom > ROOM.width) camera.x = Math.round((ROOM.width - size.width / zoom) / 2);
    if (size.height / zoom > ROOM.height) camera.y = Math.round((ROOM.height - size.height / zoom) / 2);
    context.setTransform(zoom, 0, 0, zoom, -camera.x * zoom, -camera.y * zoom);
    context.fillStyle = '#d6c9ad'; context.fillRect(32, 80, 896, 528);
    if (images.floor) for (let y = 96; y < 608; y += 32) for (let x = 32; x < 928; x += 32) context.drawImage(images.floor, x, y);
    context.fillStyle = '#b4a58a'; context.fillRect(48, 528, 864, 4);
    if (images.wall) {
      for (let x = 32; x < 928; x += 128) context.drawImage(images.wall, x, 80);
      for (let x = 32; x < 928; x += 128) if (x < 416 || x > 512) context.drawImage(images.wall, x, 592);
      for (let y = 112; y < 592; y += 128) {
        context.save(); context.translate(32, y); context.rotate(Math.PI / 2); context.drawImage(images.wall, 0, -16); context.restore();
        context.save(); context.translate(928, y); context.rotate(Math.PI / 2); context.drawImage(images.wall, 0, -16); context.restore();
      }
    }
    label('外环维护组', 480, 142);
    label('南门', 480, 614, currentTarget?.id === 'exit');
    const objects = [...FURNITURE.map(item => ({ ...item, type: 'object' })), ...npcs.map(item => ({ ...item, type: 'npc' })), { id: 'player', type: 'player', x: player.x, y: player.y }].sort((a, b) => a.y - b.y);
    for (const object of objects) {
      if (object.type === 'player' || object.type === 'npc') {
        context.fillStyle = object.type === 'player' ? '#235c6255' : '#302e2930';
        context.beginPath(); context.ellipse(Math.round(object.x), Math.round(object.y - 1), 11, 4, 0, 0, Math.PI * 2); context.fill();
      }
      if (object.type === 'player') drawImage('hero', object.x, object.y, facing < 0, moving ? Math.sin(time / 75) * 1.2 : 0);
      else drawImage(object.image, object.x, object.y);
    }
    for (const npc of npcs) label(npc.name, npc.x, npc.y - 63, currentTarget?.id === npc.id);
    label('维克多', player.x, player.y - 63, true);
    if (currentTarget && !npcs.some(npc => npc.id === currentTarget.id) && currentTarget.id !== 'exit') label(currentTarget.label, currentTarget.x, currentTarget.y - 38, true);
    if (path.length) {
      const goal = path[path.length - 1]; context.strokeStyle = '#235c62'; context.lineWidth = 1;
      context.beginPath(); context.ellipse(goal.x, goal.y, 7, 3, 0, 0, Math.PI * 2); context.stroke();
    }
    canvas.dataset.playerX = player.x.toFixed(1); canvas.dataset.playerY = player.y.toFixed(1);
    canvas.dataset.target = currentTarget?.id || '';
    canvas.dataset.cameraX = camera.x; canvas.dataset.cameraY = camera.y; canvas.dataset.zoom = zoom;
    canvas.dataset.moving = String(moving);
    canvas.dataset.characters = npcs.map(npc => npc.name).join(',');
  }
  function tick(time) {
    if (!alive) return;
    const seconds = previousTime ? Math.min((time - previousTime) / 1000, .05) : 0; previousTime = time;
    if (paused() || document.hidden) { clearInput(); moving = false; }
    else {
      let axis = { x: Number(isHeld('right')) - Number(isHeld('left')), y: Number(isHeld('down')) - Number(isHeld('up')) };
      if (!axis.x && !axis.y && path.length) {
        const goal = path[0]; const distance = Math.hypot(goal.x - player.x, goal.y - player.y);
        if (distance < 3) path.shift();
        else axis = { x: (goal.x - player.x) / Math.max(distance, 1), y: (goal.y - player.y) / Math.max(distance, 1) };
      }
      const next = movePlayer(player, axis, seconds, obstacles);
      moving = Math.hypot(next.x - player.x, next.y - player.y) > .01;
      if (axis.x) facing = Math.sign(axis.x);
      player = next; onPosition({ ...player });
    }
    currentTarget = nearestTarget(player, targets);
    prompt.disabled = !currentTarget || paused() || !ready;
    const promptText = currentTarget ? `${EXPLORATION_HINTS.interact} ${currentTarget.label}` : '靠近人物或物件';
    if (prompt.textContent !== promptText) prompt.textContent = promptText;
    if (pendingTarget && currentTarget?.id === pendingTarget.id && !path.length && !paused()) interact();
    if (ready) draw(time);
    else { context.fillStyle = '#e9e4d7'; context.fillRect(0, 0, size.width, size.height); }
    animationFrame = requestAnimationFrame(tick);
  }
  animationFrame = requestAnimationFrame(tick);
  return {
    ready: loaded,
    focus() { canvas.focus({ preventScroll: true }); },
    destroy() { alive = false; cancelAnimationFrame(animationFrame); observer.disconnect(); canvas.removeEventListener('keydown', keydown); canvas.removeEventListener('keyup', keyup); canvas.removeEventListener('blur', clearInput); canvas.removeEventListener('pointerdown', pointerdown); window.removeEventListener('blur', clearInput); prompt.removeEventListener('click', interact); },
  };
}
