using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace PersonaCards.WebAligned
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class EnergyGraphic : MaskableGraphic
    {
        public Vector2 From,To;public float Progress;
        protected override void OnPopulateMesh(VertexHelper vh){vh.Clear();for(int layer=0;layer<4;layer++)for(int i=0;i<52;i++){float a=i/52f,b=(i+1)/52f;if(a>Progress||a<Progress-.45f)continue;var p=Point(a,layer);var q=Point(b,layer);var n=new Vector2(-(q-p).y,(q-p).x).normalized;float width=(layer==0?7:layer==1?3.2f:1.1f)*Mathf.Sin(Mathf.PI*Mathf.Clamp01((Progress-a)/.45f));var c=color;c.a=(layer==0?.12f:layer==1?.4f:.95f)*Mathf.Clamp01((1.3f-Progress)*4);int s=vh.currentVertCount;vh.AddVert(p+n*width,c,Vector2.zero);vh.AddVert(p-n*width,c,Vector2.zero);vh.AddVert(q-n*width,c,Vector2.zero);vh.AddVert(q+n*width,c,Vector2.zero);vh.AddTriangle(s,s+1,s+2);vh.AddTriangle(s,s+2,s+3);}}
        Vector2 Point(float t,int layer){var p=Vector2.Lerp(From,To,t);p.y-=Mathf.Sin(t*Mathf.PI)*(95+layer*13)+Mathf.Sin(t*20+Progress*8)*layer*4;return new Vector2(p.x-rectTransform.rect.width*.5f,rectTransform.rect.height*.5f-p.y);}
    }
}
