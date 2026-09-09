using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace PersonaCards.WebAligned
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class NativeFrameGraphic : MaskableGraphic
    {
        public float Thickness=1;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;Border(vh,r,Thickness,1);
            if(r.width<250||r.height<90||color.a==0)return;
            Border(vh,new Rect(r.xMin+4,r.yMin+4,r.width-8,r.height-8),.6f,.3f);
            foreach(float x in new[]{r.xMin+5,r.xMax-5})foreach(float y in new[]{r.yMin+5,r.yMax-5})
            {
                int n=vh.currentVertCount;var c=color;c.a*=.75f;
                vh.AddVert(new Vector3(x-3,y),c,Vector2.zero);vh.AddVert(new Vector3(x,y-3),c,Vector2.zero);vh.AddVert(new Vector3(x+3,y),c,Vector2.zero);vh.AddVert(new Vector3(x,y+3),c,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
            }
        }
        void Border(VertexHelper v,Rect r,float t,float alpha){Add(v,r.xMin,r.yMin,r.width,t,alpha);Add(v,r.xMin,r.yMax-t,r.width,t,alpha);Add(v,r.xMin,r.yMin,t,r.height,alpha);Add(v,r.xMax-t,r.yMin,t,r.height,alpha);}
        void Add(VertexHelper v,float x,float y,float w,float h,float alpha){int n=v.currentVertCount;var c=color;c.a*=alpha;v.AddVert(new Vector3(x,y),c,Vector2.zero);v.AddVert(new Vector3(x+w,y),c,Vector2.zero);v.AddVert(new Vector3(x+w,y+h),c,Vector2.zero);v.AddVert(new Vector3(x,y+h),c,Vector2.zero);v.AddTriangle(n,n+1,n+2);v.AddTriangle(n,n+2,n+3);}
    }
}
