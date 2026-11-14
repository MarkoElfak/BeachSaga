using BeachTycoon.Gameplay;
using NUnit.Framework;

namespace BeachTycoon.Tests
{
    /// <summary>Smooth on slower phones (M22): the picture is lowered only after a window of steady slow frames.</summary>
    public sealed class FrameRateGovernorTests
    {
        private const float Fast = 1f / 60f;
        private const float Slow = 1f / 40f;

        [Test]
        public void SteadySlowFrames_AskForAStepDown_AtTheEndOfTheWindow()
        {
            var governor = new FrameRateGovernor(60, 1.2f, 1f, 0.5f);
            bool asked = false;
            for (int i = 0; i < 30; i++) asked |= governor.Sample(Slow);
            Assert.IsFalse(asked, "not before the window ends");
            for (int i = 0; i < 12; i++) asked |= governor.Sample(Slow);
            Assert.IsTrue(asked, "a full second of 40 fps frames");
        }

        [Test]
        public void SmoothPlay_AndAFewHitches_KeepThePicture()
        {
            var governor = new FrameRateGovernor(60, 1.2f, 1f, 0.5f);
            bool asked = false;
            for (int i = 0; i < 300; i++) asked |= governor.Sample(i % 5 == 0 ? Slow : Fast);
            Assert.IsFalse(asked, "one slow frame in five is under half");
        }

        [Test]
        public void PausesAndLoads_AreLeftOut()
        {
            var governor = new FrameRateGovernor(60, 1.2f, 1f, 0.5f);
            for (int i = 0; i < 10; i++) Assert.IsFalse(governor.Sample(2f), "a two-second load frame never counts");
            bool asked = false;
            for (int i = 0; i < 60; i++) asked |= governor.Sample(Fast);
            Assert.IsFalse(asked);
        }

        [Test]
        public void EachWindowStartsFresh()
        {
            var governor = new FrameRateGovernor(60, 1.2f, 1f, 0.5f);
            for (int i = 0; i < 44; i++) governor.Sample(Slow); // past one window: about 1.1 s
            bool asked = false;
            for (int i = 0; i < 60; i++) asked |= governor.Sample(Fast);
            Assert.IsFalse(asked, "the slow window is forgotten once it was reported");
        }
    }
}
