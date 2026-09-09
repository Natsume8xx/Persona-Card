using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PersonaCards.UI.Editor
{
    /// <summary>
    /// WebGL 字体预烘焙工具（WebGL 播放器动态字体渲染 CJK 实测不可靠：LegacyRuntime/随包 OTF/随包 TTF
    /// 三种动态方案均缺字）。方案：扫描项目全部文本字符（代码/ScriptableObject/prefab/场景），
    /// 写入字体导入器的 m_CustomCharacters（Custom Set 静态字形），构建时烘焙字形纹理，
    /// 播放器直接采样，完全绕开运行时动态渲染。
    /// 用法：打包 WebGL 前跑菜单 Persona Cards/Bake WebGL Font（幂等，可反复跑）。
    /// </summary>
    public static class FontBakeTool
    {
        private const string FontPath = "Assets/PersonaCards/Resources/Fonts/ArialUnicode.ttf";

        [MenuItem("Persona Cards/Bake WebGL Font")]
        public static void Bake()
        {
            var chars = ScanProjectCharacters();
            if (chars.Length == 0)
            {
                Debug.LogError("FontBakeTool: 扫描字符集为空，中止");
                return;
            }

            var importer = AssetImporter.GetAtPath(FontPath);
            if (importer == null)
            {
                Debug.LogError("FontBakeTool: importer not found at " + FontPath);
                return;
            }
            var so = new SerializedObject(importer);
            var prop = so.FindProperty("m_CustomCharacters");
            if (prop == null)
            {
                Debug.LogError("FontBakeTool: m_CustomCharacters 字段不存在");
                return;
            }
            prop.stringValue = chars;
            so.ApplyModifiedProperties();
            AssetDatabase.ImportAsset(FontPath, ImportAssetOptions.ForceSynchronousImport);

            // 验证静态字形已烘焙
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            var tex = font.material != null ? font.material.mainTexture : null;
            Debug.Log("FontBakeTool: 字符集 " + CountNonAscii(chars) + " 个非 ASCII 字符（含 ASCII 共 " + chars.Length + " 个）已写入 Custom Set；"
                + "bakedGlyphs=" + (font != null ? font.characterInfo.Length : -1)
                + " / tex=" + (tex != null ? tex.width + "x" + tex.height : "NONE")
                + " / HasCharacter('人')=" + (font != null && font.HasCharacter('人')));
        }

        /// <summary>扫描 Assets 下全部文本资产（.cs/.asset/.prefab/.unity），收集非 ASCII 字符；ASCII 32~126 全量兜底。</summary>
        private static string ScanProjectCharacters()
        {
            var set = new SortedSet<char>();
            for (char c = (char)32; c <= 126; c++) set.Add(c);
            var files = Directory.GetFiles(Application.dataPath, "*.*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext != ".cs" && ext != ".asset" && ext != ".prefab" && ext != ".unity") continue;
                try
                {
                    var content = File.ReadAllText(file);
                    foreach (var c in content)
                        if (c > 126) set.Add(c);
                }
                catch
                {
                    // 忽略不可读文件（权限/二进制误判）
                }
            }
            var sb = new StringBuilder(set.Count);
            foreach (var c in set) sb.Append(c);
            return sb.ToString();
        }

        private static int CountNonAscii(string s)
        {
            int count = 0;
            foreach (var c in s)
                if (c > 126) count++;
            return count;
        }
    }
}
