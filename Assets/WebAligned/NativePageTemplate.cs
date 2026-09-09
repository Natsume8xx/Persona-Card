using UnityEngine;

namespace PersonaCards.WebAligned
{
    [DisallowMultipleComponent]
    public sealed class NativePageTemplate : MonoBehaviour
    {
        [Tooltip("界面绑定标识。可编辑子对象的位置、尺寸、字体、颜色和图片；不要修改此标识。")]
        public string PageId;
    }
}
