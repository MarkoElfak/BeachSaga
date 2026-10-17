using System;
using BeachTycoon.Data;

namespace BeachTycoon.Services
{
    /// <summary>Offline identity: a random id created on first launch and kept in the save.</summary>
    public sealed class LocalAuthService : IAuthService
    {
        private const string IdPrefix = "local-";
        private readonly MetaModel _meta;

        public LocalAuthService(MetaModel meta, string defaultName)
        {
            _meta = meta;
            if (string.IsNullOrEmpty(meta.playerId)) meta.playerId = IdPrefix + Guid.NewGuid().ToString("N");
            if (string.IsNullOrEmpty(meta.playerName)) meta.playerName = defaultName ?? string.Empty;
        }

        public bool IsSignedIn => true;
        public string PlayerId => _meta.playerId;
        public string DisplayName => _meta.playerName;

        public void SignIn(Action<bool> onDone) => onDone?.Invoke(true);
    }
}
