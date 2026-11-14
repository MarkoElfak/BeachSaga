using System.Collections.Generic;
using BeachTycoon.Core;
using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>Shows a WorkerView for every worker and job seeker, adding and removing them as they come and go.</summary>
    public sealed class WorkerViewLayer : MonoBehaviour
    {
        private readonly Dictionary<int, WorkerView> _views = new Dictionary<int, WorkerView>();
        private StaffService _staff;
        private StandService _stands;
        private GameDatabase _database;
        private TerrainShape _ground;

        public int Count => _views.Count;

        public WorkerView Find(int workerId) => _views.TryGetValue(workerId, out WorkerView view) ? view : null;

        public void Initialize(GameDatabase database, StaffService staff, StandService stands, TerrainShape ground)
        {
            _database = database;
            _staff = staff;
            _stands = stands;
            _ground = ground;
            for (int i = 0; i < staff.Staff.Count; i++) Show(staff.Staff[i]);
            for (int i = 0; i < staff.Seekers.Count; i++) Show(staff.Seekers[i]);
            EventBus.Subscribe<WorkerShown>(OnShown);
            EventBus.Subscribe<WorkerGone>(OnGone);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<WorkerShown>(OnShown);
            EventBus.Unsubscribe<WorkerGone>(OnGone);
        }

        private void OnShown(WorkerShown evt)
        {
            StaffMember member = _staff.Find(evt.WorkerId);
            if (member != null) Show(member);
        }

        private void OnGone(WorkerGone evt)
        {
            if (!_views.TryGetValue(evt.WorkerId, out WorkerView view)) return;
            _views.Remove(evt.WorkerId);
            Destroy(view.gameObject);
        }

        private void Show(StaffMember member)
        {
            GameObject prefab = _database.staff.workerPrefab;
            if (prefab == null || _views.ContainsKey(member.Id)) return;
            var view = Instantiate(prefab, transform, false).GetComponent<WorkerView>();
            view.Bind(member, _stands, _database.palette, _database.presentation, _ground);
            _views.Add(member.Id, view);
        }
    }
}
