'use strict';
// 冻结 JS（source.txt）命名实现与共享向量对照测试：
// 从 source.txt 提取 ai-persona-name-client 与 persona-presentation 的 displayNameOf，
// 在 node 沙箱中执行，断言与 PersonaNameValidationCases.json 一致。
// 该测试锁定「页面侧（冻结 JS）与 C#/服务端三方共用同一套规则与清单」。
// 用法：node frozen-js-name.test.cjs
const path = require('path');
const fs = require('fs');

const webAligned = path.join(__dirname, '..', '..', 'Assets', 'WebAligned');
const source = fs.readFileSync(path.join(webAligned, 'Resources', 'WebCore', 'source.txt'), 'utf8');
const cases = JSON.parse(fs.readFileSync(path.join(webAligned, 'Tests', 'PersonaNameValidationCases.json'), 'utf8'));

// 提取 // SOURCE: marker 之后第一个 IIFE 模块代码
function extractModule(code, marker) {
  const start = code.indexOf('// SOURCE: ' + marker);
  if (start < 0) throw new Error('module not found: ' + marker);
  const rest = code.slice(start);
  const next = rest.indexOf('\n\n\n// SOURCE:');
  const chunk = next >= 0 ? rest.slice(0, next) : rest;
  const iifeStart = chunk.indexOf('(');
  const iifeEnd = chunk.lastIndexOf(')(globalThis)');
  if (iifeStart < 0 || iifeEnd < 0) throw new Error('IIFE not found in ' + marker);
  return chunk.slice(iifeStart, iifeEnd + ')(globalThis)'.length);
}

function loadModule(marker) {
  const sandbox = {};
  new Function('globalThis', extractModule(source, marker))(sandbox);
  return sandbox;
}

let passed = 0, failed = 0;
function assert(condition, label) {
  if (condition) { passed++; return; }
  failed++;
  console.error('[FAIL] ' + label);
}
function assertEqual(actual, expected, label) {
  if (actual === expected) { passed++; return; }
  failed++;
  console.error(`[FAIL] ${label}：期望 ${JSON.stringify(expected)}，实际 ${JSON.stringify(actual)}`);
}

const nameRoot = loadModule('game/ai-persona-name-client.js');
const presentationRoot = loadModule('game/persona-presentation.js');
const client = nameRoot.AiPersonaNameClient;

assert(!!client, 'AiPersonaNameClient 已挂载');
assertEqual(client.NAME_STYLE_VERSION, cases.styleVersion, '页面侧 styleVersion 与共享向量一致');
assertEqual(client.DEFAULT_TIMEOUT_MS, 3000, '命名超时 = 3 秒（规格 6.1）');

// 保留清单与共享向量一致
const full = client.fullReservedNames();
assertEqual(full.length, 10 + 10 + 40, '页面侧完整保留名 = 60');
for (const name of cases.baseReservedNames) assert(full.includes(name), `页面侧保留清单含基础名 ${name}`);
for (const name of cases.bossBaseNames) assert(full.includes(name), `页面侧保留清单含 Boss 名 ${name}`);
for (const name of cases.bossReservedInvalidNames) assert(full.includes(name), `页面侧保留清单含 Boss 变体 ${name}`);

// displayName 校验与向量一致
for (const name of cases.formatValidNames) {
  assert(client.validateDisplayName(name).ok, `页面侧合法名「${name}」通过`);
  assert(client.validateDisplayName(name, { reservedNames: full }).ok, `页面侧合法名「${name}」不撞保留清单`);
}
for (const name of cases.formatInvalidNames) {
  assert(!client.validateDisplayName(name).ok, `页面侧格式非法名「${JSON.stringify(name)}」被拒`);
}
for (const name of cases.reservedInvalidNames) {
  assert(!client.validateDisplayName(name, { reservedNames: full }).ok, `页面侧撞保留名「${name}」被拒`);
}
for (const name of cases.semanticInvalidNames) {
  assert(client.validateDisplayName(name).ok, `页面侧语义反例「${name}」格式合法（不拦语义，由提示词与策划验收把关）`);
}

// validateResponse：绑定信息与五字段契约
const request = {
  schemaVersion: 1,
  requestId: 'AI_PERSONA_NAME_V1:N04:test01',
  selectionRequestId: 'AI_PERSONA_SELECTION_V1:N04:AI_BEHAVIOR_SNAPSHOT_V1:N04:3',
  runtimeNodeId: 'N04',
  selectedCandidateId: 'candidate-x',
  directionId: 'AI_DIRECTION_BRIDGE',
  styleVersion: 'PERSONA_NAME_STYLE_V1_1',
  playerCopy: { trigger: 't', mainEffect: 'm', growth: 'g', summary: 's' },
  usedNames: [],
};
const goodResponse = {
  schemaVersion: 1,
  requestId: request.requestId,
  selectedCandidateId: request.selectedCandidateId,
  styleVersion: 'PERSONA_NAME_STYLE_V1_1',
  displayName: '留白守望者',
};
assertEqual(client.validateResponse(request, goodResponse).ok, true, '合法响应通过');
assertEqual(client.validateResponse(request, goodResponse).displayName, '留白守望者', '合法响应回传显示名');
assertEqual(client.validateResponse(request, { ...goodResponse, extra: 1 }).ok, false, '多余字段被拒（恰好五字段）');
assertEqual(client.validateResponse(request, { ...goodResponse, requestId: 'AI_PERSONA_NAME_V1:N04:other' }).ok, false, 'requestId 不绑定被拒');
assertEqual(client.validateResponse(request, { ...goodResponse, selectedCandidateId: 'other' }).ok, false, 'selectedCandidateId 不绑定被拒');
assertEqual(client.validateResponse(request, { ...goodResponse, styleVersion: 'PERSONA_NAME_STYLE_V0_9' }).ok, false, 'styleVersion 不绑定被拒');
assertEqual(client.validateResponse(request, { ...goodResponse, displayName: '克制的赌徒' }).ok, false, '撞保留名响应被拒');
assertEqual(client.validateResponse(request, { ...goodResponse, displayName: 'AB' }).ok, false, '格式非法响应被拒');
assertEqual(client.validateResponse(null, goodResponse).ok, false, '空请求被拒');

// displayNameOf（persona-presentation）：AI_NAMED 采用 / 其余回落内部名
const displayNameOf = presentationRoot.GamePersonaPresentation.displayNameOf;
assert(!!displayNameOf, 'displayNameOf 已挂载');
assertEqual(displayNameOf({ name: 'AI人格3' }), 'AI人格3', '无命名回落内部名');
assertEqual(displayNameOf({ name: 'AI人格3', aiPersonaMeta: { naming: { status: 'FALLBACK', displayName: null } } }), 'AI人格3', 'FALLBACK 回落内部名');
assertEqual(displayNameOf({ name: 'AI人格3', aiPersonaMeta: { naming: { status: 'AI_NAMED', displayName: '留白守望者' } } }), '留白守望者', 'AI_NAMED 采用显示名');
assertEqual(displayNameOf({ name: 'AI人格3', aiPersonaMeta: { naming: { status: 'AI_NAMED', displayName: '' } } }), 'AI人格3', 'AI_NAMED 但空显示名回落内部名');
assertEqual(displayNameOf({ name: 'AI人格3', aiPersonaMeta: { naming: { status: 'AI_NAMED', displayName: 123 } } }), 'AI人格3', 'AI_NAMED 但非字符串回落内部名');
assertEqual(displayNameOf(null), '未命名人格', '空模板给占位名');

console.log(`通过 ${passed}，失败 ${failed}`);
process.exit(failed ? 1 : 0);
