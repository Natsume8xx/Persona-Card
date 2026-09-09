using UnityEngine;
using UnityEngine.UI;

namespace PersonaCards.WebAligned
{
    // Vertex-only finish: no generated textures, materials or per-frame updates.
    [DisallowMultipleComponent]
    public sealed class NativeSurfaceFinish : BaseMeshEffect
    {
        [Range(0, .15f)] public float Highlight = .045f;
        [Range(0, .5f)] public float BottomShade = .25f;
        public Color LightTint = new Color(1f, .77f, .42f, 1f);

        public override void ModifyMesh(VertexHelper mesh)
        {
            if (!IsActive()) return;
            var rect = ((RectTransform)transform).rect;
            if (rect.height <= 0) return;
            var vertex = new UIVertex();
            for (int i = 0; i < mesh.currentVertCount; i++)
            {
                mesh.PopulateUIVertex(ref vertex, i);
                var t = Mathf.Clamp01((vertex.position.y - rect.yMin) / rect.height);
                Color c = vertex.color;
                var shade = 1f - BottomShade * (1f - t);
                c.r = Mathf.Clamp01(c.r * shade + LightTint.r * Highlight * t);
                c.g = Mathf.Clamp01(c.g * shade + LightTint.g * Highlight * t);
                c.b = Mathf.Clamp01(c.b * shade + LightTint.b * Highlight * t);
                vertex.color = c; mesh.SetUIVertex(vertex, i);
            }
        }

        public static void ApplyPage(RectTransform page)
        {
            foreach (var image in page.GetComponentsInChildren<Image>(true))
            {
                var rect = image.rectTransform.rect;
                // Leave portraits, decorated buttons, masks and transparent hit areas alone.
                if (image.sprite != null || image.color.a < .5f || rect.width < 250 || rect.height < 65
                    || image.transform.Find("Frame") == null) continue;
                NativePageFactory.Ensure<NativeSurfaceFinish>(image.gameObject);
            }
            foreach (var face in page.GetComponentsInChildren<RawImage>(true))
            {
                if (face.name != "Face" || !face.transform.parent.name.StartsWith("Card ")) continue;
                if (face.GetComponent<Shadow>() != null) continue;
                var shadow = face.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0, 0, 0, .65f);
                shadow.effectDistance = new Vector2(3, -7);
                shadow.useGraphicAlpha = true;
            }
        }
    }
}
