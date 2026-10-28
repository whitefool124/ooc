import { CHARACTERS, CHAPTERS, SCENES, QUESTS, NODES } from './content.mjs';
import { createInitialState, reduce, getDerived, validateSavedState } from './model.mjs';
import { mountExploration, EXPLORATION_HINTS } from './exploration.mjs';
import { SPAWN } from './exploration-world.mjs';

const STORAGE_KEY = new URLSearchParams(location.search).has('test') ? 'occ-story-quest-prototype-exploration-test' : 'occ-story-quest-prototype-v1';
const app = document.querySelector('#app');
const overlayRoot = document.querySelector('#overlay-root');
const announcement = document.querySelector('#announcement');
const characters = Object.fromEntries(CHARACTERS.map(character => [character.id, character]));
const escape = value => String(value ?? '').replace(/[&<>"']/g, symbol => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[symbol]));
const shortName = id => characters[id]?.name.split('·')[0] || id;
let saveMessage = '进度自动保存';
let saveFailed = false;
let state = load();
let returnFocus = null;
let exploration = null;
let roomPosition = { ...SPAWN };
const ui = { tab: 'base', quest: 'academy_thread', person: 'lise', node: null, modal: null, scene: null, line: 0, transcript: false };

const paths = {
  home: '<path d="m3 10 9-7 9 7v10H3z"/><path d="M9 20v-7h6v7"/>',
  book: '<path d="M12 5C9 3 5 3 2 4v15c4-1 7-1 10 1 3-2 6-2 10-1V4c-3-1-7-1-10 1Z"/><path d="M12 5v15"/>',
  people: '<circle cx="9" cy="7" r="3"/><path d="M3 20v-3a6 6 0 0 1 12 0v3M17 4a3 3 0 0 1 0 6m1 4a5 5 0 0 1 3 5"/>',
  arrow: '<path d="M4 12h15m-6-6 6 6-6 6"/>',
  back: '<path d="M20 12H5m6-6-6 6 6 6"/>',
  check: '<path d="m5 12 4 4L19 6"/>',
  close: '<path d="m6 6 12 12M6 18 18 6"/>',
  bag: '<path d="M6 7h12l2 14H4L6 7Z"/><path d="M9 8V5a3 3 0 0 1 6 0v3M8 13h8"/>',
  route: '<circle cx="5" cy="18" r="2"/><circle cx="19" cy="5" r="2"/><path d="M7 18h9a3 3 0 0 0 0-6H8a3 3 0 0 1 0-6h9"/>',
  talk: '<path d="M3 4h18v13H9l-6 4V4Z"/><path d="M7 9h10M7 13h6"/>',
  lock: '<rect x="5" y="10" width="14" height="11" rx="1"/><path d="M8 10V6a4 4 0 0 1 8 0v4m-4 4v3"/>',
  reset: '<path d="M4 10a8 8 0 1 1 1 8M4 4v6h6"/>',
  heart: '<path d="M12 21 3 12C-2 6 6 0 12 7 18 0 26 6 21 12l-9 9Z"/>',
  star: '<path d="m12 2 3 7 7 3-7 3-3 7-3-7-7-3 7-3 3-7Z"/>',
};
const icon = (name, className = '') => `<svg class="icon ${className}" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">${paths[name] || paths.book}</svg>`;
const button = (label, action, classes = '', extra = '') => `<button type="button" class="button ${classes}" data-action="${action}" ${extra}>${label}</button>`;
const sceneStatus = id => state.skippedScenes.includes(id) ? '已略过' : state.seenScenes.includes(id) ? '已读' : '未读';
const chapter = () => CHAPTERS[state.phase];
const node = () => NODES.find(item => item.id === (ui.node || getDerived(state).activeNodeId || getDerived(state).nextNodeId)) || NODES[0];

function load() {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return createInitialState();
    const parsed = JSON.parse(raw);
    if (validateSavedState(parsed)) return parsed;
    localStorage.setItem(`${STORAGE_KEY}-unreadable`, raw);
    saveMessage = '旧演示记录无法读取，已保留副本并开启新演示';
  } catch {
    saveFailed = true;
    saveMessage = '演示记录无法读取，本次可以继续体验';
  }
  return createInitialState();
}

function save() {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
    saveFailed = false;
    saveMessage = '进度已保存';
  } catch {
    saveFailed = true;
    saveMessage = '进度未能保存，请保留当前页面';
  }
}

function dispatch(action) {
  const next = reduce(state, action);
  if (next === state) return;
  if (next.location === 'base' && state.location !== 'base') roomPosition = { ...SPAWN };
  state = next;
  save();
  render();
}

function render() {
  exploration?.destroy();
  exploration = null;
  document.body.classList.toggle('exploring', ui.tab === 'base' && state.location === 'base');
  const current = chapter();
  const tabs = [['base', 'home', state.location === 'base' ? '探索' : '当前地点'], ['quests', 'book', '任务'], ['people', 'people', '人物']];
  app.innerHTML = `<div class="shell">
    <header class="topbar">
      <a class="brand" href="#" data-action="tab" data-value="base" aria-label="以太，返回当前地点"><span class="brand-name">以太</span><span class="brand-note">学院篇</span></a>
      <nav aria-label="主要页面">${tabs.map(([id, glyph, label]) => `<button class="nav-tab ${ui.tab === id ? 'active' : ''}" data-action="tab" data-value="${id}" aria-current="${ui.tab === id ? 'page' : 'false'}">${icon(glyph)}<span>${label}</span></button>`).join('')}</nav>
      <div class="top-actions"><span class="prototype-label">前端原型</span>${button(icon('reset') + '<span>重新开始</span>', 'reset', 'quiet reset-button')}</div>
    </header>
    <main id="main-content" tabindex="-1">${ui.tab === 'quests' ? renderQuests() : ui.tab === 'people' ? renderPeople() : renderLocation()}</main>
    <footer class="footer"><span>${escape(current.kicker)}${state.location === 'summary' && state.endReason === 'defeat' ? '（本轮结束）' : ''}</span><span class="save-status ${saveFailed ? 'save-failed' : ''}" role="status">${escape(saveMessage)}</span><span class="footer-hint">演示进度与游戏存档独立</span></footer>
  </div>`;
  renderModal();
  const canvas = document.querySelector('#exploration-canvas');
  if (canvas) {
    const mainScene = SCENES[current.mainSceneId];
    const restScenes = current.presentCharacterIds.includes('lise') ? Object.values(SCENES).filter(scene => scene.optional && scene.characterId === 'lise' && scene.phase <= state.phase) : [];
    const optional = restScenes.find(scene => !state.seenScenes.includes(scene.id)) || restScenes.at(-1);
    const presentCharacters = current.presentCharacterIds.map(id => ({
      id, name: shortName(id),
      sceneId: mainScene.characterId === id ? mainScene.id : Object.values(SCENES).find(scene => scene.optional && scene.phase === state.phase && scene.characterId === id)?.id,
    })).filter(person => person.sceneId);
    exploration = mountExploration({
      canvas, presentCharacters, optionalSceneId: optional?.id, position: roomPosition,
      paused: () => Boolean(ui.modal) || ui.tab !== 'base' || state.location !== 'base',
      onPosition: position => { roomPosition = position; },
      onInteract: target => {
        if (target.action === 'scene' || target.action === 'optional') openScene(target.sceneId);
        else if (target.action === 'loadout') { ui.modal = 'loadout'; renderModal(); }
        else if (target.action === 'journal') { ui.modal = 'journal'; renderModal(); }
        else if (target.action === 'depart') { ui.node = null; dispatch({ type: 'DEPART' }); }
      },
    });
    exploration.ready.then(() => { if (canvas.isConnected && !ui.modal && document.activeElement === document.body) canvas.focus({ preventScroll: true }); });
  }
}

function renderLocation() {
  if (state.location === 'base') return renderBase();
  if (state.location === 'map') return renderMap();
  if (state.location === 'encounter') return renderEncounter();
  if (state.location === 'result') return renderResult();
  return renderSummary();
}

function renderBase() {
  const current = chapter();
  return `<section class="exploration-page">
    <div class="exploration-heading"><div><h1>工读值房</h1><p>${escape(current.objective)}</p></div><span class="chapter-label">${escape(current.kicker)}</span></div>
    <div class="exploration-stage"><canvas id="exploration-canvas" role="application" tabindex="0" data-action="explore-focus" aria-label="自由探索基地，WASD或方向键移动，E交互，也可点击地面行走"></canvas><button id="explore-interact" class="world-interact" type="button" disabled>靠近人物或物件</button></div>
    <div class="exploration-controls"><span><kbd>${EXPLORATION_HINTS.move}</kbd> 或方向键移动</span><span><kbd>${EXPLORATION_HINTS.interact}</kbd> 交互</span><span>点击地面行走，点击人物走近交谈</span></div>
  </section>`;
}

function renderMap() {
  const selected = node();
  const next = getDerived(state).nextNodeId;
  return `<section class="workspace"><div class="page-heading"><div><h1>学院路线</h1><p>选一段任务，带着明确的事情出发。</p></div><span class="chapter-label">路线示例</span></div>
    <div class="route-workspace"><div class="route-board"><div class="route-line"></div>${NODES.map((item, index) => {
      const complete = state.completedNodes.includes(item.id); const active = item.id === next;
      return `<button class="route-stop ${complete ? 'complete' : ''} ${active ? 'available' : ''} ${selected.id === item.id ? 'selected' : ''}" data-action="select-node" data-id="${item.id}" ${!complete && !active ? 'disabled' : ''}><span class="route-symbol">${icon(complete ? 'check' : active ? 'route' : 'lock')}</span><span class="route-region">${escape(item.region)}</span><strong>${escape(item.title)}</strong><span>${complete ? '已完成' : active ? '可以出发' : '尚未到达'}</span></button>`;
    }).join('')}<p class="route-note">四个示例节点串起交班、返程、异常与塔前准备。</p></div>
    <div class="node-detail"><span class="small-label">${escape(selected.type)}</span><h2>${escape(selected.title)}</h2><p>${escape(selected.description)}</p><dl><dt>当前目标</dt><dd>${escape(selected.objective)}</dd><dt>已知边界</dt><dd>${escape(selected.risk)}</dd></dl>${button(selected.id === next ? '确认出发' + icon('arrow') : '本段已完成' + icon('check'), 'start-node', 'primary full', `data-id="${selected.id}" ${selected.id === next ? '' : 'disabled'}`)}<p class="subtle-note">此处演示任务流转，现场结果由演示按钮选择。</p></div></div>
  </section>`;
}

function renderEncounter() {
  const current = node();
  return `<section class="encounter-page"><div class="page-heading"><div><h1>${escape(current.title)}</h1><p>${escape(current.region)}的${escape(current.type)}</p></div><span class="chapter-label">现场演示</span></div>
    <div class="encounter-scene"><div class="encounter-landscape" aria-hidden="true"></div><div class="encounter-sheet"><span class="small-label">这次要做的事</span><h2>${escape(current.objective)}</h2><p>${escape(current.risk)}</p><div class="resource-row"><span>生命<strong>${state.hp}</strong></span><span>个人魔力<strong>${state.mana}</strong></span></div><div class="encounter-actions">${button('演示通过' + icon('check'), 'resolve-victory', 'primary')}${button('演示未通过', 'resolve-defeat', 'outline')}</div><p class="subtle-note">只演示剧情与结果衔接，不包含实际战斗。</p></div></div>
  </section>`;
}

function renderResult() {
  const current = node(); const victory = state.result.outcome === 'victory';
  return `<section class="result-page"><div class="result-symbol ${victory ? '' : 'failed'}">${icon(victory ? 'check' : 'reset')}</div><h1>${victory ? '记录已完成' : '本次未通过'}</h1><p class="result-subtitle">${escape(current.title)}</p><p class="result-description">${victory ? '整理这次的结果，再回值房与同伴交接。' : '可以恢复本场开始前的状态，再试一次。'}</p>
    ${victory ? `<div class="reward-row"><span>演示报酬</span><strong>${state.result.rewardGold}</strong><span>金币</span></div><p class="subtle-note">领取后保存，只结算一次。</p>${button('领取并' + (state.phase === 3 ? '查看总结' : '返回值房') + icon('arrow'), 'claim', 'primary')}` : `<div class="result-actions">${button('恢复战前状态' + icon('reset'), 'retry', 'primary')}${button('结束本轮', 'end-run', 'quiet danger')}</div>`}
    <div class="resource-row compact"><span>生命<strong>${state.hp}</strong></span><span>个人魔力<strong>${state.mana}</strong></span><span>已领取金币<strong>${state.gold}</strong></span></div></section>`;
}

function renderSummary() {
  const completed = state.endReason === 'completed';
  return `<section class="summary-layout"><div class="summary-image"><img src="./assets/duty-room-concept.png" alt="回到学院值房" /></div><div class="summary-content"><span class="small-label">学院经历</span><h1>${completed ? '这一段，走到这里' : '先把这一轮放下'}</h1><p>${completed ? '留下完成的记录，也给没有做完的事情留一个清楚的位置。' : '已经完成的任务仍留在本轮记录中。新的演示会从交班开始。'}</p><dl class="summary-stats"><div><dt>已完成任务</dt><dd>${state.completedNodes.length}</dd></div><div><dt>已领取金币</dt><dd>${state.gold}</dd></div><div><dt>已读交谈</dt><dd>${state.seenScenes.length - state.skippedScenes.length}</dd></div></dl>${completed ? button('听听莉瑟的回应' + icon('talk'), 'scene', 'primary', 'data-id="homeward"') : ''}<div class="summary-buttons">${button('回看交谈', 'journal', 'outline')}${button('重新开始', 'reset', 'quiet')}</div></div></section>`;
}

function stepScene(quest, step) {
  if (quest.id === 'lise_thread') return ({ 0: 'checkin', 1: 'return_plans', 2: 'lise_future', 4: 'homeward' })[step.phase];
  if (quest.id === 'nata_thread') return 'tower_briefing';
  if (quest.id === 'veil_thread') return step.phase === 2 ? 'record_dispute' : 'veil_margin';
  if (quest.id === 'cassian_thread') return 'cassian_compare';
  return CHAPTERS[step.phase].mainSceneId;
}

function renderQuests() {
  const selected = QUESTS.find(item => item.id === ui.quest) || QUESTS[0];
  const unlocked = selected.unlockPhase <= state.phase;
  const visibleSteps = selected.steps.filter(step => step.phase <= state.phase);
  const stepsMarkup = visibleSteps.map(step => {
    const sceneId = stepScene(selected, step);
    const read = state.seenScenes.includes(sceneId) && !state.skippedScenes.includes(sceneId);
    const done = selected.kind === 'main' ? step.phase < state.phase || (step.phase === 4 && read) : read;
    const status = selected.kind === 'main'
      ? done ? '已完成' : step.phase === 4 ? '等待同伴回应' : '等待任务结果'
      : read ? '交谈已读' : state.skippedScenes.includes(sceneId) ? '交谈已略过，可回看' : '等待交谈';
    return `<li class="${done ? 'done' : 'current'}"><span class="step-symbol">${icon(done ? 'check' : selected.kind === 'main' ? 'route' : 'talk')}</span><div><strong>${escape(step.label)}</strong><span>${status}</span></div></li>`;
  }).join('');
  const futureMarkup = selected.steps.some(step => step.phase > state.phase) ? `<li class="future"><span class="step-symbol">${icon('lock')}</span><div><strong>后续经历</strong><span>随当前任务逐步展开</span></div></li>` : '';
  return `<section class="workspace"><div class="page-heading"><div><h1>正在经历的事</h1><p>查看当前任务与人物故事。</p></div><button class="text-button" data-action="journal">${icon('book')}<span>交谈记录</span></button></div><div class="ledger-layout"><aside class="ledger-list">${QUESTS.map(item => `<button class="ledger-row ${item.id === selected.id ? 'selected' : ''}" data-action="select-quest" data-id="${item.id}"><span class="ledger-type">${item.kind === 'main' ? '主线' : shortName(item.characterId)}</span><strong>${item.unlockPhase <= state.phase ? escape(item.title) : '尚未展开的故事'}</strong>${icon(item.unlockPhase <= state.phase ? 'arrow' : 'lock')}</button>`).join('')}</aside><article class="ledger-detail"><span class="small-label">${selected.kind === 'main' ? '学院主线' : escape(characters[selected.characterId].name)}</span><h2>${unlocked ? escape(selected.title) : '还没到这段故事'}</h2><p>${unlocked ? escape(selected.description) : '随当前经历推进，会在合适的会面中展开。'}</p>${unlocked ? `<ol class="quest-steps">${stepsMarkup}${futureMarkup}</ol>` : ''}${button('返回当前地点' + icon('arrow'), 'go-base', 'outline')}</article></div></section>`;
}

function scenesForPerson(id) {
  return Object.values(SCENES).filter(scene => scene.characterId === id && (state.seenScenes.includes(scene.id) || ((state.location === 'base' || (state.location === 'summary' && state.endReason === 'completed')) && chapter().presentCharacterIds.includes(id) && (scene.phase === state.phase || (scene.optional && scene.phase < state.phase)))));
}

function renderPeople() {
  const person = characters[ui.person] || characters.lise;
  const scenes = scenesForPerson(person.id);
  const present = (state.location === 'base' || state.location === 'summary') && chapter().presentCharacterIds.includes(person.id);
  const description = person.id === 'veil' && state.phase < 2 ? '封存区的记录员。普通借还、获准抄件和区域边界，都要经过他核对。' : person.description;
  return `<section class="workspace"><div class="page-heading"><div><h1>一起做事的人</h1><p>走近在场的人交谈，已经发生的内容可以在这里回看。</p></div></div><div class="people-layout"><aside class="people-list">${CHARACTERS.map(item => `<button class="person-row ${item.id === person.id ? 'selected' : ''}" data-action="select-person" data-id="${item.id}"><span class="person-initial" style="--person-color:${item.color}">${escape(shortName(item.id).slice(0, 1))}</span><div><strong>${escape(shortName(item.id))}</strong><span>${escape(item.role)}</span></div>${icon('arrow')}</button>`).join('')}</aside><article class="person-detail ${person.id === 'victor' ? 'with-portrait' : ''}">${person.id === 'victor' ? '<img class="victor-portrait" src="./assets/victor.png" alt="维克多的现有角色立绘" />' : ''}<div class="person-copy"><span class="presence">${person.id === 'victor' ? '当前角色' : present ? '现在在值房' : '各有自己的去处'}</span><h2>${escape(person.name)}</h2><p>${escape(description)}</p><h3>交谈与回看</h3>${scenes.length ? `<div class="scene-list">${scenes.map(scene => `<button class="scene-row" data-action="scene" data-id="${scene.id}" ${state.seenScenes.includes(scene.id) ? '' : 'disabled'}>${icon('talk')}<span>${escape(scene.title)}</span><span class="status-text">${state.seenScenes.includes(scene.id) ? sceneStatus(scene.id) : '在场景中交谈'}</span>${state.seenScenes.includes(scene.id) ? icon('arrow') : ''}</button>`).join('')}</div>` : '<p class="empty-copy">目前没有新的会面，已发生的交谈会留在这里。</p>'}</div></article></div></section>`;
}

function openScene(id) {
  const scene = SCENES[id];
  if (!scene || scene.phase > state.phase || (state.location === 'summary' && state.endReason === 'defeat' && !state.seenScenes.includes(id))) return;
  ui.scene = id; ui.line = 0; ui.transcript = false; ui.modal = 'dialogue'; renderModal();
}

function closeModal() {
  ui.modal = null; ui.scene = null; ui.transcript = false; renderModal();
}

function finishScene(skipped) {
  const id = ui.scene;
  closeModal();
  dispatch({ type: 'COMPLETE_SCENE', sceneId: id, skipped });
  announcement.textContent = skipped ? '交谈已略过，可在记录中回看。' : '交谈已读。';
}

function renderModal() {
  if (ui.modal && !overlayRoot.firstElementChild && document.activeElement?.dataset.action) {
    const active = document.activeElement;
    returnFocus = { action: active.dataset.action, id: active.dataset.id, value: active.dataset.value };
  }
  app.inert = Boolean(ui.modal);
  document.body.classList.toggle('has-modal', Boolean(ui.modal));
  if (!ui.modal) {
    overlayRoot.innerHTML = '';
    if (returnFocus) {
      const previous = returnFocus;
      returnFocus = null;
      requestAnimationFrame(() => {
        const selector = `[data-action="${CSS.escape(previous.action)}"]${previous.id ? `[data-id="${CSS.escape(previous.id)}"]` : ''}${previous.value ? `[data-value="${CSS.escape(previous.value)}"]` : ''}`;
        app.querySelector(selector)?.focus({ preventScroll: true });
      });
    }
    return;
  }
  let content = '';
  if (ui.modal === 'dialogue') {
    const scene = SCENES[ui.scene]; const line = scene.lines[ui.line]; const speaker = characters[line.speaker];
    content = `<div class="dialogue-window"><div class="dialogue-top"><span>${escape(scene.title)}</span><button class="icon-button" data-action="close-modal" aria-label="暂时关闭交谈">${icon('close')}</button></div>${ui.transcript ? `<div class="transcript">${scene.lines.slice(0, ui.line + 1).map(item => `<p><strong>${escape(shortName(item.speaker))}</strong><span>${escape(item.text)}</span></p>`).join('')}</div>` : `<div class="dialogue-body"><div class="speaker-sign" style="--person-color:${speaker.color}">${line.speaker === 'victor' ? '<img src="./assets/victor.png" alt="维克多" />' : `<span>${escape(shortName(line.speaker).slice(0, 1))}</span>`}</div><div class="dialogue-copy"><h2>${escape(speaker.name)}</h2><p id="dialogue-text">${escape(line.text)}</p></div></div>`}<div class="dialogue-controls"><div><button class="text-button" data-action="transcript">${icon('book')}<span>${ui.transcript ? '返回交谈' : '本段回看'}</span></button><button class="text-button" data-action="skip-scene">略过本段</button></div><span class="dialogue-progress">${ui.line + 1} / ${scene.lines.length}</span>${button((ui.line === scene.lines.length - 1 ? '结束交谈' : '继续') + icon('arrow'), 'next-line', 'primary')}</div></div>`;
  } else if (ui.modal === 'loadout') {
    content = `<div class="small-modal"><div class="modal-heading"><h2>整理行囊</h2><button class="icon-button" data-action="close-modal" aria-label="关闭行囊">${icon('close')}</button></div><p>选择这次携带方式，作为剧情流程的整备示例。</p><div class="loadout-options">${[['careful', '谨慎携带', '先把检查与防护用品理清。'], ['mobile', '轻便携带', '留下必要工具，方便行动。']].map(([id, title, description]) => `<button class="loadout-option ${state.loadout === id ? 'selected' : ''}" data-action="set-loadout" data-value="${id}" aria-pressed="${state.loadout === id}"><span>${icon('bag')}<strong>${title}</strong></span><span>${description}</span>${state.loadout === id ? icon('check') : ''}</button>`).join('')}</div><p class="subtle-note">演示不改动实际装备和战斗数值。</p>${button('收好行囊', 'close-modal', 'primary full')}</div>`;
  } else if (ui.modal === 'journal') {
    const seen = Object.values(SCENES).filter(scene => state.seenScenes.includes(scene.id));
    content = `<div class="small-modal journal-modal"><div class="modal-heading"><h2>交谈记录</h2><button class="icon-button" data-action="close-modal" aria-label="关闭记录">${icon('close')}</button></div>${seen.length ? seen.map(scene => `<button class="journal-row" data-action="scene" data-id="${scene.id}"><span class="small-label">${escape(shortName(scene.characterId))}</span><strong>${escape(scene.title)}</strong><span>${sceneStatus(scene.id)}</span>${icon('arrow')}</button>`).join('') : '<div class="empty-state">' + icon('book') + '<p>还没有交谈记录。</p><span>与值房里的人聊聊，记录会留在这里。</span></div>'}</div>`;
  } else {
    const end = ui.modal === 'end-run';
    content = `<div class="small-modal"><h2>${end ? '结束这一轮？' : '重新开始演示？'}</h2><p>${end ? '本轮将进入失败总结，无法再重试当前现场。' : '清除本次演示的任务与交谈记录，从开局交班重新开始。'}</p><div class="confirm-actions">${button('保留当前进度', 'close-modal', 'outline')}${button(end ? '结束本轮' : '重新开始', end ? 'confirm-end' : 'confirm-reset', 'primary')}</div></div>`;
  }
  overlayRoot.innerHTML = `<div class="modal-backdrop"><section role="dialog" aria-modal="true" aria-label="${ui.modal === 'dialogue' ? escape(SCENES[ui.scene].title) : ui.modal === 'loadout' ? '整理行囊' : ui.modal === 'journal' ? '交谈记录' : '确认操作'}">${content}</section></div>`;
  requestAnimationFrame(() => overlayRoot.querySelector(ui.modal === 'dialogue' ? '[data-action="next-line"]' : 'button')?.focus({ preventScroll: true }));
}

function handleAction(event) {
  const target = event.target.closest('[data-action]');
  if (!target || target.disabled) return;
  event.preventDefault();
  switch (target.dataset.action) {
    case 'tab': ui.tab = target.dataset.value; render(); if (ui.tab === 'base') exploration?.focus(); break;
    case 'go-base': ui.tab = 'base'; render(); exploration?.focus(); break;
    case 'scene': openScene(target.dataset.id); break;
    case 'loadout': if (state.location === 'base') { ui.modal = 'loadout'; renderModal(); } break;
    case 'set-loadout': dispatch({ type: 'SET_LOADOUT', value: target.dataset.value }); break;
    case 'depart': ui.node = null; ui.tab = 'base'; dispatch({ type: 'DEPART' }); break;
    case 'select-node': ui.node = target.dataset.id; render(); break;
    case 'start-node': dispatch({ type: 'START_NODE', nodeId: target.dataset.id }); break;
    case 'resolve-victory': dispatch({ type: 'RESOLVE_NODE', outcome: 'victory' }); break;
    case 'resolve-defeat': dispatch({ type: 'RESOLVE_NODE', outcome: 'defeat' }); break;
    case 'claim': ui.node = null; dispatch({ type: 'CLAIM_RESULT' }); break;
    case 'retry': dispatch({ type: 'RETRY' }); break;
    case 'end-run': ui.modal = 'end-run'; renderModal(); break;
    case 'confirm-end': closeModal(); dispatch({ type: 'END_RUN' }); break;
    case 'select-quest': ui.quest = target.dataset.id; render(); break;
    case 'select-person': ui.person = target.dataset.id; render(); break;
    case 'next-line': if (ui.line === SCENES[ui.scene].lines.length - 1) finishScene(false); else { ui.line++; ui.transcript = false; renderModal(); } break;
    case 'skip-scene': finishScene(true); break;
    case 'transcript': ui.transcript = !ui.transcript; renderModal(); break;
    case 'journal': ui.modal = 'journal'; renderModal(); break;
    case 'close-modal': closeModal(); break;
    case 'reset': ui.modal = 'reset'; renderModal(); break;
    case 'confirm-reset': closeModal(); state = createInitialState(); roomPosition = { ...SPAWN }; ui.tab = 'base'; ui.node = null; announcement.textContent = ''; save(); render(); break;
  }
}

app.addEventListener('click', handleAction);
overlayRoot.addEventListener('click', handleAction);
document.addEventListener('keydown', event => {
  if (!ui.modal) return;
  if (event.key === 'Escape') { event.preventDefault(); closeModal(); return; }
  if (event.key === 'Tab') {
    const items = [...overlayRoot.querySelectorAll('button:not([disabled])')];
    const first = items[0]; const last = items.at(-1);
    if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last?.focus(); }
    else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first?.focus(); }
  }
});

render();
document.documentElement.dataset.ready = 'true';
