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
        private const float SpotGap = 0.35f;     // from the counter's side to the character
        private const float ApproachGap = 1.1f;  // where the walk ends before the last straight step in
        private static readonly Vector3 WorkFacing = new Vector3(0.45f, 0f, -1f).normalized; // the queue, mostly the camera

        private readonly GameDatabase _database;
        private readonly CultureService _cultures;
        private readonly StandService _stands;
        private readonly PlacementService _placement;
        private readonly WorldNavigation _navigation;
        private readonly MetaModel _meta;
        private readonly Dictionary<CharacterDefinition, Texture2D> _portraits = new Dictionary<CharacterDefinition, Texture2D>();
        private Vector3 _arriveFacing = Vector3.back;

        public CharacterService(GameDatabase database, CultureService cultures, StandService stands, PlacementService placement,
            WorldNavigation navigation, MetaModel meta)
        {
            _database = database;
            _cultures = cultures;
            _stands = stands;
            _placement = placement;
            _navigation = navigation;
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
            Agent.Position = StandIndex >= 0 ? WorkSpot(StandIndex, out _arriveFacing) : WaitingSpot();
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
            if (!HasJoined || StandIndex < 0) return;
            StandModel model = _stands.Get(StandIndex).Model;
            bool mine = evt.Kind == PlaceableKind.Stand && evt.Index == StandIndex;
            bool myLot = evt.Kind == PlaceableKind.Lot && evt.Index == model.lotIndex && !model.placed;
            if (!mine && !myLot) return;
            StopWork();
            WalkToWork();
        }

        private void WalkToWork()
        {
            Vector3 spot = WorkSpot(StandIndex, out _arriveFacing);
            _navigation.BuildPath(Agent.Position, Approach(StandIndex), Agent.Path);
            Agent.Path.Add(spot);
        }

        private void StartWork()
        {
            IsWorking = true;
            _stands.Get(StandIndex).ServeSpeed = ServeSpeed;
            EventBus.Publish(new CharacterStartedWork(StandIndex));
        }

        private void StopWork()
        {
            if (IsWorking && StandIndex >= 0) _stands.Get(StandIndex).ServeSpeed = 1f;
            IsWorking = false;
        }

        // Beside the counter on the stand's left, facing the queue.
        private Vector3 WorkSpot(int standIndex, out Vector3 facing)
        {
            Rect footprint = _stands.Get(standIndex).Product.standFootprint;
            Quaternion rotation = _placement.GetRotation(PlaceableId.Stand(standIndex));
            facing = rotation * WorkFacing;
            return StandPoint(standIndex, rotation, new Vector3(footprint.xMin - SpotGap, 0f, footprint.center.y - 0.1f));
        }

        // Further out on the same side, clear of the stand, so the last step never cuts through the counter.
        private Vector3 Approach(int standIndex)
        {
            Rect footprint = _stands.Get(standIndex).Product.standFootprint;
            Quaternion rotation = _placement.GetRotation(PlaceableId.Stand(standIndex));
            float side = footprint.xMin - SpotGap - _database.world.pathClearance - ApproachGap;
            return StandPoint(standIndex, rotation, new Vector3(side, 0f, footprint.center.y - 0.1f));
        }

        private Vector3 StandPoint(int standIndex, Quaternion rotation, Vector3 local)
        {
            Vector2 at = _placement.GetStandPosition(standIndex);
            return new Vector3(at.x, 0f, at.y) + rotation * local;
        }

        // No stand yet: wait in front of the main building.
        private Vector3 WaitingSpot()
        {
            var main = PlaceableId.Lot(BuildingManager.MainLotIndex);
            Rect footprint = _placement.GetFootprint(main, _placement.GetLotPosition(BuildingManager.MainLotIndex));
            return new Vector3(footprint.xMin - SpotGap, 0f, footprint.yMin - SpotGap);
        }
    }
}
