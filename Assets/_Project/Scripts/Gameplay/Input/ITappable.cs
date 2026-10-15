namespace BeachTycoon.Gameplay
{
    /// <summary>Anything in the world that reacts to a tap. TapInputRouter finds it on the hit collider or its parents.</summary>
    public interface ITappable
    {
        void OnTapped();
    }
}
