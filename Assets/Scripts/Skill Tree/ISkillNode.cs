using UnityEngine;

namespace SkillTree
{
    public interface ISkillNodeDescription
    {
        public string Title { get; }
        public string Description { get; }
        public Sprite Icon { get; }
        public float ExpCost { get; }
    }

    public interface ISkillNode
    {
        public ISkillNode Copy(ISkillNode source);
        public void Unlock();
    }
}