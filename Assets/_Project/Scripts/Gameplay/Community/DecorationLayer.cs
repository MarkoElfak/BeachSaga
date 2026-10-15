using BeachTycoon.Core;
using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>Spawns decoration prefabs on their spots (on the ground), for saved and newly bought ones.</summary>
    public sealed class DecorationLayer : MonoBehaviour
    {
        private GameDatabase _database;
        private DecorationService _decorations;
        private TerrainShape _ground;

        public void Initialize(GameDatabase database, DecorationService decorations, TerrainShape ground)
        {
            _database = database;
            _decorations = decorations;
            _ground = ground;
            for (int i = 0; i < decorations.Count; i++) Spawn(i);
            EventBus.Subscribe<DecorationPlaced>(OnPlaced);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<DecorationPlaced>(OnPlaced);
        }

        private void OnPlaced(DecorationPlaced evt) => Spawn(evt.Index);

        private void Spawn(int index)
        {
            DecorationModel model = _decorations.Get(index);
            DecorationDefinition definition = _database.FindDecoration(model.decorationId);
            Vector2[] spots = _database.world.decorationSpots;
            if (definition == null || definition.prefab == null || spots == null || model.spotIndex >= spots.Length)
            {
                return;
            }

            Vector2 spot = spots[model.spotIndex];
            // Face roughly toward the middle of the beach so props read well from the camera.
            float yaw = spot.x < 0f ? 90f : -90f;
            Instantiate(definition.prefab, _ground.Ground(spot.x, spot.y), Quaternion.Euler(0f, yaw + model.spotIndex * 11f, 0f), transform);
        }
    }
}
