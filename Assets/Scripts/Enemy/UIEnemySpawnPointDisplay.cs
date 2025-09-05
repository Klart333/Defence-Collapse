using UnityEngine.EventSystems;
using Sirenix.OdinInspector;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

namespace Enemy
{
    public class UIEnemySpawnPointDisplay : PooledMonoBehaviour//, IPointerEnterHandler, IPointerExitHandler
    {
        /*[Title("References")]
        [SerializeField]
        private Image selectedImage;

        [SerializeField]
        private TextMeshProUGUI levelText;
        
        [Title("Eyes")]
        [SerializeField]
        private Image eyesImage;

        [SerializeField]
        private Gradient eyeGradient;

        [SerializeField]
        private AnimationCurve gradientCurve;
        
        private EnemySpawnPoint enemySpawnPoint;

        private void OnEnable()
        {
            Events.OnWaveStarted += OnWaveStarted;

            eyesImage.color = eyeGradient.Evaluate(0);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            
            Events.OnWaveStarted -= OnWaveStarted;
            eyesImage.color = eyeGradient.Evaluate(0);
        }

        private void OnWaveStarted()
        {
            float level = Mathf.FloorToInt(enemySpawnPoint.SpawnLevel / (float)enemySpawnPoint.LevelFrequency) + enemySpawnPoint.SpawnLevel / (enemySpawnPoint.LevelFrequency * 5.0f);
            float value = gradientCurve.Evaluate(level);
            eyesImage.color = eyeGradient.Evaluate(value);
        }

        public void DisplayPoint(Vector3 point, EnemySpawnPoint spawnPoint)
        {
            enemySpawnPoint = spawnPoint;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            selectedImage.gameObject.SetActive(true);
            levelText.gameObject.SetActive(true);

            //levelText.text = $"Level {enemySpawnPoint.SpawnLevel:N0}";
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            selectedImage.gameObject.SetActive(false);
            levelText.gameObject.SetActive(false);
        }*/
    }
}