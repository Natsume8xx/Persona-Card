using NUnit.Framework;
using PersonaCards.UI;

namespace PersonaCards.Tests.EditMode
{
    /// <summary>P0-1G：TutorialSequence 纯逻辑测试（五步推进 + 文案主题与策划 11.3.1 对应 + 自动播放判定）。</summary>
    public class TutorialSequenceTests
    {
        [Test]
        public void StartsInactive()
        {
            var sequence = new TutorialSequence();
            Assert.That(sequence.CurrentStep, Is.EqualTo(-1));
            Assert.That(sequence.IsActive, Is.False);
        }

        [Test]
        public void StartActivatesFirstStep()
        {
            var sequence = new TutorialSequence();
            sequence.Start();
            Assert.That(sequence.IsActive, Is.True);
            Assert.That(sequence.CurrentStep, Is.EqualTo(0));
        }

        [Test]
        public void NextAdvancesThroughStepsThenEnds()
        {
            var sequence = new TutorialSequence();
            sequence.Start();
            // 四步推进：0 → 1 → 2 → 3 → 4（末步）
            for (var index = 1; index < TutorialSequence.StepCount; index++)
            {
                sequence.Next();
                Assert.That(sequence.CurrentStep, Is.EqualTo(index));
            }
            // 末步再推进 → 结束
            sequence.Next();
            Assert.That(sequence.CurrentStep, Is.EqualTo(-1));
            Assert.That(sequence.IsActive, Is.False);
        }

        [Test]
        public void SkipEndsImmediately()
        {
            var sequence = new TutorialSequence();
            sequence.Start();
            sequence.Skip();
            Assert.That(sequence.CurrentStep, Is.EqualTo(-1));
            Assert.That(sequence.IsActive, Is.False);
        }

        [Test]
        public void NextAfterEndStaysInactive()
        {
            var sequence = new TutorialSequence();
            sequence.Next(); // 未激活时推进：保持结束（幂等）
            Assert.That(sequence.CurrentStep, Is.EqualTo(-1));
            sequence.Skip(); // 结束后跳过：保持结束
            Assert.That(sequence.IsActive, Is.False);
        }

        [Test]
        public void GetTitleAndBodyRejectOutOfRange()
        {
            Assert.That(TutorialSequence.GetTitle(-1), Is.Empty);
            Assert.That(TutorialSequence.GetBody(-1), Is.Empty);
            Assert.That(TutorialSequence.GetTitle(TutorialSequence.StepCount), Is.Empty);
            Assert.That(TutorialSequence.GetBody(TutorialSequence.StepCount), Is.Empty);
            Assert.That(TutorialSequence.GetTitle(0), Is.Not.Empty);
        }

        [Test]
        public void StepTextsMatchDesignDocThemes()
        {
            // 五步主题与策划 11.3.1 一一对应（锁死对应关系，防文案漂移）
            Assert.That(TutorialSequence.GetTitle(0), Does.Contain("得分"));
            Assert.That(TutorialSequence.GetBody(0), Does.Contain("得分").And.Contain("目标分"));

            Assert.That(TutorialSequence.GetTitle(1), Does.Contain("手牌"));
            Assert.That(TutorialSequence.GetBody(1), Does.Contain("手牌").And.Contain("选牌").And.Contain("弃牌"));

            Assert.That(TutorialSequence.GetTitle(2), Does.Contain("计分"));
            Assert.That(TutorialSequence.GetBody(2), Does.Contain("牌型"));

            Assert.That(TutorialSequence.GetTitle(3), Does.Contain("人格"));
            Assert.That(TutorialSequence.GetBody(3), Does.Contain("人格").And.Contain("触发"));

            Assert.That(TutorialSequence.GetTitle(4), Does.Contain("首领"));
            Assert.That(TutorialSequence.GetBody(4), Does.Contain("首领").And.Contain("协议"));
        }

        [Test]
        public void StepCountMatchesTextArrays()
        {
            Assert.That(TutorialSequence.StepCount, Is.EqualTo(5));
            Assert.That(TutorialSequence.StepTitles.Length, Is.EqualTo(TutorialSequence.StepCount));
            Assert.That(TutorialSequence.StepBodies.Length, Is.EqualTo(TutorialSequence.StepCount));
        }

        [Test]
        public void ShouldAutoPlayFourQuadrants()
        {
            Assert.That(TutorialSequence.ShouldAutoPlay(true, true), Is.True);  // 重播请求 + 已看：重播优先
            Assert.That(TutorialSequence.ShouldAutoPlay(true, false), Is.True); // 重播请求 + 未看
            Assert.That(TutorialSequence.ShouldAutoPlay(false, false), Is.True); // 无请求 + 未看：首见自动
            Assert.That(TutorialSequence.ShouldAutoPlay(false, true), Is.False); // 无请求 + 已看：不再自动
        }
    }
}
