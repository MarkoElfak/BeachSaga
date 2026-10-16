using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Shows a visited island's buildings, houses, stands and decorations as plain props: the same prefabs as at
    /// home, but nothing is bound to game state, so tapping or long-pressing them does nothing.
    /// </summary>
    public sealed class IslandPropSpawner : MonoBehaviour
    {
        public int SpawnedCount { get; private set; }

        public void Spawn(GameDatabase database, IslandLayout layout, TerrainShape ground)
        {
            PlacementService placement = layout.Placement;
            for (int i = 0; i < layout.Buildings.LotCount; i++)
            {
                LotModel lot = layout.Buildings.GetLot(i);
                if (lot.state != LotState.Built) continue;
                Vector2 at = placement.GetLotPosition(i);
                Add(BuildingManager.IsHouseLot(i) ? HousePrefab(database, lot.variant) : TierPrefab(database, lot.tier), at, 0f, lot.yaw);
            }

            for (int i = 0; i < layout.Stands.Count; i++)
            {
                ProductDefinition product = database.FindProduct(layout.Stands[i].productId);
                GameObject stand = Add(product != null ? product.standPrefab : null, placement.GetStandPosition(i), 0f, layout.Stands[i].yaw);
                if (stand != null) MakeStatic(stand, layout.Stands[i].manager);
            }

            for (int i = 0; i < layout.Decorations.Count; i++)
            {
                DecorationDefinition definition = database.FindDecoration(layout.Decorations[i].decorationId);
                Vector2 at = placement.GetDecorationPosition(i);
                Add(definition != null ? definition.prefab : null, at, ground.Ground(at.x, at.y).y, layout.Decorations[i].yaw);
            }
        }

        // Buildings and stands stand on levelled ground at height 0; decorations follow the terrain.
        private GameObject Add(GameObject prefab, Vector2 at, float height, float yaw)
        {
            if (prefab == null) return null;
            GameObject instance = Instantiate(prefab, new Vector3(at.x, height, at.y), Quaternion.Euler(0f, yaw, 0f), transform);
            SpawnedCount++;
            return instance;
        }

        // An unbound stand would only show an empty progress bar. Its manager's hat still shows.
        private static void MakeStatic(GameObject stand, bool manager)
        {
            foreach (ProgressBarView bar in stand.GetComponentsInChildren<ProgressBarView>(true)) bar.SetVisible(false);
            foreach (StandView view in stand.GetComponentsInChildren<StandView>(true))
            {
                view.ShowManager(manager);
                view.enabled = false;
            }
        }

        private static GameObject TierPrefab(GameDatabase database, int tier)
        {
            TierDefinition definition = database.GetTier(tier);
            return definition != null ? definition.buildingPrefab : null;
        }

        private static GameObject HousePrefab(GameDatabase database, int variant)
        {
            GameObject[] houses = database.neighbours.housePrefabs;
            return houses != null && houses.Length > 0 ? houses[Mathf.Abs(variant) % houses.Length] : null;
        }
    }
}
