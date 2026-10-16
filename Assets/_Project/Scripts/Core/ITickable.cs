namespace BeachTycoon.Core
{
    /// <summary>Plain C# service that GameBootstrap advances once per frame.</summary>
    public interface ITickable
    {
        void Tick(float deltaTime);
    }
}
