using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PersonaCards.WebAligned
{
    public sealed class NativePersonaDropSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        NativeGameView owner;
        Action<string> commit;
        Outline outline;
        public void Bind(NativeGameView view,Action<string> place){owner=view;commit=place;outline=NativePageFactory.Ensure<Outline>(gameObject);outline.effectColor=new Color(.95f,.75f,.32f);outline.effectDistance=new Vector2(3,-3);outline.enabled=false;}
        NativePersonaDrag Source(PointerEventData e)=>e.pointerDrag==null?null:e.pointerDrag.GetComponent<NativePersonaDrag>();
        public void OnPointerEnter(PointerEventData e){var source=Source(e);if(outline!=null)outline.enabled=source!=null&&source.Dragging&&source.Owner==owner;}
        public void OnPointerExit(PointerEventData e){if(outline!=null)outline.enabled=false;}
        public void OnDrop(PointerEventData e){var source=Source(e);if(outline!=null)outline.enabled=false;if(source==null||!source.Dragging||source.Owner!=owner)return;var id=source.PersonaId;source.Cancel();commit?.Invoke(id);}
        void OnDisable(){if(outline!=null)outline.enabled=false;}
    }
}
