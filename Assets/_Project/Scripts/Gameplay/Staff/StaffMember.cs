using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>A worker or job seeker on the beach: their saved model plus where they are and what they do now.</summary>
    public sealed class StaffMember
    {
        public StaffMember(WorkerModel model, bool seeker)
        {
            Model = model;
            IsSeeker = seeker;
        }

        public WorkerModel Model { get; }
        public int Id => Model.id;

        /// <summary>Position, facing and walking path (read by the view).</summary>
        public VisitorAgent Agent { get; } = new VisitorAgent(-1);

        /// <summary>Waiting on the beach to be hired (shows a badge).</summary>
        public bool IsSeeker { get; internal set; }

        /// <summary>At their stand's counter, serving.</summary>
        public bool IsWorking { get; internal set; }

        public bool IsWalking => Agent.Path.Count > 0;

        /// <summary>Beach spot a job seeker waits on (reserved from WorldNavigation), or -1.</summary>
        internal int Spot = -1;

        /// <summary>Way to face once the walk ends.</summary>
        internal Vector3 ArriveFacing = Vector3.back;
    }
}
