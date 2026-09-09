using System.Collections;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace PersonaCards.WebAligned
{
    public sealed partial class NativeGameView
    {
        void RenderPersonaSummary(RectTransform panel,JToken persona)
        {
            BindPersonaDetails(panel,S(persona["id"]));
            Picture(panel,Art(S(persona["portrait"])),12,12,102,156);
            FitPersonaLabel(Label(panel,S(persona["name"]),128,8,254,35,26,Pale),18,26);
            FitPersonaLabel(Label(panel,S(persona["trigger"]),128,47,254,43,19,new Color32(200,182,140,255)),16,19);
            Label(panel,"主属性",128,96,60,28,16,Muted);
            FitPersonaLabel(Label(panel,S(persona["effect"]),192,92,192,32,22,Gold),15,22);
            var affixes=(JArray)persona["affixes"];
            for(int j=0;j<affixes.Count;j++){
                bool unlocked=(bool?)affixes[j]["unlocked"]==true;
                var row=Panel(panel,125,129+j*24,261,22,unlocked?new Color32(36,27,14,210):new Color32(13,11,8,160));
                row.GetComponentInChildren<NativeFrameGraphic>().color=unlocked?new Color32(154,112,60,140):new Color32(103,84,59,90);
                Label(row,"副属性"+(j+1),6,1,67,20,13,Muted);
                FitPersonaLabel(Label(row,unlocked?S(affixes[j]["effectText"]):"未解锁",79,1,176,20,15,unlocked?Gold:Muted),12,15);
            }
        }

        static void FitPersonaLabel(Text label,int min,int max)
        {
            label.resizeTextForBestFit=true;label.resizeTextMinSize=min;label.resizeTextMaxSize=max;
        }

        Vector2 EffectPoint(RectTransform rect,Vector2 normalized)
        {
            var bounds=rect.rect;
            var local=fxRoot.InverseTransformPoint(rect.TransformPoint(new Vector3(Mathf.Lerp(bounds.xMin,bounds.xMax,normalized.x),Mathf.Lerp(bounds.yMin,bounds.yMax,normalized.y),0)));
            return new Vector2(local.x,-local.y);
        }

        IEnumerator ShowPersonaTrigger(int slot)
        {
            var panel=slot>=0&&slot<personaRects.Count?personaRects[slot]:null;
            var frame=panel!=null?panel.GetComponentInChildren<NativeFrameGraphic>():null;
            var surface=panel!=null?panel.GetComponent<Image>():null;
            var originalFrame=frame!=null?frame.color:Color.clear;
            var originalSurface=surface!=null?surface.color:Color.clear;
            var to=EffectPoint(previewScore.rectTransform,new Vector2(.5f,.5f));
            PlayCue("personaCharge");
            for(float elapsed=0;elapsed<.3f;elapsed+=Time.unscaledDeltaTime*PlaybackSpeed){
                float strength=Mathf.Clamp01(elapsed/.3f);
                if(frame!=null)frame.color=Color.Lerp(originalFrame,Gold,strength);
                if(surface!=null)surface.color=Color.Lerp(originalSurface,new Color32(76,49,16,255),strength*.65f);
                yield return null;
            }
            NativeEnergyTrail trail=null;PlayCue("personaTravel");
            if(panel!=null){trail=fxRoot.gameObject.AddComponent<NativeEnergyTrail>();trail.Initialize(fxRoot,EffectPoint(panel,new Vector2(1,.5f)),to,Gold);}
            for(float elapsed=0;elapsed<.76f;elapsed+=Time.unscaledDeltaTime*PlaybackSpeed){if(trail!=null)trail.SetProgress(elapsed/.76f);yield return null;}
            if(trail!=null){trail.SetProgress(1);trail.Finish();}
            PlayCue("personaImpact");
            if(frame!=null)frame.color=originalFrame;if(surface!=null)surface.color=originalSurface;
        }
    }
}
