using System;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// A tiny offline synthesizer for placeholder audio: tones with pitch glides, bells, plucked strings and
    /// filtered noise, mixed into mono float buffers. With wrap on, a note that runs past the end continues at the
    /// start, so music loops play seamlessly.
    /// </summary>
    public static class SoundSynth
    {
        public const int SampleRate = 22050;
        private const float TwoPi = (float)(Math.PI * 2.0);
        private const float FadeOutSeconds = 0.012f;

        public enum Wave
        {
            Sine,
            Triangle,
            Square,
            Saw,
        }

        public static float[] Buffer(float seconds) => new float[Math.Max(1, (int)(seconds * SampleRate))];

        /// <summary>Frequency of a MIDI note (69 = A4 = 440 Hz).</summary>
        public static float Note(int midi) => 440f * (float)Math.Pow(2.0, (midi - 69) / 12.0);

        /// <summary>
        /// Adds one note: the pitch glides from f0 to f1, a short attack, then an exponential decay (per second)
        /// and a tiny fade at the end so nothing clicks.
        /// </summary>
        public static void Tone(float[] buffer, float start, float duration, float f0, float f1, Wave wave, float volume,
            float decay = 0f, float attack = 0.004f, bool wrap = false)
        {
            int first = (int)(start * SampleRate);
            int count = (int)(duration * SampleRate);
            double phase = 0.0;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float frequency = f0 * (float)Math.Pow(f1 / f0, t / duration);
                phase += TwoPi * frequency / SampleRate;
                float sample = Shape(wave, (float)phase) * Envelope(t, duration, attack, decay) * volume;
                Add(buffer, first + i, sample, wrap);
            }
        }

        /// <summary>A bell: a sine with inharmonic overtones that fade faster than the fundamental.</summary>
        public static void Bell(float[] buffer, float start, float duration, float frequency, float volume, float decay = 4f, bool wrap = false)
        {
            Tone(buffer, start, duration, frequency, frequency, Wave.Sine, volume, decay, 0.002f, wrap);
            Tone(buffer, start, duration, frequency * 2.76f, frequency * 2.76f, Wave.Sine, volume * 0.35f, decay * 2.2f, 0.002f, wrap);
            Tone(buffer, start, duration, frequency * 5.4f, frequency * 5.4f, Wave.Sine, volume * 0.15f, decay * 3.5f, 0.002f, wrap);
        }

        /// <summary>A plucked string (Karplus-Strong): a burst of soft noise ringing in a short delay line.</summary>
        public static void Pluck(float[] buffer, float start, float duration, float frequency, float volume, Random random,
            float damping = 0.996f, bool wrap = false)
        {
            int period = Math.Max(2, (int)(SampleRate / frequency));
            var line = new float[period];
            float previous = 0f;
            for (int i = 0; i < period; i++)
            {
                previous += 0.5f * ((float)(random.NextDouble() * 2.0 - 1.0) - previous); // softer attack
                line[i] = previous;
            }

            int first = (int)(start * SampleRate);
            int count = (int)(duration * SampleRate);
            for (int i = 0; i < count; i++)
            {
                int index = i % period;
                float value = line[index];
                line[index] = 0.5f * (value + line[(index + 1) % period]) * damping;
                float t = i / (float)SampleRate;
                Add(buffer, first + i, value * volume * Envelope(t, duration, 0.001f, 0f), wrap);
            }
        }

        /// <summary>Noise through a one-pole low-pass (brightness 0..1: dull thud to hiss).</summary>
        public static void Noise(float[] buffer, float start, float duration, float volume, float brightness, Random random,
            float decay = 0f, bool wrap = false)
        {
            int first = (int)(start * SampleRate);
            int count = (int)(duration * SampleRate);
            float smoothing = Math.Max(0.01f, Math.Min(1f, brightness));
            float filtered = 0f;
            for (int i = 0; i < count; i++)
            {
                filtered += smoothing * ((float)(random.NextDouble() * 2.0 - 1.0) - filtered);
                float t = i / (float)SampleRate;
                Add(buffer, first + i, filtered * volume * Envelope(t, duration, 0.002f, decay), wrap);
            }
        }

        /// <summary>Scales the buffer so its loudest sample is peak; a gentle curve rounds off any hot spots.</summary>
        public static void Normalize(float[] buffer, float peak)
        {
            float max = 0f;
            for (int i = 0; i < buffer.Length; i++) max = Math.Max(max, Math.Abs(buffer[i]));
            if (max <= 0f) return;
            float gain = peak / max;
            for (int i = 0; i < buffer.Length; i++) buffer[i] = (float)Math.Tanh(buffer[i] * gain * 1.1f) / (float)Math.Tanh(1.1f);
        }

        private static float Shape(Wave wave, float phase)
        {
            switch (wave)
            {
                case Wave.Triangle:
                    return (float)(2.0 / Math.PI * Math.Asin(Math.Sin(phase)));
                case Wave.Square: // band-limited: odd harmonics only, so it buzzes without aliasing
                    return (float)(Math.Sin(phase) + Math.Sin(3 * phase) / 3 + Math.Sin(5 * phase) / 5 + Math.Sin(7 * phase) / 7) * 0.9f;
                case Wave.Saw:
                    double saw = 0.0;
                    for (int h = 1; h <= 8; h++) saw += Math.Sin(h * phase) / h;
                    return (float)saw * 0.6f;
                default:
                    return (float)Math.Sin(phase);
            }
        }

        private static float Envelope(float t, float duration, float attack, float decay)
        {
            float rise = attack > 0f ? Math.Min(1f, t / attack) : 1f;
            float fall = decay > 0f ? (float)Math.Exp(-decay * t) : 1f;
            float tail = Math.Min(1f, (duration - t) / FadeOutSeconds);
            return rise * fall * Math.Max(0f, tail);
        }

        private static void Add(float[] buffer, int index, float sample, bool wrap)
        {
            if (wrap) buffer[((index % buffer.Length) + buffer.Length) % buffer.Length] += sample;
            else if (index >= 0 && index < buffer.Length) buffer[index] += sample;
        }
    }
}
