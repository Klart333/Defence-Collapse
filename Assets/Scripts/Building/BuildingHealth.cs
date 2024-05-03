using System;
using UnityEngine;

public class BuildingHealth : MonoBehaviour, IHealth<BuildingHealth>
{
    public event Action<BuildingHealth> OnDeath;

    private Building building;

    private void Awake()
    {
        building = GetComponent<Building>();
    }

    public void TakeDamage(float amount)
    {
        building.BuildingHandler[building].Health.TakeDamage(amount);
    }
}
