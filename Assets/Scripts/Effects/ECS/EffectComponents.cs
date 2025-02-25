using Unity.Entities;
using Unity.Mathematics;

namespace Effects.ECS
{
    public struct ColliderComponent : IComponentData
    {
        public float Radius;
    }

	public struct DamageComponent : IComponentData
	{
		public bool TriggerDamageDone;
		public bool HasLimitedHits;
		public byte LimitedHits;
		public int Key;
		public float Damage;
	}

	public struct PositionComponent : IComponentData
	{
		public float3 Position;
	}

	public struct LifetimeComponent : IComponentData
	{
		public float Lifetime;
	}

	public struct ArchedMovementComponent : IComponentData
	{
		public float3 StartPosition;
		public float3 EndPosition;
		public float3 Pivot;
		
		public float Value;
	}

	public struct HealthComponent : IComponentData
	{
		public float Health;
		public float PendingDamage;
	}

	public struct DeathTag : IComponentData
	{
		
	}

	public struct RotateTowardsVelocityComponent : IComponentData
	{
		public float3 LastPosition;
	}

	public struct DeathCallbackComponent : IComponentData
	{
		public int Key;
	}
}