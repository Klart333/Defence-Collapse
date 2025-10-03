using System.Collections.Generic;
using Exp.Gemstones;
using MessagePack;

namespace Saving
{
    public class ExpSaveLoad
    {
        private const string SaveFileName = "ExpSaveData";
        
        private ISaveSystem saveSystem;
        
        public ExpSaveLoad(ISaveSystem saveSystem)
        {
            this.saveSystem = saveSystem;    
        }
        
        public void SaveExpData(int exp, int slotsUnlocked, List<Gemstone> gemstones, List<Gemstone> activeGemstones)
        {
            GemStoneSaveData[] gemstonesArray = new GemStoneSaveData[gemstones.Count];
            GemStoneSaveData[] activeGemstonesArray = new GemStoneSaveData[activeGemstones.Count];
            for (int i = 0; i < gemstones.Count; i++)
            {
                gemstonesArray[i] = GetGemstoneSaveData(gemstones[i]);
            }

            for (int i = 0; i < activeGemstones.Count; i++)
            {
                activeGemstonesArray[i] = GetGemstoneSaveData(activeGemstones[i]);
            }

            ExpSaveData expData = new ExpSaveData
            {
                Exp = exp,
                SlotsUnlocked = slotsUnlocked,
                GemStones = gemstonesArray,
                ActiveGemStones = activeGemstonesArray
            };

            saveSystem.SaveData(expData, SaveFileName);
        }

        private GemStoneSaveData GetGemstoneSaveData(Gemstone gemstone)
        {
            return new GemStoneSaveData
            {
                Seed = gemstone.Seed,
                Level = gemstone.Level,
                GemstoneType = gemstone.GemstoneType,
            };
        }

        public ExpSaveData LoadExpData()
        {
            ExpSaveData saveData = saveSystem.LoadData(SaveFileName, new ExpSaveData
            {
                SlotsUnlocked = 1,
            });
            return saveData;
        }
    }

    [MessagePackObject]
    [System.Serializable]
    public struct ExpSaveData
    {
        [Key(0)]
        public int Exp;
     
        [Key(1)]
        public int SlotsUnlocked;
        
        [Key(2)]
        public GemStoneSaveData[] GemStones;
        
        [Key(3)]
        public GemStoneSaveData[] ActiveGemStones;
    }

    [MessagePackObject]
    [System.Serializable]
    public struct GemStoneSaveData
    {
        [Key(4)]
        public int Seed;

        [Key(5)]
        public int Level;

        [Key(6)]
        public GemstoneType GemstoneType;
    }
}