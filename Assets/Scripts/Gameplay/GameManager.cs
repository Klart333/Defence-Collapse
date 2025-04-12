using System;
using UnityEngine;
using Random = System.Random;

namespace Gameplay
{
    public class GameManager : Singleton<GameManager>
    {
        public int Seed { get; private set; }

        private void OnEnable()
        {
#if UNITY_EDITOR
            SetupGame(new Random().Next());  
#endif   
        }
        
        
        public void SetupGame(int seed)
        {
            Seed = seed;
        }
    }
}