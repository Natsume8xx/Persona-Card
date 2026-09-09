using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace PersonaCards.WebAligned
{
    public sealed class NativeEnergyTrail : MonoBehaviour
    {
        EnergyGraphic graphic;
        public float Progress=>graphic!=null?graphic.Progress:1;
        public void Initialize(RectTransform parent,Vector2 from,Vector2 to,Color color){var go=new GameObject("Persona energy",typeof(RectTransform));var r=(RectTransform)go.transform;r.SetParent(parent,false);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;graphic=go.AddComponent<EnergyGraphic>();graphic.From=from;graphic.To=to;graphic.color=color;graphic.raycastTarget=false;}
        public void SetProgress(float progress){if(graphic==null)return;graphic.Progress=Mathf.Clamp01(progress);graphic.SetVerticesDirty();}
        public void Finish(){if(graphic!=null)Destroy(graphic.gameObject);Destroy(this);}
        void OnDestroy(){if(graphic!=null)Destroy(graphic.gameObject);}
    }
}
