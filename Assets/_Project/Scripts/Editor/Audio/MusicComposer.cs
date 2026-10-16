using System;
using W = BeachTycoon.EditorTools.SoundSynth.Wave;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// Two seamless 8-bar loops for the placeholder soundtrack. Day: a sunny ukulele groove (100 BPM) with bass,
    /// marimba tune and shaker. Night: a slow, soft pad (72 BPM) with a few bell notes and distant waves.
    /// </summary>
    public static class MusicComposer
    {
        private const int Bars = 8;

        // Chord roots (MIDI) and their notes, one chord per bar.
        private static readonly int[][] DayChords =
        {
            new[] { 60, 64, 67 }, new[] { 57, 60, 64 }, new[] { 53, 57, 60 }, new[] { 55, 59, 62 },
            new[] { 60, 64, 67 }, new[] { 57, 60, 64 }, new[] { 53, 57, 60 }, new[] { 55, 59, 62, 65 },
        };

        private static readonly int[][] NightChords =
        {
            new[] { 57, 60, 64 }, new[] { 53, 57, 60 }, new[] { 60, 64, 67 }, new[] { 55, 59, 62 },
            new[] { 57, 60, 64 }, new[] { 53, 57, 60 }, new[] { 52, 56, 59 }, new[] { 55, 59, 62 },
        };

        // Day tune on an eighth-note grid, 8 per bar (0 = rest), C major pentatonic.
        private static readonly int[] DayTune =
        {
            76, 0, 79, 0, 81, 79, 76, 0,   72, 0, 76, 0, 74, 0, 72, 0,
            69, 0, 72, 74, 76, 0, 74, 72,  71, 0, 74, 0, 79, 0, 0, 0,
            76, 0, 79, 0, 81, 0, 84, 81,   79, 0, 76, 0, 74, 76, 72, 0,
            69, 0, 72, 0, 74, 76, 79, 0,   74, 0, 71, 0, 67, 0, 0, 0,
        };

        // Night bells on a quarter-note grid, 4 per bar.
        private static readonly int[] NightTune =
        {
            76, 0, 72, 0,  77, 0, 0, 72,  79, 0, 76, 0,  74, 0, 0, 0,
            76, 0, 81, 0,  77, 0, 72, 0,  71, 0, 68, 0,  71, 0, 0, 0,
        };

        public static float[] Day()
        {
            const float beat = 60f / 100f;
            float[] b = SoundSynth.Buffer(Bars * 4 * beat);
            var random = new Random(7);
            for (int bar = 0; bar < Bars; bar++)
            {
                float start = bar * 4 * beat;
                int[] chord = DayChords[bar];
                SoundSynth.Tone(b, start, beat * 1.5f, N(chord[0] - 24), N(chord[0] - 24), W.Triangle, 0.5f, 1.5f, 0.01f, true);
                SoundSynth.Tone(b, start + 2 * beat, beat * 1.5f, N(chord[2] - 24), N(chord[2] - 24), W.Triangle, 0.45f, 1.5f, 0.01f, true);
                for (int hit = 0; hit < 2; hit++)
                {
                    SoundSynth.Tone(b, start + hit * 2 * beat, 0.15f, 90f, 50f, W.Sine, 0.45f, 18f, 0.002f, true); // soft kick
                }

                // Ukulele strums on the off-beats ("and" of each beat), strings a few ms apart.
                for (int eighth = 1; eighth < 8; eighth += 2)
                {
                    for (int s = 0; s < chord.Length; s++)
                    {
                        float at = start + eighth * beat * 0.5f + s * 0.012f;
                        SoundSynth.Pluck(b, at, beat * 0.9f, N(chord[s] + 12), 0.16f, random, 0.994f, true);
                    }
                }

                for (int eighth = 0; eighth < 8; eighth++)
                {
                    float at = start + eighth * beat * 0.5f;
                    SoundSynth.Noise(b, at, 0.05f, eighth % 2 == 1 ? 0.07f : 0.04f, 0.9f, random, 50f, true); // shaker
                    int note = DayTune[bar * 8 + eighth];
                    if (note > 0) Marimba(b, at, beat * 0.9f, note, 0.32f);
                }
            }

            SoundSynth.Normalize(b, 0.8f);
            return b;
        }

        public static float[] Night()
        {
            const float beat = 60f / 72f;
            float loop = Bars * 4 * beat;
            float[] b = SoundSynth.Buffer(loop);
            var random = new Random(11);
            for (int bar = 0; bar < Bars; bar++)
            {
                float start = bar * 4 * beat;
                int[] chord = NightChords[bar];
                SoundSynth.Tone(b, start, beat * 3.8f, N(chord[0] - 24), N(chord[0] - 24), W.Sine, 0.35f, 0.4f, 0.05f, true);
                foreach (int note in chord)
                {
                    // Pad: two slightly detuned soft voices with a slow swell, overlapping into the next bar.
                    SoundSynth.Tone(b, start, beat * 4.6f, N(note) * 0.998f, N(note) * 0.998f, W.Triangle, 0.09f, 0.3f, beat, true);
                    SoundSynth.Tone(b, start, beat * 4.6f, N(note) * 1.002f, N(note) * 1.002f, W.Triangle, 0.09f, 0.3f, beat, true);
                }

                for (int quarter = 0; quarter < 4; quarter++)
                {
                    int note = NightTune[bar * 4 + quarter];
                    if (note > 0) SoundSynth.Bell(b, start + quarter * beat, beat * 2.5f, N(note), 0.16f, 2.5f, true);
                }
            }

            Waves(b, loop, random);
            SoundSynth.Normalize(b, 0.7f);
            return b;
        }

        // Marimba-like: a sine with a quick, bright overtone.
        private static void Marimba(float[] b, float at, float length, int note, float volume)
        {
            SoundSynth.Tone(b, at, length, N(note), N(note), W.Sine, volume, 7f, 0.002f, true);
            SoundSynth.Tone(b, at, length * 0.4f, N(note) * 4f, N(note) * 4f, W.Sine, volume * 0.25f, 30f, 0.001f, true);
        }

        // Low noise that swells and settles four times per loop, like waves on the shore.
        private static void Waves(float[] b, float loop, Random random)
        {
            var noise = new float[b.Length];
            SoundSynth.Noise(noise, 0f, loop, 1f, 0.05f, random);
            for (int i = 0; i < b.Length; i++)
            {
                float phase = i / (float)b.Length * 4f;
                float swell = 0.5f - 0.5f * (float)Math.Cos(phase * Math.PI * 2.0);
                b[i] += noise[i] * swell * swell * 0.12f;
            }
        }

        private static float N(int midi) => SoundSynth.Note(midi);
    }
}
