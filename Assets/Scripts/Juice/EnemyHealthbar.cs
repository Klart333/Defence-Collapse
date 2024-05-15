using DG.Tweening;
using UnityEngine;

public class EnemyHealthbar : MonoBehaviour
{
    [SerializeField]
    private Transform fillHandle;

    [SerializeField]
    private float showTime = 5;

    private EnemyHealth enemyHealth;

    private float maxScale;
    private float timer;
    private bool show = true;

    private void Awake()
    {
        maxScale = fillHandle.transform.localScale.x;

        ToggleEnabled(false);
    }

    public void Setup(EnemyHealth health)
    {
        enemyHealth = health;
        enemyHealth.Health.Attacker.Stats.MaxHealth.OnValueChanged += DisplayHealth;
        enemyHealth.Health.OnTakeDamage += DisplayHealth;
    }

    public void Reset()
    {
        ToggleEnabled(false);

        fillHandle.DOKill();
        fillHandle.localScale = new Vector3(maxScale, fillHandle.localScale.y, fillHandle.localScale.z);
    }

    private void Update()
    {
        if (show)
        {
            timer -= Time.deltaTime;

            if (timer <= 0)
            {
                ToggleEnabled(false);
            }
        }
    }

    private void DisplayHealth()
    {
        ToggleEnabled(true);

        fillHandle.DOKill();

        float scale = Mathf.Lerp(0, maxScale, enemyHealth.Health.HealthPercentage);
        fillHandle.DOScaleX(scale, 0.2f);

        timer = showTime;
    }

    private void ToggleEnabled(bool enabled)
    {
        if (show == enabled) return;

        show = enabled;
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(enabled);
        }
    }
}