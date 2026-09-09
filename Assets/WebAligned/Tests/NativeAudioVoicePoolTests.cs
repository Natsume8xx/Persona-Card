using NUnit.Framework;
namespace PersonaCards.WebAligned.Tests
{
    public class NativeAudioVoicePoolTests
    {
        [TestCase(0,0,true)] [TestCase(9,2,true)] [TestCase(10,0,false)]
        [TestCase(3,3,false)] [TestCase(10,3,false)]
        public void MatchesWebTotalAndPerRecordingCaps(int total,int same,bool expected)
        {Assert.AreEqual(expected,NativeAudioVoicePool.Allows(total,same));}
    }
}
