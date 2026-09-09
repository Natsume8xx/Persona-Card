using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace PersonaCards.WebAligned
{
    public sealed class NativeButtonMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public static bool AnimationEnabled=true;
        bool hover;void Update(){var b=GetComponent<Button>();if(!AnimationEnabled){transform.localScale=Vector3.one;return;}transform.localScale=Vector3.Lerp(transform.localScale,Vector3.one*(hover&&b.interactable?1.025f:1),1-Mathf.Exp(-Time.unscaledDeltaTime*16));}
        public void OnPointerEnter(PointerEventData e){hover=true;}public void OnPointerExit(PointerEventData e){hover=false;}
    }
}
