using Sirenix.OdinInspector;
using UnityEngine.UI;
using UnityEngine;
using System;
using TMPro;

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

        [Title("Debug")]
        [SerializeField]
        private TextMeshProUGUI indexText;
            
        private SkillNodePosition positionNode; 
        private RectTransform rectTransform;
        
        public ISkillNodeDescription SkillNodeDescription { get; private set; }

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        public void DisplaySkillNode(ISkillNodeDescription skillNode, SkillNodePosition skillNodePosition, int debugIndex)
        {
            indexText.text = debugIndex.ToString();
            
            positionNode = skillNodePosition;
            positionNode.OnPositionChanged += SetPositionToNode;
            
            SkillNodeDescription = skillNode;
            iconImage.sprite = skillNode.Icon;
            
            SetPositionToNode();
        }

        private void SetPositionToNode()
        {
            rectTransform.anchoredPosition = positionNode.Position;
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