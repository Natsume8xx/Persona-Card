using Newtonsoft.Json.Linq;
using UnityEngine;

namespace PersonaCards.WebAligned
{
    public sealed partial class NativeGameView
    {
        // 远程 AI 异步泵：请求在途及其后 3 秒内，低频对比 JS 快照，
        // 捕获 V8 线程上异步打开的人格成长弹窗（openPersonaGrowth 的 thenable 路径）并重渲染。
        // 无请求时零开销（窗口不激活）；请求完成后的续跑窗口覆盖 JS 续体落地的延迟。
        float aiPumpActiveUntil, nextAiPumpAt;

        void AiPumpTick()
        {
            if (state == null) return;
            var now = Time.realtimeSinceStartup;
            if (AiFetchHost.InFlight > 0) aiPumpActiveUntil = now + 3f;
            if (now >= aiPumpActiveUntil || now < nextAiPumpAt) return;
            nextAiPumpAt = now + .1f;
            if (busy || settingsOpen) return; // 交互/设置中不抢渲染，窗口仍在，下一拍再试
            var next = core.Snapshot();
            if (JToken.DeepEquals(next, state)) return;
            state = next;
            Render();
        }
    }
}
