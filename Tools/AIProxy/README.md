# AI 人格选择代理（腾讯云 SCF）

## 职责

《人格牌》远程 AI 接入的服务端部分。JS 客户端（网页版与 Unity 版共用）在成长节点把
「行为快照 + 方向 + 本地合法候选列表」POST 到这里，本函数调 DeepSeek 挑 1 个候选 ID
并回显 requestId。DeepSeek 密钥只存在于函数环境变量，绝不进客户端与仓库。

## 契约（与 JS 客户端 selectedIdFrom 严格对齐）

- 响应必须**恰好两个字段**：`{"requestId": "<原样回显>", "selectedCandidateId": "<候选ID>"}`
- 模型失败/超时/非法 ID → `selectedCandidateId` 回空串，客户端本地校验判无效并走本地安全兜底

## 部署

```bash
# 凭据只从环境变量读取（CAM 子账号，仅授 SCF 权限）
export TENCENT_SECRET_ID=...
export TENCENT_SECRET_KEY=...
export TENCENT_REGION=ap-guangzhou     # 可选，默认广州
export DEEPSEEK_API_KEY=sk-...         # 部署时写入函数环境变量

npm install
node deploy.cjs --list   # 只读验证凭据
node ensure-role.cjs     # 账号首次使用 SCF 时补建默认执行角色（需子账号有 cam:CreateRole；否则开一次 SCF 控制台自动创建）
node deploy.cjs          # 创建/更新函数
```

部署后到控制台「触发管理」手动添加一次「函数 URL」触发器（免鉴权）获取公网地址；
函数 URL 触发器没有可用 API（SDK CreateTrigger 不支持 functionurl 类型），只能控制台操作一次，
之后脚本更新代码不受影响。

## 安全约定

- `DEEPSEEK_API_KEY` 只进云函数环境变量；本目录的 `.gitignore` 已排除本地密钥文件
- 部署完成后建议轮换/禁用 CAM 凭据

## 接入 Unity

Unity 侧不直连本地址：C# 的 fetch 宿主对象会把 JS 里写死的 Cloudflare Worker 地址
重写为本代理地址（URL 从 C# 配置读取），JS 与网页工程零改动。
