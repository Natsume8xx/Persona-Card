using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace PersonaCards.WebAligned.Tests
{
    public class NativeRewardDisplayTests
    {
        [TestCase(0,0)]
        [TestCase(3,7)]
        public void ExtraRewardsAppearOnlyWhenPresentAndDisplayDoesNotChangeCoins(int card,int persona)
        {
            using var rules=new NativeRules(false);rules.Action("start");rules.Action("click","#stage-start");rules.Action("click","#tutorial-skip");
            // Presentation fixture supplies earned bonuses; finish and settlement remain the source rules.
            rules.EvaluateForTest($"coins+={card+persona};earnedThisBattle+={card+persona};cardGoldThisBattle={card};personaGoldThisBattle={persona};score=battleTarget();finish(true);__flush();");
            var snapshot=rules.Snapshot();var before=snapshot.ToString();var host=new GameObject("reward display test");
            try{
                var view=host.AddComponent<NativeGameView>();view.BakePage(snapshot,false,AssetDatabase.LoadAssetAtPath<Font>("Assets/WebAligned/Fonts/NotoSerifSC.ttf"));
                var texts=host.GetComponentsInChildren<Text>();
                foreach(var extra in new[]{("卡牌额外金币",card),("人格额外金币",persona)}){
                    var row=texts.SingleOrDefault(t=>t.name=="结算奖励 · "+extra.Item1);
                    if(extra.Item2==0)Assert.IsNull(row);else{Assert.NotNull(row);Assert.AreEqual("+ "+extra.Item2,row.text);}
                }
                Assert.AreEqual("本战金币  "+snapshot["text"]["#result-coins"]+"    持有金币  "+snapshot["text"]["#result-current-coins"],texts.Single(t=>t.name=="结算金币合计").text);
                Assert.AreEqual((int)snapshot["earnedThisBattle"],texts.Where(t=>t.name.StartsWith("结算奖励 · ")).Sum(t=>int.Parse(t.text.Replace("+ ",""))));
                Assert.AreEqual(before,rules.Snapshot().ToString());
            }finally{Object.DestroyImmediate(host);}
        }
    }
}
