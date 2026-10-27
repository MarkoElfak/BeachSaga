using BeachTycoon.Core;
using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Shows a stand's serve progress and drops coins on sales (they collect themselves while a level 3 worker runs
    /// it, shown by a hat) and wears one more decoration per mini-game level passed. Tapping selects it (rotate,
    /// upgrade, play); long-pressing moves it on its own. Tells WorldNavigation where its queue starts.
    /// </summary>
    public sealed class StandView : MonoBehaviour, ITappable, IMovable
    {
        [SerializeField] private Transform coinAnchor = null;
        [SerializeField] private ProgressBarView progressBar = null;

        [Tooltip("First queue position; the line grows along this transform's forward axis.")]
        [SerializeField] private Transform queueAnchor = null;

        [Tooltip("Shown while a level 3 worker runs the stand and collects its coins.")]
        [SerializeField] private GameObject managerHat = null;

        [Tooltip("Shown one by one as the stand's mini-game levels are passed.")]
        [SerializeField] private GameObject[] upgradePieces = new GameObject[0];

        private CoinSpawner _coinSpawner;
        private PlacementService _placement;

        public StandController Stand { get; private set; }

        public PlaceableId Placeable => PlaceableId.Stand(Stand != null ? Stand.Index : -1);
        public bool CanMoveNow => Stand != null;
        public Transform MoveRoot => transform;

        public void Bind(StandController stand, CoinSpawner coinSpawner)
        {
            Stand = stand;
            _coinSpawner = coinSpawner;
            _placement = ServiceLocator.Get<PlacementService>();
            progressBar.SetVisible(false);
            EventBus.Subscribe<SaleCompleted>(OnSaleCompleted);
            EventBus.Subscribe<ObjectMoved>(OnObjectMoved);
            EventBus.Subscribe<ManagerHired>(OnManagerHired);
            EventBus.Subscribe<ManagerLeft>(OnManagerLeft);
            EventBus.Subscribe<MiniGameLevelPassed>(OnMiniGameLevelPassed);
            EventBus.Subscribe<StandUpgraded>(OnStandUpgraded);
            ShowManager(stand.Model.manager);
            ShowUpgrades(ServiceLocator.TryGet(out MiniGameService games) ? games.LevelsPassed(stand.Model.productId) : 0);
            SnapToPlacement();
        }

        public void OnTapped()
        {
            if (Stand == null) return;
            float top = SelectionService.TopOf(transform);
            ServiceLocator.Get<SelectionService>().Select(Placeable, top, Stand.Model.lotIndex);
        }

        private void Update()
        {
            if (Stand == null)
            {
                return;
            }

            bool serving = Stand.IsServing;
            progressBar.SetVisible(serving);
            if (serving)
            {
                progressBar.SetProgress(Stand.ServeProgress);
            }
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<SaleCompleted>(OnSaleCompleted);
            EventBus.Unsubscribe<ObjectMoved>(OnObjectMoved);
            EventBus.Unsubscribe<ManagerHired>(OnManagerHired);
            EventBus.Unsubscribe<ManagerLeft>(OnManagerLeft);
            EventBus.Unsubscribe<MiniGameLevelPassed>(OnMiniGameLevelPassed);
            EventBus.Unsubscribe<StandUpgraded>(OnStandUpgraded);
        }

        public void ShowManager(bool hired)
        {
            if (managerHat != null) managerHat.SetActive(hired);
        }

        /// <summary>Pieces shown so far (one per level passed).</summary>
        public int UpgradesShown { get; private set; }

        public void ShowUpgrades(int levelsPassed)
        {
            UpgradesShown = 0;
            for (int i = 0; i < upgradePieces.Length; i++)
            {
                bool show = i < levelsPassed;
                if (upgradePieces[i] != null) upgradePieces[i].SetActive(show);
                if (show) UpgradesShown++;
            }
        }

        private void OnMiniGameLevelPassed(MiniGameLevelPassed evt)
        {
            if (Stand == null || evt.ProductId != Stand.Model.productId) return;
            ShowUpgrades(evt.LevelsPassed);
            Punch();
        }

        private void OnStandUpgraded(StandUpgraded evt)
        {
            if (Stand != null && evt.StandIndex == Stand.Index) Punch();
        }

        /// <summary>A quick springy bounce (levelled up, new manager, new decoration, just built).</summary>
        public void Punch()
        {
            PresentationConfig config = ServiceLocator.Get<GameDatabase>().presentation;
            ScalePunch.On(this, config.punchScale, config.punchSeconds);
        }

        // A worker reached the collecting level here: they pick up whatever is already lying at the stand.
        private void OnManagerHired(ManagerHired evt)
        {
            if (Stand == null || evt.StandIndex != Stand.Index) return;
            ShowManager(true);
            Punch();
            _coinSpawner.AutoCollectSource(Stand.Index);
        }

        private void OnManagerLeft(ManagerLeft evt)
        {
            if (Stand != null && evt.StandIndex == Stand.Index) ShowManager(false);
        }

        // Its own move, or its lot changing size (stands in the default row slide along).
        private void OnObjectMoved(ObjectMoved evt)
        {
            bool mine = evt.Kind == PlaceableKind.Stand && evt.Index == Stand.Index;
            bool myLot = evt.Kind == PlaceableKind.Lot && evt.Index == Stand.Model.lotIndex && !Stand.Model.placed;
            if (mine || myLot) SnapToPlacement();
        }

        // Stands sit on levelled ground at height 0, turned by their saved yaw (the queue anchor turns too).
        private void SnapToPlacement()
        {
            Vector2 position = _placement.GetStandPosition(Stand.Index);
            transform.SetPositionAndRotation(new Vector3(position.x, 0f, position.y), _placement.GetRotation(Placeable));
            RegisterQueue();
        }

        private void RegisterQueue()
        {
            if (queueAnchor != null && ServiceLocator.TryGet(out WorldNavigation navigation))
            {
                navigation.RegisterQueue(Stand.Index, queueAnchor.position, queueAnchor.forward);
                navigation.RegisterStandPosition(Stand.Index, transform.position);
            }
        }

        private void OnSaleCompleted(SaleCompleted evt)
        {
            if (Stand != null && evt.StandIndex == Stand.Index)
            {
                _coinSpawner.Spawn(Stand.Index, coinAnchor.position, evt.Amount, Stand.Model.manager);
            }
        }
    }
}
