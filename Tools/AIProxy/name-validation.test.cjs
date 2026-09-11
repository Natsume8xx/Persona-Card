'use strict';
// AI 命名服务端纯函数本地测试：与 C# EditMode 测试共享同一验证向量文件
// （../Assets/WebAligned/Tests/PersonaNameValidationCases.json），保证双端校验规则一致。
// 用法：node name-validation.test.cjs
// 不触碰 DeepSeek、腾讯云凭据与网络。
const path = require('path');
const fs = require('fs');

const { __test: t } = require('./index.js');
const cases = JSON.parse(
  fs.readFileSync(path.join(__dirname, '..', '..', 'Assets', 'WebAligned', 'Tests', 'PersonaNameValidationCases.json'), 'utf8')
);

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

// ---- 样式版本与保留名清单 ----
assertEqual(t.NAME_STYLE_VERSION, cases.styleVersion, 'styleVersion 与共享向量一致');
assertEqual(JSON.stringify(t.BASE_PERSONA_NAMES), JSON.stringify(cases.baseReservedNames), '基础保留名清单与共享向量一致');
assertEqual(JSON.stringify(t.BOSS_BASE_NAMES), JSON.stringify(cases.bossBaseNames), 'Boss 本名清单与共享向量一致');
assertEqual(JSON.stringify(t.BOSS_VARIANT_PREFIXES), JSON.stringify(cases.bossVariantPrefixes), 'Boss 变体前缀与共享向量一致');

const full = t.fullReservedNames();
assertEqual(full.length, 10 + 10 + 40, '完整保留名 = 基础10 + Boss10 + 变体40');
for (const name of cases.baseReservedNames) assert(full.includes(name), `完整清单含基础名 ${name}`);
for (const name of cases.bossBaseNames) assert(full.includes(name), `完整清单含 Boss 名 ${name}`);
for (const name of cases.bossReservedInvalidNames) assert(full.includes(name), `完整清单含 Boss 变体 ${name}`);

// ---- displayName 校验（与客户端共用向量） ----
for (const name of cases.formatValidNames) {
  assert(t.validateDisplayName(name).ok, `合法名「${name}」通过`);
  assert(t.validateDisplayName(name, { reservedNames: full }).ok, `合法名「${name}」不撞完整保留清单`);
}
for (const name of cases.formatInvalidNames) {
  assert(!t.validateDisplayName(name).ok, `格式非法名「${JSON.stringify(name)}」被拒`);
}
// 语义反例（规格 4.2：效果承诺/位阶/编号变体等）无法用正则保证——
// 它们格式合法，会通过程序格式检查；语义拒绝属于提示词约束 + 内容评审层面。
// 此断言锁定「程序不拦语义」口径，防止误以为全部限制已由程序保证。
for (const name of cases.semanticInvalidNames) {
  assert(t.validateDisplayName(name).ok, `语义反例「${name}」格式合法（不拦语义，由提示词与策划验收把关）`);
}
for (const name of cases.reservedInvalidNames) {
  assert(!t.validateDisplayName(name, { reservedNames: cases.baseReservedNames }).ok, `撞基础保留名「${name}」被拒`);
  assert(!t.validateDisplayName(name, { reservedNames: full }).ok, `撞完整保留名「${name}」被拒`);
}
for (const name of cases.bossReservedInvalidNames) {
  assert(!t.validateDisplayName(name, { reservedNames: full }).ok, `撞 Boss 保留名「${name}」被拒`);
}
for (const name of cases.usedNamesInvalid) {
  assert(t.validateDisplayName(name).ok, `「${name}」本身格式合法（仅作 usedNames 用）`);
  assert(!t.validateDisplayName(name, { usedNames: cases.usedNamesInvalid }).ok, `撞 usedNames「${name}」被拒`);
}
assert(!t.validateDisplayName('克制的赌徒一号', { reservedNames: full }).ok, '旧名后加数字造变体被拒（非纯汉字）');
assert(!t.validateDisplayName(null).ok, '非字符串被拒');

// ---- 命名请求校验 ----
for (const request of cases.validNameRequests) {
  const result = t.validateNameRequest(request);
  assert(result.ok, `合法请求（${request.runtimeNodeId}）通过`);
}
for (const [label, request] of Object.entries(cases.invalidNameRequests)) {
  const result = t.validateNameRequest(request);
  assert(!result.ok, `非法请求「${label}」被拒`);
  assert(Number.isInteger(result.code) && result.code >= 400, `非法请求「${label}」返回错误码`);
}
assert(!t.validateNameRequest(null).ok, 'null 请求被拒');
assert(!t.validateNameRequest([]).ok, '数组请求被拒');
// 错误码细分：结构/版本 400；来源字段（requestId/selectionRequestId 格式或节点不匹配）403
assertEqual(t.validateNameRequest(cases.invalidNameRequests['版本错误']).code, 400, '版本错误 → 400');
assertEqual(t.validateNameRequest(cases.invalidNameRequests['风格版本不认识']).code, 400, '风格版本不认识 → 400');
assertEqual(t.validateNameRequest(cases.invalidNameRequests['请求编号前缀非法']).code, 403, '请求编号前缀非法 → 403');
assertEqual(t.validateNameRequest(cases.invalidNameRequests['选择编号节点不匹配']).code, 403, '选择编号节点不匹配 → 403');
assertEqual(t.validateNameRequest(cases.invalidNameRequests['方向非法']).code, 400, '方向非法 → 400');
assertEqual(t.validateNameRequest(cases.invalidNameRequests['已用名超限']).code, 400, '已用名超限 → 400');

// ---- 路由 ----
assertEqual(t.routeOf({ rawPath: '/api/ai-persona/name' }), 'NAME', 'rawPath 路由命名');
assertEqual(t.routeOf({ requestContext: { http: { path: '/api/ai-persona/name' } } }), 'NAME', '函数URL路径路由命名');
assertEqual(t.routeOf({ path: '/api/ai-persona/name' }), 'NAME', 'API网关路径路由命名');
assertEqual(t.routeOf({ path: '/api/ai-persona/select' }), 'SELECT', 'select 路径走旧选择');
assertEqual(t.routeOf({}), 'SELECT', '无路径走旧选择（兼容旧部署）');

// ---- user 消息构造：含资料与保留名，客户端指令原样为资料 ----
const sample = cases.validNameRequests[0];
const userMessage = JSON.parse(t.buildNameUserMessage(sample, full));
assertEqual(userMessage.requestId, sample.requestId, 'user 消息回显 requestId');
assertEqual(userMessage.directionId, sample.directionId, 'user 消息回显 directionId');
assertEqual(JSON.stringify(userMessage.playerCopy), JSON.stringify(sample.playerCopy), 'user 消息携带 playerCopy');
assertEqual(JSON.stringify(userMessage.usedNames), JSON.stringify(sample.usedNames), 'user 消息携带 usedNames');
assertEqual(JSON.stringify(userMessage.reservedNames), JSON.stringify(full), 'user 消息携带完整保留名');
assertEqual(userMessage.playerCopy.trigger, sample.playerCopy.trigger, 'playerCopy 字段原样透传（不执行指令）');

// ---- 提示词完整性 ----
const prompt = t.NAME_SYSTEM_PROMPT;
assert(prompt.includes('PERSONA_NAME_STYLE_V1_1') === false, '提示词不泄漏内部版本标识');
assert(prompt.includes('留手谋划者'), '提示词含最新命名依据样本');
assert(prompt.includes('花色漫游者'), '提示词禁用历史别名');
assert(prompt.includes('reservedNames') && prompt.includes('usedNames'), '提示词引用保留名与已用名');
assert(prompt.includes('只输出合法JSON'), '提示词限定输出格式');

console.log(`通过 ${passed}，失败 ${failed}`);
process.exit(failed ? 1 : 0);
