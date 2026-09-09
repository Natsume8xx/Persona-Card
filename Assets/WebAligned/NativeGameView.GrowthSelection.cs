using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace PersonaCards.WebAligned
{
    public sealed partial class NativeGameView
    {
        readonly List<(Button button,Text action)> growthRows=new();
        readonly Dictionary<Button,Color> growthAuthoredColors=new();
        Text growthNote,growthConfirmCaption;Button growthConfirm;
        Coroutine growthSelectionMotion;CanvasGroup growthNoteGroup;float growthNoteAlpha;
        static Color GrowthRowColor(bool selected)=>selected?new Color32(64,43,18,255):new Color32(24,18,12,255);
        void StopGrowthSelectionMotion()
        {
            if(growthSelectionMotion!=null)StopCoroutine(growthSelectionMotion);
            growthSelectionMotion=null;if(growthNoteGroup!=null)growthNoteGroup.alpha=growthNoteAlpha;
        }
        void CaptureGrowthAppearance()
        {
            growthAuthoredColors.Clear();
            if(S(state["dialog"])!="#persona-growth-dialog")return;
            for(int i=0;i<growthRows.Count;i++){
                var row=growthRows[i].button;
                if(row!=null&&row.image.color!=GrowthRowColor((int?)state["growth"]?["selectedSlot"]==i))growthAuthoredColors[row]=row.image.color;
            }
        }
        bool UpdateGrowthSelection(JObject before)
        {
            if(settingsOpen||S(before["dialog"])!="#persona-growth-dialog"||S(state["dialog"])!="#persona-growth-dialog"||
                S(before["growth"]?["persona"]?["id"])!=S(state["growth"]?["persona"]?["id"])||growthConfirm==null)return false;
            int selected=(int)state["growth"]["selectedSlot"];
            for(int i=0;i<growthRows.Count;i++){
                var row=growthRows[i];if(row.button==null)return false;
                row.button.image.color=growthAuthoredColors.TryGetValue(row.button,out var authored)?authored:GrowthRowColor(i==selected);
                row.action.text=i==selected?(state["personas"][i].Type==JTokenType.Null?"将放入":"将替换"):"选择";
            }
            growthNote.text=T("#persona-growth-replace-note");growthConfirmCaption.text=T("#persona-growth-confirm");growthConfirm.name=growthConfirmCaption.text;
            growthConfirm.interactable=(bool?)state["disabled"]?["#persona-growth-confirm"]!=true;
            StopGrowthSelectionMotion();
            if(motionOn&&(int?)before["growth"]?["selectedSlot"]!=selected){
                growthNoteGroup=growthNote.GetComponent<CanvasGroup>()??growthNote.gameObject.AddComponent<CanvasGroup>();growthNoteAlpha=growthNoteGroup.alpha;
                growthSelectionMotion=StartCoroutine(RevealGrowthSelection(growthNoteGroup,growthNoteAlpha));
            }
            return true;
        }
        IEnumerator RevealGrowthSelection(CanvasGroup group,float alpha)
        {
            for(float t=0;t<.18f&&group!=null&&group.gameObject.activeInHierarchy;t+=Time.unscaledDeltaTime){group.alpha=alpha*Mathf.Lerp(.5f,1,Mathf.Clamp01(t/.18f));yield return null;}
            if(group!=null)group.alpha=alpha;growthSelectionMotion=null;
        }
    }
}

