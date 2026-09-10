# AI 接口文档（人格选择）

> 本文档面向后续接手远程 AI 模块的开发同学。部署细节以 `Tools/AIProxy/README.md` 为准，本文只讲设计与使用。

## 一、总览

成长节点（N04 / N08 / N12）会请求远程 AI：把「行为快照 + 成长方向 + 本地合法候选列表」发给 DeepSeek，由它挑出 1 个候选 ID。这个能力由网页版 JS 客户端实现、服务端由队友的 Cloudflare Worker 提供；Unity 版复用了前者、把后者替换成了自建的腾讯云 SCF 代理（国内直连免 VPN）。

三条设计原则：

1. **网页版零改动**：JS 客户端与接口契约原样复用，地址重写在 Unity 的 C# 侧完成。
2. **密钥绝不下发**：DeepSeek 密钥只存在于云函数环境变量，客户端与仓库里都没有。
3. **失败安全兜底**：网络失败 / 超时 / 非法响应一律回落本地生成（取候选列表第一项），玩家无感。

## 二、架构与数据流

```
网页版 JS 客户端（冻结导出，零改动）
   │   POST 请求体（原地址：Cloudflare Worker，代码里写死）
   ▼
host.js 的 fetch 包装（唯一网络例外）
   │   nativeNet.Fetch(url, body)
   ▼
C# AiFetchHost（游戏内唯一网络出口）
   │   白名单校验 → URL 重写为腾讯云 SCF 函数 URL → POST
   ▼
腾讯云 SCF 代理（Nodejs18.15，广州）
   │   转发 DeepSeek，校验返回的候选 ID 在本地候选列表内
   ▼
DeepSeek API（chat/completions）
```

响应原路返回。全程只有一个网络出口（`nativeNet.Fetch`），其余 URL 一律拒绝——冻结的 JS 导出没有任意网络权限。

## 三、接口契约

请求体由网页版 JS 客户端生成，服务端与 Unity 侧都不解析其内部结构（只原样转发），此处不展开。

**响应必须恰好两个字段**，多一个、少一个、类型不对都会被客户端判为无效：

```json
{
  "requestId": "<原样回显请求里的 requestId>",
  "selectedCandidateId": "<从本地合法候选中挑出的 1 个 ID>"
}
```

- `selectedCandidateId` 不在本地候选列表内 → 判无效。
- 模型失败 / 超时 / 判无效 → 客户端走本地安全兜底（`LOCAL_FALLBACK`）。

## 四、Unity 侧设计

| 文件 | 职责 |
|---|---|
| `Assets/WebAligned/AiFetchHost.cs` | ClearScript 宿主对象。只放行写死的 Worker 地址，并把它重写为 SCF 函数 URL（可被 `PlayerPrefs["ai-selector-endpoint"]` 覆盖，置空即禁用网络）；20 秒超时；返回 `{ok,status,body}` 信封供 JS 包装解析 |
| `Assets/WebAligned/NativeRules.cs` | 注册 `nativeNet` 宿主对象。**必须**开 `V8ScriptEngineFlags.EnableTaskPromiseConversion`——ClearScript 默认不把宿主 `Task<string>` 转成 JS Promise，不开的话 fetch 会瞬间落入 `HTTP_0` 兜底（实测踩过的坑）。headless 模式（`new NativeRules(false)`，测试与编辑器迁移用）不传网络权限，走确定性本地兜底 |
| `Assets/WebAligned/NativeGameView.AiPump.cs` | 请求在途及其后 3 秒内低频对比 JS 快照，捕获 V8 线程上异步打开的人格成长弹窗并重渲染 |
| `Tools/WebMigration/host.js` | 末尾的 `fetch` 包装（→ `nativeNet.Fetch`）。改完后需 `cp` 同步到 `Assets/WebAligned/Resources/WebCore/host.txt` |

**传输状态**（存档事件里可见）：`REMOTE_RESPONSE`（远程成功）、`LOCAL_FALLBACK`（本地兜底）、`NETWORK_ERROR`、`TIMEOUT`、`HTTP_<状态码>`。

## 五、服务端

- 代码与部署脚本在 `Tools/AIProxy/`，凭据只从环境变量读取（详见该目录 README）。
- 函数 URL 触发器（免鉴权）没有可用 API，只能在控制台手动创建一次；之后脚本更新代码不受影响。
- 密钥安全约定：`DEEPSEEK_API_KEY` 只进函数环境变量；部署完成后建议轮换 / 禁用 CAM 凭据。

## 六、使用与调试

- **正常游玩**：零配置，成长节点自动走远程 AI。
- **验证口径**：开发者模式下成长弹窗会显示「AI调用：DeepSeek 已参与选择」；存档事件记录 `REMOTE_RESPONSE` / `LOCAL_FALLBACK`。
- **调试覆盖**：`PlayerPrefs.SetString("ai-selector-endpoint", "")` 可强制关闭网络（等价于永远走本地兜底）。
- **回归测试**：headless 测试全部走本地兜底，不依赖网络与 DeepSeek 可用性；真实链路的端到端验证是临时测试（跑通后已删），改动本模块后可按「打到 N04 断言 REMOTE_RESPONSE」的口径重做。

## 七、常见问题

| 现象 | 原因与处理 |
|---|---|
| 存档里出现 `HTTP_0` | fetch 包装拿到的不是字符串响应。确认 `EnableTaskPromiseConversion` flag 是否被误删 |
| 一直 `LOCAL_FALLBACK` | 检查 SCF 函数状态是否为 Active、函数 URL 是否可访问（curl 试 POST）、DeepSeek 环境变量是否还在 |
| 云函数返回 403/400 | 函数 URL 授权类型应为「开放」；请求体为空（如 `{}`）会被函数校验拒绝，属预期 |
