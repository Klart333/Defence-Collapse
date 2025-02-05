using System;
using Unity.Mathematics;
using UnityEngine;

namespace Buildings.District
{
    public class DistrictData
    {
        public event Action OnLevelup;

        private readonly int cellCount;
        
        public UpgradeData UpgradeData { get; private set; }
        public Vector3 Position { get; private set; }
        public DistrictState State { get; }
        
        public int2 Index { get; set; }

        public DistrictData(DistrictType districtType, int cellsCount, Vector3 position)
        {
            UpgradeData = new UpgradeData(1, 1, 1);
            cellCount = cellsCount;
            Events.OnWaveStarted += OnWaveStarted;

            State = districtType switch
            {
                DistrictType.Archer => new ArcherState(this, BuildingUpgradeManager.Instance.ArcherData, position),
                DistrictType.Bomb => new BombState(this, BuildingUpgradeManager.Instance.BombData, position),
                //DistrictType.Church => expr,
                //DistrictType.Farm => expr,
                //DistrictType.Mine => expr,
                _ => throw new ArgumentOutOfRangeException(nameof(districtType), districtType, null)
            };

            Position = position;
            State.OnStateEntered();
        }

        ~DistrictData()
        {
            Events.OnWaveStarted -= OnWaveStarted;
        }

        private void OnWaveStarted()
        {
            State.OnWaveStart(cellCount);
        }

        public void LevelUp()
        {
            OnLevelup?.Invoke();
        }

        public void Update()
        {
            State.Update();
        }
    
    }
}