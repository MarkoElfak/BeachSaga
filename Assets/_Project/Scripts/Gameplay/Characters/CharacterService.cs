using System;
using System.Collections.Generic;
using BeachTycoon.Core;
using BeachTycoon.Data;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// The player's character: who they are (from the picked culture), the stand they work at and where they are
    /// on the beach. They stand beside their stand's counter; once there, that stand serves faster. Sent to another
    /// stand, they walk over around everything in the way.
    /// </summary>
    public sealed class CharacterService : ITickable, IDisposable
    {
        private const int PortraitPixels = 128;

        private readonly GameDatabase _database;
        private readonly CultureService _cultures;
        private readonly StandService _stands;
        private readonly StandSpots _spots;
        private readonly MetaModel _meta;
        private readonly Dictionary<CharacterDefinition, Texture2D> _portraits = new Dictionary<CharacterDefinition, Texture2D>();
        private Vector3 _arriveFacing = Vector3.back;

        public CharacterService(GameDatabase database, CultureService cultures, StandService stands, PlacementService placement,
            WorldNavigation navigation, MetaModel meta)
        {
            _database = database;
            _cultures = cultures;
            _stands = stands;
            _spots = new StandSpots(stands, placement, navigation, database.world);
            _meta = meta;
            EventBus.Subscribe<CultureChosen>(OnCultureChosen);
            EventBus.Subscribe<StandCreated>(OnStandCreated);
            EventBus.Subscribe<ObjectMoved>(OnObjectMoved);
            TryJoin();
        }

        /// <summary>The picked culture's character; null before a culture is picked.</summary>
        public CharacterDefinition Character => _cultures.Current != null ? _cultures.Current.character : null;

        /// <summary>True once the character is on the beach.</summary>
        public bool HasJoined { get; private set; }

        /// <summary>Position, facing and walking path (read by the view).</summary>
        public VisitorAgent Agent { get; } = new VisitorAgent(-1);

        /// <summary>Stand the character works at (or is walking to); -1 while there is none.</summary>
        public int StandIndex => _meta.characterStand;

        public bool IsWorking { get; private set; }
        public bool IsWalking => Agent.Path.Count > 0;
        public float ServeSpeed => Character != null ? 1f + Character.serveSpeedBonus : 1f;

        public bool CanWorkAt(int standIndex) =>
            HasJoined && standIndex >= 0 && standIndex < _stands.Count && standIndex != StandIndex;

        /// <summary>Sends the character to work at a stand; they walk there and start once they arrive.</summary>
        public bool TrySendTo(int standIndex)
        {
            if (!CanWorkAt(standIndex)) return false;
            StopWork();
            _meta.characterStand = standIndex;
            WalkToWork();
            EventBus.Publish(new CharacterAssigned(standIndex));
            return true;
        }

        /// <summary>A painted head-and-shoulders picture of any character, made on first use and kept.</summary>
        public Texture2D Portrait(CharacterDefinition character)
        {
            if (character == null) return null;
            if (_portraits.TryGetValue(character, out Texture2D texture)) return texture;
            texture = new Texture2D(PortraitPixels, PortraitPixels, TextureFormat.RGBA32, false)
            {
                name = "Portrait " + character.id,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            texture.SetPixels32(PortraitPainter.Paint(character, _database.palette, PortraitPixels));
            texture.Apply(false, true);
            _portraits.Add(character, texture);
            return texture;
        }

        public void Tick(float deltaTime)
        {
            if (!IsWalking) return;
            if (!Agent.Move(Character.walkSpeed * deltaTime, _database.visitors.arriveDistance)) return;
            Agent.Facing = _arriveFacing;
            StartWork();
        }

        public void Dispose()
        {
            EventBus.Unsubscribe<CultureChosen>(OnCultureChosen);
            EventBus.Unsubscribe<StandCreated>(OnStandCreated);
            EventBus.Unsubscribe<ObjectMoved>(OnObjectMoved);
            foreach (Texture2D texture in _portraits.Values)
            {
                if (Application.isPlaying) Object.Destroy(texture);
                else Object.DestroyImmediate(texture);
            }

            _portraits.Clear();
        }

        // On load the character is already at work; a save from before characters starts at the first stand.
        private void TryJoin()
        {
            if (HasJoined || Character == null) return;
            HasJoined = true;
            if (_meta.characterStand >= _stands.Count) _meta.characterStand = -1;
            if (_meta.characterStand < 0 && _stands.Count > 0) _meta.characterStand = 0;
            Agent.Position = StandIndex >= 0 ? _spots.Work(StandIndex, out _arriveFacing) : _spots.Waiting();
            Agent.Facing = StandIndex >= 0 ? _arriveFacing : Vector3.back;
            if (StandIndex >= 0) StartWork();
            EventBus.Publish(new CharacterJoined());
        }

        private void OnCultureChosen(CultureChosen evt) => TryJoin();

        // The very first stand of a new beach gets the character.
        private void OnStandCreated(StandCreated evt)
        {
            if (HasJoined && StandIndex < 0) TrySendTo(evt.StandIndex);
        }

        // Their stand moved (on its own, or with its lot's default row): walk to its new side.
        private void OnObjectMoved(ObjectMoved evt)
        {
            if (!HasJoined || StandIndex < 0 || !StandSpots.Moves(evt, _stands.Get(StandIndex))) return;
            StopWork();
            WalkToWork();
        }

        private void WalkToWork() => _spots.BuildWalk(Agent.Position, StandIndex, Agent.Path, out _arriveFacing);

        private void StartWork()
        {
            IsWorking = true;
            _stands.Get(StandIndex).ServeSpeed = ServeSpeed;
            EventBus.Publish(new CharacterStartedWork(StandIndex));
        }

        private void StopWork()
        {
            if (IsWorking && StandIndex >= 0) _stands.Get(StandIndex).ServeSpeed = _stands.Get(StandIndex).UnstaffedSpeed;
            IsWorking = false;
        }
    }
}
