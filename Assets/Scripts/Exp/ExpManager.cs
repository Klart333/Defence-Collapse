using System.Collections.Generic;
using Sirenix.OdinInspector;
using Exp.Gemstones;
using UnityEngine;
using Saving;
using System;
using Cysharp.Threading.Tasks;
using Effects;
using Gameplay.Event;

namespace Exp
{
    public class ExpManager : Singleton<ExpManager>
    {
        public event Action OnExpChanged;
        public event Action OnActiveGemstonesChanged;
        
        [Title("Gemstone")]
        [SerializeField]
        private GemstoneGenerator gemstoneGenerator;
        
        private PersistantSaveManager persistantSaveManager;
        
        public List<Gemstone> Gemstones { get; private set; }
        public List<Gemstone> ActiveGemstones { get; private set; }
        public int SlotsUnlocked { get; private set; }
        public int Exp { get; private set; }
        
        public Stat ExpMultiplier { get; private set; }
        
        protected override void Awake()
        {
            base.Awake();

            if (Instance != this) return;

            GetSaveManager().Forget();
         
            ExpMultiplier = new Stat(1);
            Events.OnGameReset += OnGameReset;
        }

        private async UniTaskVoid GetSaveManager()
        {
            persistantSaveManager = await PersistantSaveManager.Get();
            LoadSaveData();
        }

        private void OnDisable()
        {
            Events.OnGameReset -= OnGameReset;
        }

        private void OnGameReset()
        {
            ExpMultiplier = new Stat(1);
        }

        private void LoadSaveData()
        {
            ExpSaveData saveData = persistantSaveManager.ExpSaveLoad.LoadExpData();
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
            
            persistantSaveManager.ExpSaveLoad.SaveExpData(Exp, SlotsUnlocked, Gemstones, ActiveGemstones);
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