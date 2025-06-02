using System;
using Saving;
using UnityEngine;

namespace Exp
{
    public class ExpManager : Singleton<ExpManager>
    {
        public int Exp { get; private set; }

        public void AddExp(int exp)
        {
            Exp += exp;
        }

        private void OnApplicationQuit()
        {
            //SaveLoad.Save
        }
    }
}