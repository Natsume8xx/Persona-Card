using UnityEngine;

namespace PersonaCards.UI
{
    /// <summary>
    /// 运行时字体工厂（WebGL 兼容）：桌面平台用 OS 字体（微软雅黑等），
    /// WebGL 无 OS 字体访问（CreateDynamicFontFromOSFont 不工作，文字会全部丢失），
    /// 回落到内置 LegacyRuntime.ttf（实测覆盖中文，构建零附加体积）。
    /// 各 View/控制器 Awake 统一从这里取字体，替代直接 CreateDynamicFontFromOSFont。
    /// </summary>
    public static class RuntimeFontFactory
    {
        private static Font _osFont;
        private static Font _fallbackFont;

        /// <summary>取运行时中文字体：桌面 OS 字体优先；WebGL 或创建失败回落内置字体。size 仅首次创建时生效。</summary>
        public static Font GetRuntimeFont(int size)
        {
#if UNITY_WEBGL
            return GetFallbackFont();
#else
            if (_osFont == null)
            {
                _osFont = Font.CreateDynamicFontFromOSFont(
                    new[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimHei", "Arial" }, size);
            }
            return _osFont != null ? _osFont : GetFallbackFont();
#endif
        }

        /// <summary>内置 LegacyRuntime.ttf（Unity 6 内建字体，编辑器实测 HasCharacter('人') == true）。</summary>
        private static Font GetFallbackFont()
        {
            if (_fallbackFont == null)
                _fallbackFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return _fallbackFont;
        }
    }
}
