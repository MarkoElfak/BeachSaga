namespace BeachTycoon.Core
{
    /// <summary>A worker or job seeker appeared on the beach (on load, on arrival or when hired from the seekers).</summary>
    public readonly struct WorkerShown
    {
        public readonly int WorkerId;

        public WorkerShown(int workerId)
        {
            WorkerId = workerId;
        }
    }

    /// <summary>A worker or job seeker is gone: dismissed, quit, or a seeker replaced by new ones.</summary>
    public readonly struct WorkerGone
    {
        public readonly int WorkerId;

        public WorkerGone(int workerId)
        {
            WorkerId = workerId;
        }
    }

    /// <summary>New job seekers arrived on the beach.</summary>
    public readonly struct JobSeekersArrived
    {
        public readonly int Count;

        public JobSeekersArrived(int count)
        {
            Count = count;
        }
    }

    /// <summary>A job seeker was hired for a stand and walks over.</summary>
    public readonly struct WorkerHired
    {
        public readonly int WorkerId;
        public readonly int StandIndex;

        public WorkerHired(int workerId, int standIndex)
        {
            WorkerId = workerId;
            StandIndex = standIndex;
        }
    }

    /// <summary>A worker was sent to a stand (hired, moved, or swapped with the player's character).</summary>
    public readonly struct WorkerAssigned
    {
        public readonly int WorkerId;
        public readonly int StandIndex;

        public WorkerAssigned(int workerId, int standIndex)
        {
            WorkerId = workerId;
            StandIndex = standIndex;
        }
    }

    /// <summary>A worker reached their stand and serves there.</summary>
    public readonly struct WorkerStartedWork
    {
        public readonly int WorkerId;
        public readonly int StandIndex;

        public WorkerStartedWork(int workerId, int standIndex)
        {
            WorkerId = workerId;
            StandIndex = standIndex;
        }
    }

    /// <summary>A worker reached a new level (one more star in their stand's skill, a higher wage).</summary>
    public readonly struct WorkerLevelUp
    {
        public readonly int WorkerId;
        public readonly int Level;

        public WorkerLevelUp(int workerId, int level)
        {
            WorkerId = workerId;
            Level = level;
        }
    }

    /// <summary>Payday: every wage that could be paid was (Amount in total).</summary>
    public readonly struct WagesPaid
    {
        public readonly long Amount;

        public WagesPaid(long amount)
        {
            Amount = amount;
        }
    }

    /// <summary>A wage could not be paid; Missed counts paydays missed in a row (the worker is unhappy).</summary>
    public readonly struct WageMissed
    {
        public readonly int WorkerId;
        public readonly int Missed;

        public WageMissed(int workerId, int missed)
        {
            WorkerId = workerId;
            Missed = missed;
        }
    }

    /// <summary>An unpaid worker left for good.</summary>
    public readonly struct WorkerQuit
    {
        public readonly string Name;

        public WorkerQuit(string name)
        {
            Name = name;
        }
    }

    /// <summary>A worker or job seeker was tapped (or picked in the staff list): the UI opens their card.</summary>
    public readonly struct WorkerCardRequested
    {
        public readonly int WorkerId;

        public WorkerCardRequested(int workerId)
        {
            WorkerId = workerId;
        }
    }

    /// <summary>Opens the staff list.</summary>
    public readonly struct StaffPanelRequested
    {
    }
}
