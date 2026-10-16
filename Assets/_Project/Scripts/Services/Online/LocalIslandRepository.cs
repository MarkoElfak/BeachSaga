using System;
using System.IO;
using System.Text;
using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Services
{
    /// <summary>Mock backend: one JSON file per owner in a folder (persistentDataPath/islands in the game).</summary>
    public sealed class LocalIslandRepository : IIslandRepository
    {
        public const string FolderName = "islands";
        private readonly string _folder;

        public LocalIslandRepository(string rootFolder)
        {
            _folder = Path.Combine(rootFolder, FolderName);
        }

        public string Folder => _folder;

        public void SaveSnapshot(IslandSnapshot snapshot, Action<bool> onDone)
        {
            bool saved = false;
            string json = snapshot != null && !string.IsNullOrEmpty(snapshot.ownerId) ? IslandSnapshotJson.ToJson(snapshot) : null;
            if (json != null && IslandSnapshotJson.FitsBudget(json))
            {
                try
                {
                    Directory.CreateDirectory(_folder);
                    string path = FileFor(snapshot.ownerId);
                    string temp = path + ".tmp";
                    File.WriteAllText(temp, json, Encoding.UTF8);
                    if (File.Exists(path)) File.Delete(path);
                    File.Move(temp, path);
                    saved = true;
                }
                catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
                {
                    Debug.LogWarning($"Could not save island snapshot: {exception.Message}");
                }
            }

            onDone?.Invoke(saved);
        }

        public void LoadSnapshot(string ownerId, Action<IslandSnapshot> onLoaded)
        {
            IslandSnapshot snapshot = null;
            string path = string.IsNullOrEmpty(ownerId) ? null : FileFor(ownerId);
            if (path != null && File.Exists(path))
            {
                try
                {
                    IslandSnapshotJson.TryParse(File.ReadAllText(path, Encoding.UTF8), out snapshot);
                }
                catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
                {
                    Debug.LogWarning($"Could not read island snapshot: {exception.Message}");
                }
            }

            onLoaded?.Invoke(snapshot);
        }

        // Owner ids become file names, so anything but letters, digits, '-' and '_' is replaced.
        private string FileFor(string ownerId)
        {
            var name = new StringBuilder(ownerId.Length);
            foreach (char c in ownerId) name.Append(char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_');
            return Path.Combine(_folder, name + ".json");
        }
    }
}
