namespace Gameplay
{
    public interface IGameSpeed
    {
        public float Value { get; }
    }

    public class SimpleGameSpeed : IGameSpeed
    {
        public float Value => 1.0f;
    }
}