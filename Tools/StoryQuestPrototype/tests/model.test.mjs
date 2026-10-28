import test from 'node:test';
import assert from 'node:assert/strict';
import { createInitialState, reduce, getDerived, validateSavedState, DEMO_RULES } from '../src/model.mjs';

const nodes = ['courtyard', 'records', 'perimeter', 'tower'];
const dispatch = (state, type, fields = {}) => reduce(state, { type, ...fields });
const saveRoundTrip = state => JSON.parse(JSON.stringify(state));
const enter = state => dispatch(dispatch(state, 'DEPART'), 'START_NODE', { nodeId: nodes[state.phase] });
const victory = state => dispatch(dispatch(enter(state), 'RESOLVE_NODE', { outcome: 'victory' }), 'CLAIM_RESULT');

test('four demo encounters complete the whole base-to-summary flow without healing', () => {
  let state = createInitialState();
  const chapters = ['交班', '归来', '记录异常', '塔前', '本轮总结'];
  for (let phase = 0; phase < nodes.length; phase++) {
    assert.equal(state.location, 'base');
    assert.equal(getDerived(state).baseChapter, chapters[phase]);
    state = dispatch(state, 'DEPART');
    assert.equal(state.location, 'map');
    state = dispatch(state, 'START_NODE', { nodeId: nodes[phase] });
    assert.equal(getDerived(state).activeNodeId, nodes[phase]);
    state = dispatch(state, 'RESOLVE_NODE', { outcome: 'victory' });
    assert.equal(state.location, 'result');
    assert.equal(state.gold, phase * DEMO_RULES.victoryGold, 'Pending rewards are not credited early.');
    state = dispatch(state, 'CLAIM_RESULT');
    assert.equal(state.phase, phase + 1);
    assert.equal(state.hp, DEMO_RULES.startingHp - (phase + 1) * DEMO_RULES.victoryHpCost);
    assert.equal(state.mana, DEMO_RULES.startingMana - DEMO_RULES.encounterManaCost);
    assert.ok(validateSavedState(state));
  }
  assert.equal(state.location, 'summary');
  assert.equal(state.endReason, 'completed');
  assert.equal(state.gold, 40);
  assert.deepEqual(state.completedNodes, nodes);
  assert.deepEqual(getDerived(state), {
    activeNodeId: null, nextNodeId: null, canDepart: false, canRetry: false,
    canEndRun: false, completedCount: 4, baseChapter: chapters[4],
  });
});

test('repeated claims and repeated resolution cannot award or spend twice', () => {
  const result = dispatch(enter(createInitialState()), 'RESOLVE_NODE', { outcome: 'victory' });
  assert.strictEqual(dispatch(result, 'RESOLVE_NODE', { outcome: 'victory' }), result);
  const claimed = dispatch(result, 'CLAIM_RESULT');
  assert.strictEqual(dispatch(claimed, 'CLAIM_RESULT'), claimed);
  assert.equal(claimed.gold, 10);
  assert.equal(claimed.completedNodes.length, 1);
  assert.strictEqual(dispatch(result, 'RETRY'), result);
  assert.strictEqual(dispatch(result, 'END_RUN'), result);
});

test('defeat retry restores the exact wounded entry snapshot and loadout', () => {
  const base = dispatch(victory(createInitialState()), 'SET_LOADOUT', { value: 'mobile' });
  const entry = enter(base);
  const before = saveRoundTrip(entry);
  let result = dispatch(entry, 'RESOLVE_NODE', { outcome: 'defeat' });
  assert.equal(result.hp, 0);
  assert.equal(result.mana, entry.mana - DEMO_RULES.encounterManaCost);
  assert.equal(getDerived(result).canRetry, true);
  assert.equal(getDerived(result).canEndRun, true);
  assert.strictEqual(dispatch(result, 'CLAIM_RESULT'), result);
  assert.strictEqual(dispatch(result, 'SET_LOADOUT', { value: 'careful' }), result);
  const retried = dispatch(result, 'RETRY');
  assert.deepEqual(retried, before);
  assert.deepEqual(entry, before, 'Reducer does not mutate the prior state.');
  result = dispatch(retried, 'RESOLVE_NODE', { outcome: 'defeat' });
  assert.deepEqual(dispatch(result, 'RETRY'), before, 'Repeated retries cannot accumulate costs.');
});

test('ending a defeated run is terminal and does not award the failed encounter', () => {
  let state = dispatch(enter(victory(createInitialState())), 'RESOLVE_NODE', { outcome: 'defeat' });
  state = dispatch(state, 'END_RUN');
  assert.equal(state.location, 'summary');
  assert.equal(state.endReason, 'defeat');
  assert.equal(state.hp, 0);
  assert.equal(state.phase, 1);
  assert.equal(state.gold, 10);
  for (const type of ['RETRY', 'DEPART', 'CLAIM_RESULT', 'END_RUN', 'VISIT_BASE']) {
    assert.strictEqual(dispatch(state, type), state);
  }
  assert.ok(validateSavedState(saveRoundTrip(state)));
});

test('each new encounter refills personal mana, preserves wounds, and rejects a forged retry snapshot', () => {
  const base = victory(createInitialState());
  const map = dispatch(base, 'DEPART');
  assert.equal(map.mana, DEMO_RULES.startingMana - DEMO_RULES.encounterManaCost);
  const entry = dispatch(map, 'START_NODE', { nodeId: 'records' });
  assert.equal(entry.hp, base.hp, 'Starting a new battle does not heal wounds.');
  assert.equal(entry.mana, DEMO_RULES.startingMana);
  assert.equal(entry.encounter.snapshot.mana, DEMO_RULES.startingMana);
  const defeat = dispatch(entry, 'RESOLVE_NODE', { outcome: 'defeat' });
  const restored = saveRoundTrip(defeat);
  assert.ok(validateSavedState(restored));
  assert.equal(dispatch(restored, 'RETRY').mana, DEMO_RULES.startingMana);
  restored.encounter.snapshot.mana = map.mana;
  assert.equal(validateSavedState(restored), false, 'Pre-departure mana is not the battle-entry snapshot.');
  assert.throws(() => dispatch(restored, 'RETRY'), TypeError);
});

test('finishing, skipping, or revisiting a scene never grants resources or completes nodes', () => {
  const before = createInitialState();
  let state = dispatch(before, 'COMPLETE_SCENE', { sceneId: 'handoff', skipped: true });
  state = dispatch(state, 'COMPLETE_SCENE', { sceneId: 'return', skipped: false });
  assert.deepEqual(state.seenScenes, ['handoff', 'return']);
  assert.deepEqual(state.skippedScenes, ['handoff']);
  assert.deepEqual({ ...state, seenScenes: [], skippedScenes: [] }, before);
  assert.strictEqual(dispatch(state, 'COMPLETE_SCENE', { sceneId: 'handoff', skipped: true }), state);
  assert.ok(validateSavedState(state));
});

test('fully replaying a skipped scene marks it read and later skipping cannot downgrade it', () => {
  const skipped = dispatch(createInitialState(), 'COMPLETE_SCENE', { sceneId: 'handoff', skipped: true });
  const read = dispatch(skipped, 'COMPLETE_SCENE', { sceneId: 'handoff', skipped: false });
  assert.deepEqual(read.seenScenes, ['handoff']);
  assert.deepEqual(read.skippedScenes, []);
  assert.deepEqual({ ...read, skippedScenes: ['handoff'] }, skipped, 'Reading changes only the skipped marker.');
  assert.deepEqual(skipped.skippedScenes, ['handoff'], 'Previous snapshots remain unchanged.');
  assert.strictEqual(dispatch(read, 'COMPLETE_SCENE', { sceneId: 'handoff', skipped: true }), read);
  assert.strictEqual(dispatch(read, 'COMPLETE_SCENE', { sceneId: 'handoff', skipped: false }), read);
  assert.ok(validateSavedState(saveRoundTrip(read)));
});

test('illegal jumps, invalid payloads, and non-base equipment edits leave state unchanged', () => {
  const base = createInitialState();
  const map = dispatch(base, 'DEPART');
  const encounter = dispatch(map, 'START_NODE', { nodeId: 'courtyard' });
  for (const nodeId of ['tower', 'records', 'unknown', null]) {
    assert.strictEqual(dispatch(map, 'START_NODE', { nodeId }), map);
  }
  for (const state of [base, map]) {
    assert.strictEqual(dispatch(state, 'RESOLVE_NODE', { outcome: 'victory' }), state);
    assert.strictEqual(dispatch(state, 'RETRY'), state);
    assert.strictEqual(dispatch(state, 'END_RUN'), state);
  }
  assert.strictEqual(dispatch(base, 'START_NODE', { nodeId: 'courtyard' }), base);
  assert.strictEqual(dispatch(encounter, 'VISIT_BASE'), encounter);
  assert.strictEqual(dispatch(encounter, 'SET_LOADOUT', { value: 'mobile' }), encounter);
  assert.strictEqual(dispatch(encounter, 'RESOLVE_NODE', { outcome: 'draw' }), encounter);
  assert.strictEqual(dispatch(base, 'SET_LOADOUT', { value: 'invincible' }), base);
  assert.strictEqual(reduce(base, null), base);
  assert.strictEqual(dispatch(base, 'COMPLETE_SCENE', { sceneId: '', skipped: true }), base);
});

test('visiting the base from the map preserves prior damage and spent mana', () => {
  const woundedBase = victory(createInitialState());
  const returned = dispatch(dispatch(woundedBase, 'DEPART'), 'VISIT_BASE');
  assert.deepEqual(returned, woundedBase);
  assert.ok(returned.hp < DEMO_RULES.startingHp);
  assert.ok(returned.mana < DEMO_RULES.startingMana);
});

test('JSON saves preserve each location, pending results, and retry snapshots', () => {
  let state = createInitialState();
  const states = [state];
  state = dispatch(state, 'DEPART'); states.push(state);
  state = dispatch(state, 'START_NODE', { nodeId: 'courtyard' }); states.push(state);
  const defeat = dispatch(state, 'RESOLVE_NODE', { outcome: 'defeat' }); states.push(defeat);
  const result = dispatch(state, 'RESOLVE_NODE', { outcome: 'victory' }); states.push(result);
  states.push(dispatch(result, 'CLAIM_RESULT'));
  states.push(dispatch(defeat, 'END_RUN'));
  for (const candidate of states) {
    const restored = saveRoundTrip(candidate);
    assert.ok(validateSavedState(restored));
    assert.deepEqual(restored, candidate);
    assert.deepEqual(getDerived(restored), getDerived(candidate));
  }
  assert.equal(saveRoundTrip(result).location, 'result');
  assert.equal(dispatch(saveRoundTrip(result), 'CLAIM_RESULT').gold, 10);
  assert.deepEqual(dispatch(saveRoundTrip(defeat), 'RETRY'), state);
});

test('save validation rejects corrupt shapes, forged progression, and inconsistent snapshots', () => {
  const base = createInitialState();
  const encounter = enter(base);
  const defeat = dispatch(encounter, 'RESOLVE_NODE', { outcome: 'defeat' });
  const corrupted = [
    null, [], {}, JSON.stringify(base),
    { ...base, version: 2 }, { ...base, phase: -1 }, { ...base, phase: 0.5 },
    { ...base, phase: 4 }, { ...base, hp: 101 }, { ...base, mana: NaN },
    { ...base, gold: 10 }, { ...base, location: 'result' },
    { ...base, selectedTab: 'temporary-ui-state' },
    { ...base, skippedScenes: ['unseen'] }, { ...base, seenScenes: ['same', 'same'] },
    { ...base, seenScenes: new Array(2) },
    { ...base, phase: 1, completedNodes: ['tower'], gold: 10 },
    { ...encounter, encounter: { ...encounter.encounter, nodeId: 'tower' } },
    { ...encounter, encounter: { ...encounter.encounter, snapshot: { hp: 50, mana: 6, loadout: 'careful' } } },
    { ...defeat, result: { ...defeat.result, rewardGold: 10 } },
    { ...defeat, hp: 1 }, { ...defeat, loadout: 'mobile' },
    { ...base, location: 'summary', endReason: 'completed' },
  ];
  for (const candidate of corrupted) assert.equal(validateSavedState(candidate), false);
  assert.throws(() => reduce(corrupted[0], { type: 'DEPART' }), TypeError);
  assert.throws(() => getDerived(corrupted[4]), TypeError);
  const getterSave = { ...base };
  Object.defineProperty(getterSave, 'hp', { enumerable: true, get() { throw new Error('must not run'); } });
  assert.equal(validateSavedState(getterSave), false);
});
