using System.Collections.Generic;
using System.Linq;

namespace PersonaCards.Data
{
    /// <summary>
    /// 「图片配置」sheet 共享契约（P0-1D 提取）：供卡牌/将来人格牌等导入命令读取绑定 ID 集合做对照校验。
    /// （HandTypeTableContract 已含同名常量、P0-1C 已完结不重构，新契约从此处引用。）
    /// </summary>
    public static class ImageSheetContract
    {
        /// <summary>工作表名（图片配置）。</summary>
        public const string SheetName = "图片配置";

        /// <summary>绑定 ID 列（各配置 sheet 的 ID 列值对照此列校验，如 CARD_xxx/HAND_xx/PER_xxx）。</summary>
        public const string ColBindingId = "绑定ID";

        /// <summary>
        /// 从图片配置 sheet 行提取绑定 ID 集合（P0-1J 共享容错）：
        /// rows 为空或首行无「绑定ID」列（最新版配表已删该列）→ 返回 false 且 bindingIds 为 null，
        /// 调用方只发一条全局提示并跳过对照校验；否则返回非空值集合（可能为空集合）与 true。
        /// </summary>
        public static bool TryBuildBindingIds(List<Dictionary<string, string>> rows, out ICollection<string> bindingIds)
        {
            bindingIds = null;
            if (rows == null || rows.Count == 0)
            {
                return false;
            }

            var header = rows[0];
            if (!header.ContainsKey(ColBindingId))
            {
                return false;
            }

            bindingIds = new HashSet<string>(rows
                .Select(row => row.TryGetValue(ColBindingId, out var value) ? value : "")
                .Where(value => !string.IsNullOrEmpty(value)));
            return true;
        }
    }
}
