using Unity.Entities;

namespace Gameplay.Turns.ECS
{
    public struct TurnIncreaseComponent : IComponentData
    {
        public int TurnIncrease;
        public int TotalTurn;
    }
}