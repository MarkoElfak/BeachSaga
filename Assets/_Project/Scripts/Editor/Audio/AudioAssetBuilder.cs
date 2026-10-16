using System.IO;
using BeachTycoon.Data;
using UnityEditor;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// Fills the SoundLibrary: a slot for every SoundId, and a generated placeholder clip (with default tuning)
    /// for each slot and music loop that has no clip yet. Clips you assign yourself are never replaced, and an
    /// existing generated .wav is reused rather than rewritten.
    /// </summary>
    internal static class AudioAssetBuilder
    {
        public const string GeneratedFolder = ProjectPaths.Audio + "/Generated";

        public static void Fill(SoundLibrary library)
        {
            ProjectPaths.EnsureFolder(GeneratedFolder);
            AddMissingSlots(library);
            for (int i = 0; i < library.sounds.Length; i++)
            {
                if (library.sounds[i].clip != null) continue;
                SoundId id = library.sounds[i].id;
                library.sounds[i] = SoundRecipes.Tuning(id);
                library.sounds[i].clip = Clip(id.ToString(), false, () => SoundRecipes.Build(id));
            }

            if (library.music == null)
            {
                library.music = Clip("MusicDay", true, MusicComposer.Day);
                library.musicVolume = 0.45f;
            }

            if (library.nightMusic == null) library.nightMusic = Clip("MusicNight", true, MusicComposer.Night);
            EditorUtility.SetDirty(library);
        }

        private static void AddMissingSlots(SoundLibrary library)
        {
            var ids = (SoundId[])System.Enum.GetValues(typeof(SoundId));
            var slots = new System.Collections.Generic.List<SoundEntry>(library.sounds ?? new SoundEntry[0]);
            foreach (SoundId id in ids)
            {
                if (!slots.Exists(slot => slot.id == id)) slots.Add(new SoundEntry { id = id, volume = 1f });
            }

            library.sounds = slots.ToArray();
        }

        private static AudioClip Clip(string name, bool music, System.Func<float[]> render)
        {
            string path = GeneratedFolder + "/" + name + ".wav";
            if (!File.Exists(path))
            {
                WavWriter.Write(path, render(), SoundSynth.SampleRate);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                Configure(path, music);
            }

            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        // Effects decompress on load (instant playback); music stays compressed in memory and loads in the background.
        private static void Configure(string path, bool music)
        {
            if (!(AssetImporter.GetAtPath(path) is AudioImporter importer)) return;
            importer.forceToMono = true;
            importer.loadInBackground = music;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = music ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = music ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.ADPCM;
            settings.quality = 0.6f;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }
    }
}
