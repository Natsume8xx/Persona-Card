using UnityEngine;

namespace PersonaCards.UI
{
    /// <summary>
    /// 运行时字体工厂（WebGL 兼容）：桌面平台用 OS 字体（微软雅黑等，Windows 打包不受影响）；
    /// WebGL 无 OS 字体访问（CreateDynamicFontFromOSFont 不工作，文字会全部丢失），
    /// 改用随包 Resources 字体（Arial Unicode，TTF 格式含 CJK；OTF/CFF 在 WebGL 播放器实测缺失，
    /// 已弃用 Noto Sans CJK OTF 版）。
    /// LegacyRuntime.ttf 仅作最终兜底（其播放器版 CJK 覆盖未经验证，曾疑似 WebGL 中文缺失根源）。
    /// 各 View/控制器 Awake 统一从这里取字体，替代直接 CreateDynamicFontFromOSFont。
    /// </summary>
    public static class RuntimeFontFactory
    {
        /// <summary>随包中文字体资源路径（Assets/PersonaCards/Resources/ 相对路径，无扩展名）。TTF 格式（WebGL 播放器对 TTF 支持成熟，OTF/CFF 有兼容风险）。</summary>
        public const string BundledFontPath = "Fonts/ArialUnicode";

        private static Font _osFont;
        private static Font _bundledFont;
        private static Font _fallbackFont;

        /// <summary>取运行时中文字体：桌面 OS 字体优先；WebGL 用随包字体，加载失败回落内置字体。size 仅首次创建时生效。</summary>
        public static Font GetRuntimeFont(int size)
        {
#if UNITY_WEBGL
            return GetBundledFont();
#else
            if (_osFont == null)
            {
                _osFont = Font.CreateDynamicFontFromOSFont(
                    new[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimHei", "Arial" }, size);
            }
            return _osFont != null ? _osFont : GetBundledFont();
#endif
        }

        /// <summary>随包 Resources 字体（WebGL 主用；桌面 OS 字体创建失败时兜底）。</summary>
        private static Font GetBundledFont()
        {
            if (_bundledFont == null)
                _bundledFont = Resources.Load<Font>(BundledFontPath);
            return _bundledFont != null ? _bundledFont : GetFallbackFont();
        }

        /// <summary>内置 LegacyRuntime.ttf（Unity 6 内建字体，编辑器实测 HasCharacter('人') == true；播放器版未验证）。</summary>
        private static Font GetFallbackFont()
        {
            if (_fallbackFont == null)
                _fallbackFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return _fallbackFont;
        }
    }
}
