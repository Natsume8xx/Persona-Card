using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace PersonaCards.WebAligned
{
    // 远程 AI 的唯一受控网络出口：以 ClearScript 宿主对象形式暴露 nativeNet.Fetch 给 host.js 的 fetch 包装。
    // 仅放行 JS 里写死的 Cloudflare Worker 地址，并把其重写为国内直连的腾讯云 SCF 代理（JS 与网页工程零改动），
    // 其余 URL 一律拒绝——保持「冻结的 JS 导出没有任意网络权限」的边界。
    public sealed class AiFetchHost
    {
        public const string WorkerEndpoint = "https://persona-card-ai-selector.dibajipa55.workers.dev";
        // 腾讯云 SCF 函数 URL（函数 URL 触发器，免鉴权）；运行时可用 PlayerPrefs ai-selector-endpoint 覆盖调试。
        public const string ProxyEndpointBase = "https://1480720213-3ej3hfkfqv.ap-guangzhou.tencentscf.com";

        readonly bool allowNetwork;
        // headless 测试（new NativeRules(false)）传 false：无网络、无 DeepSeek 依赖，成长节点走确定性本地兜底，
        // 日常回归不受远端波动影响；真实游戏路径（NativeGameView / persistence=true）传 true。
        public AiFetchHost(bool allowNetwork = true) { this.allowNetwork = allowNetwork; }

        static int inFlight;
        public static int InFlight => Volatile.Read(ref inFlight);

        public async Task<string> Fetch(string url, string body, double timeoutMs = 0)
        {
            var target = Resolve(url);
            if (target == null) throw new InvalidOperationException("AI 选择请求被拒：非白名单地址 " + url);
            Interlocked.Increment(ref inFlight);
            try
            {
                // 命名请求由 JS 侧透传 3 秒超时（规格 6.1）；选择请求沿用 20 秒
                var timeout = timeoutMs > 0 ? TimeSpan.FromMilliseconds(timeoutMs) : TimeSpan.FromSeconds(20);
                using (var client = new HttpClient { Timeout = timeout })
                {
                    var content = new StringContent(body ?? "", System.Text.Encoding.UTF8, "application/json");
                    using (var response = await client.PostAsync(target, content).ConfigureAwait(false))
                    {
                        var status = (int)response.StatusCode;
                        var ok = response.IsSuccessStatusCode;
                        var text = ok ? await response.Content.ReadAsStringAsync().ConfigureAwait(false) : null;
                        // 信封结构供 host.js fetch 包装解析为 {ok,status,json()}；body 保持原始 JSON 对象
                        return "{\"ok\":" + (ok ? "true" : "false") + ",\"status\":" + status + ",\"body\":" + (text ?? "null") + "}";
                    }
                }
            }
            catch (Exception)
            {
                throw; // → Task fault → JS promise reject → 客户端 NETWORK_ERROR → 本地安全兜底
            }
            finally
            {
                Interlocked.Decrement(ref inFlight);
            }
        }

        string Resolve(string url)
        {
            if (!allowNetwork) return null;
            if (!url.StartsWith(WorkerEndpoint, StringComparison.Ordinal)) return null;
            string endpoint;
            try { endpoint = PlayerPrefs.GetString("ai-selector-endpoint", ProxyEndpointBase); }
            catch (Exception) { endpoint = ProxyEndpointBase; }
            if (string.IsNullOrWhiteSpace(endpoint)) return null;
            return endpoint + url.Substring(WorkerEndpoint.Length);
        }
    }
}
