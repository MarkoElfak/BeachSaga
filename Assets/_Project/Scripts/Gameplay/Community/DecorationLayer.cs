using System.Collections.Generic;
using BeachTycoon.Core;
using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>Spawns decoration prefabs on the ground (saved and newly bought ones) and follows their moves.</summary>
    public sealed class DecorationLayer : MonoBehaviour
    {
        private readonly List<Transform> _instances = new List<Transform>();
        private GameDatabase _database;
        private DecorationService _decorations;
        private PlacementService _placement;
        private TerrainShape _ground;

        public void Initialize(GameDatabase database, DecorationService decorations, PlacementService placement,
            TerrainShape ground)
        {
            _database = database;
            _decorations = decorations;
            _placement = placement;
            _ground = ground;
            for (int i = 0; i < decorations.Count; i++) Spawn(i);
            EventBus.Subscribe<DecorationPlaced>(OnPlaced);
            EventBus.Subscribe<ObjectMoved>(OnMoved);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<DecorationPlaced>(OnPlaced);
            EventBus.Unsubscribe<ObjectMoved>(OnMoved);
        }

        private void OnPlaced(DecorationPlaced evt) => Spawn(evt.Index);

        private void OnMoved(ObjectMoved evt)
        {
            if (evt.Kind != PlaceableKind.Decoration || evt.Index >= _instances.Count || _instances[evt.Index] == null) return;
            Vector2 position = _placement.GetDecorationPosition(evt.Index);
            _instances[evt.Index].SetPositionAndRotation(_ground.Ground(position.x, position.y),
                _placement.GetRotation(PlaceableId.Decoration(evt.Index)));
        }

        private void Spawn(int index)
        {
            while (_instances.Count <= index) _instances.Add(null);
            DecorationModel model = _decorations.Get(index);
            DecorationDefinition definition = _database.FindDecoration(model.decorationId);
            if (definition == null || definition.prefab == null)
            {
                return;
            }

            // The tap box is fitted to the unturned prefab (like its footprint), then the saved yaw is applied.
            Vector2 position = _placement.GetDecorationPosition(index);
            GameObject prefab = ServiceLocator.TryGet(out CultureService culture) ? culture.Resolve(definition.prefab, culture.Current) : definition.prefab;
            GameObject instance = Instantiate(prefab, _ground.Ground(position.x, position.y), Quaternion.identity, transform);
            instance.AddComponent<DecorationView>().Bind(index);
            instance.transform.rotation = _placement.GetRotation(PlaceableId.Decoration(index));
            _instances[index] = instance.transform;
        }
    }
}
