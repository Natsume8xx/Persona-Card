'use strict';
// 《人格牌》AI 选择代理：腾讯云 SCF 部署脚本
// 凭据只从环境变量读取（TENCENT_SECRET_ID / TENCENT_SECRET_KEY），绝不写入文件与仓库。
// 用法：
//   node deploy.cjs --list    # 只读：列出现有函数与命名空间（验证凭据是否可用）
//   node deploy.cjs           # 创建/更新函数 + 确保公网触发器
// 环境变量：
//   TENCENT_REGION      默认 ap-guangzhou
//   SCF_FUNCTION_NAME   默认 persona-card-ai-selector
//   DEEPSEEK_API_KEY    部署时写入函数环境变量（可留空，之后单独更新）
//   DEEPSEEK_BASE_URL / DEEPSEEK_MODEL  可选覆盖
const path = require('path');
const AdmZip = require('adm-zip');
const tencentcloud = require('tencentcloud-sdk-nodejs');

const REGION = process.env.TENCENT_REGION || 'ap-guangzhou';
const FUNCTION_NAME = process.env.SCF_FUNCTION_NAME || 'persona-card-ai-selector';
const NAMESPACE = process.env.SCF_NAMESPACE || 'default';
// SCF 执行角色（控制台首次使用云函数时自动创建的默认角色）
const ROLE = process.env.SCF_ROLE || 'SCF_QcsRole';

const ScfClient = tencentcloud.scf.v20180416.Client;

function client() {
  return new ScfClient({
    credential: {
      secretId: process.env.TENCENT_SECRET_ID || '',
      secretKey: process.env.TENCENT_SECRET_KEY || '',
    },
    region: REGION,
    profile: { httpProfile: { endpoint: 'scf.tencentcloudapi.com', reqTimeout: 60 } },
  });
}

function zipBase64() {
  const zip = new AdmZip();
  zip.addLocalFile(path.join(__dirname, 'index.js'));
  return zip.toBuffer().toString('base64');
}

function envVars() {
  const vars = [];
  if (process.env.DEEPSEEK_API_KEY) vars.push({ Key: 'DEEPSEEK_API_KEY', Value: process.env.DEEPSEEK_API_KEY });
  if (process.env.DEEPSEEK_BASE_URL) vars.push({ Key: 'DEEPSEEK_BASE_URL', Value: process.env.DEEPSEEK_BASE_URL });
  if (process.env.DEEPSEEK_MODEL) vars.push({ Key: 'DEEPSEEK_MODEL', Value: process.env.DEEPSEEK_MODEL });
  return vars;
}

const FUNCTION_CONFIG = {
  Description: '《人格牌》AI 人格选择代理：DeepSeek 服务端转发，密钥仅在函数环境变量',
  Handler: 'index.main_handler',
  Runtime: 'Nodejs18.15',
  MemorySize: 256,
  Timeout: 60,
  Role: ROLE,
};

async function listAll(scf) {
  try {
    const namespaces = await scf.ListNamespaces({ Limit: 100 });
    console.log('[list] 命名空间：', (namespaces.Namespaces || []).map((n) => n.Name).join(', ') || '(空)');
  } catch (e) {
    console.warn('[list] 命名空间查询失败：', e && e.code, e && e.message);
  }
  const functions = await scf.ListFunctions({ Namespace: NAMESPACE, Limit: 100, Offset: 0 });
  const items = functions.Functions || [];
  if (!items.length) {
    console.log('[list] 函数：无');
    return;
  }
  for (const f of items) {
    console.log(`[list] ${f.FunctionName}  ${f.Runtime || ''}  ${f.Status || ''}  Modified=${f.ModifiedTime || f.AddTime || ''}`);
    try {
      const triggers = await scf.ListTriggers({ FunctionName: f.FunctionName, Namespace: NAMESPACE, Limit: 100 });
      const descs = (triggers.Triggers || []).map((t) => `${t.Type || ''}(${t.TriggerName || ''})`);
      if (descs.length) console.log(`       触发器：${descs.join('；')}`);
    } catch (e) {
      // 触发器查询失败不阻塞列表
    }
  }
}

async function ensurePublicTrigger(scf) {
  const existing = await scf.ListTriggers({ FunctionName: FUNCTION_NAME, Namespace: NAMESPACE, Limit: 100 });
  const triggers = existing.Triggers || [];
  if (triggers.some((t) => t.Type === 'functionurl' || t.Type === 'http')) {
    console.log('[deploy] 已存在函数 URL 触发器');
    return;
  }
  if (triggers.some((t) => t.Type === 'apigw')) {
    console.log('[deploy] 已存在 API 网关触发器');
    return;
  }
  // 函数 URL 触发器没有可用的 API（本 SDK 版本 CreateTrigger 不支持 functionurl 类型，
  // 实测返回 InvalidParameterValue.Type），只能控制台手动添加一次，之后脚本更新代码不受影响。
  console.warn('[deploy] 尚未配置公网触发器。请在控制台「触发管理」手动添加「函数 URL」触发器（免鉴权），获取公网地址。');
}

// 执行角色只在函数访问腾讯云资源（COS/DB 等）时才必需；本函数仅出网调用 DeepSeek。
// 账号首次使用 SCF 前不存在默认角色 SCF_QcsRole（要开一次控制台才会自动创建），
// 因此这里自动回退为「无角色创建」，跳过控制台激活这一步。
// 另注：UpdateFunctionConfiguration 实测不识别 Handler 参数（UnknownParameter），更新配置时须剔除。
let roleOmitted = false;
function createConfig(skipRole) {
  const config = { ...FUNCTION_CONFIG };
  if (skipRole || roleOmitted) delete config.Role;
  return config;
}
function updateConfig() {
  const config = { ...FUNCTION_CONFIG };
  delete config.Handler;
  if (roleOmitted) delete config.Role;
  return config;
}

async function waitUntilActive(scf, base) {
  for (let i = 0; i < 18; i++) {
    try {
      const info = await scf.GetFunction(base);
      if (info.Status === 'Active') return;
    } catch (e) {
      // 状态查询失败不阻塞，继续等待
    }
    await new Promise((resolve) => setTimeout(resolve, 10000));
  }
  throw new Error('函数长时间未进入 Active 状态');
}

async function deploy(scf) {
  const base = { FunctionName: FUNCTION_NAME, Namespace: NAMESPACE };
  let created = false;
  try {
    await scf.CreateFunction({ ...base, ...createConfig(false), Environment: { Variables: envVars() }, Code: { ZipFile: zipBase64() } });
    created = true;
    console.log('[deploy] 函数已创建：', FUNCTION_NAME);
  } catch (e) {
    if (e && (e.code === 'ResourceInUse.FunctionName' || e.code === 'ResourceInUse.Function')) {
      console.log('[deploy] 函数已存在，更新代码与配置…');
    } else if (e && e.code === 'ResourceNotFound.Role') {
      console.warn('[deploy] 默认执行角色不存在，回退为无角色创建…');
      roleOmitted = true;
      await scf.CreateFunction({ ...base, ...createConfig(true), Environment: { Variables: envVars() }, Code: { ZipFile: zipBase64() } });
      created = true;
      console.log('[deploy] 函数已创建（无执行角色）：', FUNCTION_NAME);
    } else {
      throw e;
    }
  }
  if (!created) {
    await scf.UpdateFunctionCode({ ...base, Code: { ZipFile: zipBase64() } });
    // 代码更新后函数进入 Updating，配置更新须等待其回到 Active，否则 FailedOperation.UpdateFunctionConfiguration
    await waitUntilActive(scf, base);
    await scf.UpdateFunctionConfiguration({ ...base, ...updateConfig(), Environment: { Variables: envVars() } });
    console.log('[deploy] 代码与配置已更新：', FUNCTION_NAME);
  }
  await ensurePublicTrigger(scf);
  console.log('[deploy] 完成。公网地址见控制台「触发管理」（函数 URL / API 网关）。');
}

async function main() {
  const mode = process.argv[2] || '--deploy';
  if (!process.env.TENCENT_SECRET_ID || !process.env.TENCENT_SECRET_KEY) {
    console.error('[error] 缺少 TENCENT_SECRET_ID / TENCENT_SECRET_KEY 环境变量');
    process.exit(2);
  }
  const scf = client();
  if (mode === '--list') return listAll(scf);
  return deploy(scf);
}

main().catch((e) => {
  console.error('[error]', (e && e.code) || e);
  process.exit(1);
});
