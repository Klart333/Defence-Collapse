using System;
using UnityEngine;

namespace Gameplay.Research
{
    public class ResearchManager : Singleton<ResearchManager>
    {
        public event Action OnResearchChanged;
        
        public int ResearchCurrency { get; private set; }

        public void AddResearchPoints(int amount)
        {
            ResearchCurrency += amount;
            OnResearchChanged?.Invoke();
        }
    }
}