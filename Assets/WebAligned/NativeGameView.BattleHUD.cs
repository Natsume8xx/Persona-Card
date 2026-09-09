using UnityEngine;
using UnityEngine.UI;

namespace PersonaCards.WebAligned
{
    public sealed partial class NativeGameView
    {
        RectTransform scoreProgressFill;
        const float ScoreBarWidth=432;

        void RenderBattleStatus()
        {
            var stack=Panel(content,1390,595,490,389,new Color32(17,12,7,250));stack.name="本场状态总览";
            var stats=Panel(stack,7,7,476,192,Color.clear,false);
            var art=InsertArt(stats,"battle-hud/score-progress-panel-v2",0,0,476,192);
            // Matches the final web status-stack rule: 105% x 145%, centered at 49%.
            if(art!=null)art.uvRect=new Rect(.02381f,.15828f,.95238f,.68966f);
            Label(stats,"当前得分",25,19,220,29,17,Muted);
            Label(stats,"目标分数",253,27,198,27,17,Muted,TextAnchor.MiddleRight);
            totalLabel=Label(stats,S(state["score"]),25,48,245,64,46,Gold);
            Label(stats,S(state["target"]),268,61,183,48,34,Pale,TextAnchor.MiddleRight);
            var track=Panel(stats,22,118,ScoreBarWidth,10,new Color32(8,6,3,255));
            scoreProgressFill=Panel(track,1,2,(ScoreBarWidth-2)*ScoreRatio((float)state["score"]),6,Gold,false);
            scoreProgressFill.name="目标分数进度";
            Label(stats,"本战金币  "+S(state["earnedThisBattle"]),25,153,220,27,18,Muted);
            Label(stats,"持有金币  "+S(state["coins"]),249,153,202,27,18,Gold,TextAnchor.MiddleRight);
            RenderTurnResource(stack,207,"出牌次数","▰",(int)state["hands"],T("#hands-max"),Gold);
            RenderTurnResource(stack,294,"弃牌次数","▱",(int)state["discards"],T("#discards-max"),new Color32(178,141,210,255));
        }

        float ScoreRatio(float score)=>Mathf.Clamp01(score/Mathf.Max(1,(float)state["target"]));

        void RenderTurnResource(RectTransform parent,float y,string title,string icon,int remaining,string maximumText,Color accent)
        {
            // Both counts come from the existing web renderer. No duplicate encounter values.
            int maximum=int.TryParse(maximumText,out var parsed)?parsed:remaining;
            var row=Panel(parent,9,y,472,84,Color.clear,false);row.name=title;
            InsertArt(row,"battle-hud/action-resource-plate-v1",0,0,472,80);
            Label(row,icon,26,19,48,44,30,accent,TextAnchor.MiddleCenter);
            Label(row,title,88,21,222,40,27,Pale);
            Label(row,remaining.ToString(),305,10,94,56,43,accent,TextAnchor.MiddleRight);
            var cap=Label(row,"/ "+maximum,403,28,54,30,19,Muted);cap.name=title+"上限";
            Panel(row,5,80,462,3,new Color32(49,35,20,255),false);
            var fill=Panel(row,5,80,462*Mathf.Clamp01(remaining/(float)Mathf.Max(1,maximum)),3,accent,false);fill.name=title+"消耗条";
        }

        void AnimateScoreTotal(int nextScore,float progress)
        {
            float eased=1-Mathf.Pow(1-Mathf.Clamp01(progress),3);
            float displayed=Mathf.Lerp((float)state["score"],nextScore,eased);
            totalLabel.text=Mathf.RoundToInt(displayed).ToString();
            if(scoreProgressFill!=null)scoreProgressFill.sizeDelta=new Vector2((ScoreBarWidth-2)*ScoreRatio(displayed),6);
        }
    }
}
