using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PersonaCards.WebAligned
{
    public sealed class NativePersonaDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public string PersonaId { get; private set; }
        public NativeGameView Owner { get; private set; }
        RectTransform overlay, ghost;
        string caption;
        Font font;
        public bool Dragging => ghost != null;
        public void Bind(NativeGameView owner,string id,string name,RectTransform layer,Font face)
        { Owner=owner;PersonaId=id;caption=name;overlay=layer;font=face; }
        public void OnBeginDrag(PointerEventData e)
        {
            if(e.button!=PointerEventData.InputButton.Left||Owner==null||string.IsNullOrEmpty(PersonaId))return;
            e.eligibleForClick=false;
            var go=new GameObject("拖动人格",typeof(RectTransform),typeof(CanvasGroup),typeof(Image));
            ghost=(RectTransform)go.transform;ghost.SetParent(overlay,false);ghost.sizeDelta=new Vector2(260,80);
            go.GetComponent<CanvasGroup>().blocksRaycasts=false;go.GetComponent<Image>().color=new Color(.25f,.18f,.08f,.94f);
            var label=new GameObject("名称",typeof(RectTransform),typeof(Text));label.transform.SetParent(ghost,false);
            var rect=(RectTransform)label.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(8,4);rect.offsetMax=new Vector2(-8,-4);
            var text=label.GetComponent<Text>();text.font=font;text.fontSize=24;text.alignment=TextAnchor.MiddleCenter;text.color=new Color(.94f,.84f,.61f);text.text=caption;text.raycastTarget=false;
            OnDrag(e);
        }
        public void OnDrag(PointerEventData e)
        { if(ghost!=null&&RectTransformUtility.ScreenPointToLocalPointInRectangle(overlay,e.position,e.pressEventCamera,out var point))ghost.localPosition=point; }
        public void OnEndDrag(PointerEventData e){e.eligibleForClick=false;Cancel();}
        public void Cancel(){if(ghost!=null){Destroy(ghost.gameObject);ghost=null;}}
        void OnDisable(){Cancel();}
    }
}
