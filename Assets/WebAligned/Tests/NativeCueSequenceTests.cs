using NUnit.Framework;
using UnityEngine;
namespace PersonaCards.WebAligned.Tests
{
    public class NativeCueSequenceTests
    {
        [TestCase("personaCharge","glass_002",.28f)]
        [TestCase("personaTravel","card-fan-1",.3f)]
        [TestCase("personaImpact","chips-collide-1",.48f)]
        public void PersonaStagesUseExistingSourceRecordings(string cue,string expected,float gain)
        {
            var sequence=new NativeCueSequence();Assert.IsTrue(sequence.TryNext(cue,0,true,out var file,out var actual));
            Assert.AreEqual(expected,file);Assert.AreEqual(gain,actual);Assert.NotNull(Resources.Load<AudioClip>("WebAudio/sfx/"+file));
        }
        [TestCase("select","card-slide-1","card-slide-2","card-slide-3")]
        [TestCase("tick","chip-lay-1","chip-lay-2","chip-lay-1")]
        [TestCase("play","card-place-1","card-place-2","card-place-1")]
        public void VariantsFollowSourceOrderAndRejectedCuesDoNotAdvance(string cue,string first,string second,string third)
        {
            var sequence=new NativeCueSequence();
            Assert.IsTrue(sequence.TryNext(cue,0,true,out var file,out _));Assert.AreEqual(first,file);
            Assert.IsFalse(sequence.TryNext(cue,.001f,true,out _,out _));
            Assert.IsFalse(sequence.TryNext(cue,1,false,out _,out _));
            Assert.IsTrue(sequence.TryNext(cue,2,true,out file,out _));Assert.AreEqual(second,file);
            Assert.IsTrue(sequence.TryNext(cue,3,true,out file,out _));Assert.AreEqual(third,file);
            foreach(var name in new[]{first,second,third})Assert.NotNull(Resources.Load<AudioClip>("WebAudio/sfx/"+name));
        }
        [Test] public void CueChannelsHaveIndependentCooldownsAndUnknownCuesAreSilent()
        {
            var sequence=new NativeCueSequence();
            Assert.IsTrue(sequence.TryNext("play",0,true,out _,out var gain));Assert.AreEqual(.72f,gain);
            Assert.IsTrue(sequence.TryNext("tick",0,true,out _,out gain));Assert.AreEqual(.32f,gain);
            Assert.IsFalse(sequence.TryNext("unknown",0,true,out _,out _));
            foreach(var name in new[]{"card-shove-1","chips-stack-1"})Assert.NotNull(Resources.Load<AudioClip>("WebAudio/sfx/"+name));
        }
    }
}
