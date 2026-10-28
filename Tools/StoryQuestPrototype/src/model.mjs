// Standalone browser-prototype rules. These compressed phases and costs are demo
// values, not OCC's production progression thresholds or battle balance.
export const DEMO_RULES = Object.freeze({
  startingHp: 100,
  startingMana: 6,
  victoryHpCost: 12,
  encounterManaCost: 1,
  victoryGold: 10,
});

const NODE_IDS = Object.freeze(['courtyard', 'records', 'perimeter', 'tower']);
const BASE_CHAPTERS = Object.freeze(['交班', '归来', '记录异常', '塔前', '本轮总结']);
const LOADOUTS = new Set(['careful', 'mobile']);
const STATE_KEYS = [
  'version', 'location', 'phase', 'hp', 'mana', 'loadout', 'gold',
  'completedNodes', 'seenScenes', 'skippedScenes', 'encounter', 'result', 'endReason',
];

export function createInitialState() {
  return {
    version: 1,
    location: 'base',
    phase: 0,
    hp: DEMO_RULES.startingHp,
    mana: DEMO_RULES.startingMana,
    loadout: 'careful',
    gold: 0,
    completedNodes: [],
    // Seen means either finished or skipped; skipped contains only scenes that
    // have not subsequently been read in full. Full reading is never downgraded.
    seenScenes: [],
    skippedScenes: [],
    encounter: null,
    result: null,
    endReason: null,
  };
}

function exactRecord(value, keys) {
  if (value === null || typeof value !== 'object' || Array.isArray(value)) return false;
  const prototype = Object.getPrototypeOf(value);
  if (prototype !== Object.prototype && prototype !== null) return false;
  const ownKeys = Reflect.ownKeys(value);
  return ownKeys.length === keys.length && keys.every(key => {
    const descriptor = Object.getOwnPropertyDescriptor(value, key);
    return descriptor?.enumerable === true && Object.hasOwn(descriptor, 'value');
  });
}

function sceneIdIsValid(value) {
  return typeof value === 'string' && value.length > 0 && value.length <= 128 &&
    value.trim() === value && !/[\u0000-\u001f\u007f]/u.test(value);
}

function uniqueStrings(value, maximum, validItem = sceneIdIsValid) {
  return Array.isArray(value) && value.length <= maximum &&
    Object.keys(value).length === value.length && Array.from(value).every(validItem) &&
    new Set(value).size === value.length;
}

function expectedResources(phase) {
  return {
    hp: DEMO_RULES.startingHp - phase * DEMO_RULES.victoryHpCost,
    mana: phase === 0 ? DEMO_RULES.startingMana :
      Math.max(0, DEMO_RULES.startingMana - DEMO_RULES.encounterManaCost),
  };
}

/** Validate a parsed version-1 save. Does not migrate, repair, or mutate it. */
export function validateSavedState(value) {
  try {
    if (!exactRecord(value, STATE_KEYS) || value.version !== 1 ||
        !Number.isInteger(value.phase) || value.phase < 0 || value.phase > NODE_IDS.length ||
        !LOADOUTS.has(value.loadout)) return false;
    if (!uniqueStrings(value.completedNodes, NODE_IDS.length, id => NODE_IDS.includes(id)) ||
        value.completedNodes.length !== value.phase ||
        !value.completedNodes.every((id, index) => id === NODE_IDS[index]) ||
        value.gold !== value.phase * DEMO_RULES.victoryGold) return false;
    if (!uniqueStrings(value.seenScenes, 128) || !uniqueStrings(value.skippedScenes, 128) ||
        !value.skippedScenes.every(id => value.seenScenes.includes(id))) return false;

    const baseline = expectedResources(value.phase);
    const manaAfterEncounter = Math.max(0, DEMO_RULES.startingMana - DEMO_RULES.encounterManaCost);
    if (value.location === 'summary') {
      if (value.encounter !== null || value.result !== null) return false;
      if (value.phase === NODE_IDS.length) {
        return value.endReason === 'completed' && value.hp === baseline.hp && value.mana === baseline.mana;
      }
      return value.endReason === 'defeat' && value.hp === 0 && value.mana === manaAfterEncounter;
    }
    if (value.phase === NODE_IDS.length || value.endReason !== null) return false;
    if (value.location === 'base' || value.location === 'map') {
      return value.encounter === null && value.result === null &&
        value.hp === baseline.hp && value.mana === baseline.mana;
    }
    if (value.location !== 'encounter' && value.location !== 'result') return false;
    if (!exactRecord(value.encounter, ['nodeId', 'snapshot']) ||
        value.encounter.nodeId !== NODE_IDS[value.phase] ||
        !exactRecord(value.encounter.snapshot, ['hp', 'mana', 'loadout'])) return false;
    const snapshot = value.encounter.snapshot;
    if (snapshot.hp !== baseline.hp || snapshot.mana !== DEMO_RULES.startingMana || snapshot.loadout !== value.loadout) return false;
    if (value.location === 'encounter') {
      return value.result === null && value.hp === snapshot.hp && value.mana === snapshot.mana;
    }
    if (!exactRecord(value.result, ['nodeId', 'outcome', 'rewardGold']) ||
        value.result.nodeId !== value.encounter.nodeId || value.mana !== manaAfterEncounter) return false;
    if (value.result.outcome === 'victory') {
      return value.result.rewardGold === DEMO_RULES.victoryGold &&
        value.hp === Math.max(0, snapshot.hp - DEMO_RULES.victoryHpCost);
    }
    return value.result.outcome === 'defeat' && value.result.rewardGold === 0 && value.hp === 0;
  } catch {
    return false;
  }
}

function requireValidState(state) {
  if (!validateSavedState(state)) throw new TypeError('Invalid story prototype state; validate the save before restoring it.');
}

/** Invalid or unavailable actions return the same object; valid actions never mutate it. */
export function reduce(state, action) {
  requireValidState(state);
  if (action === null || typeof action !== 'object') return state;
  switch (action.type) {
    case 'DEPART':
      return state.location === 'base' ? { ...state, location: 'map' } : state;
    case 'VISIT_BASE':
      return state.location === 'map' ? { ...state, location: 'base' } : state;
    case 'SET_LOADOUT':
      return state.location === 'base' && LOADOUTS.has(action.value) && action.value !== state.loadout
        ? { ...state, loadout: action.value } : state;
    case 'START_NODE':
      if (state.location !== 'map' || action.nodeId !== NODE_IDS[state.phase]) return state;
      return {
        ...state,
        location: 'encounter',
        mana: DEMO_RULES.startingMana,
        encounter: { nodeId: action.nodeId, snapshot: { hp: state.hp, mana: DEMO_RULES.startingMana, loadout: state.loadout } },
      };
    case 'RESOLVE_NODE': {
      if (state.location !== 'encounter' || !['victory', 'defeat'].includes(action.outcome)) return state;
      const victory = action.outcome === 'victory';
      return {
        ...state,
        location: 'result',
        hp: victory ? Math.max(0, state.hp - DEMO_RULES.victoryHpCost) : 0,
        mana: Math.max(0, state.mana - DEMO_RULES.encounterManaCost),
        result: { nodeId: state.encounter.nodeId, outcome: action.outcome, rewardGold: victory ? DEMO_RULES.victoryGold : 0 },
      };
    }
    case 'CLAIM_RESULT': {
      if (state.location !== 'result' || state.result.outcome !== 'victory') return state;
      const phase = state.phase + 1;
      const finished = phase === NODE_IDS.length;
      return {
        ...state,
        phase,
        location: finished ? 'summary' : 'base',
        gold: state.gold + state.result.rewardGold,
        completedNodes: [...state.completedNodes, state.result.nodeId],
        encounter: null,
        result: null,
        endReason: finished ? 'completed' : null,
      };
    }
    case 'RETRY':
      if (state.location !== 'result' || state.result.outcome !== 'defeat') return state;
      return { ...state, ...state.encounter.snapshot, location: 'encounter', result: null };
    case 'END_RUN':
      if (state.location !== 'result' || state.result.outcome !== 'defeat') return state;
      return { ...state, location: 'summary', encounter: null, result: null, endReason: 'defeat' };
    case 'COMPLETE_SCENE': {
      if (!sceneIdIsValid(action.sceneId) || typeof action.skipped !== 'boolean') return state;
      const seen = state.seenScenes.includes(action.sceneId);
      const skipped = state.skippedScenes.includes(action.sceneId);
      if (seen) {
        if (!skipped || action.skipped) return state;
        return { ...state, skippedScenes: state.skippedScenes.filter(id => id !== action.sceneId) };
      }
      if (state.seenScenes.length >= 128) return state;
      return {
        ...state,
        seenScenes: [...state.seenScenes, action.sceneId],
        skippedScenes: action.skipped ? [...state.skippedScenes, action.sceneId] : state.skippedScenes,
      };
    }
    default:
      return state;
  }
}

export function getDerived(state) {
  requireValidState(state);
  const defeated = state.location === 'result' && state.result.outcome === 'defeat';
  return {
    activeNodeId: state.encounter?.nodeId ?? null,
    nextNodeId: state.location === 'summary' ? null : NODE_IDS[state.phase],
    canDepart: state.location === 'base',
    canRetry: defeated,
    canEndRun: defeated,
    completedCount: state.completedNodes.length,
    baseChapter: BASE_CHAPTERS[state.phase],
  };
}
