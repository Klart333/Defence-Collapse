using Sirenix.OdinInspector;
using UnityEngine.UI;
using UnityEngine;
using System;

namespace SkillTree.UI
{
    public class UISkillTreeNode : MonoBehaviour
    {
        public event Action OnClick;
        
        [Title("Setup")]
        [SerializeField]
        private Image iconImage;

        [SerializeField]
        private Button button;

        [SerializeField]
        private GameObject disabledOverlay;
        
        [SerializeField]
        private GameObject unlockedOverlay;
        
        public ISkillNodeDescription SkillNodeDescription { get; private set; }
        
        public void DisplaySkillNode(ISkillNodeDescription skillNode)
        {
            SkillNodeDescription = skillNode;
            iconImage.sprite = skillNode.Icon;
        }

        public void Clicked()
        {
            OnClick?.Invoke();
        }

        public void SetLocked(bool locked)
        {
            button.interactable = !locked;
            disabledOverlay.SetActive(locked);
        }

        public void SetUnlocked()
        {
            button.interactable = false;
            unlockedOverlay.SetActive(true);
        }
    }
}