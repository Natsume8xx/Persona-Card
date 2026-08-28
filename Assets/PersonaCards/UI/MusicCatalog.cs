using System.Collections.Generic;
using UnityEngine;

namespace PersonaCards.UI
{
    /// <summary>
    /// 游戏音乐目录（音乐系统）：
    /// 集中管理 BGM / SFX 的资源键、阶段→曲目映射与 AudioClip 加载缓存。
    /// 音频文件位于 Assets/PersonaCards/Resources/Music/{key}.wav（策划资源 YY/游戏音乐 的 wav 源文件，
    /// 英文文件名保证代码路径稳定；导入设置统一 CompressedInMemory + Vorbis + 后台加载）。
    ///
    /// 曲目安排（策划指定）：
    ///   battle-1（对战1）：普通战斗、商店页面
    ///   battle-2（对战2）：主界面、人格铸牌阶段（PersonaForge / 中局铸牌 PersonaGen）
    ///   battle-3（对战3）：Boss 战（揭示界面 + Battle 阶段的 Boss 节点）
    /// 未显式映射的阶段（准备/奖励/结算/失败结算）返回 null，由播放器延续当前曲不打断——
    /// 准备屏从主菜单进入自然延续 battle-2，奖励屏延续刚打完的战斗曲。
    ///
    /// SFX 按文件名语义接线（策划指定）：
    ///   click=按钮/点牌（MusicManager.AttachClickSound 统一挂）；draw=出牌/弃牌后补牌；discard=弃牌；
    ///   score-count=出牌结算事件演示；victory/defeat=战斗胜利/失败结算；
    ///   coin=金币获取（游戏当前无金币获取事件，预留待金币系统实装后接线）。
    /// </summary>
    public static class MusicCatalog
    {
        // ---- BGM 键（对应 Resources/Music/{key}.wav）----
        public const string BgmBattle1 = "battle-1"; // 对战1：普通战斗、商店
        public const string BgmBattle2 = "battle-2"; // 对战2：主界面、人格铸牌
        public const string BgmBattle3 = "battle-3"; // 对战3：Boss 战

        // ---- SFX 键（按文件名语义接线，coin 预留待金币系统实装）----
        public const string SfxClick = "click";             // 点击（通用按钮）
        public const string SfxDraw = "draw";               // 抽牌
        public const string SfxDiscard = "discard";         // 弃牌
        public const string SfxCoin = "coin";               // 金币获取
        public const string SfxScoreCount = "score-count";  // 分数计算
        public const string SfxVictory = "victory";         // 胜利
        public const string SfxDefeat = "defeat";           // 失败

        /// <summary>Resources 相对路径前缀（不带扩展名）。</summary>
        private const string ResourcePrefix = "Music/";

        /// <summary>加载缓存：资源键 → AudioClip（含加载失败的 null 结果，避免重复 Resources.Load）。</summary>
        private static readonly Dictionary<string, AudioClip> ClipCache = new Dictionary<string, AudioClip>();

        /// <summary>
        /// 阶段 → BGM 键。返回 null 表示该阶段不换曲（延续当前 BGM）。
        /// Battle 阶段同一枚举承载普通战斗与 Boss 战，需按路线节点类型区分。
        /// </summary>
        public static string BgmKeyForStage(PrototypeFlowStage stage, bool isBossBattle)
        {
            switch (stage)
            {
                case PrototypeFlowStage.MainMenu:
                case PrototypeFlowStage.PersonaForge:
                case PrototypeFlowStage.PersonaGen:
                    return BgmBattle2;
                case PrototypeFlowStage.Battle:
                    return isBossBattle ? BgmBattle3 : BgmBattle1;
                case PrototypeFlowStage.Shop:
                    return BgmBattle1;
                case PrototypeFlowStage.BossReveal:
                    return BgmBattle3;
                // PersonaSetup / Reward / RunReport / FailureResult：延续当前曲，不打断
                default:
                    return null;
            }
        }

        /// <summary>按键加载 BGM 音频；缺失返回 null（播放器静默跳过并告警）。</summary>
        public static AudioClip BgmClipFor(string key)
        {
            return LoadCached(key);
        }

        /// <summary>按键加载 SFX 音频；缺失返回 null（播放器静默跳过并告警）。</summary>
        public static AudioClip SfxClipFor(string key)
        {
            return LoadCached(key);
        }

        private static AudioClip LoadCached(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (ClipCache.TryGetValue(key, out var cached)) return cached;
            var clip = Resources.Load<AudioClip>(ResourcePrefix + key);
            ClipCache[key] = clip;
            return clip;
        }
    }
}
