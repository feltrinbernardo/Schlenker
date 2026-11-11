// Runs the prepared HMI scripts against tag mocks; does not connect to TIA.
const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const root = path.resolve(__dirname, '..');
const bindings = JSON.parse(fs.readFileSync(path.join(root, 'REV12/HMI/SMC_LogicalTest_Bindings.json'), 'utf8'));
const rows = fs.readFileSync(path.join(root, 'REV12/HMI/SMC_LogicalTest_Tags.csv'), 'utf8').trim().split(/\r?\n/).slice(1);
const names = new Set(rows.map(row => row.split(',')[0]));
let checked = 0;
function run(script, values = {}, quality = {}) {
  const writes = [];
  const runtime = { Tags(name) {
    assert(names.has(name), `Undeclared HMI alias: ${name}`);
    return {
      LastError: quality[name]?.error ?? 0,
      QualityCode: quality[name]?.code ?? 192,
      Read() { return values[name] ?? false; },
      Write(value) { writes.push([name, value]); }
    };
  }};
  const result = new Function('HMIRuntime', script)(runtime);
  checked++;
  return { result, writes };
}
assert.equal(names.size, 67);
assert.equal(bindings.valves.length, 11);
for (const [index, valve] of bindings.valves.entries()) {
  const i = index + 1;
  assert.equal(valve.valve, `125Y${i}`);
  assert.deepEqual(run(valve.downBody).writes, [[`SMC_Y${i}_Request`, true]]);
  assert.deepEqual(run(valve.upBody).writes, [[`SMC_Y${i}_Request`, false]]);
  const stateTag = `SMC_Y${i}_State`;
  const values = { SMC_Logical_HmiLinkOK: true, [stateTag]: 0 };
  for (const [state, expected] of ['IDLE', 'INTERLOCKED', 'RELEASE', 'ACCEPTED', 'MULTIPLE'].entries()) {
    assert.equal(run(valve.stateText, { ...values, [stateTag]: state }).result, expected);
  }
  assert.equal(run(valve.stateText, values, { [stateTag]: { code: 0 } }).result, 'NO DATA');
  assert.equal(run(valve.stateText, values, { [stateTag]: { error: 1 } }).result, 'NO DATA');
  assert.equal(run(valve.stateText, { ...values, SMC_Logical_HmiLinkOK: false }).result, 'NO DATA');
  assert.equal(run(valve.stateText, { ...values, [stateTag]: 255 }).result, 'INVALID STATE');
}
for (const link of [false, true]) for (const permit of [false, true]) {
  const values = { SMC_Logical_HmiLinkOK: link, SMC_Logical_Permit: permit };
  assert.equal(run(bindings.requestEnabled, values).result, link && permit);
  assert.equal(run(bindings.enableOnEnabled, values).result, link);
}
assert.equal(run(bindings.requestEnabled, { SMC_Logical_HmiLinkOK: true, SMC_Logical_Permit: true }, { SMC_Logical_Permit: { code: 0 } }).result, false);
for (const script of [bindings.screenLoadedBody, bindings.screenUnloadedBody]) {
  const writes = new Map(run(script).writes);
  assert.equal(writes.get('SMC_Logical_Enable'), false);
  for (let i = 1; i <= 11; i++) assert.equal(writes.get(`SMC_Y${i}_Request`), false);
}
assert.equal(new Map(run(bindings.screenLoadedBody).writes).get('SMC_Logical_PageActive'), true);
assert.equal(new Map(run(bindings.screenUnloadedBody).writes).get('SMC_Logical_PageActive'), false);
console.log(`PASS: ${checked} actual prepared HMI script executions; tag resolution, momentary requests, quality fallback, permit gating and lifecycle resets. Not native TIA compilation or runtime validation.`);
