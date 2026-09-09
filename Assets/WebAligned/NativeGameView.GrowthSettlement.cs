using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace PersonaCards.WebAligned
{
    public sealed partial class NativeGameView
    {
        void RenderGrowthPage(RectTransform panel)
        {
            growthRows.Clear();
            var persona=state["growth"]?["persona"];
            Label(panel,"已有人格 "+T("#persona-growth-owned-count")+"    已装备 "+T("#persona-growth-equipped-count"),45,125,1250,40,22,Muted);
            var card=Panel(panel,45,185,460,440,Ink);BindPersonaDetails(card,S(persona?["id"]));
            bool hasPortrait=!string.IsNullOrEmpty(S(persona?["portrait"]));
            if(hasPortrait)Picture(card,Art(S(persona?["portrait"])),20,20,135,190);
            Label(card,S(persona?["name"]),hasPortrait?180:20,20,hasPortrait?255:420,65,30,Pale);
            Label(card,"新获得的人格",hasPortrait?180:20,hasPortrait?140:85,hasPortrait?255:420,35,20,Muted);
            var copy="触发条件\n"+S(persona?["trigger"])+"\n\n生效效果\n"+S(persona?["effect"]);
            var growth=S(persona?["template"]?["mainEffect"]?["growthText"]);if(growth!="")copy+="\n\n成长方式\n"+growth;
            TextScroll(card,copy,20,hasPortrait?225:135,420,hasPortrait?195:285,22,Pale);
            TextScroll(panel,T("#persona-growth-owned-summary"),45,640,460,90,20,Muted);
            var equipped=(JArray)state["personas"];
            for(int i=0;i<equipped.Count;i++){
                int slot=i;var p=equipped[i] as JObject;bool selected=(int?)state["growth"]?["selectedSlot"]==i;
                var row=Btn(panel,"",535,185+i*110,760,100,()=>Act("growth-slot",index:slot),selected);row.name="成长槽位 "+i;
                Label(row.transform,$"{i+1:00}  "+(p==null?"空槽位":S(p["name"])),15,8,590,28,23,Pale);
                Label(row.transform,S(p?["trigger"]),15,40,600,24,18,Muted);
                Label(row.transform,S(p?["effect"]),15,69,600,24,18,Gold);
                var action=Label(row.transform,selected?(p==null?"将放入":"将替换"):"选择",635,28,110,45,23,Gold);growthRows.Add((row,action));
            }
            var note=Label(panel,T("#persona-growth-replace-note"),535,640,760,90,23,Gold);note.name="成长替换说明";
            growthNote=note;note.resizeTextForBestFit=true;note.resizeTextMinSize=18;note.resizeTextMaxSize=23;
            growthConfirm=Btn(panel,T("#persona-growth-confirm"),705,770,550,65,()=>Act("click","#persona-growth-confirm"),true);growthConfirm.interactable=(bool?)state["disabled"]?["#persona-growth-confirm"]!=true;
            growthConfirmCaption=growthConfirm.GetComponentInChildren<UnityEngine.UI.Text>();growthConfirmCaption.resizeTextForBestFit=true;growthConfirmCaption.resizeTextMinSize=16;growthConfirmCaption.resizeTextMaxSize=24;
            Click(panel,"暂不替换，保留当前装备",75,770,550,"#persona-growth-keep",false);
        }
        void RenderSettlementRewards(RectTransform panel)
        {
            var descriptions=new System.Collections.Generic.List<string>{T("#result-victory-formula"),"剩余出牌  "+T("#result-hands")+" × "+T("#result-hand-rate"),"剩余弃牌  "+T("#result-discards")+" × "+T("#result-discard-rate")};
            var values=new System.Collections.Generic.List<string>{T("#result-victory-coins"),T("#result-hand-coins"),T("#result-discard-coins")};
            foreach(var extra in new[]{("卡牌额外金币","#result-card-coins"),("人格额外金币","#result-persona-coins")}){
                if(int.TryParse(T(extra.Item2),out var amount)&&amount!=0){descriptions.Add(extra.Item1);values.Add(T(extra.Item2));}
            }
            for(int i=0;i<descriptions.Count;i++){
                var label=Label(panel,descriptions[i],250,390+i*48,610,40,27,Pale);label.resizeTextForBestFit=true;label.resizeTextMinSize=18;label.resizeTextMaxSize=27;
                var value=Label(panel,"+ "+values[i],900,390+i*48,200,40,29,Gold);value.name="结算奖励 · "+descriptions[i];
            }
            var bottom=400+descriptions.Count*48;Rule(panel,250,bottom,840);
            var total=Label(panel,"本战金币  "+T("#result-coins")+"    持有金币  "+T("#result-current-coins"),250,bottom+12,840,48,28,Gold,TextAnchor.MiddleCenter);total.name="结算金币合计";
        }
    }
}

