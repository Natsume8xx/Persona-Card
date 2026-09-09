using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace PersonaCards.WebAligned.Tests
{
    public class NativeMusicTests
    {
        [TestCase(true,false,true)]
        [TestCase(false,true,true)]
        [TestCase(false,false,false)]
        [TestCase(true,true,true)]
        public void ResultUsesMusicOrSfxFallback(bool music,bool sfx,bool expected)
        {Assert.AreEqual(expected,NativeMusicMixer.ResultEnabled(music,sfx,.8f));Assert.IsFalse(NativeMusicMixer.ResultEnabled(music,sfx,0));}
        [TestCase(true,"victory")]
        [TestCase(false,"failure")]
        public void SettlementEmitsOneResultEventWithoutSnapshotReplay(bool win,string result)
        {
            using var rules=new NativeRules(false);rules.Action("start");rules.Action("click","#stage-start");rules.Action("click","#tutorial-skip");rules.DrainAudioCues();
            rules.EvaluateForTest(win?"score=battleTarget();finish(true);__flush();":"score=0;hands=0;finish(false);__flush();");
            var before=rules.Snapshot().ToString();var cues=rules.DrainAudioCues().Values<string>().ToArray();Assert.AreEqual(1,cues.Count(c=>c=="stinger:"+result));
            Assert.IsEmpty(rules.DrainAudioCues());Assert.AreEqual(before,rules.Snapshot().ToString());
            Assert.NotNull(Resources.Load<AudioClip>("WebAudio/sfx/"+(win?"confirmation_002":"error_004")));
        }
    }
}
