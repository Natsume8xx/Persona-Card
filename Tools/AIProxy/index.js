'use strict';
// 《人格牌》AI 人格选择代理（部署到腾讯云 SCF，Nodejs18.15）
//
// 职责：接收 JS 客户端（ai-persona-selection-client.js）发来的候选列表，
//       调用 DeepSeek 挑出 1 个候选 ID，原样回显 requestId。
// 契约（客户端 selectedIdFrom 严格校验，响应必须恰好两个字段）：
//   {"requestId": "<与请求完全一致>", "selectedCandidateId": "<候选ID>"}
// 模型失败/超时/非法 ID 时回空串 selectedCandidateId：
//   客户端本地校验会将其判为无效选择，自动走本地安全兜底（candidates[0]），玩家无感。
// 密钥只存在于本函数的环境变量 DEEPSEEK_API_KEY，绝不进客户端与仓库。

const BASE_URL = process.env.DEEPSEEK_BASE_URL || 'https://api.deepseek.com';
const MODEL = process.env.DEEPSEEK_MODEL || 'deepseek-chat';
const DEEPSEEK_TIMEOUT_MS = Number(process.env.DEEPSEEK_TIMEOUT_MS || 30000);

const DIRECTION_TEXT = {
  AI_DIRECTION_BRIDGE: '衔接：保留部分现有打法，同时连接另一条可用路线',
  AI_DIRECTION_BREAK: '打破：提供不依赖当前主路线的可触发解法',
  AI_DIRECTION_FOLLOW: '跟随：围绕已经稳定形成的主要打法继续强化',
};

const SYSTEM_PROMPT = [
  '你是《人格牌》卡牌游戏的 AI 人格选择器。',
  '玩家正在一局游戏中，游戏需要在成长节点为玩家生成一张贴合其行为风格的新人格牌。',
  '输入包含：',
  '- requestId：请求标识，必须原样回显',
  '- directionId：本节点生成方向（' + Object.entries(DIRECTION_TEXT).map(([k, v]) => k + '=' + v).join('；') + '）',
  '- behaviorSnapshot：玩家行为统计快照（打过的牌型、弃牌习惯、得分结构等）',
  '- candidates：一组已通过数值与规则校验的候选，每个含 id、playerCopy（玩家可见效果描述）、behaviorTags（行为标签）、budget（数值预算）',
  '你的任务：从 candidates 中挑选 1 个最贴合玩家行为与生成方向的候选。',
  '只输出 JSON，不要输出任何其他内容：{"requestId":"<与输入完全一致的requestId>","selectedCandidateId":"<候选id>"}',
].join('\n');

function httpResponse(statusCode, payload) {
  return {
    statusCode,
    headers: {
      'Content-Type': 'application/json; charset=utf-8',
      'Access-Control-Allow-Origin': '*',
      'Access-Control-Allow-Headers': 'Content-Type',
      'Access-Control-Allow-Methods': 'POST,OPTIONS',
    },
    body: JSON.stringify(payload),
  };
}

function parseBody(event) {
  if (!event || event.body === undefined || event.body === null) return {};
  if (typeof event.body === 'string') {
    try { return JSON.parse(event.body); } catch (e) { return {}; }
  }
  return event.body;
}

function parseRequest(request) {
  if (!request || typeof request !== 'object') return null;
  const { requestId, candidateIds } = request;
  if (typeof requestId !== 'string' || !requestId) return null;
  if (!Array.isArray(candidateIds) || !candidateIds.length) return null;
  return request;
}

async function pickViaDeepSeek(request) {
  const { requestId, candidateIds, candidates, directionId, behaviorSnapshot } = request;
  const apiKey = process.env.DEEPSEEK_API_KEY;
  if (!apiKey) throw new Error('DEEPSEEK_API_KEY not configured');
  const user = JSON.stringify({ directionId, behaviorSnapshot, candidates });
  const resp = await fetch(`${BASE_URL}/chat/completions`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${apiKey}` },
    body: JSON.stringify({
      model: MODEL,
      messages: [
        { role: 'system', content: SYSTEM_PROMPT },
        { role: 'user', content: user },
      ],
      response_format: { type: 'json_object' },
      temperature: 0.3,
      max_tokens: 512,
    }),
    signal: AbortSignal.timeout(DEEPSEEK_TIMEOUT_MS),
  });
  if (!resp.ok) throw new Error(`deepseek http ${resp.status}`);
  const data = await resp.json();
  const content = data && data.choices && data.choices[0] && data.choices[0].message && data.choices[0].message.content;
  if (typeof content !== 'string' || !content.trim()) throw new Error('deepseek empty content');
  const parsed = JSON.parse(content);
  if (typeof parsed.selectedCandidateId !== 'string') throw new Error('deepseek missing selectedCandidateId');
  if (!candidateIds.includes(parsed.selectedCandidateId)) throw new Error('deepseek invalid candidate id');
  return parsed.selectedCandidateId;
}

exports.main_handler = async (event) => {
  // API 网关/函数 URL 的 OPTIONS 预检直接放行
  if (event && event.httpMethod === 'OPTIONS') return httpResponse(200, {});
  const request = parseRequest(parseBody(event));
  if (!request) return httpResponse(400, { error: 'bad request' });
  let selectedCandidateId = '';
  try {
    selectedCandidateId = await pickViaDeepSeek(request);
  } catch (e) {
    console.error('deepseek selection failed:', e && e.message);
  }
  return httpResponse(200, { requestId: request.requestId, selectedCandidateId });
};
