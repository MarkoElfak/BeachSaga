using System;
using System.IO;

namespace BeachTycoon.EditorTools
{
    /// <summary>Writes mono float samples (-1..1) as a 16-bit PCM .wav file.</summary>
    public static class WavWriter
    {
        public static void Write(string path, float[] samples, int sampleRate)
        {
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (var writer = new BinaryWriter(stream))
            {
                int dataBytes = samples.Length * 2;
                writer.Write(new[] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + dataBytes);
                writer.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
                writer.Write(16);                 // fmt chunk size
                writer.Write((short)1);           // PCM
                writer.Write((short)1);           // mono
                writer.Write(sampleRate);
                writer.Write(sampleRate * 2);     // bytes per second
                writer.Write((short)2);           // bytes per frame
                writer.Write((short)16);          // bits per sample
                writer.Write(new[] { 'd', 'a', 't', 'a' });
                writer.Write(dataBytes);
                for (int i = 0; i < samples.Length; i++)
                {
                    writer.Write((short)Math.Round(Math.Max(-1f, Math.Min(1f, samples[i])) * short.MaxValue));
                }
            }
        }
    }
}
