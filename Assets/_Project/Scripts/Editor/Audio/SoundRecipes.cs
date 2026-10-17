using System;
using BeachTycoon.Data;
using W = BeachTycoon.EditorTools.SoundSynth.Wave;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// The placeholder sound for every hook, built with SoundSynth, plus each slot's default tuning (volume,
    /// pitch variation, repeat limit, vibration). Bright and short, to suit a cheerful beach game.
    /// </summary>
    public static class SoundRecipes
    {
        public static float[] Build(SoundId id)
        {
            var random = new Random(1000 + (int)id); // the same clip every time
            float[] b;
            switch (id)
            {
                case SoundId.CoinCollect:
                    b = SoundSynth.Buffer(0.35f);
                    SoundSynth.Tone(b, 0f, 0.07f, N(83), N(83), W.Square, 0.35f, 10f);
                    SoundSynth.Tone(b, 0.07f, 0.26f, N(88), N(88), W.Square, 0.35f, 12f);
                    break;
                case SoundId.Sale:
                    b = SoundSynth.Buffer(0.16f);
                    SoundSynth.Tone(b, 0f, 0.15f, N(91), N(91), W.Sine, 0.5f, 30f);
                    SoundSynth.Tone(b, 0f, 0.1f, N(103), N(103), W.Sine, 0.15f, 45f);
                    break;
                case SoundId.BuildStart:
                    b = SoundSynth.Buffer(0.45f);
                    for (int i = 0; i < 3; i++)
                    {
                        SoundSynth.Noise(b, i * 0.13f, 0.05f, 0.6f, 0.3f, random, 40f);
                        SoundSynth.Tone(b, i * 0.13f, 0.1f, 180f, 90f, W.Sine, 0.8f, 30f);
                    }

                    break;
                case SoundId.TierUpgrade:
                    b = Fanfare(new[] { 72, 76, 79 }, new[] { 84, 88, 91 }, 0.09f, 1.1f, W.Triangle);
                    SoundSynth.Bell(b, 0.27f, 0.6f, N(96), 0.12f, 6f);
                    break;
                case SoundId.ProductUnlock:
                    b = SoundSynth.Buffer(0.85f);
                    SoundSynth.Tone(b, 0f, 0.25f, N(79), N(86), W.Sine, 0.4f, 6f);
                    SoundSynth.Bell(b, 0.2f, 0.65f, N(86), 0.45f, 6f);
                    break;
                case SoundId.StandUpgrade:
                    b = SoundSynth.Buffer(0.65f);
                    SoundSynth.Tone(b, 0f, 0.18f, 392f, 784f, W.Square, 0.22f, 4f);
                    SoundSynth.Tone(b, 0.16f, 0.48f, N(84), N(84), W.Triangle, 0.45f, 7f);
                    SoundSynth.Tone(b, 0.16f, 0.48f, N(91), N(91), W.Sine, 0.2f, 9f);
                    break;
                case SoundId.NeighbourMovedIn:
                    b = SoundSynth.Buffer(1.3f);
                    SoundSynth.Bell(b, 0f, 0.8f, N(76), 0.5f, 4f);
                    SoundSynth.Bell(b, 0.35f, 0.95f, N(72), 0.5f, 3.5f);
                    break;
                case SoundId.QuestComplete:
                    b = Arpeggio(new[] { 79, 84, 88 }, 0.1f, 0.55f, W.Triangle, 0.4f);
                    break;
                case SoundId.Reward:
                    b = Arpeggio(new[] { 84, 88, 91, 96, 100 }, 0.05f, 0.6f, W.Sine, 0.35f);
                    break;
                case SoundId.UiClick:
                    b = SoundSynth.Buffer(0.06f);
                    SoundSynth.Tone(b, 0f, 0.05f, 1800f, 1200f, W.Sine, 0.5f, 60f);
                    SoundSynth.Noise(b, 0f, 0.012f, 0.15f, 0.8f, random);
                    break;
                case SoundId.MiniGameHit:
                    b = SoundSynth.Buffer(0.12f);
                    SoundSynth.Tone(b, 0f, 0.11f, 880f, 1320f, W.Sine, 0.55f, 25f);
                    break;
                case SoundId.MiniGameMiss:
                    b = SoundSynth.Buffer(0.26f);
                    SoundSynth.Tone(b, 0f, 0.25f, 220f, 150f, W.Square, 0.3f, 6f);
                    SoundSynth.Tone(b, 0f, 0.25f, 233f, 160f, W.Square, 0.2f, 6f);
                    break;
                case SoundId.MiniGameWin:
                    b = Fanfare(new[] { 72, 76, 79, 84 }, new[] { 84, 88, 91 }, 0.1f, 1.0f, W.Square);
                    break;
                case SoundId.MiniGameLose:
                    b = SoundSynth.Buffer(1.5f);
                    int[] down = { 67, 66, 65 };
                    for (int i = 0; i < down.Length; i++) SoundSynth.Tone(b, i * 0.25f, 0.24f, N(down[i]), N(down[i]), W.Triangle, 0.4f, 3f);
                    SoundSynth.Tone(b, 0.75f, 0.7f, N(64), N(63), W.Triangle, 0.4f, 2.5f);
                    break;
                case SoundId.Trophy:
                    b = SoundSynth.Buffer(1.3f);
                    foreach (int note in new[] { 84, 88, 91 }) SoundSynth.Bell(b, 0f, 1.2f, N(note), 0.3f, 3f);
                    for (int i = 0; i < 4; i++) SoundSynth.Tone(b, 0.1f + i * 0.06f, 0.15f, N(96 + i * 2), N(96 + i * 2), W.Sine, 0.1f, 20f);
                    break;
                case SoundId.EventStart:
                    b = SoundSynth.Buffer(0.6f);
                    for (int i = 0; i < 2; i++)
                    {
                        SoundSynth.Tone(b, i * 0.3f, 0.22f, N(69), N(69), W.Square, 0.22f, 2f, 0.02f);
                        SoundSynth.Tone(b, i * 0.3f, 0.22f, N(73), N(73), W.Triangle, 0.28f, 2f, 0.02f);
                    }

                    break;
                default: // Place: a soft wooden thunk
                    b = SoundSynth.Buffer(0.14f);
                    SoundSynth.Tone(b, 0f, 0.12f, 160f, 90f, W.Sine, 0.8f, 30f);
                    SoundSynth.Noise(b, 0f, 0.025f, 0.3f, 0.4f, random, 60f);
                    break;
            }

            SoundSynth.Normalize(b, 0.85f);
            return b;
        }

        /// <summary>Default slot tuning: frequent sounds are quieter, vary in pitch and are rate-limited.</summary>
        public static SoundEntry Tuning(SoundId id)
        {
            var entry = new SoundEntry { id = id, volume = 0.8f, haptic = HapticStrength.Medium };
            switch (id)
            {
                case SoundId.Sale: Set(ref entry, 0.3f, 0.1f, 0.12f, HapticStrength.None); break;
                case SoundId.CoinCollect: Set(ref entry, 0.6f, 0.08f, 0.05f, HapticStrength.Light); break;
                case SoundId.UiClick: Set(ref entry, 0.5f, 0.05f, 0.03f, HapticStrength.None); break;
                case SoundId.BuildStart: Set(ref entry, 0.7f, 0f, 0f, HapticStrength.Light); break;
                case SoundId.Place: Set(ref entry, 0.7f, 0.08f, 0.05f, HapticStrength.Light); break;
                case SoundId.MiniGameHit: Set(ref entry, 0.6f, 0.06f, 0.03f, HapticStrength.Light); break;
                case SoundId.EventStart: Set(ref entry, 0.7f, 0f, 0f, HapticStrength.Light); break;
                case SoundId.TierUpgrade:
                case SoundId.MiniGameWin: Set(ref entry, 0.85f, 0f, 0f, HapticStrength.Heavy); break;
            }

            return entry;
        }

        private static void Set(ref SoundEntry entry, float volume, float jitter, float interval, HapticStrength haptic)
        {
            entry.volume = volume;
            entry.pitchJitter = jitter;
            entry.minInterval = interval;
            entry.haptic = haptic;
        }

        private static float N(int midi) => SoundSynth.Note(midi);

        // Notes one after another, the last one ringing out.
        private static float[] Arpeggio(int[] notes, float step, float ring, W wave, float volume)
        {
            float[] b = SoundSynth.Buffer(step * notes.Length + ring);
            for (int i = 0; i < notes.Length; i++)
            {
                bool last = i == notes.Length - 1;
                SoundSynth.Tone(b, i * step, last ? ring : step * 2f, N(notes[i]), N(notes[i]), wave, volume, last ? 4f : 10f);
            }

            return b;
        }

        // A quick run up, then a held chord.
        private static float[] Fanfare(int[] run, int[] chord, float step, float hold, W wave)
        {
            float[] b = SoundSynth.Buffer(step * run.Length + hold);
            for (int i = 0; i < run.Length; i++) SoundSynth.Tone(b, i * step, step * 2f, N(run[i]), N(run[i]), wave, 0.35f, 8f);
            float at = step * run.Length;
            foreach (int note in chord) SoundSynth.Tone(b, at, hold, N(note), N(note), W.Triangle, 0.28f, 3f);
            return b;
        }
    }
}
