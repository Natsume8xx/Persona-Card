'use strict';
// 创建 SCF 默认执行角色 SCF_QcsRole。
// 背景：账号首次使用云函数前该角色不存在（正常需开一次 SCF 控制台自动创建），
//       CreateFunction 无论是否显式传 Role 都会要求它存在。这里用 CAM API 补建。
// 用法（凭据只从环境变量读取）：
//   node ensure-role.cjs
const tencentcloud = require('tencentcloud-sdk-nodejs');

const REGION = process.env.TENCENT_REGION || 'ap-guangzhou';
const CamClient = tencentcloud.cam.v20190116.Client;

// 信任策略：允许云函数服务代扮演该角色
const TRUST = JSON.stringify({
  version: '2.0',
  statement: [{
    action: 'name/sts:AssumeRole',
    effect: 'allow',
    principal: { service: ['scf.qcloud.com'] },
  }],
});

// 预设策略 QcloudSCFFullAccess（云函数全读写）的固定 PolicyId
const SCF_FULL_ACCESS_POLICY_ID = 28341897;

function client() {
  return new CamClient({
    credential: {
      secretId: process.env.TENCENT_SECRET_ID || '',
      secretKey: process.env.TENCENT_SECRET_KEY || '',
    },
    region: REGION,
    profile: { httpProfile: { endpoint: 'cam.tencentcloudapi.com', reqTimeout: 30 } },
  });
}

async function main() {
  if (!process.env.TENCENT_SECRET_ID || !process.env.TENCENT_SECRET_KEY) {
    console.error('[error] 缺少 TENCENT_SECRET_ID / TENCENT_SECRET_KEY 环境变量');
    process.exit(2);
  }
  const cam = client();
  const existing = await cam.DescribeRoleList({ Page: 1, Rp: 200 });
  const roles = existing.List || [];
  console.log('[role] 现有角色：', roles.map((r) => r.RoleName).join(', ') || '(空)');
  if (roles.some((r) => r.RoleName === 'SCF_QcsRole')) {
    console.log('[role] SCF_QcsRole 已存在，无需创建');
    return;
  }
  await cam.CreateRole({
    RoleName: 'SCF_QcsRole',
    PolicyDocument: TRUST,
    Description: '云函数（SCF）默认操作角色',
    ConsoleLogin: 0,
  });
  console.log('[role] SCF_QcsRole 已创建');
  await cam.AttachRolePolicy({ PolicyId: SCF_FULL_ACCESS_POLICY_ID, AttachRoleName: 'SCF_QcsRole' });
  console.log('[role] 已附加 QcloudSCFFullAccess 预设策略');
}

main().catch((e) => {
  console.error('[error]', e && e.code, e && e.message);
  process.exit(1);
});
