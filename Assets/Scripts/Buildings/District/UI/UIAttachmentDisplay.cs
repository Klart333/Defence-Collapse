using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine.UI;
using UnityEngine;
using DG.Tweening;
using Variables;
using TMPro;

namespace Buildings.District.UI
{
    public class UIAttachmentDisplay : PooledMonoBehaviour
    {
        [Title("Setup")]
        [SerializeField]
        private Image attachementImage;
        
        [SerializeField]
        private Image attackingProgressImage;

        [SerializeField]
        private TextMeshProUGUI attackingText;

        [SerializeField]
        private Image hasTargetIcon;
        
        [Title("Has Target")]
        [SerializeField]
        private SpriteReference hasTargetSprite;
        
        [SerializeField]
        private ColorReference hasTargetColor;
        
        [SerializeField]
        private SpriteReference noTargetSprite;
        
        [SerializeField]
        private ColorReference noTargetColor;
        
        [Title("Attacking")]
        [SerializeField]
        private StringReference attackingDescription;
        
        [Title("Animation")]
        [SerializeField]
        private float fillAnimationDuration = 1.0f;
        
        [SerializeField]
        private Ease fillAnimationEase = Ease.InOutSine;

        protected override void OnDisable()
        {
            base.OnDisable();

            attackingProgressImage.DOKill();
            attackingProgressImage.fillAmount = 0.0f;
        }

        public void DisplayAttachementData(DistrictData districtData, DistrictAttachmentData data)
        {
            float attackProgress = 1.0f - (data.AttackTimer / data.AttackSpeed);
            int turnsUntilAttack = (int)math.ceil(data.AttackTimer);
            Sprite targetIcon = data.HasTarget ? hasTargetSprite.Value : noTargetSprite.Value;
            Color targetColor = data.HasTarget ? hasTargetColor.Value : noTargetColor.Value;

            attackingText.text = attackingDescription.Variable.LocalizedText.GetLocalizedString(turnsUntilAttack);
            attachementImage.sprite = districtData.TowerData.Icon;
            hasTargetIcon.sprite = targetIcon;
            hasTargetIcon.color = targetColor;
            
            attackingProgressImage.DOKill();
            attackingProgressImage.DOFillAmount(attackProgress, fillAnimationDuration).SetEase(fillAnimationEase);
        }
    }
}