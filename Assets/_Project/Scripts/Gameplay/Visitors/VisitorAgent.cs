using System.Collections.Generic;
using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    public enum VisitorState
    {
        Inactive = 0,
        Arriving,
        WalkingToSpot,
        Relaxing,
        Queueing,
        Buying,
        Leaving,
    }

    /// <summary>
    /// Plain C# state for one pooled visitor. VisitorStateMachine drives it; VisitorView only reads it.
    /// Id is the fixed pool slot, so views and stands can refer to it without allocations.
    /// </summary>
    public sealed class VisitorAgent
    {
        private const int PathCapacity = 4;

        public VisitorAgent(int id)
        {
            Id = id;
            Path = new List<Vector3>(PathCapacity);
        }

        public int Id { get; }
        public List<Vector3> Path { get; }

        public VisitorState State;
        public int TypeIndex;
        public VisitorTypeDefinition Type;
        public Vector3 Position;
        public Vector3 Facing = Vector3.forward;
        public int EntryIndex;
        public int SpotIndex = -1;
        public int StandIndex = -1;
        public int QueueSlot = -1;
        public float Timer;
        public float Patience;
        public int Purchases;
        public int MaxPurchases;
        public bool IsMoving;

        public bool IsActive => State != VisitorState.Inactive;

        public void Reset()
        {
            State = VisitorState.Inactive;
            Type = null;
            SpotIndex = -1;
            StandIndex = -1;
            QueueSlot = -1;
            Timer = 0f;
            Patience = 0f;
            Purchases = 0;
            MaxPurchases = 0;
            IsMoving = false;
            Path.Clear();
        }

        /// <summary>Walks along Path. Returns true once the last waypoint is reached.</summary>
        public bool Move(float distance, float arriveDistance)
        {
            while (Path.Count > 0)
            {
                Vector3 target = Path[0];
                Vector3 delta = target - Position;
                float remaining = delta.magnitude;
                if (remaining <= arriveDistance || remaining <= distance)
                {
                    distance -= remaining;
                    Position = target;
                    Path.RemoveAt(0);
                    continue;
                }

                Facing = delta / remaining;
                Position += Facing * distance;
                IsMoving = true;
                return false;
            }

            IsMoving = false;
            return true;
        }
    }
}
