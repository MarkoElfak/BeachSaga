using System;
using System.IO;
using BeachTycoon.Data;
using BeachTycoon.EditorTools;
using NUnit.Framework;

namespace BeachTycoon.Tests
{
    /// <summary>The generated placeholder audio: every hook gets a clean, audible clip and the music loops seamlessly.</summary>
    public sealed class SoundSynthTests
    {
        [Test]
        public void EveryHookHasAnAudibleCleanClip()
        {
            foreach (SoundId id in (SoundId[])Enum.GetValues(typeof(SoundId)))
            {
                float[] clip = SoundRecipes.Build(id);
                float seconds = clip.Length / (float)SoundSynth.SampleRate;
                Assert.That(seconds, Is.InRange(0.05f, 1.6f), $"{id} length");
                Assert.That(Peak(clip), Is.InRange(0.5f, 1f), $"{id} is loud enough and does not clip");
                Assert.Less(Math.Abs(clip[clip.Length - 1]), 0.02f, $"{id} ends without a click");
            }
        }

        [Test]
        public void ClipsAreTheSameEveryTime()
        {
            CollectionAssert.AreEqual(SoundRecipes.Build(SoundId.BuildStart), SoundRecipes.Build(SoundId.BuildStart));
        }

        [Test]
        public void FrequentSoundsAreQuietLimitedAndVaried()
        {
            SoundEntry sale = SoundRecipes.Tuning(SoundId.Sale);
            Assert.Greater(sale.minInterval, 0f);
            Assert.Greater(sale.pitchJitter, 0f);
            Assert.Less(sale.volume, SoundRecipes.Tuning(SoundId.TierUpgrade).volume);
            Assert.AreEqual(HapticStrength.None, sale.haptic, "no buzz on every sale");
            Assert.AreEqual(HapticStrength.Heavy, SoundRecipes.Tuning(SoundId.TierUpgrade).haptic);
        }

        [Test]
        public void MusicLoopsAreSeamless()
        {
            AssertLoop(MusicComposer.Day(), 8 * 4 * 60f / 100f, "day");
            AssertLoop(MusicComposer.Night(), 8 * 4 * 60f / 72f, "night");
        }

        [Test]
        public void WavFileHasAValidHeader()
        {
            string path = Path.Combine(Path.GetTempPath(), "beach_tycoon_test.wav");
            WavWriter.Write(path, new[] { 0f, 0.5f, -0.5f, 1f }, SoundSynth.SampleRate);
            byte[] bytes = File.ReadAllBytes(path);
            File.Delete(path);
            Assert.AreEqual(44 + 4 * 2, bytes.Length);
            Assert.AreEqual("RIFF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
            Assert.AreEqual("WAVE", System.Text.Encoding.ASCII.GetString(bytes, 8, 4));
            Assert.AreEqual(SoundSynth.SampleRate, BitConverter.ToInt32(bytes, 24));
            Assert.AreEqual(short.MaxValue, BitConverter.ToInt16(bytes, 44 + 6), "full scale");
        }

        private static void AssertLoop(float[] loop, float expectedSeconds, string name)
        {
            Assert.AreEqual(expectedSeconds, loop.Length / (float)SoundSynth.SampleRate, 0.01f, $"{name} length");
            Assert.That(Peak(loop), Is.InRange(0.5f, 1f), $"{name} level");
            Assert.Greater(Rms(loop), 0.05f, $"{name} is not mostly silence");

            // The jump from the last sample back to the first is no bigger than a typical step inside the loop.
            float seam = Math.Abs(loop[0] - loop[loop.Length - 1]);
            float typical = 0f;
            for (int i = 1; i < loop.Length; i++) typical = Math.Max(typical, Math.Abs(loop[i] - loop[i - 1]));
            Assert.LessOrEqual(seam, typical, $"{name} wraps without a click");
        }

        private static float Peak(float[] samples)
        {
            float peak = 0f;
            foreach (float sample in samples) peak = Math.Max(peak, Math.Abs(sample));
            return peak;
        }

        private static float Rms(float[] samples)
        {
            double sum = 0.0;
            foreach (float sample in samples) sum += sample * sample;
            return (float)Math.Sqrt(sum / samples.Length);
        }
    }
}
