using Random = Unity.Mathematics.Random;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine.Rendering;
using Gameplay.Upgrades.ECS;
using Gameplay.Upgrades;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Rendering;
using Unity.Entities;
using Effects.ECS;
using UnityEngine;
using Gameplay;
using VFX.ECS;
using System;
using Enemy.ECS;

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable ConvertToConstant.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnassignedField.Global
// ReSharper disable CollectionNeverUpdated.Global

namespace Effects
{
    public interface IEffect
    {
        public void Perform(IAttacker attacker);
        public void Revert(IAttacker attacker);

        public float ModifierValue { get; set; }
        public bool IsDamageEffect { get; } 
    }

    public interface IEffectHolder : IEffect 
    {
        public List<IEffect> Effects { get; set; }

        public IEffectHolder Clone();
    }

    #region Increase Stat

    [Serializable]
    public class IncreaseStatEffect : IEffect
    {
        [TitleGroup("Modifier")]
        [OdinSerialize]
        public float ModifierValue { get; set; } = 20f;

        [TitleGroup("Modifier")]
        public Modifier.ModifierType ModifierType;

        [TitleGroup("Modifier")]
        public StatType StatType;

        [TitleGroup("Options")]
        public bool CanIncrease = true;

        private Dictionary<IAttacker, Modifier> ModifierDictionary;

        public bool IsDamageEffect => false;
        
        public void Perform(IAttacker unit)
        {
            ModifierDictionary ??= new Dictionary<IAttacker, Modifier>();

            if (!ModifierDictionary.TryGetValue(unit, out Modifier value))
            {
                Modifier modifier = new Modifier
                {
                    Type = ModifierType,
                    Value = ModifierValue
                };
                
                ModifierDictionary.Add(unit, modifier);
                unit.Stats.ModifyStat(StatType, modifier);
            }
            else if (CanIncrease)
            {
                value.Value += ModifierType == Modifier.ModifierType.Multiplicative 
                    ? Mathf.Max(ModifierValue - 1.0f, 0) 
                    : ModifierValue;
                
                unit.Stats.Get(StatType).SetDirty(false);
            }
        }

        public void Revert(IAttacker unit)
        {
            if (ModifierDictionary == null || !ModifierDictionary.TryGetValue(unit, out Modifier value))
            {
                return;
            }

            unit.Stats.RevertModifiedStat(StatType, value);

            ModifierDictionary.Remove(unit);
        }
    }

    #endregion

    #region Timed Stat Increase

    [Serializable]
    public class TimedStatIncreaseEffect : IEffect
    {
        [Title("Increase Amount")]
        [OdinSerialize]
        public float ModifierValue { get; set; } = 1f;

        [Title("Stat")]
        public Modifier.ModifierType ModifierType;

        public StatType StatType;

        public float Time = 3;

        private Dictionary<IAttacker, Modifier> ModifierDictionary;

        public bool IsDamageEffect => false;

        public void Perform(IAttacker unit)
        {
            PerformAsync(unit).Forget();
        }

        private async UniTaskVoid PerformAsync(IAttacker unit)
        {
            ModifierDictionary ??= new Dictionary<IAttacker, Modifier>();

            if (ModifierDictionary.TryGetValue(unit, out Modifier value))
            {
                value.Value += ModifierValue;
            }
            else
            {
                ModifierDictionary.Add(unit, new Modifier
                {
                    Type = ModifierType,
                    Value = ModifierValue
                });

                unit.Stats.ModifyStat(StatType, ModifierDictionary[unit]);
            }
            float originalValue = ModifierDictionary[unit].Value;

            await UniTask.Delay(TimeSpan.FromSeconds(Time + originalValue / 100.0f));

            if (!Mathf.Approximately(ModifierDictionary[unit].Value, originalValue)) // A different instance will revert it
            {
                return;
            }

            Revert(unit);
        }

        public void Revert(IAttacker unit)
        {
            if (ModifierDictionary == null || !ModifierDictionary.ContainsKey(unit))
            {
                return;
            }

            unit.Stats.RevertModifiedStat(StatType, ModifierDictionary[unit]);

            ModifierDictionary.Remove(unit);
        }
    }

    #endregion

    #region Temporary Increase Stat

    [Serializable]
    public class TemporaryIncreaseStatEffect : IEffect
    {
        [TitleGroup("Modifier")]
        [OdinSerialize]
        public float ModifierValue { get; set; } = 20f;

        [TitleGroup("Modifier")]
        public Modifier.ModifierType ModifierType;

        [TitleGroup("Modifier")]
        public StatType StatType;

        [TitleGroup("Modifier")]
        public float ChanceToTrigger = 0.2f;

        private HashSet<IAttacker> unitsAttacking = new HashSet<IAttacker>();

        public bool IsDamageEffect => false;

        public void Perform(IAttacker unit)
        {
            unitsAttacking ??= new HashSet<IAttacker>();

            if (unitsAttacking.Contains(unit)) return;

            unit.Stats.ModifyStat(StatType, new Modifier
            {
                Type = ModifierType,
                Value = ModifierValue
            });

            Action RevertAfterAttack = null;
            RevertAfterAttack = () =>
            {
                Revert(unit);
                unit.OnAttack -= RevertAfterAttack;
            };

            unit.OnAttack += RevertAfterAttack;
            unitsAttacking.Add(unit);
        }

        public void Revert(IAttacker unit)
        {
            RevertAsync(unit).Forget();
        }

        private async UniTaskVoid RevertAsync(IAttacker unit)
        {
            if (unitsAttacking == null)
            {
                return;
            }

            unit.Stats.RevertModifiedStat(StatType, new Modifier
            {
                Type = ModifierType,
                Value = ModifierValue
            });

            await UniTask.NextFrame();
            unitsAttacking.Remove(unit);
        }
    }

    #endregion

    #region Damage Collider

    [Serializable]
    public class DamageColliderEffect : IEffect
    {
        [Title("Attack Damage")]
        [OdinSerialize]
        public float ModifierValue { get; set; } = 20f;

        [Title("Hits")]
        public bool LimitedHits = false;
        [ShowIf(nameof(LimitedHits))]
        public byte Hits = 1;

        [Title("Collider")]
        public float Radius = 1;

        [Title("LifeTime")]
        public bool HasLifetime = true;
        [ShowIf(nameof(HasLifetime))]
        public float Lifetime = 0.5f;

        [Title("Callbacks")]
        public bool TriggerDamageDone = true;

        [Title("Fire")]
        [SerializeField]
        private bool hasFireComponent;

        [SerializeField, ShowIf(nameof(hasFireComponent))]
        private float fireTotalDamage = 50;
        
        [Title("Lightning")]
        [SerializeField]
        private bool hasLightningComponent;
        
        [SerializeField, ShowIf(nameof(hasLightningComponent))]
        private float lightningDamage = 5;
        
        [SerializeField, ShowIf(nameof(hasLightningComponent))]
        private int lightningBounces = 5;
        
        public bool IsDamageEffect => true;
        
        public void Perform(IAttacker unit)
        {
            Vector3 pos = unit.AttackPosition;
            
            DamageComponent dmgComponent = new DamageComponent
            {
                HealthDamage = ModifierValue * unit.Stats.HealthDamage.Value,
                ArmorDamage = ModifierValue * unit.Stats.ArmorDamage.Value,
                ShieldDamage = ModifierValue * unit.Stats.ShieldDamage.Value,
                
                Key = unit.Key,
                TriggerDamageDone = TriggerDamageDone,
                LimitedHits = Hits,
                HasLimitedHits = LimitedHits,
                IsOneShot = !HasLifetime
            };

            ColliderComponent colliderComponent = new ColliderComponent
            {
                Radius = Radius,
            };
            
            CritComponent critComponent = new CritComponent
            {
                CritChance = unit.Stats.CritChance.Value,
                CritDamage = unit.Stats.CritMultiplier.Value,
            };
            
            Entity colliderEntity = CreateEntity();
            
            Entity CreateEntity()
            {
                EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

                ComponentType[] componentTypes = {
                    typeof(AddComponentInitComponent),
                    typeof(ColliderComponent),
                    typeof(PositionComponent),
                    typeof(RandomComponent),
                    typeof(DamageComponent),
                    typeof(CritComponent),
                };

                Entity spawned = entityManager.CreateEntity(componentTypes);
                entityManager.SetComponentData(spawned, new PositionComponent { Position = pos });
                entityManager.SetComponentData(spawned, new RandomComponent { Random = Random.CreateFromIndex((uint)UnityEngine.Random.Range(1, 100000)) });
                entityManager.SetComponentData(spawned, critComponent);
                entityManager.SetComponentData(spawned, colliderComponent);
                entityManager.SetComponentData(spawned, dmgComponent);

                entityManager.SetComponentData(spawned, new AddComponentInitComponent
                {
                    CategoryType = unit.CategoryType | (LimitedHits ? 0 : CategoryType.AoE),
                });
                
                if (HasLifetime)
                {
                    entityManager.AddComponentData(spawned, new LifetimeComponent { Lifetime = Lifetime });
                }

                if (hasFireComponent)
                {
                    entityManager.AddComponentData(spawned, new FireComponent { TotalDamage = fireTotalDamage });
                }
                
                if (hasLightningComponent)
                {
                    entityManager.AddComponentData(spawned, new LightningComponent
                    {
                        Damage = lightningDamage,
                        Bounces = lightningBounces,
                        Radius = 1,
                    });
                }
            
                return spawned;
            }
        }
        
        public void Revert(IAttacker unit)
        {
            // Cant revert
        }
    }

    #endregion

    #region Arched Damage Collider

    [Serializable]
    public class ArchedDamageColliderEffect : IEffect
    {
        [FoldoutGroup("Attack Damage", order: 1)]
        [OdinSerialize]
        public float ModifierValue { get; set; } = 20f;

        [FoldoutGroup("Hits")]
        public bool LimitedHits;
        
        [FoldoutGroup("Hits")]
        [ShowIf(nameof(LimitedHits))]
        public byte Hits = 1;
        
        [FoldoutGroup("Hits")]
        public bool TriggerDamageDone = true;

        [FoldoutGroup("Collider")]
        public float Radius = 1;

        [FoldoutGroup("Movement")]
        public float Height = 5;
        [FoldoutGroup("Movement")]
        public float UnitsPerSecond = 8;

        [FoldoutGroup("OnComplete Effects")]
        public List<IEffect> Effects;
        
        [FoldoutGroup("Visual")]
        public Mesh Mesh;
        [FoldoutGroup("Visual")]
        public Material Material;
        [FoldoutGroup("Visual")]
        public float Scale = 1;
        [FoldoutGroup("Visual")]
        public float TrailScaleFactor = 1;

        public bool IsDamageEffect => true;
        
        public void Perform(IAttacker unit)
        {
            float3 targetPosition = unit.AttackPosition;
            
            float distance = Vector3.Distance(unit.OriginPosition, targetPosition) + Height;
            float lifetime = distance / UnitsPerSecond;
            
            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            Entity colliderEntity = CreateEntity(unit.OriginPosition);

            if (Effects.Count > 0)
            {
                entityManager.AddComponentData(colliderEntity, new DeathCallbackComponent { Key = DeathSystem.Key });
                DeathSystem.DeathCallbacks.Add(DeathSystem.Key++, OnColliderDestroyed);
            }
            
            Entity CreateEntity(float3 pos) // COULD MAYBE BAKE INTO A PREFAB
            {
                ComponentType[] componentTypes = {
                    typeof(RotateTowardsVelocityComponent),
                    typeof(AddComponentInitComponent),
                    typeof(ArchedMovementComponent),
                    typeof(InitTrailComponent),
                    typeof(ColliderComponent),
                    typeof(PositionComponent),
                    typeof(LifetimeComponent),
                    typeof(DamageComponent),
                    typeof(RandomComponent),
                    typeof(SpeedComponent),
                    typeof(LocalTransform),
                    typeof(CritComponent),
                };

                Entity spawned = entityManager.CreateEntity(componentTypes);
                entityManager.SetComponentData(spawned, new SpeedComponent { Speed = 1.0f / lifetime });
                entityManager.SetComponentData(spawned, new LifetimeComponent { Lifetime = lifetime + 0.02f });
                entityManager.SetComponentData(spawned, new PositionComponent { Position = pos });
                entityManager.SetComponentData(spawned, new ColliderComponent { Radius = Radius });
                entityManager.SetComponentData(spawned, new InitTrailComponent { ScaleFactor = TrailScaleFactor });
                entityManager.SetComponentData(spawned, new AddComponentInitComponent
                {
                    CategoryType = unit.CategoryType | CategoryType.Projectile,
                });

                entityManager.SetComponentData(spawned, new RandomComponent
                {
                    Random = Random.CreateFromIndex((uint)UnityEngine.Random.Range(1, 100000))
                });
                entityManager.SetComponentData(spawned, new CritComponent
                {
                    CritChance = unit.Stats.CritChance.Value,
                    CritDamage = unit.Stats.CritMultiplier.Value,
                });
                
                float3 direction = math.normalize(targetPosition - pos);
                entityManager.SetComponentData(spawned, new LocalTransform
                {
                    Position = pos,
                    Rotation = quaternion.LookRotation(direction, Vector3.up),
                    Scale = Scale
                });
                
                entityManager.SetComponentData(spawned, new RotateTowardsVelocityComponent
                {
                    LastPosition = pos - direction,
                });
                
                entityManager.SetComponentData(spawned, new DamageComponent
                {
                    HealthDamage = ModifierValue * unit.Stats.HealthDamage.Value,
                    ArmorDamage = ModifierValue * unit.Stats.ArmorDamage.Value,
                    ShieldDamage = ModifierValue * unit.Stats.ShieldDamage.Value,
                    
                    Key = unit.Key,
                    TriggerDamageDone = TriggerDamageDone,
                    LimitedHits = Hits,
                    HasLimitedHits = LimitedHits,
                });
                entityManager.SetComponentData(spawned, new ArchedMovementComponent
                {
                    StartPosition = unit.OriginPosition,
                    EndPosition = targetPosition,
                    Pivot = Vector3.Lerp(unit.OriginPosition, targetPosition, 0.5f) + Vector3.up * Height,
                });
                
                RenderMeshDescription desc = new RenderMeshDescription(
                    shadowCastingMode: ShadowCastingMode.Off,
                    receiveShadows: false);

                RenderMeshArray renderMeshArray = new RenderMeshArray(new Material[] { Material }, new Mesh[] { Mesh });

                RenderMeshUtility.AddComponents(
                    spawned,
                    entityManager,
                    desc,
                    renderMeshArray,
                    MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));
                
                return spawned;
            }
            
            void OnColliderDestroyed()
            { 
                unit.AttackPosition = targetPosition;

                for (int i = 0; i < Effects.Count; i++)
                {
                    Effects[i].Perform(unit);
                }
            }
        }


        public void Revert(IAttacker unit)
        {
            // Cant revert
        }
    }

    #endregion

    #region Stacking Effect

    [Serializable]
    public class StackingEffectEffect : IEffect
    {
        [TitleGroup("Stat Increase")]
        [OdinSerialize]
        public float ModifierValue { get; set; } = 1f;

        [TitleGroup("Stat Increase")]
        public bool ShouldMultiply = false;

        [Title("Effect")]
        [OdinSerialize]
        public IEffect EffectToStack;

        private Dictionary<IAttacker, float> MultiplierDictionary;

        public bool IsDamageEffect => false;

        public void Perform(IAttacker unit)
        {
            MultiplierDictionary ??= new Dictionary<IAttacker, float>();

            if (!MultiplierDictionary.ContainsKey(unit))
            {
                MultiplierDictionary.Add(unit, ShouldMultiply ? EffectToStack.ModifierValue : ModifierValue);
            }
            else
            {
                if (ShouldMultiply)
                {
                    MultiplierDictionary[unit] *= ModifierValue;
                }
                else
                {
                    MultiplierDictionary[unit] += ModifierValue;
                }
            }

            float value = EffectToStack.ModifierValue;

            EffectToStack.ModifierValue = MultiplierDictionary[unit];
            EffectToStack.Perform(unit);

            EffectToStack.ModifierValue = value;
        }

        public void Revert(IAttacker unit)
        {
            if (MultiplierDictionary == null || !MultiplierDictionary.ContainsKey(unit))
            {
                return;
            }

            float value = EffectToStack.ModifierValue;

            EffectToStack.ModifierValue = MultiplierDictionary[unit];
            EffectToStack.Revert(unit);

            EffectToStack.ModifierValue = value;

            MultiplierDictionary.Remove(unit);
        }
    }

    #endregion

    #region Damage Over Time On Damage

    [Serializable]
    public class DamageOverTimeOnDamageEffect : IEffect
    {
        [TitleGroup("Percent Damage DOT'd")]
        [OdinSerialize]
        public float ModifierValue { get; set; }

        [TitleGroup("Percent Damage DOT'd")]
        public float TimePeriod = 0;

        private const float tickRate = 0.2f;
        private const int EffectKey = 150;
        
        public bool IsDamageEffect => true;

        public async void Perform(IAttacker unit)
        {
            DamageInstance damageToDOT = unit.LastDamageDone;

            if (damageToDOT == null || damageToDOT.SpecialEffectSet.Contains(EffectKey))
            {
                return;
            }

            int ticks = Mathf.FloorToInt(Mathf.Max(1, TimePeriod / tickRate));
            float totalDamage = (damageToDOT.Damage) * ModifierValue;
            float damage = totalDamage / ticks;
            //Debug.Log(tickRate + ", Triggering DamageOverTime with " + damageToDOT.AbilityDamage + ", that is first multiplied with " + ModifierValue + " resulting in " + totalDamage + ", then that is divided by " + ticks + " finally resulting in " + damage); ;

            DamageInstance dotInstance = new DamageInstance
            {
                Source = unit,
                Damage = damage,
                CritMultiplier = unit.Stats.GetCritMultiplier(),
                SpecialEffectSet = damageToDOT.SpecialEffectSet,
                AttackPosition = damageToDOT.AttackPosition,
            };

            dotInstance.SpecialEffectSet.Add(EffectKey);

            for (int i = 0; i < ticks; i++)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(tickRate));
                //if (dotInstance.TargetHit == null)
                //{
                //    break;
                //}
                //
                //dotInstance.AttackPosition = dotInstance.TargetHit.OriginPosition;
                //dotInstance.TargetHit.TakeDamage(dotInstance, out DamageInstance damageDone);

                //damageDone.SpecialEffectSet.Add(EffectKey);
                //unit.OnUnitDoneDamage(damageDone);
            }
        }

        public void Revert(IAttacker unit)
        {
            // Nothing to revert
        }
    }

    #endregion

    #region Random Bonus

    [Serializable]
    public class RandomBonusEffect : IEffect
    {
        [TitleGroup("Modifier")]
        [OdinSerialize]
        public float ModifierValue { get; set; } = 2f;

        [TitleGroup("Modifier")]
        public Modifier.ModifierType ModifierType;

        [TitleGroup("Modifier")]
        public int StatAmount;

        [TitleGroup("Options")]
        public bool CanIncrease = true;

        private Dictionary<IAttacker, List<(StatType, Modifier)>> ModifierDictionary;
        
        public bool IsDamageEffect => false;

        public void Perform(IAttacker unit)
        {
            ModifierDictionary ??= new Dictionary<IAttacker, List<(StatType, Modifier)>>();

            if (!ModifierDictionary.TryGetValue(unit, out List<(StatType, Modifier)> value))
            {
                ModifierDictionary.Add(unit, new List<(StatType, Modifier)>());

                for (int i = 0; i < StatAmount; i++)
                {
                    int statTypeIndex = UnityEngine.Random.Range(0, Enum.GetValues(typeof(StatType)).Length);
                    Modifier modifier = new Modifier
                    {
                        Type = ModifierType,
                        Value = ModifierValue
                    };

                    unit.Stats.ModifyStat((StatType)statTypeIndex, modifier);

                    ModifierDictionary[unit].Add(((StatType)statTypeIndex, modifier));
                }

            }
            else if (CanIncrease)
            {
                //value.Value += ModifierValue;
            }
        }

        public void Revert(IAttacker unit)
        {
            if (ModifierDictionary == null || !ModifierDictionary.ContainsKey(unit))
            {
                return;
            }

            for (int i = 0; i < ModifierDictionary[unit].Count; i++)
            {
                unit.Stats.RevertModifiedStat(ModifierDictionary[unit][i].Item1, ModifierDictionary[unit][i].Item2);
            }

            ModifierDictionary.Remove(unit);
        }
    }

    #endregion

    #region Repeated Effect

    [Serializable]
    public class RepeatEffect : IEffectHolder
    {
        [Title("Total Time")]
        [OdinSerialize]
        public float ModifierValue { get; set; } = 0.5f;

        [Title("Effect")]
        [OdinSerialize]
        public List<IEffect> Effects { get; set; }

        [SerializeField]
        private float repeatFrequency = 0.1f;

        [SerializeField]
        private float initialDelay = 0.0f;
        
        public bool IsDamageEffect => false;

        public void Perform(IAttacker unit)
        {
            PerformAsync(unit, unit.AttackPosition, unit.OriginPosition).Forget();
        }

        private async UniTaskVoid PerformAsync(IAttacker unit, Vector3 targetPosition, Vector3 originPosition)
        {
            float timer = repeatFrequency - initialDelay;
            float totalTimer = 0;

            while (totalTimer < ModifierValue)
            {
                await UniTask.Yield();

                float delta = Time.deltaTime * GameSpeedManager.Instance.Value;
                totalTimer += delta;
                timer += delta;

                if (!(timer >= repeatFrequency)) continue;
                timer = 0;
                
                for (int i = 0; i < Effects.Count; i++)
                {
                    unit.OriginPosition = originPosition;                    
                    unit.AttackPosition = targetPosition;
                    Effects[i].Perform(unit);
                }
            }
        }

        public void Revert(IAttacker unit)
        {
            for (int i = 0; i < Effects.Count; i++)
            {
                Effects[i].Revert(unit); // Might work
            }
        }

        public IEffectHolder Clone()
        {
            return new RepeatEffect
            {
                Effects = new List<IEffect>(),
                initialDelay = initialDelay,
                ModifierValue = ModifierValue,
                repeatFrequency = repeatFrequency
            };
        }
    }

    #endregion
    
    #region Delayed Effect

    [Serializable]
    public class DelayedEffect : IEffect
    {
        [Title("Times Performed")]
        [OdinSerialize]
        public float ModifierValue { get; set; } = 1;

        [Title("Effect")]
        [OdinSerialize]
        public IEffect Effect { get; set; }

        [SerializeField]
        private float delay = 0.5f;

        public bool IsDamageEffect => false;

        public void Perform(IAttacker unit)
        {
            PeformAsync(unit).Forget();
        }

        private async UniTaskVoid PeformAsync(IAttacker unit)
        {
            Vector3 attackPos = unit.AttackPosition;
            Vector3 originPos = unit.OriginPosition;
            await UniTask.Delay(TimeSpan.FromSeconds(delay));

            for (int i = 0; i < ModifierValue; i++)
            {
                Vector3 currentAttackPos = unit.AttackPosition;
                Vector3 currentOriginPos = unit.OriginPosition;
                unit.AttackPosition = attackPos;
                unit.OriginPosition = originPos;
                
                Effect.Perform(unit);
                
                unit.AttackPosition = currentAttackPos;
                unit.OriginPosition = currentOriginPos;

                await UniTask.Yield();
            }
        }

        public void Revert(IAttacker unit)
        {
            // Nah
        }
    }

    #endregion
    
    #region Targeted Effect

    [Serializable]
    public class OnDamageTargetedEffect : IEffectHolder
    {
        [Title("Nohting")]
        [OdinSerialize]
        public float ModifierValue { get; set; } = 10;

        [Title("Effect")]
        [OdinSerialize]
        public List<IEffect> Effects { get; set; }

        [Title("Delay")]
        public bool UseDelay = false;

        [ShowIf(nameof(UseDelay))]
        public float[] Delays;

        public bool IsDamageEffect => false;

        public IEffectHolder Clone()
        {
            return new OnDamageTargetedEffect()
            {
                Effects = new List<IEffect>(Effects),
                ModifierValue = ModifierValue,
                UseDelay = UseDelay,
                Delays = Delays
            };
        }

        public void Perform(IAttacker attacker)
        {
            PerformAsync(attacker).Forget();
        }

        private async UniTaskVoid PerformAsync(IAttacker attacker)
        {
            Vector3 attackPosition = attacker.LastDamageDone.AttackPosition;
            for (int i = 0; i < Effects.Count; i++)
            {
                if (UseDelay && i < Delays.Length)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(Delays[i]));
                }

                attacker.AttackPosition = attackPosition;
                Effects[i].Perform(attacker);
            }
        }

        public void Revert(IAttacker unit)
        {
            for (int i = 0; i < Effects.Count; i++)
            {
                Effects[i].Revert(unit); // Might work
            }
        }
    }

    #endregion

    #region Visual Effect

    [Serializable]
    public class VisualEffectEffect : IEffect 
    {
        [Title("Effect Scale")]
        [OdinSerialize]
        public float ModifierValue { get; set; } = 1;

        [Title("VFX")]
        [AssetSelector]
        public AttackVisualEffect AttackEffect;

        [Title("Position")]
        [SerializeField]
        private bool spawnAtUnitOrigin;
        
        [SerializeField, ShowIf(nameof(spawnAtUnitOrigin))]
        private bool orientEffectToAttackPosition;
        
        public bool IsDamageEffect => false;

        public void Perform(IAttacker unit)
        {
            Vector3 targetPosition = spawnAtUnitOrigin 
                    ? unit.OriginPosition
                    : unit.AttackPosition;
            
            Quaternion rot = orientEffectToAttackPosition 
                ? Quaternion.LookRotation((unit.AttackPosition - unit.OriginPosition).normalized) 
                : Quaternion.identity;

            AttackEffect.Spawn(targetPosition, unit.OriginPosition, unit.AttackPosition, rot, ModifierValue);
        }

        public void Revert(IAttacker unit)
        {
            // Cant revert
        }
    }
    
    #endregion

    public enum EffectType
    {
        Effect,
        Holder,
        DoneDamage
    }
}
