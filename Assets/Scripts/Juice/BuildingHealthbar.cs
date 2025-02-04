using DG.Tweening;
using System;
using UnityEngine;

public class BuildingHealthbar : MonoBehaviour
{
    [SerializeField]
    private Transform fillHandle;

    [SerializeField]
    private float showTime = 5;

    private Building building;

    private float maxScale;
    private float timer;
    private bool show = true;

    public Building Building
    {
        get
        {
            if (building == null)
            {
                building = GetComponent<Building>();
            }

            return building;
        }
    }

    private void Awake()
    {
        maxScale = fillHandle.transform.localScale.x;
    }

    private void OnEnable()
    {
        Building.BuildingHandler[building].Health.Stats.MaxHealth.OnValueChanged += DisplayHealth;
        Building.BuildingHandler[building].Health.OnTakeDamage += DisplayHealth;

        ToggleEnabled(false);
    }

    private void OnDisable()
    {
        Building.BuildingHandler[building].Health.Stats.MaxHealth.OnValueChanged -= DisplayHealth;
        Building.BuildingHandler[building].Health.OnTakeDamage -= DisplayHealth;

        Reset();
    }

    private void Reset()
    {
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

        fillHandle.DOKill();

        float scale = Mathf.Lerp(0, maxScale, Building.BuildingHandler[building].Health.HealthPercentage);
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
