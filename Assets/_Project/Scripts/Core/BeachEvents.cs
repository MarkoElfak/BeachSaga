namespace BeachTycoon.Core
{
    /// <summary>A timed beach event (surf contest, heatwave, tourist bus...) started.</summary>
    public readonly struct BeachEventStarted
    {
        public readonly string EventId;

        public BeachEventStarted(string eventId)
        {
            EventId = eventId;
        }
    }

    /// <summary>The running beach event finished (or was stopped).</summary>
    public readonly struct BeachEventEnded
    {
        public readonly string EventId;

        public BeachEventEnded(string eventId)
        {
            EventId = eventId;
        }
    }
}
