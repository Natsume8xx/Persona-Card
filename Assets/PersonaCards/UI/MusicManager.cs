using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PersonaCards.UI
{
    /// <summary>
    /// 游戏音乐管理器（单例组件，运行时首次访问自动创建，场景无需挂载、退出 Play 自动销毁）：
    /// - BGM：双 AudioSource 交叉淡入淡出（1 秒），loop 循环，同曲重复请求直接忽略；
    /// - SFX：独立 AudioSource PlayOneShot，可叠加且不打断 BGM；
    /// - 阶段同步：PrototypeFlowController.Render 每帧调用 SyncStage（幂等），曲目映射见 MusicCatalog。
    /// </summary>
    public sealed class MusicManager : MonoBehaviour
    {
        /// <summary>BGM 交叉淡入淡出时长（秒）。</summary>
        private const float CrossfadeSeconds = 1f;

        /// <summary>默认音量：BGM 略低避免盖过音效，SFX 全量。</summary>
        private const float DefaultBgmVolume = 0.7f;
        private const float DefaultSfxVolume = 0.9f;

        private static MusicManager _instance;

        /// <summary>单例访问：首次访问自动创建挂载对象（场景零改动）。</summary>
        public static MusicManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("Music Manager");
                    _instance = go.AddComponent<MusicManager>();
                    _instance.Initialize();
                }
                return _instance;
            }
        }

        /// <summary>双 BGM 音源（轮流担任「当前曲」与「淡入曲」，实现无缝隙交叉切换）。</summary>
        private readonly AudioSource[] _bgmSources = new AudioSource[2];

        /// <summary>当前正在播放的 BGM 音源下标（另一个为淡出/空闲音源）。</summary>
        private int _activeBgmIndex;

        /// <summary>SFX 专用音源（PlayOneShot 支持多音效叠加）。</summary>
        private AudioSource _sfxSource;

        /// <summary>当前 BGM 键（同曲幂等判断；含资源缺失时记录，避免每帧重复告警）。</summary>
        private string _currentBgmKey;

        /// <summary>进行中的淡入淡出协程（换曲时打断并从当前音量平滑过渡）。</summary>
        private Coroutine _crossfadeRoutine;

        private float _bgmVolume = DefaultBgmVolume;
        private float _sfxVolume = DefaultSfxVolume;

        /// <summary>当前正在播放的 BGM 键（调试 / 测试用）。</summary>
        public string CurrentBgmKey => _currentBgmKey;

        /// <summary>初始化三路音源：2 路循环 BGM + 1 路 SFX。</summary>
        private void Initialize()
        {
            for (var i = 0; i < _bgmSources.Length; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = true; // BGM 循环播放
                source.volume = 0f;
                _bgmSources[i] = source;
            }
            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;

            // 场景为纯 UI 布局（无相机、无 AudioListener），Unity 音频引擎没有监听器不运行——
            // 运行时自动补一个，音乐系统自包含，场景零改动
            if (FindObjectsOfType<AudioListener>().Length == 0)
            {
                gameObject.AddComponent<AudioListener>();
            }
        }

        /// <summary>
        /// 按阶段同步 BGM（由流程控制器每帧调用）：
        /// 映射为 null 的阶段（准备/奖励/结算等）延续当前曲不打断；同曲重复请求直接忽略。
        /// </summary>
        public void SyncStage(PrototypeFlowStage stage, bool isBossBattle)
        {
            var key = MusicCatalog.BgmKeyForStage(stage, isBossBattle);
            if (key != null) PlayBgm(key);
        }

        /// <summary>切换 BGM：同曲忽略；换曲则旧源 1 秒淡出、新源循环 1 秒淡入。</summary>
        public void PlayBgm(string key)
        {
            if (key == _currentBgmKey) return;
            var clip = MusicCatalog.BgmClipFor(key);
            if (clip == null)
            {
                // 记录当前键：既避免每帧重复告警，也保证资源到货后重启即生效（目录类已缓存 null）
                Debug.LogWarning($"[Music] BGM 资源缺失：{key}，保持静音直至资源到货。");
                _currentBgmKey = key;
                return;
            }

            var outgoing = _bgmSources[_activeBgmIndex];
            _activeBgmIndex = 1 - _activeBgmIndex; // 切到空闲音源作为淡入源
            var incoming = _bgmSources[_activeBgmIndex];
            incoming.clip = clip;
            incoming.volume = 0f;
            incoming.Play();
            _currentBgmKey = key;

            if (_crossfadeRoutine != null) StopCoroutine(_crossfadeRoutine);
            _crossfadeRoutine = StartCoroutine(CrossfadeRoutine(outgoing, incoming));
        }

        /// <summary>交叉淡入淡出：旧源从当前音量淡出到 0 并停止，新源淡入到 BGM 音量。</summary>
        private IEnumerator CrossfadeRoutine(AudioSource outgoing, AudioSource incoming)
        {
            var startOut = outgoing.volume; // 从打断时的实际音量继续淡出，换曲不跳变
            var elapsed = 0f;
            while (elapsed < CrossfadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / CrossfadeSeconds);
                outgoing.volume = (1f - t) * startOut;
                incoming.volume = t * _bgmVolume;
                yield return null;
            }
            outgoing.volume = 0f;
            outgoing.Stop();
            incoming.volume = _bgmVolume;
            _crossfadeRoutine = null;
        }

        /// <summary>播放一次性音效（不打断 BGM；多个 SFX 可叠加）。</summary>
        public void PlaySfx(string key)
        {
            var clip = MusicCatalog.SfxClipFor(key);
            if (clip == null)
            {
                Debug.LogWarning($"[Music] SFX 资源缺失：{key}");
                return;
            }
            _sfxSource.PlayOneShot(clip, _sfxVolume);
        }

        /// <summary>播放通用点击音效（按钮点击接线用，一行接入）。</summary>
        public void PlayClick()
        {
            PlaySfx(MusicCatalog.SfxClick);
        }

        /// <summary>
        /// 给一组按钮统一挂通用点击音效（onClick 叠加监听，不影响既有业务逻辑；null 元素静默跳过）。
        /// 供各控制器 Awake 时对静态按钮批量接线。
        /// </summary>
        public static void AttachClickSound(params Button[] buttons)
        {
            var manager = Instance;
            foreach (var button in buttons)
            {
                if (button == null) continue;
                button.onClick.AddListener(manager.PlayClick);
            }
        }

        /// <summary>设置 BGM 音量（0~1，立即同步到当前音源；供设置面板接入）。</summary>
        public void SetBgmVolume(float volume)
        {
            _bgmVolume = Mathf.Clamp01(volume);
            _bgmSources[_activeBgmIndex].volume = _bgmVolume;
        }

        /// <summary>设置 SFX 音量（0~1；供设置面板接入）。</summary>
        public void SetSfxVolume(float volume)
        {
            _sfxVolume = Mathf.Clamp01(volume);
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}
