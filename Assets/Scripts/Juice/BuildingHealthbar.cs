using UnityEngine;

public class BuildingHealthbar : MonoBehaviour
{
    [SerializeField]
    private Transform fillHandle;

    [SerializeField]
    private float showTime = 5;

    private BuildingHandler buildingHandler;

    private float maxScale;
    private float timer;
    private bool show = true;
/*
    private void Start()
    {
        maxScale = fillHandle.transform.localScale.x;

        buildingHandler = GetComponent<BuildingHandler>();
        unit.UnitStats.MaxHealth.OnValueChanged += DisplayHealth;
        unit.OnTakeDamage += DisplayHealth;

        ToggleEnabled(false);
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

        float scale = Mathf.Lerp(0, maxScale, unit.Health.HealthPercentage);
        fillHandle.localScale = new Vector3(scale, fillHandle.localScale.y, fillHandle.localScale.z);

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
    }*/
}
