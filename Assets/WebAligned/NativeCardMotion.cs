using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace PersonaCards.WebAligned
{
    public sealed class NativeCardMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public static bool AnimationEnabled=true;
        RectTransform rect;Vector2 home,velocity;bool selected,hover;float tilt,baseTilt;public bool ExternalMotion;
        public void Setup(Vector2 position,bool isSelected,float angle=0){rect=(RectTransform)transform;home=position;selected=isSelected;baseTilt=angle;tilt=angle;transform.localRotation=Quaternion.Euler(0,0,isSelected?0:angle);}
        public void Select(bool value){selected=value;var frame=GetComponentInChildren<NativeFrameGraphic>();if(frame!=null){frame.Thickness=value?2:1;frame.color=value?new Color32(232,190,104,255):Color.clear;}}
        public void Deal(Vector2 from){rect.anchoredPosition=from;transform.localScale=Vector3.one*.35f;tilt=-18;}
        void Update(){if(rect==null||ExternalMotion)return;var target=home+Vector2.up*(selected?42:hover?18:0);if(!AnimationEnabled){rect.anchoredPosition=target;transform.localScale=Vector3.one;transform.localRotation=Quaternion.Euler(0,0,selected?0:baseTilt);return;}rect.anchoredPosition=Vector2.SmoothDamp(rect.anchoredPosition,target,ref velocity,.095f,Mathf.Infinity,Time.unscaledDeltaTime);transform.localScale=Vector3.Lerp(transform.localScale,Vector3.one*(hover?1.045f:1),1-Mathf.Exp(-Time.unscaledDeltaTime*16));tilt=Mathf.Lerp(tilt,selected||hover?0:baseTilt,1-Mathf.Exp(-Time.unscaledDeltaTime*14));transform.localRotation=Quaternion.Euler(0,0,tilt);}
        public void OnPointerEnter(PointerEventData e){if(!ExternalMotion)hover=true;}
        public void OnPointerExit(PointerEventData e){hover=false;}
    }
}
