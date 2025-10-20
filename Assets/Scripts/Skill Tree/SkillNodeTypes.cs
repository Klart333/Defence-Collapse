using System;
using UnityEngine;

namespace SkillTree
{
    [Serializable]
    public class StatIncreaseSkillNode : ISkillNode
    {
        public ISkillNode Copy(ISkillNode source)
        {
            return new StatIncreaseSkillNode();
        }

        public void Unlock()
        {
            Debug.Log("Unlocked!");
        }
    }
}