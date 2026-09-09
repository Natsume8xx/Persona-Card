using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;

namespace PersonaCards.WebAligned.Tests
{
    public class NativeGraphicsTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void CustomGraphicRendersInsideScrollMaskAndSurvivesRemoval(bool energy)
        {
            var canvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            try
            {
                canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
                viewport.transform.SetParent(canvas.transform, false);
                ((RectTransform)viewport.transform).sizeDelta = new Vector2(600, 400);
                var frame = new GameObject("Frame", typeof(RectTransform));
                frame.transform.SetParent(viewport.transform, false);
                ((RectTransform)frame.transform).sizeDelta = new Vector2(300, 200);
                MaskableGraphic graphic;
                if (energy)
                {
                    var trail = frame.AddComponent<EnergyGraphic>();
                    trail.From = new Vector2(20, 30);
                    trail.To = new Vector2(200, 150);
                    trail.Progress = .5f;
                    graphic = trail;
                }
                else graphic = frame.AddComponent<NativeFrameGraphic>();
                Assert.IsNotNull(frame.GetComponent<CanvasRenderer>(), "Custom graphics must supply their renderer before OnEnable.");
                Canvas.ForceUpdateCanvases();
                graphic.SetClipRect(new Rect(0, 0, 200, 200), true);
                graphic.Rebuild(CanvasUpdate.PreRender);
                var mesh = graphic.canvasRenderer.GetMesh();
                Assert.IsNotNull(mesh);
                Assert.Greater(mesh.vertexCount, 0);
                frame.SetActive(false);
                frame.SetActive(true);
                Canvas.ForceUpdateCanvases();
                Object.DestroyImmediate(frame);
                Canvas.ForceUpdateCanvases();
                LogAssert.NoUnexpectedReceived();
            }
            finally { Object.DestroyImmediate(canvas); }
        }
    }
}
