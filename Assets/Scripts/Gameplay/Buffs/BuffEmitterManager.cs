using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Buildings.District;
using Unity.Mathematics;
using UnityEngine;
using Buildings;
using Effects;

namespace Gameplay.Buffs
{
    public class BuffEmitterManager : Singleton<BuffEmitterManager>
    {
        [Title("Building References")]
        [SerializeField]
        private BuildingHandler buildingHandler;
        
        [SerializeField]
        private BarricadeHandler barricadeHandler;
        
        [SerializeField]
        private DistrictHandler districtHandler;

        private HashSet<BuffEmitter> emitters = new HashSet<BuffEmitter>();
        private List<IEnumerable<IBuffable>> buffables = new List<IEnumerable<IBuffable>>();

        private void OnEnable()
        {
            buildingHandler.OnWallStateCreated += OnWallStateCreated;
            barricadeHandler.OnBarricadeStateCreated += OnBarricadeStateCreated;
            districtHandler.OnDistrictCreated += OnDistrictCreated;
            
            buffables.Add(buildingHandler.WallStates.Values);
            buffables.Add(barricadeHandler.BarricadeStates.Values);
            buffables.Add(districtHandler.Districts.Values);
        }

        private void OnDisable()
        {
            buildingHandler.OnWallStateCreated -= OnWallStateCreated;
            barricadeHandler.OnBarricadeStateCreated -= OnBarricadeStateCreated;
            districtHandler.OnDistrictCreated -= OnDistrictCreated;
        }

        private void OnDistrictCreated(DistrictData districtData)
        {
            ApplyAllEmitters(districtData.State.Stats, districtData.Position);
        }

        private void OnBarricadeStateCreated(BarricadeState barricadeState)
        {
            ApplyAllEmitters(barricadeState.Stats, barricadeState.Position);
        }

        private void OnWallStateCreated(WallState wallState)
        {
            ApplyAllEmitters(wallState.Stats, wallState.Position);
        }
        
        private void ApplyAllEmitters(Stats stats, float3 position)
        {
            foreach (BuffEmitter emitter in emitters)
            {
                ApplyEmitter(emitter, stats, position);
            }
        }
        
        public void AddBuffEmitter(BuffEmitter emitter)
        {
            if (!emitters.Add(emitter))
            {
                Debug.LogError("Emitter Already Added");
                return;
            }

            ApplyEmitter(emitter);
        }
        
        private void ApplyEmitter(BuffEmitter emitter)
        {
            foreach (IEnumerable<IBuffable> buffableContainer in buffables)
            {
                foreach (IBuffable buffable in buffableContainer)
                {
                    ApplyEmitter(emitter, buffable.Stats, buffable.Position);
                }
            }
        }
        
        private void ApplyEmitter(BuffEmitter emitter, Stats stats, float3 position)
        {
            float distSq = math.distancesq(position, emitter.Position);
            if (distSq <= emitter.RangeSquared)
            {
                foreach (Buff buff in emitter.Buffs)
                {
                    stats.Get(buff.Type).AddModifier(buff.Modifier);
                }            
            }
        }
        
        public void RemoveBuffEmitter(BuffEmitter emitter)
        {
            if (!emitters.Remove(emitter))
            {
                Debug.LogError("Emitter Not Added");
                return;
            }
            
            RemoveEmitter(emitter);
        }
        
        private void RemoveEmitter(BuffEmitter emitter)
        {
            foreach (IEnumerable<IBuffable> buffableContainer in buffables)
            {
                foreach (IBuffable buffable in buffableContainer)
                {
                    RemoveEmitter(emitter, buffable.Stats, buffable.Position);
                }
            }
        }
        
        private void RemoveEmitter(BuffEmitter emitter, Stats stats, float3 position)
        {
            float distSq = math.distancesq(position, emitter.Position);
            if (distSq <= emitter.RangeSquared)
            {
                foreach (Buff buff in emitter.Buffs)
                {
                    stats.Get(buff.Type).RemoveModifier(buff.Modifier);
                }         
            }
        }
    }

    public class BuffEmitter
    {
        public Buff[] Buffs;
        public float RangeSquared;
        public float3 Position;

        public override string ToString()
        {
            return $"BuffEmitter: {Buffs.Length} Buffs, {Position} pos, {RangeSquared} rangesq";
        }
    }

    [Serializable]
    public struct Buff
    {
        public StatType Type;
        public Modifier Modifier;

        public Buff(Buff copy)
        {
            Type = copy.Type;
            Modifier = new Modifier(copy.Modifier);
        }
    }

    public interface IBuffable
    {
        public Stats Stats { get; }
        public Vector3 Position { get; }
    }
}