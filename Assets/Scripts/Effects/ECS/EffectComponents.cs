using Random = Unity.Mathematics.Random;
using Gameplay.Upgrades;
using Unity.Mathematics;
using Unity.Entities;

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
		public bool IsOneShot;
		public byte LimitedHits;
		public int Key;
		
		public float HealthDamage;
		public float ShieldDamage;
		public float ArmorDamage;
	}

	public struct CritComponent : IComponentData
	{
		public float CritChance;
		public float CritDamage;
	}
	
	public struct SimpleDamageComponent : IComponentData
	{
		public float Damage;
	}
	
	public struct DamageTakenComponent : IComponentData
	{
		public float DamageTaken;
		public Health.HealthType DamageTakenType;
		public bool IsCrit;
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
		public Entity Bar; 
		public float Health;
		public float Armor;
		public float Shield;
	}

	public struct PendingDamageComponent : IComponentData
	{
		public float HealthDamage;
		public float ArmorDamage;
		public float ShieldDamage;
		public bool IsCrit;
		
		public bool TriggerDamageDone;
		public int Key;

		public Entity SourceEntity;
	}

	public struct MaxHealthComponent : IComponentData
	{
		public float Health;
		public float Armor;
		public float Shield;
	}

	public struct HealthScalingComponent : IComponentData
	{
		public float Multiplier;
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

	public struct MoneyOnDeathComponent : IComponentData
	{
		public float Amount;
	}

	public struct RandomComponent : IComponentData
	{
		public Random Random;
	}

	public struct AddComponentComponent : IComponentData
	{
		public CategoryType AppliedCategory;
		public UpgradeComponentType ComponentType;

		public float Strength;
	}
	
	#region Effect Components
	
	public struct LightningComponent : IComponentData
	{
		public float Radius;
		public float Damage;
		public int Bounces;
	}

	public struct ExplosionComponent : IComponentData
	{
		
	}

	public struct FireComponent : IComponentData
	{
		public float TotalDamage;
		public float Timer;
	}
	
	public struct PoisonComponent : IComponentData
	{
		public float TotalDamage;
		public float Timer;
	}

	
	#endregion
}