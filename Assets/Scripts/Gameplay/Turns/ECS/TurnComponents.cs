using Unity.Entities;

namespace Gameplay.Turns.ECS
{
    public struct TurnIncreaseComponent : IComponentData
    {
        public int TurnIncrease;
        public int TotalTurn;
    }

    public struct TurnProgressionComponent : IComponentData
    {
        public bool UpdatedDistrict;
        public bool UpdatedEnemies;
    }
    
    public struct UpdateDistrictTag : IComponentData { }
    
    public struct UpdateEnemiesTag : IComponentData { }
    
    public struct ProgressionBlockerTag : IComponentData { }
}