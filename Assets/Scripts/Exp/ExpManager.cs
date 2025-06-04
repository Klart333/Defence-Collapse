using System;
using System.Collections.Generic;
using System.Diagnostics;
using Sirenix.OdinInspector;
using Exp.Gemstones;
using UnityEngine;
using Saving;
using Debug = UnityEngine.Debug;

namespace Exp
{
    public class ExpManager : Singleton<ExpManager>
    {
        public event Action OnExpChanged;
        public event Action OnActiveGemstonesChanged;
        
        [Title("Gemstone")]
        [SerializeField]
        private GemstoneGenerator gemstoneGenerator;
        
        public List<Gemstone> Gemstones { get; private set; }
        public List<Gemstone> ActiveGemstones { get; private set; }
        public int SlotsUnlocked { get; private set; }
        public int Exp { get; private set; }
        
        private ExpSaveLoad saveLoad;

        protected override void Awake()
        {
            base.Awake();

            if (Instance != this) return;
            
            saveLoad = new ExpSaveLoad(new MessagePackSaveSystem());
            LoadSaveData();

            //Gemstones = new List<Gemstone>();
            //for (int i = 0; i < 10; i++)
            //{
            //    Gemstones.Add(gemstoneGenerator.GetGemstone((GemstoneType)UnityEngine.Random.Range(0, 3), UnityEngine.Random.Range(1, 100), UnityEngine.Random.Range(0, int.MaxValue)));
            //}
        }

        private void LoadSaveData()
        {
            ExpSaveData saveData = saveLoad.LoadExpData();
            Exp = saveData.Exp;
            SlotsUnlocked = Mathf.Max(1, saveData.SlotsUnlocked);

            if (saveData.GemStones == null)
            {
                Gemstones = new List<Gemstone>();
            }
            else
            {
                Gemstones = new List<Gemstone>(saveData.GemStones.Length);
                for (int i = 0; i < saveData.GemStones.Length; i++)
                {
                    Gemstone gemstone = gemstoneGenerator.GetGemstoneFromSaveData(saveData.GemStones[i]);
                    Gemstones.Add(gemstone); 
                }
            }

            if (saveData.ActiveGemStones == null)
            {
                ActiveGemstones = new List<Gemstone>();
            }
            else
            {
                ActiveGemstones = new List<Gemstone>(saveData.ActiveGemStones.Length);
                for (int i = 0; i < saveData.ActiveGemStones.Length; i++)
                {
                    Gemstone gemstone = gemstoneGenerator.GetGemstoneFromSaveData(saveData.ActiveGemStones[i]);
                    ActiveGemstones.Add(gemstone);
                }
            }
        }

        public void AddExp(int exp)
        {
            Exp += exp;
            OnExpChanged?.Invoke();
        }

        public void RemoveExp(int exp)
        {
            Exp -= exp;
            OnExpChanged?.Invoke();
        }

        public void UnlockSlot()
        {
            SlotsUnlocked++;
        }

        public void AddGemstone(Gemstone gemstone)
        {
            Gemstones.Add(gemstone);
        }

        public void RemoveGemstone(Gemstone gemstone)
        {
            Gemstones.Remove(gemstone);
        }
        
        public void AddActiveGemstone(Gemstone gem)
        {
            ActiveGemstones.Add(gem);
            OnActiveGemstonesChanged?.Invoke();
        }

        public void RemoveActiveGemstone(Gemstone gem)
        {
            ActiveGemstones.Remove(gem);
            OnActiveGemstonesChanged?.Invoke();
        }
            
        private void OnApplicationQuit()
        {
            if (Instance != this) return;
            
            saveLoad.SaveExpData(Exp, SlotsUnlocked, Gemstones, ActiveGemstones);
        }

#if UNITY_EDITOR

        [Button]
        private void DebugAddExp(int amount = 1000)
        {
            AddExp(amount);
        }
        
#endif
    }
}